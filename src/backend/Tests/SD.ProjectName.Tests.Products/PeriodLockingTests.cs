using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products
{
    public class PeriodLockingTests
    {
        private static void CreateTestOrganization(InMemoryReportStore store)
        {
            store.CreateOrganization(new CreateOrganizationRequest
            {
                Name = "Test Organization",
                LegalForm = "LLC",
                Country = "US",
                Identifier = "12345",
                CreatedBy = "test-user",
                CoverageType = "full",
                CoverageJustification = "Test coverage"
            });
        }

        private static void CreateTestOrganizationalUnit(InMemoryReportStore store)
        {
            store.CreateOrganizationalUnit(new CreateOrganizationalUnitRequest
            {
                Name = "Test Organization Unit",
                Description = "Default unit for testing",
                CreatedBy = "test-user"
            });
        }

        private static void CreateTestConfiguration(InMemoryReportStore store)
        {
            CreateTestOrganization(store);
            CreateTestOrganizationalUnit(store);
        }

        private static string CreateTestPeriod(InMemoryReportStore store, string name = "FY 2024")
        {
            CreateTestConfiguration(store);
            
            var request = new CreateReportingPeriodRequest
            {
                Name = name,
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "user-2",  // Use pre-existing admin user
                OwnerName = "Admin User"
            };
            
            var (isValid, _, snapshot) = store.ValidateAndCreatePeriod(request);
            Assert.True(isValid);
            Assert.NotNull(snapshot);
            
            return snapshot.Periods[0].Id;
        }

        [Fact]
        public void LockPeriod_WithValidRequest_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var periodId = CreateTestPeriod(store);
            
            var lockRequest = new LockPeriodRequest
            {
                UserId = "user-2",
                UserName = "Admin User",
                Reason = "Period is complete and ready for audit"
            };

            // Act
            var (isSuccess, errorMessage, period) = store.LockPeriod(periodId, lockRequest);

            // Assert
            Assert.True(isSuccess);
            Assert.Null(errorMessage);
            Assert.NotNull(period);
            Assert.True(period.IsLocked);
            Assert.NotNull(period.LockedAt);
            Assert.Equal("user-2", period.LockedBy);
            Assert.Equal("Admin User", period.LockedByName);
        }

        [Fact]
        public void LockPeriod_NonexistentPeriod_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var lockRequest = new LockPeriodRequest
            {
                UserId = "user-2",
                UserName = "Admin User",
                Reason = "Testing"
            };

            // Act
            var (isSuccess, errorMessage, period) = store.LockPeriod("nonexistent-id", lockRequest);

            // Assert
            Assert.False(isSuccess);
            Assert.NotNull(errorMessage);
            Assert.Contains("not found", errorMessage);
            Assert.Null(period);
        }

        [Fact]
        public void LockPeriod_AlreadyLocked_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var periodId = CreateTestPeriod(store);
            
            var lockRequest = new LockPeriodRequest
            {
                UserId = "user-2",
                UserName = "Admin User",
                Reason = "First lock"
            };
            store.LockPeriod(periodId, lockRequest);

            // Act - Try to lock again
            var (isSuccess, errorMessage, period) = store.LockPeriod(periodId, lockRequest);

            // Assert
            Assert.False(isSuccess);
            Assert.NotNull(errorMessage);
            Assert.Contains("already locked", errorMessage);
            Assert.Null(period);
        }

        [Fact]
        public void LockPeriod_CreatesAuditLogEntry()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var periodId = CreateTestPeriod(store);
            
            var lockRequest = new LockPeriodRequest
            {
                UserId = "user-2",
                UserName = "Admin User",
                Reason = "Data collection complete"
            };

            // Act
            store.LockPeriod(periodId, lockRequest);
            var auditTrail = store.GetPeriodAuditTrail(periodId);

            // Assert
            Assert.NotEmpty(auditTrail);
            var lockEntry = auditTrail.First();
            Assert.Equal("lock", lockEntry.Action);
            Assert.Equal("ReportingPeriod", lockEntry.EntityType);
            Assert.Equal(periodId, lockEntry.EntityId);
            Assert.Equal("user-2", lockEntry.UserId);
            Assert.Equal("Admin User", lockEntry.UserName);
            Assert.Equal("Data collection complete", lockEntry.ChangeNote);
            Assert.Contains(lockEntry.Changes, c => c.Field == "IsLocked" && c.NewValue == "true");
        }

        [Fact]
        public void UnlockPeriod_ByAdmin_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var periodId = CreateTestPeriod(store);
            
            // Lock the period first
            var lockRequest = new LockPeriodRequest
            {
                UserId = "user-2",
                UserName = "Admin User",
                Reason = "Locking for audit"
            };
            store.LockPeriod(periodId, lockRequest);
            
            // Act - Unlock
            var unlockRequest = new UnlockPeriodRequest
            {
                UserId = "user-2",
                UserName = "Admin User",
                Reason = "Need to make corrections"
            };
            var (isSuccess, errorMessage, period) = store.UnlockPeriod(periodId, unlockRequest, isAdmin: true);

            // Assert
            Assert.True(isSuccess);
            Assert.Null(errorMessage);
            Assert.NotNull(period);
            Assert.False(period.IsLocked);
            Assert.Null(period.LockedAt);
            Assert.Null(period.LockedBy);
            Assert.Null(period.LockedByName);
        }

        [Fact]
        public void UnlockPeriod_ByNonAdmin_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var periodId = CreateTestPeriod(store);
            
            // Lock the period
            var lockRequest = new LockPeriodRequest
            {
                UserId = "user-2",
                UserName = "Admin User",
                Reason = "Locking for audit"
            };
            store.LockPeriod(periodId, lockRequest);
            
            // Act - Try to unlock as non-admin
            var unlockRequest = new UnlockPeriodRequest
            {
                UserId = "user-3",
                UserName = "John Smith",
                Reason = "Need to make changes"
            };
            var (isSuccess, errorMessage, period) = store.UnlockPeriod(periodId, unlockRequest, isAdmin: false);

            // Assert
            Assert.False(isSuccess);
            Assert.NotNull(errorMessage);
            Assert.Contains("Only administrators", errorMessage);
            Assert.Null(period);
        }

        [Fact]
        public void UnlockPeriod_WithoutReason_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var periodId = CreateTestPeriod(store);
            
            // Lock the period
            var lockRequest = new LockPeriodRequest
            {
                UserId = "user-2",
                UserName = "Admin User",
                Reason = "Locking for audit"
            };
            store.LockPeriod(periodId, lockRequest);
            
            // Act - Try to unlock without reason
            var unlockRequest = new UnlockPeriodRequest
            {
                UserId = "user-2",
                UserName = "Admin User",
                Reason = ""
            };
            var (isSuccess, errorMessage, period) = store.UnlockPeriod(periodId, unlockRequest, isAdmin: true);

            // Assert
            Assert.False(isSuccess);
            Assert.NotNull(errorMessage);
            Assert.Contains("reason is required", errorMessage);
            Assert.Null(period);
        }

        [Fact]
        public void UnlockPeriod_NotLocked_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var periodId = CreateTestPeriod(store);
            
            // Act - Try to unlock a period that's not locked
            var unlockRequest = new UnlockPeriodRequest
            {
                UserId = "user-2",
                UserName = "Admin User",
                Reason = "Testing unlock"
            };
            var (isSuccess, errorMessage, period) = store.UnlockPeriod(periodId, unlockRequest, isAdmin: true);

            // Assert
            Assert.False(isSuccess);
            Assert.NotNull(errorMessage);
            Assert.Contains("not locked", errorMessage);
            Assert.Null(period);
        }

        [Fact]
        public void UnlockPeriod_CreatesAuditLogEntry()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var periodId = CreateTestPeriod(store);
            
            // Lock the period
            var lockRequest = new LockPeriodRequest
            {
                UserId = "user-2",
                UserName = "Admin User",
                Reason = "Locking for audit"
            };
            store.LockPeriod(periodId, lockRequest);
            
            // Act - Unlock
            var unlockRequest = new UnlockPeriodRequest
            {
                UserId = "user-2",
                UserName = "Admin User",
                Reason = "Correction needed after audit feedback"
            };
            store.UnlockPeriod(periodId, unlockRequest, isAdmin: true);
            var auditTrail = store.GetPeriodAuditTrail(periodId);

            // Assert
            Assert.Equal(2, auditTrail.Count); // Lock + Unlock
            var unlockEntry = auditTrail.Last();
            Assert.Equal("unlock", unlockEntry.Action);
            Assert.Equal("ReportingPeriod", unlockEntry.EntityType);
            Assert.Equal(periodId, unlockEntry.EntityId);
            Assert.Equal("user-2", unlockEntry.UserId);
            Assert.Equal("Admin User", unlockEntry.UserName);
            Assert.Equal("Correction needed after audit feedback", unlockEntry.ChangeNote);
            Assert.Contains(unlockEntry.Changes, c => c.Field == "IsLocked" && c.NewValue == "false");
        }

        [Fact]
        public void UpdateDataPoint_WhenPeriodLocked_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var periodId = CreateTestPeriod(store);
            
            // Get the first section
            var sections = store.GetSections(periodId);
            var section = sections.First();
            
            // Create a data point
            var createRequest = new CreateDataPointRequest
            {
                SectionId = section.Id,
                Type = "metric",
                Title = "Test Metric",
                Content = "Test value",
                OwnerId = "user-3",
                Source = "Test Source",
                InformationType = "fact",
                CompletenessStatus = "complete"
            };
            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);
            
            // Lock the period
            var lockRequest = new LockPeriodRequest
            {
                UserId = "user-2",
                UserName = "Admin User",
                Reason = "Period locked for audit"
            };
            store.LockPeriod(periodId, lockRequest);
            
            // Act - Try to update the data point
            var updateRequest = new UpdateDataPointRequest
            {
                Type = "metric",
                Title = "Updated Metric",
                Content = "Updated value",
                Classification = "",
                OwnerId = "user-3",
                Source = "",
                InformationType = "",
                Assumptions = "",
                CompletenessStatus = "complete",
                UpdatedBy = "user-3"
            };
            var (isValid, errorMessage, _) = store.UpdateDataPoint(dataPoint.Id, updateRequest);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("locked", errorMessage);
        }

        [Fact]
        public void UpdateSectionOwner_WhenPeriodLocked_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var periodId = CreateTestPeriod(store);
            
            // Get the first section
            var sections = store.GetSections(periodId);
            var section = sections.First();
            
            // Lock the period
            var lockRequest = new LockPeriodRequest
            {
                UserId = "user-2",
                UserName = "Admin User",
                Reason = "Period locked for audit"
            };
            store.LockPeriod(periodId, lockRequest);
            
            // Act - Try to update section owner
            var updateRequest = new UpdateSectionOwnerRequest
            {
                OwnerId = "user-3",
                UpdatedBy = "user-2",
                ChangeNote = "Changing owner"
            };
            var (isValid, errorMessage, _) = store.UpdateSectionOwner(section.Id, updateRequest);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("locked", errorMessage);
        }
    }
}
