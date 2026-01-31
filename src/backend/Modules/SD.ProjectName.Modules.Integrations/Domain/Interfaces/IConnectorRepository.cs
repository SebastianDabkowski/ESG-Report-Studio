using SD.ProjectName.Modules.Integrations.Domain.Entities;

namespace SD.ProjectName.Modules.Integrations.Domain.Interfaces;

/// <summary>
/// Repository interface for Connector entity
/// </summary>
public interface IConnectorRepository
{
    /// <summary>
    /// Get a connector by ID
    /// </summary>
    Task<Connector?> GetByIdAsync(int id);
    
    /// <summary>
    /// Get all connectors
    /// </summary>
    Task<List<Connector>> GetAllAsync();
    
    /// <summary>
    /// Get connectors by status
    /// </summary>
    Task<List<Connector>> GetByStatusAsync(ConnectorStatus status);
    
    /// <summary>
    /// Create a new connector
    /// </summary>
    Task<Connector> CreateAsync(Connector connector);
    
    /// <summary>
    /// Update an existing connector
    /// </summary>
    Task<Connector> UpdateAsync(Connector connector);
    
    /// <summary>
    /// Delete a connector
    /// </summary>
    Task DeleteAsync(int id);
}
