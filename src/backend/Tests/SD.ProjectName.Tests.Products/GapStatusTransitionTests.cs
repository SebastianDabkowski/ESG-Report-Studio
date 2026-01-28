using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products
{
    public class GapStatusTransitionTests
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
                OwnerId = "user-1",
                OwnerName = "Sarah Chen"
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
                Description = "Test section for data points",
                OwnerId = "user-1",
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
        public void TransitionGapStatus_FromNoneToMissing_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Energy Consumption",
                Content = "Total energy used",
                OwnerId = "user-1",
                Source = "Energy Management System",
                InformationType = "fact",
                CompletenessStatus = "incomplete"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // Act
            var transitionRequest = new TransitionGapStatusRequest
            {
                TransitionedBy = "user-1",
                TargetStatus = "missing",
                ChangeNote = "Data not available for Q4"
            };

            var (isValid, errorMessage, updatedDataPoint) = store.TransitionGapStatus(dataPoint.Id, transitionRequest);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(updatedDataPoint);
            Assert.Equal("missing", updatedDataPoint.GapStatus);
            Assert.True(updatedDataPoint.IsMissing);
            Assert.Equal("missing", updatedDataPoint.CompletenessStatus);
        }

        [Fact]
        public void TransitionGapStatus_FromMissingToEstimated_WithAllFields_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Scope 3 Emissions",
                Content = "Supply chain emissions",
                OwnerId = "user-1",
                Source = "Carbon Calculator",
                InformationType = "fact",
                CompletenessStatus = "incomplete"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // First, transition to missing
            var missingRequest = new TransitionGapStatusRequest
            {
                TransitionedBy = "user-1",
                TargetStatus = "missing",
                ChangeNote = "Supplier data not available"
            };
            store.TransitionGapStatus(dataPoint.Id, missingRequest);

            // Act - Transition to estimated
            var estimateRequest = new TransitionGapStatusRequest
            {
                TransitionedBy = "user-1",
                TargetStatus = "estimated",
                EstimateType = "extrapolated",
                EstimateMethod = "Industry average multiplied by spend data",
                ConfidenceLevel = "low",
                ChangeNote = "Using industry benchmarks"
            };

            var (isValid, errorMessage, updatedDataPoint) = store.TransitionGapStatus(dataPoint.Id, estimateRequest);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(updatedDataPoint);
            Assert.Equal("estimated", updatedDataPoint.GapStatus);
            Assert.False(updatedDataPoint.IsMissing);
            Assert.Equal("estimate", updatedDataPoint.InformationType);
            Assert.Equal("extrapolated", updatedDataPoint.EstimateType);
            Assert.Equal("Industry average multiplied by spend data", updatedDataPoint.EstimateMethod);
            Assert.Equal("low", updatedDataPoint.ConfidenceLevel);
            Assert.Equal("incomplete", updatedDataPoint.CompletenessStatus);
        }

        [Fact]
        public void TransitionGapStatus_FromEstimatedToProvided_ShouldPreserveEstimate()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Water Consumption",
                Content = "Total water usage",
                Value = "1250",
                Unit = "m³",
                OwnerId = "user-1",
                Source = "Estimation Model",
                InformationType = "fact",
                CompletenessStatus = "incomplete"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // Transition to missing
            store.TransitionGapStatus(dataPoint.Id, new TransitionGapStatusRequest
            {
                TransitionedBy = "user-1",
                TargetStatus = "missing"
            });

            // Transition to estimated
            store.TransitionGapStatus(dataPoint.Id, new TransitionGapStatusRequest
            {
                TransitionedBy = "user-1",
                TargetStatus = "estimated",
                EstimateType = "proxy-based",
                EstimateMethod = "Similar facility usage patterns",
                ConfidenceLevel = "medium",
                ChangeNote = "Using proxy data from similar site"
            });

            // Act - Transition to provided
            var providedRequest = new TransitionGapStatusRequest
            {
                TransitionedBy = "user-1",
                TargetStatus = "provided",
                ChangeNote = "Actual meter readings now available"
            };

            var (isValid, errorMessage, updatedDataPoint) = store.TransitionGapStatus(dataPoint.Id, providedRequest);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(updatedDataPoint);
            Assert.Equal("provided", updatedDataPoint.GapStatus);
            Assert.False(updatedDataPoint.IsMissing);
            Assert.Equal("complete", updatedDataPoint.CompletenessStatus);
            
            // Verify estimate was preserved
            Assert.NotNull(updatedDataPoint.PreviousEstimateSnapshot);
            Assert.Contains("proxy-based", updatedDataPoint.PreviousEstimateSnapshot);
            Assert.Contains("Similar facility usage patterns", updatedDataPoint.PreviousEstimateSnapshot);
            Assert.Contains("medium", updatedDataPoint.PreviousEstimateSnapshot);
        }

        [Fact]
        public void TransitionGapStatus_ToEstimatedWithoutEstimateType_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Test Data Point",
                Content = "Test content",
                OwnerId = "user-1",
                Source = "Test Source",
                InformationType = "fact",
                CompletenessStatus = "incomplete"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // First transition to missing (required workflow step)
            store.TransitionGapStatus(dataPoint.Id, new TransitionGapStatusRequest
            {
                TransitionedBy = "user-1",
                TargetStatus = "missing"
            });

            // Act - Try to transition to estimated without EstimateType
            var transitionRequest = new TransitionGapStatusRequest
            {
                TransitionedBy = "user-1",
                TargetStatus = "estimated",
                // Missing EstimateType
                EstimateMethod = "Some method",
                ConfidenceLevel = "medium"
            };

            var (isValid, errorMessage, updatedDataPoint) = store.TransitionGapStatus(dataPoint.Id, transitionRequest);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("EstimateType is required", errorMessage);
        }

        [Fact]
        public void TransitionGapStatus_ToEstimatedWithoutEstimateMethod_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Test Data Point",
                Content = "Test content",
                OwnerId = "user-1",
                Source = "Test Source",
                InformationType = "fact",
                CompletenessStatus = "incomplete"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // First transition to missing (required workflow step)
            store.TransitionGapStatus(dataPoint.Id, new TransitionGapStatusRequest
            {
                TransitionedBy = "user-1",
                TargetStatus = "missing"
            });

            // Act - Try to transition to estimated without EstimateMethod
            var transitionRequest = new TransitionGapStatusRequest
            {
                TransitionedBy = "user-1",
                TargetStatus = "estimated",
                EstimateType = "point",
                // Missing EstimateMethod
                ConfidenceLevel = "medium"
            };

            var (isValid, errorMessage, updatedDataPoint) = store.TransitionGapStatus(dataPoint.Id, transitionRequest);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("EstimateMethod is required", errorMessage);
        }

        [Fact]
        public void TransitionGapStatus_ToEstimatedWithoutConfidenceLevel_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Test Data Point",
                Content = "Test content",
                OwnerId = "user-1",
                Source = "Test Source",
                InformationType = "fact",
                CompletenessStatus = "incomplete"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // First transition to missing (required workflow step)
            store.TransitionGapStatus(dataPoint.Id, new TransitionGapStatusRequest
            {
                TransitionedBy = "user-1",
                TargetStatus = "missing"
            });

            // Act - Try to transition to estimated without ConfidenceLevel
            var transitionRequest = new TransitionGapStatusRequest
            {
                TransitionedBy = "user-1",
                TargetStatus = "estimated",
                EstimateType = "point",
                EstimateMethod = "Some method"
                // Missing ConfidenceLevel
            };

            var (isValid, errorMessage, updatedDataPoint) = store.TransitionGapStatus(dataPoint.Id, transitionRequest);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("ConfidenceLevel is required", errorMessage);
        }

        [Fact]
        public void TransitionGapStatus_BackwardFromEstimatedToMissing_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Test Data Point",
                Content = "Test content",
                OwnerId = "user-1",
                Source = "Test Source",
                InformationType = "fact",
                CompletenessStatus = "incomplete"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // Transition to missing
            store.TransitionGapStatus(dataPoint.Id, new TransitionGapStatusRequest
            {
                TransitionedBy = "user-1",
                TargetStatus = "missing"
            });

            // Transition to estimated
            store.TransitionGapStatus(dataPoint.Id, new TransitionGapStatusRequest
            {
                TransitionedBy = "user-1",
                TargetStatus = "estimated",
                EstimateType = "point",
                EstimateMethod = "Some method",
                ConfidenceLevel = "medium"
            });

            // Act - Try to go back to missing
            var transitionRequest = new TransitionGapStatusRequest
            {
                TransitionedBy = "user-1",
                TargetStatus = "missing"
            };

            var (isValid, errorMessage, updatedDataPoint) = store.TransitionGapStatus(dataPoint.Id, transitionRequest);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("Cannot transition from 'estimated' back to 'missing'", errorMessage);
        }

        [Fact]
        public void TransitionGapStatus_BackwardFromProvidedToEstimated_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Test Data Point",
                Content = "Test content",
                OwnerId = "user-1",
                Source = "Test Source",
                InformationType = "fact",
                CompletenessStatus = "incomplete"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // Transition through workflow
            store.TransitionGapStatus(dataPoint.Id, new TransitionGapStatusRequest
            {
                TransitionedBy = "user-1",
                TargetStatus = "missing"
            });

            store.TransitionGapStatus(dataPoint.Id, new TransitionGapStatusRequest
            {
                TransitionedBy = "user-1",
                TargetStatus = "estimated",
                EstimateType = "point",
                EstimateMethod = "Some method",
                ConfidenceLevel = "medium"
            });

            store.TransitionGapStatus(dataPoint.Id, new TransitionGapStatusRequest
            {
                TransitionedBy = "user-1",
                TargetStatus = "provided"
            });

            // Act - Try to go back to estimated
            var transitionRequest = new TransitionGapStatusRequest
            {
                TransitionedBy = "user-1",
                TargetStatus = "estimated",
                EstimateType = "point",
                EstimateMethod = "Some method",
                ConfidenceLevel = "medium"
            };

            var (isValid, errorMessage, updatedDataPoint) = store.TransitionGapStatus(dataPoint.Id, transitionRequest);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("Cannot transition from 'provided' back to earlier states", errorMessage);
        }

        [Fact]
        public void TransitionGapStatus_WithInvalidTargetStatus_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Test Data Point",
                Content = "Test content",
                OwnerId = "user-1",
                Source = "Test Source",
                InformationType = "fact",
                CompletenessStatus = "incomplete"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // Act
            var transitionRequest = new TransitionGapStatusRequest
            {
                TransitionedBy = "user-1",
                TargetStatus = "invalid-status"
            };

            var (isValid, errorMessage, updatedDataPoint) = store.TransitionGapStatus(dataPoint.Id, transitionRequest);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("must be one of: missing, estimated, provided", errorMessage);
        }

        [Fact]
        public void TransitionGapStatus_ByNonOwner_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Test Data Point",
                Content = "Test content",
                OwnerId = "user-1", // Owned by user-1
                Source = "Test Source",
                InformationType = "fact",
                CompletenessStatus = "incomplete"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // Act - Try to transition as user-3 (contributor, not owner)
            var transitionRequest = new TransitionGapStatusRequest
            {
                TransitionedBy = "user-3",
                TargetStatus = "missing"
            };

            var (isValid, errorMessage, updatedDataPoint) = store.TransitionGapStatus(dataPoint.Id, transitionRequest);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("Permission denied", errorMessage);
            
            // Verify unauthorized attempt was logged
            var auditLog = store.GetAuditLog();
            var deniedEntry = auditLog.FirstOrDefault(e => 
                e.Action == "transition-gap-status-denied" && 
                e.EntityId == dataPoint.Id);
            Assert.NotNull(deniedEntry);
            Assert.Equal("user-3", deniedEntry.UserId);
        }

        [Fact]
        public void TransitionGapStatus_ByAdmin_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Test Data Point",
                Content = "Test content",
                OwnerId = "user-1", // Owned by user-1
                Source = "Test Source",
                InformationType = "fact",
                CompletenessStatus = "incomplete"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // Act - Transition as admin (user-2)
            var transitionRequest = new TransitionGapStatusRequest
            {
                TransitionedBy = "user-2", // Admin
                TargetStatus = "missing"
            };

            var (isValid, errorMessage, updatedDataPoint) = store.TransitionGapStatus(dataPoint.Id, transitionRequest);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(updatedDataPoint);
            Assert.Equal("missing", updatedDataPoint.GapStatus);
        }

        [Fact]
        public void TransitionGapStatus_ShouldCreateAuditLogEntry()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Carbon Emissions",
                Content = "Total carbon footprint",
                OwnerId = "user-1",
                Source = "Carbon Calculator",
                InformationType = "fact",
                CompletenessStatus = "incomplete"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // Act
            var transitionRequest = new TransitionGapStatusRequest
            {
                TransitionedBy = "user-1",
                TargetStatus = "missing",
                ChangeNote = "Q4 data delayed from supplier"
            };

            store.TransitionGapStatus(dataPoint.Id, transitionRequest);

            // Assert
            var auditLog = store.GetAuditLog();
            var transitionEntry = auditLog.FirstOrDefault(e => 
                e.Action == "transition-gap-status" && 
                e.EntityId == dataPoint.Id);

            Assert.NotNull(transitionEntry);
            Assert.Equal("user-1", transitionEntry.UserId);
            Assert.Equal("Sarah Chen", transitionEntry.UserName);
            Assert.Contains("Q4 data delayed from supplier", transitionEntry.ChangeNote);
            Assert.Contains(transitionEntry.Changes, c => c.Field == "GapStatus");
        }

        [Fact]
        public void TransitionGapStatus_SameStatus_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Test Data Point",
                Content = "Test content",
                OwnerId = "user-1",
                Source = "Test Source",
                InformationType = "fact",
                CompletenessStatus = "incomplete"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // Transition to missing
            store.TransitionGapStatus(dataPoint.Id, new TransitionGapStatusRequest
            {
                TransitionedBy = "user-1",
                TargetStatus = "missing"
            });

            // Act - Try to transition to missing again
            var transitionRequest = new TransitionGapStatusRequest
            {
                TransitionedBy = "user-1",
                TargetStatus = "missing"
            };

            var (isValid, errorMessage, updatedDataPoint) = store.TransitionGapStatus(dataPoint.Id, transitionRequest);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("already in 'missing' status", errorMessage);
        }

        [Fact]
        public void TransitionGapStatus_SkipMissingToEstimated_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Test Data Point",
                Content = "Test content",
                OwnerId = "user-1",
                Source = "Test Source",
                InformationType = "fact",
                CompletenessStatus = "incomplete"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // Act - Try to skip directly to estimated without going through missing
            var transitionRequest = new TransitionGapStatusRequest
            {
                TransitionedBy = "user-1",
                TargetStatus = "estimated",
                EstimateType = "point",
                EstimateMethod = "Some method",
                ConfidenceLevel = "medium"
            };

            var (isValid, errorMessage, updatedDataPoint) = store.TransitionGapStatus(dataPoint.Id, transitionRequest);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("Cannot skip 'missing' state", errorMessage);
        }

        [Fact]
        public void TransitionGapStatus_SkipMissingToProvided_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Test Data Point",
                Content = "Test content",
                OwnerId = "user-1",
                Source = "Test Source",
                InformationType = "fact",
                CompletenessStatus = "incomplete"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // Act - Try to skip directly to provided without going through missing and estimated
            var transitionRequest = new TransitionGapStatusRequest
            {
                TransitionedBy = "user-1",
                TargetStatus = "provided"
            };

            var (isValid, errorMessage, updatedDataPoint) = store.TransitionGapStatus(dataPoint.Id, transitionRequest);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("Cannot skip 'estimated' state", errorMessage);
        }

        [Fact]
        public void TransitionGapStatus_SkipEstimatedToProvided_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "metric",
                Title = "Test Data Point",
                Content = "Test content",
                OwnerId = "user-1",
                Source = "Test Source",
                InformationType = "fact",
                CompletenessStatus = "incomplete"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // Transition to missing first
            store.TransitionGapStatus(dataPoint.Id, new TransitionGapStatusRequest
            {
                TransitionedBy = "user-1",
                TargetStatus = "missing"
            });

            // Act - Try to skip directly from missing to provided without going through estimated
            var transitionRequest = new TransitionGapStatusRequest
            {
                TransitionedBy = "user-1",
                TargetStatus = "provided"
            };

            var (isValid, errorMessage, updatedDataPoint) = store.TransitionGapStatus(dataPoint.Id, transitionRequest);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("Cannot skip 'estimated' state", errorMessage);
        }
    }
}
