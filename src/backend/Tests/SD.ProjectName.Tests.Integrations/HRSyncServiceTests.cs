using Moq;
using SD.ProjectName.Modules.Integrations.Application;
using SD.ProjectName.Modules.Integrations.Domain.Entities;
using SD.ProjectName.Modules.Integrations.Domain.Interfaces;

namespace SD.ProjectName.Tests.Integrations;

public class HRSyncServiceTests
{
    [Fact]
    public async Task TestConnectionAsync_WithNonHRConnector_ShouldReturnFailure()
    {
        // Arrange
        var connector = new Connector
        {
            Id = 1,
            Name = "Test ERP Connector",
            ConnectorType = "ERP", // Not HR
            Status = ConnectorStatus.Enabled,
            EndpointBaseUrl = "https://api.erp-system.com",
            CreatedBy = "admin"
        };

        var mockConnectorRepo = new Mock<IConnectorRepository>();
        mockConnectorRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(connector);

        var mockIntegrationLogRepo = new Mock<IIntegrationLogRepository>();
        var integrationExecutionService = new IntegrationExecutionService(
            mockConnectorRepo.Object,
            mockIntegrationLogRepo.Object);

        var service = new HRSyncService(
            mockConnectorRepo.Object,
            Mock.Of<IHREntityRepository>(),
            Mock.Of<IHRSyncRecordRepository>(),
            integrationExecutionService);

        // Act
        var result = await service.TestConnectionAsync(1, "admin");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not an HR connector", result.Message);
    }

    [Fact]
    public async Task TestConnectionAsync_WithNonExistentConnector_ShouldReturnFailure()
    {
        // Arrange
        var mockConnectorRepo = new Mock<IConnectorRepository>();
        mockConnectorRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Connector?)null);

        var mockIntegrationLogRepo = new Mock<IIntegrationLogRepository>();
        var integrationExecutionService = new IntegrationExecutionService(
            mockConnectorRepo.Object,
            mockIntegrationLogRepo.Object);

        var service = new HRSyncService(
            mockConnectorRepo.Object,
            Mock.Of<IHREntityRepository>(),
            Mock.Of<IHRSyncRecordRepository>(),
            integrationExecutionService);

        // Act
        var result = await service.TestConnectionAsync(999, "admin");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.Message);
    }

    [Fact]
    public async Task ExecuteSyncAsync_WithDisabledConnector_ShouldThrowException()
    {
        // Arrange
        var connector = new Connector
        {
            Id = 1,
            Name = "Test HR Connector",
            ConnectorType = "HR",
            Status = ConnectorStatus.Disabled, // Disabled
            EndpointBaseUrl = "https://api.hr-system.com",
            CreatedBy = "admin"
        };

        var mockConnectorRepo = new Mock<IConnectorRepository>();
        mockConnectorRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(connector);

        var mockIntegrationLogRepo = new Mock<IIntegrationLogRepository>();
        var integrationExecutionService = new IntegrationExecutionService(
            mockConnectorRepo.Object,
            mockIntegrationLogRepo.Object);

        var service = new HRSyncService(
            mockConnectorRepo.Object,
            Mock.Of<IHREntityRepository>(),
            Mock.Of<IHRSyncRecordRepository>(),
            integrationExecutionService);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ExecuteSyncAsync(1, "admin", false));
    }

    [Fact]
    public async Task ExecuteSyncAsync_WithNonHRConnector_ShouldThrowException()
    {
        // Arrange
        var connector = new Connector
        {
            Id = 1,
            Name = "Test ERP Connector",
            ConnectorType = "ERP",
            Status = ConnectorStatus.Enabled,
            EndpointBaseUrl = "https://api.erp-system.com",
            CreatedBy = "admin"
        };

        var mockConnectorRepo = new Mock<IConnectorRepository>();
        mockConnectorRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(connector);

        var mockIntegrationLogRepo = new Mock<IIntegrationLogRepository>();
        var integrationExecutionService = new IntegrationExecutionService(
            mockConnectorRepo.Object,
            mockIntegrationLogRepo.Object);

        var service = new HRSyncService(
            mockConnectorRepo.Object,
            Mock.Of<IHREntityRepository>(),
            Mock.Of<IHRSyncRecordRepository>(),
            integrationExecutionService);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ExecuteSyncAsync(1, "admin", false));
        
        Assert.Contains("not an HR connector", exception.Message);
    }

    [Fact]
    public async Task GetSyncHistoryAsync_ShouldReturnSyncRecords()
    {
        // Arrange
        var syncRecords = new List<HRSyncRecord>
        {
            new HRSyncRecord
            {
                Id = 1,
                ConnectorId = 1,
                Status = HRSyncStatus.Success,
                SyncedAt = DateTime.UtcNow,
                InitiatedBy = "admin"
            },
            new HRSyncRecord
            {
                Id = 2,
                ConnectorId = 1,
                Status = HRSyncStatus.Rejected,
                RejectionReason = "Missing required field",
                SyncedAt = DateTime.UtcNow,
                InitiatedBy = "admin"
            }
        };

        var mockHRSyncRecordRepo = new Mock<IHRSyncRecordRepository>();
        mockHRSyncRecordRepo
            .Setup(r => r.GetByConnectorIdAsync(1, 100))
            .ReturnsAsync(syncRecords);

        var mockIntegrationLogRepo = new Mock<IIntegrationLogRepository>();
        var integrationExecutionService = new IntegrationExecutionService(
            Mock.Of<IConnectorRepository>(),
            mockIntegrationLogRepo.Object);

        var service = new HRSyncService(
            Mock.Of<IConnectorRepository>(),
            Mock.Of<IHREntityRepository>(),
            mockHRSyncRecordRepo.Object,
            integrationExecutionService);

        // Act
        var result = await service.GetSyncHistoryAsync(1, 100);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(HRSyncStatus.Success, result[0].Status);
        Assert.Equal(HRSyncStatus.Rejected, result[1].Status);
    }

    [Fact]
    public async Task GetRejectedRecordsAsync_ShouldReturnRejectedRecords()
    {
        // Arrange
        var rejectedRecords = new List<HRSyncRecord>
        {
            new HRSyncRecord
            {
                Id = 1,
                ConnectorId = 1,
                Status = HRSyncStatus.Rejected,
                RejectionReason = "Missing required field",
                SyncedAt = DateTime.UtcNow,
                InitiatedBy = "admin"
            }
        };

        var mockHRSyncRecordRepo = new Mock<IHRSyncRecordRepository>();
        mockHRSyncRecordRepo
            .Setup(r => r.GetRejectedRecordsAsync(1, 100))
            .ReturnsAsync(rejectedRecords);

        var mockIntegrationLogRepo = new Mock<IIntegrationLogRepository>();
        var integrationExecutionService = new IntegrationExecutionService(
            Mock.Of<IConnectorRepository>(),
            mockIntegrationLogRepo.Object);

        var service = new HRSyncService(
            Mock.Of<IConnectorRepository>(),
            Mock.Of<IHREntityRepository>(),
            mockHRSyncRecordRepo.Object,
            integrationExecutionService);

        // Act
        var result = await service.GetRejectedRecordsAsync(1, 100);

        // Assert
        Assert.Single(result);
        Assert.Equal(HRSyncStatus.Rejected, result[0].Status);
        Assert.Equal("Missing required field", result[0].RejectionReason);
    }
}
