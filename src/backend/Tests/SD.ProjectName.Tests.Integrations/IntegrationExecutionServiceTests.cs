using Moq;
using SD.ProjectName.Modules.Integrations.Application;
using SD.ProjectName.Modules.Integrations.Domain.Entities;
using SD.ProjectName.Modules.Integrations.Domain.Interfaces;

namespace SD.ProjectName.Tests.Integrations;

public class IntegrationExecutionServiceTests
{
    [Fact]
    public async Task ExecuteWithRetryAsync_WithDisabledConnector_ShouldSkip()
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
            MaxRetryAttempts = 3,
            RetryDelaySeconds = 1,
            UseExponentialBackoff = true,
            CreatedBy = "admin"
        };

        var mockConnectorRepository = new Mock<IConnectorRepository>();
        mockConnectorRepository
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(connector);

        var mockLogRepository = new Mock<IIntegrationLogRepository>();
        mockLogRepository
            .Setup(r => r.CreateAsync(It.IsAny<IntegrationLog>()))
            .ReturnsAsync((IntegrationLog log) => log);

        var service = new IntegrationExecutionService(
            mockConnectorRepository.Object,
            mockLogRepository.Object);

        // Act
        var log = await service.ExecuteWithRetryAsync(
            1,
            "pull",
            "corr-123",
            "test-user",
            async () => new IntegrationCallResult());

        // Assert
        Assert.Equal(IntegrationStatus.Skipped, log.Status);
        Assert.Contains("disabled", log.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, log.RetryAttempts);
        Assert.Equal(0, log.DurationMs);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithEnabledConnector_ShouldExecuteSuccessfully()
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
            MaxRetryAttempts = 3,
            RetryDelaySeconds = 1,
            UseExponentialBackoff = true,
            CreatedBy = "admin"
        };

        var mockConnectorRepository = new Mock<IConnectorRepository>();
        mockConnectorRepository
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(connector);

        var mockLogRepository = new Mock<IIntegrationLogRepository>();
        mockLogRepository
            .Setup(r => r.CreateAsync(It.IsAny<IntegrationLog>()))
            .ReturnsAsync((IntegrationLog log) => log);

        var service = new IntegrationExecutionService(
            mockConnectorRepository.Object,
            mockLogRepository.Object);

        // Act
        var log = await service.ExecuteWithRetryAsync(
            1,
            "pull",
            "corr-123",
            "test-user",
            async () => new IntegrationCallResult
            {
                HttpMethod = "GET",
                Endpoint = "/employees",
                HttpStatusCode = 200,
                ResponseSummary = "Success"
            });

        // Assert
        Assert.Equal(IntegrationStatus.Success, log.Status);
        Assert.Equal(0, log.RetryAttempts); // Should succeed on first attempt
        Assert.Equal("GET", log.HttpMethod);
        Assert.Equal("/employees", log.Endpoint);
        Assert.Equal(200, log.HttpStatusCode);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithFailure_ShouldRetryAndLog()
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
            MaxRetryAttempts = 2,
            RetryDelaySeconds = 1,
            UseExponentialBackoff = false,
            CreatedBy = "admin"
        };

        var mockConnectorRepository = new Mock<IConnectorRepository>();
        mockConnectorRepository
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(connector);

        var mockLogRepository = new Mock<IIntegrationLogRepository>();
        mockLogRepository
            .Setup(r => r.CreateAsync(It.IsAny<IntegrationLog>()))
            .ReturnsAsync((IntegrationLog log) => log);

        var service = new IntegrationExecutionService(
            mockConnectorRepository.Object,
            mockLogRepository.Object);

        var attemptCount = 0;

        // Act
        var log = await service.ExecuteWithRetryAsync(
            1,
            "pull",
            "corr-123",
            "test-user",
            async () =>
            {
                attemptCount++;
                throw new HttpRequestException("Connection failed");
            });

        // Assert
        Assert.Equal(IntegrationStatus.Failed, log.Status);
        Assert.Equal(2, log.RetryAttempts); // Should retry MaxRetryAttempts times
        Assert.Equal(3, attemptCount); // Initial attempt + 2 retries
        Assert.Contains("Connection failed", log.ErrorMessage);
        Assert.NotNull(log.ErrorDetails);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_ShouldPopulateCorrelationId()
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
            MaxRetryAttempts = 3,
            RetryDelaySeconds = 1,
            UseExponentialBackoff = true,
            CreatedBy = "admin"
        };

        var mockConnectorRepository = new Mock<IConnectorRepository>();
        mockConnectorRepository
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(connector);

        var mockLogRepository = new Mock<IIntegrationLogRepository>();
        mockLogRepository
            .Setup(r => r.CreateAsync(It.IsAny<IntegrationLog>()))
            .ReturnsAsync((IntegrationLog log) => log);

        var service = new IntegrationExecutionService(
            mockConnectorRepository.Object,
            mockLogRepository.Object);

        var correlationId = "test-correlation-id-123";

        // Act
        var log = await service.ExecuteWithRetryAsync(
            1,
            "pull",
            correlationId,
            "test-user",
            async () => new IntegrationCallResult());

        // Assert
        Assert.Equal(correlationId, log.CorrelationId);
        Assert.Equal("pull", log.OperationType);
        Assert.Equal("test-user", log.InitiatedBy);
    }
}
