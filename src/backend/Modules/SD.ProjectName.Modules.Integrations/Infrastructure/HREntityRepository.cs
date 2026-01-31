using Microsoft.EntityFrameworkCore;
using SD.ProjectName.Modules.Integrations.Domain.Entities;
using SD.ProjectName.Modules.Integrations.Domain.Interfaces;

namespace SD.ProjectName.Modules.Integrations.Infrastructure;

/// <summary>
/// Repository implementation for HREntity
/// </summary>
public class HREntityRepository : IHREntityRepository
{
    private readonly IntegrationDbContext _context;

    public HREntityRepository(IntegrationDbContext context)
    {
        _context = context;
    }

    public async Task<HREntity> CreateAsync(HREntity entity)
    {
        _context.HREntities.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<HREntity> UpdateAsync(HREntity entity)
    {
        _context.HREntities.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<HREntity?> GetByIdAsync(int id)
    {
        return await _context.HREntities.FindAsync(id);
    }

    public async Task<HREntity?> GetByExternalIdAsync(int connectorId, string externalId)
    {
        return await _context.HREntities
            .FirstOrDefaultAsync(e => e.ConnectorId == connectorId && e.ExternalId == externalId);
    }

    public async Task<List<HREntity>> GetByConnectorIdAsync(int connectorId, int limit = 100)
    {
        return await _context.HREntities
            .Where(e => e.ConnectorId == connectorId)
            .OrderByDescending(e => e.ImportedAt)
            .Take(limit)
            .ToListAsync();
    }
}
