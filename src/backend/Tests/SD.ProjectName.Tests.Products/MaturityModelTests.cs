using ARP.ESG_ReportStudio.API.Reporting;
using ARP.ESG_ReportStudio.API.Services;
using Xunit;

namespace SD.ProjectName.Tests.Products
{
    public class MaturityModelTests
    {
        private static InMemoryReportStore CreateStore()
        {
            return new InMemoryReportStore(new TextDiffService());
        }

        [Fact]
        public void CreateMaturityModel_WithValidData_ShouldSucceed()
        {
            // Arrange
            var store = CreateStore();
            var request = new CreateMaturityModelRequest
            {
                Name = "ESG Reporting Maturity Framework",
                Description = "A framework to measure ESG reporting maturity",
                CreatedBy = "admin-user",
                CreatedByName = "Admin User",
                Levels = new List<MaturityLevelRequest>
                {
                    new MaturityLevelRequest
                    {
                        Name = "Initial",
                        Description = "Ad-hoc reporting with minimal structure",
                        Order = 1,
                        Criteria = new List<MaturityCriterionRequest>
                        {
                            new MaturityCriterionRequest
                            {
                                Name = "Basic data collection",
                                Description = "At least 30% of KPIs have data",
                                CriterionType = "data-completeness",
                                TargetValue = "30",
                                Unit = "%",
                                MinCompletionPercentage = 30,
                                IsMandatory = true
                            }
                        }
                    },
                    new MaturityLevelRequest
                    {
                        Name = "Repeatable",
                        Description = "Consistent reporting with documented processes",
                        Order = 2,
                        Criteria = new List<MaturityCriterionRequest>
                        {
                            new MaturityCriterionRequest
                            {
                                Name = "Good data coverage",
                                Description = "At least 60% of KPIs have data",
                                CriterionType = "data-completeness",
                                TargetValue = "60",
                                Unit = "%",
                                MinCompletionPercentage = 60,
                                IsMandatory = true
                            },
                            new MaturityCriterionRequest
                            {
                                Name = "Evidence documentation",
                                Description = "At least 40% of KPIs have evidence",
                                CriterionType = "evidence-quality",
                                TargetValue = "40",
                                Unit = "%",
                                MinEvidencePercentage = 40,
                                IsMandatory = true
                            }
                        }
                    }
                }
            };

            // Act
            var (isValid, errorMessage, model) = store.CreateMaturityModel(request);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(model);
            Assert.Equal("ESG Reporting Maturity Framework", model.Name);
            Assert.Equal(1, model.Version);
            Assert.True(model.IsActive);
            Assert.Equal(2, model.Levels.Count);
            Assert.Equal("Initial", model.Levels[0].Name);
            Assert.Equal("Repeatable", model.Levels[1].Name);
            Assert.Single(model.Levels[0].Criteria);
            Assert.Equal(2, model.Levels[1].Criteria.Count);
        }

        [Fact]
        public void CreateMaturityModel_WithoutName_ShouldFail()
        {
            // Arrange
            var store = CreateStore();
            var request = new CreateMaturityModelRequest
            {
                Name = "",
                Description = "Test",
                CreatedBy = "admin-user",
                CreatedByName = "Admin User",
                Levels = new List<MaturityLevelRequest>()
            };

            // Act
            var (isValid, errorMessage, model) = store.CreateMaturityModel(request);

            // Assert
            Assert.False(isValid);
            Assert.Equal("Name is required.", errorMessage);
            Assert.Null(model);
        }

        [Fact]
        public void CreateMaturityModel_WithoutLevels_ShouldFail()
        {
            // Arrange
            var store = CreateStore();
            var request = new CreateMaturityModelRequest
            {
                Name = "Test Model",
                Description = "Test",
                CreatedBy = "admin-user",
                CreatedByName = "Admin User",
                Levels = new List<MaturityLevelRequest>()
            };

            // Act
            var (isValid, errorMessage, model) = store.CreateMaturityModel(request);

            // Assert
            Assert.False(isValid);
            Assert.Equal("At least one maturity level is required.", errorMessage);
            Assert.Null(model);
        }

