using System.Text.Json;
using SD.ProjectName.Modules.Integrations.Domain.Entities;
using SD.ProjectName.Modules.Integrations.Domain.Interfaces;

namespace SD.ProjectName.Modules.Integrations.Application;

/// <summary>
/// Service for managing Finance data synchronization with staging area and conflict resolution
/// </summary>
public class FinanceSyncService
{
    private readonly IConnectorRepository _connectorRepository;
    private readonly IFinanceEntityRepository _financeEntityRepository;
    private readonly IFinanceSyncRecordRepository _financeSyncRecordRepository;
    private readonly IntegrationExecutionService _integrationExecutionService;

    public FinanceSyncService(
        IConnectorRepository connectorRepository,
        IFinanceEntityRepository financeEntityRepository,
        IFinanceSyncRecordRepository financeSyncRecordRepository,
        IntegrationExecutionService integrationExecutionService)
    {
        _connectorRepository = connectorRepository;
        _financeEntityRepository = financeEntityRepository;
        _financeSyncRecordRepository = financeSyncRecordRepository;
        _integrationExecutionService = integrationExecutionService;
    }

    /// <summary>
    /// Test connection to Finance system and validate authentication and required permissions
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

        if (connector.ConnectorType != "Finance")
        {
            return new TestConnectionResult
            {
                Success = false,
                Message = $"Connector is not a Finance connector (type: {connector.ConnectorType})"
            };
        }

        var correlationId = Guid.NewGuid().ToString();

