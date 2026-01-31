namespace ARP.ESG_ReportStudio.API.Services;

/// <summary>
/// Represents a version of an export schema with semantic versioning.
/// Supports backward compatibility tracking and breaking change detection.
/// </summary>
public sealed class ExportSchemaVersion
{
    /// <summary>
    /// Major version number. Increment for breaking changes.
    /// </summary>
    public int Major { get; set; }
    
    /// <summary>
    /// Minor version number. Increment for backward-compatible additions.
    /// </summary>
    public int Minor { get; set; }
    
    /// <summary>
    /// Patch version number. Increment for backward-compatible bug fixes.
    /// </summary>
    public int Patch { get; set; }
    
    /// <summary>
    /// Format type this version applies to (e.g., "pdf", "docx", "json").
    /// </summary>
    public string Format { get; set; } = string.Empty;
    
    /// <summary>
    /// Human-readable description of this version.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// When this version was released.
    /// </summary>
    public string? ReleasedAt { get; set; }
    
    /// <summary>
    /// Whether this version is currently supported.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Whether this version is deprecated (still supported but not recommended).
    /// </summary>
    public bool IsDeprecated { get; set; } = false;
    
    /// <summary>
    /// Minimum version that this version is backward compatible with.
    /// </summary>
    public string? BackwardCompatibleWith { get; set; }
    
    /// <summary>
    /// Gets the version string in semantic versioning format (MAJOR.MINOR.PATCH).
    /// </summary>
    public string VersionString => $"{Major}.{Minor}.{Patch}";
    
    /// <summary>
    /// Creates a new export schema version.
    /// </summary>
    public ExportSchemaVersion(int major, int minor, int patch, string format)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        Format = format;
    }
    
    /// <summary>
    /// Parses a version string in MAJOR.MINOR.PATCH format.
    /// </summary>
    public static ExportSchemaVersion Parse(string versionString, string format)
    {
        var parts = versionString.Split('.');
        if (parts.Length != 3)
        {
            throw new ArgumentException($"Invalid version string: {versionString}. Expected format: MAJOR.MINOR.PATCH");
        }
        
        if (!int.TryParse(parts[0], out var major) ||
            !int.TryParse(parts[1], out var minor) ||
            !int.TryParse(parts[2], out var patch))
        {
            throw new ArgumentException($"Invalid version string: {versionString}. All parts must be integers.");
        }
        
        return new ExportSchemaVersion(major, minor, patch, format);
    }
    
    /// <summary>
    /// Determines if this version is a breaking change from another version.
    /// </summary>
    public bool IsBreakingChangeFrom(ExportSchemaVersion other)
    {
        return Major > other.Major;
    }
    
    /// <summary>
    /// Determines if this version is backward compatible with another version.
    /// </summary>
    public bool IsBackwardCompatibleWith(ExportSchemaVersion other)
    {
        // Same major version means backward compatible
        return Major == other.Major && (Minor > other.Minor || (Minor == other.Minor && Patch >= other.Patch));
    }
}

/// <summary>
/// Registry of export schema versions and version management utilities.
/// </summary>
public static class ExportSchemaRegistry
{
    /// <summary>
    /// Current version for JSON exports.
    /// </summary>
    public static readonly ExportSchemaVersion JsonV1 = new(1, 0, 0, "json")
    {
        Description = "Initial JSON export format with full report data",
        ReleasedAt = "2024-01-01",
        IsActive = true,
        BackwardCompatibleWith = "1.0.0"
    };
    
    /// <summary>
    /// Current version for PDF exports.
    /// </summary>
    public static readonly ExportSchemaVersion PdfV1 = new(1, 0, 0, "pdf")
    {
        Description = "Initial PDF export format with QuestPDF rendering",
        ReleasedAt = "2024-01-01",
        IsActive = true,
        BackwardCompatibleWith = "1.0.0"
    };
    
    /// <summary>
    /// Current version for DOCX exports.
    /// </summary>
    public static readonly ExportSchemaVersion DocxV1 = new(1, 0, 0, "docx")
    {
        Description = "Initial DOCX export format with OpenXML",
        ReleasedAt = "2024-01-01",
        IsActive = true,
        BackwardCompatibleWith = "1.0.0"
    };
    
    /// <summary>
    /// Gets the current active version for a given format.
    /// </summary>
    public static ExportSchemaVersion GetCurrentVersion(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "json" => JsonV1,
            "pdf" => PdfV1,
            "docx" => DocxV1,
            _ => throw new ArgumentException($"Unknown export format: {format}")
        };
    }
    
    /// <summary>
    /// Gets all supported versions for a given format.
    /// </summary>
    public static IEnumerable<ExportSchemaVersion> GetSupportedVersions(string format)
    {
        // Currently only one version per format, but this would return all supported versions
        return format.ToLowerInvariant() switch
        {
            "json" => new[] { JsonV1 },
            "pdf" => new[] { PdfV1 },
            "docx" => new[] { DocxV1 },
            _ => Array.Empty<ExportSchemaVersion>()
        };
    }
}
