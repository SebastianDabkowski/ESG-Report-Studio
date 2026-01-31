using Microsoft.EntityFrameworkCore;
using SD.ProjectName.Modules.Integrations.Domain.Entities;
using SD.ProjectName.Modules.Integrations.Domain.Interfaces;

namespace SD.ProjectName.Modules.Integrations.Infrastructure;

/// <summary>
/// Repository implementation for webhook deliveries
/// </summary>
public class WebhookDeliveryRepository : IWebhookDeliveryRepository
{
    private readonly IntegrationDbContext _context;

    public WebhookDeliveryRepository(IntegrationDbContext context)
    {
        _context = context;
    }

    public async Task<WebhookDelivery> CreateAsync(WebhookDelivery delivery)
    {
        _context.WebhookDeliveries.Add(delivery);
        await _context.SaveChangesAsync();
        return delivery;
    }

    public async Task<WebhookDelivery?> GetByIdAsync(int id)
    {
        return await _context.WebhookDeliveries
            .Include(d => d.WebhookSubscription)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<List<WebhookDelivery>> GetBySubscriptionIdAsync(int subscriptionId, int skip = 0, int take = 100)
    {
        return await _context.WebhookDeliveries
            .Where(d => d.WebhookSubscriptionId == subscriptionId)
            .OrderByDescending(d => d.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<List<WebhookDelivery>> GetPendingRetriesAsync(int limit = 100)
    {
        var now = DateTime.UtcNow;
        return await _context.WebhookDeliveries
            .Include(d => d.WebhookSubscription)
            .Where(d => d.Status == WebhookDeliveryStatus.Retrying &&
                       d.NextRetryAt != null &&
                       d.NextRetryAt <= now)
            .OrderBy(d => d.NextRetryAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<WebhookDelivery> UpdateAsync(WebhookDelivery delivery)
    {
        _context.WebhookDeliveries.Update(delivery);
        await _context.SaveChangesAsync();
        return delivery;
    }

    public async Task<List<WebhookDelivery>> GetFailedDeliveriesAsync(int subscriptionId, int skip = 0, int take = 100)
    {
        return await _context.WebhookDeliveries
            .Where(d => d.WebhookSubscriptionId == subscriptionId &&
                       d.Status == WebhookDeliveryStatus.Failed)
            .OrderByDescending(d => d.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }
}
