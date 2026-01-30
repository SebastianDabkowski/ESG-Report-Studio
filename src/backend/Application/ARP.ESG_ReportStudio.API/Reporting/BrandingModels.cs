namespace ARP.ESG_ReportStudio.API.Reporting;

/// <summary>
/// Represents a corporate branding profile for document exports.
/// Multiple profiles can exist for different subsidiaries or brands.
/// </summary>
public sealed class BrandingProfile
{
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the branding profile (e.g., "Parent Company", "Subsidiary A")
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional description of the branding profile
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Organization ID this branding profile is associated with
    /// </summary>
    public string? OrganizationId { get; set; }
    
    /// <summary>
    /// Subsidiary name if this profile is for a specific subsidiary
    /// </summary>
    public string? SubsidiaryName { get; set; }
    
    /// <summary>
    /// Base64-encoded logo image or URL to logo
    /// </summary>
    public string? LogoData { get; set; }
    
    /// <summary>
    /// Logo content type (e.g., "image/png", "image/jpeg")
    /// </summary>
    public string? LogoContentType { get; set; }
    
    /// <summary>
    /// Primary brand color in hex format (e.g., "#1E40AF")
    /// </summary>
    public string? PrimaryColor { get; set; }
    
    /// <summary>
    /// Secondary brand color in hex format
    /// </summary>
    public string? SecondaryColor { get; set; }
    
    /// <summary>
    /// Accent color in hex format
    /// </summary>
    public string? AccentColor { get; set; }
    
    /// <summary>
    /// Footer text to appear on each page of exported documents
    /// </summary>
    public string? FooterText { get; set; }
    
    /// <summary>
    /// Indicates if this is the default branding profile
    /// </summary>
    public bool IsDefault { get; set; }
    
    /// <summary>
    /// Indicates if this profile is active and can be used
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// User who created this branding profile
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// When the profile was created (ISO 8601 format)
    /// </summary>
    public string CreatedAt { get; set; } = string.Empty;
    
    /// <summary>
    /// User who last updated this profile
    /// </summary>
    public string? UpdatedBy { get; set; }
    
    /// <summary>
    /// When the profile was last updated (ISO 8601 format)
    /// </summary>
    public string? UpdatedAt { get; set; }
}

/// <summary>
/// Represents a versioned document template for exports.
/// Templates can be updated, and each version is tracked for auditability.
/// </summary>
public sealed class DocumentTemplate
{
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the template (e.g., "Standard ESG Report", "Executive Summary")
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of the template
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Template type (e.g., "pdf", "docx", "excel")
    /// </summary>
    public string TemplateType { get; set; } = "pdf";
    
    /// <summary>
    /// Current version number of the template
    /// </summary>
    public int Version { get; set; } = 1;
    
    /// <summary>
    /// Template configuration as JSON
    /// Contains layout, styling, and formatting options
    /// </summary>
    public string Configuration { get; set; } = "{}";
    
    /// <summary>
    /// Indicates if this is the default template for its type
    /// </summary>
    public bool IsDefault { get; set; }
    
    /// <summary>
    /// Indicates if this template is active and can be used
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// User who created this template
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// When the template was created (ISO 8601 format)
    /// </summary>
    public string CreatedAt { get; set; } = string.Empty;
    
    /// <summary>
    /// User who last updated this template
    /// </summary>
    public string? UpdatedBy { get; set; }
    
    /// <summary>
    /// When the template was last updated (ISO 8601 format)
    /// </summary>
    public string? UpdatedAt { get; set; }
}

/// <summary>
/// Records each time a template version is used for export.
/// Provides audit trail for template usage.
/// </summary>
public sealed class TemplateUsageRecord
{
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Template ID that was used
    /// </summary>
    public string TemplateId { get; set; } = string.Empty;
    
    /// <summary>
    /// Template version that was used
    /// </summary>
    public int TemplateVersion { get; set; }
    
    /// <summary>
    /// Reporting period ID for which the template was used
    /// </summary>
    public string PeriodId { get; set; } = string.Empty;
    
    /// <summary>
    /// Branding profile ID that was used (if any)
    /// </summary>
    public string? BrandingProfileId { get; set; }
    
    /// <summary>
    /// Export type (e.g., "pdf", "docx")
    /// </summary>
    public string ExportType { get; set; } = string.Empty;
    
    /// <summary>
    /// User who generated the export
    /// </summary>
    public string GeneratedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// When the export was generated (ISO 8601 format)
    /// </summary>
    public string GeneratedAt { get; set; } = string.Empty;
}

/// <summary>
/// Request to create a new branding profile
/// </summary>
public sealed class CreateBrandingProfileRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? OrganizationId { get; set; }
    public string? SubsidiaryName { get; set; }
    public string? LogoData { get; set; }
    public string? LogoContentType { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? AccentColor { get; set; }
    public string? FooterText { get; set; }
    public bool IsDefault { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Request to update an existing branding profile
/// </summary>
public sealed class UpdateBrandingProfileRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SubsidiaryName { get; set; }
    public string? LogoData { get; set; }
    public string? LogoContentType { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? AccentColor { get; set; }
    public string? FooterText { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Request to create a new document template
/// </summary>
public sealed class CreateDocumentTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TemplateType { get; set; } = "pdf";
    public string Configuration { get; set; } = "{}";
    public bool IsDefault { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Request to update an existing document template
/// Creates a new version for auditability
/// </summary>
public sealed class UpdateDocumentTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Configuration { get; set; } = "{}";
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}
