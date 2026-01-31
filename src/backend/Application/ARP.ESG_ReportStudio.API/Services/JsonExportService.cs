using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ARP.ESG_ReportStudio.API.Services;

/// <summary>
/// Service for exporting generated reports to JSON format with versioned schema.
/// Provides machine-readable export format for downstream tooling integration.
/// </summary>
public sealed class JsonExportService : IJsonExportService
{
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    
    private static readonly JsonSerializerOptions CompactJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    
    public byte[] GenerateJson(Reporting.GeneratedReport report, JsonExportOptions? options = null)
    {
        options ??= new JsonExportOptions();
        
        // Get schema version (use provided or default to current)
        var schemaVersion = options.SchemaVersion ?? ExportSchemaRegistry.GetCurrentVersion("json");
        
        // Create export metadata
        var exportMetadata = ExportMetadata.FromSchemaVersion(
            schemaVersion,
            options.UserId,
            options.UserName
        );
        exportMetadata.PeriodId = report.Period.Id;
        exportMetadata.PeriodName = report.Period.Name;
        exportMetadata.GenerationId = report.Id;
        exportMetadata.VariantName = options.VariantName;
        
        // Filter report data based on options
        var filteredReport = FilterReportData(report, options);
        
        // Create export container
        var container = new JsonExportContainer
        {
            ExportMetadata = exportMetadata,
            Report = filteredReport
        };
        
        // Serialize to JSON
        var jsonOptions = options.FormatOutput ? DefaultJsonOptions : CompactJsonOptions;
        var json = JsonSerializer.Serialize(container, jsonOptions);
        
        return Encoding.UTF8.GetBytes(json);
    }
    
    public string GenerateFilename(Reporting.GeneratedReport report, string? variantName = null)
    {
        return ExportUtilities.GenerateFilename(report, variantName, ".json");
    }
    
    /// <summary>
    /// Filters report data based on export options.
    /// Creates a copy of the report with only the requested data.
    /// </summary>
    private Reporting.GeneratedReport FilterReportData(Reporting.GeneratedReport report, JsonExportOptions options)
    {
        // For v1.0.0, we include all data by default but filter based on options
        // Future versions might have different filtering rules
        
        var filteredReport = new Reporting.GeneratedReport
        {
            Id = report.Id,
            Period = report.Period,
            Organization = report.Organization,
            GeneratedAt = report.GeneratedAt,
            GeneratedBy = report.GeneratedBy,
            GeneratedByName = report.GeneratedByName,
            GenerationNote = report.GenerationNote,
            Checksum = report.Checksum,
            Sections = new List<Reporting.GeneratedReportSection>()
        };
        
        foreach (var section in report.Sections)
        {
            var filteredSection = new Reporting.GeneratedReportSection
            {
                Section = section.Section,
                Owner = section.Owner,
                DataPoints = section.DataPoints,
                Assumptions = options.IncludeAssumptions ? section.Assumptions : new List<Reporting.AssumptionRecord>(),
                Gaps = options.IncludeGaps ? section.Gaps : new List<Reporting.GapRecord>(),
                Evidence = options.IncludeEvidence ? section.Evidence : new List<Reporting.EvidenceMetadata>()
            };
            
            filteredReport.Sections.Add(filteredSection);
        }
        
        return filteredReport;
    }
}
