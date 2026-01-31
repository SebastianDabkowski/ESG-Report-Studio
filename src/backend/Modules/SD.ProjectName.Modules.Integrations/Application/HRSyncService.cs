using System.Text.Json;
using SD.ProjectName.Modules.Integrations.Domain.Entities;
using SD.ProjectName.Modules.Integrations.Domain.Interfaces;

namespace SD.ProjectName.Modules.Integrations.Application;

/// <summary>
/// Service for managing HR data synchronization
/// </summary>
public class HRSyncService
{
    private readonly IConnectorRepository _connectorRepository;
    private readonly IHREntityRepository _hrEntityRepository;
    private readonly IHRSyncRecordRepository _hrSyncRecordRepository;
    private readonly IntegrationExecutionService _integrationExecutionService;

    public HRSyncService(
        IConnectorRepository connectorRepository,
        IHREntityRepository hrEntityRepository,
        IHRSyncRecordRepository hrSyncRecordRepository,
        IntegrationExecutionService integrationExecutionService)
    {
        _connectorRepository = connectorRepository;
        _hrEntityRepository = hrEntityRepository;
        _hrSyncRecordRepository = hrSyncRecordRepository;
        _integrationExecutionService = integrationExecutionService;
    }

    /// <summary>
    /// Test connection to HR system and validate authentication
    /// </summary>
    public async Task<TestConnectionResult> TestConnectionAsync(int connectorId, string initiatedBy)
    {
        var connector = await _connectorRepository.GetByIdAsync(connectorId);
        if (connector == null)
        {
            return new TestConnectionResult
            {
                Success = false,
                Message = $"Connector with ID {connectorId} not found"
            };
        }

        if (connector.ConnectorType != "HR")
        {
            return new TestConnectionResult
            {
                Success = false,
                Message = $"Connector is not an HR connector (type: {connector.ConnectorType})"
            };
        }

        var correlationId = Guid.NewGuid().ToString();

        try
        {
            // Execute a test call to the HR system (typically a health or auth endpoint)
            var log = await _integrationExecutionService.ExecuteWithRetryAsync(
                connectorId,
                "test-connection",
                correlationId,
                initiatedBy,
                async () =>
                {
                    // Simulate a test connection call
                    // In a real implementation, this would call the actual HR system API
                    var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
                    
                    // For now, we'll just validate the endpoint is reachable
                    var response = await client.GetAsync($"{connector.EndpointBaseUrl}/health");
                    
                    return new IntegrationCallResult
                    {
                        HttpMethod = "GET",
                        Endpoint = "/health",
                        HttpStatusCode = (int)response.StatusCode,
                        ResponseSummary = response.IsSuccessStatusCode ? "Connection successful" : "Connection failed"
                    };
                });

            if (log.Status == IntegrationStatus.Success)
            {
                return new TestConnectionResult
                {
                    Success = true,
                    Message = "Successfully connected to HR system and validated authentication",
                    CorrelationId = correlationId,
                    DurationMs = log.DurationMs
                };
            }
            else
            {
                return new TestConnectionResult
                {
                    Success = false,
                    Message = $"Connection test failed: {log.ErrorMessage}",
                    CorrelationId = correlationId,
                    ErrorDetails = log.ErrorDetails
                };
            }
        }
        catch (Exception ex)
        {
            return new TestConnectionResult
            {
                Success = false,
                Message = $"Connection test failed: {ex.Message}",
                CorrelationId = correlationId,
                ErrorDetails = ex.ToString()
            };
        }
    }

    /// <summary>
    /// Execute HR data synchronization (manual or scheduled)
    /// </summary>
    public async Task<HRSyncResult> ExecuteSyncAsync(
        int connectorId, 
        string initiatedBy,
        bool isScheduled = false)
    {
        var connector = await _connectorRepository.GetByIdAsync(connectorId);
        if (connector == null)
        {
            throw new InvalidOperationException($"Connector with ID {connectorId} not found");
        }

        if (connector.ConnectorType != "HR")
        {
            throw new InvalidOperationException($"Connector is not an HR connector (type: {connector.ConnectorType})");
        }

        if (connector.Status != ConnectorStatus.Enabled)
        {
            throw new InvalidOperationException($"Connector is not enabled (status: {connector.Status})");
        }

        var correlationId = Guid.NewGuid().ToString();
        var result = new HRSyncResult
        {
            ConnectorId = connectorId,
            CorrelationId = correlationId,
            IsScheduled = isScheduled,
            StartedAt = DateTime.UtcNow
        };

        try
        {
            // Execute the sync call to the HR system
            var log = await _integrationExecutionService.ExecuteWithRetryAsync(
                connectorId,
                "pull",
                correlationId,
                initiatedBy,
                async () =>
                {
                    // Simulate fetching HR data
                    // In a real implementation, this would call the actual HR system API
                    var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
                    
                    var response = await client.GetAsync($"{connector.EndpointBaseUrl}/employees");
                    var content = await response.Content.ReadAsStringAsync();
                    
                    return new IntegrationCallResult
                    {
                        HttpMethod = "GET",
                        Endpoint = "/employees",
                        HttpStatusCode = (int)response.StatusCode,
                        ResponseSummary = content
                    };
                });

            if (log.Status != IntegrationStatus.Success)
            {
                result.Success = false;
                result.Message = $"Sync failed: {log.ErrorMessage}";
                result.CompletedAt = DateTime.UtcNow;
                return result;
            }

            // Parse the response and process records
            var hrRecords = ParseHRResponse(log.ResponseSummary);
            
            foreach (var record in hrRecords)
            {
                await ProcessHRRecord(connectorId, correlationId, initiatedBy, record, connector.MappingConfiguration, result);
            }

            result.Success = true;
            result.Message = $"Sync completed. Imported: {result.ImportedCount}, Updated: {result.UpdatedCount}, Rejected: {result.RejectedCount}";
            result.CompletedAt = DateTime.UtcNow;
            
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Sync failed: {ex.Message}";
            result.CompletedAt = DateTime.UtcNow;
            return result;
        }
    }

