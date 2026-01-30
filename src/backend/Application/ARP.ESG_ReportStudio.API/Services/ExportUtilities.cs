namespace ARP.ESG_ReportStudio.API.Services;

/// <summary>
/// Utility methods for export services.
/// </summary>
public static class ExportUtilities
{
    /// <summary>
    /// Generates a filename for an export including company name, period, variant, and date.
    /// </summary>
    /// <param name="report">The generated report.</param>
    /// <param name="variantName">Optional variant name to include in filename.</param>
    /// <param name="extension">File extension (including the dot, e.g., ".pdf" or ".docx").</param>
    /// <returns>Formatted filename.</returns>
    public static string GenerateFilename(Reporting.GeneratedReport report, string? variantName, string extension)
    {
        var companyName = SanitizeFilename(report.Organization?.Name ?? "ESG-Report");
        var periodName = SanitizeFilename(report.Period.Name);
        var generatedDate = DateTime.TryParse(report.GeneratedAt, out var dt) 
            ? dt.ToString("yyyy-MM-dd") 
            : DateTime.UtcNow.ToString("yyyy-MM-dd");
        
        var parts = new List<string> { companyName, periodName };
        
        if (!string.IsNullOrWhiteSpace(variantName))
        {
            parts.Add(SanitizeFilename(variantName));
        }
        
        parts.Add(generatedDate);
        
        return $"{string.Join("_", parts)}{extension}";
    }
    
    /// <summary>
    /// Sanitizes a filename by replacing invalid characters with underscores.
    /// </summary>
    /// <param name="filename">The filename to sanitize.</param>
    /// <returns>Sanitized filename.</returns>
    public static string SanitizeFilename(string filename)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", filename.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Trim().Replace(" ", "_");
    }
    
    /// <summary>
    /// Formats an ISO 8601 datetime string for display.
    /// </summary>
    /// <param name="isoDateTime">The ISO 8601 datetime string.</param>
    /// <returns>Formatted datetime string.</returns>
    public static string FormatDateTime(string? isoDateTime)
    {
        if (string.IsNullOrWhiteSpace(isoDateTime))
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
        }
        
        if (DateTime.TryParse(isoDateTime, out var dt))
        {
            return dt.ToString("yyyy-MM-dd HH:mm:ss UTC");
        }
        
        return isoDateTime;
    }
}
