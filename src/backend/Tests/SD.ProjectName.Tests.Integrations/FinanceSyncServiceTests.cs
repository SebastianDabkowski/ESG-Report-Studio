using Moq;
using SD.ProjectName.Modules.Integrations.Application;
using SD.ProjectName.Modules.Integrations.Domain.Entities;
using SD.ProjectName.Modules.Integrations.Domain.Interfaces;
using Xunit;

namespace SD.ProjectName.Tests.Integrations;

public class FinanceSyncServiceTests
{
    private readonly Mock<IConnectorRepository> _connectorRepositoryMock;
    private readonly Mock<IFinanceEntityRepository> _financeEntityRepositoryMock;
    private readonly Mock<IFinanceSyncRecordRepository> _financeSyncRecordRepositoryMock;
    private readonly Mock<IntegrationExecutionService> _integrationExecutionServiceMock;
    private readonly FinanceSyncService _financeSyncService;

    public FinanceSyncServiceTests()
    {
        _connectorRepositoryMock = new Mock<IConnectorRepository>();
        _financeEntityRepositoryMock = new Mock<IFinanceEntityRepository>();
        _financeSyncRecordRepositoryMock = new Mock<IFinanceSyncRecordRepository>();
        _integrationExecutionServiceMock = new Mock<IntegrationExecutionService>(
            Mock.Of<IConnectorRepository>(),
            Mock.Of<IIntegrationLogRepository>());

        _financeSyncService = new FinanceSyncService(
            _connectorRepositoryMock.Object,
            _financeEntityRepositoryMock.Object,
            _financeSyncRecordRepositoryMock.Object,
            _integrationExecutionServiceMock.Object);
    }

    [Fact]
    public async Task TestConnectionAsync_WithNonExistentConnector_ReturnsFailure()
    {
        // Arrange
        var connectorId = 999;
        _connectorRepositoryMock.Setup(r => r.GetByIdAsync(connectorId))
            .ReturnsAsync((Connector?)null);

        // Act
        var result = await _financeSyncService.TestConnectionAsync(connectorId, "testuser");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.Message);
    }

    [Fact]
    public async Task TestConnectionAsync_WithNonFinanceConnector_ReturnsFailure()
    {
        // Arrange
        var connectorId = 1;
        var connector = new Connector
        {
            Id = connectorId,
            Name = "HR System",
            ConnectorType = "HR",
            Status = ConnectorStatus.Enabled
        };

        _connectorRepositoryMock.Setup(r => r.GetByIdAsync(connectorId))
            .ReturnsAsync(connector);

        // Act
        var result = await _financeSyncService.TestConnectionAsync(connectorId, "testuser");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not a Finance connector", result.Message);
    }

    [Fact]
    public async Task ExecuteSyncAsync_WithDisabledConnector_ThrowsException()
    {
        // Arrange
        var connectorId = 1;
        var connector = new Connector
        {
            Id = connectorId,
            Name = "Finance System",
            ConnectorType = "Finance",
            Status = ConnectorStatus.Disabled
        };

        _connectorRepositoryMock.Setup(r => r.GetByIdAsync(connectorId))
            .ReturnsAsync(connector);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _financeSyncService.ExecuteSyncAsync(connectorId, "testuser"));
    }

    [Fact]
    public async Task ExecuteSyncAsync_WithNonFinanceConnector_ThrowsException()
    {
        // Arrange
        var connectorId = 1;
        var connector = new Connector
        {
            Id = connectorId,
            Name = "HR System",
            ConnectorType = "HR",
            Status = ConnectorStatus.Enabled
        };

        _connectorRepositoryMock.Setup(r => r.GetByIdAsync(connectorId))
            .ReturnsAsync(connector);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _financeSyncService.ExecuteSyncAsync(connectorId, "testuser"));
    }

    [Fact]
    public async Task GetSyncHistoryAsync_ReturnsRecords()
    {
        // Arrange
        var connectorId = 1;
        var expectedRecords = new List<FinanceSyncRecord>
        {
            new FinanceSyncRecord
            {
                Id = 1,
                ConnectorId = connectorId,
                CorrelationId = "corr-123",
                Status = FinanceSyncStatus.Success,
                SyncedAt = DateTime.UtcNow,
                InitiatedBy = "testuser"
            }
        };

        _financeSyncRecordRepositoryMock.Setup(r => r.GetByConnectorIdAsync(connectorId, 100))
            .ReturnsAsync(expectedRecords);

        // Act
        var result = await _financeSyncService.GetSyncHistoryAsync(connectorId, 100);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(connectorId, result[0].ConnectorId);
    }

    [Fact]
    public async Task GetRejectedRecordsAsync_ReturnsRejectedRecords()
    {
        // Arrange
        var connectorId = 1;
        var expectedRecords = new List<FinanceSyncRecord>
        {
            new FinanceSyncRecord
            {
                Id = 1,
                ConnectorId = connectorId,
                CorrelationId = "corr-123",
                Status = FinanceSyncStatus.Rejected,
                RejectionReason = "Missing required field",
                SyncedAt = DateTime.UtcNow,
                InitiatedBy = "testuser"
            }
        };

        _financeSyncRecordRepositoryMock.Setup(r => r.GetRejectedByConnectorIdAsync(connectorId, 100))
            .ReturnsAsync(expectedRecords);

        // Act
        var result = await _financeSyncService.GetRejectedRecordsAsync(connectorId, 100);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(FinanceSyncStatus.Rejected, result[0].Status);
        Assert.NotNull(result[0].RejectionReason);
    }

    [Fact]
    public async Task GetConflictsAsync_ReturnsConflictRecords()
    {
        // Arrange
        var connectorId = 1;
        var expectedRecords = new List<FinanceSyncRecord>
        {
            new FinanceSyncRecord
            {
                Id = 1,
                ConnectorId = connectorId,
                CorrelationId = "corr-123",
                Status = FinanceSyncStatus.ConflictPreserved,
                ConflictDetected = true,
                ConflictResolution = "PreservedManual",
                RejectionReason = "Cannot overwrite approved manual data",
                SyncedAt = DateTime.UtcNow,
                InitiatedBy = "testuser"
            }
        };

        _financeSyncRecordRepositoryMock.Setup(r => r.GetConflictsByConnectorIdAsync(connectorId, 100))
            .ReturnsAsync(expectedRecords);

        // Act
        var result = await _financeSyncService.GetConflictsAsync(connectorId, 100);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.True(result[0].ConflictDetected);
        Assert.Equal("PreservedManual", result[0].ConflictResolution);
    }
}
