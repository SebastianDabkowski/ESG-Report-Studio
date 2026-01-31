using System.Net.Http.Json;
using Microsoft.Extensions.Http;
using SD.ProjectName.Modules.Integrations.Domain.Entities;
using SD.ProjectName.Modules.Integrations.Domain.Interfaces;

namespace SD.ProjectName.Modules.Integrations.Application;

/// <summary>
/// Application service for managing webhook subscriptions
/// </summary>
public class WebhookSubscriptionService
{
    private readonly IWebhookSubscriptionRepository _subscriptionRepository;
    private readonly WebhookSignatureService _signatureService;
    private readonly HttpClient _httpClient;

    public WebhookSubscriptionService(
        IWebhookSubscriptionRepository subscriptionRepository,
        WebhookSignatureService signatureService,
        IHttpClientFactory httpClientFactory)
    {
        _subscriptionRepository = subscriptionRepository;
        _signatureService = signatureService;
        _httpClient = httpClientFactory.CreateClient("WebhookClient");
    }

    /// <summary>
    /// Create a new webhook subscription
    /// </summary>
    public async Task<WebhookSubscription> CreateSubscriptionAsync(
        string name,
        string endpointUrl,
        string[] subscribedEvents,
        string createdBy,
        int maxRetryAttempts = 3,
        int retryDelaySeconds = 5,
        bool useExponentialBackoff = true,
        string? description = null)
    {
        // Validate event types
        foreach (var eventType in subscribedEvents)
        {
            if (!WebhookEventType.AllEventTypes.Contains(eventType))
            {
                throw new ArgumentException($"Invalid event type: {eventType}");
            }
        }

        var subscription = new WebhookSubscription
        {
            Name = name,
            EndpointUrl = endpointUrl,
            SubscribedEvents = string.Join(",", subscribedEvents),
            Status = WebhookSubscriptionStatus.PendingVerification,
            SigningSecret = _signatureService.GenerateSigningSecret(),
            VerificationToken = _signatureService.GenerateVerificationToken(),
            MaxRetryAttempts = maxRetryAttempts,
            RetryDelaySeconds = retryDelaySeconds,
            UseExponentialBackoff = useExponentialBackoff,
            Description = description,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _subscriptionRepository.CreateAsync(subscription);
        
        // Attempt verification handshake asynchronously (don't wait for it)
        _ = Task.Run(async () => await PerformVerificationHandshakeAsync(created.Id));
        
        return created;
    }

    /// <summary>
    /// Perform verification handshake with the webhook endpoint
    /// </summary>
    public async Task<bool> PerformVerificationHandshakeAsync(int subscriptionId)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
        if (subscription == null)
        {
            return false;
        }

        try
        {
            // Send verification challenge
            var challenge = new
            {
                type = "webhook.verification",
                token = subscription.VerificationToken,
                timestamp = DateTime.UtcNow.ToString("o")
            };

            var response = await _httpClient.PostAsJsonAsync(subscription.EndpointUrl, challenge);
            
            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                
                // Check if the response echoes back the verification token
                if (responseData.Contains(subscription.VerificationToken ?? ""))
                {
                    subscription.Status = WebhookSubscriptionStatus.Active;
                    subscription.VerifiedAt = DateTime.UtcNow;
                    await _subscriptionRepository.UpdateAsync(subscription);
                    return true;
                }
            }
        }
        catch (Exception)
        {
            // Verification failed, subscription remains in PendingVerification
        }