        [Fact]
        public void CreateMaturityModel_WithDuplicateOrders_ShouldFail()
        {
            // Arrange
            var store = CreateStore();
            var request = new CreateMaturityModelRequest
            {
                Name = "Test Model",
                Description = "Test",
                CreatedBy = "admin-user",
                CreatedByName = "Admin User",
                Levels = new List<MaturityLevelRequest>
                {
                    new MaturityLevelRequest { Name = "Level 1", Description = "Test", Order = 1, Criteria = new List<MaturityCriterionRequest>() },
                    new MaturityLevelRequest { Name = "Level 2", Description = "Test", Order = 1, Criteria = new List<MaturityCriterionRequest>() }
                }
            };

            // Act
            var (isValid, errorMessage, model) = store.CreateMaturityModel(request);

            // Assert
            Assert.False(isValid);
            Assert.Equal("Maturity level orders must be unique.", errorMessage);
            Assert.Null(model);
        }

        [Fact]
        public void UpdateMaturityModel_CreatesNewVersion()
        {
            // Arrange
            var store = CreateStore();
            var createRequest = new CreateMaturityModelRequest
            {
                Name = "Original Model",
                Description = "Original description",
                CreatedBy = "admin-user",
                CreatedByName = "Admin User",
                Levels = new List<MaturityLevelRequest>
                {
                    new MaturityLevelRequest
                    {
                        Name = "Initial",
                        Description = "Level 1",
                        Order = 1,
                        Criteria = new List<MaturityCriterionRequest>()
                    }
                }
            };

            var (_, __, createdModel) = store.CreateMaturityModel(createRequest);

            var updateRequest = new UpdateMaturityModelRequest
            {
                Name = "Updated Model",
                Description = "Updated description",
                UpdatedBy = "admin-user",
                UpdatedByName = "Admin User",
                Levels = new List<MaturityLevelRequest>
                {
                    new MaturityLevelRequest
                    {
                        Name = "Initial",
                        Description = "Level 1",
                        Order = 1,
                        Criteria = new List<MaturityCriterionRequest>()
                    },
                    new MaturityLevelRequest
                    {
                        Name = "Repeatable",
                        Description = "Level 2",
                        Order = 2,
                        Criteria = new List<MaturityCriterionRequest>()
                    }
                }
            };

            // Act
            var (isValid, errorMessage, updatedModel) = store.UpdateMaturityModel(createdModel!.Id, updateRequest);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(updatedModel);
            Assert.Equal("Updated Model", updatedModel.Name);
            Assert.Equal(2, updatedModel.Version);
            Assert.True(updatedModel.IsActive);
            Assert.Equal(2, updatedModel.Levels.Count);

            // Verify version history
            var versions = store.GetMaturityModelVersionHistory(createdModel.Id);
            Assert.Equal(2, versions.Count);
            Assert.Equal(2, versions[0].Version); // Latest version first
            Assert.Equal(1, versions[1].Version);
            Assert.True(versions[0].IsActive);
            Assert.False(versions[1].IsActive);
        }

        [Fact]
        public void GetActiveMaturityModel_ReturnsActiveModel()
        {
            // Arrange
            var store = CreateStore();
            var request = new CreateMaturityModelRequest
            {
                Name = "Active Model",
                Description = "Test",
                CreatedBy = "admin-user",
                CreatedByName = "Admin User",
                Levels = new List<MaturityLevelRequest>
                {
                    new MaturityLevelRequest
                    {
                        Name = "Initial",
                        Description = "Test",
                        Order = 1,
                        Criteria = new List<MaturityCriterionRequest>()
                    }
                }
            };

            store.CreateMaturityModel(request);

            // Act
            var activeModel = store.GetActiveMaturityModel();

            // Assert
            Assert.NotNull(activeModel);
            Assert.Equal("Active Model", activeModel.Name);
            Assert.True(activeModel.IsActive);
        }

