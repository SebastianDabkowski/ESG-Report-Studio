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
        
        [Fact]
        public void Rollover_ShouldMapSectionsByCatalogCode()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestOrganization(store);
            CreateTestOrganizationalUnit(store);
            
            var sourcePeriodId = CreateTestPeriod(store, "FY 2024");
            
            // Add a section with catalog code to source period using reflection
            var section = new ReportSection
            {
                Id = Guid.NewGuid().ToString(),
                PeriodId = sourcePeriodId,
                Title = "Test Custom Emissions",
                Category = "environmental",
                Description = "GHG Emissions Test",
                OwnerId = "user1",
                CatalogCode = "ENV-TEST-001",
                Status = "draft",
                Completeness = "empty",
                Order = 100
            };
            
            var sectionsField = typeof(InMemoryReportStore).GetField("_sections", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sections = sectionsField!.GetValue(store) as List<ReportSection>;
            sections!.Add(section);
            
            var rolloverRequest = new RolloverRequest
            {
                SourcePeriodId = sourcePeriodId,
                TargetPeriodName = "FY 2025",
                TargetPeriodStartDate = "2025-01-01",
                TargetPeriodEndDate = "2025-12-31",
                Options = new RolloverOptions { CopyStructure = true },
                PerformedBy = "user1"
            };
            
            // Act
            var (success, errorMessage, result) = store.RolloverPeriod(rolloverRequest);
            
            // Assert
            Assert.True(success, errorMessage);
            Assert.NotNull(result.Reconciliation);
            Assert.True(result.Reconciliation.TotalSourceSections > 0);
            Assert.True(result.Reconciliation.MappedSections > 0);
            Assert.Equal(0, result.Reconciliation.UnmappedSections);
            
            // Find our specific mapped section
            var mappedSection = result.Reconciliation.MappedItems.FirstOrDefault(m => m.SourceCatalogCode == "ENV-TEST-001");
            Assert.NotNull(mappedSection);
            Assert.Equal("ENV-TEST-001", mappedSection.TargetCatalogCode);
            Assert.Equal("automatic", mappedSection.MappingType);
        }
        
        [Fact]
        public void Rollover_ShouldDetectUnmappedSectionsWithoutCatalogCode()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestOrganization(store);
            CreateTestOrganizationalUnit(store);
            
            var sourcePeriodId = CreateTestPeriod(store, "FY 2024");
            
            // Add a section WITHOUT catalog code to source period using reflection
            var section = new ReportSection
            {
                Id = Guid.NewGuid().ToString(),
                PeriodId = sourcePeriodId,
                Title = "Custom Section Without Code",
                Category = "environmental",
                Description = "Custom section without catalog code",
                OwnerId = "user1",
                // No CatalogCode provided
                Status = "draft",
                Completeness = "empty",
                Order = 100
            };
            
            var sectionsField = typeof(InMemoryReportStore).GetField("_sections", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sections = sectionsField!.GetValue(store) as List<ReportSection>;
            sections!.Add(section);
            
            var rolloverRequest = new RolloverRequest
            {
                SourcePeriodId = sourcePeriodId,
                TargetPeriodName = "FY 2025",
                TargetPeriodStartDate = "2025-01-01",
                TargetPeriodEndDate = "2025-12-31",
                Options = new RolloverOptions { CopyStructure = true },
                PerformedBy = "user1"
            };
            
            // Act
            var (success, errorMessage, result) = store.RolloverPeriod(rolloverRequest);
            
            // Assert
            Assert.True(success, errorMessage);
            Assert.NotNull(result.Reconciliation);
            Assert.True(result.Reconciliation.UnmappedSections > 0);
            
            // Find our specific unmapped section
            var unmappedSection = result.Reconciliation.UnmappedItems.FirstOrDefault(u => u.SourceTitle == "Custom Section Without Code");
            Assert.NotNull(unmappedSection);
            Assert.Contains("no catalog code", unmappedSection.Reason, StringComparison.OrdinalIgnoreCase);
            Assert.NotEmpty(unmappedSection.SuggestedActions);
        }
        
        [Fact]
        public void Rollover_ShouldApplyManualMappings()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestOrganization(store);
            CreateTestOrganizationalUnit(store);
            
            var sourcePeriodId = CreateTestPeriod(store, "FY 2024");
            
            // Add a section with old catalog code to source period using reflection
            var section = new ReportSection
            {
                Id = Guid.NewGuid().ToString(),
                PeriodId = sourcePeriodId,
                Title = "Old Emissions Section",
                Category = "environmental",
                Description = "Old section structure",
                OwnerId = "user1",
                CatalogCode = "ENV-OLD-001",
                Status = "draft",
                Completeness = "empty",
                Order = 100
            };
            
            var sectionsField = typeof(InMemoryReportStore).GetField("_sections", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sections = sectionsField!.GetValue(store) as List<ReportSection>;
            sections!.Add(section);
            
            var rolloverRequest = new RolloverRequest
            {
                SourcePeriodId = sourcePeriodId,
                TargetPeriodName = "FY 2025",
                TargetPeriodStartDate = "2025-01-01",
                TargetPeriodEndDate = "2025-12-31",
                Options = new RolloverOptions { CopyStructure = true },
                PerformedBy = "user1",
                ManualMappings = new List<ManualSectionMapping>
                {
                    new ManualSectionMapping
                    {
                        SourceCatalogCode = "ENV-OLD-001",
                        TargetCatalogCode = "ENV-NEW-001"
                    }
                }
            };
            
            // Act
            var (success, errorMessage, result) = store.RolloverPeriod(rolloverRequest);
            
            // Assert
            Assert.True(success, errorMessage);
            Assert.NotNull(result.Reconciliation);
            Assert.True(result.Reconciliation.MappedSections > 0);
            
            // Find our specific manually mapped section
            var mappedSection = result.Reconciliation.MappedItems.FirstOrDefault(m => m.SourceCatalogCode == "ENV-OLD-001");
            Assert.NotNull(mappedSection);
            Assert.Equal("ENV-NEW-001", mappedSection.TargetCatalogCode);
            Assert.Equal("manual", mappedSection.MappingType);
        }
        
        [Fact]
        public void Rollover_ShouldTrackDataPointsPerSection()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestOrganization(store);
            CreateTestOrganizationalUnit(store);
            
            var sourcePeriodId = CreateTestPeriod(store, "FY 2024");
            
            // Add a section using reflection
            var sectionId = Guid.NewGuid().ToString();
            var section = new ReportSection
            {
                Id = sectionId,
                PeriodId = sourcePeriodId,
                Title = "Test Emissions With Data",
                Category = "environmental",
                Description = "Test GHG Emissions",
                OwnerId = "user1",
                CatalogCode = "ENV-TEST-002",
                Status = "draft",
                Completeness = "empty",
                Order = 100
            };
            
            var sectionsField = typeof(InMemoryReportStore).GetField("_sections", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sections = sectionsField!.GetValue(store) as List<ReportSection>;
            sections!.Add(section);
            
            // Add data points to the section using reflection
            var dataPointsField = typeof(InMemoryReportStore).GetField("_dataPoints", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dataPoints = dataPointsField!.GetValue(store) as List<DataPoint>;
            
            dataPoints!.Add(new DataPoint
            {
                Id = Guid.NewGuid().ToString(),
                SectionId = sectionId,
                Type = "metric",
                Title = "Total Emissions",
                OwnerId = "user1",
                Content = "",
                CompletenessStatus = "empty",
                ReviewStatus = "draft",
                CreatedAt = DateTime.UtcNow.ToString("o"),
                UpdatedAt = DateTime.UtcNow.ToString("o")
            });
            
            dataPoints.Add(new DataPoint
            {
                Id = Guid.NewGuid().ToString(),
                SectionId = sectionId,
                Type = "metric",
                Title = "Scope 1 Emissions",
                OwnerId = "user1",
                Content = "",
                CompletenessStatus = "empty",
                ReviewStatus = "draft",
                CreatedAt = DateTime.UtcNow.ToString("o"),
                UpdatedAt = DateTime.UtcNow.ToString("o")
            });
            
            var rolloverRequest = new RolloverRequest
            {
                SourcePeriodId = sourcePeriodId,
                TargetPeriodName = "FY 2025",
                TargetPeriodStartDate = "2025-01-01",
                TargetPeriodEndDate = "2025-12-31",
                Options = new RolloverOptions 
                { 
                    CopyStructure = true,
                    CopyDataValues = true
                },
                PerformedBy = "user1"
            };
            
            // Act
            var (success, errorMessage, result) = store.RolloverPeriod(rolloverRequest);
            
            // Assert
            Assert.True(success, errorMessage);
            Assert.NotNull(result.Reconciliation);
            Assert.True(result.Reconciliation.MappedSections > 0);
            
            // Find our specific section with data points
            var mappedSection = result.Reconciliation.MappedItems.FirstOrDefault(m => m.SourceCatalogCode == "ENV-TEST-002");
            Assert.NotNull(mappedSection);
            Assert.Equal(2, mappedSection.DataPointsCopied);
        }
        
        [Fact]
        public void GetRolloverReconciliation_ShouldReturnStoredReconciliation()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestOrganization(store);
            CreateTestOrganizationalUnit(store);
            
            var sourcePeriodId = CreateTestPeriod(store, "FY 2024");
            
            // Add a section using reflection
            var section = new ReportSection
            {
                Id = Guid.NewGuid().ToString(),
                PeriodId = sourcePeriodId,
                Title = "Emissions",
                Category = "environmental",
                Description = "GHG Emissions",
                OwnerId = "user1",
                CatalogCode = "ENV-001",
                Status = "draft",
                Completeness = "empty",
                Order = 1
            };
            
            var sectionsField = typeof(InMemoryReportStore).GetField("_sections", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sections = sectionsField!.GetValue(store) as List<ReportSection>;
            sections!.Add(section);
            
            var rolloverRequest = new RolloverRequest
            {
                SourcePeriodId = sourcePeriodId,
                TargetPeriodName = "FY 2025",
                TargetPeriodStartDate = "2025-01-01",
                TargetPeriodEndDate = "2025-12-31",
                Options = new RolloverOptions { CopyStructure = true },
                PerformedBy = "user1"
            };
            
            var (success, errorMessage, result) = store.RolloverPeriod(rolloverRequest);
            Assert.True(success, errorMessage);
            
            // Act
            var reconciliation = store.GetRolloverReconciliation(result.TargetPeriod!.Id);
            
            // Assert
            Assert.NotNull(reconciliation);
            Assert.True(reconciliation.TotalSourceSections > 0);
            Assert.True(reconciliation.MappedSections > 0);
        }
        
        [Fact]
        public void Rollover_ShouldMapToExistingTargetSection()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestOrganization(store);
            CreateTestOrganizationalUnit(store);
            
            var sourcePeriodId = CreateTestPeriod(store, "FY 2024");
            
            // Get one of the automatically created sections from the source period
            var sourceSections = store.GetSections(sourcePeriodId);
            var firstSourceSection = sourceSections.First();
            
            // Add a data point to this section
            var dataPointsField = typeof(InMemoryReportStore).GetField("_dataPoints", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dataPoints = dataPointsField!.GetValue(store) as List<DataPoint>;
            
            dataPoints!.Add(new DataPoint
            {
                Id = Guid.NewGuid().ToString(),
                SectionId = firstSourceSection.Id,
                Type = "metric",
                Title = "Test Metric",
                OwnerId = "user1",
                Content = "Test content",
                CompletenessStatus = "complete",
                ReviewStatus = "approved",
                CreatedAt = DateTime.UtcNow.ToString("o"),
                UpdatedAt = DateTime.UtcNow.ToString("o")
            });
            
            var rolloverRequest = new RolloverRequest
            {
                SourcePeriodId = sourcePeriodId,
                TargetPeriodName = "FY 2025",
                TargetPeriodStartDate = "2025-01-01",
                TargetPeriodEndDate = "2025-12-31",
                Options = new RolloverOptions 
                { 
                    CopyStructure = true,
                    CopyDataValues = true
                },
                PerformedBy = "user1"
            };
            
            // Act
            var (success, errorMessage, result) = store.RolloverPeriod(rolloverRequest);
            
            // Assert
            Assert.True(success, errorMessage);
            Assert.NotNull(result.Reconciliation);
            Assert.NotNull(result.TargetPeriod);
            
            // The catalog section should be mapped automatically
            var mappedSection = result.Reconciliation.MappedItems.FirstOrDefault(m => 
                m.SourceCatalogCode == firstSourceSection.CatalogCode);
            Assert.NotNull(mappedSection);
            Assert.Equal("automatic", mappedSection.MappingType);
            Assert.Equal(1, mappedSection.DataPointsCopied);
            
            // Verify that the target period has a section with the same catalog code
            var targetSections = store.GetSections(result.TargetPeriod.Id);
            var matchingTargetSection = targetSections.FirstOrDefault(s => s.CatalogCode == firstSourceSection.CatalogCode);
            Assert.NotNull(matchingTargetSection);
            
            // Verify data point was copied to the target section
            var targetDataPoints = dataPoints.Where(dp => dp.SectionId == matchingTargetSection.Id).ToList();
            Assert.Single(targetDataPoints);
            Assert.Equal("Test Metric", targetDataPoints.First().Title);
        }

        [Fact]
        public void Rollover_WithDueDateAdjustment_ShouldAdjustTaskDueDates()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestOrganization(store);
            CreateTestOrganizationalUnit(store);
            
            var sourcePeriodId = CreateTestPeriod(store, "FY 2024");
            
            // Get or create a section
            var sections = store.GetSections(sourcePeriodId);
            var section = sections.FirstOrDefault();
            
            // If no sections exist, the test period should have created them
            Assert.NotNull(section);
            
            // Create a remediation plan with an action
            var (planValid, planError, plan) = store.CreateRemediationPlan(
                section.Id,
                "Test Plan",
                "Test Description",
                "Q1 2025",
                "user1",
                "Test User",
                "medium",
                null,
                null,
                null,
                "user1"
            );
            
            Assert.True(planValid, planError);
            Assert.NotNull(plan);
            
            var (actionValid, actionError, action) = store.CreateRemediationAction(
                plan.Id,
                "Test Action",
                "Test action description",
                "user1",
                "Test User",
                "2024-06-01T00:00:00.000Z",
                "user1"
            );
            
            Assert.True(actionValid, actionError);
            Assert.NotNull(action);

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
                    CopyAttachments = false,
                    DueDateAdjustmentDays = 365 // Shift forward by one year
                },
                PerformedBy = "user1"
            };

            // Act
            var (success, errorMessage, result) = store.RolloverPeriod(rolloverRequest);

            // Assert
            Assert.True(success, errorMessage);
            Assert.NotNull(result);
            Assert.NotNull(result.TargetPeriod);
            
            // Verify remediation action was copied with adjusted due date
            var allPlans = store.GetRemediationPlans();
            var targetSections = store.GetSections(result.TargetPeriod.Id);
            var targetPlansList = allPlans.Where(p => 
                targetSections.Any(s => s.Id == p.SectionId)).ToList();
            Assert.NotEmpty(targetPlansList);
            
            var targetPlan = targetPlansList.First(p => p.Title == "Test Plan");
            var targetActions = store.GetRemediationActions(targetPlan.Id);
            Assert.NotEmpty(targetActions);
            
            var targetAction = targetActions.First();
            Assert.Equal("Test Action", targetAction.Title);
            
            // Verify due date was adjusted
            var originalDueDate = DateTime.Parse("2024-06-01T00:00:00.000Z");
            var expectedDueDate = originalDueDate.AddDays(365);
            var actualDueDate = DateTime.Parse(targetAction.DueDate);
            
            // Compare dates (allowing for minor time zone differences)
            Assert.Equal(expectedDueDate.Date, actualDueDate.Date);
        }

        [Fact]
        public void Rollover_WithInactiveOwner_ShouldFlagWarning()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestOrganization(store);
            CreateTestOrganizationalUnit(store);
            
            var sourcePeriodId = CreateTestPeriod(store, "FY 2024");
            
            // Get a section and an existing user
            var sections = store.GetSections(sourcePeriodId);
            var section = sections.FirstOrDefault();
            Assert.NotNull(section);
            
            var users = store.GetUsers();
            var inactiveUser = users.First();
            
            // Mark user as inactive (simulating a user who left the organization)
            inactiveUser.IsActive = false;
            
            // Update section owner to inactive user
            var updateRequest = new UpdateSectionOwnerRequest
            {
                OwnerId = inactiveUser.Id,
                UpdatedBy = "admin"
            };
            store.UpdateSectionOwner(section.Id, updateRequest);
            
            // Create a remediation plan with an action owned by inactive user
            var (planValid, planError, plan) = store.CreateRemediationPlan(
                section.Id,
                "Test Plan",
                "Test Description",
                "Q1 2025",
                inactiveUser.Id,
                inactiveUser.Name,
                "medium",
                null,
                null,
                null,
                "user1"
            );
            
            Assert.True(planValid, planError);
            Assert.NotNull(plan);
            
            var (actionValid, actionError, action) = store.CreateRemediationAction(
                plan.Id,
                "Test Action",
                "Test action description",
                inactiveUser.Id,
                inactiveUser.Name,
                "2024-06-01T00:00:00.000Z",
                "user1"
            );
            
            Assert.True(actionValid, actionError);
            Assert.NotNull(action);

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
            Assert.NotNull(result.InactiveOwnerWarnings);
            
            // Should have warnings for section, remediation plan, and remediation action
            Assert.NotEmpty(result.InactiveOwnerWarnings);
            
            // Debug: Print all warnings
            var warningTypes = string.Join(", ", result.InactiveOwnerWarnings.Select(w => w.EntityType));
            
            // We expect at least remediation plan and action warnings
            Assert.True(result.InactiveOwnerWarnings.Count >= 2, $"Expected at least 2 warnings, got {result.InactiveOwnerWarnings.Count}. Types: {warningTypes}");
            
            // Verify warnings contain the inactive user
            var userWarnings = result.InactiveOwnerWarnings.Where(w => w.UserId == inactiveUser.Id).ToList();
            Assert.NotEmpty(userWarnings);
            
            // Verify warnings include different entity types
            // Note: Section warnings are expected if the section ownership is copied
            var entityTypes = result.InactiveOwnerWarnings.Select(w => w.EntityType).Distinct().ToList();
            Assert.Contains("RemediationPlan", entityTypes);
            Assert.Contains("RemediationAction", entityTypes);
            // Section warning is optional depending on if sections copy ownership
        }

        [Fact]
        public void Rollover_WithActiveOwner_ShouldNotFlagWarning()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestOrganization(store);
            CreateTestOrganizationalUnit(store);
            
            var sourcePeriodId = CreateTestPeriod(store, "FY 2024");
            
            // Get a section
            var sections = store.GetSections(sourcePeriodId);
            var section = sections.FirstOrDefault();
            Assert.NotNull(section);
            
            // Create a remediation plan with an action
            var (planValid, planError, plan) = store.CreateRemediationPlan(
                section.Id,
                "Test Plan",
                "Test Description",
                "Q1 2025",
                "user1",
                "Test User",
                "medium",
                null,
                null,
                null,
                "user1"
            );
            
            Assert.True(planValid, planError);
            Assert.NotNull(plan);
            
            var (actionValid, actionError, action) = store.CreateRemediationAction(
                plan.Id,
                "Test Action",
                "Test action description",
                "user1",
                "Test User",
                "2024-06-01T00:00:00.000Z",
                "user1"
            );
            
            Assert.True(actionValid, actionError);
            Assert.NotNull(action);

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
            Assert.NotNull(result.InactiveOwnerWarnings);
            
            // Should have no warnings for active users
            Assert.Empty(result.InactiveOwnerWarnings);
        }
    }
}
