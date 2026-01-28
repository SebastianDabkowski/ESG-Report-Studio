using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products
{
    public class CarryForwardTests
    {
        private static void CreateTestConfiguration(InMemoryReportStore store)
        {
            // Create organization
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

            // Create organizational unit
            store.CreateOrganizationalUnit(new CreateOrganizationalUnitRequest
            {
                Name = "Test Organization Unit",
                Description = "Default unit for testing",
                CreatedBy = "test-user"
            });
        }

        [Fact]
        public void CarryForward_ShouldCopyOpenGaps()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);

            // Create period 1
            var period1Request = new CreateReportingPeriodRequest
            {
                Name = "FY 2024",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                OwnerId = "user1",
                OwnerName = "Test User"
            };
            var (isValid1, _, snapshot1) = store.ValidateAndCreatePeriod(period1Request);
            Assert.True(isValid1);
            var period1 = snapshot1!.Periods[0];
            var section1 = snapshot1.Sections.First();

            // Add an open gap
            var gap = new Gap
            {
                Id = Guid.NewGuid().ToString(),
                SectionId = section1.Id,
                Title = "Missing Data",
                Description = "No supplier data available",
                Impact = "high",
                CreatedBy = "user1",
                CreatedAt = DateTime.UtcNow.ToString("O"),
                Resolved = false
            };
            // Using reflection to add gap since there's no public method
            var gapsField = typeof(InMemoryReportStore).GetField("_gaps", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var gaps = (List<Gap>)gapsField!.GetValue(store)!;
            gaps.Add(gap);

            // Act - Create period 2 with carry-forward
            var period2Request = new CreateReportingPeriodRequest
            {
                Name = "FY 2025",
                StartDate = "2025-01-01",
                EndDate = "2025-12-31",
                ReportingMode = "simplified",
                OwnerId = "user1",
                OwnerName = "Test User",
                CopyOwnershipFromPeriodId = period1.Id,
                CarryForwardGapsAndAssumptions = true
            };
            var (isValid2, _, snapshot2) = store.ValidateAndCreatePeriod(period2Request);

            // Assert
            Assert.True(isValid2);
            var period2Sections = snapshot2!.Sections.Where(s => s.PeriodId == snapshot2.Periods.Last().Id).Select(s => s.Id).ToList();
            var gapsInPeriod2 = gaps.Where(g => period2Sections.Contains(g.SectionId)).ToList();
            Assert.Single(gapsInPeriod2);
            Assert.Contains("Carried forward from previous period", gapsInPeriod2[0].Description);
            Assert.False(gapsInPeriod2[0].Resolved);
        }

        [Fact]
        public void CarryForward_ShouldNotCopyResolvedGaps()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);

            // Create period 1
            var period1Request = new CreateReportingPeriodRequest
            {
                Name = "FY 2024",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                OwnerId = "user1",
                OwnerName = "Test User"
            };
            var (isValid1, _, snapshot1) = store.ValidateAndCreatePeriod(period1Request);
            Assert.True(isValid1);
            var period1 = snapshot1!.Periods[0];
            var section1 = snapshot1.Sections.First();

            // Add a resolved gap
            var gapsField = typeof(InMemoryReportStore).GetField("_gaps", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var gaps = (List<Gap>)gapsField!.GetValue(store)!;
            gaps.Add(new Gap
            {
                Id = Guid.NewGuid().ToString(),
                SectionId = section1.Id,
                Title = "Resolved Gap",
                Description = "This was fixed",
                Impact = "low",
                CreatedBy = "user1",
                CreatedAt = DateTime.UtcNow.ToString("O"),
                Resolved = true
            });

            // Act - Create period 2 with carry-forward
            var period2Request = new CreateReportingPeriodRequest
            {
                Name = "FY 2025",
                StartDate = "2025-01-01",
                EndDate = "2025-12-31",
                ReportingMode = "simplified",
                OwnerId = "user1",
                OwnerName = "Test User",
                CopyOwnershipFromPeriodId = period1.Id,
                CarryForwardGapsAndAssumptions = true
            };
            var (isValid2, _, snapshot2) = store.ValidateAndCreatePeriod(period2Request);

            // Assert
            Assert.True(isValid2);
            var period2Sections = snapshot2!.Sections.Where(s => s.PeriodId == snapshot2.Periods.Last().Id).Select(s => s.Id).ToList();
            var gapsInPeriod2 = gaps.Where(g => period2Sections.Contains(g.SectionId)).ToList();
            Assert.Empty(gapsInPeriod2); // Resolved gap should not be carried forward
        }

        [Fact]
        public void CarryForward_ShouldCopyActiveAssumptions()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);

            // Create period 1
            var period1Request = new CreateReportingPeriodRequest
            {
                Name = "FY 2024",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                OwnerId = "user1",
                OwnerName = "Test User"
            };
            var (isValid1, _, snapshot1) = store.ValidateAndCreatePeriod(period1Request);
            Assert.True(isValid1);
            var period1 = snapshot1!.Periods[0];
            var section1 = snapshot1.Sections.First();

            // Add an active assumption
            var (isValidAssumption, _, assumption) = store.CreateAssumption(
                section1.Id,
                "Energy Consumption Estimate",
                "Using industry average",
                "Company-wide",
                "2024-01-01",
                "2025-12-31",
                "Industry benchmark",
                "Limited actual data",
                new List<string>(),
                null,
                new List<AssumptionSource>(),
                "user1"
            );
            Assert.True(isValidAssumption);

            // Act - Create period 2 with carry-forward
            var period2Request = new CreateReportingPeriodRequest
            {
                Name = "FY 2025",
                StartDate = "2025-01-01",
                EndDate = "2025-12-31",
                ReportingMode = "simplified",
                OwnerId = "user1",
                OwnerName = "Test User",
                CopyOwnershipFromPeriodId = period1.Id,
                CarryForwardGapsAndAssumptions = true
            };
            var (isValid2, _, snapshot2) = store.ValidateAndCreatePeriod(period2Request);

            // Assert
            Assert.True(isValid2);
            var period2Sections = snapshot2!.Sections.Where(s => s.PeriodId == snapshot2.Periods.Last().Id).Select(s => s.Id).ToList();
            var assumptionsInPeriod2 = store.GetAssumptions()
                .Where(a => period2Sections.Contains(a.SectionId))
                .ToList();
            Assert.Single(assumptionsInPeriod2);
            Assert.Contains("Carried forward from previous period", assumptionsInPeriod2[0].Description);
            Assert.Equal("active", assumptionsInPeriod2[0].Status);
        }

        [Fact]
        public void CarryForward_ShouldFlagExpiredAssumptions()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);

            // Create period 1
            var period1Request = new CreateReportingPeriodRequest
            {
                Name = "FY 2024",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                OwnerId = "user1",
                OwnerName = "Test User"
            };
            var (isValid1, _, snapshot1) = store.ValidateAndCreatePeriod(period1Request);
            Assert.True(isValid1);
            var period1 = snapshot1!.Periods[0];
            var section1 = snapshot1.Sections.First();

            // Add an expired assumption
            var (isValidAssumption1, _, assumption) = store.CreateAssumption(
                section1.Id,
                "Old Assumption",
                "This expired",
                "Company-wide",
                "2023-01-01",
                "2024-06-30", // Expires before period 2 starts
                "Old method",
                "Old limitation",
                new List<string>(),
                null,
                new List<AssumptionSource>(),
                "user1"
            );
            Assert.True(isValidAssumption1);

            // Act - Create period 2 with carry-forward
            var period2Request = new CreateReportingPeriodRequest
            {
                Name = "FY 2025",
                StartDate = "2025-01-01",
                EndDate = "2025-12-31",
                ReportingMode = "simplified",
                OwnerId = "user1",
                OwnerName = "Test User",
                CopyOwnershipFromPeriodId = period1.Id,
                CarryForwardGapsAndAssumptions = true
            };
            var (isValid2, _, snapshot2) = store.ValidateAndCreatePeriod(period2Request);

            // Assert
            Assert.True(isValid2);
            var period2Sections = snapshot2!.Sections.Where(s => s.PeriodId == snapshot2.Periods.Last().Id).Select(s => s.Id).ToList();
            var assumptionsInPeriod2 = store.GetAssumptions()
                .Where(a => period2Sections.Contains(a.SectionId))
                .ToList();
            Assert.Single(assumptionsInPeriod2);
            Assert.Contains("⚠️ WARNING: This assumption expired", assumptionsInPeriod2[0].Description);
            Assert.Contains("[EXPIRED - Requires Review]", assumptionsInPeriod2[0].Limitations);
        }

        [Fact]
        public void CarryForward_ShouldNotCopyDeprecatedAssumptions()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);

            // Create period 1
            var period1Request = new CreateReportingPeriodRequest
            {
                Name = "FY 2024",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                OwnerId = "user1",
                OwnerName = "Test User"
            };
            var (isValid1, _, snapshot1) = store.ValidateAndCreatePeriod(period1Request);
            Assert.True(isValid1);
            var period1 = snapshot1!.Periods[0];
            var section1 = snapshot1.Sections.First();

            // Add a deprecated assumption
            var (isValidAssumption2, _, assumption2) = store.CreateAssumption(
                section1.Id,
                "Old Assumption",
                "This is deprecated",
                "Company-wide",
                "2024-01-01",
                "2025-12-31",
                "Old method",
                "Old limitation",
                new List<string>(),
                null,
                new List<AssumptionSource>(),
                "user1"
            );
            Assert.True(isValidAssumption2);
            store.DeprecateAssumption(assumption2!.Id, null, "No longer valid", "user1");

            // Act - Create period 2 with carry-forward
            var period2Request = new CreateReportingPeriodRequest
            {
                Name = "FY 2025",
                StartDate = "2025-01-01",
                EndDate = "2025-12-31",
                ReportingMode = "simplified",
                OwnerId = "user1",
                OwnerName = "Test User",
                CopyOwnershipFromPeriodId = period1.Id,
                CarryForwardGapsAndAssumptions = true
            };
            var (isValid2, _, snapshot2) = store.ValidateAndCreatePeriod(period2Request);

            // Assert
            Assert.True(isValid2);
            var period2Sections = snapshot2!.Sections.Where(s => s.PeriodId == snapshot2.Periods.Last().Id).Select(s => s.Id).ToList();
            var assumptionsInPeriod2 = store.GetAssumptions()
                .Where(a => period2Sections.Contains(a.SectionId))
                .ToList();
            Assert.Empty(assumptionsInPeriod2); // Deprecated assumption should not be carried forward
        }

        [Fact]
        public void CarryForward_WithoutFlag_ShouldNotCopyItems()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);

            // Create period 1
            var period1Request = new CreateReportingPeriodRequest
            {
                Name = "FY 2024",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                OwnerId = "user1",
                OwnerName = "Test User"
            };
            var (isValid1, _, snapshot1) = store.ValidateAndCreatePeriod(period1Request);
            Assert.True(isValid1);
            var period1 = snapshot1!.Periods[0];
            var section1 = snapshot1.Sections.First();

            // Add gap and assumption
            var gapsField = typeof(InMemoryReportStore).GetField("_gaps", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var gaps = (List<Gap>)gapsField!.GetValue(store)!;
            gaps.Add(new Gap
            {
                Id = Guid.NewGuid().ToString(),
                SectionId = section1.Id,
                Title = "Missing Data",
                Description = "No data",
                Impact = "high",
                CreatedBy = "user1",
                CreatedAt = DateTime.UtcNow.ToString("O"),
                Resolved = false
            });

            store.CreateAssumption(
                section1.Id,
                "Assumption",
                "Test",
                "Company-wide",
                "2024-01-01",
                "2025-12-31",
                "Method",
                "Limit",
                new List<string>(),
                null,
                new List<AssumptionSource>(),
                "user1"
            );

            // Act - Create period 2 WITHOUT carry-forward flag
            var period2Request = new CreateReportingPeriodRequest
            {
                Name = "FY 2025",
                StartDate = "2025-01-01",
                EndDate = "2025-12-31",
                ReportingMode = "simplified",
                OwnerId = "user1",
                OwnerName = "Test User",
                CopyOwnershipFromPeriodId = period1.Id,
                CarryForwardGapsAndAssumptions = false // Explicitly false
            };
            var (isValid2, _, snapshot2) = store.ValidateAndCreatePeriod(period2Request);

            // Assert
            Assert.True(isValid2);
            var period2Sections = snapshot2!.Sections.Where(s => s.PeriodId == snapshot2.Periods.Last().Id).Select(s => s.Id).ToList();
            var gapsInPeriod2 = gaps.Where(g => period2Sections.Contains(g.SectionId)).ToList();
            var assumptionsInPeriod2 = store.GetAssumptions()
                .Where(a => period2Sections.Contains(a.SectionId))
                .ToList();
            
            Assert.Empty(gapsInPeriod2); // No gaps should be carried forward
            Assert.Empty(assumptionsInPeriod2); // No assumptions should be carried forward
        }
    }
}
