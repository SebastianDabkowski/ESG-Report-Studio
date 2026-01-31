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
    
    /// <summary>
    /// Session timeout configuration settings.
    /// </summary>
    public SessionTimeoutSettings SessionTimeout { get; set; } = new();
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
    /// The claim type to check for MFA verification.
    /// Common values: "amr" (Authentication Methods Reference) or "mfa_verified".
    /// </summary>
    public string MfaClaimType { get; set; } = "amr";
    
    /// <summary>
    /// The expected value in the MFA claim to indicate MFA was completed.
    /// For "amr" claim, common values are "mfa" or "otp".
    /// </summary>
    public string MfaClaimValue { get; set; } = "mfa";

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

/// <summary>
/// Session timeout configuration settings.
/// </summary>
public sealed class SessionTimeoutSettings
{
    /// <summary>
    /// Whether session timeout is enabled.
    /// Default: true for security.
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Idle timeout in minutes before a session is considered inactive.
    /// Default: 30 minutes.
    /// </summary>
    public int IdleTimeoutMinutes { get; set; } = 30;
    
    /// <summary>
    /// Absolute timeout in minutes regardless of activity.
    /// Default: 480 minutes (8 hours).
    /// </summary>
    public int AbsoluteTimeoutMinutes { get; set; } = 480;
    
    /// <summary>
    /// Warning time in minutes before session expires to notify the user.
    /// Default: 5 minutes.
    /// </summary>
    public int WarningBeforeTimeoutMinutes { get; set; } = 5;
    
    /// <summary>
    /// Whether to allow session refresh when warned about timeout.
    /// Default: true.
    /// </summary>
    public bool AllowSessionRefresh { get; set; } = true;
}
