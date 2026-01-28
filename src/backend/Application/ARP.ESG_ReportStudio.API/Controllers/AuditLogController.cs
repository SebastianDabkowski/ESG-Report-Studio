using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

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
    /// Get audit log entries with optional filtering by entity type, entity ID, user ID, and date range.
    /// </summary>
    /// <param name="entityType">Filter by entity type (e.g., "DataPoint")</param>
    /// <param name="entityId">Filter by specific entity ID</param>
    /// <param name="userId">Filter by user who made the change</param>
    /// <param name="startDate">Filter by entries after this date (ISO 8601 format)</param>
    /// <param name="endDate">Filter by entries before this date (ISO 8601 format)</param>
    /// <returns>List of audit log entries in reverse chronological order</returns>
    [HttpGet]
    public ActionResult<IReadOnlyList<AuditLogEntry>> GetAuditLog(
        [FromQuery] string? entityType = null,
        [FromQuery] string? entityId = null,
        [FromQuery] string? userId = null,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null)
    {
        return Ok(_store.GetAuditLog(entityType, entityId, userId, startDate, endDate));
    }
}
