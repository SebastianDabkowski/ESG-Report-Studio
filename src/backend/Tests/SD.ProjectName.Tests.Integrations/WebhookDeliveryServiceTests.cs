using Moq;
using SD.ProjectName.Modules.Integrations.Application;
using SD.ProjectName.Modules.Integrations.Domain.Entities;
using SD.ProjectName.Modules.Integrations.Domain.Interfaces;

namespace SD.ProjectName.Tests.Integrations;

public class WebhookDeliveryServiceTests
{
    [Fact]
    public async Task DispatchEventAsync_ShouldCreateDeliveriesForAllActiveSubscriptions()
    {
        // Arrange
        var activeSubscriptions = new List<WebhookSubscription>
        {
            new()
            {
                Id = 1,
                Name = "Webhook 1",
                Status = WebhookSubscriptionStatus.Active,
                SubscribedEvents = WebhookEventType.DataChange,
                SigningSecret = "secret1",
                EndpointUrl = "https://example1.com/webhook",
                CreatedBy = "admin",
                MaxRetryAttempts = 3,
                RetryDelaySeconds = 5,
                UseExponentialBackoff = true
            },
            new()
            {
                Id = 2,
                Name = "Webhook 2",
                Status = WebhookSubscriptionStatus.Active,
                SubscribedEvents = WebhookEventType.DataChange,
                SigningSecret = "secret2",
                EndpointUrl = "https://example2.com/webhook",
                CreatedBy = "admin",
                MaxRetryAttempts = 3,
                RetryDelaySeconds = 5,
                UseExponentialBackoff = true
            }
        };

        var mockSubscriptionRepository = new Mock<IWebhookSubscriptionRepository>();
        mockSubscriptionRepository
            .Setup(r => r.GetActiveSubscriptionsForEventAsync(WebhookEventType.DataChange))
            .ReturnsAsync(activeSubscriptions);

        var mockDeliveryRepository = new Mock<IWebhookDeliveryRepository>();
        mockDeliveryRepository
            .Setup(r => r.CreateAsync(It.IsAny<WebhookDelivery>()))
            .ReturnsAsync((WebhookDelivery d) => d);

        var signatureService = new WebhookSignatureService();
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        mockHttpClientFactory
            .Setup(f => f.CreateClient("WebhookClient"))
            .Returns(new HttpClient());

        var service = new WebhookDeliveryService(
            mockSubscriptionRepository.Object,
            mockDeliveryRepository.Object,
            signatureService,
            mockHttpClientFactory.Object);

        var eventData = new { message = "Test data change" };
        var correlationId = "test-correlation-id";

        // Act
        await service.DispatchEventAsync(WebhookEventType.DataChange, eventData, correlationId);

        // Wait a moment for async operations
        await Task.Delay(100);

        // Assert
        mockDeliveryRepository.Verify(
            r => r.CreateAsync(It.Is<WebhookDelivery>(d =>
                d.EventType == WebhookEventType.DataChange &&
                d.CorrelationId == correlationId)),
            Times.Exactly(2));
    }

