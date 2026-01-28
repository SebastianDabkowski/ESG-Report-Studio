using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products
{
    public class SectionCatalogTests
    {
        [Fact]
        public void GetSectionCatalog_ShouldReturnDefaultSections()
        {
            // Arrange
            var store = new InMemoryReportStore();

            // Act
            var catalog = store.GetSectionCatalog();

            // Assert
            Assert.NotEmpty(catalog);
            Assert.All(catalog, item => Assert.False(item.IsDeprecated));
        }

        [Fact]
        public void GetSectionCatalog_WithIncludeDeprecated_ShouldReturnAllSections()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var firstItem = store.GetSectionCatalog().First();
            store.DeprecateSectionCatalogItem(firstItem.Id);

            // Act
            var catalogWithoutDeprecated = store.GetSectionCatalog(includeDeprecated: false);
            var catalogWithDeprecated = store.GetSectionCatalog(includeDeprecated: true);

            // Assert
            Assert.Equal(catalogWithDeprecated.Count - 1, catalogWithoutDeprecated.Count);
        }

        [Fact]
        public void CreateSectionCatalogItem_WithValidData_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var request = new CreateSectionCatalogItemRequest
            {
                Title = "New Section",
                Code = "NEW-001",
                Category = "environmental",
                Description = "A new test section"
            };

            // Act
            var (isValid, errorMessage, item) = store.CreateSectionCatalogItem(request);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(item);
            Assert.Equal(request.Title, item.Title);
            Assert.Equal(request.Code, item.Code);
            Assert.False(item.IsDeprecated);
        }

        [Fact]
        public void CreateSectionCatalogItem_WithDuplicateCode_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var request1 = new CreateSectionCatalogItemRequest
            {
                Title = "Section 1",
                Code = "TEST-001",
                Category = "environmental",
                Description = "Test section"
            };
            var (isValid1, _, _) = store.CreateSectionCatalogItem(request1);
            Assert.True(isValid1);

            var request2 = new CreateSectionCatalogItemRequest
            {
                Title = "Section 2",
                Code = "TEST-001",
                Category = "social",
                Description = "Another test section"
            };

            // Act
            var (isValid, errorMessage, item) = store.CreateSectionCatalogItem(request2);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("already exists", errorMessage);
            Assert.Null(item);
        }

        [Fact]
        public void CreateSectionCatalogItem_WithoutTitle_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var request = new CreateSectionCatalogItemRequest
            {
                Title = "",
                Code = "TEST-001",
                Category = "environmental",
                Description = "Test section"
            };

            // Act
            var (isValid, errorMessage, item) = store.CreateSectionCatalogItem(request);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("Title is required", errorMessage);
            Assert.Null(item);
        }

        [Fact]
        public void CreateSectionCatalogItem_WithoutCode_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var request = new CreateSectionCatalogItemRequest
            {
                Title = "Test Section",
                Code = "",
                Category = "environmental",
                Description = "Test section"
            };

            // Act
            var (isValid, errorMessage, item) = store.CreateSectionCatalogItem(request);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("Code is required", errorMessage);
            Assert.Null(item);
        }

        [Fact]
        public void UpdateSectionCatalogItem_WithValidData_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var createRequest = new CreateSectionCatalogItemRequest
            {
                Title = "Original Title",
                Code = "TEST-001",
                Category = "environmental",
                Description = "Original description"
            };
            var (_, _, createdItem) = store.CreateSectionCatalogItem(createRequest);

            var updateRequest = new UpdateSectionCatalogItemRequest
            {
                Title = "Updated Title",
                Code = "TEST-001",
                Category = "social",
                Description = "Updated description"
            };

            // Act
            var (isValid, errorMessage, item) = store.UpdateSectionCatalogItem(createdItem!.Id, updateRequest);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(item);
            Assert.Equal("Updated Title", item.Title);
            Assert.Equal("social", item.Category);
            Assert.Equal("Updated description", item.Description);
        }

        [Fact]
        public void UpdateSectionCatalogItem_WithNonExistentId_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var updateRequest = new UpdateSectionCatalogItemRequest
            {
                Title = "Updated Title",
                Code = "TEST-001",
                Category = "environmental",
                Description = "Updated description"
            };

            // Act
            var (isValid, errorMessage, item) = store.UpdateSectionCatalogItem("non-existent-id", updateRequest);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("not found", errorMessage);
            Assert.Null(item);
        }

        [Fact]
        public void UpdateSectionCatalogItem_WithDuplicateCode_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            
            // Create first item
            var request1 = new CreateSectionCatalogItemRequest
            {
                Title = "Section 1",
                Code = "TEST-001",
                Category = "environmental",
                Description = "Test section 1"
            };
            store.CreateSectionCatalogItem(request1);

            // Create second item
            var request2 = new CreateSectionCatalogItemRequest
            {
                Title = "Section 2",
                Code = "TEST-002",
                Category = "environmental",
                Description = "Test section 2"
            };
            var (_, _, item2) = store.CreateSectionCatalogItem(request2);

            // Try to update second item with code from first item
            var updateRequest = new UpdateSectionCatalogItemRequest
            {
                Title = "Section 2 Updated",
                Code = "TEST-001", // Duplicate code
                Category = "environmental",
                Description = "Updated description"
            };

            // Act
            var (isValid, errorMessage, updatedItem) = store.UpdateSectionCatalogItem(item2!.Id, updateRequest);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("already exists", errorMessage);
            Assert.Null(updatedItem);
        }

        [Fact]
        public void DeprecateSectionCatalogItem_WithValidId_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var createRequest = new CreateSectionCatalogItemRequest
            {
                Title = "Test Section",
                Code = "TEST-001",
                Category = "environmental",
                Description = "Test section"
            };
            var (_, _, createdItem) = store.CreateSectionCatalogItem(createRequest);

            // Act
            var (isValid, errorMessage) = store.DeprecateSectionCatalogItem(createdItem!.Id);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);

            var item = store.GetSectionCatalogItem(createdItem.Id);
            Assert.True(item!.IsDeprecated);
            Assert.NotNull(item.DeprecatedAt);
        }

        [Fact]
        public void DeprecateSectionCatalogItem_WithNonExistentId_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();

            // Act
            var (isValid, errorMessage) = store.DeprecateSectionCatalogItem("non-existent-id");

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("not found", errorMessage);
        }

        [Fact]
        public void DeprecateSectionCatalogItem_WhenAlreadyDeprecated_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var createRequest = new CreateSectionCatalogItemRequest
            {
                Title = "Test Section",
                Code = "TEST-001",
                Category = "environmental",
                Description = "Test section"
            };
            var (_, _, createdItem) = store.CreateSectionCatalogItem(createRequest);
            store.DeprecateSectionCatalogItem(createdItem!.Id);

            // Act
            var (isValid, errorMessage) = store.DeprecateSectionCatalogItem(createdItem.Id);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("already deprecated", errorMessage);
        }

        [Fact]
        public void CreatePeriod_ShouldNotIncludeDeprecatedSections()
        {
            // Arrange
            var store = new InMemoryReportStore();
            
            // Create organization and organizational unit
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

            store.CreateOrganizationalUnit(new CreateOrganizationalUnitRequest
            {
                Name = "Test Unit",
                Description = "Test unit",
                CreatedBy = "test-user"
            });

            // Deprecate one section from the catalog
            var catalog = store.GetSectionCatalog();
            var firstSection = catalog.First();
            store.DeprecateSectionCatalogItem(firstSection.Id);

            // Act
            var request = new CreateReportingPeriodRequest
            {
                Name = "FY 2024",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                OwnerId = "user1",
                OwnerName = "Test User"
            };

            var (isValid, errorMessage, snapshot) = store.ValidateAndCreatePeriod(request);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(snapshot);
            
            // Check that deprecated section is not included
            var periodSections = snapshot.Sections.Where(s => s.PeriodId == snapshot.Periods.First().Id).ToList();
            Assert.DoesNotContain(periodSections, s => s.Title == firstSection.Title);
        }
    }
}
