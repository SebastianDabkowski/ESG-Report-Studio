using SD.ProjectName.Modules.Integrations.Domain.Entities;
using SD.ProjectName.Modules.Integrations.Domain.Interfaces;

namespace SD.ProjectName.Modules.Integrations.Application;

/// <summary>
/// Service for executing integration calls with retry logic and logging
/// </summary>
public class IntegrationExecutionService
{
    private readonly IConnectorRepository _connectorRepository;
    private readonly IIntegrationLogRepository _integrationLogRepository;

    public IntegrationExecutionService(
        IConnectorRepository connectorRepository,
        IIntegrationLogRepository integrationLogRepository)
    {
        _connectorRepository = connectorRepository;
        _integrationLogRepository = integrationLogRepository;
    }

    /// <summary>
    /// Execute an integration call with retry logic
    /// </summary>
    /// <param name="connectorId">ID of the connector to use</param>
    /// <param name="operationType">Type of operation (pull/push/webhook)</param>
    /// <param name="correlationId">Correlation ID for tracing</param>
    /// <param name="initiatedBy">User or service initiating the call</param>
    /// <param name="executeCallAsync">Function to execute the actual integration call</param>
    /// <returns>Integration log with result</returns>
    public async Task<IntegrationLog> ExecuteWithRetryAsync(
        int connectorId,
        string operationType,
        string correlationId,
        string initiatedBy,
        Func<Task<IntegrationCallResult>> executeCallAsync)
    {
        var connector = await _connectorRepository.GetByIdAsync(connectorId);
        if (connector == null)
        {
            throw new InvalidOperationException($"Connector with ID {connectorId} not found");
        }

        var log = new IntegrationLog
        {
            ConnectorId = connectorId,
            CorrelationId = correlationId,
            OperationType = operationType,
            InitiatedBy = initiatedBy,
            StartedAt = DateTime.UtcNow,
            Status = IntegrationStatus.InProgress
        };

        // Check if connector is disabled
        if (connector.Status == ConnectorStatus.Disabled)
        {
            log.Status = IntegrationStatus.Skipped;
            log.ErrorMessage = "Connector is disabled. No outbound calls will be executed.";
            log.CompletedAt = DateTime.UtcNow;
            log.DurationMs = 0;
            
            return await _integrationLogRepository.CreateAsync(log);
        }

        var startTime = DateTime.UtcNow;
        IntegrationCallResult? result = null;
        Exception? lastException = null;

        // Execute with retry logic
        for (int attempt = 0; attempt <= connector.MaxRetryAttempts; attempt++)
        {
            try
            {
                if (attempt > 0)
                {
                    // Calculate delay for retry
                    var delaySeconds = connector.UseExponentialBackoff
                        ? connector.RetryDelaySeconds * Math.Pow(2, attempt - 1)
                        : connector.RetryDelaySeconds;
                    
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                }

                result = await executeCallAsync();
                
                // Success
                log.Status = IntegrationStatus.Success;
                log.HttpMethod = result.HttpMethod;
                log.Endpoint = result.Endpoint;
                log.HttpStatusCode = result.HttpStatusCode;
                log.RequestSummary = result.RequestSummary;
                log.ResponseSummary = result.ResponseSummary;
                log.RetryAttempts = attempt;
                break;
            }
            catch (Exception ex)
            {
                lastException = ex;
                log.RetryAttempts = attempt;

                if (attempt >= connector.MaxRetryAttempts)
                {
                    // All retries exhausted
                    log.Status = IntegrationStatus.Failed;
                    log.ErrorMessage = ex.Message;
                    log.ErrorDetails = ex.ToString();
                }
            }
        }

        var endTime = DateTime.UtcNow;
        log.CompletedAt = endTime;
        log.DurationMs = (long)(endTime - startTime).TotalMilliseconds;

        return await _integrationLogRepository.CreateAsync(log);
    }

    /// <summary>
    /// Get integration logs for a connector
    /// </summary>
    public async Task<List<IntegrationLog>> GetConnectorLogsAsync(int connectorId, int limit = 100)
    {
        return await _integrationLogRepository.GetByConnectorIdAsync(connectorId, limit);
    }

    /// <summary>
    /// Get integration logs by correlation ID
    /// </summary>
    public async Task<List<IntegrationLog>> GetLogsByCorrelationIdAsync(string correlationId)
    {
        return await _integrationLogRepository.GetByCorrelationIdAsync(correlationId);
    }
}

/// <summary>
/// Result of an integration call
/// </summary>
public class IntegrationCallResult
{
    public string? HttpMethod { get; set; }
    public string? Endpoint { get; set; }
    public int? HttpStatusCode { get; set; }
    public string? RequestSummary { get; set; }
    public string? ResponseSummary { get; set; }
}
