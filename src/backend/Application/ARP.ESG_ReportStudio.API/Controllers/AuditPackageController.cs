using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// Controller for generating audit packages for external auditors.
/// Exports comprehensive bundles including report data, audit trails, decisions, and evidence.
/// </summary>
[ApiController]
[Route("api/audit-package")]
public sealed class AuditPackageController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public AuditPackageController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Export an audit package as a ZIP file containing report data, audit trails, decisions, and evidence references.
    /// </summary>
    /// <param name="request">Export configuration specifying period and optional section filters</param>
    /// <returns>ZIP file with structured JSON and metadata</returns>
    [HttpPost("export")]
    public ActionResult<ExportAuditPackageResult> ExportAuditPackage([FromBody] ExportAuditPackageRequest request)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.PeriodId))
        {
            return BadRequest(new { error = "PeriodId is required." });
        }

        if (string.IsNullOrWhiteSpace(request.ExportedBy))
        {
            return BadRequest(new { error = "ExportedBy is required." });
        }

        // Verify period exists
        var period = _store.GetPeriods().FirstOrDefault(p => p.Id == request.PeriodId);
        if (period == null)
        {
            return NotFound(new { error = $"Period with ID '{request.PeriodId}' not found." });
        }

        // Generate the package
        var (isValid, errorMessage, result) = _store.GenerateAuditPackage(request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(result);
    }

    /// <summary>
    /// Download an audit package as a ZIP file.
    /// </summary>
    /// <param name="request">Export configuration</param>
    /// <returns>ZIP file download</returns>
    [HttpPost("export/download")]
    public IActionResult DownloadAuditPackage([FromBody] ExportAuditPackageRequest request)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.PeriodId))
        {
            return BadRequest(new { error = "PeriodId is required." });
        }

        if (string.IsNullOrWhiteSpace(request.ExportedBy))
        {
            return BadRequest(new { error = "ExportedBy is required." });
        }

        // Verify period exists
        var period = _store.GetPeriods().FirstOrDefault(p => p.Id == request.PeriodId);
        if (period == null)
        {
            return NotFound(new { error = $"Period with ID '{request.PeriodId}' not found." });
        }

        // Generate package contents
        var contents = _store.BuildAuditPackageContents(request);
        if (contents == null)
        {
            return BadRequest(new { error = "Failed to build audit package contents." });
        }

        // Create ZIP file in memory
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            // Add manifest.json
            var manifestEntry = archive.CreateEntry("manifest.json");
            using (var entryStream = manifestEntry.Open())
            using (var writer = new StreamWriter(entryStream))
            {
                var manifest = new
                {
                    contents.Metadata,
                    Period = new
                    {
                        contents.Period.Id,
                        contents.Period.Name,
                        contents.Period.StartDate,
                        contents.Period.EndDate,
                        contents.Period.ReportingMode,
                        contents.Period.ReportScope
                    },
                    Summary = new
                    {
                        SectionCount = contents.Sections.Count,
                        DataPointCount = contents.Sections.Sum(s => s.DataPoints.Count),
                        AuditLogEntryCount = contents.AuditTrail.Count,
                        DecisionCount = contents.Decisions.Count,
                        AssumptionCount = contents.Sections.Sum(s => s.Assumptions.Count),
                        GapCount = contents.Sections.Sum(s => s.Gaps.Count),
                        EvidenceFileCount = contents.EvidenceFiles.Count
                    },
                    GeneratedAt = DateTime.UtcNow.ToString("O")
                };
                var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
                writer.Write(json);
            }

            // Add sections data
            var sectionsEntry = archive.CreateEntry("sections.json");
            using (var entryStream = sectionsEntry.Open())
            using (var writer = new StreamWriter(entryStream))
            {
                var json = JsonSerializer.Serialize(contents.Sections, new JsonSerializerOptions { WriteIndented = true });
                writer.Write(json);
            }

            // Add audit trail
            var auditTrailEntry = archive.CreateEntry("audit-trail.json");
            using (var entryStream = auditTrailEntry.Open())
            using (var writer = new StreamWriter(entryStream))
            {
                var json = JsonSerializer.Serialize(contents.AuditTrail, new JsonSerializerOptions { WriteIndented = true });
                writer.Write(json);
            }

            // Add decisions
            var decisionsEntry = archive.CreateEntry("decisions.json");
            using (var entryStream = decisionsEntry.Open())
            using (var writer = new StreamWriter(entryStream))
            {
                var json = JsonSerializer.Serialize(contents.Decisions, new JsonSerializerOptions { WriteIndented = true });
                writer.Write(json);
            }

            // Add evidence references
            var evidenceEntry = archive.CreateEntry("evidence-references.json");
            using (var entryStream = evidenceEntry.Open())
            using (var writer = new StreamWriter(entryStream))
            {
                var json = JsonSerializer.Serialize(contents.EvidenceFiles, new JsonSerializerOptions { WriteIndented = true });
                writer.Write(json);
            }

            // Add README
            var readmeEntry = archive.CreateEntry("README.txt");
            using (var entryStream = readmeEntry.Open())
            using (var writer = new StreamWriter(entryStream))
            {
                writer.WriteLine("ESG Report Studio - Audit Package");
                writer.WriteLine("=====================================");
                writer.WriteLine();
                writer.WriteLine($"Export ID: {contents.Metadata.ExportId}");
                writer.WriteLine($"Exported At: {contents.Metadata.ExportedAt}");
                writer.WriteLine($"Exported By: {contents.Metadata.ExportedByName}");
                writer.WriteLine($"Period: {period.Name}");
                writer.WriteLine();
                writer.WriteLine("Contents:");
                writer.WriteLine("  - manifest.json: Export metadata and summary");
                writer.WriteLine("  - sections.json: Section data with data points, gaps, assumptions, and provenance");
                writer.WriteLine("  - audit-trail.json: Complete audit log for the period");
                writer.WriteLine("  - decisions.json: Decision log entries");
                writer.WriteLine("  - evidence-references.json: Evidence file references with checksums");
                writer.WriteLine();
                writer.WriteLine("Note: Evidence files themselves are not included in this package.");
                writer.WriteLine("Use the file references and checksums to retrieve and verify evidence separately.");
            }
        }

        // Calculate checksum
        var packageBytes = memoryStream.ToArray();
        string checksum;
        using (var sha256 = SHA256.Create())
        {
            var hashBytes = sha256.ComputeHash(packageBytes);
            checksum = Convert.ToBase64String(hashBytes);
        }

        // Record the export
        _store.RecordAuditPackageExport(request, checksum, packageBytes.Length);

        // Return the file
        var fileName = $"audit-package-{period.Name.Replace(" ", "-")}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip";
        return File(packageBytes, "application/zip", fileName);
    }

    /// <summary>
    /// Get list of all audit package exports for a period.
    /// </summary>
    /// <param name="periodId">Reporting period ID</param>
    /// <returns>List of export records</returns>
    [HttpGet("exports/{periodId}")]
    public ActionResult<IReadOnlyList<AuditPackageExportRecord>> GetExportHistory(string periodId)
    {
        var exports = _store.GetAuditPackageExports(periodId);
        return Ok(exports);
    }
}
