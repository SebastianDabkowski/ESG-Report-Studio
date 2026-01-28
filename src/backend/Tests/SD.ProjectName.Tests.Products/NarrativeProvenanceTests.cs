using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products
{
    public class NarrativeProvenanceTests
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

            // Create reporting period
            store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
            {
                Name = "FY 2024",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "owner-1",
                OwnerName = "Test Owner"
            });
        }

        private static string CreateTestSection(InMemoryReportStore store)
        {
            var snapshot = store.GetSnapshot();
            var periodId = snapshot.Periods.First().Id;
            
            var section = new ReportSection
            {
                Id = Guid.NewGuid().ToString(),
                PeriodId = periodId,
                Title = "Test Section",
                Category = "environmental",
                Description = "Test section for provenance",
                OwnerId = "owner-1",
                Status = "draft",
                Completeness = "empty",
                Order = 1
            };
            
            var sectionsField = typeof(InMemoryReportStore).GetField("_sections", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sections = sectionsField!.GetValue(store) as List<ReportSection>;
            sections!.Add(section);
            
            return section.Id;
        }

        [Fact]
        public void CreateDataPoint_WithSourceReferences_ShouldStoreAllProvenanceFields()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var sourceReferences = new List<NarrativeSourceReference>
            {
                new NarrativeSourceReference
                {
                    SourceType = "data-point",
                    SourceReference = "DP-2024-001",
                    Description = "Energy consumption data from operational metrics",
                    OriginSystem = "Energy Management System",
                    OwnerId = "owner-1",
                    OwnerName = "Energy Manager",
                    LastUpdated = "2024-01-15T10:00:00Z"
                },
                new NarrativeSourceReference
                {
                    SourceType = "evidence",
                    SourceReference = "EV-2024-042",
                    Description = "Annual sustainability report",
                    OriginSystem = "Document Management System",
                    OwnerId = "owner-2",
                    OwnerName = "Sustainability Lead",
                    LastUpdated = "2024-01-10T14:30:00Z"
                }
            };

            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Renewable Energy Adoption",
                Content = "The company has increased renewable energy usage to 45% of total consumption in 2024.",
                OwnerId = "owner-1",
                Source = "Energy Management System",
                InformationType = "fact",
                CompletenessStatus = "complete",
                Type = "narrative",
                SourceReferences = sourceReferences
            };

            // Act
            var (isValid, error, dataPoint) = store.CreateDataPoint(request);

            // Assert
            Assert.True(isValid, $"Expected valid data point creation but got error: {error}");
            Assert.NotNull(dataPoint);
            Assert.Equal(2, dataPoint.SourceReferences.Count);
            
            // Verify first source reference
            var source1 = dataPoint.SourceReferences[0];
            Assert.Equal("data-point", source1.SourceType);
            Assert.Equal("DP-2024-001", source1.SourceReference);
            Assert.Equal("Energy Management System", source1.OriginSystem);
            Assert.Equal("owner-1", source1.OwnerId);
            Assert.Equal("Energy Manager", source1.OwnerName);
            Assert.Equal("2024-01-15T10:00:00Z", source1.LastUpdated);
            
            // Verify second source reference
            var source2 = dataPoint.SourceReferences[1];
            Assert.Equal("evidence", source2.SourceType);
            Assert.Equal("EV-2024-042", source2.SourceReference);
            Assert.Equal("Document Management System", source2.OriginSystem);
            
            // Verify provenance flags
            Assert.False(dataPoint.ProvenanceNeedsReview);
            Assert.Null(dataPoint.ProvenanceReviewReason);
        }

        [Fact]
        public void UpdateDataPoint_WithSourceReferences_ShouldUpdateProvenanceFields()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var initialSources = new List<NarrativeSourceReference>
            {
                new NarrativeSourceReference
                {
                    SourceType = "data-point",
                    SourceReference = "DP-001",
                    Description = "Initial data source"
                }
            };

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Test Narrative",
                Content = "Initial content",
                OwnerId = "owner-1",
                Source = "System A",
                InformationType = "fact",
                CompletenessStatus = "incomplete",
                Type = "narrative",
                SourceReferences = initialSources
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            var updatedSources = new List<NarrativeSourceReference>
            {
                new NarrativeSourceReference
                {
                    SourceType = "data-point",
                    SourceReference = "DP-001",
                    Description = "Initial data source"
                },
                new NarrativeSourceReference
                {
                    SourceType = "evidence",
                    SourceReference = "EV-042",
                    Description = "Supporting evidence added",
                    OriginSystem = "DMS"
                }
            };

            var updateRequest = new UpdateDataPointRequest
            {
                Title = "Test Narrative",
                Content = "Updated content with additional sources",
                OwnerId = "owner-1",
                Source = "System A",
                InformationType = "fact",
                CompletenessStatus = "complete",
                Type = "narrative",
                SourceReferences = updatedSources
            };

            // Act
            var (isValid, error, updatedDataPoint) = store.UpdateDataPoint(dataPoint.Id, updateRequest);

            // Assert
            Assert.True(isValid, $"Expected valid update but got error: {error}");
            Assert.NotNull(updatedDataPoint);
            Assert.Equal(2, updatedDataPoint.SourceReferences.Count);
            Assert.Contains(updatedDataPoint.SourceReferences, s => s.SourceReference == "EV-042");
        }

        [Fact]
        public void FlagProvenanceForReview_ShouldSetReviewFlags()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Test Statement",
                Content = "This statement is based on source data",
                OwnerId = "owner-1",
                Source = "System",
                InformationType = "fact",
                CompletenessStatus = "complete",
                Type = "narrative",
                SourceReferences = new List<NarrativeSourceReference>
                {
                    new NarrativeSourceReference
                    {
                        SourceType = "data-point",
                        SourceReference = "DP-123",
                        Description = "Source data"
                    }
                }
            };

            var (_, _, dataPoint) = store.CreateDataPoint(request);
            Assert.NotNull(dataPoint);

            // Act
            var (success, error) = store.FlagProvenanceForReview(
                dataPoint.Id, 
                "Source data point DP-123 was updated", 
                "system");

            // Assert
            Assert.True(success, $"Expected successful flagging but got error: {error}");
            
            var flaggedDataPoint = store.GetDataPoint(dataPoint.Id);
            Assert.NotNull(flaggedDataPoint);
            Assert.True(flaggedDataPoint.ProvenanceNeedsReview);
            Assert.Equal("Source data point DP-123 was updated", flaggedDataPoint.ProvenanceReviewReason);
            Assert.Equal("system", flaggedDataPoint.ProvenanceFlaggedBy);
            Assert.NotNull(flaggedDataPoint.ProvenanceFlaggedAt);
        }

        [Fact]
        public void ClearProvenanceReviewFlag_ShouldResetReviewFlags()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Test Statement",
                Content = "This statement is based on source data",
                OwnerId = "owner-1",
                Source = "System",
                InformationType = "fact",
                CompletenessStatus = "complete",
                Type = "narrative"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(request);
            Assert.NotNull(dataPoint);

            // Flag for review first
            store.FlagProvenanceForReview(dataPoint.Id, "Test reason", "user-1");
            var flaggedDataPoint = store.GetDataPoint(dataPoint.Id);
            Assert.True(flaggedDataPoint!.ProvenanceNeedsReview);

            // Act
            var (success, error) = store.ClearProvenanceReviewFlag(dataPoint.Id);

            // Assert
            Assert.True(success, $"Expected successful clearing but got error: {error}");
            
            var clearedDataPoint = store.GetDataPoint(dataPoint.Id);
            Assert.NotNull(clearedDataPoint);
            Assert.False(clearedDataPoint.ProvenanceNeedsReview);
            Assert.Null(clearedDataPoint.ProvenanceReviewReason);
            Assert.Null(clearedDataPoint.ProvenanceFlaggedBy);
            Assert.Null(clearedDataPoint.ProvenanceFlaggedAt);
            Assert.NotNull(clearedDataPoint.ProvenanceLastVerified);
        }

        [Fact]
        public void CaptureProvenanceSnapshot_ShouldGenerateHashAndSetVerified()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var sourceReferences = new List<NarrativeSourceReference>
            {
                new NarrativeSourceReference
                {
                    SourceType = "data-point",
                    SourceReference = "DP-001",
                    Description = "Data source 1",
                    LastUpdated = "2024-01-15T10:00:00Z"
                },
                new NarrativeSourceReference
                {
                    SourceType = "evidence",
                    SourceReference = "EV-042",
                    Description = "Evidence source",
                    LastUpdated = "2024-01-10T14:00:00Z"
                }
            };

            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Test Statement",
                Content = "Statement with sources",
                OwnerId = "owner-1",
                Source = "System",
                InformationType = "fact",
                CompletenessStatus = "complete",
                Type = "narrative",
                SourceReferences = sourceReferences
            };

            var (_, _, dataPoint) = store.CreateDataPoint(request);
            Assert.NotNull(dataPoint);

            // Act
            var (success, error) = store.CaptureProvenanceSnapshot(dataPoint.Id);

            // Assert
            Assert.True(success, $"Expected successful snapshot but got error: {error}");
            
            var snapshotDataPoint = store.GetDataPoint(dataPoint.Id);
            Assert.NotNull(snapshotDataPoint);
            Assert.NotNull(snapshotDataPoint.PublicationSourceHash);
            Assert.NotEmpty(snapshotDataPoint.PublicationSourceHash);
            Assert.NotNull(snapshotDataPoint.ProvenanceLastVerified);
            Assert.False(snapshotDataPoint.ProvenanceNeedsReview);
        }

        [Fact]
        public void CreateDataPoint_WithoutSourceReferences_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Simple Narrative",
                Content = "This narrative has no source references yet",
                OwnerId = "owner-1",
                Source = "Manual entry",
                InformationType = "declaration",
                CompletenessStatus = "incomplete",
                Type = "narrative"
                // SourceReferences not provided
            };

            // Act
            var (isValid, error, dataPoint) = store.CreateDataPoint(request);

            // Assert
            Assert.True(isValid, $"Expected valid creation but got error: {error}");
            Assert.NotNull(dataPoint);
            Assert.Empty(dataPoint.SourceReferences);
            Assert.False(dataPoint.ProvenanceNeedsReview);
        }

        [Fact]
        public void SourceReferences_ShouldSupportMultipleSourceTypes()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var diverseSources = new List<NarrativeSourceReference>
            {
                new NarrativeSourceReference
                {
                    SourceType = "data-point",
                    SourceReference = "DP-001",
                    Description = "Internal metric data point"
                },
                new NarrativeSourceReference
                {
                    SourceType = "evidence",
                    SourceReference = "EV-042",
                    Description = "Uploaded evidence file"
                },
                new NarrativeSourceReference
                {
                    SourceType = "assumption",
                    SourceReference = "ASM-015",
                    Description = "Referenced assumption"
                },
                new NarrativeSourceReference
                {
                    SourceType = "external-system",
                    SourceReference = "HR-SYSTEM-2024",
                    Description = "HR system export",
                    OriginSystem = "Workday HR"
                },
                new NarrativeSourceReference
                {
                    SourceType = "uploaded-file",
                    SourceReference = "FILE-789",
                    Description = "Manual CSV import",
                    OriginSystem = "operations_data_2024.csv"
                }
            };

            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Title = "Multi-Source Statement",
                Content = "This statement is based on data from multiple sources",
                OwnerId = "owner-1",
                Source = "Aggregated",
                InformationType = "fact",
                CompletenessStatus = "complete",
                Type = "narrative",
                SourceReferences = diverseSources
            };

            // Act
            var (isValid, error, dataPoint) = store.CreateDataPoint(request);

            // Assert
            Assert.True(isValid, $"Expected valid creation but got error: {error}");
            Assert.NotNull(dataPoint);
            Assert.Equal(5, dataPoint.SourceReferences.Count);
            
            // Verify different source types are preserved
            Assert.Contains(dataPoint.SourceReferences, s => s.SourceType == "data-point");
            Assert.Contains(dataPoint.SourceReferences, s => s.SourceType == "evidence");
            Assert.Contains(dataPoint.SourceReferences, s => s.SourceType == "assumption");
            Assert.Contains(dataPoint.SourceReferences, s => s.SourceType == "external-system");
            Assert.Contains(dataPoint.SourceReferences, s => s.SourceType == "uploaded-file");
        }
    }
}
