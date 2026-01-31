using Microsoft.EntityFrameworkCore;
using SD.ProjectName.Modules.Integrations.Domain.Entities;
using SD.ProjectName.Modules.Integrations.Domain.Interfaces;

namespace SD.ProjectName.Modules.Integrations.Infrastructure;

/// <summary>
/// Repository implementation for Connector entity
/// </summary>
public class ConnectorRepository : IConnectorRepository
{
    private readonly IntegrationDbContext _context;

    public ConnectorRepository(IntegrationDbContext context)
    {
        _context = context;
    }

    public async Task<Connector?> GetByIdAsync(int id)
    {
        return await _context.Connectors.FindAsync(id);
    }

    public async Task<List<Connector>> GetAllAsync()
    {
        return await _context.Connectors.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<List<Connector>> GetByStatusAsync(ConnectorStatus status)
    {
        return await _context.Connectors
            .Where(c => c.Status == status)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Connector> CreateAsync(Connector connector)
    {
        _context.Connectors.Add(connector);
        await _context.SaveChangesAsync();
        return connector;
    }

    public async Task<Connector> UpdateAsync(Connector connector)
    {
        _context.Connectors.Update(connector);
        await _context.SaveChangesAsync();
        return connector;
    }

    public async Task DeleteAsync(int id)
    {
        var connector = await _context.Connectors.FindAsync(id);
        if (connector != null)
        {
            _context.Connectors.Remove(connector);
            await _context.SaveChangesAsync();
        }
    }
}
