# SSO Authentication Configuration Guide

This guide provides step-by-step instructions for configuring SSO authentication via OpenID Connect (OIDC) in the ESG Report Studio.

## Overview

The ESG Report Studio supports SSO authentication through OIDC, allowing users to sign in using their organization's identity provider (IdP) such as Azure AD (Entra ID), Okta, Auth0, or any OIDC-compliant provider.

## Prerequisites

- Administrator access to your identity provider
- Ability to register applications in your IdP
- Access to the application's `appsettings.json` configuration files

## Configuration Steps

### 1. Register Application in Identity Provider

#### For Azure AD (Microsoft Entra ID)

1. Navigate to Azure Portal → Azure Active Directory → App registrations
2. Click "New registration"
3. Configure the application:
   - **Name**: ESG Report Studio API
   - **Supported account types**: Choose based on your needs (typically "Accounts in this organizational directory only")
   - **Redirect URI**: Leave blank for API (not needed for JWT Bearer authentication)
4. Click "Register"
5. Note down:
   - **Application (client) ID** - You'll need this for `ClientId`
   - **Directory (tenant) ID** - Used in the `Authority` URL
6. Go to "Certificates & secrets" → "New client secret"
   - Add a description (e.g., "ESG Studio API Secret")
   - Choose expiration period
   - Click "Add"
   - **Copy the secret value immediately** - You'll need this for `ClientSecret`
7. Go to "API permissions"
   - Ensure these Microsoft Graph permissions are granted:
     - `User.Read` (typically added by default)
     - `openid`
     - `profile`
     - `email`
8. Go to "Token configuration" (optional but recommended)
   - Add optional claims for `email` and `name` to ensure user profile information is included

#### For Okta

1. Navigate to Okta Admin Console → Applications → Applications
2. Click "Create App Integration"
3. Select:
   - **Sign-in method**: OIDC - OpenID Connect
   - **Application type**: Web Application (or API Services if available)
4. Configure:
   - **App integration name**: ESG Report Studio API
   - **Grant type**: Authorization Code
   - **Sign-in redirect URIs**: Your API callback URL (if needed)
5. Click "Save"
6. Note down:
   - **Client ID**
   - **Client Secret**
   - **Okta domain** (e.g., `https://your-domain.okta.com`)

#### For Auth0

1. Navigate to Auth0 Dashboard → Applications → Applications
2. Click "Create Application"
3. Choose:
   - **Name**: ESG Report Studio API
   - **Application Type**: Machine to Machine Applications
4. Configure and note down:
   - **Domain** (e.g., `your-tenant.auth0.com`)
   - **Client ID**
   - **Client Secret**
5. Go to "APIs" and ensure you have an API configured
6. Configure permissions as needed

### 2. Configure Application Settings

Update your `appsettings.json` (for production) or `appsettings.Development.json` (for development/testing):

#### For Azure AD

```json
{
  "Authentication": {
    "EnableLocalAuth": false,
    "Oidc": {
      "Enabled": true,
      "Authority": "https://login.microsoftonline.com/{your-tenant-id}/v2.0",
      "ClientId": "{your-application-client-id}",
      "ClientSecret": "{your-client-secret}",
      "Scope": "openid profile email",
      "NameClaimType": "preferred_username",
      "EmailClaimType": "email",
      "DisplayNameClaimType": "name",
      "ValidateIssuerSigningKey": true,
      "ValidateIssuer": true,
      "ValidateAudience": true,
      "RequireHttpsMetadata": true
    }
  }
}
```

**Replace**:
- `{your-tenant-id}` with your Azure AD tenant ID
- `{your-application-client-id}` with the Application (client) ID
- `{your-client-secret}` with the client secret value

#### For Okta

```json
{
  "Authentication": {
    "EnableLocalAuth": false,
    "Oidc": {
      "Enabled": true,
      "Authority": "https://{your-okta-domain}/oauth2/default",
      "ClientId": "{your-client-id}",
      "ClientSecret": "{your-client-secret}",
      "Scope": "openid profile email",
      "NameClaimType": "preferred_username",
      "EmailClaimType": "email",
      "DisplayNameClaimType": "name",
      "ValidateIssuerSigningKey": true,
      "ValidateIssuer": true,
      "ValidateAudience": true,
      "RequireHttpsMetadata": true
    }
  }
}
```

**Replace**:
- `{your-okta-domain}` with your Okta domain (e.g., `dev-12345.okta.com`)
- `{your-client-id}` with your Okta client ID
- `{your-client-secret}` with your Okta client secret

#### For Auth0

```json
{
  "Authentication": {
    "EnableLocalAuth": false,
    "Oidc": {
      "Enabled": true,
      "Authority": "https://{your-auth0-domain}/",
      "ClientId": "{your-client-id}",
      "ClientSecret": "{your-client-secret}",
      "Scope": "openid profile email",
      "NameClaimType": "sub",
      "EmailClaimType": "email",
      "DisplayNameClaimType": "name",
      "ValidateIssuerSigningKey": true,
      "ValidateIssuer": true,
      "ValidateAudience": true,
      "RequireHttpsMetadata": true
    }
  }
}
```

**Replace**:
- `{your-auth0-domain}` with your Auth0 domain (e.g., `your-tenant.auth0.com`)
- `{your-client-id}` with your Auth0 client ID
- `{your-client-secret}` with your Auth0 client secret

### 3. Secure Configuration Values

**IMPORTANT**: Never commit secrets to source control!

#### Using User Secrets (Development)

```bash
cd src/backend/Application/ARP.ESG_ReportStudio.API
dotnet user-secrets init
dotnet user-secrets set "Authentication:Oidc:ClientSecret" "your-secret-here"
```

