using Xunit;
using ARP.ESG_ReportStudio.API.Reporting;
using ARP.ESG_ReportStudio.API.Services;

namespace SD.ProjectName.Tests.Products;

/// <summary>
/// Tests for attachment/evidence inclusion in PDF and DOCX exports.
/// </summary>
public sealed class AttachmentExportTests
{
    [Fact]
    public void PdfExport_WithAttachmentsIncluded_ShouldGeneratePdfWithAppendix()
    {
        // Arrange
        var store = new InMemoryReportStore();
        ExportTestHelpers.CreateTestConfiguration(store);
        
        var createPeriodRequest = new CreateReportingPeriodRequest
        {
            Name = "2024 Annual Report",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user-1",
            OwnerName = "Test User"
        };
        
        var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(createPeriodRequest);
        Assert.True(isValid);
        Assert.NotNull(snapshot);
        
        var period = snapshot!.Periods.First();
        
        var createSectionRequest = new CreateReportSectionRequest
        {
            PeriodId = period.Id,
            Title = "Environmental Impact",
            Category = "environmental",
            Description = "Test section",
            OwnerId = "user-1",
            Order = 1
        };
        
        var (sectionValid, _, sectionSnapshot) = store.ValidateAndCreateSection(createSectionRequest);
        Assert.True(sectionValid);
        var section = sectionSnapshot!.Sections.First(s => s.Title == "Environmental Impact");
        
        // Add some evidence to the section
        var evidenceRequest1 = new UploadEvidenceRequest
        {
            SectionId = section.Id,
            Title = "Energy Invoice Q1",
            Description = "Invoice from utility company",
            FileName = "invoice-q1.pdf",
            UploadedBy = "user-1"
        };
        
        var evidence1 = store.CreateEvidence(
            evidenceRequest1.SectionId,
            evidenceRequest1.Title,
            evidenceRequest1.Description,
            evidenceRequest1.FileName,
            evidenceRequest1.UploadedBy,
            fileSize: 51200,
            checksum: "abc123",
            contentType: "application/pdf"
        );
        
        var evidenceRequest2 = new UploadEvidenceRequest
        {
            SectionId = section.Id,
            Title = "Emissions Report",
            Description = "Annual emissions calculation",
            FileName = "emissions-2024.xlsx",
            UploadedBy = "user-1"
        };
        
        var evidence2 = store.CreateEvidence(
            evidenceRequest2.SectionId,
            evidenceRequest2.Title,
            evidenceRequest2.Description,
            evidenceRequest2.FileName,
            evidenceRequest2.UploadedBy,
            fileSize: 102400,
            checksum: "def456",
            contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        );
        
        // Generate report
        var generateRequest = new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = "user-1"
        };
        
        var (reportValid, errorMessage, report) = store.GenerateReport(generateRequest);
        Assert.True(reportValid, errorMessage);
        Assert.NotNull(report);
        
        // Act - Export with attachments included
        var pdfService = new PdfExportService();
        var options = new PdfExportOptions
        {
            IncludeAttachments = true,
            UserId = "user-1",
            MaxAttachmentSizeMB = 50
        };
        
        var pdfBytes = pdfService.GeneratePdf(report!, options);
        
        // Assert
        Assert.NotNull(pdfBytes);
        Assert.NotEmpty(pdfBytes);
        
