using System.Text.Json;
using System.Text.Json.Serialization;

namespace ARP.ESG_ReportStudio.API.Services;

/// <summary>
/// Service for exporting generated reports to JSON format with versioned schema.
/// </summary>
public interface IJsonExportService
{
    /// <summary>
    /// Generates a JSON document from a generated report.
    /// </summary>
    /// <param name="report">The generated report to export.</param>
    /// <param name="options">Optional export options for customization.</param>
    /// <returns>JSON file as byte array.</returns>
    byte[] GenerateJson(Reporting.GeneratedReport report, JsonExportOptions? options = null);
    
    /// <summary>
    /// Generates a filename for the JSON export including company name, period, variant, and date.
    /// </summary>
    /// <param name="report">The generated report.</param>
    /// <param name="variantName">Optional variant name to include in filename.</param>
    /// <returns>Formatted filename for the JSON.</returns>
    string GenerateFilename(Reporting.GeneratedReport report, string? variantName = null);
}

/// <summary>
/// Options for JSON export customization.
/// </summary>
public sealed class JsonExportOptions
{
    /// <summary>
    /// Variant name to display in metadata.
    /// </summary>
    public string? VariantName { get; set; }
    
    /// <summary>
    /// Whether to include evidence metadata in the export.
    /// Default: true
    /// </summary>
    public bool IncludeEvidence { get; set; } = true;
    
    /// <summary>
    /// Whether to include assumptions in the export.
    /// Default: true
    /// </summary>
    public bool IncludeAssumptions { get; set; } = true;
    
    /// <summary>
    /// Whether to include gaps in the export.
    /// Default: true
    /// </summary>
    public bool IncludeGaps { get; set; } = true;
    
    /// <summary>
    /// User ID requesting the export (used for export metadata).
    /// </summary>
    public string? UserId { get; set; }
    
    /// <summary>
    /// User name requesting the export (used for export metadata).
    /// </summary>
    public string? UserName { get; set; }
    
    /// <summary>
    /// Export schema version to use. If not specified, uses the current active version.
    /// This enables backward compatibility by allowing exports with older schema versions.
    /// </summary>
    public ExportSchemaVersion? SchemaVersion { get; set; }
    
    /// <summary>
    /// Whether to format the JSON output with indentation.
    /// Default: true (formatted for human readability)
    /// </summary>
    public bool FormatOutput { get; set; } = true;
}

/// <summary>
/// Root container for JSON export with versioned schema.
/// </summary>
public sealed class JsonExportContainer
{
    /// <summary>
    /// Export metadata including version and schema information.
    /// </summary>
    [JsonPropertyName("exportMetadata")]
    public ExportMetadata ExportMetadata { get; set; } = new();
    
    /// <summary>
    /// The generated report data.
    /// </summary>
    [JsonPropertyName("report")]
    public Reporting.GeneratedReport Report { get; set; } = new();
    
    /// <summary>
    /// Optional additional metadata for future extensibility.
    /// This allows new optional fields to be added without breaking existing consumers.
    /// </summary>
    [JsonPropertyName("extensions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Extensions { get; set; }
}
