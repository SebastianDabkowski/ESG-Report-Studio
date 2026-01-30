using ARP.ESG_ReportStudio.API.Reporting;
using ARP.ESG_ReportStudio.API.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace SD.ProjectName.Tests.Products
{
    public class RetentionControllerTests
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

            return store;
        }

        [Fact]
        public void CreateRetentionPolicy_WithAdminUser_ShouldSucceed()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var controller = new RetentionController(store);
            var request = new CreateRetentionPolicyRequest
            {
                DataCategory = "audit-log",
                RetentionDays = 365,
                AllowDeletion = true,
                CreatedBy = "user-2"
            };

            // Act
            var result = controller.CreateRetentionPolicy("user-2", request);

            // Assert
            Assert.IsType<CreatedResult>(result.Result);
            var createdResult = result.Result as CreatedResult;
            var policy = createdResult!.Value as RetentionPolicy;
            Assert.NotNull(policy);
            Assert.Equal(365, policy.RetentionDays);
        }

        [Fact]
        public void CreateRetentionPolicy_WithNonAdminUser_ShouldDenyAccess()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var controller = new RetentionController(store);
            var request = new CreateRetentionPolicyRequest
            {
                DataCategory = "audit-log",
                RetentionDays = 365,
                CreatedBy = "user-1"
            };

            // Act
            var result = controller.CreateRetentionPolicy("user-1", request); // user-1 is report-owner

            // Assert
            Assert.IsType<ObjectResult>(result.Result);
            var objectResult = result.Result as ObjectResult;
            Assert.Equal(403, objectResult!.StatusCode);
        }

        [Fact]
        public void GetRetentionPolicies_WithAdminUser_ShouldSucceed()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var controller = new RetentionController(store);
            
            // Create a policy first
            store.CreateRetentionPolicy(new CreateRetentionPolicyRequest
            {
                DataCategory = "audit-log",
                RetentionDays = 365,
                CreatedBy = "user-2"
            });

            // Act
            var result = controller.GetRetentionPolicies("user-2");

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
            var okResult = result.Result as OkObjectResult;
            var policies = okResult!.Value as IReadOnlyList<RetentionPolicy>;
            Assert.NotEmpty(policies!);
        }

        [Fact]
        public void GetRetentionPolicies_WithAuditorUser_ShouldSucceed()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var controller = new RetentionController(store);
            
            store.CreateRetentionPolicy(new CreateRetentionPolicyRequest
            {
                DataCategory = "audit-log",
                RetentionDays = 365,
                CreatedBy = "user-2"
            });

            // Act
            var result = controller.GetRetentionPolicies("user-6"); // user-6 is auditor

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public void GetRetentionPolicies_WithContributorUser_ShouldDenyAccess()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var controller = new RetentionController(store);

            // Act
            var result = controller.GetRetentionPolicies("user-3"); // user-3 is contributor

            // Assert
            Assert.IsType<ObjectResult>(result.Result);
            var objectResult = result.Result as ObjectResult;
            Assert.Equal(403, objectResult!.StatusCode);
        }

        [Fact]
        public void UpdateRetentionPolicy_WithAdminUser_ShouldSucceed()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var controller = new RetentionController(store);
            
            var (_, _, policy) = store.CreateRetentionPolicy(new CreateRetentionPolicyRequest
            {
                DataCategory = "audit-log",
                RetentionDays = 365,
                CreatedBy = "user-2"
            });

            var updateRequest = new UpdateRetentionPolicyRequest
            {
                RetentionDays = 730,
                AllowDeletion = false
            };

            // Act
            var result = controller.UpdateRetentionPolicy("user-2", policy!.Id, updateRequest);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public void UpdateRetentionPolicy_WithNonAdminUser_ShouldDenyAccess()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var controller = new RetentionController(store);
            
            var (_, _, policy) = store.CreateRetentionPolicy(new CreateRetentionPolicyRequest
            {
                DataCategory = "audit-log",
                RetentionDays = 365,
                CreatedBy = "user-2"
            });

            var updateRequest = new UpdateRetentionPolicyRequest
            {
                RetentionDays = 730,
                AllowDeletion = false
            };

            // Act
            var result = controller.UpdateRetentionPolicy("user-1", policy!.Id, updateRequest); // user-1 is report-owner

            // Assert
            Assert.IsType<ObjectResult>(result);
            var objectResult = result as ObjectResult;
            Assert.Equal(403, objectResult!.StatusCode);
        }

        [Fact]
        public void DeactivateRetentionPolicy_WithAdminUser_ShouldSucceed()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var controller = new RetentionController(store);
            
            var (_, _, policy) = store.CreateRetentionPolicy(new CreateRetentionPolicyRequest
            {
                DataCategory = "audit-log",
                RetentionDays = 365,
                CreatedBy = "user-2"
            });

            // Act
            var result = controller.DeactivateRetentionPolicy("user-2", policy!.Id);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public void DeactivateRetentionPolicy_WithNonAdminUser_ShouldDenyAccess()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var controller = new RetentionController(store);
            
            var (_, _, policy) = store.CreateRetentionPolicy(new CreateRetentionPolicyRequest
            {
                DataCategory = "audit-log",
                RetentionDays = 365,
                CreatedBy = "user-2"
            });

            // Act
            var result = controller.DeactivateRetentionPolicy("user-3", policy!.Id); // user-3 is contributor

            // Assert
            Assert.IsType<ObjectResult>(result);
            var objectResult = result as ObjectResult;
            Assert.Equal(403, objectResult!.StatusCode);
        }

        [Fact]
        public void RunCleanup_WithAdminUser_ShouldSucceed()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var controller = new RetentionController(store);
            
            store.CreateRetentionPolicy(new CreateRetentionPolicyRequest
            {
                DataCategory = "audit-log",
                RetentionDays = 365,
                CreatedBy = "user-2"
            });

            var request = new RunCleanupRequest
            {
                DryRun = true,
                InitiatedBy = "user-2"
            };

            // Act
            var result = controller.RunCleanup("user-2", request);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public void RunCleanup_WithNonAdminUser_ShouldDenyAccess()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var controller = new RetentionController(store);
            
            var request = new RunCleanupRequest
            {
                DryRun = true,
                InitiatedBy = "user-1"
            };

            // Act
            var result = controller.RunCleanup("user-1", request); // user-1 is report-owner

            // Assert
            Assert.IsType<ObjectResult>(result.Result);
            var objectResult = result.Result as ObjectResult;
            Assert.Equal(403, objectResult!.StatusCode);
        }

        [Fact]
        public void GetDeletionReports_WithAdminUser_ShouldSucceed()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var controller = new RetentionController(store);

            // Act
            var result = controller.GetDeletionReports("user-2");

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public void GetDeletionReports_WithAuditorUser_ShouldSucceed()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var controller = new RetentionController(store);

            // Act
            var result = controller.GetDeletionReports("user-6"); // user-6 is auditor

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public void GetDeletionReports_WithContributorUser_ShouldDenyAccess()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var controller = new RetentionController(store);

            // Act
            var result = controller.GetDeletionReports("user-3"); // user-3 is contributor

            // Assert
            Assert.IsType<ObjectResult>(result.Result);
            var objectResult = result.Result as ObjectResult;
            Assert.Equal(403, objectResult!.StatusCode);
        }

        [Fact]
        public void GetApplicableRetentionPolicy_WithAdminUser_ShouldSucceed()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var controller = new RetentionController(store);
            
            store.CreateRetentionPolicy(new CreateRetentionPolicyRequest
            {
                DataCategory = "audit-log",
                RetentionDays = 365,
                CreatedBy = "user-2"
            });

            // Act
            var result = controller.GetApplicableRetentionPolicy("user-2", "audit-log");

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
            var okResult = result.Result as OkObjectResult;
            var policy = okResult!.Value as RetentionPolicy;
            Assert.NotNull(policy);
            Assert.Equal("audit-log", policy.DataCategory);
        }

        [Fact]
        public void GetApplicableRetentionPolicy_WithNoMatchingPolicy_ShouldReturnNotFound()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var controller = new RetentionController(store);

            // Act
            var result = controller.GetApplicableRetentionPolicy("user-2", "non-existent");

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }
    }
}
