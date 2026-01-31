using Microsoft.EntityFrameworkCore;
using SD.ProjectName.Modules.Integrations.Domain.Entities;
using SD.ProjectName.Modules.Integrations.Domain.Interfaces;

namespace SD.ProjectName.Modules.Integrations.Infrastructure;

/// <summary>
/// Repository implementation for IntegrationJobMetadata entity
/// </summary>
public class IntegrationJobMetadataRepository : IIntegrationJobMetadataRepository
{
    private readonly IntegrationDbContext _context;

    public IntegrationJobMetadataRepository(IntegrationDbContext context)
    {
        _context = context;
    }

    public async Task<IntegrationJobMetadata?> GetByIdAsync(int id)
    {
        return await _context.IntegrationJobMetadata
            .Include(j => j.Connector)
            .FirstOrDefaultAsync(j => j.Id == id);
    }

    public async Task<IntegrationJobMetadata?> GetByJobIdAsync(string jobId)
    {
        return await _context.IntegrationJobMetadata
            .Include(j => j.Connector)
            .FirstOrDefaultAsync(j => j.JobId == jobId);
    }

    public async Task<List<IntegrationJobMetadata>> GetByCorrelationIdAsync(string correlationId)
    {
        return await _context.IntegrationJobMetadata
            .Include(j => j.Connector)
            .Where(j => j.CorrelationId == correlationId)
            .OrderBy(j => j.StartedAt)
            .ToListAsync();
    }

    public async Task<List<IntegrationJobMetadata>> GetByConnectorIdAsync(int connectorId, int limit = 100)
    {
        return await _context.IntegrationJobMetadata
            .Where(j => j.ConnectorId == connectorId)
            .OrderByDescending(j => j.StartedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<IntegrationJobMetadata>> SearchJobsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        IntegrationJobStatus? status = null,
        int? connectorId = null,
        string? jobType = null,
        string? initiatedBy = null,
        int skip = 0,
        int take = 50)
    {
        var query = _context.IntegrationJobMetadata.Include(j => j.Connector).AsQueryable();
        
        if (startDate.HasValue)
            query = query.Where(j => j.StartedAt >= startDate.Value);
        
        if (endDate.HasValue)
            query = query.Where(j => j.StartedAt <= endDate.Value);
        
        if (status.HasValue)
            query = query.Where(j => j.Status == status.Value);
        
        if (connectorId.HasValue)
            query = query.Where(j => j.ConnectorId == connectorId.Value);
        
        if (!string.IsNullOrEmpty(jobType))
            query = query.Where(j => j.JobType == jobType);
        
        if (!string.IsNullOrEmpty(initiatedBy))
            query = query.Where(j => j.InitiatedBy == initiatedBy);
        
        return await query
            .OrderByDescending(j => j.StartedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IntegrationJobMetadata> CreateAsync(IntegrationJobMetadata job)
    {
        _context.IntegrationJobMetadata.Add(job);
        await _context.SaveChangesAsync();
        return job;
    }

    public async Task<IntegrationJobMetadata> UpdateAsync(IntegrationJobMetadata job)
    {
        _context.IntegrationJobMetadata.Update(job);
        await _context.SaveChangesAsync();
        return job;
    }

    public async Task<int> GetJobCountAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        IntegrationJobStatus? status = null,
        int? connectorId = null,
        string? jobType = null,
        string? initiatedBy = null)
    {
        var query = _context.IntegrationJobMetadata.AsQueryable();
        
        if (startDate.HasValue)
            query = query.Where(j => j.StartedAt >= startDate.Value);
        
        if (endDate.HasValue)
            query = query.Where(j => j.StartedAt <= endDate.Value);
        
        if (status.HasValue)
            query = query.Where(j => j.Status == status.Value);
        
        if (connectorId.HasValue)
            query = query.Where(j => j.ConnectorId == connectorId.Value);
        
        if (!string.IsNullOrEmpty(jobType))
            query = query.Where(j => j.JobType == jobType);
        
        if (!string.IsNullOrEmpty(initiatedBy))
            query = query.Where(j => j.InitiatedBy == initiatedBy);
        
        return await query.CountAsync();
    }
}
