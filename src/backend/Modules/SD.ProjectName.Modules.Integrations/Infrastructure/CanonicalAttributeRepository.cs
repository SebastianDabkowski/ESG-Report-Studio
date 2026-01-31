namespace SD.ProjectName.Modules.Integrations.Infrastructure;

using Microsoft.EntityFrameworkCore;
using SD.ProjectName.Modules.Integrations.Domain.Entities;
using SD.ProjectName.Modules.Integrations.Domain.Interfaces;

/// <summary>
/// Repository for managing canonical attribute definitions
/// </summary>
public class CanonicalAttributeRepository : ICanonicalAttributeRepository
{
    private readonly IntegrationDbContext _context;

    public CanonicalAttributeRepository(IntegrationDbContext context)
    {
        _context = context;
    }

    public async Task<CanonicalAttribute?> GetByIdAsync(int id)
    {
        return await _context.CanonicalAttributes.FindAsync(id);
    }

    public async Task<List<CanonicalAttribute>> GetAttributesAsync(CanonicalEntityType entityType, int schemaVersion)
    {
        return await _context.CanonicalAttributes
            .Where(a => a.EntityType == entityType && a.SchemaVersion == schemaVersion && !a.IsDeprecated)
            .OrderBy(a => a.AttributeName)
            .ToListAsync();
    }

    public async Task<List<CanonicalAttribute>> GetRequiredAttributesAsync(CanonicalEntityType entityType, int schemaVersion)
    {
        return await _context.CanonicalAttributes
            .Where(a => a.EntityType == entityType && a.SchemaVersion == schemaVersion && a.IsRequired && !a.IsDeprecated)
            .OrderBy(a => a.AttributeName)
            .ToListAsync();
    }

    public async Task<CanonicalAttribute?> GetByNameAsync(CanonicalEntityType entityType, int schemaVersion, string attributeName)
    {
        return await _context.CanonicalAttributes
            .FirstOrDefaultAsync(a => a.EntityType == entityType 
                && a.SchemaVersion == schemaVersion 
                && a.AttributeName == attributeName);
    }

    public async Task<CanonicalAttribute> CreateAsync(CanonicalAttribute attribute)
    {
        _context.CanonicalAttributes.Add(attribute);
        await _context.SaveChangesAsync();
        return attribute;
    }

    public async Task UpdateAsync(CanonicalAttribute attribute)
    {
        attribute.UpdatedAt = DateTime.UtcNow;
        _context.CanonicalAttributes.Update(attribute);
        await _context.SaveChangesAsync();
    }

    public async Task DeprecateAttributeAsync(int id, int deprecatedInVersion, string? replacedBy = null)
    {
        var attribute = await GetByIdAsync(id);
        if (attribute != null)
        {
            attribute.IsDeprecated = true;
            attribute.DeprecatedInVersion = deprecatedInVersion;
            attribute.ReplacedBy = replacedBy;
            await UpdateAsync(attribute);
        }
    }
}
