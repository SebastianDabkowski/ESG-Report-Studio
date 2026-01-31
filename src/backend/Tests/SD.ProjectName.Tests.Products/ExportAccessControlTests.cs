using ARP.ESG_ReportStudio.API.Reporting;
using ARP.ESG_ReportStudio.API.Controllers;
using ARP.ESG_ReportStudio.API.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace SD.ProjectName.Tests.Products
{
    public class ExportAccessControlTests
    {
        private static InMemoryReportStore CreateStoreWithTestData()
        {
            var store = new InMemoryReportStore();
            
            // Create organization
            store.CreateOrganization(new CreateOrganizationRequest
            {
                Name = "Test Organization",
                LegalForm = "LLC",
                Country = "US",
                Identifier = "12345",
                CreatedBy = "user-2",
                CoverageType = "full",
                CoverageJustification = "Test coverage"
            });

            // Create organizational unit
            store.CreateOrganizationalUnit(new CreateOrganizationalUnitRequest
            {
                Name = "Test Organization Unit",
                Description = "Default unit for testing",
                CreatedBy = "user-2"
            });

            // Create reporting period owned by user-1
            var (_, _, snapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
            {
                Name = "FY 2024",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "user-1",
                OwnerName = "Sarah Chen"
            });

            return store;
        }

        #region Permission Check Tests

        [Fact]
        public void CheckExportPermission_WithAdminUser_ShouldAllowExport()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var periodId = store.GetPeriods().First().Id;

            // Act
            var (hasPermission, errorMessage) = store.CheckExportPermission("user-2", periodId); // user-2 is admin with CanExport=true

            // Assert
            Assert.True(hasPermission);
            Assert.Null(errorMessage);
        }

        [Fact]
        public void CheckExportPermission_WithReportOwnerRole_ShouldAllowExport()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var periodId = store.GetPeriods().First().Id;

            // Act
            var (hasPermission, errorMessage) = store.CheckExportPermission("user-1", periodId); // user-1 is report-owner with CanExport=true

            // Assert
            Assert.True(hasPermission);
            Assert.Null(errorMessage);
        }

        [Fact]
        public void CheckExportPermission_WithAuditorRole_ShouldAllowExport()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var periodId = store.GetPeriods().First().Id;

            // Act
            var (hasPermission, errorMessage) = store.CheckExportPermission("user-6", periodId); // user-6 is auditor with CanExport=true

            // Assert
            Assert.True(hasPermission);
            Assert.Null(errorMessage);
        }

        [Fact]
        public void CheckExportPermission_WithContributorRole_ShouldDenyExport()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var periodId = store.GetPeriods().First().Id;

            // Act
            var (hasPermission, errorMessage) = store.CheckExportPermission("user-3", periodId); // user-3 is contributor with CanExport=false

            // Assert
            Assert.False(hasPermission);
            Assert.NotNull(errorMessage);
            Assert.Contains("do not have permission to export", errorMessage);
        }

        [Fact]
        public void CheckExportPermission_WithPeriodOwner_ShouldAllowExport()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var periodId = store.GetPeriods().First().Id;

            // Act - user-1 is the period owner (even though they also have CanExport=true)
            var (hasPermission, errorMessage) = store.CheckExportPermission("user-1", periodId);

            // Assert
            Assert.True(hasPermission);
            Assert.Null(errorMessage);
        }

        [Fact]
        public void CheckExportPermission_WithPeriodOwnerButNoGlobalPermission_ShouldAllowExport()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            
            // Create a user with CanExport=false
            var user = store.GetUser("user-3"); // contributor with CanExport=false
            Assert.NotNull(user);
            Assert.False(user.CanExport);
            
            // Create a new period owned by this contributor
            var (isValid, errorMessage, snapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
            {
                Name = "FY 2025",
                StartDate = "2025-01-01",
                EndDate = "2025-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "user-3", // contributor owns this period
                OwnerName = "John Smith"
            });
            
            Assert.True(isValid);
            var newPeriod = snapshot?.Periods.Last();
            Assert.NotNull(newPeriod);
            
            // Act - user-3 should be able to export their own period despite CanExport=false
            var (hasPermission, permissionError) = store.CheckExportPermission("user-3", newPeriod.Id);

            // Assert
            Assert.True(hasPermission, "Period owner should be able to export their own period");
            Assert.Null(permissionError);
        }

        [Fact]
        public void CheckExportPermission_WithoutOwnershipOrGlobalPermission_ShouldDenyExport()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            
            // Create a new period owned by user-1
            var (isValid, errorMessage, snapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
            {
                Name = "FY 2026",
                StartDate = "2026-01-01",
                EndDate = "2026-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "user-1", // owned by user-1
                OwnerName = "Sarah Chen"
            });
            
            Assert.True(isValid);
            var newPeriod = snapshot?.Periods.Last();
            Assert.NotNull(newPeriod);
            
            // Act - user-3 (contributor) tries to export a period they don't own
            var (hasPermission, permissionError) = store.CheckExportPermission("user-3", newPeriod.Id);

            // Assert
            Assert.False(hasPermission, "Non-owner without global permission should be denied");
            Assert.NotNull(permissionError);
            Assert.Contains("do not have permission to export", permissionError);
        }

        [Fact]
        public void CheckExportPermission_WithInvalidUser_ShouldDenyExport()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var periodId = store.GetPeriods().First().Id;

            // Act
            var (hasPermission, errorMessage) = store.CheckExportPermission("invalid-user", periodId);

            // Assert
            Assert.False(hasPermission);
            Assert.NotNull(errorMessage);
            Assert.Equal("User not found.", errorMessage);
        }

        #endregion

        #region Export PDF Tests

        [Fact]
        public void ExportPdf_WithAuthorizedUser_ShouldSucceed()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var mockPdfService = new Mock<IPdfExportService>();
            var mockDocxService = new Mock<IDocxExportService>();
            var mockNotificationService = new Mock<INotificationService>();
            
            mockPdfService.Setup(x => x.GeneratePdf(It.IsAny<GeneratedReport>(), It.IsAny<PdfExportOptions>()))
                .Returns(new byte[] { 1, 2, 3 });
            mockPdfService.Setup(x => x.GenerateFilename(It.IsAny<GeneratedReport>(), It.IsAny<string?>()))
                .Returns("test-report.pdf");
            
            var controller = new ReportingController(store, mockNotificationService.Object, mockPdfService.Object, mockDocxService.Object);
            var periodId = store.GetPeriods().First().Id;

            // Act
            var request = new ExportPdfRequest { GeneratedBy = "user-2" }; // admin with CanExport=true
            var result = controller.ExportPdf(periodId, request);

            // Assert
            Assert.IsType<FileContentResult>(result);
            mockPdfService.Verify(x => x.GeneratePdf(It.IsAny<GeneratedReport>(), It.IsAny<PdfExportOptions>()), Times.Once);
        }

        [Fact]
        public void ExportPdf_WithUnauthorizedUser_ShouldReturn403()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var mockPdfService = new Mock<IPdfExportService>();
            var mockDocxService = new Mock<IDocxExportService>();
            var mockNotificationService = new Mock<INotificationService>();
            
            var controller = new ReportingController(store, mockNotificationService.Object, mockPdfService.Object, mockDocxService.Object);
            var periodId = store.GetPeriods().First().Id;

            // Act
            var request = new ExportPdfRequest { GeneratedBy = "user-3" }; // contributor with CanExport=false
            var result = controller.ExportPdf(periodId, request);

            // Assert
            Assert.IsType<ObjectResult>(result);
            var objectResult = result as ObjectResult;
            Assert.Equal(403, objectResult!.StatusCode);
            
            // Verify PDF service was never called
            mockPdfService.Verify(x => x.GeneratePdf(It.IsAny<GeneratedReport>(), It.IsAny<PdfExportOptions>()), Times.Never);
        }

        [Fact]
        public void ExportPdf_WithUnauthorizedUser_ShouldLogDeniedAttempt()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var mockPdfService = new Mock<IPdfExportService>();
            var mockDocxService = new Mock<IDocxExportService>();
            var mockNotificationService = new Mock<INotificationService>();
            
            var controller = new ReportingController(store, mockNotificationService.Object, mockPdfService.Object, mockDocxService.Object);
            var periodId = store.GetPeriods().First().Id;

            // Act
            var request = new ExportPdfRequest { GeneratedBy = "user-3" }; // contributor with CanExport=false
            controller.ExportPdf(periodId, request);

            // Assert - Check audit log for denied attempt
            var auditLog = store.GetAuditLog(entityType: "ReportExport", action: "export-denied");
            Assert.NotEmpty(auditLog);
            
            var deniedEntry = auditLog.First();
            Assert.Equal("user-3", deniedEntry.UserId);
            Assert.Equal("export-denied", deniedEntry.Action);
            Assert.Equal(periodId, deniedEntry.EntityId);
            Assert.Contains("denied", deniedEntry.ChangeNote, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ExportPdf_WithAuthorizedUser_ShouldLogSuccessfulAttempt()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var mockPdfService = new Mock<IPdfExportService>();
            var mockDocxService = new Mock<IDocxExportService>();
            var mockNotificationService = new Mock<INotificationService>();
            
            mockPdfService.Setup(x => x.GeneratePdf(It.IsAny<GeneratedReport>(), It.IsAny<PdfExportOptions>()))
                .Returns(new byte[] { 1, 2, 3 });
            mockPdfService.Setup(x => x.GenerateFilename(It.IsAny<GeneratedReport>(), It.IsAny<string?>()))
                .Returns("test-report.pdf");
            
            var controller = new ReportingController(store, mockNotificationService.Object, mockPdfService.Object, mockDocxService.Object);
            var periodId = store.GetPeriods().First().Id;

            // Act
            var request = new ExportPdfRequest { GeneratedBy = "user-2", VariantName = "Stakeholder" };
            controller.ExportPdf(periodId, request);

            // Assert - Check audit log for successful attempt
            var auditLog = store.GetAuditLog(entityType: "ReportExport", action: "export");
            Assert.NotEmpty(auditLog);
            
            var successEntry = auditLog.First();
            Assert.Equal("user-2", successEntry.UserId);
            Assert.Equal("export", successEntry.Action);
            Assert.Equal(periodId, successEntry.EntityId);
            Assert.Contains("Successfully exported", successEntry.ChangeNote);
            Assert.Contains("PDF", successEntry.ChangeNote);
            Assert.Contains("Stakeholder", successEntry.ChangeNote);
        }

        #endregion

        #region Export DOCX Tests

        [Fact]
        public void ExportDocx_WithAuthorizedUser_ShouldSucceed()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var mockPdfService = new Mock<IPdfExportService>();
            var mockDocxService = new Mock<IDocxExportService>();
            var mockNotificationService = new Mock<INotificationService>();
            
            mockDocxService.Setup(x => x.GenerateDocx(It.IsAny<GeneratedReport>(), It.IsAny<DocxExportOptions>()))
                .Returns(new byte[] { 1, 2, 3 });
            mockDocxService.Setup(x => x.GenerateFilename(It.IsAny<GeneratedReport>(), It.IsAny<string?>()))
                .Returns("test-report.docx");
            
            var controller = new ReportingController(store, mockNotificationService.Object, mockPdfService.Object, mockDocxService.Object);
            var periodId = store.GetPeriods().First().Id;

            // Act
            var request = new ExportDocxRequest { GeneratedBy = "user-2" }; // admin with CanExport=true
            var result = controller.ExportDocx(periodId, request);

            // Assert
            Assert.IsType<FileContentResult>(result);
            mockDocxService.Verify(x => x.GenerateDocx(It.IsAny<GeneratedReport>(), It.IsAny<DocxExportOptions>()), Times.Once);
        }

        [Fact]
        public void ExportDocx_WithUnauthorizedUser_ShouldReturn403()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var mockPdfService = new Mock<IPdfExportService>();
            var mockDocxService = new Mock<IDocxExportService>();
            var mockNotificationService = new Mock<INotificationService>();
            
            var controller = new ReportingController(store, mockNotificationService.Object, mockPdfService.Object, mockDocxService.Object);
            var periodId = store.GetPeriods().First().Id;

            // Act
            var request = new ExportDocxRequest { GeneratedBy = "user-3" }; // contributor with CanExport=false
            var result = controller.ExportDocx(periodId, request);

            // Assert
            Assert.IsType<ObjectResult>(result);
            var objectResult = result as ObjectResult;
            Assert.Equal(403, objectResult!.StatusCode);
            
            // Verify DOCX service was never called
            mockDocxService.Verify(x => x.GenerateDocx(It.IsAny<GeneratedReport>(), It.IsAny<DocxExportOptions>()), Times.Never);
        }

        [Fact]
        public void ExportDocx_WithUnauthorizedUser_ShouldLogDeniedAttempt()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var mockPdfService = new Mock<IPdfExportService>();
            var mockDocxService = new Mock<IDocxExportService>();
            var mockNotificationService = new Mock<INotificationService>();
            
            var controller = new ReportingController(store, mockNotificationService.Object, mockPdfService.Object, mockDocxService.Object);
            var periodId = store.GetPeriods().First().Id;

            // Act
            var request = new ExportDocxRequest { GeneratedBy = "user-4" }; // contributor with CanExport=false
            controller.ExportDocx(periodId, request);

            // Assert - Check audit log for denied attempt
            var auditLog = store.GetAuditLog(entityType: "ReportExport", action: "export-denied");
            Assert.NotEmpty(auditLog);
            
            var deniedEntry = auditLog.First();
            Assert.Equal("user-4", deniedEntry.UserId);
            Assert.Equal("export-denied", deniedEntry.Action);
            Assert.Equal(periodId, deniedEntry.EntityId);
            Assert.Contains("denied", deniedEntry.ChangeNote, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ExportDocx_WithAuthorizedUser_ShouldLogSuccessfulAttempt()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var mockPdfService = new Mock<IPdfExportService>();
            var mockDocxService = new Mock<IDocxExportService>();
            var mockNotificationService = new Mock<INotificationService>();
            
            mockDocxService.Setup(x => x.GenerateDocx(It.IsAny<GeneratedReport>(), It.IsAny<DocxExportOptions>()))
                .Returns(new byte[] { 1, 2, 3 });
            mockDocxService.Setup(x => x.GenerateFilename(It.IsAny<GeneratedReport>(), It.IsAny<string?>()))
                .Returns("test-report.docx");
            
            var controller = new ReportingController(store, mockNotificationService.Object, mockPdfService.Object, mockDocxService.Object);
            var periodId = store.GetPeriods().First().Id;

            // Act
            var request = new ExportDocxRequest { GeneratedBy = "user-1", VariantName = "Internal" };
            controller.ExportDocx(periodId, request);

            // Assert - Check audit log for successful attempt
            var auditLog = store.GetAuditLog(entityType: "ReportExport", action: "export");
            Assert.NotEmpty(auditLog);
            
            var successEntry = auditLog.First();
            Assert.Equal("user-1", successEntry.UserId);
            Assert.Equal("export", successEntry.Action);
            Assert.Equal(periodId, successEntry.EntityId);
            Assert.Contains("Successfully exported", successEntry.ChangeNote);
            Assert.Contains("DOCX", successEntry.ChangeNote);
            Assert.Contains("Internal", successEntry.ChangeNote);
        }

        #endregion

        #region Audit Trail Tests

        [Fact]
        public void RecordExportAttempt_ShouldIncludeAllRequiredFields()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var periodId = store.GetPeriods().First().Id;

            // Act
            store.RecordExportAttempt("user-2", "Admin User", periodId, "pdf", "Stakeholder", wasAllowed: true);

            // Assert
            var auditLog = store.GetAuditLog(entityType: "ReportExport", action: "export");
            Assert.NotEmpty(auditLog);
            
            var entry = auditLog.First();
            Assert.Equal("user-2", entry.UserId);
            Assert.Equal("Admin User", entry.UserName);
            Assert.Equal(periodId, entry.EntityId);
            Assert.Equal("ReportExport", entry.EntityType);
            Assert.NotEmpty(entry.Changes);
            
            // Check that format, variant name, and allowed status are recorded
            var formatChange = entry.Changes.FirstOrDefault(c => c.Field == "Format");
            Assert.NotNull(formatChange);
            Assert.Equal("pdf", formatChange.NewValue);
            
            var variantChange = entry.Changes.FirstOrDefault(c => c.Field == "VariantName");
            Assert.NotNull(variantChange);
            Assert.Equal("Stakeholder", variantChange.NewValue);
            
            var allowedChange = entry.Changes.FirstOrDefault(c => c.Field == "Allowed");
            Assert.NotNull(allowedChange);
            Assert.Equal("True", allowedChange.NewValue);
        }

        [Fact]
        public void GetAuditLog_FilterByReportExport_ShouldReturnOnlyExportAttempts()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var periodId = store.GetPeriods().First().Id;
            
            // Create multiple export attempts
            store.RecordExportAttempt("user-2", "Admin User", periodId, "pdf", null, wasAllowed: true);
            store.RecordExportAttempt("user-3", "John Smith", periodId, "pdf", null, wasAllowed: false, errorMessage: "No permission");
            store.RecordExportAttempt("user-1", "Sarah Chen", periodId, "docx", "Internal", wasAllowed: true);

            // Act
            var exportAttempts = store.GetAuditLog(entityType: "ReportExport");

            // Assert
            Assert.Equal(3, exportAttempts.Count);
            Assert.All(exportAttempts, e => Assert.Equal("ReportExport", e.EntityType));
        }

        #endregion
        
        #region Export Scope Tests
        
        [Fact]
        public void ExportPdf_WithFullReport_ShouldRecordFullScope()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var mockPdfService = new Mock<IPdfExportService>();
            var mockDocxService = new Mock<IDocxExportService>();
            var mockNotificationService = new Mock<INotificationService>();
            
            mockPdfService.Setup(x => x.GeneratePdf(It.IsAny<GeneratedReport>(), It.IsAny<PdfExportOptions>()))
                .Returns(new byte[] { 1, 2, 3 });
            mockPdfService.Setup(x => x.GenerateFilename(It.IsAny<GeneratedReport>(), It.IsAny<string?>()))
                .Returns("test-report.pdf");
            
            var controller = new ReportingController(store, mockNotificationService.Object, mockPdfService.Object, mockDocxService.Object);
            var periodId = store.GetPeriods().First().Id;

            // Act - Export without specifying section IDs (full report)
            var request = new ExportPdfRequest { GeneratedBy = "user-2" };
            controller.ExportPdf(periodId, request);

            // Assert - Check export history has full scope
            var history = store.GetExportHistory(periodId);
            Assert.NotEmpty(history);
            
            var exportEntry = history.First();
            Assert.Equal("full", exportEntry.ExportScope);
            Assert.NotEmpty(exportEntry.IncludedSectionIds);
            Assert.NotEmpty(exportEntry.IncludedSectionNames);
        }
        
        [Fact]
        public void ExportPdf_WithSelectedSections_ShouldRecordPartialScope()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var mockPdfService = new Mock<IPdfExportService>();
            var mockDocxService = new Mock<IDocxExportService>();
            var mockNotificationService = new Mock<INotificationService>();
            
            mockPdfService.Setup(x => x.GeneratePdf(It.IsAny<GeneratedReport>(), It.IsAny<PdfExportOptions>()))
                .Returns(new byte[] { 1, 2, 3 });
            mockPdfService.Setup(x => x.GenerateFilename(It.IsAny<GeneratedReport>(), It.IsAny<string?>()))
                .Returns("test-report.pdf");
            
            var controller = new ReportingController(store, mockNotificationService.Object, mockPdfService.Object, mockDocxService.Object);
            var periodId = store.GetPeriods().First().Id;
            
            // Get some section IDs
            var sections = store.GetSections(periodId);
            var selectedSectionIds = sections.Take(2).Select(s => s.Id).ToList();

            // Act - Export with specific section IDs
            var request = new ExportPdfRequest 
            { 
                GeneratedBy = "user-2",
                SectionIds = selectedSectionIds
            };
            controller.ExportPdf(periodId, request);

            // Assert - Check export history has partial scope
            var history = store.GetExportHistory(periodId);
            Assert.NotEmpty(history);
            
            var exportEntry = history.First();
            Assert.Equal("partial", exportEntry.ExportScope);
            Assert.NotEmpty(exportEntry.IncludedSectionIds);
            Assert.NotEmpty(exportEntry.IncludedSectionNames);
            // The actual sections in the generated report should match what was requested
            Assert.All(exportEntry.IncludedSectionIds, id => Assert.Contains(id, selectedSectionIds));
        }
        
        [Fact]
        public void ExportDocx_WithFullReport_ShouldRecordFullScope()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var mockPdfService = new Mock<IPdfExportService>();
            var mockDocxService = new Mock<IDocxExportService>();
            var mockNotificationService = new Mock<INotificationService>();
            
            mockDocxService.Setup(x => x.GenerateDocx(It.IsAny<GeneratedReport>(), It.IsAny<DocxExportOptions>()))
                .Returns(new byte[] { 1, 2, 3 });
            mockDocxService.Setup(x => x.GenerateFilename(It.IsAny<GeneratedReport>(), It.IsAny<string?>()))
                .Returns("test-report.docx");
            
            var controller = new ReportingController(store, mockNotificationService.Object, mockPdfService.Object, mockDocxService.Object);
            var periodId = store.GetPeriods().First().Id;

            // Act - Export without specifying section IDs (full report)
            var request = new ExportDocxRequest { GeneratedBy = "user-2" };
            controller.ExportDocx(periodId, request);

            // Assert - Check export history has full scope
            var history = store.GetExportHistory(periodId);
            Assert.NotEmpty(history);
            
            var exportEntry = history.First();
            Assert.Equal("full", exportEntry.ExportScope);
            Assert.NotEmpty(exportEntry.IncludedSectionIds);
            Assert.NotEmpty(exportEntry.IncludedSectionNames);
        }
        
        [Fact]
        public void GetExportHistory_ShouldIncludeScopeInformation()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var mockPdfService = new Mock<IPdfExportService>();
            var mockDocxService = new Mock<IDocxExportService>();
            var mockNotificationService = new Mock<INotificationService>();
            
            mockPdfService.Setup(x => x.GeneratePdf(It.IsAny<GeneratedReport>(), It.IsAny<PdfExportOptions>()))
                .Returns(new byte[] { 1, 2, 3 });
            mockPdfService.Setup(x => x.GenerateFilename(It.IsAny<GeneratedReport>(), It.IsAny<string?>()))
                .Returns("test-report.pdf");
            
            var controller = new ReportingController(store, mockNotificationService.Object, mockPdfService.Object, mockDocxService.Object);
            var periodId = store.GetPeriods().First().Id;

            // Act - Create multiple exports
            controller.ExportPdf(periodId, new ExportPdfRequest { GeneratedBy = "user-2" });
            controller.ExportPdf(periodId, new ExportPdfRequest { GeneratedBy = "user-1", VariantName = "Internal" });

            // Get export history as admin (user-2)
            var result = controller.GetExportHistory(periodId, "user-2");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var history = Assert.IsAssignableFrom<IReadOnlyList<ExportHistoryEntry>>(okResult.Value);
            
            Assert.Equal(2, history.Count);
            Assert.All(history, entry =>
            {
                Assert.NotNull(entry.ExportScope);
                Assert.NotNull(entry.IncludedSectionIds);
                Assert.NotNull(entry.IncludedSectionNames);
                Assert.NotEmpty(entry.ExportedBy);
                Assert.NotEmpty(entry.ExportedByName);
                Assert.NotEmpty(entry.ExportedAt);
            });
        }
        
        [Fact]
        public void GetExportHistory_WithAuthorizedUser_ShouldSucceed()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var mockPdfService = new Mock<IPdfExportService>();
            var mockDocxService = new Mock<IDocxExportService>();
            var mockNotificationService = new Mock<INotificationService>();
            
            mockPdfService.Setup(x => x.GeneratePdf(It.IsAny<GeneratedReport>(), It.IsAny<PdfExportOptions>()))
                .Returns(new byte[] { 1, 2, 3 });
            mockPdfService.Setup(x => x.GenerateFilename(It.IsAny<GeneratedReport>(), It.IsAny<string?>()))
                .Returns("test-report.pdf");
            
            var controller = new ReportingController(store, mockNotificationService.Object, mockPdfService.Object, mockDocxService.Object);
            var periodId = store.GetPeriods().First().Id;
            
            controller.ExportPdf(periodId, new ExportPdfRequest { GeneratedBy = "user-2" });

            // Act - Admin user (user-2) with CanExport=true views history
            var result = controller.GetExportHistory(periodId, "user-2");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var history = Assert.IsAssignableFrom<IReadOnlyList<ExportHistoryEntry>>(okResult.Value);
            Assert.NotEmpty(history);
        }
        
        [Fact]
        public void GetExportHistory_WithPeriodOwner_ShouldSucceed()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var mockPdfService = new Mock<IPdfExportService>();
            var mockDocxService = new Mock<IDocxExportService>();
            var mockNotificationService = new Mock<INotificationService>();
            
            mockPdfService.Setup(x => x.GeneratePdf(It.IsAny<GeneratedReport>(), It.IsAny<PdfExportOptions>()))
                .Returns(new byte[] { 1, 2, 3 });
            mockPdfService.Setup(x => x.GenerateFilename(It.IsAny<GeneratedReport>(), It.IsAny<string?>()))
                .Returns("test-report.pdf");
            
            var controller = new ReportingController(store, mockNotificationService.Object, mockPdfService.Object, mockDocxService.Object);
            var periodId = store.GetPeriods().First().Id;
            var period = store.GetPeriods().First();
            
            controller.ExportPdf(periodId, new ExportPdfRequest { GeneratedBy = "user-2" });

            // Act - Period owner (user-1) views their own period's history
            var result = controller.GetExportHistory(periodId, period.OwnerId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var history = Assert.IsAssignableFrom<IReadOnlyList<ExportHistoryEntry>>(okResult.Value);
            Assert.NotEmpty(history);
        }
        
        [Fact]
        public void GetExportHistory_WithUnauthorizedUser_ShouldReturn403()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var mockPdfService = new Mock<IPdfExportService>();
            var mockDocxService = new Mock<IDocxExportService>();
            var mockNotificationService = new Mock<INotificationService>();
            
            var controller = new ReportingController(store, mockNotificationService.Object, mockPdfService.Object, mockDocxService.Object);
            var periodId = store.GetPeriods().First().Id;

            // Act - Contributor (user-3) with CanExport=false tries to view history
            var result = controller.GetExportHistory(periodId, "user-3");

            // Assert
            Assert.IsType<ObjectResult>(result.Result);
            var objectResult = result.Result as ObjectResult;
            Assert.Equal(403, objectResult!.StatusCode);
        }
        
        [Fact]
        public void GetExportHistory_WithoutUserId_ShouldReturn400()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var mockPdfService = new Mock<IPdfExportService>();
            var mockDocxService = new Mock<IDocxExportService>();
            var mockNotificationService = new Mock<INotificationService>();
            
            var controller = new ReportingController(store, mockNotificationService.Object, mockPdfService.Object, mockDocxService.Object);
            var periodId = store.GetPeriods().First().Id;

            // Act - Request without userId parameter
            var result = controller.GetExportHistory(periodId, null);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }
        
        [Fact]
        public void GetExportHistory_WithNonExistentUser_ShouldReturn404()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var mockPdfService = new Mock<IPdfExportService>();
            var mockDocxService = new Mock<IDocxExportService>();
            var mockNotificationService = new Mock<INotificationService>();
            
            var controller = new ReportingController(store, mockNotificationService.Object, mockPdfService.Object, mockDocxService.Object);
            var periodId = store.GetPeriods().First().Id;

            // Act - Request with non-existent userId
            var result = controller.GetExportHistory(periodId, "non-existent-user");

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        #endregion
    }
}
