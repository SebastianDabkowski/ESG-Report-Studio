using SD.ProjectName.Modules.Integrations.Domain.Entities;

namespace SD.ProjectName.Modules.Integrations.Domain.Interfaces;

/// <summary>
/// Repository interface for webhook subscriptions
/// </summary>
public interface IWebhookSubscriptionRepository
{
    /// <summary>
    /// Create a new webhook subscription
    /// </summary>
    Task<WebhookSubscription> CreateAsync(WebhookSubscription subscription);
    
    /// <summary>
    /// Get a webhook subscription by ID
    /// </summary>
    Task<WebhookSubscription?> GetByIdAsync(int id);
    
    /// <summary>
    /// Get all webhook subscriptions
    /// </summary>
    Task<List<WebhookSubscription>> GetAllAsync();
    
    /// <summary>
    /// Get active subscriptions for a specific event type
    /// </summary>
    Task<List<WebhookSubscription>> GetActiveSubscriptionsForEventAsync(string eventType);
    
    /// <summary>
    /// Update a webhook subscription
    /// </summary>
    Task<WebhookSubscription> UpdateAsync(WebhookSubscription subscription);
    
    /// <summary>
    /// Delete a webhook subscription
    /// </summary>
    Task DeleteAsync(int id);
}
