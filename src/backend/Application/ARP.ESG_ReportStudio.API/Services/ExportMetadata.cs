namespace ARP.ESG_ReportStudio.API.Services;

/// <summary>
/// Metadata about an export, including version and schema information.
/// This is included in all exports to enable downstream tooling compatibility.
/// </summary>
public sealed class ExportMetadata
{
    /// <summary>
    /// Unique identifier for this export instance.
    /// </summary>
    public string ExportId { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Export format (e.g., "pdf", "docx", "json").
    /// </summary>
    public string Format { get; set; } = string.Empty;
    
    /// <summary>
    /// Export schema version in semantic versioning format (MAJOR.MINOR.PATCH).
    /// </summary>
    public string SchemaVersion { get; set; } = string.Empty;
    
    /// <summary>
    /// Schema identifier for validation purposes.
    /// Format: "esg-report-studio/{format}/v{major}"
    /// Example: "esg-report-studio/json/v1"
    /// </summary>
    public string SchemaIdentifier { get; set; } = string.Empty;
    
    /// <summary>
    /// When this export was generated (ISO 8601 format).
    /// </summary>
    public string ExportedAt { get; set; } = DateTime.UtcNow.ToString("o");
    
    /// <summary>
    /// User ID who generated the export.
    /// </summary>
    public string? ExportedBy { get; set; }
    
    /// <summary>
    /// User name who generated the export.
    /// </summary>
    public string? ExportedByName { get; set; }
    
    /// <summary>
    /// Reporting period ID this export is for.
    /// </summary>
    public string? PeriodId { get; set; }
    
    /// <summary>
    /// Reporting period name this export is for.
    /// </summary>
    public string? PeriodName { get; set; }
    
    /// <summary>
    /// Generation ID this export is based on.
    /// </summary>
    public string? GenerationId { get; set; }
    
    /// <summary>
    /// Optional variant name for this export.
    /// </summary>
    public string? VariantName { get; set; }
    
    /// <summary>
    /// Additional metadata specific to the export format.
    /// </summary>
    public Dictionary<string, string>? AdditionalMetadata { get; set; }
    
    /// <summary>
    /// Creates export metadata from an export schema version.
    /// </summary>
    public static ExportMetadata FromSchemaVersion(
        ExportSchemaVersion schemaVersion,
        string? exportedBy = null,
        string? exportedByName = null)
    {
        return new ExportMetadata
        {
            Format = schemaVersion.Format,
            SchemaVersion = schemaVersion.VersionString,
            SchemaIdentifier = $"esg-report-studio/{schemaVersion.Format}/v{schemaVersion.Major}",
            ExportedBy = exportedBy,
            ExportedByName = exportedByName
        };
    }
    
    /// <summary>
    /// Formats metadata as a human-readable string for inclusion in documents.
    /// </summary>
    public string ToDisplayString()
    {
        var parts = new List<string>
        {
            $"Export ID: {ExportId}",
            $"Format: {Format}",
            $"Schema Version: {SchemaVersion}",
            $"Schema: {SchemaIdentifier}",
            $"Exported: {ExportedAt}"
        };
        
        if (!string.IsNullOrWhiteSpace(ExportedByName))
        {
            parts.Add($"Exported By: {ExportedByName}");
        }
        
        if (!string.IsNullOrWhiteSpace(PeriodName))
        {
            parts.Add($"Period: {PeriodName}");
        }
        
        if (!string.IsNullOrWhiteSpace(VariantName))
        {
            parts.Add($"Variant: {VariantName}");
        }
        
        return string.Join(" | ", parts);
    }
    
    /// <summary>
    /// Formats metadata as a multi-line string for document footers or headers.
    /// </summary>
    public IEnumerable<string> ToDocumentLines()
    {
        yield return $"Export ID: {ExportId}";
        yield return $"Schema: {SchemaIdentifier} (v{SchemaVersion})";
        yield return $"Exported: {ExportedAt}";
        
        if (!string.IsNullOrWhiteSpace(ExportedByName))
        {
            yield return $"Exported By: {ExportedByName}";
        }
    }
}
