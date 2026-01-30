using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;
using System.IO.Compression;
using System.Text.Json;
using System.Security.Cryptography;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// Controller for generating year-over-year annex exports for auditors.
/// Provides metric deltas, variance explanations, narrative diffs, and evidence references.
/// </summary>
[ApiController]
[Route("api/yoy-annex")]
public sealed class YearOverYearAnnexController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public YearOverYearAnnexController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Export a year-over-year annex as a downloadable package.
    /// Includes metric deltas, variance explanations, narrative diffs, and evidence references.
    /// Filters confidential items based on user access rights.
    /// </summary>
    /// <param name="request">Export configuration specifying periods and options</param>
    /// <returns>Downloadable package (ZIP or JSON based on content size)</returns>
    [HttpPost("export")]
    public IActionResult ExportYoYAnnex([FromBody] ExportYoYAnnexRequest request)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.CurrentPeriodId))
        {
            return BadRequest(new { error = "CurrentPeriodId is required." });
        }

        if (string.IsNullOrWhiteSpace(request.PriorPeriodId))
        {
            return BadRequest(new { error = "PriorPeriodId is required." });
        }

        if (string.IsNullOrWhiteSpace(request.ExportedBy))
        {
            return BadRequest(new { error = "ExportedBy is required." });
        }

        // Verify periods exist
        var currentPeriod = _store.GetPeriods().FirstOrDefault(p => p.Id == request.CurrentPeriodId);
        if (currentPeriod == null)
        {
            return NotFound(new { error = $"Current period with ID '{request.CurrentPeriodId}' not found." });
        }

        var priorPeriod = _store.GetPeriods().FirstOrDefault(p => p.Id == request.PriorPeriodId);
        if (priorPeriod == null)
        {
            return NotFound(new { error = $"Prior period with ID '{request.PriorPeriodId}' not found." });
        }

        // Build annex contents
        var contents = _store.BuildYoYAnnexContents(request);
        if (contents == null)
        {
            return BadRequest(new { error = "Failed to build YoY annex contents." });
        }

        // Create ZIP package in memory
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
                    Summary = contents.Summary,
                    CurrentPeriod = new
                    {
                        contents.CurrentPeriod.Id,
                        contents.CurrentPeriod.Name,
                        contents.CurrentPeriod.StartDate,
                        contents.CurrentPeriod.EndDate
                    },
                    PriorPeriod = new
                    {
                        contents.PriorPeriod.Id,
                        contents.PriorPeriod.Name,
                        contents.PriorPeriod.StartDate,
                        contents.PriorPeriod.EndDate
                    },
                    ExclusionNotes = contents.ExclusionNotes,
                    GeneratedAt = DateTime.UtcNow.ToString("O")
                };
                var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
                writer.Write(json);
            }

            // Add metric-comparisons.json
            var metricsEntry = archive.CreateEntry("metric-comparisons.json");
            using (var entryStream = metricsEntry.Open())
            using (var writer = new StreamWriter(entryStream))
            {
                var json = JsonSerializer.Serialize(contents.Sections, new JsonSerializerOptions { WriteIndented = true });
                writer.Write(json);
            }

            // Add variance-explanations.json
            if (request.IncludeVarianceExplanations && contents.VarianceExplanations.Any())
            {
                var varianceEntry = archive.CreateEntry("variance-explanations.json");
                using (var entryStream = varianceEntry.Open())
                using (var writer = new StreamWriter(entryStream))
                {
                    var json = JsonSerializer.Serialize(contents.VarianceExplanations, new JsonSerializerOptions { WriteIndented = true });
                    writer.Write(json);
                }
            }

            // Add evidence-references.json
            if (request.IncludeEvidenceReferences && contents.EvidenceReferences.Any())
            {
                var evidenceEntry = archive.CreateEntry("evidence-references.json");
                using (var entryStream = evidenceEntry.Open())
                using (var writer = new StreamWriter(entryStream))
                {
                    var json = JsonSerializer.Serialize(contents.EvidenceReferences, new JsonSerializerOptions { WriteIndented = true });
                    writer.Write(json);
                }
            }

            // Add narrative-diffs.json
            if (request.IncludeNarrativeDiffs && contents.NarrativeDiffs.Any())
            {
                var diffsEntry = archive.CreateEntry("narrative-diffs.json");
                using (var entryStream = diffsEntry.Open())
                using (var writer = new StreamWriter(entryStream))
                {
                    var json = JsonSerializer.Serialize(contents.NarrativeDiffs, new JsonSerializerOptions { WriteIndented = true });
                    writer.Write(json);
                }
            }

            // Add README.txt
            var readmeEntry = archive.CreateEntry("README.txt");
            using (var entryStream = readmeEntry.Open())
            using (var writer = new StreamWriter(entryStream))
            {
                writer.WriteLine("ESG Report Studio - Year-over-Year Annex for Auditors");
                writer.WriteLine("======================================================");
                writer.WriteLine();
                writer.WriteLine($"Export ID: {contents.Metadata.ExportId}");
                writer.WriteLine($"Exported At: {contents.Metadata.ExportedAt}");
                writer.WriteLine($"Exported By: {contents.Metadata.ExportedByName}");
                writer.WriteLine();
                writer.WriteLine($"Current Period: {contents.Summary.CurrentPeriodName}");
                writer.WriteLine($"Prior Period: {contents.Summary.PriorPeriodName}");
                writer.WriteLine();
                writer.WriteLine("Summary:");
                writer.WriteLine($"  - Sections: {contents.Summary.SectionCount}");
                writer.WriteLine($"  - Metric Comparisons: {contents.Summary.MetricRowCount}");
                writer.WriteLine($"  - Variance Explanations: {contents.Summary.VarianceExplanationCount}");
                writer.WriteLine($"  - Narrative Comparisons: {contents.Summary.NarrativeComparisonCount}");
                writer.WriteLine($"  - Evidence References: {contents.Summary.EvidenceReferenceCount}");
                
                if (contents.Summary.ConfidentialItemsExcluded > 0)
                {
                    writer.WriteLine();
                    writer.WriteLine("Security Notice:");
                    writer.WriteLine($"  - {contents.Summary.ConfidentialItemsExcluded} confidential items excluded based on access rights");
                }
                
                writer.WriteLine();
                writer.WriteLine("Contents:");
                writer.WriteLine("  - manifest.json: Export metadata and summary");
                writer.WriteLine("  - metric-comparisons.json: Section-level metric deltas (current vs. prior)");
                
                if (request.IncludeVarianceExplanations && contents.VarianceExplanations.Any())
                {
                    writer.WriteLine("  - variance-explanations.json: Explanations for significant changes");
                }
                
                if (request.IncludeEvidenceReferences && contents.EvidenceReferences.Any())
                {
                    writer.WriteLine("  - evidence-references.json: Evidence file references with checksums");
                }
                
                if (request.IncludeNarrativeDiffs && contents.NarrativeDiffs.Any())
                {
                    writer.WriteLine("  - narrative-diffs.json: Summary of narrative text changes");
                }
                
                writer.WriteLine();
                writer.WriteLine("Notes:");
                writer.WriteLine("  - All exports are deterministic for audit repeatability");
                writer.WriteLine("  - Evidence files themselves are not included; use references to retrieve separately");
                writer.WriteLine("  - This export is logged in the audit trail for compliance");
                
                if (contents.ExclusionNotes.Any())
                {
                    writer.WriteLine();
                    writer.WriteLine("Exclusions:");
                    foreach (var note in contents.ExclusionNotes)
                    {
                        writer.WriteLine($"  - {note}");
                    }
                }
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
        _store.RecordYoYAnnexExport(request, checksum, packageBytes.Length);

        // Return the file
        var fileName = $"yoy-annex-{priorPeriod.Name.Replace(" ", "-")}-to-{currentPeriod.Name.Replace(" ", "-")}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip";
        return File(packageBytes, "application/zip", fileName);
    }

    /// <summary>
    /// Get list of all YoY annex exports for a specific current period.
    /// </summary>
    /// <param name="currentPeriodId">Current (more recent) reporting period ID</param>
    /// <returns>List of export records</returns>
    [HttpGet("exports/{currentPeriodId}")]
    public ActionResult<IReadOnlyList<YoYAnnexExportRecord>> GetExportHistory(string currentPeriodId)
    {
        var exports = _store.GetYoYAnnexExports(currentPeriodId);
        return Ok(exports);
    }
}