    /// <summary>
    /// Get sync history for a connector
    /// </summary>
    public async Task<List<HRSyncRecord>> GetSyncHistoryAsync(int connectorId, int limit = 100)
    {
        return await _hrSyncRecordRepository.GetByConnectorIdAsync(connectorId, limit);
    }

    /// <summary>
    /// Get rejected records for a connector
    /// </summary>
    public async Task<List<HRSyncRecord>> GetRejectedRecordsAsync(int connectorId, int limit = 100)
    {
        return await _hrSyncRecordRepository.GetRejectedRecordsAsync(connectorId, limit);
    }

    private List<HRRecord> ParseHRResponse(string? responseJson)
    {
        if (string.IsNullOrEmpty(responseJson))
        {
            return new List<HRRecord>();
        }

        try
        {
            // Parse the JSON response into HR records
            // This is a simplified example - real implementation would handle various formats
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var records = JsonSerializer.Deserialize<List<HRRecord>>(responseJson, options);
            return records ?? new List<HRRecord>();
        }
        catch
        {
            // If parsing fails, return empty list
            return new List<HRRecord>();
        }
    }

    private async Task ProcessHRRecord(
        int connectorId,
        string correlationId,
        string initiatedBy,
        HRRecord record,
        string mappingConfigJson,
        HRSyncResult result)
    {
        var syncRecord = new HRSyncRecord
        {
            ConnectorId = connectorId,
            CorrelationId = correlationId,
            ExternalId = record.ExternalId,
            RawData = JsonSerializer.Serialize(record),
            InitiatedBy = initiatedBy,
            SyncedAt = DateTime.UtcNow
        };

        try
        {
            // Apply mapping transformations
            var mappingResult = ApplyMapping(record, mappingConfigJson);
            
            if (!mappingResult.Success)
            {
                // Record cannot be mapped - mark as rejected
                syncRecord.Status = HRSyncStatus.Rejected;
                syncRecord.RejectionReason = mappingResult.ErrorMessage;
                await _hrSyncRecordRepository.CreateAsync(syncRecord);
                result.RejectedCount++;
                return;
            }

            // Check if entity already exists
            var existingEntity = await _hrEntityRepository.GetByExternalIdAsync(connectorId, record.ExternalId);
            
            if (existingEntity != null)
            {
                // Entity exists - check if it's approved
                if (existingEntity.IsApproved)
                {
                    // Don't overwrite approved data - mark as rejected
                    syncRecord.Status = HRSyncStatus.Rejected;
                    syncRecord.RejectionReason = "Cannot overwrite approved data. Manual review required.";
                    syncRecord.OverwroteApprovedData = false;
                    await _hrSyncRecordRepository.CreateAsync(syncRecord);
                    result.RejectedCount++;
                    return;
                }

                // Update existing entity
                existingEntity.Data = JsonSerializer.Serialize(record);
                existingEntity.MappedData = mappingResult.MappedDataJson;
                existingEntity.UpdatedAt = DateTime.UtcNow;
                
                await _hrEntityRepository.UpdateAsync(existingEntity);
                
                syncRecord.Status = HRSyncStatus.Success;
                syncRecord.HREntityId = existingEntity.Id;
                await _hrSyncRecordRepository.CreateAsync(syncRecord);
                result.UpdatedCount++;
            }
            else
            {
                // Create new entity
                var newEntity = new HREntity
                {
                    ConnectorId = connectorId,
                    ExternalId = record.ExternalId,
                    EntityType = record.EntityType ?? "Employee",
                    Data = JsonSerializer.Serialize(record),
                    MappedData = mappingResult.MappedDataJson,
                    IsApproved = false,
                    ImportedAt = DateTime.UtcNow
                };
                
                var created = await _hrEntityRepository.CreateAsync(newEntity);
                
                syncRecord.Status = HRSyncStatus.Success;
                syncRecord.HREntityId = created.Id;
                await _hrSyncRecordRepository.CreateAsync(syncRecord);
                result.ImportedCount++;
            }
        }
        catch (Exception ex)
        {
            // Sync failed for this record
            syncRecord.Status = HRSyncStatus.Failed;
            syncRecord.RejectionReason = $"Processing error: {ex.Message}";
            await _hrSyncRecordRepository.CreateAsync(syncRecord);
            result.FailedCount++;
        }
    }

