using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using SD.ProjectName.Modules.Integrations.Application;
using SD.ProjectName.Modules.Integrations.Domain.Entities;

namespace ARP.ESG_ReportStudio.API.Controllers.Integrations;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class ConnectorsController : ControllerBase
{
    private readonly ConnectorService _connectorService;
    private readonly IntegrationExecutionService _integrationExecutionService;

    public ConnectorsController(
        ConnectorService connectorService,
        IntegrationExecutionService integrationExecutionService)
    {
        _connectorService = connectorService;
        _integrationExecutionService = integrationExecutionService;
    }

    /// <summary>
    /// Get all connectors
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ConnectorResponse>>> GetAllConnectors()
    {
        var connectors = await _connectorService.GetAllConnectorsAsync();
        var response = connectors.Select(c => MapToResponse(c)).ToList();
        return Ok(response);
    }

    /// <summary>
    /// Get a connector by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ConnectorResponse>> GetConnector(int id)
    {
        var connector = await _connectorService.GetConnectorByIdAsync(id);
        if (connector == null)
        {
            return NotFound(new { message = $"Connector with ID {id} not found" });
        }

        return Ok(MapToResponse(connector));
    }

    /// <summary>
    /// Create a new connector
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ConnectorResponse>> CreateConnector([FromBody] CreateConnectorRequest request)
    {
        // Get current user from claims (defaulting to "system" for now)
        var currentUser = User.Identity?.Name ?? "system";

        var connector = await _connectorService.CreateConnectorAsync(
            request.Name,
            request.ConnectorType,
            request.EndpointBaseUrl,
            request.AuthenticationType,
            request.AuthenticationSecretRef,
            request.Capabilities,
            currentUser,
            request.RateLimitPerMinute,
            request.MaxRetryAttempts,
            request.RetryDelaySeconds,
            request.UseExponentialBackoff,
            request.Description);

        return CreatedAtAction(
            nameof(GetConnector),
            new { id = connector.Id },
            MapToResponse(connector));
    }

    /// <summary>
    /// Update an existing connector
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ConnectorResponse>> UpdateConnector(int id, [FromBody] UpdateConnectorRequest request)
    {
        var currentUser = User.Identity?.Name ?? "system";

        try
        {
            var connector = await _connectorService.UpdateConnectorAsync(
                id,
                request.Name,
                request.EndpointBaseUrl,
                request.AuthenticationType,
                request.AuthenticationSecretRef,
                request.Capabilities,
                currentUser,
                request.RateLimitPerMinute,
                request.MaxRetryAttempts,
                request.RetryDelaySeconds,
                request.UseExponentialBackoff,
                request.Description,
                request.MappingConfiguration);

            return Ok(MapToResponse(connector));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Enable a connector
    /// </summary>
    [HttpPost("{id}/enable")]
    public async Task<ActionResult<ConnectorResponse>> EnableConnector(int id)
    {
        var currentUser = User.Identity?.Name ?? "system";

        try
        {
            var connector = await _connectorService.EnableConnectorAsync(id, currentUser);
            return Ok(MapToResponse(connector));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Disable a connector
    /// </summary>
    [HttpPost("{id}/disable")]
    public async Task<ActionResult<ConnectorResponse>> DisableConnector(int id)
    {
        var currentUser = User.Identity?.Name ?? "system";

        try
        {
            var connector = await _connectorService.DisableConnectorAsync(id, currentUser);
            return Ok(MapToResponse(connector));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get integration logs for a connector
    /// </summary>
    [HttpGet("{id}/logs")]
    public async Task<ActionResult<List<IntegrationLogResponse>>> GetConnectorLogs(int id, [FromQuery] int limit = 100)
    {
        var logs = await _integrationExecutionService.GetConnectorLogsAsync(id, limit);
        var response = logs.Select(l => MapLogToResponse(l)).ToList();
        return Ok(response);
    }

    /// <summary>
    /// Get integration logs by correlation ID
    /// </summary>
    [HttpGet("logs/by-correlation/{correlationId}")]
    public async Task<ActionResult<List<IntegrationLogResponse>>> GetLogsByCorrelationId(string correlationId)
    {
        var logs = await _integrationExecutionService.GetLogsByCorrelationIdAsync(correlationId);
        var response = logs.Select(l => MapLogToResponse(l)).ToList();
        return Ok(response);
    }

    private static ConnectorResponse MapToResponse(Connector connector)
    {
        return new ConnectorResponse
        {
            Id = connector.Id,
            Name = connector.Name,
            ConnectorType = connector.ConnectorType,
            Status = connector.Status.ToString(),
            EndpointBaseUrl = connector.EndpointBaseUrl,
            AuthenticationType = connector.AuthenticationType,
            AuthenticationSecretRef = connector.AuthenticationSecretRef,
            Capabilities = connector.Capabilities,
            RateLimitPerMinute = connector.RateLimitPerMinute,
            MaxRetryAttempts = connector.MaxRetryAttempts,
            RetryDelaySeconds = connector.RetryDelaySeconds,
            UseExponentialBackoff = connector.UseExponentialBackoff,
            MappingConfiguration = connector.MappingConfiguration,
            Description = connector.Description,
            CreatedAt = connector.CreatedAt,
            CreatedBy = connector.CreatedBy,
            UpdatedAt = connector.UpdatedAt,
            UpdatedBy = connector.UpdatedBy
        };
    }

    private static IntegrationLogResponse MapLogToResponse(IntegrationLog log)
    {
        return new IntegrationLogResponse
        {
            Id = log.Id,
            ConnectorId = log.ConnectorId,
            ConnectorName = log.Connector?.Name,
            CorrelationId = log.CorrelationId,
            OperationType = log.OperationType,
            Status = log.Status.ToString(),
            HttpMethod = log.HttpMethod,
            Endpoint = log.Endpoint,
            HttpStatusCode = log.HttpStatusCode,
            RetryAttempts = log.RetryAttempts,
            ErrorMessage = log.ErrorMessage,
            ErrorDetails = log.ErrorDetails,
            DurationMs = log.DurationMs,
            StartedAt = log.StartedAt,
            CompletedAt = log.CompletedAt,
            InitiatedBy = log.InitiatedBy
        };
    }
}
