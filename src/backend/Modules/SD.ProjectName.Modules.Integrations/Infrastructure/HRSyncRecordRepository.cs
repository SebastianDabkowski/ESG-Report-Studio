using Microsoft.EntityFrameworkCore;
using SD.ProjectName.Modules.Integrations.Domain.Entities;
using SD.ProjectName.Modules.Integrations.Domain.Interfaces;

namespace SD.ProjectName.Modules.Integrations.Infrastructure;

/// <summary>
/// Repository implementation for HRSyncRecord
/// </summary>
public class HRSyncRecordRepository : IHRSyncRecordRepository
{
    private readonly IntegrationDbContext _context;

    public HRSyncRecordRepository(IntegrationDbContext context)
    {
        _context = context;
    }

    public async Task<HRSyncRecord> CreateAsync(HRSyncRecord record)
    {
        _context.HRSyncRecords.Add(record);
        await _context.SaveChangesAsync();
        return record;
    }

    public async Task<HRSyncRecord?> GetByIdAsync(int id)
    {
        return await _context.HRSyncRecords
            .Include(r => r.Connector)
            .Include(r => r.HREntity)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<HRSyncRecord>> GetByConnectorIdAsync(int connectorId, int limit = 100)
    {
        return await _context.HRSyncRecords
            .Where(r => r.ConnectorId == connectorId)
            .OrderByDescending(r => r.SyncedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<HRSyncRecord>> GetByCorrelationIdAsync(string correlationId)
    {
        return await _context.HRSyncRecords
            .Where(r => r.CorrelationId == correlationId)
            .OrderBy(r => r.SyncedAt)
            .ToListAsync();
    }

    public async Task<List<HRSyncRecord>> GetRejectedRecordsAsync(int connectorId, int limit = 100)
    {
        return await _context.HRSyncRecords
            .Where(r => r.ConnectorId == connectorId && r.Status == HRSyncStatus.Rejected)
            .OrderByDescending(r => r.SyncedAt)
            .Take(limit)
            .ToListAsync();
    }
}
