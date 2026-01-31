namespace ARP.ESG_ReportStudio.API.Services;

/// <summary>
/// Service for exporting generated reports to PDF format.
/// </summary>
public interface IPdfExportService
{
    /// <summary>
    /// Generates a PDF document from a generated report.
    /// </summary>
    /// <param name="report">The generated report to export.</param>
    /// <param name="options">Optional export options for customization.</param>
    /// <returns>PDF file as byte array.</returns>
    byte[] GeneratePdf(Reporting.GeneratedReport report, PdfExportOptions? options = null);
    
    /// <summary>
    /// Generates a filename for the PDF export including company name, period, variant, and date.
    /// </summary>
    /// <param name="report">The generated report.</param>
    /// <param name="variantName">Optional variant name to include in filename.</param>
    /// <returns>Formatted filename for the PDF.</returns>
    string GenerateFilename(Reporting.GeneratedReport report, string? variantName = null);
}

/// <summary>
/// Options for PDF export customization.
/// </summary>
public sealed class PdfExportOptions
{
    /// <summary>
    /// Whether to include a table of contents.
    /// </summary>
    public bool IncludeTableOfContents { get; set; } = true;
    
    /// <summary>
    /// Whether to include page numbers.
    /// </summary>
    public bool IncludePageNumbers { get; set; } = true;
    
    /// <summary>
    /// Whether to include a title page.
    /// </summary>
    public bool IncludeTitlePage { get; set; } = true;
    
    /// <summary>
    /// Variant name to display on the title page and in metadata.
    /// </summary>
    public string? VariantName { get; set; }
    
    /// <summary>
    /// Whether to include evidence and attachments as an appendix.
    /// Default: false
    /// </summary>
    public bool IncludeAttachments { get; set; } = false;
    
    /// <summary>
    /// User ID requesting the export (used for permission checks on restricted attachments).
    /// </summary>
    public string? UserId { get; set; }
    
    /// <summary>
    /// Maximum total size of attachments to include (in MB).
    /// If exceeded, a warning is noted and only metadata is included.
    /// Default: 50 MB
    /// </summary>
    public int MaxAttachmentSizeMB { get; set; } = 50;
    
    /// <summary>
    /// Branding profile to apply to the export (optional).
    /// If not provided, default branding (if configured) may be applied.
    /// </summary>
    public Reporting.BrandingProfile? BrandingProfile { get; set; }
    
    /// <summary>
    /// Output language for labels and formatting (e.g., "en-US", "de-DE").
    /// When set, uses this language for section titles, labels, and locale-specific formatting.
    /// User-entered content remains in the original language.
    /// Defaults to "en-US" if not specified.
    /// </summary>
    public string? Language { get; set; }
    
    /// <summary>
    /// Export schema version to use. If not specified, uses the current active version.
    /// This enables backward compatibility by allowing exports with older schema versions.
    /// </summary>
    public ExportSchemaVersion? SchemaVersion { get; set; }
    
    /// <summary>
    /// User name requesting the export (used for export metadata).
    /// </summary>
    public string? UserName { get; set; }
}
