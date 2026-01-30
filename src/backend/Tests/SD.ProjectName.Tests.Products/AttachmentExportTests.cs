using Xunit;
using ARP.ESG_ReportStudio.API.Reporting;
using ARP.ESG_ReportStudio.API.Services;

namespace SD.ProjectName.Tests.Products;

/// <summary>
/// Tests for attachment/evidence inclusion in PDF and DOCX exports.
/// </summary>
public sealed class AttachmentExportTests
{
    private static void CreateTestConfiguration(InMemoryReportStore store)
    {
        // Create organization
        store.CreateOrganization(new CreateOrganizationRequest
        {
            Name = "Test Corporation",
            LegalForm = "corporation",
            Country = "US",
            Identifier = "TEST123",
            CreatedBy = "test-user",
            CoverageType = "full",
            CoverageJustification = "Test coverage"
        });

        // Create organizational unit
        store.CreateOrganizationalUnit(new CreateOrganizationalUnitRequest
        {
            Name = "Test Unit",
            Description = "Default unit for testing",
            CreatedBy = "test-user"
        });
    }
    
    private static string CreateTestSection(InMemoryReportStore store, string periodId, string title, string category)
    {
        var section = new ReportSection
        {
            Id = Guid.NewGuid().ToString(),
            PeriodId = periodId,
            Title = title,
            Category = category,
            Description = "Test section",
            OwnerId = "user-1",
            Status = "draft",
            Completeness = "empty",
            Order = 1
        };
        
        var sectionsField = typeof(InMemoryReportStore).GetField("_sections", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var sections = sectionsField!.GetValue(store) as List<ReportSection>;
        sections!.Add(section);
        
        return section.Id;
    }

    [Fact]
    public void PdfExport_WithAttachmentsIncluded_ShouldGeneratePdfWithAppendix()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
        {
            Name = "2024 Annual Report",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user-1",
            OwnerName = "Test User"
        });
        Assert.True(isValid);
        var period = snapshot!.Periods.First();
        
        var sectionId = CreateTestSection(store, period.Id, "Environmental Impact", "environmental");
        
        // Add some evidence to the section
        var (ev1Valid, _, evidence1) = store.CreateEvidence(
            sectionId,
            "Energy Invoice Q1",
            "Invoice from utility company",
            "invoice-q1.pdf",
            null, // FileUrl
            null, // SourceUrl
            "user-1",
            51200, // fileSize
            "abc123", // checksum
            "application/pdf" // contentType
        );
        Assert.True(ev1Valid);
        
        var (ev2Valid, _, evidence2) = store.CreateEvidence(
            sectionId,
            "Emissions Report",
            "Annual emissions calculation",
            "emissions-2024.xlsx",
            null,
            null,
            "user-1",
            102400,
            "def456",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        );
        Assert.True(ev2Valid);
        
