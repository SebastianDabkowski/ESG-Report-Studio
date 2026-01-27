using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products
{
    public class OrganizationCoverageTests
    {
        [Fact]
        public void CreateOrganization_WithFullCoverage_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var request = new CreateOrganizationRequest
            {
                Name = "Test Company",
                LegalForm = "GmbH",
                Country = "Germany",
                Identifier = "DE123456789",
                CreatedBy = "test-user",
                CoverageType = "full"
            };

            // Act
            var organization = store.CreateOrganization(request);

            // Assert
            Assert.NotNull(organization);
            Assert.Equal("full", organization.CoverageType);
            Assert.Null(organization.CoverageJustification);
        }

        [Fact]
        public void CreateOrganization_WithLimitedCoverageAndJustification_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var request = new CreateOrganizationRequest
            {
                Name = "Test Company",
                LegalForm = "GmbH",
                Country = "Germany",
                Identifier = "DE123456789",
                CreatedBy = "test-user",
                CoverageType = "limited",
                CoverageJustification = "Only reporting for main production facility due to data availability constraints"
            };

            // Act
            var organization = store.CreateOrganization(request);

            // Assert
            Assert.NotNull(organization);
            Assert.Equal("limited", organization.CoverageType);
            Assert.Equal("Only reporting for main production facility due to data availability constraints", organization.CoverageJustification);
        }

        [Fact]
        public void CreateOrganization_WithDefaultCoverage_ShouldBeFull()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var request = new CreateOrganizationRequest
            {
                Name = "Test Company",
                LegalForm = "GmbH",
                Country = "Germany",
                Identifier = "DE123456789",
                CreatedBy = "test-user"
            };

            // Act
            var organization = store.CreateOrganization(request);

            // Assert
            Assert.NotNull(organization);
            Assert.Equal("full", organization.CoverageType);
        }

        [Fact]
        public void UpdateOrganization_WithLimitedCoverageAndJustification_ShouldSucceed()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var createRequest = new CreateOrganizationRequest
            {
                Name = "Test Company",
                LegalForm = "GmbH",
                Country = "Germany",
                Identifier = "DE123456789",
                CreatedBy = "test-user",
                CoverageType = "full"
            };
            var created = store.CreateOrganization(createRequest);

            var updateRequest = new UpdateOrganizationRequest
            {
                Name = "Test Company",
                LegalForm = "GmbH",
                Country = "Germany",
                Identifier = "DE123456789",
                CoverageType = "limited",
                CoverageJustification = "Excluding satellite offices that lack data collection systems"
            };

            // Act
            var updated = store.UpdateOrganization(created.Id, updateRequest);

            // Assert
            Assert.NotNull(updated);
            Assert.Equal("limited", updated.CoverageType);
            Assert.Equal("Excluding satellite offices that lack data collection systems", updated.CoverageJustification);
        }

        [Fact]
        public void UpdateOrganization_FromLimitedToFull_ShouldClearJustification()
        {
            // Arrange
            var store = new InMemoryReportStore();
            var createRequest = new CreateOrganizationRequest
            {
                Name = "Test Company",
                LegalForm = "GmbH",
                Country = "Germany",
                Identifier = "DE123456789",
                CreatedBy = "test-user",
                CoverageType = "limited",
                CoverageJustification = "Initial limited coverage"
            };
            var created = store.CreateOrganization(createRequest);

            var updateRequest = new UpdateOrganizationRequest
            {
                Name = "Test Company",
                LegalForm = "GmbH",
                Country = "Germany",
                Identifier = "DE123456789",
                CoverageType = "full",
                CoverageJustification = null
            };

            // Act
            var updated = store.UpdateOrganization(created.Id, updateRequest);

            // Assert
            Assert.NotNull(updated);
            Assert.Equal("full", updated.CoverageType);
            Assert.Null(updated.CoverageJustification);
        }

        [Fact]
        public void CreateOrganization_WithInvalidCoverageType_ShouldStillCreateInStore()
        {
            // Arrange
            var store = new InMemoryReportStore();
            
            // Note: The controller validates this, but the store itself accepts any value
            // This test verifies store behavior - controller tests would verify the validation
            var request = new CreateOrganizationRequest
            {
                Name = "Test Company",
                LegalForm = "GmbH",
                Country = "Germany",
                Identifier = "DE123456789",
                CreatedBy = "test-user",
                CoverageType = "invalid-type"
            };

            // Act
            var organization = store.CreateOrganization(request);

            // Assert
            Assert.NotNull(organization);
            Assert.Equal("invalid-type", organization.CoverageType);
        }
    }
}