        [Fact]
        public void GetMaturityModels_IncludeInactive_ReturnsAllVersions()
        {
            // Arrange
            var store = CreateStore();
            var createRequest = new CreateMaturityModelRequest
            {
                Name = "Model",
                Description = "Test",
                CreatedBy = "admin-user",
                CreatedByName = "Admin User",
                Levels = new List<MaturityLevelRequest>
                {
                    new MaturityLevelRequest { Name = "Initial", Description = "Test", Order = 1, Criteria = new List<MaturityCriterionRequest>() }
                }
            };

            var (_, __, createdModel) = store.CreateMaturityModel(createRequest);

            var updateRequest = new UpdateMaturityModelRequest
            {
                Name = "Model Updated",
                Description = "Test",
                UpdatedBy = "admin-user",
                UpdatedByName = "Admin User",
                Levels = new List<MaturityLevelRequest>
                {
                    new MaturityLevelRequest { Name = "Initial", Description = "Test", Order = 1, Criteria = new List<MaturityCriterionRequest>() }
                }
            };

            store.UpdateMaturityModel(createdModel!.Id, updateRequest);

            // Act
            var allModels = store.GetMaturityModels(includeInactive: true);
            var activeOnly = store.GetMaturityModels(includeInactive: false);

            // Assert
            Assert.Equal(2, allModels.Count);
            Assert.Single(activeOnly);
            Assert.True(activeOnly[0].IsActive);
        }

        [Fact]
        public void DeleteMaturityModel_RemovesAllVersions()
        {
            // Arrange
            var store = CreateStore();
            var createRequest = new CreateMaturityModelRequest
            {
                Name = "Model to Delete",
                Description = "Test",
                CreatedBy = "admin-user",
                CreatedByName = "Admin User",
                Levels = new List<MaturityLevelRequest>
                {
                    new MaturityLevelRequest { Name = "Initial", Description = "Test", Order = 1, Criteria = new List<MaturityCriterionRequest>() }
                }
            };

            var (_, __, createdModel) = store.CreateMaturityModel(createRequest);

            // Act
            var (isValid, errorMessage) = store.DeleteMaturityModel(createdModel!.Id);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);

            var models = store.GetMaturityModels(includeInactive: true);
            Assert.Empty(models);
        }

        [Fact]
        public void MaturityModel_SupportsMultipleCriterionTypes()
        {
            // Arrange
            var store = CreateStore();
            var request = new CreateMaturityModelRequest
            {
                Name = "Comprehensive Model",
                Description = "Test all criterion types",
                CreatedBy = "admin-user",
                CreatedByName = "Admin User",
                Levels = new List<MaturityLevelRequest>
                {
                    new MaturityLevelRequest
                    {
                        Name = "Managed",
                        Description = "Advanced maturity level",
                        Order = 3,
                        Criteria = new List<MaturityCriterionRequest>
                        {
                            new MaturityCriterionRequest
                            {
                                Name = "High data completeness",
                                Description = "80% of KPIs have data",
                                CriterionType = "data-completeness",
                                TargetValue = "80",
                                Unit = "%",
                                MinCompletionPercentage = 80,
                                IsMandatory = true
                            },
                            new MaturityCriterionRequest
                            {
                                Name = "Strong evidence coverage",
                                Description = "70% of KPIs have evidence",
                                CriterionType = "evidence-quality",
                                TargetValue = "70",
                                Unit = "%",
                                MinEvidencePercentage = 70,
                                IsMandatory = true
                            },
                            new MaturityCriterionRequest
                            {
                                Name = "Process controls",
                                Description = "Required controls in place",
                                CriterionType = "process-control",
                                TargetValue = "All",
                                Unit = "controls",
                                RequiredControls = new List<string> { "approval-workflow", "dual-validation", "audit-trail" },
                                IsMandatory = true
                            },
                            new MaturityCriterionRequest
                            {
                                Name = "Custom metric",
                                Description = "Organization-specific requirement",
                                CriterionType = "custom",
                                TargetValue = "Yes",
                                Unit = "yes/no",
                                IsMandatory = false
                            }
                        }
                    }
                }
            };

            // Act
            var (isValid, errorMessage, model) = store.CreateMaturityModel(request);

            // Assert
            Assert.True(isValid);
            Assert.NotNull(model);
            Assert.Single(model.Levels);
            Assert.Equal(4, model.Levels[0].Criteria.Count);

            var criteria = model.Levels[0].Criteria;
            Assert.Contains(criteria, c => c.CriterionType == "data-completeness");
            Assert.Contains(criteria, c => c.CriterionType == "evidence-quality");
            Assert.Contains(criteria, c => c.CriterionType == "process-control");
            Assert.Contains(criteria, c => c.CriterionType == "custom");

            var processControl = criteria.First(c => c.CriterionType == "process-control");
            Assert.Equal(3, processControl.RequiredControls.Count);
            Assert.Contains("approval-workflow", processControl.RequiredControls);
        }
    }
}
