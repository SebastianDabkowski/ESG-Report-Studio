using Xunit;
using ARP.ESG_ReportStudio.API.Reporting;
using ARP.ESG_ReportStudio.API.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace SD.ProjectName.Tests.Products;

public class EscalationServiceTests
{
    [Fact]
    public async Task ProcessEscalations_ShouldNotEscalate_WhenItemNotOverdue()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var mockEmailService = new Mock<IEmailService>();
        var mockLogger = new Mock<ILogger<EscalationService>>();

        var escalationService = new EscalationService(
            store,
            mockEmailService.Object,
            mockLogger.Object);

        var period = CreateTestPeriod(store);
        CreateEscalationConfig(store, period.Id);
        
        // Create data point with future deadline
        var futureDeadline = DateTime.UtcNow.AddDays(5).ToString("O");
        CreateTestDataPoint(store, period.Id, "dp-1", "incomplete", futureDeadline);

        // Act
        await escalationService.ProcessEscalationsAsync(CancellationToken.None);

        // Assert - no emails should be sent for future deadline
        mockEmailService.Verify(
            x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessEscalations_ShouldNotEscalate_WhenItemCompleted()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var mockEmailService = new Mock<IEmailService>();
        var mockLogger = new Mock<ILogger<EscalationService>>();

        var escalationService = new EscalationService(
            store,
            mockEmailService.Object,
            mockLogger.Object);

        var period = CreateTestPeriod(store);
        CreateEscalationConfig(store, period.Id);
        
        // Create completed data point with overdue deadline
        var pastDeadline = DateTime.UtcNow.AddDays(-3).ToString("O");
        CreateTestDataPoint(store, period.Id, "dp-1", "complete", pastDeadline);

        // Act
        await escalationService.ProcessEscalationsAsync(CancellationToken.None);

        // Assert - no emails should be sent for completed items
        mockEmailService.Verify(
            x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessEscalations_ShouldEscalate_WhenItemOverdueByConfiguredDays()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var mockEmailService = new Mock<IEmailService>();
        var mockLogger = new Mock<ILogger<EscalationService>>();

        mockEmailService
            .Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var escalationService = new EscalationService(
            store,
            mockEmailService.Object,
            mockLogger.Object);

        var period = CreateTestPeriod(store);
        CreateEscalationConfig(store, period.Id, new List<int> { 3, 7 }); // Escalate at 3 and 7 days
        
        // Create data point that is exactly 3 days overdue
        var deadline = DateTime.UtcNow.AddDays(-3).ToString("O");
        CreateTestDataPoint(store, period.Id, "dp-1", "incomplete", deadline);

        // Act
        await escalationService.ProcessEscalationsAsync(CancellationToken.None);

        // Assert - should send 2 emails (owner + admin)
        mockEmailService.Verify(
            x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Exactly(2));

        // Verify owner email
        mockEmailService.Verify(
            x => x.SendEmailAsync(
                "john.smith@company.com",
                "John Smith",
                It.Is<string>(s => s.Contains("OVERDUE")),
                It.Is<string>(s => s.Contains("3 day(s) OVERDUE"))),
            Times.Once);

        // Verify admin email
        mockEmailService.Verify(
            x => x.SendEmailAsync(
                "sarah.chen@company.com",
                "Sarah Chen",
                It.Is<string>(s => s.Contains("ESCALATION")),
                It.Is<string>(s => s.Contains("3 day(s) OVERDUE"))),
            Times.Once);
    }

    [Fact]
    public async Task ProcessEscalations_ShouldRecordHistory_WhenEscalationSent()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var mockEmailService = new Mock<IEmailService>();
        var mockLogger = new Mock<ILogger<EscalationService>>();

        mockEmailService
            .Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var escalationService = new EscalationService(
            store,
            mockEmailService.Object,
            mockLogger.Object);

        var period = CreateTestPeriod(store);
        CreateEscalationConfig(store, period.Id, new List<int> { 3 });
        
        var deadline = DateTime.UtcNow.AddDays(-3).ToString("O");
        var dataPointId = CreateTestDataPoint(store, period.Id, "dp-1", "incomplete", deadline);

        // Act
        await escalationService.ProcessEscalationsAsync(CancellationToken.None);

        // Assert
        var history = store.GetEscalationHistory(dataPointId);
        Assert.Single(history);
        
        var record = history[0];
        Assert.Equal(dataPointId, record.DataPointId);
        Assert.Equal(3, record.DaysOverdue);
        Assert.Equal("user-3", record.OwnerUserId);
        Assert.Equal("john.smith@company.com", record.OwnerEmail);
        Assert.Equal("user-1", record.EscalatedToUserId);
        Assert.Equal("sarah.chen@company.com", record.EscalatedToEmail);
        Assert.True(record.OwnerEmailSent);
        Assert.True(record.AdminEmailSent);
        Assert.Null(record.ErrorMessage);
    }