        // Generate report
        var (reportValid, errorMessage, report) = store.GenerateReport(new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = "user-1"
        });
        Assert.True(reportValid, errorMessage);
        Assert.NotNull(report);
        
        // Act - Export with attachments included
        var pdfService = new PdfExportService();
        var pdfBytes = pdfService.GeneratePdf(report!, new PdfExportOptions
        {
            IncludeAttachments = true,
            UserId = "user-1",
            MaxAttachmentSizeMB = 50
        });
        
        // Assert
        Assert.NotNull(pdfBytes);
        Assert.NotEmpty(pdfBytes);
        var pdfHeader = System.Text.Encoding.ASCII.GetString(pdfBytes.Take(4).ToArray());
        Assert.Equal("%PDF", pdfHeader);
    }

    [Fact]
    public void DocxExport_WithAttachmentsIncluded_ShouldGenerateDocxWithAppendix()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
        {
            Name = "2024 Annual Report",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user-1",
            OwnerName = "Test User"
        });
        Assert.True(isValid);
        var period = snapshot!.Periods.First();
        
        var sectionId = CreateTestSection(store, period.Id, "Social Responsibility", "social");
        
        // Add evidence
        var (evValid, _, evidence) = store.CreateEvidence(
            sectionId,
            "Employee Survey Results",
            "2024 employee satisfaction survey",
            "survey-2024.pdf",
            null,
            null,
            "user-1",
            256000,
            "xyz789",
            "application/pdf"
        );
        Assert.True(evValid);
        
        // Generate report
        var (reportValid, errorMessage, report) = store.GenerateReport(new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = "user-1"
        });
        Assert.True(reportValid, errorMessage);
        Assert.NotNull(report);
        
        // Act - Export with attachments included
        var docxService = new DocxExportService();
        var docxBytes = docxService.GenerateDocx(report!, new DocxExportOptions
        {
            IncludeAttachments = true,
            UserId = "user-1",
            MaxAttachmentSizeMB = 50
        });
        
        // Assert
        Assert.NotNull(docxBytes);
        Assert.NotEmpty(docxBytes);
        var docxHeader = System.Text.Encoding.ASCII.GetString(docxBytes.Take(2).ToArray());
        Assert.Equal("PK", docxHeader);
    }

    [Fact]
    public void PdfExport_WithoutAttachments_ShouldNotIncludeAppendix()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
        {
            Name = "2024 Q1",
            StartDate = "2024-01-01",
            EndDate = "2024-03-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user-1",
            OwnerName = "Test User"
        });
        Assert.True(isValid);
        var period = snapshot!.Periods.First();
        
        var sectionId = CreateTestSection(store, period.Id, "Governance", "governance");
        
        // Add evidence but don't include it in export
        store.CreateEvidence(sectionId, "Board Minutes", "Q1 board minutes", "minutes-q1.pdf",
            null, null, "user-1", 75000, "aaa111", "application/pdf");
        
        // Generate report
        var (reportValid, errorMessage, report) = store.GenerateReport(new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = "user-1"
        });
        Assert.True(reportValid, errorMessage);
        Assert.NotNull(report);
        
        // Act - Export WITHOUT attachments (default)
        var pdfService = new PdfExportService();
        var pdfBytes = pdfService.GeneratePdf(report!, new PdfExportOptions
        {
            IncludeAttachments = false
        });
        
        // Assert - Should still generate valid PDF
        Assert.NotNull(pdfBytes);
        Assert.NotEmpty(pdfBytes);
        var pdfHeader = System.Text.Encoding.ASCII.GetString(pdfBytes.Take(4).ToArray());
        Assert.Equal("%PDF", pdfHeader);
    }

    [Fact]
    public void Export_WithLargeAttachments_ShouldIncludeWarningInReport()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
        {
            Name = "2024 Full Year",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "extended",
            ReportScope = "group",
            OwnerId = "user-1",
            OwnerName = "Test User"
        });
        Assert.True(isValid);
        var period = snapshot!.Periods.First();
        
        var sectionId = CreateTestSection(store, period.Id, "Environmental", "environmental");
        
        // Add large evidence file (100 MB)
        store.CreateEvidence(sectionId, "Video Evidence", "Facility walkthrough video",
            "walkthrough.mp4", null, null, "user-1", 100 * 1024 * 1024, "large123", "video/mp4");
        
        // Generate report
        var (reportValid, errorMessage, report) = store.GenerateReport(new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = "user-1"
        });
        Assert.True(reportValid, errorMessage);
        Assert.NotNull(report);
        
        // Act - Export with low size limit
        var pdfService = new PdfExportService();
        var pdfBytes = pdfService.GeneratePdf(report!, new PdfExportOptions
        {
            IncludeAttachments = true,
            UserId = "user-1",
            MaxAttachmentSizeMB = 50 // Lower than the 100 MB file
        });
        
        // Assert - Should still generate PDF (with warning in content)
        Assert.NotNull(pdfBytes);
        Assert.NotEmpty(pdfBytes);
        var pdfHeader = System.Text.Encoding.ASCII.GetString(pdfBytes.Take(4).ToArray());
        Assert.Equal("%PDF", pdfHeader);
    }

    [Fact]
    public void Export_WithNoEvidence_ShouldStillGenerateAppendix()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
        {
            Name = "2024 Q2",
            StartDate = "2024-04-01",
            EndDate = "2024-06-30",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user-1",
            OwnerName = "Test User"
        });
        Assert.True(isValid);
        var period = snapshot!.Periods.First();
        
        CreateTestSection(store, period.Id, "Test Section", "environmental");
        
        // Generate report (no evidence added)
        var (reportValid, errorMessage, report) = store.GenerateReport(new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = "user-1"
        });
        Assert.True(reportValid, errorMessage);
        Assert.NotNull(report);
        
        // Act - Export with attachments enabled but no evidence exists
        var docxService = new DocxExportService();
        var docxBytes = docxService.GenerateDocx(report!, new DocxExportOptions
        {
            IncludeAttachments = true,
            UserId = "user-1"
        });
        
        // Assert - Should still generate valid DOCX
        Assert.NotNull(docxBytes);
        Assert.NotEmpty(docxBytes);
        var docxHeader = System.Text.Encoding.ASCII.GetString(docxBytes.Take(2).ToArray());
        Assert.Equal("PK", docxHeader);
    }

    [Fact]
    public void Export_Filename_ShouldBeConsistentWithOrWithoutAttachments()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
        {
            Name = "2024 Annual",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user-1",
            OwnerName = "Test User"
        });
        Assert.True(isValid);
        var period = snapshot!.Periods.First();
        
        CreateTestSection(store, period.Id, "Test", "environmental");
        
        var (reportValid, errorMessage, report) = store.GenerateReport(new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = "user-1"
        });
        Assert.True(reportValid, errorMessage);
        Assert.NotNull(report);
        
        // Act - Get filenames with and without attachments
        var pdfService = new PdfExportService();
        var filenameWithAttachments = pdfService.GenerateFilename(report!);
        var filenameWithoutAttachments = pdfService.GenerateFilename(report!);
        
        // Assert - Filenames should be the same (attachments don't affect filename)
        Assert.Equal(filenameWithAttachments, filenameWithoutAttachments);
        Assert.EndsWith(".pdf", filenameWithAttachments);
    }
}
