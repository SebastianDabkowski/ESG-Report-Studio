using Moq;
using SD.ProjectName.Modules.Integrations.Application;
using SD.ProjectName.Modules.Integrations.Domain.Entities;
using SD.ProjectName.Modules.Integrations.Domain.Interfaces;

namespace SD.ProjectName.Tests.Integrations;

public class ConnectorServiceTests
{
    [Fact]
    public async Task CreateConnectorAsync_ShouldCreateConnectorWithDisabledStatus()
    {
        // Arrange
        var mockRepository = new Mock<IConnectorRepository>();
        mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<Connector>()))
            .ReturnsAsync((Connector c) => c);

        var service = new ConnectorService(mockRepository.Object);

        // Act
        var connector = await service.CreateConnectorAsync(
            "Test Connector",
            "HR",
            "https://api.hr-system.com",
            "OAuth2",
            "SecretStore:HR-ApiKey",
            "pull,push",
            "admin");

        // Assert
        Assert.Equal("Test Connector", connector.Name);
        Assert.Equal("HR", connector.ConnectorType);
        Assert.Equal(ConnectorStatus.Disabled, connector.Status); // Should default to Disabled
        Assert.Equal("https://api.hr-system.com", connector.EndpointBaseUrl);
        Assert.Equal("OAuth2", connector.AuthenticationType);
        Assert.Equal("SecretStore:HR-ApiKey", connector.AuthenticationSecretRef);
        Assert.Equal("pull,push", connector.Capabilities);
        Assert.Equal("admin", connector.CreatedBy);
    }

    [Fact]
    public async Task EnableConnectorAsync_ShouldSetStatusToEnabled()
    {
        // Arrange
        var connector = new Connector
        {
            Id = 1,
            Name = "Test Connector",
            ConnectorType = "HR",
            Status = ConnectorStatus.Disabled,
            EndpointBaseUrl = "https://api.hr-system.com",
            AuthenticationType = "OAuth2",
            AuthenticationSecretRef = "SecretStore:HR-ApiKey",
            Capabilities = "pull",
            CreatedBy = "admin"
        };

        var mockRepository = new Mock<IConnectorRepository>();
        mockRepository
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(connector);
        mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Connector>()))
            .ReturnsAsync((Connector c) => c);

        var service = new ConnectorService(mockRepository.Object);

        // Act
        var result = await service.EnableConnectorAsync(1, "admin");

        // Assert
        Assert.Equal(ConnectorStatus.Enabled, result.Status);
        Assert.Equal("admin", result.UpdatedBy);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task DisableConnectorAsync_ShouldSetStatusToDisabled()
    {
        // Arrange
        var connector = new Connector
        {
            Id = 1,
            Name = "Test Connector",
            ConnectorType = "HR",
            Status = ConnectorStatus.Enabled,
            EndpointBaseUrl = "https://api.hr-system.com",
            AuthenticationType = "OAuth2",
            AuthenticationSecretRef = "SecretStore:HR-ApiKey",
            Capabilities = "pull",
            CreatedBy = "admin"
        };

        var mockRepository = new Mock<IConnectorRepository>();
        mockRepository
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(connector);
        mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Connector>()))
            .ReturnsAsync((Connector c) => c);

        var service = new ConnectorService(mockRepository.Object);

        // Act
        var result = await service.DisableConnectorAsync(1, "admin");

        // Assert
        Assert.Equal(ConnectorStatus.Disabled, result.Status);
        Assert.Equal("admin", result.UpdatedBy);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task EnableConnectorAsync_WithNonExistentConnector_ShouldThrowException()
    {
        // Arrange
        var mockRepository = new Mock<IConnectorRepository>();
        mockRepository
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Connector?)null);

        var service = new ConnectorService(mockRepository.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.EnableConnectorAsync(999, "admin"));
    }
}
