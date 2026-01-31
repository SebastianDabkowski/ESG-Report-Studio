using Microsoft.EntityFrameworkCore;
using SD.ProjectName.Modules.Integrations.Domain.Entities;
using SD.ProjectName.Modules.Integrations.Domain.Interfaces;

namespace SD.ProjectName.Modules.Integrations.Infrastructure;

/// <summary>
/// Repository implementation for FinanceEntity
/// </summary>
public class FinanceEntityRepository : IFinanceEntityRepository
{
    private readonly IntegrationDbContext _context;

    public FinanceEntityRepository(IntegrationDbContext context)
    {
        _context = context;
    }

    public async Task<FinanceEntity?> GetByIdAsync(int id)
    {
        return await _context.FinanceEntities
            .Include(e => e.Connector)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<FinanceEntity?> GetByExternalIdAsync(int connectorId, string externalId)
    {
        return await _context.FinanceEntities
            .Include(e => e.Connector)
            .FirstOrDefaultAsync(e => e.ConnectorId == connectorId && e.ExternalId == externalId);
    }

    public async Task<List<FinanceEntity>> GetByConnectorIdAsync(int connectorId, int limit = 100)
    {
        return await _context.FinanceEntities
            .Include(e => e.Connector)
            .Where(e => e.ConnectorId == connectorId)
            .OrderByDescending(e => e.ImportedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<FinanceEntity>> GetApprovedByConnectorIdAsync(int connectorId, int limit = 100)
    {
        return await _context.FinanceEntities
            .Include(e => e.Connector)
            .Where(e => e.ConnectorId == connectorId && e.IsApproved)
            .OrderByDescending(e => e.ImportedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<FinanceEntity> AddAsync(FinanceEntity entity)
    {
        _context.FinanceEntities.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<FinanceEntity> UpdateAsync(FinanceEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.FinanceEntities.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.FinanceEntities.FindAsync(id);
        if (entity != null)
        {
            _context.FinanceEntities.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