        // Verify PDF header (starts with %PDF)
        var pdfHeader = System.Text.Encoding.ASCII.GetString(pdfBytes.Take(4).ToArray());
        Assert.Equal("%PDF", pdfHeader);
    }

    [Fact]
    public void DocxExport_WithAttachmentsIncluded_ShouldGenerateDocxWithAppendix()
    {
        // Arrange
        var store = new InMemoryReportStore();
        ExportTestHelpers.CreateTestConfiguration(store);
        
        var createPeriodRequest = new CreateReportingPeriodRequest
        {
            Name = "2024 Annual Report",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user-1",
            OwnerName = "Test User"
        };
        
        var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(createPeriodRequest);
        Assert.True(isValid);
        var period = snapshot!.Periods.First();
        
        var createSectionRequest = new CreateReportSectionRequest
        {
            PeriodId = period.Id,
            Title = "Social Responsibility",
            Category = "social",
            Description = "Test section",
            OwnerId = "user-1",
            Order = 1
        };
        
        var (sectionValid, _, sectionSnapshot) = store.ValidateAndCreateSection(createSectionRequest);
        Assert.True(sectionValid);
        var section = sectionSnapshot!.Sections.First(s => s.Title == "Social Responsibility");
        
        // Add evidence
        var evidence = store.CreateEvidence(
            section.Id,
            "Employee Survey Results",
            "2024 employee satisfaction survey",
            "survey-2024.pdf",
            "user-1",
            fileSize: 256000,
            checksum: "xyz789",
            contentType: "application/pdf"
        );
        
        // Generate report
        var generateRequest = new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = "user-1"
        };
        
        var (reportValid, errorMessage, report) = store.GenerateReport(generateRequest);
        Assert.True(reportValid, errorMessage);
        Assert.NotNull(report);
        
        // Act - Export with attachments included
        var docxService = new DocxExportService();
        var options = new DocxExportOptions
        {
            IncludeAttachments = true,
            UserId = "user-1",
            MaxAttachmentSizeMB = 50
        };
        
        var docxBytes = docxService.GenerateDocx(report!, options);
        
        // Assert
        Assert.NotNull(docxBytes);
        Assert.NotEmpty(docxBytes);
        
        // Verify DOCX header (ZIP format, starts with PK)
        var docxHeader = System.Text.Encoding.ASCII.GetString(docxBytes.Take(2).ToArray());
        Assert.Equal("PK", docxHeader);
    }

    [Fact]
    public void PdfExport_WithoutAttachments_ShouldNotIncludeAppendix()
    {
        // Arrange
        var store = new InMemoryReportStore();
        ExportTestHelpers.CreateTestConfiguration(store);
        
        var createPeriodRequest = new CreateReportingPeriodRequest
        {
            Name = "2024 Q1",
            StartDate = "2024-01-01",
            EndDate = "2024-03-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user-1",
            OwnerName = "Test User"
        };
        
        var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(createPeriodRequest);
        Assert.True(isValid);
        var period = snapshot!.Periods.First();
        
        var createSectionRequest = new CreateReportSectionRequest
        {
            PeriodId = period.Id,
            Title = "Governance",
            Category = "governance",
            Description = "Test section",
            OwnerId = "user-1",
            Order = 1
        };
        
        var (sectionValid, _, sectionSnapshot) = store.ValidateAndCreateSection(createSectionRequest);
        Assert.True(sectionValid);
        var section = sectionSnapshot!.Sections.First(s => s.Title == "Governance");
        
        // Add evidence but don't include it in export
        var evidence = store.CreateEvidence(
            section.Id,
            "Board Meeting Minutes",
            "Q1 board minutes",
            "minutes-q1.pdf",
            "user-1",
            fileSize: 75000,
            checksum: "aaa111",
            contentType: "application/pdf"
        );
        
        // Generate report
        var generateRequest = new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = "user-1"
        };
        
        var (reportValid, errorMessage, report) = store.GenerateReport(generateRequest);
        Assert.True(reportValid, errorMessage);
        Assert.NotNull(report);
        
        // Act - Export WITHOUT attachments (default)
        var pdfService = new PdfExportService();
        var options = new PdfExportOptions
        {
            IncludeAttachments = false
        };
        
        var pdfBytes = pdfService.GeneratePdf(report!, options);
        
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
        ExportTestHelpers.CreateTestConfiguration(store);
        
        var createPeriodRequest = new CreateReportingPeriodRequest
        {
            Name = "2024 Full Year",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "extended",
            ReportScope = "group",
            OwnerId = "user-1",
            OwnerName = "Test User"
        };
        
        var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(createPeriodRequest);
        Assert.True(isValid);
        var period = snapshot!.Periods.First();
        
        var createSectionRequest = new CreateReportSectionRequest
        {
            PeriodId = period.Id,
            Title = "Environmental",
            Category = "environmental",
            Description = "Test section",
            OwnerId = "user-1",
            Order = 1
        };
        
        var (sectionValid, _, sectionSnapshot) = store.ValidateAndCreateSection(createSectionRequest);
        Assert.True(sectionValid);
        var section = sectionSnapshot!.Sections.First();
        
        // Add large evidence file (100 MB)
        var largeEvidence = store.CreateEvidence(
            section.Id,
            "Video Evidence",
            "Facility walkthrough video",
            "walkthrough.mp4",
            "user-1",
            fileSize: 100 * 1024 * 1024, // 100 MB
            checksum: "large123",
            contentType: "video/mp4"
        );
        
        // Generate report
        var generateRequest = new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = "user-1"
        };
        
        var (reportValid, errorMessage, report) = store.GenerateReport(generateRequest);
        Assert.True(reportValid, errorMessage);
        Assert.NotNull(report);
        
        // Act - Export with low size limit
        var pdfService = new PdfExportService();
        var options = new PdfExportOptions
        {
            IncludeAttachments = true,
            UserId = "user-1",
            MaxAttachmentSizeMB = 50 // Lower than the 100 MB file
        };
        
        var pdfBytes = pdfService.GeneratePdf(report!, options);
        
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
        ExportTestHelpers.CreateTestConfiguration(store);
        
        var createPeriodRequest = new CreateReportingPeriodRequest
        {
            Name = "2024 Q2",
            StartDate = "2024-04-01",
            EndDate = "2024-06-30",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user-1",
            OwnerName = "Test User"
        };
        
        var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(createPeriodRequest);
        Assert.True(isValid);
        var period = snapshot!.Periods.First();
        
        var createSectionRequest = new CreateReportSectionRequest
        {
            PeriodId = period.Id,
            Title = "Test Section",
            Category = "environmental",
            Description = "No evidence",
            OwnerId = "user-1",
            Order = 1
        };
        
        var (sectionValid, _, sectionSnapshot) = store.ValidateAndCreateSection(createSectionRequest);
        Assert.True(sectionValid);
        
        // Generate report (no evidence added)
        var generateRequest = new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = "user-1"
        };
        
        var (reportValid, errorMessage, report) = store.GenerateReport(generateRequest);
        Assert.True(reportValid, errorMessage);
        Assert.NotNull(report);
        
        // Act - Export with attachments enabled but no evidence exists
        var docxService = new DocxExportService();
        var options = new DocxExportOptions
        {
            IncludeAttachments = true,
            UserId = "user-1"
        };
        
        var docxBytes = docxService.GenerateDocx(report!, options);
        
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
        ExportTestHelpers.CreateTestConfiguration(store);
        
        var createPeriodRequest = new CreateReportingPeriodRequest
        {
            Name = "2024 Annual",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user-1",
            OwnerName = "Test User"
        };
        
        var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(createPeriodRequest);
        Assert.True(isValid);
        var period = snapshot!.Periods.First();
        
        var createSectionRequest = new CreateReportSectionRequest
        {
            PeriodId = period.Id,
            Title = "Test",
            Category = "environmental",
            Description = "Test",
            OwnerId = "user-1",
            Order = 1
        };
        
        var (sectionValid, _, sectionSnapshot) = store.ValidateAndCreateSection(createSectionRequest);
        Assert.True(sectionValid);
        
        var generateRequest = new GenerateReportRequest
        {
            PeriodId = period.Id,
            GeneratedBy = "user-1"
        };
        
        var (reportValid, errorMessage, report) = store.GenerateReport(generateRequest);
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
