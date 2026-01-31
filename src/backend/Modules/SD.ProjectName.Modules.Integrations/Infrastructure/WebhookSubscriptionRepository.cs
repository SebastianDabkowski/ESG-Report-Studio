using Microsoft.EntityFrameworkCore;
using SD.ProjectName.Modules.Integrations.Domain.Entities;
using SD.ProjectName.Modules.Integrations.Domain.Interfaces;

namespace SD.ProjectName.Modules.Integrations.Infrastructure;

/// <summary>
/// Repository implementation for webhook subscriptions
/// </summary>
public class WebhookSubscriptionRepository : IWebhookSubscriptionRepository
{
    private readonly IntegrationDbContext _context;

    public WebhookSubscriptionRepository(IntegrationDbContext context)
    {
        _context = context;
    }

    public async Task<WebhookSubscription> CreateAsync(WebhookSubscription subscription)
    {
        _context.WebhookSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();
        return subscription;
    }

    public async Task<WebhookSubscription?> GetByIdAsync(int id)
    {
        return await _context.WebhookSubscriptions.FindAsync(id);
    }

    public async Task<List<WebhookSubscription>> GetAllAsync()
    {
        return await _context.WebhookSubscriptions.ToListAsync();
    }

    public async Task<List<WebhookSubscription>> GetActiveSubscriptionsForEventAsync(string eventType)
    {
        return await _context.WebhookSubscriptions
            .Where(s => s.Status == WebhookSubscriptionStatus.Active &&
                       s.SubscribedEvents.Contains(eventType))
            .ToListAsync();
    }

    public async Task<WebhookSubscription> UpdateAsync(WebhookSubscription subscription)
    {
        _context.WebhookSubscriptions.Update(subscription);
        await _context.SaveChangesAsync();
        return subscription;
    }

    public async Task DeleteAsync(int id)
    {
        var subscription = await GetByIdAsync(id);
        if (subscription != null)
        {
            _context.WebhookSubscriptions.Remove(subscription);
            await _context.SaveChangesAsync();
        }
    }
}