    [Fact]
    public async Task GetDeliveryHistoryAsync_ShouldReturnDeliveriesForSubscription()
    {
        // Arrange
        var deliveries = new List<WebhookDelivery>
        {
            new()
            {
                Id = 1,
                WebhookSubscriptionId = 1,
                EventType = WebhookEventType.DataChange,
                CorrelationId = "corr-1",
                Payload = "{}",
                Signature = "sig-1",
                Status = WebhookDeliveryStatus.Succeeded
            },
            new()
            {
                Id = 2,
                WebhookSubscriptionId = 1,
                EventType = WebhookEventType.DataChange,
                CorrelationId = "corr-2",
                Payload = "{}",
                Signature = "sig-2",
                Status = WebhookDeliveryStatus.Failed
            }
        };

        var mockSubscriptionRepository = new Mock<IWebhookSubscriptionRepository>();
        var mockDeliveryRepository = new Mock<IWebhookDeliveryRepository>();
        mockDeliveryRepository
            .Setup(r => r.GetBySubscriptionIdAsync(1, 0, 100))
            .ReturnsAsync(deliveries);

        var signatureService = new WebhookSignatureService();
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();

        var service = new WebhookDeliveryService(
            mockSubscriptionRepository.Object,
            mockDeliveryRepository.Object,
            signatureService,
            mockHttpClientFactory.Object);

        // Act
        var result = await service.GetDeliveryHistoryAsync(1);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, d => Assert.Equal(1, d.WebhookSubscriptionId));
    }

    [Fact]
    public async Task GetFailedDeliveriesAsync_ShouldReturnOnlyFailedDeliveries()
    {
        // Arrange
        var failedDeliveries = new List<WebhookDelivery>
        {
            new()
            {
                Id = 1,
                WebhookSubscriptionId = 1,
                EventType = WebhookEventType.DataChange,
                CorrelationId = "corr-1",
                Payload = "{}",
                Signature = "sig-1",
                Status = WebhookDeliveryStatus.Failed,
                LastErrorMessage = "Connection timeout"
            }
        };

        var mockSubscriptionRepository = new Mock<IWebhookSubscriptionRepository>();
        var mockDeliveryRepository = new Mock<IWebhookDeliveryRepository>();
        mockDeliveryRepository
            .Setup(r => r.GetFailedDeliveriesAsync(1, 0, 100))
            .ReturnsAsync(failedDeliveries);

        var signatureService = new WebhookSignatureService();
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();

        var service = new WebhookDeliveryService(
            mockSubscriptionRepository.Object,
            mockDeliveryRepository.Object,
            signatureService,
            mockHttpClientFactory.Object);

        // Act
        var result = await service.GetFailedDeliveriesAsync(1);

        // Assert
        Assert.Single(result);
        Assert.Equal(WebhookDeliveryStatus.Failed, result[0].Status);
        Assert.NotNull(result[0].LastErrorMessage);
    }

    [Fact]
    public async Task ProcessPendingRetriesAsync_ShouldProcessPendingDeliveries()
    {
        // Arrange
        var subscription = new WebhookSubscription
        {
            Id = 1,
            Name = "Test Webhook",
            Status = WebhookSubscriptionStatus.Active,
            SubscribedEvents = WebhookEventType.DataChange,
            SigningSecret = "secret",
            EndpointUrl = "https://example.com/webhook",
            CreatedBy = "admin",
            MaxRetryAttempts = 3,
            RetryDelaySeconds = 5,
            UseExponentialBackoff = true
        };

        var pendingDeliveries = new List<WebhookDelivery>
        {
            new()
            {
                Id = 1,
                WebhookSubscriptionId = 1,
                WebhookSubscription = subscription,
                EventType = WebhookEventType.DataChange,
                CorrelationId = "corr-1",
                Payload = "{}",
                Signature = "sig-1",
                Status = WebhookDeliveryStatus.Retrying,
                NextRetryAt = DateTime.UtcNow.AddSeconds(-10),
                AttemptCount = 1
            }
        };

        var mockSubscriptionRepository = new Mock<IWebhookSubscriptionRepository>();
        var mockDeliveryRepository = new Mock<IWebhookDeliveryRepository>();
        mockDeliveryRepository
            .Setup(r => r.GetPendingRetriesAsync(100))
            .ReturnsAsync(pendingDeliveries);

        var signatureService = new WebhookSignatureService();
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        mockHttpClientFactory
            .Setup(f => f.CreateClient("WebhookClient"))
            .Returns(new HttpClient());

        var service = new WebhookDeliveryService(
            mockSubscriptionRepository.Object,
            mockDeliveryRepository.Object,
            signatureService,
            mockHttpClientFactory.Object);

        // Act
        await service.ProcessPendingRetriesAsync();

        // Wait for async operations
        await Task.Delay(100);

        // Assert
        mockDeliveryRepository.Verify(
            r => r.GetPendingRetriesAsync(100),
            Times.Once);
    }
}
