using Xunit;
using ARP.ESG_ReportStudio.API.Reporting;
using ARP.ESG_ReportStudio.API.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace SD.ProjectName.Tests.Products;

public class NotificationServiceTests
{
    [Fact]
    public async Task SendSectionAssignedNotification_ShouldCreateNotificationAndSendEmail()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var mockEmailService = new Mock<IEmailService>();
        var mockLogger = new Mock<ILogger<OwnerAssignmentNotificationService>>();
        
        mockEmailService
            .Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var notificationService = new OwnerAssignmentNotificationService(
            store, 
            mockEmailService.Object, 
            mockLogger.Object);

        var section = new ReportSection
        {
            Id = "section-1",
            Title = "Energy & Emissions",
            Category = "environmental"
        };

        var newOwner = new User
        {
            Id = "user-1",
            Name = "John Doe",
            Email = "john.doe@company.com",
            Role = "contributor"
        };

        var changedBy = new User
        {
            Id = "user-2",
            Name = "Admin User",
            Email = "admin@company.com",
            Role = "admin"
        };

        // Act
        await notificationService.SendSectionAssignedNotificationAsync(
            section, newOwner, changedBy, "Initial assignment");

        // Assert
        var notifications = store.GetNotifications(newOwner.Id);
        Assert.Single(notifications);
        
        var notification = notifications[0];
        Assert.Equal("user-1", notification.RecipientUserId);
        Assert.Equal("section-assigned", notification.NotificationType);
        Assert.Equal("section-1", notification.EntityId);
        Assert.Equal("ReportSection", notification.EntityType);
        Assert.Equal("Energy & Emissions", notification.EntityTitle);
        Assert.Equal("user-2", notification.ChangedBy);
        Assert.Equal("Admin User", notification.ChangedByName);
        Assert.False(notification.IsRead);
        Assert.True(notification.EmailSent);

