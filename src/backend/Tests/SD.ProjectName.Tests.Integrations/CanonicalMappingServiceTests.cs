using Moq;
using SD.ProjectName.Modules.Integrations.Application;
using SD.ProjectName.Modules.Integrations.Domain.Entities;
using SD.ProjectName.Modules.Integrations.Domain.Interfaces;

namespace SD.ProjectName.Tests.Integrations;

public class CanonicalMappingServiceTests
{
    [Fact]
    public async Task CreateSchemaVersionAsync_ShouldCreateNewVersion()
    {
        // Arrange
        var mockVersionRepository = new Mock<ICanonicalEntityVersionRepository>();
        mockVersionRepository
            .Setup(r => r.GetVersionAsync(It.IsAny<CanonicalEntityType>(), It.IsAny<int>()))
            .ReturnsAsync((CanonicalEntityVersion?)null);
        mockVersionRepository
            .Setup(r => r.CreateAsync(It.IsAny<CanonicalEntityVersion>()))
            .ReturnsAsync((CanonicalEntityVersion v) => v);

        var service = new CanonicalMappingService(
            Mock.Of<ICanonicalEntityRepository>(),
            mockVersionRepository.Object,
            Mock.Of<ICanonicalAttributeRepository>(),
            Mock.Of<ICanonicalMappingRepository>());

        // Act
        var version = await service.CreateSchemaVersionAsync(
            CanonicalEntityType.Employee,
            1,
            "{\"type\":\"object\"}",
            "Initial version",
            "admin");

        // Assert
        Assert.Equal(CanonicalEntityType.Employee, version.EntityType);
        Assert.Equal(1, version.Version);
        Assert.Equal("{\"type\":\"object\"}", version.SchemaDefinition);
        Assert.Equal("Initial version", version.Description);
        Assert.True(version.IsActive);
        Assert.False(version.IsDeprecated);
        Assert.Equal("admin", version.CreatedBy);
    }

