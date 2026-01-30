using ARP.ESG_ReportStudio.API.Reporting;
using Xunit;

namespace SD.ProjectName.Tests.Products
{
    public class RolloverTests
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

        private static string CreateTestPeriod(InMemoryReportStore store, string name, string status = "active")
        {
            var request = new CreateReportingPeriodRequest
            {
                Name = name,
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "user1",
                OwnerName = "Test User",
                OrganizationId = "org1"
            };

            var (isValid, errorMessage, snapshot) = store.ValidateAndCreatePeriod(request);
            Assert.True(isValid, errorMessage);
            Assert.NotNull(snapshot);
            
            var period = snapshot.Periods.First(p => p.Name == name);
            
            // Update status if needed using reflection
            if (status != "active")
            {
                period.Status = status;
            }
            
            return period.Id;
        }

        [Fact]
        public void Rollover_ShouldBlockDraftPeriods()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestOrganization(store);
            CreateTestOrganizationalUnit(store);
            
            var sourcePeriodId = CreateTestPeriod(store, "FY 2024", "draft");

            var rolloverRequest = new RolloverRequest
            {
                SourcePeriodId = sourcePeriodId,
                TargetPeriodName = "FY 2025",
                TargetPeriodStartDate = "2025-01-01",
                TargetPeriodEndDate = "2025-12-31",
                Options = new RolloverOptions
                {
                    CopyStructure = true,
                    CopyDisclosures = false,
                    CopyDataValues = false,
                    CopyAttachments = false
                },
                PerformedBy = "user1"
            };

            // Act
            var (success, errorMessage, result) = store.RolloverPeriod(rolloverRequest);

            // Assert
            Assert.False(success);
            Assert.Contains("draft", errorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Rollover_StructureOnly_ShouldCopySections()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestOrganization(store);
            CreateTestOrganizationalUnit(store);
            
            var sourcePeriodId = CreateTestPeriod(store, "FY 2024");

            var rolloverRequest = new RolloverRequest
            {
                SourcePeriodId = sourcePeriodId,
                TargetPeriodName = "FY 2025",
                TargetPeriodStartDate = "2025-01-01",
                TargetPeriodEndDate = "2025-12-31",
                Options = new RolloverOptions
                {
                    CopyStructure = true,
                    CopyDisclosures = false,
                    CopyDataValues = false,
                    CopyAttachments = false
                },
                PerformedBy = "user1"
            };

            // Act
            var (success, errorMessage, result) = store.RolloverPeriod(rolloverRequest);

            // Assert
            Assert.True(success, errorMessage);
            Assert.NotNull(result);
            Assert.NotNull(result.TargetPeriod);
            Assert.NotNull(result.AuditLog);
            Assert.Equal("FY 2025", result.TargetPeriod.Name);
            Assert.True(result.AuditLog.SectionsCopied > 0);
            Assert.Equal(0, result.AuditLog.DataPointsCopied);
            Assert.Equal(0, result.AuditLog.GapsCopied);
            Assert.Equal(0, result.AuditLog.AssumptionsCopied);
        }

        [Fact]
        public void Rollover_WithDisclosures_ShouldCopyGapsAndAssumptions()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestOrganization(store);
            CreateTestOrganizationalUnit(store);
            
            var sourcePeriodId = CreateTestPeriod(store, "FY 2024");
            
            // Add a gap and assumption to source period using reflection
            var sections = store.GetSections(sourcePeriodId);
            var sectionId = sections.First().Id;
            
            var gap = new Gap
            {
                Id = Guid.NewGuid().ToString(),
                SectionId = sectionId,
                Title = "Test Gap",
                Description = "Test gap description",
                Impact = "high",
                CreatedBy = "user1",
                CreatedAt = DateTime.UtcNow.ToString("O"),
                Resolved = false
            };
            
            var assumption = new Assumption
            {
                Id = Guid.NewGuid().ToString(),
                SectionId = sectionId,
                Title = "Test Assumption",
                Description = "Test assumption description",
                Scope = "Company-wide",
                Methodology = "Statistical",
                ValidityStartDate = "2024-01-01",
                ValidityEndDate = "2025-12-31",
                Limitations = "Test limitations",
                Status = "active",
                CreatedBy = "user1",
                CreatedAt = DateTime.UtcNow.ToString("O")
            };
            
