using SD.ProjectName.Modules.Integrations.Domain.Entities;

namespace SD.ProjectName.Modules.Integrations.Domain.Interfaces;

/// <summary>
/// Repository interface for webhook deliveries
/// </summary>
public interface IWebhookDeliveryRepository
{
    /// <summary>
    /// Create a new webhook delivery
    /// </summary>
    Task<WebhookDelivery> CreateAsync(WebhookDelivery delivery);
    
    /// <summary>
    /// Get a webhook delivery by ID
    /// </summary>
    Task<WebhookDelivery?> GetByIdAsync(int id);
    
    /// <summary>
    /// Get deliveries for a specific subscription
    /// </summary>
    Task<List<WebhookDelivery>> GetBySubscriptionIdAsync(int subscriptionId, int skip = 0, int take = 100);
    
    /// <summary>
    /// Get pending deliveries ready for retry
    /// </summary>
    Task<List<WebhookDelivery>> GetPendingRetriesAsync(int limit = 100);
    
    /// <summary>
    /// Update a webhook delivery
    /// </summary>
    Task<WebhookDelivery> UpdateAsync(WebhookDelivery delivery);
    
    /// <summary>
    /// Get failed deliveries for a subscription
    /// </summary>
    Task<List<WebhookDelivery>> GetFailedDeliveriesAsync(int subscriptionId, int skip = 0, int take = 100);
}
