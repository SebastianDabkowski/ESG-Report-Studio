using System.Text.Json;
using SD.ProjectName.Modules.Integrations.Domain.Entities;
using SD.ProjectName.Modules.Integrations.Domain.Interfaces;

namespace SD.ProjectName.Modules.Integrations.Application;

/// <summary>
/// Application service for managing canonical entity mappings
/// </summary>
public class CanonicalMappingService
{
    private readonly ICanonicalEntityRepository _canonicalEntityRepository;
    private readonly ICanonicalEntityVersionRepository _versionRepository;
    private readonly ICanonicalAttributeRepository _attributeRepository;
    private readonly ICanonicalMappingRepository _mappingRepository;

    public CanonicalMappingService(
        ICanonicalEntityRepository canonicalEntityRepository,
        ICanonicalEntityVersionRepository versionRepository,
        ICanonicalAttributeRepository attributeRepository,
        ICanonicalMappingRepository mappingRepository)
    {
        _canonicalEntityRepository = canonicalEntityRepository;
        _versionRepository = versionRepository;
        _attributeRepository = attributeRepository;
        _mappingRepository = mappingRepository;
    }

    /// <summary>
    /// Map external data to a canonical entity using configured mappings
    /// </summary>
    public async Task<CanonicalEntity> MapToCanonicalEntityAsync(
        int connectorId,
        CanonicalEntityType targetEntityType,
        Dictionary<string, object> externalData,
        string sourceSystem,
        string? sourceVersion = null,
        string? importJobId = null)
    {
        // Get latest active version for the entity type
        var latestVersion = await _versionRepository.GetLatestActiveVersionAsync(targetEntityType);
        if (latestVersion == null)
        {
            throw new InvalidOperationException($"No active schema version found for entity type {targetEntityType}");
        }

        return await MapToCanonicalEntityAsync(
            connectorId,
            targetEntityType,
            latestVersion.Version,
            externalData,
            sourceSystem,
            sourceVersion,
            importJobId);
    }

    /// <summary>
    /// Map external data to a canonical entity at a specific schema version
    /// </summary>
    public async Task<CanonicalEntity> MapToCanonicalEntityAsync(
        int connectorId,
        CanonicalEntityType targetEntityType,
        int targetSchemaVersion,
        Dictionary<string, object> externalData,
        string sourceSystem,
        string? sourceVersion = null,
        string? importJobId = null)
    {
        // Get mappings for this connector and entity type
        var mappings = await _mappingRepository.GetByConnectorTypeAndVersionAsync(
            connectorId, 
            targetEntityType, 
            targetSchemaVersion);

        if (!mappings.Any())
        {
            throw new InvalidOperationException(
                $"No mappings configured for connector {connectorId} and entity type {targetEntityType} version {targetSchemaVersion}");
        }

        // Validate required mappings
        var requiredMappings = mappings.Where(m => m.IsRequired).ToList();
        var missingFields = new List<string>();

        foreach (var mapping in requiredMappings)
        {
            if (!externalData.ContainsKey(mapping.ExternalField) && string.IsNullOrEmpty(mapping.DefaultValue))
            {
                missingFields.Add(mapping.ExternalField);
            }
        }

        if (missingFields.Any())
        {
            throw new InvalidOperationException(
                $"Required external fields missing: {string.Join(", ", missingFields)}");
        }

        // Apply mappings to build canonical data
        var canonicalData = new Dictionary<string, object>();
        var vendorExtensions = new Dictionary<string, object>();

        foreach (var mapping in mappings.OrderBy(m => m.Priority))
        {
            object? value = null;

            // Get value from external data or use default
            if (externalData.TryGetValue(mapping.ExternalField, out var externalValue))
            {
                value = ApplyTransformation(externalValue, mapping);
            }
            else if (!string.IsNullOrEmpty(mapping.DefaultValue))
            {
                value = mapping.DefaultValue;
            }

            if (value != null)
            {
                canonicalData[mapping.CanonicalAttribute] = value;
            }
        }

        // Store unmapped external fields as vendor extensions
        foreach (var kvp in externalData)
        {
            if (!mappings.Any(m => m.ExternalField == kvp.Key))
            {
                vendorExtensions[kvp.Key] = kvp.Value;
            }
        }

        // Determine external ID
        string? externalId = null;
        if (externalData.ContainsKey("id"))
        {
            externalId = externalData["id"]?.ToString();
        }
        else if (externalData.ContainsKey("externalId"))
        {
            externalId = externalData["externalId"]?.ToString();
        }
        else if (externalData.ContainsKey("external_id"))
        {
            externalId = externalData["external_id"]?.ToString();
        }

        // Create canonical entity
        var canonicalEntity = new CanonicalEntity
        {
            EntityType = targetEntityType,
            SchemaVersion = targetSchemaVersion,
            ExternalId = externalId,
            Data = JsonSerializer.Serialize(canonicalData),
            SourceSystem = sourceSystem,
            SourceVersion = sourceVersion,
            ImportedAt = DateTime.UtcNow,
            ImportedByJobId = importJobId,
            VendorExtensions = vendorExtensions.Any() ? JsonSerializer.Serialize(vendorExtensions) : null,
            IsApproved = false // Default to unapproved for review
        };

        return await _canonicalEntityRepository.CreateAsync(canonicalEntity);
    }