        return false;
    }

    /// <summary>
    /// Get a subscription by ID
    /// </summary>
    public async Task<WebhookSubscription?> GetSubscriptionByIdAsync(int id)
    {
        return await _subscriptionRepository.GetByIdAsync(id);
    }

    /// <summary>
    /// Get all subscriptions
    /// </summary>
    public async Task<List<WebhookSubscription>> GetAllSubscriptionsAsync()
    {
        return await _subscriptionRepository.GetAllAsync();
    }

    /// <summary>
    /// Get active subscriptions for a specific event type
    /// </summary>
    public async Task<List<WebhookSubscription>> GetActiveSubscriptionsForEventAsync(string eventType)
    {
        return await _subscriptionRepository.GetActiveSubscriptionsForEventAsync(eventType);
    }

    /// <summary>
    /// Update a subscription
    /// </summary>
    public async Task<WebhookSubscription> UpdateSubscriptionAsync(
        int id,
        string name,
        string endpointUrl,
        string[] subscribedEvents,
        string updatedBy,
        int maxRetryAttempts = 3,
        int retryDelaySeconds = 5,
        bool useExponentialBackoff = true,
        string? description = null)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(id);
        if (subscription == null)
        {
            throw new InvalidOperationException($"Webhook subscription with ID {id} not found");
        }

        // Validate event types
        foreach (var eventType in subscribedEvents)
        {
            if (!WebhookEventType.AllEventTypes.Contains(eventType))
            {
                throw new ArgumentException($"Invalid event type: {eventType}");
            }
        }

        subscription.Name = name;
        subscription.EndpointUrl = endpointUrl;
        subscription.SubscribedEvents = string.Join(",", subscribedEvents);
        subscription.MaxRetryAttempts = maxRetryAttempts;
        subscription.RetryDelaySeconds = retryDelaySeconds;
        subscription.UseExponentialBackoff = useExponentialBackoff;
        subscription.Description = description;
        subscription.UpdatedBy = updatedBy;
        subscription.UpdatedAt = DateTime.UtcNow;

        return await _subscriptionRepository.UpdateAsync(subscription);
    }

    /// <summary>
    /// Activate a subscription
    /// </summary>
    public async Task<WebhookSubscription> ActivateSubscriptionAsync(int id, string updatedBy)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(id);
        if (subscription == null)
        {
            throw new InvalidOperationException($"Webhook subscription with ID {id} not found");
        }

        subscription.Status = WebhookSubscriptionStatus.Active;
        subscription.ConsecutiveFailures = 0;
        subscription.DegradedAt = null;
        subscription.DegradedReason = null;
        subscription.UpdatedBy = updatedBy;
        subscription.UpdatedAt = DateTime.UtcNow;

        return await _subscriptionRepository.UpdateAsync(subscription);
    }

    /// <summary>
    /// Pause a subscription
    /// </summary>
    public async Task<WebhookSubscription> PauseSubscriptionAsync(int id, string updatedBy)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(id);
        if (subscription == null)
        {
            throw new InvalidOperationException($"Webhook subscription with ID {id} not found");
        }

        subscription.Status = WebhookSubscriptionStatus.Paused;
        subscription.UpdatedBy = updatedBy;
        subscription.UpdatedAt = DateTime.UtcNow;

        return await _subscriptionRepository.UpdateAsync(subscription);
    }

    /// <summary>
    /// Rotate the signing secret for a subscription
    /// </summary>
    public async Task<WebhookSubscription> RotateSigningSecretAsync(int id, string updatedBy)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(id);
        if (subscription == null)
        {
            throw new InvalidOperationException($"Webhook subscription with ID {id} not found");
        }

        subscription.SigningSecret = _signatureService.GenerateSigningSecret();
        subscription.SecretRotatedAt = DateTime.UtcNow;
        subscription.UpdatedBy = updatedBy;
        subscription.UpdatedAt = DateTime.UtcNow;

        return await _subscriptionRepository.UpdateAsync(subscription);
    }

    /// <summary>
    /// Mark a subscription as degraded
    /// </summary>
    public async Task<WebhookSubscription> MarkAsDegradedAsync(int id, string reason)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(id);
        if (subscription == null)
        {
            throw new InvalidOperationException($"Webhook subscription with ID {id} not found");
        }

        subscription.Status = WebhookSubscriptionStatus.Degraded;
        subscription.DegradedAt = DateTime.UtcNow;
        subscription.DegradedReason = reason;
        subscription.UpdatedAt = DateTime.UtcNow;

        return await _subscriptionRepository.UpdateAsync(subscription);
    }

    /// <summary>
    /// Delete a subscription
    /// </summary>
    public async Task DeleteSubscriptionAsync(int id)
    {
        await _subscriptionRepository.DeleteAsync(id);
    }
}