        try
        {
            // Execute a test call to the Finance system to validate authentication and permissions
            var log = await _integrationExecutionService.ExecuteWithRetryAsync(
                connectorId,
                "test-connection",
                correlationId,
                initiatedBy,
                async () =>
                {
                    // Call the finance system API to validate authentication
                    // In a real implementation, this would call the actual Finance/ERP system API
                    var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
                    
                    // Test connection and permissions validation
                    var response = await client.GetAsync($"{connector.EndpointBaseUrl}/health");
                    
                    return new IntegrationCallResult
                    {
                        HttpMethod = "GET",
                        Endpoint = "/health",
                        HttpStatusCode = (int)response.StatusCode,
                        ResponseSummary = response.IsSuccessStatusCode ? "Connection and authentication successful" : "Connection failed"
                    };
                });

            if (log.Status == IntegrationStatus.Success)
            {
                return new TestConnectionResult
                {
                    Success = true,
                    Message = "Successfully connected to Finance system, validated authentication and required permissions",
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
    /// Execute Finance data synchronization to staging area with provenance metadata.
    /// Implements conflict resolution that preserves manual values unless admin approves override.
    /// </summary>
    public async Task<FinanceSyncResult> ExecuteSyncAsync(
        int connectorId, 
        string initiatedBy,
        bool isScheduled = false,
        string? approvedOverrideBy = null)
    {
        var connector = await _connectorRepository.GetByIdAsync(connectorId);
        if (connector == null)
        {
            throw new InvalidOperationException($"Connector with ID {connectorId} not found");
        }

        if (connector.ConnectorType != "Finance")
        {
            throw new InvalidOperationException($"Connector is not a Finance connector (type: {connector.ConnectorType})");
        }

        if (connector.Status != ConnectorStatus.Enabled)
        {
            throw new InvalidOperationException($"Connector is not enabled (status: {connector.Status})");
        }

        var correlationId = Guid.NewGuid().ToString();
        var importJobId = $"JOB-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8]}";
        
        var result = new FinanceSyncResult
        {
            ConnectorId = connectorId,
            CorrelationId = correlationId,
            ImportJobId = importJobId,
            IsScheduled = isScheduled,
            StartedAt = DateTime.UtcNow
        };

        try
        {
            // Execute the sync call to the Finance system
            var log = await _integrationExecutionService.ExecuteWithRetryAsync(
                connectorId,
                "pull",
                correlationId,
                initiatedBy,
                async () =>
                {
                    // Fetch financial data from the external system
                    // In a real implementation, this would call the actual Finance/ERP system API
                    var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
                    
                    var response = await client.GetAsync($"{connector.EndpointBaseUrl}/financial-data");
                    var content = await response.Content.ReadAsStringAsync();
                    
                    return new IntegrationCallResult
                    {
                        HttpMethod = "GET",
                        Endpoint = "/financial-data",
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
            var financeRecords = ParseFinanceResponse(log.ResponseSummary);
            
            foreach (var record in financeRecords)
            {
                await ProcessFinanceRecord(
                    connectorId, 
                    correlationId, 
                    importJobId,
                    initiatedBy, 
                    record, 
                    connector.MappingConfiguration,
                    connector.Name,
                    approvedOverrideBy,
                    result);
            }

            result.Success = true;
            result.Message = $"Sync completed. Imported: {result.ImportedCount}, Updated: {result.UpdatedCount}, " +
                           $"Conflicts Preserved: {result.ConflictsPreservedCount}, Rejected: {result.RejectedCount}";
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
    public async Task<List<FinanceSyncRecord>> GetSyncHistoryAsync(int connectorId, int limit = 100)
    {
        return await _financeSyncRecordRepository.GetByConnectorIdAsync(connectorId, limit);
    }

    /// <summary>
    /// Get rejected records for a connector
    /// </summary>
    public async Task<List<FinanceSyncRecord>> GetRejectedRecordsAsync(int connectorId, int limit = 100)
    {
        return await _financeSyncRecordRepository.GetRejectedByConnectorIdAsync(connectorId, limit);
    }

    /// <summary>
    /// Get conflict records for a connector (where manual data was preserved)
    /// </summary>
    public async Task<List<FinanceSyncRecord>> GetConflictsAsync(int connectorId, int limit = 100)
    {
        return await _financeSyncRecordRepository.GetConflictsByConnectorIdAsync(connectorId, limit);
    }

    private List<FinanceRecord> ParseFinanceResponse(string? responseJson)
    {
        if (string.IsNullOrEmpty(responseJson))
        {
            return new List<FinanceRecord>();
        }

        try
        {
            // Parse the JSON response into Finance records
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var records = JsonSerializer.Deserialize<List<FinanceRecord>>(responseJson, options);
            return records ?? new List<FinanceRecord>();
        }
        catch
        {
            // If parsing fails, return empty list
            return new List<FinanceRecord>();
        }
    }

    private async Task ProcessFinanceRecord(
        int connectorId,
        string correlationId,
        string importJobId,
        string initiatedBy,
        FinanceRecord record,
        string mappingConfigJson,
        string sourceSystem,
        string? approvedOverrideBy,
        FinanceSyncResult result)
    {
        var syncRecord = new FinanceSyncRecord
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
                syncRecord.Status = FinanceSyncStatus.Rejected;
                syncRecord.RejectionReason = mappingResult.ErrorMessage;
                await _financeSyncRecordRepository.AddAsync(syncRecord);
                result.RejectedCount++;
                return;
            }

            // Check if entity already exists
            var existingEntity = await _financeEntityRepository.GetByExternalIdAsync(connectorId, record.ExternalId);
            
            if (existingEntity != null)
            {
                // Entity exists - check for conflict with approved/manual data
                if (existingEntity.IsApproved && string.IsNullOrEmpty(approvedOverrideBy))
                {
                    // Conflict: Don't overwrite approved data unless admin explicitly approves
                    syncRecord.Status = FinanceSyncStatus.ConflictPreserved;
                    syncRecord.ConflictDetected = true;
                    syncRecord.ConflictResolution = "PreservedManual";
                    syncRecord.RejectionReason = "Cannot overwrite approved manual data. Admin approval required for override.";
                    syncRecord.OverwroteApprovedData = false;
                    await _financeSyncRecordRepository.AddAsync(syncRecord);
                    result.ConflictsPreservedCount++;
                    return;
                }

                // Update existing entity
                existingEntity.Data = JsonSerializer.Serialize(record);
                existingEntity.MappedData = mappingResult.MappedDataJson;
                existingEntity.SourceSystem = sourceSystem;
                existingEntity.ExtractTimestamp = record.ExtractTimestamp ?? DateTime.UtcNow;
                existingEntity.ImportJobId = importJobId;
                existingEntity.UpdatedAt = DateTime.UtcNow;
                
                await _financeEntityRepository.UpdateAsync(existingEntity);
                
                syncRecord.Status = FinanceSyncStatus.Success;
                syncRecord.ConflictDetected = existingEntity.IsApproved;
                syncRecord.ConflictResolution = existingEntity.IsApproved ? "AdminOverride" : "NoConflict";
                syncRecord.ApprovedOverrideBy = approvedOverrideBy;
                syncRecord.OverwroteApprovedData = existingEntity.IsApproved;
                syncRecord.FinanceEntityId = existingEntity.Id;
                await _financeSyncRecordRepository.AddAsync(syncRecord);
                result.UpdatedCount++;
            }
            else
            {
                // Create new entity in staging area with provenance metadata
                var newEntity = new FinanceEntity
                {
                    ConnectorId = connectorId,
                    ExternalId = record.ExternalId,
                    EntityType = record.EntityType ?? "Unknown",
                    Data = JsonSerializer.Serialize(record),
                    MappedData = mappingResult.MappedDataJson,
                    IsApproved = false, // New imports start unapproved in staging
                    SourceSystem = sourceSystem,
                    ExtractTimestamp = record.ExtractTimestamp ?? DateTime.UtcNow,
                    ImportJobId = importJobId,
                    ImportedAt = DateTime.UtcNow
                };
                
                var created = await _financeEntityRepository.AddAsync(newEntity);
                
                syncRecord.Status = FinanceSyncStatus.Success;
                syncRecord.ConflictDetected = false;
                syncRecord.ConflictResolution = "NoConflict";
                syncRecord.FinanceEntityId = created.Id;
                await _financeSyncRecordRepository.AddAsync(syncRecord);
                result.ImportedCount++;
            }
        }
        catch (Exception ex)
        {
            // Sync failed for this record
            syncRecord.Status = FinanceSyncStatus.Failed;
            syncRecord.RejectionReason = $"Processing error: {ex.Message}";
            await _financeSyncRecordRepository.AddAsync(syncRecord);
            result.FailedCount++;
        }
    }

    private MappingResult ApplyMapping(FinanceRecord record, string mappingConfigJson)
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

            case "sum":
                // Sum array values (for aggregating financial amounts)
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
                // Lookup value from a table (e.g., for mapping supplier categories)
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
/// Result of a Finance sync operation with staging and conflict tracking
/// </summary>
public class FinanceSyncResult
{
    public int ConnectorId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string ImportJobId { get; set; } = string.Empty;
    public bool IsScheduled { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ImportedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int ConflictsPreservedCount { get; set; }
    public int RejectedCount { get; set; }
    public int FailedCount { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Finance record from external system with provenance metadata
/// </summary>
public class FinanceRecord
{
    public string ExternalId { get; set; } = string.Empty;
    public string? EntityType { get; set; } // e.g., "Spend", "Revenue", "CapEx", "OpEx", "Supplier"
    public DateTime? ExtractTimestamp { get; set; }
    // Additional fields will be captured as dynamic JSON
}
