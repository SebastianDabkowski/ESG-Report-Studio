using ARP.ESG_ReportStudio.API.Reporting;
using ARP.ESG_ReportStudio.API.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace SD.ProjectName.Tests.Products;

public class ReminderTests
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
            OwnerId = "user-1",
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
            Description = "Test section for data points",
            OwnerId = "user-1",
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
    public void CreateReminderConfiguration_CreatesNewConfiguration()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        var snapshot = store.GetSnapshot();
        var periodId = snapshot.Periods.First().Id;

        var config = new ReminderConfiguration
        {
            PeriodId = periodId,
            Enabled = true,
            DaysBeforeDeadline = new List<int> { 7, 3, 1 },
            CheckFrequencyHours = 24
        };

        // Act
        var result = store.CreateOrUpdateReminderConfiguration(periodId, config);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Id);
        Assert.Equal(periodId, result.PeriodId);
        Assert.True(result.Enabled);
        Assert.Equal(3, result.DaysBeforeDeadline.Count);
    }

    [Fact]
    public void UpdateReminderConfiguration_UpdatesExistingConfiguration()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        var snapshot = store.GetSnapshot();
        var periodId = snapshot.Periods.First().Id;

        var config = new ReminderConfiguration
        {
            PeriodId = periodId,
            Enabled = true,
            DaysBeforeDeadline = new List<int> { 7, 3, 1 },
            CheckFrequencyHours = 24
        };

        // Create initial configuration
        var created = store.CreateOrUpdateReminderConfiguration(periodId, config);

        // Act - Update configuration
        var updatedConfig = new ReminderConfiguration
        {
            PeriodId = periodId,
            Enabled = false,
            DaysBeforeDeadline = new List<int> { 5, 2 },
            CheckFrequencyHours = 12
        };
        var result = store.CreateOrUpdateReminderConfiguration(periodId, updatedConfig);

        // Assert
        Assert.Equal(created.Id, result.Id); // Same ID means it's an update
        Assert.False(result.Enabled);
        Assert.Equal(2, result.DaysBeforeDeadline.Count);
        Assert.Equal(12, result.CheckFrequencyHours);
    }

    [Fact]
    public void GetReminderConfiguration_ReturnsNullForNonExistentPeriod()
    {
        // Arrange
        var store = new InMemoryReportStore();

        // Act
        var result = store.GetReminderConfiguration("non-existent-period");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void DataPoint_WithDeadline_CanBeCreated()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        var sectionId = CreateTestSection(store);

        var deadline = DateTime.UtcNow.AddDays(7).ToString("O");
        var request = new CreateDataPointRequest
        {
            SectionId = sectionId,
            Type = "narrative",
            Title = "Test Data Point",
            Content = "Test content",
            OwnerId = "user-1",
            Source = "Test source",
            InformationType = "fact",
            CompletenessStatus = "incomplete",
            Deadline = deadline
        };

        // Act
        var (isValid, errorMessage, dataPoint) = store.CreateDataPoint(request);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
        Assert.NotNull(dataPoint);
        Assert.Equal(deadline, dataPoint.Deadline);
    }

    [Fact]
    public void DataPoint_Deadline_CanBeUpdated()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        var sectionId = CreateTestSection(store);

        var request = new CreateDataPointRequest
        {
            SectionId = sectionId,
            Type = "narrative",
            Title = "Test Data Point",
            Content = "Test content",
            OwnerId = "user-1",
            Source = "Test source",
            InformationType = "fact",
            CompletenessStatus = "incomplete"
        };

        var (_, _, dataPoint) = store.CreateDataPoint(request);
        Assert.NotNull(dataPoint);

        // Act - Update with deadline
        var newDeadline = DateTime.UtcNow.AddDays(14).ToString("O");
        var updateRequest = new UpdateDataPointRequest
        {
            Type = dataPoint.Type,
            Title = dataPoint.Title,
            Content = dataPoint.Content,
            OwnerId = dataPoint.OwnerId,
            Source = dataPoint.Source,
            InformationType = dataPoint.InformationType,
            CompletenessStatus = dataPoint.CompletenessStatus,
            Deadline = newDeadline
        };

        var (isValid, errorMessage, updatedDataPoint) = store.UpdateDataPoint(dataPoint.Id, updateRequest);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
        Assert.NotNull(updatedDataPoint);
        Assert.Equal(newDeadline, updatedDataPoint.Deadline);
    }

    [Fact]
    public void GetDataPointsForPeriod_ReturnsOnlyDataPointsInPeriod()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        var snapshot = store.GetSnapshot();
        var periodId = snapshot.Periods.First().Id;
        var sectionId = CreateTestSection(store);

        // Create data points
        for (int i = 0; i < 3; i++)
        {
            var request = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "narrative",
                Title = $"Test Data Point {i}",
                Content = "Test content",
                OwnerId = "user-1",
                Source = "Test source",
                InformationType = "fact",
                CompletenessStatus = "incomplete"
            };
            store.CreateDataPoint(request);
        }

        // Act
        var dataPoints = store.GetDataPointsForPeriod(periodId);

        // Assert
        Assert.Equal(3, dataPoints.Count);
    }

    [Fact]
    public void RecordReminderSent_StoresReminderHistory()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        var sectionId = CreateTestSection(store);

        var deadline = DateTime.UtcNow.AddDays(3).ToString("O");
        var request = new CreateDataPointRequest
        {
            SectionId = sectionId,
            Type = "narrative",
            Title = "Test Data Point",
            Content = "Test content",
            OwnerId = "user-1",
            Source = "Test source",
            InformationType = "fact",
            CompletenessStatus = "incomplete",
            Deadline = deadline
        };

        var (_, _, dataPoint) = store.CreateDataPoint(request);
        Assert.NotNull(dataPoint);

        var reminderHistory = new ReminderHistory
        {
            Id = Guid.NewGuid().ToString(),
            DataPointId = dataPoint.Id,
            RecipientUserId = "user-1",
            RecipientEmail = "test@example.com",
            SentAt = DateTime.UtcNow.ToString("O"),
            ReminderType = "incomplete",
            DaysUntilDeadline = 3,
            DeadlineDate = deadline,
            EmailSent = true
        };

        // Act
        store.RecordReminderSent(reminderHistory);
        var history = store.GetReminderHistory(dataPoint.Id);

        // Assert
        Assert.Single(history);
        Assert.Equal(dataPoint.Id, history[0].DataPointId);
        Assert.Equal("user-1", history[0].RecipientUserId);
        Assert.Equal(3, history[0].DaysUntilDeadline);
    }

    [Fact]
    public void HasReminderBeenSentToday_ReturnsTrueForTodaysReminder()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        var sectionId = CreateTestSection(store);

        var request = new CreateDataPointRequest
        {
            SectionId = sectionId,
            Type = "narrative",
            Title = "Test Data Point",
            Content = "Test content",
            OwnerId = "user-1",
            Source = "Test source",
            InformationType = "fact",
            CompletenessStatus = "incomplete"
        };

        var (_, _, dataPoint) = store.CreateDataPoint(request);
        Assert.NotNull(dataPoint);

        var reminderHistory = new ReminderHistory
        {
            Id = Guid.NewGuid().ToString(),
            DataPointId = dataPoint.Id,
            RecipientUserId = "user-1",
            RecipientEmail = "test@example.com",
            SentAt = DateTime.UtcNow.ToString("O"),
            ReminderType = "incomplete",
            DaysUntilDeadline = 3,
            EmailSent = true
        };

        store.RecordReminderSent(reminderHistory);

        // Act
        var result = store.HasReminderBeenSentToday(dataPoint.Id, 3);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasReminderBeenSentToday_ReturnsFalseForDifferentDaysUntilDeadline()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        var sectionId = CreateTestSection(store);

        var request = new CreateDataPointRequest
        {
            SectionId = sectionId,
            Type = "narrative",
            Title = "Test Data Point",
            Content = "Test content",
            OwnerId = "user-1",
            Source = "Test source",
            InformationType = "fact",
            CompletenessStatus = "incomplete"
        };

        var (_, _, dataPoint) = store.CreateDataPoint(request);
        Assert.NotNull(dataPoint);

        var reminderHistory = new ReminderHistory
        {
            Id = Guid.NewGuid().ToString(),
            DataPointId = dataPoint.Id,
            RecipientUserId = "user-1",
            RecipientEmail = "test@example.com",
            SentAt = DateTime.UtcNow.ToString("O"),
            ReminderType = "incomplete",
            DaysUntilDeadline = 3,
            EmailSent = true
        };

        store.RecordReminderSent(reminderHistory);

        // Act
        var result = store.HasReminderBeenSentToday(dataPoint.Id, 7); // Different days

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ReminderService_SendsReminderForIncompleteDataPointWithDeadline()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        var snapshot = store.GetSnapshot();
        var periodId = snapshot.Periods.First().Id;
        var sectionId = CreateTestSection(store);

        // Configure reminders for the period
        var config = new ReminderConfiguration
        {
            PeriodId = periodId,
            Enabled = true,
            DaysBeforeDeadline = new List<int> { 7, 3, 1 },
            CheckFrequencyHours = 24
        };
        store.CreateOrUpdateReminderConfiguration(periodId, config);

        // Create incomplete data point with deadline in 3 days
        var deadline = DateTime.UtcNow.AddDays(3).ToString("O");
        var request = new CreateDataPointRequest
        {
            SectionId = sectionId,
            Type = "narrative",
            Title = "Test Data Point",
            Content = "Test content",
            OwnerId = "user-1",
            Source = "Test source",
            InformationType = "fact",
            CompletenessStatus = "incomplete",
            Deadline = deadline
        };
        store.CreateDataPoint(request);

        // Mock email service
        var mockEmailService = new Mock<IEmailService>();
        mockEmailService.Setup(x => x.SendEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(true);

        var mockLogger = new Mock<ILogger<ReminderService>>();
        var reminderService = new ReminderService(store, mockEmailService.Object, mockLogger.Object);

        // Act
        await reminderService.ProcessRemindersAsync(CancellationToken.None);

        // Assert
        mockEmailService.Verify(x => x.SendEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);

        // Verify reminder was recorded
        var history = store.GetReminderHistory();
        Assert.Single(history);
        Assert.Equal("incomplete", history[0].ReminderType);
    }

    [Fact]
    public async Task ReminderService_DoesNotSendReminderForCompletedDataPoint()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        var snapshot = store.GetSnapshot();
        var periodId = snapshot.Periods.First().Id;
        var sectionId = CreateTestSection(store);

        // Configure reminders
        var config = new ReminderConfiguration
        {
            PeriodId = periodId,
            Enabled = true,
            DaysBeforeDeadline = new List<int> { 7, 3, 1 },
            CheckFrequencyHours = 24
        };
        store.CreateOrUpdateReminderConfiguration(periodId, config);

        // Create complete data point
        var deadline = DateTime.UtcNow.AddDays(3).ToString("O");
        var request = new CreateDataPointRequest
        {
            SectionId = sectionId,
            Type = "narrative",
            Title = "Test Data Point",
            Content = "Test content",
            OwnerId = "user-1",
            Source = "Test source",
            InformationType = "fact",
            CompletenessStatus = "complete", // Complete status
            Deadline = deadline
        };
        store.CreateDataPoint(request);

        // Mock email service
        var mockEmailService = new Mock<IEmailService>();
        var mockLogger = new Mock<ILogger<ReminderService>>();
        var reminderService = new ReminderService(store, mockEmailService.Object, mockLogger.Object);

        // Act
        await reminderService.ProcessRemindersAsync(CancellationToken.None);

        // Assert - No email should be sent for completed items
        mockEmailService.Verify(x => x.SendEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }
}
