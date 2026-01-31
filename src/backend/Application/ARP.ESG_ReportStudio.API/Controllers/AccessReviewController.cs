using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;
using System.Text;
using System.Text.Json;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// Controller for periodic access reviews.
/// Enables Compliance Officers to review user access, roles, and section scopes,
/// and make decisions to retain or revoke access.
/// </summary>
[ApiController]
[Route("api/access-reviews")]
public sealed class AccessReviewController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public AccessReviewController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Start a new access review.
    /// Lists all active users with their roles, section scopes, and owned reporting periods.
    /// </summary>
    /// <param name="request">Access review initiation request</param>
    /// <response code="201">Access review started successfully</response>
    /// <response code="400">Invalid request</response>
    [HttpPost]
    [ProducesResponseType(typeof(AccessReview), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<AccessReview> StartAccessReview([FromBody] StartAccessReviewRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { error = "Review title is required." });
        }

        if (string.IsNullOrWhiteSpace(request.StartedBy))
        {
            return BadRequest(new { error = "StartedBy user ID is required." });
        }

        var review = _store.StartAccessReview(request);
        
        return CreatedAtAction(nameof(GetAccessReview), new { id = review.Id }, review);
    }

    /// <summary>
    /// Get all access reviews.
    /// </summary>
    /// <response code="200">Returns list of access reviews</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AccessReview>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<AccessReview>> GetAccessReviews()
    {
        var reviews = _store.GetAccessReviews();
        return Ok(reviews);
    }

    /// <summary>
    /// Get a specific access review by ID.
    /// </summary>
    /// <param name="id">Access review ID</param>
    /// <response code="200">Returns the access review</response>
    /// <response code="404">Access review not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AccessReview), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<AccessReview> GetAccessReview(string id)
    {
        var review = _store.GetAccessReview(id);
        if (review == null)
        {
            return NotFound(new { error = $"Access review with ID '{id}' not found." });
        }

        return Ok(review);
    }

    /// <summary>
    /// Record a review decision for a user.
    /// Decision can be "retain" to keep access or "revoke" to remove access.
    /// Revocation removes all roles and section grants immediately and is logged.
    /// </summary>
    /// <param name="id">Access review ID</param>
    /// <param name="request">Review decision request</param>
    /// <response code="200">Decision recorded successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="404">Access review not found</response>
    [HttpPost("{id}/decisions")]
    [ProducesResponseType(typeof(AccessReview), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<AccessReview> RecordReviewDecision(string id, [FromBody] RecordReviewDecisionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EntryId))
        {
            return BadRequest(new { error = "Entry ID is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Decision))
        {
            return BadRequest(new { error = "Decision is required." });
        }

        if (request.Decision != "retain" && request.Decision != "revoke")
        {
            return BadRequest(new { error = "Decision must be 'retain' or 'revoke'." });
        }

        if (string.IsNullOrWhiteSpace(request.DecisionBy))
        {
            return BadRequest(new { error = "DecisionBy user ID is required." });
        }

        var (success, errorMessage) = _store.RecordReviewDecision(id, request);
        if (!success)
        {
            if (errorMessage?.Contains("not found") == true)
            {
                return NotFound(new { error = errorMessage });
            }
            return BadRequest(new { error = errorMessage });
        }

        var review = _store.GetAccessReview(id);
        return Ok(review);
    }

    /// <summary>
    /// Complete an access review.
    /// Marks the review as completed and prevents further changes.
    /// </summary>
    /// <param name="id">Access review ID</param>
    /// <param name="request">Completion request</param>
    /// <response code="200">Review completed successfully</response>
    /// <response code="400">Invalid request or review already completed</response>
    /// <response code="404">Access review not found</response>
    [HttpPost("{id}/complete")]
    [ProducesResponseType(typeof(AccessReview), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<AccessReview> CompleteAccessReview(string id, [FromBody] CompleteAccessReviewRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompletedBy))
        {
            return BadRequest(new { error = "CompletedBy user ID is required." });
        }

        var (success, errorMessage) = _store.CompleteAccessReview(id, request);
        if (!success)
        {
            if (errorMessage?.Contains("not found") == true)
            {
                return NotFound(new { error = errorMessage });
            }
            return BadRequest(new { error = errorMessage });
        }

        var review = _store.GetAccessReview(id);
        return Ok(review);
    }

    /// <summary>
    /// Get audit log for an access review.
    /// Includes all decisions, revocations, and status changes.
    /// </summary>
    /// <param name="id">Access review ID</param>
    /// <response code="200">Returns access review log entries</response>
    [HttpGet("{id}/log")]
    [ProducesResponseType(typeof(IReadOnlyList<AccessReviewLogEntry>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<AccessReviewLogEntry>> GetAccessReviewLog(string id)
    {
        var log = _store.GetAccessReviewLog(id);
        return Ok(log);
    }

    /// <summary>
    /// Export access review results as CSV.
    /// Includes all users, decisions, reviewers, and timestamps.
    /// </summary>
    /// <param name="id">Access review ID</param>
    /// <response code="200">Returns CSV file</response>
    /// <response code="404">Access review not found</response>
    [HttpGet("{id}/export/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult ExportAccessReviewCsv(string id)
    {
        var review = _store.GetAccessReview(id);
        if (review == null)
        {
            return NotFound(new { error = $"Access review with ID '{id}' not found." });
        }

        var csv = new StringBuilder();
        // Add UTF-8 BOM for better Excel compatibility
        csv.Append('\ufeff');
        csv.AppendLine("\"User ID\",\"User Name\",\"User Email\",\"Is Active\",\"Roles\",\"Section Count\",\"Owned Periods\",\"Decision\",\"Decision At\",\"Decision By\",\"Decision Note\"");
        
        foreach (var entry in review.Entries)
        {
            var roles = string.Join("; ", entry.RoleNames);
            var ownedPeriods = string.Join("; ", entry.OwnedPeriodIds);
            
            csv.AppendLine($"{FormatCsvField(entry.UserId)},{FormatCsvField(entry.UserName)},{FormatCsvField(entry.UserEmail)},{entry.IsActive},{FormatCsvField(roles)},{entry.SectionScopes.Count},{FormatCsvField(ownedPeriods)},{FormatCsvField(entry.Decision)},{FormatCsvField(entry.DecisionAt ?? "")},{FormatCsvField(entry.DecisionByName ?? "")},{FormatCsvField(entry.DecisionNote ?? "")}");
        }
        
        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        var fileName = $"access-review-{review.Id}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        
        return File(bytes, "text/csv", fileName);
    }

    /// <summary>
    /// Export access review results as JSON.
    /// Includes complete review data with decisions, reviewers, and timestamps.
    /// </summary>
    /// <param name="id">Access review ID</param>
    /// <response code="200">Returns JSON file</response>
    /// <response code="404">Access review not found</response>
    [HttpGet("{id}/export/json")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult ExportAccessReviewJson(string id)
    {
        var review = _store.GetAccessReview(id);
        if (review == null)
        {
            return NotFound(new { error = $"Access review with ID '{id}' not found." });
        }

        var log = _store.GetAccessReviewLog(id);
        
        var exportData = new
        {
            Review = review,
            AuditLog = log
        };
        
        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        
        var bytes = Encoding.UTF8.GetBytes(json);
        var fileName = $"access-review-{review.Id}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
        
        return File(bytes, "application/json", fileName);
    }

    private static string FormatCsvField(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "\"\"";
        
        // Escape double quotes by doubling them
        var escaped = value.Replace("\"", "\"\"");
        
        // Always wrap in quotes for consistency and to handle special characters
        return $"\"{escaped}\"";
    }
}
