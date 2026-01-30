using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// Controller for managing rollover rules configuration.
/// Defines how different data types are handled during period rollover.
/// </summary>
/// <remarks>
/// SECURITY NOTE: In production, this controller should have:
/// - [Authorize] attribute at controller level for authenticated access
/// - Role-based authorization checks to ensure:
///   * Only admins can create, update, or delete rollover rules
///   * Contributors and auditors can view rules but not modify them
/// - Additional authorization checks in each method to validate user permissions
/// </remarks>
[ApiController]
[Route("api/rollover-rules")]
public sealed class RolloverRulesController : ControllerBase
{
    private readonly InMemoryReportStore _store;
    private readonly ILogger<RolloverRulesController> _logger;

    public RolloverRulesController(
        InMemoryReportStore store,
        ILogger<RolloverRulesController> logger)
    {
        _store = store;
        _logger = logger;
    }

    /// <summary>
    /// Gets all rollover rules or a specific rule by data type.
    /// </summary>
    /// <param name="dataType">Optional data type filter (e.g., "narrative", "metric", "kpi", "policy")</param>
    /// <returns>List of rollover rules</returns>
    [HttpGet]
    public ActionResult<IReadOnlyList<DataTypeRolloverRule>> GetRolloverRules([FromQuery] string? dataType = null)
    {
        try
        {
            var rules = _store.GetRolloverRules(dataType);
            return Ok(rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rollover rules");
            return StatusCode(500, new { error = "Failed to retrieve rollover rules" });
        }
    }

    /// <summary>
    /// Gets the rollover rule for a specific data type.
    /// </summary>
    /// <param name="dataType">Data type to get rule for</param>
    /// <returns>Rollover rule or 404 if not found</returns>
    [HttpGet("{dataType}")]
    public ActionResult<DataTypeRolloverRule> GetRolloverRuleForDataType(string dataType)
    {
        try
        {
            var rule = _store.GetRolloverRuleForDataType(dataType);
            
            if (rule == null)
            {
                return NotFound(new { error = $"No rollover rule found for data type '{dataType}'" });
            }
            
            return Ok(rule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rollover rule for data type {DataType}", dataType);
            return StatusCode(500, new { error = "Failed to retrieve rollover rule" });
        }
    }

    /// <summary>
    /// Creates or updates a rollover rule for a data type.
    /// </summary>
    /// <param name="request">Rule configuration request</param>
    /// <returns>Created or updated rule</returns>
    [HttpPost]
    public ActionResult<DataTypeRolloverRule> SaveRolloverRule([FromBody] SaveDataTypeRolloverRuleRequest request)
    {
        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.DataType))
            {
                return BadRequest(new { error = "DataType is required" });
            }

            if (string.IsNullOrWhiteSpace(request.RuleType))
            {
                return BadRequest(new { error = "RuleType is required" });
            }

            if (string.IsNullOrWhiteSpace(request.SavedBy))
            {
                return BadRequest(new { error = "SavedBy is required" });
            }

            // Validate rule type
            var validRuleTypes = new[] { "copy", "reset", "copyasdraft", "copy-as-draft" };
            if (!validRuleTypes.Contains(request.RuleType.ToLowerInvariant().Replace("-", "")))
            {
                return BadRequest(new { error = $"Invalid RuleType. Valid values are: copy, reset, copy-as-draft" });
            }

            var rule = _store.SaveRolloverRule(request);

            _logger.LogInformation(
                "Rollover rule saved for data type {DataType}: {RuleType} (Version {Version})",
                rule.DataType, rule.RuleType, rule.Version);

            return Ok(rule);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving rollover rule for data type {DataType}", request.DataType);
            return StatusCode(500, new { error = "Failed to save rollover rule" });
        }
    }

    /// <summary>
    /// Deletes a rollover rule for a data type (resets to default Copy behavior).
    /// </summary>
    /// <param name="dataType">Data type to reset rule for</param>
    /// <param name="deletedBy">User ID performing the deletion</param>
    /// <returns>Success status</returns>
    [HttpDelete("{dataType}")]
    public ActionResult DeleteRolloverRule(string dataType, [FromQuery] string deletedBy)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(deletedBy))
            {
                return BadRequest(new { error = "deletedBy query parameter is required" });
            }

            var deleted = _store.DeleteRolloverRule(dataType, deletedBy);

            if (!deleted)
            {
                return NotFound(new { error = $"No rollover rule found for data type '{dataType}'" });
            }

            _logger.LogInformation(
                "Rollover rule deleted for data type {DataType} by user {DeletedBy}",
                dataType, deletedBy);

            return Ok(new { message = $"Rollover rule for '{dataType}' deleted successfully (reset to default)" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting rollover rule for data type {DataType}", dataType);
            return StatusCode(500, new { error = "Failed to delete rollover rule" });
        }
    }

    /// <summary>
    /// Gets the change history for a rollover rule.
    /// </summary>
    /// <param name="dataType">Data type to get history for</param>
    /// <returns>List of historical changes</returns>
    [HttpGet("{dataType}/history")]
    public ActionResult<IReadOnlyList<RolloverRuleHistory>> GetRolloverRuleHistory(string dataType)
    {
        try
        {
            var history = _store.GetRolloverRuleHistory(dataType);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rollover rule history for data type {DataType}", dataType);
            return StatusCode(500, new { error = "Failed to retrieve rollover rule history" });
        }
    }
}
