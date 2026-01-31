using Moq;
using SD.ProjectName.Modules.Integrations.Application;
using SD.ProjectName.Modules.Integrations.Domain.Entities;
using SD.ProjectName.Modules.Integrations.Domain.Interfaces;

namespace SD.ProjectName.Tests.Integrations;

public class IntegrationMonitoringServiceTests
{
    [Fact]
    public async Task SearchJobsAsync_WithNoFilters_ShouldReturnAllJobs()
    {
        // Arrange
        var jobs = new List<IntegrationJobMetadata>
        {
            new IntegrationJobMetadata
            {
                Id = 1,
                JobId = "job-1",
                ConnectorId = 1,
                CorrelationId = "corr-1",
                JobType = "HRSync",
                Status = IntegrationJobStatus.Completed,
                StartedAt = DateTime.UtcNow.AddHours(-2),
                CompletedAt = DateTime.UtcNow.AddHours(-1),
                DurationMs = 3600000,
                TotalRecords = 100,
                SuccessCount = 95,
                FailureCount = 5,
                InitiatedBy = "user1"
            },
            new IntegrationJobMetadata
            {
                Id = 2,
                JobId = "job-2",
                ConnectorId = 2,
                CorrelationId = "corr-2",
                JobType = "FinanceSync",
                Status = IntegrationJobStatus.Failed,
                StartedAt = DateTime.UtcNow.AddHours(-1),
                TotalRecords = 50,
                FailureCount = 50,
                InitiatedBy = "user2"
            }
        };

        var mockJobRepo = new Mock<IIntegrationJobMetadataRepository>();
        mockJobRepo.Setup(r => r.SearchJobsAsync(
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<IntegrationJobStatus?>(),
            It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(jobs);
        mockJobRepo.Setup(r => r.GetJobCountAsync(
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<IntegrationJobStatus?>(),
            It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(2);

        var mockLogRepo = new Mock<IIntegrationLogRepository>();
        var mockHRRepo = new Mock<IHRSyncRecordRepository>();
        var mockFinanceRepo = new Mock<IFinanceSyncRecordRepository>();

        var service = new IntegrationMonitoringService(
            mockJobRepo.Object,
            mockLogRepo.Object,
            mockHRRepo.Object,
            mockFinanceRepo.Object);

        // Act
        var (resultJobs, totalCount) = await service.SearchJobsAsync();

        // Assert
        Assert.Equal(2, resultJobs.Count);
        Assert.Equal(2, totalCount);
        Assert.Contains(resultJobs, j => j.JobId == "job-1");
        Assert.Contains(resultJobs, j => j.JobId == "job-2");
    }

    [Fact]
    public async Task SearchJobsAsync_WithStatusFilter_ShouldReturnFilteredJobs()
    {
        // Arrange
        var filteredJobs = new List<IntegrationJobMetadata>
        {
            new IntegrationJobMetadata
            {
                Id = 1,
                JobId = "job-1",
                ConnectorId = 1,
                CorrelationId = "corr-1",
                JobType = "HRSync",
                Status = IntegrationJobStatus.Completed,
                StartedAt = DateTime.UtcNow.AddHours(-2),
                CompletedAt = DateTime.UtcNow.AddHours(-1),
                InitiatedBy = "user1"
            }
        };

        var mockJobRepo = new Mock<IIntegrationJobMetadataRepository>();
        mockJobRepo.Setup(r => r.SearchJobsAsync(
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), IntegrationJobStatus.Completed,
            It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(filteredJobs);
        mockJobRepo.Setup(r => r.GetJobCountAsync(
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), IntegrationJobStatus.Completed,
            It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(1);

        var mockLogRepo = new Mock<IIntegrationLogRepository>();
        var mockHRRepo = new Mock<IHRSyncRecordRepository>();
        var mockFinanceRepo = new Mock<IFinanceSyncRecordRepository>();

        var service = new IntegrationMonitoringService(
            mockJobRepo.Object,
            mockLogRepo.Object,
            mockHRRepo.Object,
            mockFinanceRepo.Object);

        // Act
        var (resultJobs, totalCount) = await service.SearchJobsAsync(
            status: IntegrationJobStatus.Completed);

        // Assert
        Assert.Single(resultJobs);
        Assert.Equal(1, totalCount);
        Assert.Equal(IntegrationJobStatus.Completed, resultJobs[0].Status);
    }

    [Fact]
    public async Task GetJobDetailsAsync_WithValidJobId_ShouldReturnJobAndLogs()
    {
        // Arrange
        var job = new IntegrationJobMetadata
        {
            Id = 1,
            JobId = "job-123",
            ConnectorId = 1,
            CorrelationId = "corr-123",
            JobType = "HRSync",
            Status = IntegrationJobStatus.Completed,
            StartedAt = DateTime.UtcNow.AddHours(-2),
            CompletedAt = DateTime.UtcNow.AddHours(-1),
            InitiatedBy = "user1"
        };

        var logs = new List<IntegrationLog>
        {
            new IntegrationLog
            {
                Id = 1,
                ConnectorId = 1,
                CorrelationId = "corr-123",
                OperationType = "pull",
                Status = IntegrationStatus.Success,
                StartedAt = DateTime.UtcNow.AddHours(-2),
                CompletedAt = DateTime.UtcNow.AddHours(-2).AddMinutes(5),
                DurationMs = 300000,
                InitiatedBy = "user1"
            }
        };

        var mockJobRepo = new Mock<IIntegrationJobMetadataRepository>();
        mockJobRepo.Setup(r => r.GetByJobIdAsync("job-123")).ReturnsAsync(job);

        var mockLogRepo = new Mock<IIntegrationLogRepository>();
        mockLogRepo.Setup(r => r.GetByCorrelationIdAsync("corr-123")).ReturnsAsync(logs);

        var mockHRRepo = new Mock<IHRSyncRecordRepository>();
        var mockFinanceRepo = new Mock<IFinanceSyncRecordRepository>();

        var service = new IntegrationMonitoringService(
            mockJobRepo.Object,
            mockLogRepo.Object,
            mockHRRepo.Object,
            mockFinanceRepo.Object);

        // Act
        var details = await service.GetJobDetailsAsync("job-123");

        // Assert
        Assert.NotNull(details);
        Assert.Equal("job-123", details.Job.JobId);
        Assert.Single(details.Logs);
        Assert.Equal(IntegrationStatus.Success, details.Logs[0].Status);
    }

    [Fact]
    public async Task GetJobDetailsAsync_WithInvalidJobId_ShouldReturnNull()
    {
        // Arrange
        var mockJobRepo = new Mock<IIntegrationJobMetadataRepository>();
        mockJobRepo.Setup(r => r.GetByJobIdAsync("invalid-job")).ReturnsAsync((IntegrationJobMetadata?)null);

        var mockLogRepo = new Mock<IIntegrationLogRepository>();
        var mockHRRepo = new Mock<IHRSyncRecordRepository>();
        var mockFinanceRepo = new Mock<IFinanceSyncRecordRepository>();

        var service = new IntegrationMonitoringService(
            mockJobRepo.Object,
            mockLogRepo.Object,
            mockHRRepo.Object,
            mockFinanceRepo.Object);

        // Act
        var details = await service.GetJobDetailsAsync("invalid-job");

        // Assert
        Assert.Null(details);
    }

    [Fact]
    public async Task GetApprovalHistoryAsync_ShouldReturnApprovedOverrides()
    {
        // Arrange
        var financeSyncRecords = new List<FinanceSyncRecord>
        {
            new FinanceSyncRecord
            {
                Id = 1,
                ConnectorId = 1,
                CorrelationId = "corr-1",
                Status = FinanceSyncStatus.Success,
                ExternalId = "ext-123",
                ConflictDetected = true,
                ConflictResolution = "AdminOverride",
                ApprovedOverrideBy = "admin-user",
                SyncedAt = DateTime.UtcNow.AddHours(-1),
                InitiatedBy = "system",
                FinanceEntityId = 100
            },
            new FinanceSyncRecord
            {
                Id = 2,
                ConnectorId = 1,
                CorrelationId = "corr-2",
                Status = FinanceSyncStatus.Success,
                ExternalId = "ext-456",
                ConflictDetected = false,
                SyncedAt = DateTime.UtcNow.AddHours(-2),
                InitiatedBy = "user1"
            }
        };

        var mockJobRepo = new Mock<IIntegrationJobMetadataRepository>();
        var mockLogRepo = new Mock<IIntegrationLogRepository>();
        var mockHRRepo = new Mock<IHRSyncRecordRepository>();
        var mockFinanceRepo = new Mock<IFinanceSyncRecordRepository>();
        mockFinanceRepo.Setup(r => r.SearchRecordsAsync(
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<FinanceSyncStatus?>(),
            It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(financeSyncRecords);

        var service = new IntegrationMonitoringService(
            mockJobRepo.Object,
            mockLogRepo.Object,
            mockHRRepo.Object,
            mockFinanceRepo.Object);

        // Act
        var history = await service.GetApprovalHistoryAsync();

        // Assert
        Assert.Single(history); // Only one record has an approved override
        Assert.Equal("admin-user", history[0].ApprovedBy);
        Assert.Equal("AdminOverride", history[0].ConflictResolution);
        Assert.Equal("Override Approved", history[0].Action);
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldCalculateCorrectAggregates()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;

        var jobs = new List<IntegrationJobMetadata>
        {
            new IntegrationJobMetadata
            {
                Id = 1,
                JobId = "job-1",
                ConnectorId = 1,
                CorrelationId = "corr-1",
                JobType = "HRSync",
                Status = IntegrationJobStatus.Completed,
                TotalRecords = 100,
                SuccessCount = 95,
                FailureCount = 5,
                DurationMs = 60000,
                StartedAt = DateTime.UtcNow.AddHours(-2),
                InitiatedBy = "user1"
            },
            new IntegrationJobMetadata
            {
                Id = 2,
                JobId = "job-2",
                ConnectorId = 1,
                CorrelationId = "corr-2",
                JobType = "FinanceSync",
                Status = IntegrationJobStatus.Failed,
                TotalRecords = 50,
                FailureCount = 50,
                DurationMs = 30000,
                StartedAt = DateTime.UtcNow.AddHours(-1),
                InitiatedBy = "user2"
            }
        };

        var logs = new List<IntegrationLog>
        {
            new IntegrationLog { Id = 1, ConnectorId = 1, Status = IntegrationStatus.Success, CorrelationId = "corr-1", OperationType = "pull", InitiatedBy = "user1", StartedAt = DateTime.UtcNow },
            new IntegrationLog { Id = 2, ConnectorId = 1, Status = IntegrationStatus.Success, CorrelationId = "corr-1", OperationType = "pull", InitiatedBy = "user1", StartedAt = DateTime.UtcNow },
            new IntegrationLog { Id = 3, ConnectorId = 1, Status = IntegrationStatus.Failed, CorrelationId = "corr-2", OperationType = "pull", InitiatedBy = "user2", StartedAt = DateTime.UtcNow }
        };

        var mockJobRepo = new Mock<IIntegrationJobMetadataRepository>();
        mockJobRepo.Setup(r => r.SearchJobsAsync(
            startDate, endDate, It.IsAny<IntegrationJobStatus?>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(jobs);

        var mockLogRepo = new Mock<IIntegrationLogRepository>();
        mockLogRepo.Setup(r => r.SearchLogsAsync(
            startDate, endDate, It.IsAny<IntegrationStatus?>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(logs);

        var mockHRRepo = new Mock<IHRSyncRecordRepository>();
        var mockFinanceRepo = new Mock<IFinanceSyncRecordRepository>();

        var service = new IntegrationMonitoringService(
            mockJobRepo.Object,
            mockLogRepo.Object,
            mockHRRepo.Object,
            mockFinanceRepo.Object);

        // Act
        var stats = await service.GetStatisticsAsync(startDate, endDate);

        // Assert
        Assert.Equal(2, stats.TotalJobs);
        Assert.Equal(1, stats.CompletedJobs);
        Assert.Equal(1, stats.FailedJobs);
        Assert.Equal(150, stats.TotalRecordsProcessed);
        Assert.Equal(95, stats.TotalRecordsSucceeded);
        Assert.Equal(55, stats.TotalRecordsFailed);
        Assert.Equal(3, stats.TotalApiCalls);
        Assert.Equal(2, stats.SuccessfulApiCalls);
        Assert.Equal(1, stats.FailedApiCalls);
        Assert.Equal(45000, stats.AverageJobDurationMs); // (60000 + 30000) / 2
    }

    [Fact]
    public async Task CreateJobAsync_ShouldCreateNewJob()
    {
        // Arrange
        var newJob = new IntegrationJobMetadata
        {
            JobId = "job-new",
            ConnectorId = 1,
            CorrelationId = "corr-new",
            JobType = "HRSync",
            Status = IntegrationJobStatus.Running,
            StartedAt = DateTime.UtcNow,
            InitiatedBy = "user1"
        };

        var mockJobRepo = new Mock<IIntegrationJobMetadataRepository>();
        mockJobRepo.Setup(r => r.CreateAsync(It.IsAny<IntegrationJobMetadata>()))
            .ReturnsAsync((IntegrationJobMetadata job) =>
            {
                job.Id = 1;
                return job;
            });

        var mockLogRepo = new Mock<IIntegrationLogRepository>();
        var mockHRRepo = new Mock<IHRSyncRecordRepository>();
        var mockFinanceRepo = new Mock<IFinanceSyncRecordRepository>();

        var service = new IntegrationMonitoringService(
            mockJobRepo.Object,
            mockLogRepo.Object,
            mockHRRepo.Object,
            mockFinanceRepo.Object);

        // Act
        var created = await service.CreateJobAsync(newJob);

        // Assert
        Assert.NotNull(created);
        Assert.Equal(1, created.Id);
        Assert.Equal("job-new", created.JobId);
        mockJobRepo.Verify(r => r.CreateAsync(It.IsAny<IntegrationJobMetadata>()), Times.Once);
    }

    [Fact]
    public async Task UpdateJobAsync_ShouldUpdateExistingJob()
    {
        // Arrange
        var existingJob = new IntegrationJobMetadata
        {
            Id = 1,
            JobId = "job-123",
            ConnectorId = 1,
            CorrelationId = "corr-123",
            JobType = "HRSync",
            Status = IntegrationJobStatus.Running,
            StartedAt = DateTime.UtcNow.AddHours(-1),
            InitiatedBy = "user1"
        };

        existingJob.Status = IntegrationJobStatus.Completed;
        existingJob.CompletedAt = DateTime.UtcNow;
        existingJob.DurationMs = 3600000;

        var mockJobRepo = new Mock<IIntegrationJobMetadataRepository>();
        mockJobRepo.Setup(r => r.UpdateAsync(It.IsAny<IntegrationJobMetadata>()))
            .ReturnsAsync((IntegrationJobMetadata job) => job);

        var mockLogRepo = new Mock<IIntegrationLogRepository>();
        var mockHRRepo = new Mock<IHRSyncRecordRepository>();
        var mockFinanceRepo = new Mock<IFinanceSyncRecordRepository>();

        var service = new IntegrationMonitoringService(
            mockJobRepo.Object,
            mockLogRepo.Object,
            mockHRRepo.Object,
            mockFinanceRepo.Object);

        // Act
        var updated = await service.UpdateJobAsync(existingJob);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(IntegrationJobStatus.Completed, updated.Status);
        Assert.NotNull(updated.CompletedAt);
        Assert.Equal(3600000, updated.DurationMs);
        mockJobRepo.Verify(r => r.UpdateAsync(It.IsAny<IntegrationJobMetadata>()), Times.Once);
    }
}
