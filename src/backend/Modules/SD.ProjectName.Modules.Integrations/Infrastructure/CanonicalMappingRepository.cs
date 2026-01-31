namespace SD.ProjectName.Modules.Integrations.Infrastructure;

using Microsoft.EntityFrameworkCore;
using SD.ProjectName.Modules.Integrations.Domain.Entities;
using SD.ProjectName.Modules.Integrations.Domain.Interfaces;

/// <summary>
/// Repository for managing canonical field mappings
/// </summary>
public class CanonicalMappingRepository : ICanonicalMappingRepository
{
    private readonly IntegrationDbContext _context;

    public CanonicalMappingRepository(IntegrationDbContext context)
    {
        _context = context;
    }

    public async Task<CanonicalMapping?> GetByIdAsync(int id)
    {
        return await _context.CanonicalMappings
            .Include(m => m.Connector)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<List<CanonicalMapping>> GetByConnectorAsync(int connectorId)
    {
        return await _context.CanonicalMappings
            .Where(m => m.ConnectorId == connectorId && m.IsActive)
            .OrderBy(m => m.Priority)
            .ThenBy(m => m.CanonicalAttribute)
            .ToListAsync();
    }

    public async Task<List<CanonicalMapping>> GetByConnectorAndTypeAsync(int connectorId, CanonicalEntityType targetEntityType)
    {
        return await _context.CanonicalMappings
            .Where(m => m.ConnectorId == connectorId && m.TargetEntityType == targetEntityType && m.IsActive)
            .OrderBy(m => m.Priority)
            .ThenBy(m => m.CanonicalAttribute)
            .ToListAsync();
    }

    public async Task<List<CanonicalMapping>> GetByConnectorTypeAndVersionAsync(
        int connectorId, 
        CanonicalEntityType targetEntityType, 
        int targetSchemaVersion)
    {
        return await _context.CanonicalMappings
            .Where(m => m.ConnectorId == connectorId 
                && m.TargetEntityType == targetEntityType 
                && m.TargetSchemaVersion == targetSchemaVersion 
                && m.IsActive)
            .OrderBy(m => m.Priority)
            .ThenBy(m => m.CanonicalAttribute)
            .ToListAsync();
    }

    public async Task<CanonicalMapping> CreateAsync(CanonicalMapping mapping)
    {
        _context.CanonicalMappings.Add(mapping);
        await _context.SaveChangesAsync();
        return mapping;
    }

    public async Task UpdateAsync(CanonicalMapping mapping)
    {
        mapping.UpdatedAt = DateTime.UtcNow;
        _context.CanonicalMappings.Update(mapping);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var mapping = await _context.CanonicalMappings.FindAsync(id);
        if (mapping != null)
        {
            _context.CanonicalMappings.Remove(mapping);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeactivateAsync(int id)
    {
        var mapping = await GetByIdAsync(id);
        if (mapping != null)
        {
            mapping.IsActive = false;
            await UpdateAsync(mapping);
        }
    }

    public async Task<List<CanonicalMapping>> GetRequiredMappingsAsync(int connectorId, CanonicalEntityType targetEntityType)
    {
        return await _context.CanonicalMappings
            .Where(m => m.ConnectorId == connectorId 
                && m.TargetEntityType == targetEntityType 
                && m.IsRequired 
                && m.IsActive)
            .ToListAsync();
    }
}