    [Fact]
    public async Task ProcessEscalations_ShouldNotDuplicateEscalation_WhenAlreadySentToday()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var mockEmailService = new Mock<IEmailService>();
        var mockLogger = new Mock<ILogger<EscalationService>>();

        mockEmailService
            .Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var escalationService = new EscalationService(
            store,
            mockEmailService.Object,
            mockLogger.Object);

        var period = CreateTestPeriod(store);
        CreateEscalationConfig(store, period.Id, new List<int> { 3 });
        
        var deadline = DateTime.UtcNow.AddDays(-3).ToString("O");
        var dataPointId = CreateTestDataPoint(store, period.Id, "dp-1", "incomplete", deadline);

        // Act - process escalations twice
        await escalationService.ProcessEscalationsAsync(CancellationToken.None);
        await escalationService.ProcessEscalationsAsync(CancellationToken.None);

        // Assert - should only send emails once
        mockEmailService.Verify(
            x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Exactly(2)); // 2 emails from first run only

        var history = store.GetEscalationHistory(dataPointId);
        Assert.Single(history); // Only one escalation record
    }

    [Fact]
    public async Task ProcessEscalations_ShouldNotEscalate_WhenDisabled()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var mockEmailService = new Mock<IEmailService>();
        var mockLogger = new Mock<ILogger<EscalationService>>();

        var escalationService = new EscalationService(
            store,
            mockEmailService.Object,
            mockLogger.Object);

        var period = CreateTestPeriod(store);
        // Create disabled config
        CreateEscalationConfig(store, period.Id, new List<int> { 3 }, enabled: false);
        
        var deadline = DateTime.UtcNow.AddDays(-3).ToString("O");
        CreateTestDataPoint(store, period.Id, "dp-1", "incomplete", deadline);

        // Act
        await escalationService.ProcessEscalationsAsync(CancellationToken.None);

        // Assert - no emails should be sent when disabled
        mockEmailService.Verify(
            x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessEscalations_ShouldNotEscalate_WhenNotMatchingConfiguredDays()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var mockEmailService = new Mock<IEmailService>();
        var mockLogger = new Mock<ILogger<EscalationService>>();

        var escalationService = new EscalationService(
            store,
            mockEmailService.Object,
            mockLogger.Object);

        var period = CreateTestPeriod(store);
        CreateEscalationConfig(store, period.Id, new List<int> { 3, 7 }); // Only 3 and 7 days
        
        // Create data point that is 5 days overdue (not in configured days)
        var deadline = DateTime.UtcNow.AddDays(-5).ToString("O");
        CreateTestDataPoint(store, period.Id, "dp-1", "incomplete", deadline);

        // Act
        await escalationService.ProcessEscalationsAsync(CancellationToken.None);

        // Assert - no emails should be sent for non-configured days
        mockEmailService.Verify(
            x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessEscalations_ShouldSendOnlyOwnerEmail_WhenOwnerIsAdmin()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var mockEmailService = new Mock<IEmailService>();
        var mockLogger = new Mock<ILogger<EscalationService>>();

        mockEmailService
            .Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var escalationService = new EscalationService(
            store,
            mockEmailService.Object,
            mockLogger.Object);

        var period = CreateTestPeriod(store);
        CreateEscalationConfig(store, period.Id, new List<int> { 3 });
        
        // Create data point where owner is the same as period owner (admin)
        var deadline = DateTime.UtcNow.AddDays(-3).ToString("O");
        var dataPointId = CreateTestDataPoint(store, period.Id, "dp-1", "incomplete", deadline, ownerId: "user-1");

        // Act
        await escalationService.ProcessEscalationsAsync(CancellationToken.None);

        // Assert - should only send 1 email (to owner who is also admin)
        mockEmailService.Verify(
            x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);

        var history = store.GetEscalationHistory(dataPointId);
        Assert.Single(history);
        var record = history[0];
        Assert.Null(record.EscalatedToUserId); // No separate admin since owner is admin
        Assert.Null(record.EscalatedToEmail);
    }

    // Helper methods

    private ReportingPeriod CreateTestPeriod(InMemoryReportStore store)
    {
        // Create organization if not exists
        var snapshot = store.GetSnapshot();
        if (snapshot.Organization == null)
        {
            store.CreateOrganization(new CreateOrganizationRequest
            {
                Name = "Test Company",
                LegalForm = "LLC",
                Country = "US",
                Identifier = "12345",
                CreatedBy = "test-user",
                CoverageType = "full",
                CoverageJustification = "Test coverage"
            });
        }

        // Create organizational unit if not exists
        snapshot = store.GetSnapshot();
        if (!snapshot.OrganizationalUnits.Any())
        {
            store.CreateOrganizationalUnit(new CreateOrganizationalUnitRequest
            {
                Name = "Test Organization Unit",
                Description = "Default unit for testing",
                CreatedBy = "test-user"
            });
        }

        // Create reporting period
        var (isValid, errorMessage, snapshot2) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
        {
            Name = "2024 ESG Report",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user-1", // Sarah Chen (report-owner)
            OwnerName = "Sarah Chen"
        });

        if (!isValid || snapshot2 == null)
        {
            throw new InvalidOperationException($"Failed to create period: {errorMessage}");
        }

        return snapshot2.Periods.First();
    }

    private void CreateEscalationConfig(InMemoryReportStore store, string periodId, List<int>? daysAfterDeadline = null, bool enabled = true)
    {
        var config = new EscalationConfiguration
        {
            PeriodId = periodId,
            Enabled = enabled,
            DaysAfterDeadline = daysAfterDeadline ?? new List<int> { 3, 7 }
        };

        store.CreateOrUpdateEscalationConfiguration(periodId, config);
    }

    private string CreateTestDataPoint(InMemoryReportStore store, string periodId, string dpId, string status, string deadline, string ownerId = "user-3")
    {
        // Get or create a section using reflection
        var snapshot = store.GetSnapshot();
        var section = snapshot.Sections.FirstOrDefault(s => s.PeriodId == periodId);
        if (section == null)
        {
            section = new ReportSection
            {
                Id = Guid.NewGuid().ToString(),
                PeriodId = periodId,
                Title = "Test Section",
                Category = "environmental",
                OwnerId = "user-1",
                Status = "draft",
                Completeness = "empty",
                Order = 1
            };
            
            // Use reflection to add section directly
            var sectionsField = typeof(InMemoryReportStore).GetField("_sections", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sections = sectionsField!.GetValue(store) as List<ReportSection>;
            sections!.Add(section);
        }

        // Create data point
        var createRequest = new CreateDataPointRequest
        {
            SectionId = section.Id,
            Type = "narrative",
            Title = "Test Data Point",
            Content = "Test content",
            OwnerId = ownerId,
            CompletenessStatus = status,
            Deadline = deadline,
            Source = "Test",
            InformationType = "fact"
        };

        var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
        return dataPoint!.Id;
    }
}
