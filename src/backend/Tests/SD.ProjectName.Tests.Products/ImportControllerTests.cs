using Xunit;
using ARP.ESG_ReportStudio.API.Controllers;
using ARP.ESG_ReportStudio.API.Reporting;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace SD.ProjectName.Tests.Products;

public sealed class ImportControllerTests
{
    private readonly InMemoryReportStore _store;
    private readonly ImportController _controller;

    public ImportControllerTests()
    {
        _store = new InMemoryReportStore();
        _controller = new ImportController(_store);
        
        // Set up organization (required before creating periods)
        _store.CreateOrganization(new CreateOrganizationRequest
        {
            Name = "Test Organization",
            LegalForm = "Limited",
            Country = "USA",
            Identifier = "TEST-ORG-001",
            CreatedBy = "user-1"
        });
        
        // Add an organizational unit (required before creating periods)
        _store.CreateOrganizationalUnit(new CreateOrganizationalUnitRequest
        {
            Name = "Main Office",
            Description = "Headquarters",
            CreatedBy = "user-1"
        });
        
        // Add a test reporting period (this automatically creates sections)
        _store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
        {
            Name = "2024 Annual Report",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user-1",
            OwnerName = "Sarah Chen"
        });
    }

    [Fact]
    public void GetTemplate_ShouldReturnCsvFile()
    {
        // Act
        var result = _controller.GetTemplate();

        // Assert
        Assert.NotNull(result);
        var fileResult = Assert.IsType<Microsoft.AspNetCore.Mvc.FileContentResult>(result);
        Assert.Equal("text/csv", fileResult.ContentType);
        Assert.Equal("data-points-template.csv", fileResult.FileDownloadName);
        
        var content = Encoding.UTF8.GetString(fileResult.FileContents);
        Assert.Contains("SectionId,Type,Classification,Title", content);
    }

    [Fact]
    public async Task ImportDataPoints_WithValidCsv_ShouldSucceed()
    {
        // Arrange
        var periodId = _store.GetPeriods()[0].Id;
        var sections = _store.GetSections(periodId);
        var sectionId = sections[0].Id;
        
        var csvContent = @"SectionId,Type,Classification,Title,Content,Value,Unit,OwnerId,ContributorIds,Source,InformationType,Assumptions,CompletenessStatus
" + sectionId + @",metric,fact,Total Energy,Energy consumption data,1250,MWh,user-1,user-3,Internal metering,fact,,incomplete";

        var file = CreateFormFile(csvContent, "test.csv");

        // Act
        var result = await _controller.ImportDataPoints(file);

        // Assert
        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var importResult = Assert.IsType<ImportResult>(okResult.Value);
        
        Assert.Equal(1, importResult.TotalRows);
        Assert.Equal(1, importResult.SuccessCount);
        Assert.Equal(0, importResult.ErrorCount);
        Assert.Single(importResult.SuccessfulRows);
        Assert.Empty(importResult.FailedRows);
    }

    [Fact]
    public async Task ImportDataPoints_WithInvalidRow_ShouldReturnPartialSuccess()
    {
        // Arrange
        var periodId = _store.GetPeriods()[0].Id;
        var sections = _store.GetSections(periodId);
        var sectionId = sections[0].Id;
        
        // Second row has invalid owner ID
        var csvContent = @"SectionId,Type,Classification,Title,Content,Value,Unit,OwnerId,ContributorIds,Source,InformationType,Assumptions,CompletenessStatus
" + sectionId + @",metric,fact,Valid Entry,Valid content,100,kg,user-1,user-3,Valid source,fact,,incomplete
" + sectionId + @",metric,fact,Invalid Entry,Invalid owner,200,kg,invalid-owner,user-3,Source,fact,,incomplete";

        var file = CreateFormFile(csvContent, "test.csv");

        // Act
        var result = await _controller.ImportDataPoints(file);

        // Assert
        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var importResult = Assert.IsType<ImportResult>(okResult.Value);
        
        Assert.Equal(2, importResult.TotalRows);
        Assert.Equal(1, importResult.SuccessCount);
        Assert.Equal(1, importResult.ErrorCount);
        Assert.Single(importResult.SuccessfulRows);
        Assert.Single(importResult.FailedRows);
        Assert.Contains("not found", importResult.FailedRows[0].ErrorMessage);
    }

    [Fact]
    public async Task ImportDataPoints_WithMissingRequiredFields_ShouldFail()
    {
        // Arrange
        var periodId = _store.GetPeriods()[0].Id;
        var sections = _store.GetSections(periodId);
        var sectionId = sections[0].Id;
        
        // Missing title and content
        var csvContent = @"SectionId,Type,Classification,Title,Content,Value,Unit,OwnerId,ContributorIds,Source,InformationType,Assumptions,CompletenessStatus
" + sectionId + @",metric,fact,,,100,kg,user-1,user-3,Source,fact,,incomplete";

        var file = CreateFormFile(csvContent, "test.csv");

        // Act
        var result = await _controller.ImportDataPoints(file);

        // Assert
        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var importResult = Assert.IsType<ImportResult>(okResult.Value);
        
        Assert.Equal(1, importResult.TotalRows);
        Assert.Equal(0, importResult.SuccessCount);
        Assert.Equal(1, importResult.ErrorCount);
        Assert.Single(importResult.FailedRows);
        Assert.Contains("required", importResult.FailedRows[0].ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ImportDataPoints_WithEstimateTypeAndNoAssumptions_ShouldFail()
    {
        // Arrange
        var periodId = _store.GetPeriods()[0].Id;
        var sections = _store.GetSections(periodId);
        var sectionId = sections[0].Id;
        
        var csvContent = @"SectionId,Type,Classification,Title,Content,Value,Unit,OwnerId,ContributorIds,Source,InformationType,Assumptions,CompletenessStatus
" + sectionId + @",metric,fact,Estimated Data,Estimated value,100,kg,user-1,user-3,Source,estimate,,incomplete";

        var file = CreateFormFile(csvContent, "test.csv");

        // Act
        var result = await _controller.ImportDataPoints(file);

        // Assert
        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var importResult = Assert.IsType<ImportResult>(okResult.Value);
        
        Assert.Equal(1, importResult.TotalRows);
        Assert.Equal(0, importResult.SuccessCount);
        Assert.Equal(1, importResult.ErrorCount);
        Assert.Contains("Assumptions", importResult.FailedRows[0].ErrorMessage);
    }

    [Fact]
    public async Task ImportDataPoints_WithMultipleContributors_ShouldSucceed()
    {
        // Arrange
        var periodId = _store.GetPeriods()[0].Id;
        var sections = _store.GetSections(periodId);
        var sectionId = sections[0].Id;
        
        // Multiple contributors separated by semicolons
        var csvContent = @"SectionId,Type,Classification,Title,Content,Value,Unit,OwnerId,ContributorIds,Source,InformationType,Assumptions,CompletenessStatus
" + sectionId + @",metric,fact,Multi Contributor,Data with multiple contributors,100,kg,user-1,user-3;user-4;user-5,Source,fact,,incomplete";

        var file = CreateFormFile(csvContent, "test.csv");

        // Act
        var result = await _controller.ImportDataPoints(file);

        // Assert
        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var importResult = Assert.IsType<ImportResult>(okResult.Value);
        
        Assert.Equal(1, importResult.TotalRows);
        Assert.Equal(1, importResult.SuccessCount);
        Assert.Equal(0, importResult.ErrorCount);
        
        // Verify the data point was created with correct contributors
        var dataPoints = _store.GetDataPoints(sectionId);
        var dataPoint = dataPoints.First(dp => dp.Title == "Multi Contributor");
        Assert.Equal(3, dataPoint.ContributorIds.Count);
        Assert.Contains("user-3", dataPoint.ContributorIds);
        Assert.Contains("user-4", dataPoint.ContributorIds);
        Assert.Contains("user-5", dataPoint.ContributorIds);
    }

    [Fact]
    public async Task ImportDataPoints_WithNoFile_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.ImportDataPoints(null!);

        // Assert
        var badRequestResult = Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task ImportDataPoints_WithNonCsvFile_ShouldReturnBadRequest()
    {
        // Arrange
        var file = CreateFormFile("some content", "test.txt");

        // Act
        var result = await _controller.ImportDataPoints(file);

        // Assert
        var badRequestResult = Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    private static IFormFile CreateFormFile(string content, string fileName)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/csv"
        };
    }
}
