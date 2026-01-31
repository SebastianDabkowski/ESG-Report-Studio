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
    
    public async Task<List<IntegrationLog>> SearchLogsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        IntegrationStatus? status = null,
        int? connectorId = null,
        string? operationType = null,
        string? initiatedBy = null,
        int skip = 0,
        int take = 50)
    {
        var query = _context.IntegrationLogs.Include(l => l.Connector).AsQueryable();
        
        if (startDate.HasValue)
            query = query.Where(l => l.StartedAt >= startDate.Value);
        
        if (endDate.HasValue)
            query = query.Where(l => l.StartedAt <= endDate.Value);
        
        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);
        
        if (connectorId.HasValue)
            query = query.Where(l => l.ConnectorId == connectorId.Value);
        
        if (!string.IsNullOrEmpty(operationType))
            query = query.Where(l => l.OperationType == operationType);
        
        if (!string.IsNullOrEmpty(initiatedBy))
            query = query.Where(l => l.InitiatedBy == initiatedBy);
        
        return await query
            .OrderByDescending(l => l.StartedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }
    
    public async Task<int> GetLogCountAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        IntegrationStatus? status = null,
        int? connectorId = null,
        string? operationType = null,
        string? initiatedBy = null)
    {
        var query = _context.IntegrationLogs.AsQueryable();
        
        if (startDate.HasValue)
            query = query.Where(l => l.StartedAt >= startDate.Value);
        
        if (endDate.HasValue)
            query = query.Where(l => l.StartedAt <= endDate.Value);
        
        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);
        
        if (connectorId.HasValue)
            query = query.Where(l => l.ConnectorId == connectorId.Value);
        
        if (!string.IsNullOrEmpty(operationType))
            query = query.Where(l => l.OperationType == operationType);
        
        if (!string.IsNullOrEmpty(initiatedBy))
            query = query.Where(l => l.InitiatedBy == initiatedBy);
        
        return await query.CountAsync();
    }
}
