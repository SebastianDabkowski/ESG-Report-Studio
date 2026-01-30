using ARP.ESG_ReportStudio.API.Reporting;
using ARP.ESG_ReportStudio.API.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace SD.ProjectName.Tests.Products
{
    public class AuditLogAccessControlTests
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

            // Create reporting period
            store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
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

        [Fact]
        public void GetAuditLog_WithAdminUser_ShouldAllowAccess()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var controller = new AuditLogController(store);

            // Act
            var result = controller.GetAuditLog("user-2"); // user-2 is admin

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public void GetAuditLog_WithAuditorUser_ShouldAllowAccess()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var controller = new AuditLogController(store);

            // Act
            var result = controller.GetAuditLog("user-6"); // user-6 is auditor

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public void GetAuditLog_WithReportOwnerUser_ShouldAllowAccess()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var controller = new AuditLogController(store);

            // Act
            var result = controller.GetAuditLog("user-1"); // user-1 is report-owner

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public void GetAuditLog_WithContributorUser_ShouldAllowLimitedAccess()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var controller = new AuditLogController(store);

            // Act
            var result = controller.GetAuditLog("user-3"); // user-3 is contributor

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
            
            var okResult = result.Result as OkObjectResult;
            var entries = okResult!.Value as IReadOnlyList<AuditLogEntry>;
            
            // Contributors should only see their own actions
            Assert.All(entries!, e => Assert.Equal("user-3", e.UserId));
        }

        [Fact]
        public void GetAuditLog_WithInvalidUser_ShouldDenyAccess()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var controller = new AuditLogController(store);

            // Act
            var result = controller.GetAuditLog("invalid-user");

            // Assert
            Assert.IsType<ObjectResult>(result.Result);
            var objectResult = result.Result as ObjectResult;
            Assert.Equal(403, objectResult!.StatusCode);
        }

        [Fact]
        public void ExportAuditLogCsv_WithAdminUser_ShouldAllowAccess()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var controller = new AuditLogController(store);

            // Act
            var result = controller.ExportAuditLogCsv("user-2"); // user-2 is admin

            // Assert
            Assert.IsType<FileContentResult>(result);
        }

        [Fact]
        public void ExportAuditLogCsv_WithAuditorUser_ShouldAllowAccess()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var controller = new AuditLogController(store);

            // Act
            var result = controller.ExportAuditLogCsv("user-6"); // user-6 is auditor

            // Assert
            Assert.IsType<FileContentResult>(result);
        }

        [Fact]
        public void ExportAuditLogCsv_WithReportOwnerUser_ShouldDenyAccess()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var controller = new AuditLogController(store);

            // Act
            var result = controller.ExportAuditLogCsv("user-1"); // user-1 is report-owner

            // Assert
            Assert.IsType<ObjectResult>(result);
            var objectResult = result as ObjectResult;
            Assert.Equal(403, objectResult!.StatusCode);
        }

        [Fact]
        public void ExportAuditLogCsv_WithContributorUser_ShouldDenyAccess()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var controller = new AuditLogController(store);

            // Act
            var result = controller.ExportAuditLogCsv("user-3"); // user-3 is contributor

            // Assert
            Assert.IsType<ObjectResult>(result);
            var objectResult = result as ObjectResult;
            Assert.Equal(403, objectResult!.StatusCode);
        }

        [Fact]
        public void ExportAuditLogJson_WithAdminUser_ShouldAllowAccess()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var controller = new AuditLogController(store);

            // Act
            var result = controller.ExportAuditLogJson("user-2"); // user-2 is admin

            // Assert
            Assert.IsType<FileContentResult>(result);
        }

        [Fact]
        public void ExportAuditLogJson_WithAuditorUser_ShouldAllowAccess()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var controller = new AuditLogController(store);

            // Act
            var result = controller.ExportAuditLogJson("user-6"); // user-6 is auditor

            // Assert
            Assert.IsType<FileContentResult>(result);
        }

        [Fact]
        public void ExportAuditLogJson_WithReportOwnerUser_ShouldDenyAccess()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var controller = new AuditLogController(store);

            // Act
            var result = controller.ExportAuditLogJson("user-1"); // user-1 is report-owner

            // Assert
            Assert.IsType<ObjectResult>(result);
            var objectResult = result as ObjectResult;
            Assert.Equal(403, objectResult!.StatusCode);
        }

        [Fact]
        public void ExportAuditLogJson_WithContributorUser_ShouldDenyAccess()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var controller = new AuditLogController(store);

            // Act
            var result = controller.ExportAuditLogJson("user-3"); // user-3 is contributor

            // Assert
            Assert.IsType<ObjectResult>(result);
            var objectResult = result as ObjectResult;
            Assert.Equal(403, objectResult!.StatusCode);
        }
    }
}
