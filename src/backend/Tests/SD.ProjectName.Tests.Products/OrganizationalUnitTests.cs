using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products
{
    public class OrganizationalUnitTests
    {
        [Fact]
        public void CreateOrganizationalUnit_WithValidData_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var request = new CreateOrganizationalUnitRequest
            {
                Name = "Engineering Department",
                Description = "Software engineering team",
                CreatedBy = "user1"
            };

            // Act
            var unit = store.CreateOrganizationalUnit(request);

            // Assert
            Assert.NotNull(unit);
            Assert.NotEmpty(unit.Id);
            Assert.Equal("Engineering Department", unit.Name);
            Assert.Equal("Software engineering team", unit.Description);
            Assert.Equal("user1", unit.CreatedBy);
            Assert.Null(unit.ParentId);
        }

        [Fact]
        public void CreateOrganizationalUnit_WithParent_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            
            // Create parent
            var parentRequest = new CreateOrganizationalUnitRequest
            {
                Name = "Technology",
                Description = "Technology division",
                CreatedBy = "user1"
            };
            var parent = store.CreateOrganizationalUnit(parentRequest);

            // Create child
            var childRequest = new CreateOrganizationalUnitRequest
            {
                Name = "Engineering",
                ParentId = parent.Id,
                Description = "Engineering department",
                CreatedBy = "user1"
            };

            // Act
            var child = store.CreateOrganizationalUnit(childRequest);

            // Assert
            Assert.NotNull(child);
            Assert.Equal(parent.Id, child.ParentId);
        }

        [Fact]
        public void CreateOrganizationalUnit_WithInvalidParent_ShouldThrow()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var request = new CreateOrganizationalUnitRequest
            {
                Name = "Engineering",
                ParentId = "non-existent-id",
                Description = "Engineering department",
                CreatedBy = "user1"
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                store.CreateOrganizationalUnit(request));
            Assert.Contains("Parent unit", exception.Message);
        }

        [Fact]
        public void UpdateOrganizationalUnit_WithValidData_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var createRequest = new CreateOrganizationalUnitRequest
            {
                Name = "Engineering",
                Description = "Old description",
                CreatedBy = "user1"
            };
            var created = store.CreateOrganizationalUnit(createRequest);

            var updateRequest = new UpdateOrganizationalUnitRequest
            {
                Name = "Engineering Department",
                Description = "New description"
            };

            // Act
            var updated = store.UpdateOrganizationalUnit(created.Id, updateRequest);

            // Assert
            Assert.NotNull(updated);
            Assert.Equal("Engineering Department", updated.Name);
            Assert.Equal("New description", updated.Description);
        }

        [Fact]
        public void UpdateOrganizationalUnit_WithSelfAsParent_ShouldThrow()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var createRequest = new CreateOrganizationalUnitRequest
            {
                Name = "Engineering",
                Description = "Description",
                CreatedBy = "user1"
            };
            var created = store.CreateOrganizationalUnit(createRequest);

            var updateRequest = new UpdateOrganizationalUnitRequest
            {
                Name = "Engineering",
                ParentId = created.Id,
                Description = "Description"
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                store.UpdateOrganizationalUnit(created.Id, updateRequest));
            Assert.Contains("cannot be its own parent", exception.Message);
        }

        [Fact]
        public void UpdateOrganizationalUnit_WithCircularReference_ShouldThrow()
        {
            // Arrange
            var store = new InMemoryReportStore();
            
            // Create parent
            var parentRequest = new CreateOrganizationalUnitRequest
            {
                Name = "Parent",
                Description = "Parent unit",
                CreatedBy = "user1"
            };
            var parent = store.CreateOrganizationalUnit(parentRequest);

            // Create child
            var childRequest = new CreateOrganizationalUnitRequest
            {
                Name = "Child",
                ParentId = parent.Id,
                Description = "Child unit",
                CreatedBy = "user1"
            };
            var child = store.CreateOrganizationalUnit(childRequest);

            // Try to make parent a child of child (circular reference)
            var updateRequest = new UpdateOrganizationalUnitRequest
            {
                Name = "Parent",
                ParentId = child.Id,
                Description = "Parent unit"
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                store.UpdateOrganizationalUnit(parent.Id, updateRequest));
            Assert.Contains("circular reference", exception.Message);
        }

        [Fact]
        public void DeleteOrganizationalUnit_WithoutChildren_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var request = new CreateOrganizationalUnitRequest
            {
                Name = "Engineering",
                Description = "Description",
                CreatedBy = "user1"
            };
            var created = store.CreateOrganizationalUnit(request);

            // Act
            var deleted = store.DeleteOrganizationalUnit(created.Id);

            // Assert
            Assert.True(deleted);
            Assert.Empty(store.GetOrganizationalUnits());
        }

        [Fact]
        public void DeleteOrganizationalUnit_WithChildren_ShouldThrow()
        {
            // Arrange
            var store = new InMemoryReportStore();
            
            // Create parent
            var parentRequest = new CreateOrganizationalUnitRequest
            {
                Name = "Parent",
                Description = "Parent unit",
                CreatedBy = "user1"
            };
            var parent = store.CreateOrganizationalUnit(parentRequest);

            // Create child
            var childRequest = new CreateOrganizationalUnitRequest
            {
                Name = "Child",
                ParentId = parent.Id,
                Description = "Child unit",
                CreatedBy = "user1"
            };
            store.CreateOrganizationalUnit(childRequest);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                store.DeleteOrganizationalUnit(parent.Id));
            Assert.Contains("has child units", exception.Message);
        }

        [Fact]
        public void CreatePeriod_WithNoOrganizationalUnits_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var request = new CreateReportingPeriodRequest
            {
                Name = "FY 2024",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                Variant = "simplified",
                OwnerId = "user1",
                OwnerName = "Test User"
            };

            // Act
            var (isValid, errorMessage, snapshot) = store.ValidateAndCreatePeriod(request);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("Organizational structure must be defined", errorMessage);
            Assert.Null(snapshot);
        }

        [Fact]
        public void CreatePeriod_WithOrganizationalUnits_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            
            // Create organizational unit first
            var unitRequest = new CreateOrganizationalUnitRequest
            {
                Name = "Main Office",
                Description = "Main office location",
                CreatedBy = "user1"
            };
            store.CreateOrganizationalUnit(unitRequest);

            var periodRequest = new CreateReportingPeriodRequest
            {
                Name = "FY 2024",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                Variant = "simplified",
                OwnerId = "user1",
                OwnerName = "Test User"
            };

            // Act
            var (isValid, errorMessage, snapshot) = store.ValidateAndCreatePeriod(periodRequest);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(snapshot);
            Assert.Single(snapshot.Periods);
        }

        [Fact]
        public void GetOrganizationalUnits_ShouldReturnAllUnits()
        {
            // Arrange
            var store = new InMemoryReportStore();
            
            store.CreateOrganizationalUnit(new CreateOrganizationalUnitRequest
            {
                Name = "Unit 1",
                Description = "First unit",
                CreatedBy = "user1"
            });

            store.CreateOrganizationalUnit(new CreateOrganizationalUnitRequest
            {
                Name = "Unit 2",
                Description = "Second unit",
                CreatedBy = "user1"
            });

            // Act
            var units = store.GetOrganizationalUnits();

            // Assert
            Assert.Equal(2, units.Count);
        }

        [Fact]
        public void GetSnapshot_ShouldIncludeOrganizationalUnits()
        {
            // Arrange
            var store = new InMemoryReportStore();
            
            store.CreateOrganizationalUnit(new CreateOrganizationalUnitRequest
            {
                Name = "Unit 1",
                Description = "First unit",
                CreatedBy = "user1"
            });

            // Act
            var snapshot = store.GetSnapshot();

            // Assert
            Assert.Single(snapshot.OrganizationalUnits);
            Assert.Equal("Unit 1", snapshot.OrganizationalUnits[0].Name);
        }
    }
}