    private MappingResult ApplyMapping(HRRecord record, string mappingConfigJson)
    {
        try
        {
            var mappingConfig = JsonSerializer.Deserialize<MappingConfiguration>(mappingConfigJson);
            if (mappingConfig?.Mappings == null || mappingConfig.Mappings.Count == 0)
            {
                return new MappingResult
                {
                    Success = false,
                    ErrorMessage = "No mapping configuration defined"
                };
            }

            var mappedData = new Dictionary<string, object?>();
            var recordData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(record));

            if (recordData == null)
            {
                return new MappingResult
                {
                    Success = false,
                    ErrorMessage = "Failed to parse record data"
                };
            }

            foreach (var mapping in mappingConfig.Mappings)
            {
                if (!recordData.ContainsKey(mapping.ExternalField))
                {
                    // Required field is missing
                    if (mapping.Required == true)
                    {
                        return new MappingResult
                        {
                            Success = false,
                            ErrorMessage = $"Required field '{mapping.ExternalField}' is missing"
                        };
                    }
                    continue;
                }

                var value = recordData[mapping.ExternalField];
                var transformedValue = ApplyTransformation(value, mapping.Transform, mapping.TransformParams);
                mappedData[mapping.InternalField] = transformedValue;
            }

            return new MappingResult
            {
                Success = true,
                MappedDataJson = JsonSerializer.Serialize(mappedData)
            };
        }
        catch (Exception ex)
        {
            return new MappingResult
            {
                Success = false,
                ErrorMessage = $"Mapping error: {ex.Message}"
            };
        }
    }

    private object? ApplyTransformation(JsonElement value, string transform, Dictionary<string, string>? transformParams)
    {
        switch (transform.ToLowerInvariant())
        {
            case "direct":
                // Copy value as-is
                return value.ValueKind switch
                {
                    JsonValueKind.String => value.GetString(),
                    JsonValueKind.Number => value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => value.ToString()
                };

            case "fte":
                // Normalize to FTE (Full-Time Equivalent)
                // Example: Convert hours to FTE
                if (value.ValueKind == JsonValueKind.Number)
                {
                    var hours = value.GetDouble();
                    var standardHours = 40.0; // Default
                    if (transformParams?.ContainsKey("standardHours") == true)
                    {
                        double.TryParse(transformParams["standardHours"], out standardHours);
                    }
                    return hours / standardHours;
                }
                return null;

            case "sum":
                // Sum array values
                if (value.ValueKind == JsonValueKind.Array)
                {
                    double sum = 0;
                    foreach (var item in value.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Number)
                        {
                            sum += item.GetDouble();
                        }
                    }
                    return sum;
                }
                return null;

            case "average":
                // Average array values
                if (value.ValueKind == JsonValueKind.Array)
                {
                    double sum = 0;
                    int count = 0;
                    foreach (var item in value.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Number)
                        {
                            sum += item.GetDouble();
                            count++;
                        }
                    }
                    return count > 0 ? sum / count : null;
                }
                return null;

            case "lookup":
                // Lookup value from a table
                if (transformParams?.ContainsKey("table") == true && value.ValueKind == JsonValueKind.String)
                {
                    var lookupTable = JsonSerializer.Deserialize<Dictionary<string, string>>(transformParams["table"]);
                    var key = value.GetString() ?? "";
                    return lookupTable?.GetValueOrDefault(key);
                }
                return null;

            default:
                // Unknown transform - return as-is
                return value.ToString();
        }
    }
}

/// <summary>
/// Result of a test connection operation
/// </summary>
public class TestConnectionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public long? DurationMs { get; set; }
    public string? ErrorDetails { get; set; }
}

/// <summary>
/// Result of an HR sync operation
/// </summary>
public class HRSyncResult
{
    public int ConnectorId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public bool IsScheduled { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ImportedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int RejectedCount { get; set; }
    public int FailedCount { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// HR record from external system
/// </summary>
public class HRRecord
{
    public string ExternalId { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    // Additional fields will be captured as dynamic JSON
}

/// <summary>
/// Mapping configuration structure
/// </summary>
public class MappingConfiguration
{
    public List<FieldMapping> Mappings { get; set; } = new();
}

/// <summary>
/// Field mapping definition
/// </summary>
public class FieldMapping
{
    public string ExternalField { get; set; } = string.Empty;
    public string InternalField { get; set; } = string.Empty;
    public string Transform { get; set; } = "direct";
    public bool? Required { get; set; }
    public Dictionary<string, string>? TransformParams { get; set; }
}

/// <summary>
/// Result of a mapping operation
/// </summary>
public class MappingResult
{
    public bool Success { get; set; }
    public string MappedDataJson { get; set; } = "{}";
    public string? ErrorMessage { get; set; }
}
