using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products
{
    public class RetentionPolicyTests
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
        public void CreateRetentionPolicy_WithValidData_ShouldSucceed()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var request = new CreateRetentionPolicyRequest
            {
                TenantId = null,
                ReportType = null,
                DataCategory = "audit-log",
                RetentionDays = 365,
                AllowDeletion = true,
                CreatedBy = "user-2"
            };

            // Act
            var (success, errorMessage, policy) = store.CreateRetentionPolicy(request);

            // Assert
            Assert.True(success);
            Assert.Null(errorMessage);
            Assert.NotNull(policy);
            Assert.Equal(365, policy.RetentionDays);
            Assert.Equal("audit-log", policy.DataCategory);
            Assert.True(policy.IsActive);
            Assert.True(policy.AllowDeletion);
        }

        [Fact]
        public void CreateRetentionPolicy_WithInvalidRetentionDays_ShouldFail()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var request = new CreateRetentionPolicyRequest
            {
                RetentionDays = 0,
                CreatedBy = "user-2"
            };

            // Act
            var (success, errorMessage, policy) = store.CreateRetentionPolicy(request);

            // Assert
            Assert.False(success);
            Assert.NotNull(errorMessage);
            Assert.Contains("at least 1", errorMessage);
            Assert.Null(policy);
        }

        [Fact]
        public void CreateRetentionPolicy_WithTenantId_ShouldHaveHigherPriority()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            
            var defaultRequest = new CreateRetentionPolicyRequest
            {
                TenantId = null,
                DataCategory = "audit-log",
                RetentionDays = 365,
                CreatedBy = "user-2"
            };
            
            var tenantRequest = new CreateRetentionPolicyRequest
            {
                TenantId = "tenant-1",
                DataCategory = "audit-log",
                RetentionDays = 730,
                CreatedBy = "user-2"
            };

            // Act
            var (_, _, defaultPolicy) = store.CreateRetentionPolicy(defaultRequest);
            var (_, _, tenantPolicy) = store.CreateRetentionPolicy(tenantRequest);

            // Assert
            Assert.NotNull(defaultPolicy);
            Assert.NotNull(tenantPolicy);
            Assert.True(tenantPolicy.Priority > defaultPolicy.Priority);
        }

        [Fact]
        public void GetApplicableRetentionPolicy_WithMultiplePolicies_ShouldReturnHighestPriority()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            
            // Create default policy
            store.CreateRetentionPolicy(new CreateRetentionPolicyRequest
            {
                DataCategory = "all",
                RetentionDays = 365,
                CreatedBy = "user-2"
            });
            
            // Create category-specific policy
            store.CreateRetentionPolicy(new CreateRetentionPolicyRequest
            {
                DataCategory = "audit-log",
                RetentionDays = 730,
                CreatedBy = "user-2"
            });
            
            // Create tenant-specific policy
            store.CreateRetentionPolicy(new CreateRetentionPolicyRequest
            {
                TenantId = "tenant-1",
                DataCategory = "audit-log",
                RetentionDays = 1095,
                CreatedBy = "user-2"
            });

            // Act
            var policy = store.GetApplicableRetentionPolicy("audit-log", "tenant-1");

            // Assert
            Assert.NotNull(policy);
            Assert.Equal(1095, policy.RetentionDays); // Should get the most specific policy
            Assert.Equal("tenant-1", policy.TenantId);
        }

        [Fact]
        public void GetRetentionPolicies_WithActiveOnly_ShouldFilterInactive()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            
            var (_, _, policy1) = store.CreateRetentionPolicy(new CreateRetentionPolicyRequest
            {
                DataCategory = "audit-log",
                RetentionDays = 365,
                CreatedBy = "user-2"
            });
            
            store.CreateRetentionPolicy(new CreateRetentionPolicyRequest
            {
                DataCategory = "evidence",
                RetentionDays = 730,
                CreatedBy = "user-2"
            });
            
            // Deactivate first policy
            store.DeactivateRetentionPolicy(policy1!.Id, "user-2");

            // Act
            var activePolicies = store.GetRetentionPolicies(activeOnly: true);
            var allPolicies = store.GetRetentionPolicies(activeOnly: false);

            // Assert
            Assert.Single(activePolicies);
            Assert.Equal(2, allPolicies.Count);
        }

        [Fact]
        public void UpdateRetentionPolicy_WithValidData_ShouldSucceed()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var (_, _, policy) = store.CreateRetentionPolicy(new CreateRetentionPolicyRequest
            {
                DataCategory = "audit-log",
                RetentionDays = 365,
                AllowDeletion = true,
                CreatedBy = "user-2"
            });

            // Act
            var (success, errorMessage) = store.UpdateRetentionPolicy(
                policy!.Id, 
                730, 
                false, 
                "user-2");

            // Assert
            Assert.True(success);
            Assert.Null(errorMessage);
            
            var policies = store.GetRetentionPolicies();
            var updatedPolicy = policies.First(p => p.Id == policy.Id);
            Assert.Equal(730, updatedPolicy.RetentionDays);
            Assert.False(updatedPolicy.AllowDeletion);
        }

        [Fact]
        public void UpdateRetentionPolicy_WithInvalidId_ShouldFail()
        {
            // Arrange
            var store = CreateStoreWithTestData();

            // Act
            var (success, errorMessage) = store.UpdateRetentionPolicy(
                "invalid-id", 
                365, 
                true, 
                "user-2");

            // Assert
            Assert.False(success);
            Assert.NotNull(errorMessage);
            Assert.Contains("not found", errorMessage);
        }

        [Fact]
        public void DeactivateRetentionPolicy_WithValidId_ShouldSucceed()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var (_, _, policy) = store.CreateRetentionPolicy(new CreateRetentionPolicyRequest
            {
                DataCategory = "audit-log",
                RetentionDays = 365,
                CreatedBy = "user-2"
            });

            // Act
            var (success, errorMessage) = store.DeactivateRetentionPolicy(policy!.Id, "user-2");

            // Assert
            Assert.True(success);
            Assert.Null(errorMessage);
            
            var policies = store.GetRetentionPolicies(activeOnly: false);
            var deactivatedPolicy = policies.First(p => p.Id == policy.Id);
            Assert.False(deactivatedPolicy.IsActive);
        }

        [Fact]
        public void CreateRetentionPolicy_ShouldCreateAuditLogEntry()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var request = new CreateRetentionPolicyRequest
            {
                DataCategory = "audit-log",
                RetentionDays = 365,
                CreatedBy = "user-2"
            };

            // Act
            var (_, _, policy) = store.CreateRetentionPolicy(request);
            var auditLog = store.GetAuditLog(entityType: "RetentionPolicy", entityId: policy!.Id);

            // Assert
            Assert.NotEmpty(auditLog);
            var entry = auditLog.First();
            Assert.Equal("create-retention-policy", entry.Action);
            Assert.Equal("user-2", entry.UserId);
            Assert.Contains(entry.Changes, c => c.Field == "RetentionDays" && c.NewValue == "365");
        }

        [Fact]
        public void UpdateRetentionPolicy_ShouldCreateAuditLogEntry()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var (_, _, policy) = store.CreateRetentionPolicy(new CreateRetentionPolicyRequest
            {
                DataCategory = "audit-log",
                RetentionDays = 365,
                AllowDeletion = true,
                CreatedBy = "user-2"
            });

            // Act
            store.UpdateRetentionPolicy(policy!.Id, 730, false, "user-2");
            var auditLog = store.GetAuditLog(entityType: "RetentionPolicy", entityId: policy.Id);

            // Assert
            Assert.True(auditLog.Count >= 2); // Create + Update
            var updateEntry = auditLog.First(e => e.Action == "update-retention-policy");
            Assert.Equal("user-2", updateEntry.UserId);
            Assert.Contains(updateEntry.Changes, c => c.Field == "RetentionDays" && c.OldValue == "365" && c.NewValue == "730");
        }

        [Fact]
        public void DeactivateRetentionPolicy_ShouldCreateAuditLogEntry()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var (_, _, policy) = store.CreateRetentionPolicy(new CreateRetentionPolicyRequest
            {
                DataCategory = "audit-log",
                RetentionDays = 365,
                CreatedBy = "user-2"
            });

            // Act
            store.DeactivateRetentionPolicy(policy!.Id, "user-2");
            var auditLog = store.GetAuditLog(entityType: "RetentionPolicy", entityId: policy.Id);

            // Assert
            Assert.True(auditLog.Count >= 2); // Create + Deactivate
            var deactivateEntry = auditLog.First(e => e.Action == "deactivate-retention-policy");
            Assert.Equal("user-2", deactivateEntry.UserId);
            Assert.Contains(deactivateEntry.Changes, c => c.Field == "IsActive" && c.NewValue == "False");
        }
    }
}
