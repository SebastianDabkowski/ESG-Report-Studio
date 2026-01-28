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
    /// Get audit log entries with optional filtering by entity type, entity ID, user ID, action, date range, section, and owner.
    /// </summary>
    /// <param name="entityType">Filter by entity type (e.g., "DataPoint", "Gap", "Assumption")</param>
    /// <param name="entityId">Filter by specific entity ID</param>
    /// <param name="userId">Filter by user who made the change</param>
    /// <param name="action">Filter by action type (e.g., "update", "approve")</param>
    /// <param name="startDate">Filter by entries after this date (ISO 8601 format)</param>
    /// <param name="endDate">Filter by entries before this date (ISO 8601 format)</param>
    /// <param name="sectionId">Filter by section ID (for entities that belong to a section)</param>
    /// <param name="ownerId">Filter by owner/creator ID (for entities that have an owner)</param>
    /// <returns>List of audit log entries in reverse chronological order</returns>
    [HttpGet]
    public ActionResult<IReadOnlyList<AuditLogEntry>> GetAuditLog(
        [FromQuery] string? entityType = null,
        [FromQuery] string? entityId = null,
        [FromQuery] string? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null,
        [FromQuery] string? sectionId = null,
        [FromQuery] string? ownerId = null)
    {
        return Ok(_store.GetAuditLog(entityType, entityId, userId, action, startDate, endDate, sectionId, ownerId));
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
        [FromQuery] string? endDate = null,
        [FromQuery] string? sectionId = null,
        [FromQuery] string? ownerId = null)
    {
        var entries = _store.GetAuditLog(entityType, entityId, userId, action, startDate, endDate, sectionId, ownerId);
        
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
        [FromQuery] string? endDate = null,
        [FromQuery] string? sectionId = null,
        [FromQuery] string? ownerId = null)
    {
        var entries = _store.GetAuditLog(entityType, entityId, userId, action, startDate, endDate, sectionId, ownerId);
        
        var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        
        var bytes = Encoding.UTF8.GetBytes(json);
        var fileName = $"audit-log-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
        
        return File(bytes, "application/json", fileName);
    }

    /// <summary>
    /// Get chronological timeline of changes for a specific entity.
    /// Returns audit log entries with before/after values for easy comparison.
    /// Includes enriched metadata for report fragments (section name, evidence, comments).
    /// </summary>
    /// <param name="entityType">Type of entity (e.g., "Gap", "Assumption", "DataPoint", "ReportSection")</param>
    /// <param name="entityId">ID of the specific entity</param>
    /// <returns>Timeline of changes in chronological order (oldest first) with before/after values and metadata</returns>
    [HttpGet("timeline/{entityType}/{entityId}")]
    public ActionResult<object> GetEntityTimeline(string entityType, string entityId)
    {
        var entries = _store.GetAuditLog(entityType: entityType, entityId: entityId);
        
        if (!entries.Any())
        {
            return NotFound(new { error = $"No audit history found for {entityType} with ID '{entityId}'." });
        }

        // Get current entity metadata for context
        var metadata = GetEntityMetadata(entityType, entityId);

        // Reverse to get chronological order (oldest first)
        var timeline = entries.Reverse().Select(entry => new
        {
            entry.Id,
            entry.Timestamp,
            entry.UserId,
            entry.UserName,
            entry.Action,
            entry.ChangeNote,
            Changes = entry.Changes.Select(c => new
            {
                c.Field,
                Before = c.OldValue,
                After = c.NewValue
            }).ToList()
        }).ToList();

        return Ok(new
        {
            EntityType = entityType,
            EntityId = entityId,
            TotalChanges = timeline.Count,
            Metadata = metadata,
            Timeline = timeline
        });
    }

    /// <summary>
    /// Compare two versions of an entity by their audit log entry IDs.
    /// Returns a side-by-side comparison showing what changed between the versions.
    /// </summary>
    /// <param name="entityType">Type of entity (e.g., "DataPoint", "Gap", "Assumption", "ReportSection")</param>
    /// <param name="entityId">ID of the specific entity</param>
    /// <param name="fromVersion">Audit log entry ID representing the earlier version</param>
    /// <param name="toVersion">Audit log entry ID representing the later version</param>
    /// <returns>Comparison showing field-by-field differences between the two versions</returns>
    [HttpGet("compare/{entityType}/{entityId}")]
    public ActionResult<object> CompareVersions(
        string entityType, 
        string entityId,
        [FromQuery] string fromVersion,
        [FromQuery] string toVersion)
    {
        var allEntries = _store.GetAuditLog(entityType: entityType, entityId: entityId);
        
        if (!allEntries.Any())
        {
            return NotFound(new { error = $"No audit history found for {entityType} with ID '{entityId}'." });
        }

        var fromEntry = allEntries.FirstOrDefault(e => e.Id == fromVersion);
        var toEntry = allEntries.FirstOrDefault(e => e.Id == toVersion);

        if (fromEntry == null)
        {
            return NotFound(new { error = $"Version '{fromVersion}' not found." });
        }

        if (toEntry == null)
        {
            return NotFound(new { error = $"Version '{toVersion}' not found." });
        }

        // Build a state map by replaying changes chronologically
        var chronologicalEntries = allEntries.Reverse().ToList();
        var fromIndex = chronologicalEntries.FindIndex(e => e.Id == fromVersion);
        var toIndex = chronologicalEntries.FindIndex(e => e.Id == toVersion);

        if (fromIndex > toIndex)
        {
            return BadRequest(new { error = "fromVersion must be earlier than toVersion." });
        }

        // Reconstruct state at fromVersion
        var fromState = new Dictionary<string, string?>();
        for (int i = 0; i <= fromIndex; i++)
        {
            foreach (var change in chronologicalEntries[i].Changes)
            {
                fromState[change.Field] = change.NewValue;
            }
        }

        // Reconstruct state at toVersion
        var toState = new Dictionary<string, string?>();
        for (int i = 0; i <= toIndex; i++)
        {
            foreach (var change in chronologicalEntries[i].Changes)
            {
                toState[change.Field] = change.NewValue;
            }
        }

        // Calculate differences
        var allFields = fromState.Keys.Union(toState.Keys).OrderBy(f => f).ToList();
        var differences = allFields.Select(field =>
        {
            var fromValue = fromState.TryGetValue(field, out var fv) ? fv : null;
            var toValue = toState.TryGetValue(field, out var tv) ? tv : null;
            
            var changeType = (fromValue == null && toValue != null) ? "added" :
                           (fromValue != null && toValue == null) ? "removed" :
                           (fromValue != toValue) ? "modified" : "unchanged";

            return new
            {
                Field = field,
                FromValue = fromValue,
                ToValue = toValue,
                ChangeType = changeType
            };
        }).Where(d => d.ChangeType != "unchanged").ToList();

        var metadata = GetEntityMetadata(entityType, entityId);

        return Ok(new
        {
            EntityType = entityType,
            EntityId = entityId,
            FromVersion = new
            {
                Id = fromEntry.Id,
                Timestamp = fromEntry.Timestamp,
                UserId = fromEntry.UserId,
                UserName = fromEntry.UserName,
                Action = fromEntry.Action,
                ChangeNote = fromEntry.ChangeNote
            },
            ToVersion = new
            {
                Id = toEntry.Id,
                Timestamp = toEntry.Timestamp,
                UserId = toEntry.UserId,
                UserName = toEntry.UserName,
                Action = toEntry.Action,
                ChangeNote = toEntry.ChangeNote
            },
            Metadata = metadata,
            Differences = differences
        });
    }

    /// <summary>
    /// Get metadata for an entity including section name, evidence links, and related data.
    /// </summary>
    private object? GetEntityMetadata(string entityType, string entityId)
    {
        return entityType switch
        {
            "DataPoint" => GetDataPointMetadata(entityId),
            "ReportSection" => GetSectionMetadata(entityId),
            "Gap" => GetGapMetadata(entityId),
            "Assumption" => GetAssumptionMetadata(entityId),
            _ => null
        };
    }

    private object? GetDataPointMetadata(string dataPointId)
    {
        var dataPoint = _store.GetDataPoint(dataPointId);
        if (dataPoint == null) return null;

        var section = _store.GetSection(dataPoint.SectionId);
        var evidence = _store.GetEvidenceForDataPoint(dataPointId);
        var notes = _store.GetNotesForDataPoint(dataPointId);

        return new
        {
            Title = dataPoint.Title,
            SectionId = dataPoint.SectionId,
            SectionName = section?.Title,
            Type = dataPoint.Type,
            EvidenceCount = evidence.Count,
            Evidence = evidence.Select(e => new { e.Id, e.FileName, e.UploadedAt }).ToList(),
            NotesCount = notes.Count,
            Notes = notes.Select(n => new { n.Id, n.Content, n.CreatedAt, n.CreatedBy }).ToList()
        };
    }

    private object? GetSectionMetadata(string sectionId)
    {
        var section = _store.GetSection(sectionId);
        if (section == null) return null;

        var dataPoints = _store.GetDataPointsForSection(sectionId);

        return new
        {
            Title = section.Title,
            Category = section.Category,
            Status = section.Status,
            OwnerId = section.OwnerId,
            DataPointCount = dataPoints.Count
        };
    }

    private object? GetGapMetadata(string gapId)
    {
        var gap = _store.GetGap(gapId);
        if (gap == null) return null;

        var section = _store.GetSection(gap.SectionId);

        return new
        {
            Title = gap.Title,
            SectionId = gap.SectionId,
            SectionName = section?.Title,
            Resolved = gap.Resolved,
            Impact = gap.Impact
        };
    }

    private object? GetAssumptionMetadata(string assumptionId)
    {
        var assumption = _store.GetAssumption(assumptionId);
        if (assumption == null) return null;

        return new
        {
            Title = assumption.Title,
            Status = assumption.Status,
            ValidityStartDate = assumption.ValidityStartDate,
            ValidityEndDate = assumption.ValidityEndDate,
            LinkedDataPointsCount = assumption.LinkedDataPointIds?.Count ?? 0
        };
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
