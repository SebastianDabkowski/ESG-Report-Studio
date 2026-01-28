using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;
using System.Text;
using System.Text.Json;

namespace ARP.ESG_ReportStudio.API.Controllers;

[ApiController]
[Route("api/audit-log")]
public sealed class AuditLogController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public AuditLogController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Get audit log entries with optional filtering by entity type, entity ID, user ID, action, and date range.
    /// </summary>
    /// <param name="entityType">Filter by entity type (e.g., "DataPoint")</param>
    /// <param name="entityId">Filter by specific entity ID</param>
    /// <param name="userId">Filter by user who made the change</param>
    /// <param name="action">Filter by action type (e.g., "update", "approve")</param>
    /// <param name="startDate">Filter by entries after this date (ISO 8601 format)</param>
    /// <param name="endDate">Filter by entries before this date (ISO 8601 format)</param>
    /// <returns>List of audit log entries in reverse chronological order</returns>
    [HttpGet]
    public ActionResult<IReadOnlyList<AuditLogEntry>> GetAuditLog(
        [FromQuery] string? entityType = null,
        [FromQuery] string? entityId = null,
        [FromQuery] string? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null)
    {
        return Ok(_store.GetAuditLog(entityType, entityId, userId, action, startDate, endDate));
    }

    /// <summary>
    /// Export audit log entries as CSV with optional filtering.
    /// </summary>
    [HttpGet("export/csv")]
    public IActionResult ExportAuditLogCsv(
        [FromQuery] string? entityType = null,
        [FromQuery] string? entityId = null,
        [FromQuery] string? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null)
    {
        var entries = _store.GetAuditLog(entityType, entityId, userId, action, startDate, endDate);
        
        var csv = new StringBuilder();
        // Add UTF-8 BOM for better Excel compatibility
        csv.Append('\ufeff');
        csv.AppendLine("\"Timestamp\",\"User ID\",\"User Name\",\"Action\",\"Entity Type\",\"Entity ID\",\"Change Note\",\"Field\",\"Old Value\",\"New Value\"");
        
        foreach (var entry in entries)
        {
            if (entry.Changes != null && entry.Changes.Count > 0)
            {
                foreach (var change in entry.Changes)
                {
                    csv.AppendLine($"{FormatCsvField(entry.Timestamp)},{FormatCsvField(entry.UserId)},{FormatCsvField(entry.UserName)},{FormatCsvField(entry.Action)},{FormatCsvField(entry.EntityType)},{FormatCsvField(entry.EntityId)},{FormatCsvField(entry.ChangeNote ?? "")},{FormatCsvField(change.Field)},{FormatCsvField(change.OldValue ?? "")},{FormatCsvField(change.NewValue ?? "")}");
                }
            }
            else
            {
                csv.AppendLine($"{FormatCsvField(entry.Timestamp)},{FormatCsvField(entry.UserId)},{FormatCsvField(entry.UserName)},{FormatCsvField(entry.Action)},{FormatCsvField(entry.EntityType)},{FormatCsvField(entry.EntityId)},{FormatCsvField(entry.ChangeNote ?? "")},\"\",\"\",\"\"");
            }
        }
        
        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        var fileName = $"audit-log-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        
        return File(bytes, "text/csv", fileName);
    }

    /// <summary>
    /// Export audit log entries as JSON with optional filtering.
    /// </summary>
    [HttpGet("export/json")]
    public IActionResult ExportAuditLogJson(
        [FromQuery] string? entityType = null,
        [FromQuery] string? entityId = null,
        [FromQuery] string? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null)
    {
        var entries = _store.GetAuditLog(entityType, entityId, userId, action, startDate, endDate);
        
        var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        
        var bytes = Encoding.UTF8.GetBytes(json);
        var fileName = $"audit-log-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
        
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
