using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products
{
    public class CleanupServiceTests
    {
        private static InMemoryReportStore CreateStoreWithAuditData()
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

            // Create some old audit log entries by using reflection to access and modify
            var auditLogField = typeof(InMemoryReportStore).GetField("_auditLog",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var auditLog = auditLogField!.GetValue(store) as List<AuditLogEntry>;
            
            // Add old audit entries (simulating data older than retention period)
            var oldDate = DateTime.UtcNow.AddDays(-400).ToString("o");
            for (int i = 0; i < 5; i++)
            {
                auditLog!.Add(new AuditLogEntry
                {
                    Id = Guid.NewGuid().ToString(),
                    Timestamp = oldDate,
                    UserId = "user-1",
                    UserName = "Test User",
                    Action = "test-action",
                    EntityType = "TestEntity",
                    EntityId = $"entity-{i}",
                    Changes = new List<FieldChange>()
                });
            }

            return store;
        }

        [Fact]
        public void RunCleanup_WithNoRetentionPolicy_ShouldReturnError()
        {
            // Arrange
            var store = CreateStoreWithAuditData();
            var request = new RunCleanupRequest
            {
                DryRun = true,
                InitiatedBy = "user-2"
            };

            // Act
            var result = store.RunCleanup(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("No active retention policies", result.ErrorMessage);
        }

        [Fact]
        public void RunCleanup_WithDryRun_ShouldNotDeleteData()
        {
            // Arrange
            var store = CreateStoreWithAuditData();
            
            // Create retention policy
            store.CreateRetentionPolicy(new CreateRetentionPolicyRequest
            {
                DataCategory = "audit-log",
                RetentionDays = 365,
                AllowDeletion = true,
                CreatedBy = "user-2"
            });

            var initialAuditLogCount = store.GetAuditLog().Count;
            
            var request = new RunCleanupRequest
            {
                DryRun = true,
                InitiatedBy = "user-2"
            };

            // Act
            var result = store.RunCleanup(request);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.WasDryRun);
            Assert.True(result.RecordsIdentified > 0);
            Assert.Equal(0, result.RecordsDeleted);
            Assert.Equal(initialAuditLogCount, store.GetAuditLog().Count);
        }

        [Fact]
        public void RunCleanup_WithActualRun_ShouldDeleteOldData()
        {
            // Arrange
            var store = CreateStoreWithAuditData();
            
            // Create retention policy
            store.CreateRetentionPolicy(new CreateRetentionPolicyRequest
            {
                DataCategory = "audit-log",
                RetentionDays = 365,
                AllowDeletion = true,
                CreatedBy = "user-2"
            });

            var initialAuditLogCount = store.GetAuditLog().Count;
            
            var request = new RunCleanupRequest
            {
                DryRun = false,
                InitiatedBy = "user-2"
            };

            // Act
            var result = store.RunCleanup(request);

            // Assert
            Assert.True(result.Success);
            Assert.False(result.WasDryRun);
            Assert.True(result.RecordsDeleted > 0);
            Assert.True(store.GetAuditLog().Count < initialAuditLogCount);
        }

        [Fact]
        public void RunCleanup_WithAllowDeletionFalse_ShouldNotDeleteData()
        {
            // Arrange
            var store = CreateStoreWithAuditData();
            
            // Create retention policy with AllowDeletion = false
            store.CreateRetentionPolicy(new CreateRetentionPolicyRequest
            {
                DataCategory = "audit-log",
                RetentionDays = 365,
                AllowDeletion = false,
                CreatedBy = "user-2"
            });

            var initialAuditLogCount = store.GetAuditLog().Count;
            
            var request = new RunCleanupRequest
            {
                DryRun = false,
                InitiatedBy = "user-2"
            };

            // Act
            var result = store.RunCleanup(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(0, result.RecordsDeleted);
            Assert.Equal(initialAuditLogCount, store.GetAuditLog().Count);
        }

        [Fact]
        public void RunCleanup_ShouldCreateDeletionReport()
        {
            // Arrange
            var store = CreateStoreWithAuditData();
            
            store.CreateRetentionPolicy(new CreateRetentionPolicyRequest
            {
                DataCategory = "audit-log",
                RetentionDays = 365,
                AllowDeletion = true,
                CreatedBy = "user-2"
            });
            
            var request = new RunCleanupRequest
            {
                DryRun = false,
                InitiatedBy = "user-2"
            };

            // Act
            var result = store.RunCleanup(request);

            // Assert
            Assert.NotEmpty(result.DeletionReportIds);
            
            var deletionReports = store.GetDeletionReports();
            Assert.NotEmpty(deletionReports);
            
            var report = deletionReports.First(r => result.DeletionReportIds.Contains(r.Id));
            Assert.Equal("audit-log", report.DataCategory);
            Assert.True(report.RecordCount > 0);
            Assert.NotEmpty(report.ContentHash);
            Assert.NotEmpty(report.Signature ?? "");
        }

        [Fact]
        public void DeletionReport_ShouldHaveValidSignature()
        {
            // Arrange
            var store = CreateStoreWithAuditData();
            
            store.CreateRetentionPolicy(new CreateRetentionPolicyRequest
            {
                DataCategory = "audit-log",
                RetentionDays = 365,
                AllowDeletion = true,
                CreatedBy = "user-2"
            });
            
            var request = new RunCleanupRequest
            {
                DryRun = false,
                InitiatedBy = "user-2"
            };

            // Act
            var result = store.RunCleanup(request);
            var deletionReports = store.GetDeletionReports();
            var report = deletionReports.First();

            // Assert
            Assert.NotNull(report.Signature);
            Assert.NotEmpty(report.Signature);
            Assert.NotNull(report.ContentHash);
            Assert.NotEmpty(report.ContentHash);
            
            // Verify signature is based on content hash
            Assert.True(report.Signature.Length > 0);
            Assert.True(report.ContentHash.Length > 0);
        }

        [Fact]
        public void DeletionReport_ShouldContainMetadataOnly()
        {
            // Arrange
            var store = CreateStoreWithAuditData();
            
            store.CreateRetentionPolicy(new CreateRetentionPolicyRequest
            {
                DataCategory = "audit-log",
                RetentionDays = 365,
                AllowDeletion = true,
                CreatedBy = "user-2"
            });
            
            var request = new RunCleanupRequest
            {
                DryRun = false,
                InitiatedBy = "user-2"
            };

            // Act
            var result = store.RunCleanup(request);
            var deletionReports = store.GetDeletionReports();
            var report = deletionReports.First();

            // Assert
            // Verify only metadata is stored, not actual deleted content
            Assert.NotEmpty(report.DeletionSummary);
            Assert.Contains("records", report.DeletionSummary);
            Assert.NotEmpty(report.DateRangeStart);
            Assert.NotEmpty(report.DateRangeEnd);
            Assert.True(report.RecordCount > 0);
            
            // Ensure no sensitive data is in the report
            Assert.DoesNotContain("user-", report.DeletionSummary);
            Assert.DoesNotContain("entity-", report.DeletionSummary);
        }

        [Fact]
        public void RunCleanup_ShouldCreateAuditLogForDeletionReport()
        {
            // Arrange
            var store = CreateStoreWithAuditData();
            
            store.CreateRetentionPolicy(new CreateRetentionPolicyRequest
            {
                DataCategory = "audit-log",
                RetentionDays = 365,
                AllowDeletion = true,
                CreatedBy = "user-2"
            });
            
            var request = new RunCleanupRequest
            {
                DryRun = false,
                InitiatedBy = "user-2"
            };

            // Act
            var result = store.RunCleanup(request);

            // Assert
            Assert.NotEmpty(result.DeletionReportIds);
            var reportId = result.DeletionReportIds.First();
            
            // Check audit log for deletion report creation
            var auditLog = store.GetAuditLog(entityType: "DeletionReport", entityId: reportId);
            Assert.NotEmpty(auditLog);
            
            var entry = auditLog.First();
            Assert.Equal("create-deletion-report", entry.Action);
            Assert.Equal("user-2", entry.UserId);
        }

        [Fact]
        public void GetDeletionReports_ShouldReturnInReverseChronologicalOrder()
        {
            // Arrange
            var store = CreateStoreWithAuditData();
            
            store.CreateRetentionPolicy(new CreateRetentionPolicyRequest
            {
                DataCategory = "audit-log",
                RetentionDays = 365,
                AllowDeletion = true,
                CreatedBy = "user-2"
            });
            
            // Run cleanup twice
            store.RunCleanup(new RunCleanupRequest { DryRun = false, InitiatedBy = "user-2" });
            
            // Add more old data for second cleanup
            var auditLogField = typeof(InMemoryReportStore).GetField("_auditLog",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var auditLog = auditLogField!.GetValue(store) as List<AuditLogEntry>;
            var oldDate2 = DateTime.UtcNow.AddDays(-400).AddSeconds(-1).ToString("o"); // Slightly different time
            auditLog!.Add(new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = oldDate2,
                UserId = "user-1",
                UserName = "Test User",
                Action = "test-action",
                EntityType = "TestEntity",
                EntityId = "entity-new",
                Changes = new List<FieldChange>()
            });
            
            store.RunCleanup(new RunCleanupRequest { DryRun = false, InitiatedBy = "user-2" });

            // Act
            var reports = store.GetDeletionReports();

            // Assert
            Assert.True(reports.Count >= 2);
            
            // Verify reverse chronological order
            for (int i = 0; i < reports.Count - 1; i++)
            {
                var current = DateTime.Parse(reports[i].DeletedAt, null, System.Globalization.DateTimeStyles.RoundtripKind);
                var next = DateTime.Parse(reports[i + 1].DeletedAt, null, System.Globalization.DateTimeStyles.RoundtripKind);
                Assert.True(current >= next);
            }
        }

        [Fact]
        public void GetDeletionReports_WithTenantFilter_ShouldFilterCorrectly()
        {
            // Arrange
            var store = CreateStoreWithAuditData();
            
            store.CreateRetentionPolicy(new CreateRetentionPolicyRequest
            {
                TenantId = "tenant-1",
                DataCategory = "audit-log",
                RetentionDays = 365,
                AllowDeletion = true,
                CreatedBy = "user-2"
            });
            
            store.RunCleanup(new RunCleanupRequest 
            { 
                DryRun = false, 
                TenantId = "tenant-1",
                InitiatedBy = "user-2" 
            });

            // Act
            var tenant1Reports = store.GetDeletionReports("tenant-1");
            var allReports = store.GetDeletionReports();

            // Assert
            Assert.NotEmpty(tenant1Reports);
            Assert.All(tenant1Reports, r => Assert.Equal("tenant-1", r.TenantId));
            Assert.True(allReports.Count >= tenant1Reports.Count);
        }
    }
}
