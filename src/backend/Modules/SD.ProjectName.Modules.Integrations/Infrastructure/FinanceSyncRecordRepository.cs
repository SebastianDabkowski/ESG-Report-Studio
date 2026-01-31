using Microsoft.EntityFrameworkCore;
using SD.ProjectName.Modules.Integrations.Domain.Entities;
using SD.ProjectName.Modules.Integrations.Domain.Interfaces;

namespace SD.ProjectName.Modules.Integrations.Infrastructure;

/// <summary>
/// Repository implementation for FinanceSyncRecord
/// </summary>
public class FinanceSyncRecordRepository : IFinanceSyncRecordRepository
{
    private readonly IntegrationDbContext _context;

    public FinanceSyncRecordRepository(IntegrationDbContext context)
    {
        _context = context;
    }

    public async Task<FinanceSyncRecord?> GetByIdAsync(int id)
    {
        return await _context.FinanceSyncRecords
            .Include(r => r.Connector)
            .Include(r => r.FinanceEntity)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<FinanceSyncRecord>> GetByConnectorIdAsync(int connectorId, int limit = 100)
    {
        return await _context.FinanceSyncRecords
            .Include(r => r.Connector)
            .Where(r => r.ConnectorId == connectorId)
            .OrderByDescending(r => r.SyncedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<FinanceSyncRecord>> GetByCorrelationIdAsync(string correlationId)
    {
        return await _context.FinanceSyncRecords
            .Include(r => r.Connector)
            .Include(r => r.FinanceEntity)
            .Where(r => r.CorrelationId == correlationId)
            .OrderByDescending(r => r.SyncedAt)
            .ToListAsync();
    }

    public async Task<List<FinanceSyncRecord>> GetRejectedByConnectorIdAsync(int connectorId, int limit = 100)
    {
        return await _context.FinanceSyncRecords
            .Include(r => r.Connector)
            .Where(r => r.ConnectorId == connectorId && r.Status == FinanceSyncStatus.Rejected)
            .OrderByDescending(r => r.SyncedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<FinanceSyncRecord>> GetConflictsByConnectorIdAsync(int connectorId, int limit = 100)
    {
        return await _context.FinanceSyncRecords
            .Include(r => r.Connector)
            .Include(r => r.FinanceEntity)
            .Where(r => r.ConnectorId == connectorId && r.ConflictDetected)
            .OrderByDescending(r => r.SyncedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<FinanceSyncRecord> AddAsync(FinanceSyncRecord record)
    {
        _context.FinanceSyncRecords.Add(record);
        await _context.SaveChangesAsync();
        return record;
    }

    public async Task<FinanceSyncRecord> UpdateAsync(FinanceSyncRecord record)
    {
        _context.FinanceSyncRecords.Update(record);
        await _context.SaveChangesAsync();
        return record;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
