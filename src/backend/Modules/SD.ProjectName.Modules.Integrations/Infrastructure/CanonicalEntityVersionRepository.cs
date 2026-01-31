namespace SD.ProjectName.Modules.Integrations.Infrastructure;

using Microsoft.EntityFrameworkCore;
using SD.ProjectName.Modules.Integrations.Domain.Entities;
using SD.ProjectName.Modules.Integrations.Domain.Interfaces;

/// <summary>
/// Repository for managing canonical entity versions
/// </summary>
public class CanonicalEntityVersionRepository : ICanonicalEntityVersionRepository
{
    private readonly IntegrationDbContext _context;

    public CanonicalEntityVersionRepository(IntegrationDbContext context)
    {
        _context = context;
    }

    public async Task<CanonicalEntityVersion?> GetByIdAsync(int id)
    {
        return await _context.CanonicalEntityVersions.FindAsync(id);
    }

    public async Task<CanonicalEntityVersion?> GetVersionAsync(CanonicalEntityType entityType, int version)
    {
        return await _context.CanonicalEntityVersions
            .FirstOrDefaultAsync(v => v.EntityType == entityType && v.Version == version);
    }

    public async Task<CanonicalEntityVersion?> GetLatestActiveVersionAsync(CanonicalEntityType entityType)
    {
        return await _context.CanonicalEntityVersions
            .Where(v => v.EntityType == entityType && v.IsActive && !v.IsDeprecated)
            .OrderByDescending(v => v.Version)
            .FirstOrDefaultAsync();
    }

    public async Task<List<CanonicalEntityVersion>> GetAllVersionsAsync(CanonicalEntityType entityType)
    {
        return await _context.CanonicalEntityVersions
            .Where(v => v.EntityType == entityType)
            .OrderBy(v => v.Version)
            .ToListAsync();
    }

    public async Task<CanonicalEntityVersion> CreateAsync(CanonicalEntityVersion version)
    {
        _context.CanonicalEntityVersions.Add(version);
        await _context.SaveChangesAsync();
        return version;
    }

    public async Task UpdateAsync(CanonicalEntityVersion version)
    {
        _context.CanonicalEntityVersions.Update(version);
        await _context.SaveChangesAsync();
    }

    public async Task DeprecateVersionAsync(CanonicalEntityType entityType, int version)
    {
        var versionEntity = await GetVersionAsync(entityType, version);
        if (versionEntity != null)
        {
            versionEntity.IsDeprecated = true;
            versionEntity.DeprecatedAt = DateTime.UtcNow;
            await UpdateAsync(versionEntity);
        }
    }
}
