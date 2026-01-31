namespace ARP.ESG_ReportStudio.API.Models;

/// <summary>
/// Configuration settings for authentication, including OIDC and local auth options.
/// </summary>
public sealed class AuthenticationSettings
{
    /// <summary>
    /// Whether local authentication is enabled as a fallback.
    /// </summary>
    public bool EnableLocalAuth { get; set; } = false;

    /// <summary>
    /// OIDC configuration settings.
    /// </summary>
    public OidcSettings? Oidc { get; set; }
}

/// <summary>
/// OpenID Connect configuration settings.
/// </summary>
public sealed class OidcSettings
{
    /// <summary>
    /// Whether OIDC authentication is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// The authority URL for the OIDC provider (e.g., Azure AD tenant).
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// The client ID registered with the OIDC provider.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// The client secret for the application.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// The scope to request from the OIDC provider.
    /// </summary>
    public string Scope { get; set; } = "openid profile email";

    /// <summary>
    /// The claim type to use for the user's unique identifier.
    /// </summary>
    public string NameClaimType { get; set; } = "preferred_username";

    /// <summary>
    /// The claim type to use for the user's email.
    /// </summary>
    public string EmailClaimType { get; set; } = "email";

    /// <summary>
    /// The claim type to use for the user's display name.
    /// </summary>
    public string DisplayNameClaimType { get; set; } = "name";

    /// <summary>
    /// Whether to validate the issuer signing key.
    /// </summary>
    public bool ValidateIssuerSigningKey { get; set; } = true;

    /// <summary>
    /// Whether to validate the issuer.
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// Whether to validate the audience.
    /// </summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>
    /// Whether to require HTTPS metadata.
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;
}