    /// <summary>
    /// Apply transformation to a value based on mapping configuration
    /// </summary>
    private object ApplyTransformation(object value, CanonicalMapping mapping)
    {
        return mapping.TransformationType.ToLower() switch
        {
            "direct" => value,
            "sum" => ApplySumTransformation(value),
            "average" => ApplyAverageTransformation(value),
            "lookup" => ApplyLookupTransformation(value, mapping.TransformationParams),
            "fte" => ApplyFteTransformation(value, mapping.TransformationParams),
            "custom" => ApplyCustomTransformation(value, mapping.TransformationParams),
            _ => value
        };
    }

    private object ApplySumTransformation(object value)
    {
        if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
        {
            return jsonElement.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.Number)
                .Sum(e => e.GetDouble());
        }

        if (value is IEnumerable<object> enumerable)
        {
            return enumerable.Sum(v => Convert.ToDouble(v));
        }

        return value;
    }

    private object ApplyAverageTransformation(object value)
    {
        if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
        {
            var numbers = jsonElement.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.Number)
                .Select(e => e.GetDouble())
                .ToList();
            
            return numbers.Any() ? numbers.Average() : 0;
        }

        if (value is IEnumerable<object> enumerable)
        {
            var list = enumerable.ToList();
            return list.Any() ? list.Average(v => Convert.ToDouble(v)) : 0;
        }

        return value;
    }

    private object ApplyLookupTransformation(object value, string? transformationParams)
    {
        if (string.IsNullOrEmpty(transformationParams))
        {
            return value;
        }

        try
        {
            var lookupTable = JsonSerializer.Deserialize<Dictionary<string, string>>(transformationParams);
            if (lookupTable != null && lookupTable.TryGetValue(value.ToString() ?? "", out var mappedValue))
            {
                return mappedValue;
            }
        }
        catch
        {
            // If lookup fails, return original value
        }

        return value;
    }

    private object ApplyFteTransformation(object value, string? transformationParams)
    {
        var hours = Convert.ToDouble(value);
        var standardHours = 40.0; // Default

        if (!string.IsNullOrEmpty(transformationParams))
        {
            try
            {
                var parameters = JsonSerializer.Deserialize<Dictionary<string, string>>(transformationParams);
                if (parameters != null && parameters.TryGetValue("standardHours", out var standardStr))
                {
                    standardHours = Convert.ToDouble(standardStr);
                }
            }
            catch
            {
                // Use default if parsing fails
            }
        }

        return hours / standardHours;
    }

    private object ApplyCustomTransformation(object value, string? transformationParams)
    {
        // Placeholder for custom transformations
        // In a real implementation, this could execute a custom script or function
        return value;
    }

    /// <summary>
    /// Create a new schema version for an entity type
    /// </summary>
    public async Task<CanonicalEntityVersion> CreateSchemaVersionAsync(
        CanonicalEntityType entityType,
        int version,
        string schemaDefinition,
        string description,
        string createdBy,
        int? backwardCompatibleWithVersion = null,
        string? migrationRules = null)
    {
        // Check if version already exists
        var existingVersion = await _versionRepository.GetVersionAsync(entityType, version);
        if (existingVersion != null)
        {
            throw new InvalidOperationException($"Version {version} already exists for entity type {entityType}");
        }

        var schemaVersion = new CanonicalEntityVersion
        {
            EntityType = entityType,
            Version = version,
            SchemaDefinition = schemaDefinition,
            Description = description,
            IsActive = true,
            IsDeprecated = false,
            BackwardCompatibleWithVersion = backwardCompatibleWithVersion,
            MigrationRules = migrationRules,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        return await _versionRepository.CreateAsync(schemaVersion);
    }

    /// <summary>
    /// Create a mapping configuration for a connector
    /// </summary>
    public async Task<CanonicalMapping> CreateMappingAsync(
        int connectorId,
        CanonicalEntityType targetEntityType,
        int targetSchemaVersion,
        string externalField,
        string canonicalAttribute,
        string transformationType,
        string createdBy,
        string? transformationParams = null,
        bool isRequired = false,
        string? defaultValue = null,
        int priority = 0,
        string? notes = null)
    {
        var mapping = new CanonicalMapping
        {
            ConnectorId = connectorId,
            TargetEntityType = targetEntityType,
            TargetSchemaVersion = targetSchemaVersion,
            ExternalField = externalField,
            CanonicalAttribute = canonicalAttribute,
            TransformationType = transformationType,
            TransformationParams = transformationParams,
            IsRequired = isRequired,
            DefaultValue = defaultValue,
            Priority = priority,
            IsActive = true,
            Notes = notes,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        return await _mappingRepository.CreateAsync(mapping);
    }

    /// <summary>
    /// Validate backward compatibility when updating a canonical entity
    /// </summary>
    public async Task<bool> ValidateBackwardCompatibilityAsync(
        CanonicalEntityType entityType,
        int currentVersion,
        int newVersion)
    {
        var currentVersionEntity = await _versionRepository.GetVersionAsync(entityType, currentVersion);
        var newVersionEntity = await _versionRepository.GetVersionAsync(entityType, newVersion);

        if (currentVersionEntity == null || newVersionEntity == null)
        {
            return false;
        }

        // Check if new version is backward compatible with current version
        if (newVersionEntity.BackwardCompatibleWithVersion.HasValue &&
            newVersionEntity.BackwardCompatibleWithVersion.Value <= currentVersion)
        {
            return true;
        }

        return false;
    }
}
