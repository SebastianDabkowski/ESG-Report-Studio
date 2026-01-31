using Microsoft.AspNetCore.Mvc;
using SD.ProjectName.Modules.Integrations.Application;
using SD.ProjectName.Modules.Integrations.Domain.Entities;

namespace ARP.ESG_ReportStudio.API.Controllers.Integrations;

[ApiController]
[Route("api/v1/canonical")]
public class CanonicalDataController : ControllerBase
{
    private readonly CanonicalMappingService _canonicalMappingService;

    public CanonicalDataController(CanonicalMappingService canonicalMappingService)
    {
        _canonicalMappingService = canonicalMappingService;
    }

    /// <summary>
    /// Create a new schema version for a canonical entity type
    /// </summary>
    [HttpPost("versions")]
    public async Task<ActionResult<CanonicalEntityVersionResponse>> CreateSchemaVersion(
        [FromBody] CreateSchemaVersionRequest request)
    {
        var currentUser = User.Identity?.Name ?? "system";

        try
        {
            var version = await _canonicalMappingService.CreateSchemaVersionAsync(
                request.EntityType,
                request.Version,
                request.SchemaDefinition,
                request.Description,
                currentUser,
                request.BackwardCompatibleWithVersion,
                request.MigrationRules);

            return Ok(MapToVersionResponse(version));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Create a new mapping configuration for a connector
    /// </summary>
    [HttpPost("mappings")]
    public async Task<ActionResult<CanonicalMappingResponse>> CreateMapping(
        [FromBody] CreateMappingRequest request)
    {
        var currentUser = User.Identity?.Name ?? "system";

        try
        {
            var mapping = await _canonicalMappingService.CreateMappingAsync(
                request.ConnectorId,
                request.TargetEntityType,
                request.TargetSchemaVersion,
                request.ExternalField,
                request.CanonicalAttribute,
                request.TransformationType,
                currentUser,
                request.TransformationParams,
                request.IsRequired,
                request.DefaultValue,
                request.Priority,
                request.Notes);

            return Ok(MapToMappingResponse(mapping));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Validate backward compatibility between two schema versions
    /// </summary>
    [HttpGet("versions/{entityType}/compatibility")]
    public async Task<ActionResult<BackwardCompatibilityResponse>> ValidateBackwardCompatibility(
        CanonicalEntityType entityType,
        [FromQuery] int currentVersion,
        [FromQuery] int newVersion)
    {
        var isCompatible = await _canonicalMappingService.ValidateBackwardCompatibilityAsync(
            entityType,
            currentVersion,
            newVersion);

        return Ok(new BackwardCompatibilityResponse
        {
            EntityType = entityType,
            CurrentVersion = currentVersion,
            NewVersion = newVersion,
            IsCompatible = isCompatible
        });
    }

    private static CanonicalEntityVersionResponse MapToVersionResponse(CanonicalEntityVersion version)
    {
        return new CanonicalEntityVersionResponse
        {
            Id = version.Id,
            EntityType = version.EntityType,
            Version = version.Version,
            SchemaDefinition = version.SchemaDefinition,
            Description = version.Description,
            IsActive = version.IsActive,
            IsDeprecated = version.IsDeprecated,
            BackwardCompatibleWithVersion = version.BackwardCompatibleWithVersion,
            MigrationRules = version.MigrationRules,
            CreatedAt = version.CreatedAt,
            CreatedBy = version.CreatedBy,
            DeprecatedAt = version.DeprecatedAt
        };
    }

    private static CanonicalMappingResponse MapToMappingResponse(CanonicalMapping mapping)
    {
        return new CanonicalMappingResponse
        {
            Id = mapping.Id,
            ConnectorId = mapping.ConnectorId,
            TargetEntityType = mapping.TargetEntityType,
            TargetSchemaVersion = mapping.TargetSchemaVersion,
            ExternalField = mapping.ExternalField,
            CanonicalAttribute = mapping.CanonicalAttribute,
            TransformationType = mapping.TransformationType,
            TransformationParams = mapping.TransformationParams,
            IsRequired = mapping.IsRequired,
            DefaultValue = mapping.DefaultValue,
            Priority = mapping.Priority,
            IsActive = mapping.IsActive,
            Notes = mapping.Notes,
            CreatedAt = mapping.CreatedAt,
            CreatedBy = mapping.CreatedBy
        };
    }
}

// DTOs
public record CreateSchemaVersionRequest
{
    public CanonicalEntityType EntityType { get; init; }
    public int Version { get; init; }
    public string SchemaDefinition { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int? BackwardCompatibleWithVersion { get; init; }
    public string? MigrationRules { get; init; }
}

public record CreateMappingRequest
{
    public int ConnectorId { get; init; }
    public CanonicalEntityType TargetEntityType { get; init; }
    public int TargetSchemaVersion { get; init; }
    public string ExternalField { get; init; } = string.Empty;
    public string CanonicalAttribute { get; init; } = string.Empty;
    public string TransformationType { get; init; } = "direct";
    public string? TransformationParams { get; init; }
    public bool IsRequired { get; init; }
    public string? DefaultValue { get; init; }
    public int Priority { get; init; }
    public string? Notes { get; init; }
}

public record CanonicalEntityVersionResponse
{
    public int Id { get; init; }
    public CanonicalEntityType EntityType { get; init; }
    public int Version { get; init; }
    public string SchemaDefinition { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsDeprecated { get; init; }
    public int? BackwardCompatibleWithVersion { get; init; }
    public string? MigrationRules { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime? DeprecatedAt { get; init; }
}

public record CanonicalMappingResponse
{
    public int Id { get; init; }
    public int ConnectorId { get; init; }
    public CanonicalEntityType TargetEntityType { get; init; }
    public int TargetSchemaVersion { get; init; }
    public string ExternalField { get; init; } = string.Empty;
    public string CanonicalAttribute { get; init; } = string.Empty;
    public string TransformationType { get; init; } = string.Empty;
    public string? TransformationParams { get; init; }
    public bool IsRequired { get; init; }
    public string? DefaultValue { get; init; }
    public int Priority { get; init; }
    public bool IsActive { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
}

public record BackwardCompatibilityResponse
{
    public CanonicalEntityType EntityType { get; init; }
    public int CurrentVersion { get; init; }
    public int NewVersion { get; init; }
    public bool IsCompatible { get; init; }
}
