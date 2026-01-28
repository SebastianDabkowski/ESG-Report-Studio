using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products
{
    public class AssumptionProvenanceTests
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
                Description = "Test section for assumptions",
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
        public void CreateAssumption_WithProvenance_ShouldStoreAllProvenanceFields()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var sources = new List<AssumptionSource>
            {
                new AssumptionSource
                {
                    SourceType = "internal-document",
                    SourceReference = "POLICY-2024-001",
                    Description = "Company sustainability policy document"
                },
                new AssumptionSource
                {
                    SourceType = "external-url",
                    SourceReference = "https://example.com/industry-standards",
                    Description = "Industry best practices guide"
                }
            };

            // Act
            var (isValid, errorMessage, assumption) = store.CreateAssumption(
                sectionId: sectionId,
                title: "Renewable Energy Target",
                description: "Assumption about renewable energy procurement",
                scope: "Company-wide",
                validityStartDate: "2024-01-01",
                validityEndDate: "2024-12-31",
                methodology: "Based on regulatory requirements and company policy",
                limitations: "Limited to direct energy procurement, excludes indirect sources",
                linkedDataPointIds: new List<string>(),
                rationale: "The assumption is made based on our commitment to achieve net-zero emissions by 2030. Industry standards suggest that renewable energy procurement is a key component of this strategy.",
                sources: sources,
                createdBy: "analyst-1"
            );

            // Assert
            Assert.True(isValid, errorMessage ?? "Should succeed");
            Assert.NotNull(assumption);
            
            // Verify core fields
            Assert.Equal("Renewable Energy Target", assumption.Title);
            Assert.Equal("Company-wide", assumption.Scope);
            Assert.Equal("active", assumption.Status);
            Assert.Equal(1, assumption.Version);
            
            // Verify provenance fields
            Assert.Equal("The assumption is made based on our commitment to achieve net-zero emissions by 2030. Industry standards suggest that renewable energy procurement is a key component of this strategy.", assumption.Rationale);
            Assert.Equal(2, assumption.Sources.Count);
            
            Assert.Equal("internal-document", assumption.Sources[0].SourceType);
            Assert.Equal("POLICY-2024-001", assumption.Sources[0].SourceReference);
            Assert.Equal("Company sustainability policy document", assumption.Sources[0].Description);
            
            Assert.Equal("external-url", assumption.Sources[1].SourceType);
            Assert.Equal("https://example.com/industry-standards", assumption.Sources[1].SourceReference);
            
            // Verify audit fields
            Assert.Equal("analyst-1", assumption.CreatedBy);
            Assert.NotNull(assumption.CreatedAt);
            Assert.NotEmpty(assumption.CreatedAt);
        }

        [Fact]
        public void UpdateAssumption_WithProvenance_ShouldUpdateProvenanceFields()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            // Create initial assumption
            var (createValid, createError, createdAssumption) = store.CreateAssumption(
                sectionId: sectionId,
                title: "Initial Assumption",
                description: "Initial description",
                scope: "Facility A",
                validityStartDate: "2024-01-01",
                validityEndDate: "2024-12-31",
                methodology: "Initial methodology",
                limitations: "Initial limitations",
                linkedDataPointIds: new List<string>(),
                rationale: "Initial rationale",
                sources: new List<AssumptionSource>(),
                createdBy: "analyst-1"
            );

            Assert.True(createValid);
            Assert.NotNull(createdAssumption);

            // Update with new provenance data
            var newSources = new List<AssumptionSource>
            {
                new AssumptionSource
                {
                    SourceType = "uploaded-evidence",
                    SourceReference = "EVIDENCE-456",
                    Description = "Updated scientific study"
                }
            };

            // Act
            var (updateValid, updateError, updatedAssumption) = store.UpdateAssumption(
                id: createdAssumption.Id,
                title: "Updated Assumption",
                description: "Updated description",
                scope: "Company-wide",
                validityStartDate: "2024-01-01",
                validityEndDate: "2024-12-31",
                methodology: "Updated methodology with better data",
                limitations: "Updated limitations",
                linkedDataPointIds: new List<string>(),
                rationale: "Updated rationale with more comprehensive analysis and recent industry developments",
                sources: newSources,
                updatedBy: "analyst-2"
            );

            // Assert
            Assert.True(updateValid, updateError ?? "Should succeed");
            Assert.NotNull(updatedAssumption);
            
            // Verify provenance fields are updated
            Assert.Equal("Updated rationale with more comprehensive analysis and recent industry developments", updatedAssumption.Rationale);
            Assert.Single(updatedAssumption.Sources);
            Assert.Equal("uploaded-evidence", updatedAssumption.Sources[0].SourceType);
            Assert.Equal("EVIDENCE-456", updatedAssumption.Sources[0].SourceReference);
            
            // Verify version incremented
            Assert.Equal(2, updatedAssumption.Version);
            
            // Verify audit trail
            Assert.Equal("analyst-1", updatedAssumption.CreatedBy);
            Assert.Equal("analyst-2", updatedAssumption.UpdatedBy);
            Assert.NotNull(updatedAssumption.UpdatedAt);
        }

        [Fact]
        public void CreateAssumption_WithoutRationale_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            // Act - Rationale is optional
            var (isValid, errorMessage, assumption) = store.CreateAssumption(
                sectionId: sectionId,
                title: "Test Assumption",
                description: "Test description",
                scope: "Test scope",
                validityStartDate: "2024-01-01",
                validityEndDate: "2024-12-31",
                methodology: "Test methodology",
                limitations: "Test limitations",
                linkedDataPointIds: new List<string>(),
                rationale: null,
                sources: new List<AssumptionSource>(),
                createdBy: "test-user"
            );

            // Assert
            Assert.True(isValid, errorMessage ?? "Should succeed");
            Assert.NotNull(assumption);
            Assert.Null(assumption.Rationale);
            Assert.Empty(assumption.Sources);
        }
    }
}