            // Use reflection to add gap and assumption
            var gapsField = typeof(InMemoryReportStore).GetField("_gaps", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var gaps = (List<Gap>)gapsField!.GetValue(store)!;
            gaps.Add(gap);
            
            var assumptionsField = typeof(InMemoryReportStore).GetField("_assumptions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var assumptions = (List<Assumption>)assumptionsField!.GetValue(store)!;
            assumptions.Add(assumption);

            var rolloverRequest = new RolloverRequest
            {
                SourcePeriodId = sourcePeriodId,
                TargetPeriodName = "FY 2025",
                TargetPeriodStartDate = "2025-01-01",
                TargetPeriodEndDate = "2025-12-31",
                Options = new RolloverOptions
                {
                    CopyStructure = true,
                    CopyDisclosures = true,
                    CopyDataValues = false,
                    CopyAttachments = false
                },
                PerformedBy = "user1"
            };

            // Act
            var (success, errorMessage, result) = store.RolloverPeriod(rolloverRequest);

            // Assert
            Assert.True(success, errorMessage);
            Assert.NotNull(result);
            Assert.NotNull(result.AuditLog);
            Assert.Equal(1, result.AuditLog.GapsCopied);
            Assert.Equal(1, result.AuditLog.AssumptionsCopied);
        }

        [Fact]
        public void Rollover_ShouldValidateOptionsDependencies()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestOrganization(store);
            CreateTestOrganizationalUnit(store);
            
            var sourcePeriodId = CreateTestPeriod(store, "FY 2024");

            var rolloverRequest = new RolloverRequest
            {
                SourcePeriodId = sourcePeriodId,
                TargetPeriodName = "FY 2025",
                TargetPeriodStartDate = "2025-01-01",
                TargetPeriodEndDate = "2025-12-31",
                Options = new RolloverOptions
                {
                    CopyStructure = false,
                    CopyDisclosures = true,  // This should fail
                    CopyDataValues = false,
                    CopyAttachments = false
                },
                PerformedBy = "user1"
            };

            // Act
            var (success, errorMessage, result) = store.RolloverPeriod(rolloverRequest);

            // Assert
            Assert.False(success);
            Assert.Contains("CopyDisclosures requires CopyStructure", errorMessage);
        }

        [Fact]
        public void Rollover_ShouldCreateAuditLog()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestOrganization(store);
            CreateTestOrganizationalUnit(store);
            
            var sourcePeriodId = CreateTestPeriod(store, "FY 2024");

            var rolloverRequest = new RolloverRequest
            {
                SourcePeriodId = sourcePeriodId,
                TargetPeriodName = "FY 2025",
                TargetPeriodStartDate = "2025-01-01",
                TargetPeriodEndDate = "2025-12-31",
                Options = new RolloverOptions
                {
                    CopyStructure = true,
                    CopyDisclosures = false,
                    CopyDataValues = false,
                    CopyAttachments = false
                },
                PerformedBy = "user1"
            };

            // Act
            var (success, errorMessage, result) = store.RolloverPeriod(rolloverRequest);

            // Assert
            Assert.True(success, errorMessage);
            Assert.NotNull(result);
            Assert.NotNull(result.AuditLog);
            Assert.Equal("user1", result.AuditLog.PerformedBy);
            Assert.Equal("FY 2024", result.AuditLog.SourcePeriodName);
            Assert.Equal("FY 2025", result.AuditLog.TargetPeriodName);
            
            // Verify audit log can be retrieved
            var logs = store.GetRolloverAuditLogs(result.TargetPeriod!.Id);
            Assert.Single(logs);
            Assert.Equal(result.AuditLog.Id, logs.First().Id);
        }

        [Fact]
        public void Rollover_WithExpiredAssumption_ShouldFlagForReview()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestOrganization(store);
            CreateTestOrganizationalUnit(store);
            
            var sourcePeriodId = CreateTestPeriod(store, "FY 2024");
            
            // Add an expired assumption to source period using reflection
            var sections = store.GetSections(sourcePeriodId);
            var sectionId = sections.First().Id;
            
            var expiredAssumption = new Assumption
            {
                Id = Guid.NewGuid().ToString(),
                SectionId = sectionId,
                Title = "Expired Assumption",
                Description = "This assumption will expire",
                Scope = "Company-wide",
                Methodology = "Statistical",
                ValidityStartDate = "2024-01-01",
                ValidityEndDate = "2024-06-30",  // Expires before target period starts
                Limitations = "Test limitations",
                Status = "active",
                CreatedBy = "user1",
                CreatedAt = DateTime.UtcNow.ToString("O")
            };
            
            var assumptionsField = typeof(InMemoryReportStore).GetField("_assumptions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var assumptions = (List<Assumption>)assumptionsField!.GetValue(store)!;
            assumptions.Add(expiredAssumption);

            var rolloverRequest = new RolloverRequest
            {
                SourcePeriodId = sourcePeriodId,
                TargetPeriodName = "FY 2025",
                TargetPeriodStartDate = "2025-01-01",
                TargetPeriodEndDate = "2025-12-31",
                Options = new RolloverOptions
                {
                    CopyStructure = true,
                    CopyDisclosures = true,
                    CopyDataValues = false,
                    CopyAttachments = false
                },
                PerformedBy = "user1"
            };

            // Act
            var (success, errorMessage, result) = store.RolloverPeriod(rolloverRequest);

            // Assert
            Assert.True(success, errorMessage);
            Assert.NotNull(result);
            Assert.Equal(1, result.AuditLog!.AssumptionsCopied);
            
            // Verify the assumption is flagged
            var targetSections = store.GetSections(result.TargetPeriod!.Id);
            var targetSectionId = targetSections.First().Id;
            
            // Get assumptions using reflection
            var targetAssumptions = assumptions.Where(a => a.SectionId == targetSectionId).ToList();
            
            Assert.Single(targetAssumptions);
            var copiedAssumption = targetAssumptions.First();
            Assert.Contains("EXPIRED", copiedAssumption.Limitations);
            Assert.Contains("WARNING", copiedAssumption.Description);
        }
    }
}
