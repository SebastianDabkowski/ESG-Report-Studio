using Microsoft.EntityFrameworkCore;
using SD.ProjectName.Modules.Integrations.Domain.Entities;
using SD.ProjectName.Modules.Integrations.Domain.Interfaces;

namespace SD.ProjectName.Modules.Integrations.Infrastructure;

/// <summary>
/// Repository implementation for IntegrationLog entity
/// </summary>
public class IntegrationLogRepository : IIntegrationLogRepository
{
    private readonly IntegrationDbContext _context;

    public IntegrationLogRepository(IntegrationDbContext context)
    {
        _context = context;
    }

    public async Task<IntegrationLog?> GetByIdAsync(int id)
    {
        return await _context.IntegrationLogs
            .Include(l => l.Connector)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<List<IntegrationLog>> GetByCorrelationIdAsync(string correlationId)
    {
        return await _context.IntegrationLogs
            .Include(l => l.Connector)
            .Where(l => l.CorrelationId == correlationId)
            .OrderBy(l => l.StartedAt)
            .ToListAsync();
    }

    public async Task<List<IntegrationLog>> GetByConnectorIdAsync(int connectorId, int limit = 100)
    {
        return await _context.IntegrationLogs
            .Where(l => l.ConnectorId == connectorId)
            .OrderByDescending(l => l.StartedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IntegrationLog> CreateAsync(IntegrationLog log)
    {
        _context.IntegrationLogs.Add(log);
        await _context.SaveChangesAsync();
        return log;
    }

    public async Task<IntegrationLog> UpdateAsync(IntegrationLog log)
    {
        _context.IntegrationLogs.Update(log);
        await _context.SaveChangesAsync();
        return log;
    }
}
