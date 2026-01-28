using Xunit;
using ARP.ESG_ReportStudio.API.Reporting;
using ARP.ESG_ReportStudio.API.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace SD.ProjectName.Tests.Products;

public class EscalationsControllerTests
{
    [Fact]
    public void GetConfiguration_WhenNoConfigExists_ShouldReturnDefaultConfiguration()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var controller = new EscalationsController(store);
        var periodId = "period-1";

        // Act
        var result = controller.GetConfiguration(periodId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var config = Assert.IsType<EscalationConfiguration>(okResult.Value);
        Assert.Equal(periodId, config.PeriodId);
        Assert.True(config.Enabled);
        Assert.Equal(new List<int> { 3, 7 }, config.DaysAfterDeadline);
    }

    [Fact]
    public void UpdateConfiguration_WithValidData_ShouldSucceed()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var controller = new EscalationsController(store);
        var periodId = "period-1";
        var config = new EscalationConfiguration
        {
            Enabled = true,
            DaysAfterDeadline = new List<int> { 2, 5, 10 }
        };

        // Act
        var result = controller.UpdateConfiguration(periodId, config);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedConfig = Assert.IsType<EscalationConfiguration>(okResult.Value);
        Assert.Equal(periodId, returnedConfig.PeriodId);
        Assert.True(returnedConfig.Enabled);
        Assert.Equal(new List<int> { 2, 5, 10 }, returnedConfig.DaysAfterDeadline);
        Assert.NotEmpty(returnedConfig.Id);
        Assert.NotEmpty(returnedConfig.CreatedAt);
        Assert.NotEmpty(returnedConfig.UpdatedAt);
    }

    [Fact]
    public void UpdateConfiguration_WithEmptyDaysAfterDeadline_ShouldReturnBadRequest()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var controller = new EscalationsController(store);
        var periodId = "period-1";
        var config = new EscalationConfiguration
        {
            Enabled = true,
            DaysAfterDeadline = new List<int>() // Empty list
        };

        // Act
        var result = controller.UpdateConfiguration(periodId, config);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public void UpdateConfiguration_WithNegativeDays_ShouldReturnBadRequest()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var controller = new EscalationsController(store);
        var periodId = "period-1";
        var config = new EscalationConfiguration
        {
            Enabled = true,
            DaysAfterDeadline = new List<int> { -1, 3 } // Negative value
        };

        // Act
        var result = controller.UpdateConfiguration(periodId, config);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public void UpdateConfiguration_WithZeroDays_ShouldReturnBadRequest()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var controller = new EscalationsController(store);
        var periodId = "period-1";
        var config = new EscalationConfiguration
        {
            Enabled = true,
            DaysAfterDeadline = new List<int> { 0, 3 } // Zero is not allowed (must be positive)
        };

        // Act
        var result = controller.UpdateConfiguration(periodId, config);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public void GetHistory_WhenNoHistory_ShouldReturnEmptyList()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var controller = new EscalationsController(store);

        // Act
        var result = controller.GetHistory();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var history = Assert.IsAssignableFrom<IReadOnlyList<EscalationHistory>>(okResult.Value);
        Assert.Empty(history);
    }

    [Fact]
    public void GetHistory_FilterByDataPointId_ShouldReturnMatchingRecords()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var controller = new EscalationsController(store);
        
        // Add some escalation history
        store.RecordEscalationSent(new EscalationHistory
        {
            Id = "esc-1",
            DataPointId = "dp-1",
            OwnerUserId = "user-1",
            OwnerEmail = "user1@test.com",
            SentAt = DateTime.UtcNow.ToString("O"),
            DaysOverdue = 3,
            DeadlineDate = DateTime.UtcNow.AddDays(-3).ToString("O"),
            OwnerEmailSent = true,
            AdminEmailSent = true
        });
        
        store.RecordEscalationSent(new EscalationHistory
        {
            Id = "esc-2",
            DataPointId = "dp-2",
            OwnerUserId = "user-2",
            OwnerEmail = "user2@test.com",
            SentAt = DateTime.UtcNow.ToString("O"),
            DaysOverdue = 5,
            DeadlineDate = DateTime.UtcNow.AddDays(-5).ToString("O"),
            OwnerEmailSent = true,
            AdminEmailSent = true
        });

        // Act
        var result = controller.GetHistory(dataPointId: "dp-1");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var history = Assert.IsAssignableFrom<IReadOnlyList<EscalationHistory>>(okResult.Value);
        Assert.Single(history);
        Assert.Equal("dp-1", history[0].DataPointId);
    }

    [Fact]
    public void GetHistory_FilterByUserId_ShouldReturnMatchingRecords()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var controller = new EscalationsController(store);
        
        // Add escalation history
        store.RecordEscalationSent(new EscalationHistory
        {
            Id = "esc-1",
            DataPointId = "dp-1",
            OwnerUserId = "user-1",
            OwnerEmail = "user1@test.com",
            EscalatedToUserId = "admin-1",
            EscalatedToEmail = "admin1@test.com",
            SentAt = DateTime.UtcNow.ToString("O"),
            DaysOverdue = 3,
            DeadlineDate = DateTime.UtcNow.AddDays(-3).ToString("O"),
            OwnerEmailSent = true,
            AdminEmailSent = true
        });
        
        store.RecordEscalationSent(new EscalationHistory
        {
            Id = "esc-2",
            DataPointId = "dp-2",
            OwnerUserId = "user-2",
            OwnerEmail = "user2@test.com",
            SentAt = DateTime.UtcNow.ToString("O"),
            DaysOverdue = 5,
            DeadlineDate = DateTime.UtcNow.AddDays(-5).ToString("O"),
            OwnerEmailSent = true,
            AdminEmailSent = true
        });

        // Act - filter by owner user ID
        var result1 = controller.GetHistory(userId: "user-1");
        var okResult1 = Assert.IsType<OkObjectResult>(result1.Result);
        var history1 = Assert.IsAssignableFrom<IReadOnlyList<EscalationHistory>>(okResult1.Value);
        
        // Act - filter by escalated-to user ID
        var result2 = controller.GetHistory(userId: "admin-1");
        var okResult2 = Assert.IsType<OkObjectResult>(result2.Result);
        var history2 = Assert.IsAssignableFrom<IReadOnlyList<EscalationHistory>>(okResult2.Value);

        // Assert
        Assert.Single(history1);
        Assert.Equal("user-1", history1[0].OwnerUserId);
        
        Assert.Single(history2);
        Assert.Equal("admin-1", history2[0].EscalatedToUserId);
    }
}
