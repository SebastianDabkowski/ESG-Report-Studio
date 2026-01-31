using Moq;
using SD.ProjectName.Modules.Integrations.Application;
using SD.ProjectName.Modules.Integrations.Domain.Entities;
using SD.ProjectName.Modules.Integrations.Domain.Interfaces;

namespace SD.ProjectName.Tests.Integrations;

public class WebhookSubscriptionServiceTests
{
    [Fact]
    public async Task CreateSubscriptionAsync_ShouldCreateSubscriptionWithPendingVerificationStatus()
    {
        // Arrange
        var mockRepository = new Mock<IWebhookSubscriptionRepository>();
        mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<WebhookSubscription>()))
            .ReturnsAsync((WebhookSubscription s) => s);

        var signatureService = new WebhookSignatureService();
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        mockHttpClientFactory
            .Setup(f => f.CreateClient("WebhookClient"))
            .Returns(new HttpClient());

        var service = new WebhookSubscriptionService(
            mockRepository.Object,
            signatureService,
            mockHttpClientFactory.Object);

        // Act
        var subscription = await service.CreateSubscriptionAsync(
            "Test Webhook",
            "https://example.com/webhook",
            new[] { WebhookEventType.DataChange, WebhookEventType.ApprovalGranted },
            "admin");

        // Assert
        Assert.Equal("Test Webhook", subscription.Name);
        Assert.Equal("https://example.com/webhook", subscription.EndpointUrl);
        Assert.Contains(WebhookEventType.DataChange, subscription.SubscribedEvents);
        Assert.Contains(WebhookEventType.ApprovalGranted, subscription.SubscribedEvents);
        Assert.Equal(WebhookSubscriptionStatus.PendingVerification, subscription.Status);
        Assert.NotEmpty(subscription.SigningSecret);
        Assert.NotEmpty(subscription.VerificationToken ?? "");
        Assert.Equal("admin", subscription.CreatedBy);
    }

    [Fact]
    public async Task CreateSubscriptionAsync_ShouldThrowExceptionForInvalidEventType()
    {
        // Arrange
        var mockRepository = new Mock<IWebhookSubscriptionRepository>();
        var signatureService = new WebhookSignatureService();
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        mockHttpClientFactory
            .Setup(f => f.CreateClient("WebhookClient"))
            .Returns(new HttpClient());

        var service = new WebhookSubscriptionService(
            mockRepository.Object,
            signatureService,
            mockHttpClientFactory.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.CreateSubscriptionAsync(
                "Test Webhook",
                "https://example.com/webhook",
                new[] { "invalid.event" },
                "admin"));
    }

    [Fact]
    public async Task ActivateSubscriptionAsync_ShouldSetStatusToActive()
    {
        // Arrange
        var subscription = new WebhookSubscription
        {
            Id = 1,
            Name = "Test Webhook",
            EndpointUrl = "https://example.com/webhook",
            SubscribedEvents = WebhookEventType.DataChange,
            Status = WebhookSubscriptionStatus.Paused,
            SigningSecret = "test-secret",
            CreatedBy = "admin",
            ConsecutiveFailures = 3
        };

        var mockRepository = new Mock<IWebhookSubscriptionRepository>();
        mockRepository
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(subscription);
        mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<WebhookSubscription>()))
            .ReturnsAsync((WebhookSubscription s) => s);

        var signatureService = new WebhookSignatureService();
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        mockHttpClientFactory
            .Setup(f => f.CreateClient("WebhookClient"))
            .Returns(new HttpClient());

        var service = new WebhookSubscriptionService(
            mockRepository.Object,
            signatureService,
            mockHttpClientFactory.Object);

        // Act
        var result = await service.ActivateSubscriptionAsync(1, "admin");

        // Assert
        Assert.Equal(WebhookSubscriptionStatus.Active, result.Status);
        Assert.Equal(0, result.ConsecutiveFailures); // Should reset failures
        Assert.Null(result.DegradedAt);
        Assert.Null(result.DegradedReason);
        Assert.Equal("admin", result.UpdatedBy);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task PauseSubscriptionAsync_ShouldSetStatusToPaused()
    {
        // Arrange
        var subscription = new WebhookSubscription
        {
            Id = 1,
            Name = "Test Webhook",
            EndpointUrl = "https://example.com/webhook",
            SubscribedEvents = WebhookEventType.DataChange,
            Status = WebhookSubscriptionStatus.Active,
            SigningSecret = "test-secret",
            CreatedBy = "admin"
        };

        var mockRepository = new Mock<IWebhookSubscriptionRepository>();
        mockRepository
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(subscription);
        mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<WebhookSubscription>()))
            .ReturnsAsync((WebhookSubscription s) => s);

        var signatureService = new WebhookSignatureService();
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        mockHttpClientFactory
            .Setup(f => f.CreateClient("WebhookClient"))
            .Returns(new HttpClient());

        var service = new WebhookSubscriptionService(
            mockRepository.Object,
            signatureService,
            mockHttpClientFactory.Object);

        // Act
        var result = await service.PauseSubscriptionAsync(1, "admin");

        // Assert
        Assert.Equal(WebhookSubscriptionStatus.Paused, result.Status);
        Assert.Equal("admin", result.UpdatedBy);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task RotateSigningSecretAsync_ShouldGenerateNewSecret()
    {
        // Arrange
        var oldSecret = "old-secret";
        var subscription = new WebhookSubscription
        {
            Id = 1,
            Name = "Test Webhook",
            EndpointUrl = "https://example.com/webhook",
            SubscribedEvents = WebhookEventType.DataChange,
            Status = WebhookSubscriptionStatus.Active,
            SigningSecret = oldSecret,
            CreatedBy = "admin"
        };

        var mockRepository = new Mock<IWebhookSubscriptionRepository>();
        mockRepository
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(subscription);
        mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<WebhookSubscription>()))
            .ReturnsAsync((WebhookSubscription s) => s);

        var signatureService = new WebhookSignatureService();
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        mockHttpClientFactory
            .Setup(f => f.CreateClient("WebhookClient"))
            .Returns(new HttpClient());

        var service = new WebhookSubscriptionService(
            mockRepository.Object,
            signatureService,
            mockHttpClientFactory.Object);

        // Act
        var result = await service.RotateSigningSecretAsync(1, "admin");

        // Assert
        Assert.NotEqual(oldSecret, result.SigningSecret);
        Assert.NotEmpty(result.SigningSecret);
        Assert.NotNull(result.SecretRotatedAt);
        Assert.Equal("admin", result.UpdatedBy);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task MarkAsDegradedAsync_ShouldSetStatusToDegraded()
    {
        // Arrange
        var subscription = new WebhookSubscription
        {
            Id = 1,
            Name = "Test Webhook",
            EndpointUrl = "https://example.com/webhook",
            SubscribedEvents = WebhookEventType.DataChange,
            Status = WebhookSubscriptionStatus.Active,
            SigningSecret = "test-secret",
            CreatedBy = "admin"
        };

        var mockRepository = new Mock<IWebhookSubscriptionRepository>();
        mockRepository
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(subscription);
        mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<WebhookSubscription>()))
            .ReturnsAsync((WebhookSubscription s) => s);

        var signatureService = new WebhookSignatureService();
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        mockHttpClientFactory
            .Setup(f => f.CreateClient("WebhookClient"))
            .Returns(new HttpClient());

        var service = new WebhookSubscriptionService(
            mockRepository.Object,
            signatureService,
            mockHttpClientFactory.Object);

        var reason = "Too many consecutive failures";

        // Act
        var result = await service.MarkAsDegradedAsync(1, reason);

        // Assert
        Assert.Equal(WebhookSubscriptionStatus.Degraded, result.Status);
        Assert.NotNull(result.DegradedAt);
        Assert.Equal(reason, result.DegradedReason);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task GetActiveSubscriptionsForEventAsync_ShouldReturnOnlyActiveSubscriptions()
    {
        // Arrange
        var activeSubscription = new WebhookSubscription
        {
            Id = 1,
            Name = "Active Webhook",
            Status = WebhookSubscriptionStatus.Active,
            SubscribedEvents = WebhookEventType.DataChange,
            SigningSecret = "secret",
            EndpointUrl = "https://example.com/webhook",
            CreatedBy = "admin"
        };

        var mockRepository = new Mock<IWebhookSubscriptionRepository>();
        mockRepository
            .Setup(r => r.GetActiveSubscriptionsForEventAsync(WebhookEventType.DataChange))
            .ReturnsAsync(new List<WebhookSubscription> { activeSubscription });

        var signatureService = new WebhookSignatureService();
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        mockHttpClientFactory
            .Setup(f => f.CreateClient("WebhookClient"))
            .Returns(new HttpClient());

        var service = new WebhookSubscriptionService(
            mockRepository.Object,
            signatureService,
            mockHttpClientFactory.Object);

        // Act
        var result = await service.GetActiveSubscriptionsForEventAsync(WebhookEventType.DataChange);

        // Assert
        Assert.Single(result);
        Assert.Equal(WebhookSubscriptionStatus.Active, result[0].Status);
    }
}