    [Fact]
    public async Task CreateSchemaVersionAsync_ShouldThrowIfVersionExists()
    {
        // Arrange
        var existingVersion = new CanonicalEntityVersion
        {
            Id = 1,
            EntityType = CanonicalEntityType.Employee,
            Version = 1
        };

        var mockVersionRepository = new Mock<ICanonicalEntityVersionRepository>();
        mockVersionRepository
            .Setup(r => r.GetVersionAsync(CanonicalEntityType.Employee, 1))
            .ReturnsAsync(existingVersion);

        var service = new CanonicalMappingService(
            Mock.Of<ICanonicalEntityRepository>(),
            mockVersionRepository.Object,
            Mock.Of<ICanonicalAttributeRepository>(),
            Mock.Of<ICanonicalMappingRepository>());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateSchemaVersionAsync(
                CanonicalEntityType.Employee,
                1,
                "{\"type\":\"object\"}",
                "Duplicate version",
                "admin"));
    }

    [Fact]
    public async Task CreateMappingAsync_ShouldCreateMapping()
    {
        // Arrange
        var mockMappingRepository = new Mock<ICanonicalMappingRepository>();
        mockMappingRepository
            .Setup(r => r.CreateAsync(It.IsAny<CanonicalMapping>()))
            .ReturnsAsync((CanonicalMapping m) => m);

        var service = new CanonicalMappingService(
            Mock.Of<ICanonicalEntityRepository>(),
            Mock.Of<ICanonicalEntityVersionRepository>(),
            Mock.Of<ICanonicalAttributeRepository>(),
            mockMappingRepository.Object);

        // Act
        var mapping = await service.CreateMappingAsync(
            connectorId: 1,
            targetEntityType: CanonicalEntityType.Employee,
            targetSchemaVersion: 1,
            externalField: "employee_count",
            canonicalAttribute: "totalEmployees",
            transformationType: "direct",
            createdBy: "admin",
            isRequired: true);

        // Assert
        Assert.Equal(1, mapping.ConnectorId);
        Assert.Equal(CanonicalEntityType.Employee, mapping.TargetEntityType);
        Assert.Equal(1, mapping.TargetSchemaVersion);
        Assert.Equal("employee_count", mapping.ExternalField);
        Assert.Equal("totalEmployees", mapping.CanonicalAttribute);
        Assert.Equal("direct", mapping.TransformationType);
        Assert.True(mapping.IsRequired);
        Assert.True(mapping.IsActive);
        Assert.Equal("admin", mapping.CreatedBy);
    }

    [Fact]
    public async Task ValidateBackwardCompatibilityAsync_ShouldReturnTrueWhenCompatible()
    {
        // Arrange
        var currentVersion = new CanonicalEntityVersion
        {
            Id = 1,
            EntityType = CanonicalEntityType.Employee,
            Version = 1,
            IsActive = true
        };

        var newVersion = new CanonicalEntityVersion
        {
            Id = 2,
            EntityType = CanonicalEntityType.Employee,
            Version = 2,
            BackwardCompatibleWithVersion = 1,
            IsActive = true
        };

        var mockVersionRepository = new Mock<ICanonicalEntityVersionRepository>();
        mockVersionRepository
            .Setup(r => r.GetVersionAsync(CanonicalEntityType.Employee, 1))
            .ReturnsAsync(currentVersion);
        mockVersionRepository
            .Setup(r => r.GetVersionAsync(CanonicalEntityType.Employee, 2))
            .ReturnsAsync(newVersion);

        var service = new CanonicalMappingService(
            Mock.Of<ICanonicalEntityRepository>(),
            mockVersionRepository.Object,
            Mock.Of<ICanonicalAttributeRepository>(),
            Mock.Of<ICanonicalMappingRepository>());

        // Act
        var isCompatible = await service.ValidateBackwardCompatibilityAsync(
            CanonicalEntityType.Employee,
            1,
            2);

        // Assert
        Assert.True(isCompatible);
    }

    [Fact]
    public async Task ValidateBackwardCompatibilityAsync_ShouldReturnFalseWhenNotCompatible()
    {
        // Arrange
        var currentVersion = new CanonicalEntityVersion
        {
            Id = 1,
            EntityType = CanonicalEntityType.Employee,
            Version = 1,
            IsActive = true
        };

        var newVersion = new CanonicalEntityVersion
        {
            Id = 2,
            EntityType = CanonicalEntityType.Employee,
            Version = 2,
            BackwardCompatibleWithVersion = null, // Not backward compatible
            IsActive = true
        };

        var mockVersionRepository = new Mock<ICanonicalEntityVersionRepository>();
        mockVersionRepository
            .Setup(r => r.GetVersionAsync(CanonicalEntityType.Employee, 1))
            .ReturnsAsync(currentVersion);
        mockVersionRepository
            .Setup(r => r.GetVersionAsync(CanonicalEntityType.Employee, 2))
            .ReturnsAsync(newVersion);

        var service = new CanonicalMappingService(
            Mock.Of<ICanonicalEntityRepository>(),
            mockVersionRepository.Object,
            Mock.Of<ICanonicalAttributeRepository>(),
            Mock.Of<ICanonicalMappingRepository>());

        // Act
        var isCompatible = await service.ValidateBackwardCompatibilityAsync(
            CanonicalEntityType.Employee,
            1,
            2);

        // Assert
        Assert.False(isCompatible);
    }

    [Fact]
    public async Task MapToCanonicalEntityAsync_ShouldThrowWhenNoActiveVersion()
    {
        // Arrange
        var mockVersionRepository = new Mock<ICanonicalEntityVersionRepository>();
        mockVersionRepository
            .Setup(r => r.GetLatestActiveVersionAsync(It.IsAny<CanonicalEntityType>()))
            .ReturnsAsync((CanonicalEntityVersion?)null);

        var service = new CanonicalMappingService(
            Mock.Of<ICanonicalEntityRepository>(),
            mockVersionRepository.Object,
            Mock.Of<ICanonicalAttributeRepository>(),
            Mock.Of<ICanonicalMappingRepository>());

        var externalData = new Dictionary<string, object>
        {
            { "employee_count", 100 }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.MapToCanonicalEntityAsync(
                1,
                CanonicalEntityType.Employee,
                externalData,
                "TestSystem"));
    }

    [Fact]
    public async Task MapToCanonicalEntityAsync_ShouldThrowWhenNoMappingsConfigured()
    {
        // Arrange
        var mockVersionRepository = new Mock<ICanonicalEntityVersionRepository>();
        mockVersionRepository
            .Setup(r => r.GetLatestActiveVersionAsync(CanonicalEntityType.Employee))
            .ReturnsAsync(new CanonicalEntityVersion { Version = 1, EntityType = CanonicalEntityType.Employee });

        var mockMappingRepository = new Mock<ICanonicalMappingRepository>();
        mockMappingRepository
            .Setup(r => r.GetByConnectorTypeAndVersionAsync(It.IsAny<int>(), It.IsAny<CanonicalEntityType>(), It.IsAny<int>()))
            .ReturnsAsync(new List<CanonicalMapping>());

        var service = new CanonicalMappingService(
            Mock.Of<ICanonicalEntityRepository>(),
            mockVersionRepository.Object,
            Mock.Of<ICanonicalAttributeRepository>(),
            mockMappingRepository.Object);

        var externalData = new Dictionary<string, object>
        {
            { "employee_count", 100 }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.MapToCanonicalEntityAsync(
                1,
                CanonicalEntityType.Employee,
                externalData,
                "TestSystem"));
    }

    [Fact]
    public async Task MapToCanonicalEntityAsync_ShouldThrowWhenRequiredFieldsMissing()
    {
        // Arrange
        var mockVersionRepository = new Mock<ICanonicalEntityVersionRepository>();
        mockVersionRepository
            .Setup(r => r.GetLatestActiveVersionAsync(CanonicalEntityType.Employee))
            .ReturnsAsync(new CanonicalEntityVersion { Version = 1, EntityType = CanonicalEntityType.Employee });

        var mockMappingRepository = new Mock<ICanonicalMappingRepository>();
        var requiredMapping = new CanonicalMapping
        {
            ExternalField = "employee_count",
            CanonicalAttribute = "totalEmployees",
            IsRequired = true,
            TransformationType = "direct"
        };
        mockMappingRepository
            .Setup(r => r.GetByConnectorTypeAndVersionAsync(It.IsAny<int>(), It.IsAny<CanonicalEntityType>(), It.IsAny<int>()))
            .ReturnsAsync(new List<CanonicalMapping> { requiredMapping });

        var service = new CanonicalMappingService(
            Mock.Of<ICanonicalEntityRepository>(),
            mockVersionRepository.Object,
            Mock.Of<ICanonicalAttributeRepository>(),
            mockMappingRepository.Object);

        var externalData = new Dictionary<string, object>
        {
            { "other_field", "value" } // Missing required field
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.MapToCanonicalEntityAsync(
                1,
                CanonicalEntityType.Employee,
                externalData,
                "TestSystem"));

        Assert.Contains("employee_count", exception.Message);
    }
}
