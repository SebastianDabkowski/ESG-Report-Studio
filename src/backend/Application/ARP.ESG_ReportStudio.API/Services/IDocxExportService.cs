namespace ARP.ESG_ReportStudio.API.Services;

/// <summary>
/// Service for exporting generated reports to DOCX format.
/// </summary>
public interface IDocxExportService
{
    /// <summary>
    /// Generates a DOCX document from a generated report.
    /// </summary>
    /// <param name="report">The generated report to export.</param>
    /// <param name="options">Optional export options for customization.</param>
    /// <returns>DOCX file as byte array.</returns>
    byte[] GenerateDocx(Reporting.GeneratedReport report, DocxExportOptions? options = null);
    
    /// <summary>
    /// Generates a filename for the DOCX export including company name, period, variant, and date.
    /// </summary>
    /// <param name="report">The generated report.</param>
    /// <param name="variantName">Optional variant name to include in filename.</param>
    /// <returns>Formatted filename for the DOCX.</returns>
    string GenerateFilename(Reporting.GeneratedReport report, string? variantName = null);
}

/// <summary>
/// Options for DOCX export customization.
/// </summary>
public sealed class DocxExportOptions
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
}
