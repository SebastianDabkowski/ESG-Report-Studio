namespace SD.ProjectName.Modules.Integrations.Infrastructure;

using Microsoft.EntityFrameworkCore;
using SD.ProjectName.Modules.Integrations.Domain.Entities;
using SD.ProjectName.Modules.Integrations.Domain.Interfaces;

/// <summary>
/// Repository for managing canonical entities
/// </summary>
public class CanonicalEntityRepository : ICanonicalEntityRepository
{
    private readonly IntegrationDbContext _context;

    public CanonicalEntityRepository(IntegrationDbContext context)
    {
        _context = context;
    }

    public async Task<CanonicalEntity?> GetByIdAsync(int id)
    {
        return await _context.CanonicalEntities
            .Include(e => e.Schema)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<CanonicalEntity>> GetByTypeAsync(CanonicalEntityType entityType, int? schemaVersion = null)
    {
        var query = _context.CanonicalEntities
            .Include(e => e.Schema)
            .Where(e => e.EntityType == entityType);

        if (schemaVersion.HasValue)
        {
            query = query.Where(e => e.SchemaVersion == schemaVersion.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<CanonicalEntity?> GetByExternalIdAsync(string externalId, string sourceSystem)
    {
        return await _context.CanonicalEntities
            .Include(e => e.Schema)
            .FirstOrDefaultAsync(e => e.ExternalId == externalId && e.SourceSystem == sourceSystem);
    }

    public async Task<List<CanonicalEntity>> GetByImportJobIdAsync(string importJobId)
    {
        return await _context.CanonicalEntities
            .Include(e => e.Schema)
            .Where(e => e.ImportedByJobId == importJobId)
            .ToListAsync();
    }

    public async Task<CanonicalEntity> CreateAsync(CanonicalEntity entity)
    {
        _context.CanonicalEntities.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(CanonicalEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.CanonicalEntities.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.CanonicalEntities.FindAsync(id);
        if (entity != null)
        {
            _context.CanonicalEntities.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(string externalId, string sourceSystem)
    {
        return await _context.CanonicalEntities
            .AnyAsync(e => e.ExternalId == externalId && e.SourceSystem == sourceSystem);
    }
}
