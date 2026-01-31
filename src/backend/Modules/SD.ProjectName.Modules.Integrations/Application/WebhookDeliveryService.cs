using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Http;
using SD.ProjectName.Modules.Integrations.Domain.Entities;
using SD.ProjectName.Modules.Integrations.Domain.Interfaces;

namespace SD.ProjectName.Modules.Integrations.Application;

/// <summary>
/// Application service for delivering webhooks
/// </summary>
public class WebhookDeliveryService
{
    private readonly IWebhookSubscriptionRepository _subscriptionRepository;
    private readonly IWebhookDeliveryRepository _deliveryRepository;
    private readonly WebhookSignatureService _signatureService;
    private readonly HttpClient _httpClient;

    public WebhookDeliveryService(
        IWebhookSubscriptionRepository subscriptionRepository,
        IWebhookDeliveryRepository deliveryRepository,
        WebhookSignatureService signatureService,
        IHttpClientFactory httpClientFactory)
    {
        _subscriptionRepository = subscriptionRepository;
        _deliveryRepository = deliveryRepository;
        _signatureService = signatureService;
        _httpClient = httpClientFactory.CreateClient("WebhookClient");
    }

    /// <summary>
    /// Dispatch a webhook event to all active subscriptions
    /// </summary>
    public async Task DispatchEventAsync(string eventType, object eventData, string correlationId)
    {
        // Get active subscriptions for this event type
        var subscriptions = await _subscriptionRepository.GetActiveSubscriptionsForEventAsync(eventType);
        
        foreach (var subscription in subscriptions)
        {
            await CreateDeliveryAsync(subscription, eventType, eventData, correlationId);
        }
    }

    /// <summary>
    /// Create a webhook delivery and attempt immediate delivery
    /// </summary>
    private async Task<WebhookDelivery> CreateDeliveryAsync(
        WebhookSubscription subscription,
        string eventType,
        object eventData,
        string correlationId)
    {
        // Build payload
        var payload = new
        {
            @event = eventType,
            timestamp = DateTime.UtcNow.ToString("o"),
            correlationId = correlationId,
            data = eventData
        };

        var payloadJson = JsonSerializer.Serialize(payload);
        var signature = _signatureService.GenerateSignature(payloadJson, subscription.SigningSecret);

        var delivery = new WebhookDelivery
        {
            WebhookSubscriptionId = subscription.Id,
            EventType = eventType,
            CorrelationId = correlationId,
            Payload = payloadJson,
            Signature = signature,
            Status = WebhookDeliveryStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _deliveryRepository.CreateAsync(delivery);
        
        // Attempt delivery asynchronously
        _ = Task.Run(async () => await AttemptDeliveryAsync(created.Id));
        
        return created;
    }

    /// <summary>
    /// Attempt to deliver a webhook
    /// </summary>
    public async Task<bool> AttemptDeliveryAsync(int deliveryId)
    {
        var delivery = await _deliveryRepository.GetByIdAsync(deliveryId);
        if (delivery == null || delivery.WebhookSubscription == null)
        {
            return false;
        }

        var subscription = delivery.WebhookSubscription;

        try
        {
            delivery.Status = WebhookDeliveryStatus.InProgress;
            delivery.AttemptCount++;
            delivery.LastAttemptAt = DateTime.UtcNow;
            await _deliveryRepository.UpdateAsync(delivery);

            // Prepare HTTP request
            var request = new HttpRequestMessage(HttpMethod.Post, subscription.EndpointUrl);
            request.Content = new StringContent(delivery.Payload, Encoding.UTF8, "application/json");
            
            // Add signature header
            request.Headers.Add("X-Webhook-Signature", delivery.Signature);
            request.Headers.Add("X-Webhook-Event", delivery.EventType);
            request.Headers.Add("X-Webhook-Correlation-Id", delivery.CorrelationId);

            // Send request
            var response = await _httpClient.SendAsync(request);
            
            delivery.LastHttpStatusCode = (int)response.StatusCode;
            delivery.LastResponseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                // Delivery succeeded
                delivery.Status = WebhookDeliveryStatus.Succeeded;
                delivery.CompletedAt = DateTime.UtcNow;
                await _deliveryRepository.UpdateAsync(delivery);

                // Reset consecutive failures on subscription
                if (subscription.ConsecutiveFailures > 0)
                {
                    subscription.ConsecutiveFailures = 0;
                    await _subscriptionRepository.UpdateAsync(subscription);
                }

                return true;
            }
            else
            {
                // Delivery failed, check if we should retry
                return await HandleDeliveryFailureAsync(delivery, subscription, 
                    $"HTTP {response.StatusCode}: {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            // Delivery failed due to exception
            return await HandleDeliveryFailureAsync(delivery, subscription, ex.Message);
        }
    }

    /// <summary>
    /// Handle a failed delivery attempt
    /// </summary>
    private async Task<bool> HandleDeliveryFailureAsync(
        WebhookDelivery delivery,
        WebhookSubscription subscription,
        string errorMessage)
    {
        delivery.LastErrorMessage = errorMessage;

        if (delivery.AttemptCount >= subscription.MaxRetryAttempts)
        {
            // All retries exhausted
            delivery.Status = WebhookDeliveryStatus.Failed;
            delivery.CompletedAt = DateTime.UtcNow;
            await _deliveryRepository.UpdateAsync(delivery);

            // Increment consecutive failures on subscription
            subscription.ConsecutiveFailures++;
            
            // Mark subscription as degraded after 5 consecutive failures
            if (subscription.ConsecutiveFailures >= 5)
            {
                subscription.Status = WebhookSubscriptionStatus.Degraded;
                subscription.DegradedAt = DateTime.UtcNow;
                subscription.DegradedReason = $"Consecutive delivery failures: {subscription.ConsecutiveFailures}";
            }
            
            await _subscriptionRepository.UpdateAsync(subscription);

            return false;
        }
        else
        {
            // Schedule retry
            delivery.Status = WebhookDeliveryStatus.Retrying;
            
            // Calculate next retry time
            var delaySeconds = subscription.RetryDelaySeconds;
            if (subscription.UseExponentialBackoff)
            {
                delaySeconds = (int)(delaySeconds * Math.Pow(2, delivery.AttemptCount - 1));
            }
            
            delivery.NextRetryAt = DateTime.UtcNow.AddSeconds(delaySeconds);
            await _deliveryRepository.UpdateAsync(delivery);

            return false;
        }
    }

    /// <summary>
    /// Process pending retries (should be called by a background job)
    /// </summary>
    public async Task ProcessPendingRetriesAsync(int limit = 100)
    {
        var pendingRetries = await _deliveryRepository.GetPendingRetriesAsync(limit);
        
        foreach (var delivery in pendingRetries)
        {
            _ = Task.Run(async () => await AttemptDeliveryAsync(delivery.Id));
        }
    }

    /// <summary>
    /// Get delivery history for a subscription
    /// </summary>
    public async Task<List<WebhookDelivery>> GetDeliveryHistoryAsync(int subscriptionId, int skip = 0, int take = 100)
    {
        return await _deliveryRepository.GetBySubscriptionIdAsync(subscriptionId, skip, take);
    }

    /// <summary>
    /// Get failed deliveries for a subscription
    /// </summary>
    public async Task<List<WebhookDelivery>> GetFailedDeliveriesAsync(int subscriptionId, int skip = 0, int take = 100)
    {
        return await _deliveryRepository.GetFailedDeliveriesAsync(subscriptionId, skip, take);
    }
}