        // Verify email was sent
        mockEmailService.Verify(
            x => x.SendEmailAsync(
                "john.doe@company.com",
                "John Doe",
                It.Is<string>(s => s.Contains("Energy & Emissions")),
                It.Is<string>(s => s.Contains("assigned as the owner"))),
            Times.Once);
    }

    [Fact]
    public async Task SendSectionRemovedNotification_ShouldCreateNotificationAndSendEmail()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var mockEmailService = new Mock<IEmailService>();
        var mockLogger = new Mock<ILogger<OwnerAssignmentNotificationService>>();
        
        mockEmailService
            .Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var notificationService = new OwnerAssignmentNotificationService(
            store, 
            mockEmailService.Object, 
            mockLogger.Object);

        var section = new ReportSection
        {
            Id = "section-1",
            Title = "Waste & Recycling",
            Category = "environmental"
        };

        var previousOwner = new User
        {
            Id = "user-3",
            Name = "Jane Smith",
            Email = "jane.smith@company.com",
            Role = "contributor"
        };

        var changedBy = new User
        {
            Id = "user-2",
            Name = "Admin User",
            Email = "admin@company.com",
            Role = "admin"
        };

        // Act
        await notificationService.SendSectionRemovedNotificationAsync(
            section, previousOwner, changedBy, "Reassigning to another team member");

        // Assert
        var notifications = store.GetNotifications(previousOwner.Id);
        Assert.Single(notifications);
        
        var notification = notifications[0];
        Assert.Equal("user-3", notification.RecipientUserId);
        Assert.Equal("section-removed", notification.NotificationType);
        Assert.Equal("section-1", notification.EntityId);
        Assert.Equal("ReportSection", notification.EntityType);
        Assert.Equal("Waste & Recycling", notification.EntityTitle);
        Assert.False(notification.IsRead);
        Assert.True(notification.EmailSent);

        // Verify email was sent
        mockEmailService.Verify(
            x => x.SendEmailAsync(
                "jane.smith@company.com",
                "Jane Smith",
                It.Is<string>(s => s.Contains("Waste & Recycling")),
                It.Is<string>(s => s.Contains("removed as the owner"))),
            Times.Once);
    }

    [Fact]
    public async Task SendDataPointAssignedNotification_ShouldCreateNotificationAndSendEmail()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var mockEmailService = new Mock<IEmailService>();
        var mockLogger = new Mock<ILogger<OwnerAssignmentNotificationService>>();
        
        mockEmailService
            .Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var notificationService = new OwnerAssignmentNotificationService(
            store, 
            mockEmailService.Object, 
            mockLogger.Object);

        var dataPoint = new DataPoint
        {
            Id = "dp-1",
            Title = "Total GHG Emissions",
            CompletenessStatus = "incomplete"
        };

        var newOwner = new User
        {
            Id = "user-4",
            Name = "Bob Johnson",
            Email = "bob.johnson@company.com",
            Role = "contributor"
        };

        var changedBy = new User
        {
            Id = "user-1",
            Name = "Report Owner",
            Email = "report.owner@company.com",
            Role = "report-owner"
        };

        // Act
        await notificationService.SendDataPointAssignedNotificationAsync(
            dataPoint, newOwner, changedBy);

        // Assert
        var notifications = store.GetNotifications(newOwner.Id);
        Assert.Single(notifications);
        
        var notification = notifications[0];
        Assert.Equal("user-4", notification.RecipientUserId);
        Assert.Equal("datapoint-assigned", notification.NotificationType);
        Assert.Equal("dp-1", notification.EntityId);
        Assert.Equal("DataPoint", notification.EntityType);
        Assert.Equal("Total GHG Emissions", notification.EntityTitle);
        Assert.False(notification.IsRead);
        Assert.True(notification.EmailSent);

        // Verify email was sent
        mockEmailService.Verify(
            x => x.SendEmailAsync(
                "bob.johnson@company.com",
                "Bob Johnson",
                It.IsAny<string>(),
                It.Is<string>(s => s.Contains("assigned as the owner"))),
            Times.Once);
    }

    [Fact]
    public void GetNotifications_ShouldReturnOnlyUnreadWhenFlagSet()
    {
        // Arrange
        var store = new InMemoryReportStore();
        
        // Create and record notifications
        var notification1 = new OwnerNotification
        {
            Id = "notif-1",
            RecipientUserId = "user-1",
            NotificationType = "section-assigned",
            EntityId = "section-1",
            EntityType = "ReportSection",
            EntityTitle = "Test Section 1",
            Message = "Test message",
            ChangedBy = "user-2",
            ChangedByName = "Admin",
            CreatedAt = DateTime.UtcNow.ToString("O"),
            IsRead = false,
            EmailSent = true
        };

        var notification2 = new OwnerNotification
        {
            Id = "notif-2",
            RecipientUserId = "user-1",
            NotificationType = "section-removed",
            EntityId = "section-2",
            EntityType = "ReportSection",
            EntityTitle = "Test Section 2",
            Message = "Test message 2",
            ChangedBy = "user-2",
            ChangedByName = "Admin",
            CreatedAt = DateTime.UtcNow.AddMinutes(-10).ToString("O"),
            IsRead = true,
            EmailSent = true
        };

        store.RecordNotification(notification1);
        store.RecordNotification(notification2);

        // Act
        var allNotifications = store.GetNotifications("user-1", unreadOnly: false);
        var unreadNotifications = store.GetNotifications("user-1", unreadOnly: true);

        // Assert
        Assert.Equal(2, allNotifications.Count);
        Assert.Single(unreadNotifications);
        Assert.Equal("notif-1", unreadNotifications[0].Id);
        Assert.False(unreadNotifications[0].IsRead);
    }

    [Fact]
    public void MarkNotificationAsRead_ShouldUpdateIsReadFlag()
    {
        // Arrange
        var store = new InMemoryReportStore();
        
        var notification = new OwnerNotification
        {
            Id = "notif-1",
            RecipientUserId = "user-1",
            NotificationType = "section-assigned",
            EntityId = "section-1",
            EntityType = "ReportSection",
            EntityTitle = "Test Section",
            Message = "Test message",
            ChangedBy = "user-2",
            ChangedByName = "Admin",
            CreatedAt = DateTime.UtcNow.ToString("O"),
            IsRead = false,
            EmailSent = true
        };

        store.RecordNotification(notification);

        // Act
        var success = store.MarkNotificationAsRead("notif-1");

        // Assert
        Assert.True(success);
        var updatedNotifications = store.GetNotifications("user-1");
        Assert.Single(updatedNotifications);
        Assert.True(updatedNotifications[0].IsRead);
    }

    [Fact]
    public void MarkNotificationAsRead_ShouldReturnFalseForNonexistentNotification()
    {
        // Arrange
        var store = new InMemoryReportStore();

        // Act
        var success = store.MarkNotificationAsRead("nonexistent-id");

        // Assert
        Assert.False(success);
    }
}