#### Using Environment Variables (Production)

Set environment variables:
- `Authentication__Oidc__ClientSecret`
- `Authentication__Oidc__Authority` (optional, if different per environment)
- `Authentication__Oidc__ClientId` (optional, if different per environment)

#### Using Azure Key Vault (Production - Recommended)

1. Create an Azure Key Vault
2. Add secrets:
   - `Authentication--Oidc--ClientSecret`
   - Other sensitive values as needed
3. Configure your application to use Key Vault:

```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

### 4. Testing the Configuration

#### Test Authentication Endpoint

1. Start the application:
   ```bash
   cd src/backend/Application/ARP.ESG_ReportStudio.API
   dotnet run
   ```

2. Check authentication config (should be accessible without auth):
   ```bash
   curl https://localhost:5001/api/auth/config
   ```

   Expected response:
   ```json
   {
     "oidcEnabled": true,
     "localAuthEnabled": false,
     "authority": "https://login.microsoftonline.com/{tenant-id}/v2.0",
     "clientId": "{your-client-id}"
   }
   ```

#### Test with Valid Token

1. Obtain a JWT token from your IdP (using Postman, your frontend app, or IdP's testing tools)

2. Make an authenticated request:
   ```bash
   curl -H "Authorization: Bearer {your-jwt-token}" \
        https://localhost:5001/api/auth/me
   ```

   Expected response:
   ```json
   {
     "id": "user-id",
     "name": "User Name",
     "email": "user@example.com",
     "isActive": true,
     "roleIds": []
   }
   ```

### 5. User Management

#### Active/Inactive Users

Users are automatically marked as active when they first sign in. To disable a user:

1. **In the IdP**: Disable/block the user account (recommended - prevents new token issuance)
2. **In the application**: Set the user's `IsActive` flag to `false` (prevents access even with valid token)

When a disabled user attempts to sign in:
- IdP-level: User cannot obtain a valid token
- App-level: Token validation fails with "User is not active in the system"

#### First-Time User Login

When a user signs in for the first time:
1. JWT token is validated by the OIDC provider
2. User profile is extracted from token claims
3. New user record is created with:
   - ID from `preferred_username` or `sub` claim
   - Email from `email` claim
   - Name from `name` claim
   - `IsActive = true`
   - Empty role list (assign roles via `/api/users/{id}/roles` endpoint)

## Configuration Reference

### Authentication Settings

| Setting | Type | Required | Description |
|---------|------|----------|-------------|
| `EnableLocalAuth` | boolean | No | Enable local authentication fallback (default: false) |
| `Oidc.Enabled` | boolean | Yes | Enable OIDC authentication |
| `Oidc.Authority` | string | Yes | OIDC provider authority URL |
| `Oidc.ClientId` | string | Yes | Application client ID from IdP |
| `Oidc.ClientSecret` | string | Yes | Application client secret from IdP |
| `Oidc.Scope` | string | No | OAuth scopes to request (default: "openid profile email") |
| `Oidc.NameClaimType` | string | No | Claim type for user identifier (default: "preferred_username") |
| `Oidc.EmailClaimType` | string | No | Claim type for email (default: "email") |
| `Oidc.DisplayNameClaimType` | string | No | Claim type for display name (default: "name") |
| `Oidc.ValidateIssuerSigningKey` | boolean | No | Validate token signature (default: true) |
| `Oidc.ValidateIssuer` | boolean | No | Validate token issuer (default: true) |
| `Oidc.ValidateAudience` | boolean | No | Validate token audience (default: true) |
| `Oidc.RequireHttpsMetadata` | boolean | No | Require HTTPS for metadata (default: true, set false for dev) |

### Common Claim Types

Different identity providers use different claim types. Here are common mappings:

| Provider | User ID Claim | Email Claim | Name Claim |
|----------|---------------|-------------|------------|
| Azure AD | `preferred_username` or `oid` | `email` | `name` |
| Okta | `preferred_username` or `sub` | `email` | `name` |
| Auth0 | `sub` | `email` | `name` |
| Generic OIDC | `sub` | `email` | `name` |

## Troubleshooting

### "Authentication failed" errors

1. Check application logs for detailed error messages
2. Verify `Authority` URL is correct and accessible
3. Verify `ClientId` and `ClientSecret` are correct
4. Ensure required claims are present in the token
5. Check token expiration

### "User is not active in the system"

1. Verify user exists in the system (check `/api/users` endpoint)
2. Verify user's `IsActive` flag is `true`
3. Check if user was disabled in the IdP

### Cannot obtain token from IdP

1. Verify application registration is complete
2. Ensure API permissions are granted
3. Check if admin consent is required (Azure AD)
4. Verify user has access to the application

### Development Environment Issues

For development (non-HTTPS scenarios):
```json
{
  "Authentication": {
    "Oidc": {
      "RequireHttpsMetadata": false
    }
  }
}
```

## Security Best Practices

1. **Never commit secrets**: Use User Secrets, environment variables, or Key Vault
2. **Use HTTPS in production**: Set `RequireHttpsMetadata: true`
3. **Rotate secrets regularly**: Update client secrets periodically
4. **Monitor authentication logs**: Review failed authentication attempts
5. **Enable MFA**: Configure multi-factor authentication in your IdP
6. **Limit token lifetime**: Configure appropriate token expiration in IdP
7. **Use least privilege**: Grant only necessary API permissions

## Support

For additional help:
- Review the [ADR-002: SSO Authentication via OIDC](docs/adr/ADR-002-sso-authentication-via-oidc.md)
- Check IdP documentation for your specific provider
- Review ASP.NET Core authentication documentation
