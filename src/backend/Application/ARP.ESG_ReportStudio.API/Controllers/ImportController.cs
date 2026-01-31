using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using ARP.ESG_ReportStudio.API.Reporting;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;

namespace ARP.ESG_ReportStudio.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/import")]
public sealed class ImportController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public ImportController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Download CSV template for bulk data point import.
    /// </summary>
    [HttpGet("template")]
    [Produces("text/csv")]
    public ActionResult GetTemplate()
    {
        var csv = new StringBuilder();
        csv.AppendLine("SectionId,Type,Classification,Title,Content,Value,Unit,OwnerId,ContributorIds,Source,InformationType,Assumptions,CompletenessStatus");
        csv.AppendLine("# Example row (delete this line before importing):");
        csv.AppendLine("section-123,metric,fact,Total Energy Consumption,Our facility consumed 1250 MWh of electricity in 2024,1250,MWh,user-1,user-3;user-4,Internal metering system,fact,,incomplete");
        
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "data-points-template.csv");
    }

    /// <summary>
    /// Bulk import data points from CSV file.
    /// </summary>
    [HttpPost("data-points")]
    public async Task<ActionResult<ImportResult>> ImportDataPoints(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file uploaded." });
        }

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Only CSV files are supported." });
        }

        // Limit file size to 10 MB
        const long maxFileSize = 10 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            return BadRequest(new { error = $"File size exceeds maximum allowed size of {maxFileSize / 1024 / 1024} MB." });
        }

        var result = new ImportResult();

        try
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                MissingFieldFound = null,
                BadDataFound = null,
                Comment = '#'
            });

            var records = new List<DataPointCsvRow>();
            await foreach (var record in csv.GetRecordsAsync<DataPointCsvRow>())
            {
                records.Add(record);
            }

            int rowNumber = 2; // Start at 2 to account for header row
            foreach (var record in records)
            {
                try
                {
                    // Parse contributor IDs (semicolon-separated)
                    var contributorIds = string.IsNullOrWhiteSpace(record.ContributorIds)
                        ? new List<string>()
                        : record.ContributorIds.Split(';', StringSplitOptions.RemoveEmptyEntries)
                            .Select(id => id.Trim())
                            .ToList();

                    var createRequest = new CreateDataPointRequest
                    {
                        SectionId = record.SectionId ?? string.Empty,
                        Type = record.Type ?? "narrative",
                        Classification = record.Classification,
                        Title = record.Title ?? string.Empty,
                        Content = record.Content ?? string.Empty,
                        Value = record.Value,
                        Unit = record.Unit,
                        OwnerId = record.OwnerId ?? string.Empty,
                        ContributorIds = contributorIds,
                        Source = record.Source ?? string.Empty,
                        InformationType = record.InformationType ?? string.Empty,
                        Assumptions = record.Assumptions,
                        CompletenessStatus = record.CompletenessStatus ?? string.Empty
                    };

                    var (isValid, errorMessage, dataPoint) = _store.CreateDataPoint(createRequest);

                    if (isValid)
                    {
                        result.SuccessCount++;
                        result.SuccessfulRows.Add(new ImportRowResult
                        {
                            RowNumber = rowNumber,
                            DataPointId = dataPoint!.Id,
                            Title = dataPoint.Title
                        });
                    }
                    else
                    {
                        result.ErrorCount++;
                        result.FailedRows.Add(new ImportRowResult
                        {
                            RowNumber = rowNumber,
                            Title = record.Title ?? "(no title)",
                            ErrorMessage = errorMessage ?? "Unknown error"
                        });
                    }
                }
                catch (Exception ex)
                {
                    result.ErrorCount++;
                    result.FailedRows.Add(new ImportRowResult
                    {
                        RowNumber = rowNumber,
                        Title = record.Title ?? "(no title)",
                        ErrorMessage = $"Exception: {ex.Message}"
                    });
                }

                rowNumber++;
            }

            result.TotalRows = records.Count;
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Failed to parse CSV file: {ex.Message}" });
        }

        return Ok(result);
    }
}

/// <summary>
/// CSV row mapping for data point import.
/// </summary>
public sealed class DataPointCsvRow
{
    public string? SectionId { get; set; }
    public string? Type { get; set; }
    public string? Classification { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Value { get; set; }
    public string? Unit { get; set; }
    public string? OwnerId { get; set; }
    public string? ContributorIds { get; set; } // Semicolon-separated
    public string? Source { get; set; }
    public string? InformationType { get; set; }
    public string? Assumptions { get; set; }
    public string? CompletenessStatus { get; set; }
}

/// <summary>
/// Result of bulk import operation.
/// </summary>
public sealed class ImportResult
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public List<ImportRowResult> SuccessfulRows { get; set; } = new();
    public List<ImportRowResult> FailedRows { get; set; } = new();
}

/// <summary>
/// Result for a single row in the import.
/// </summary>
public sealed class ImportRowResult
{
    public int RowNumber { get; set; }
    public string? DataPointId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}
