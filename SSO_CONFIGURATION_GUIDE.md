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
| `Oidc.MfaClaimType` | string | No | Claim type to check for MFA verification (default: "amr") |
| `Oidc.MfaClaimValue` | string | No | Expected value in MFA claim (default: "mfa") |

### Common Claim Types

Different identity providers use different claim types. Here are common mappings:

| Provider | User ID Claim | Email Claim | Name Claim |
|----------|---------------|-------------|------------|
| Azure AD | `preferred_username` or `oid` | `email` | `name` |
| Okta | `preferred_username` or `sub` | `email` | `name` |
| Auth0 | `sub` | `email` | `name` |
| Generic OIDC | `sub` | `email` | `name` |

## Multi-Factor Authentication (MFA) Enforcement

The ESG Report Studio supports MFA enforcement for privileged roles to enhance security for sensitive operations.

### How MFA Enforcement Works

1. **Privileged Roles**: Certain roles are marked as privileged and require MFA:
   - **Admin**: Full system access
   - **Management**: Strategic oversight and approval authority
   - **Compliance Officer**: Regulatory compliance and audit access

2. **MFA Verification**: When a user with a privileged role signs in:
   - The system checks the user's authentication token for MFA claims
   - If MFA is not satisfied, access is denied with an error message
   - If MFA is satisfied, access is granted and MFA status is recorded

3. **Audit Trail**: MFA status is recorded in session claims:
   - The `mfa_verified` claim is added to the user's session
   - This information is available via the `/api/auth/me` endpoint
   - Audit logs show which sessions used MFA

### Configuring MFA in Your Identity Provider

#### Azure AD (Entra ID)

1. **Enable Conditional Access Policy**:
   - Go to Azure Portal → Azure Active Directory → Security → Conditional Access
   - Create a new policy: "Require MFA for ESG Report Studio Admins"
   
2. **Configure the policy**:
   - **Users**: Select specific users or groups (e.g., "ESG Admins")
   - **Cloud apps**: Select "ESG Report Studio API" application
   - **Grant**: Require multi-factor authentication
   - **Enable policy**: On

3. **Verify MFA claims**: Azure AD includes MFA information in the `amr` (Authentication Methods Reference) claim when MFA is used.

#### Okta

1. **Configure Sign-On Policy**:
   - Go to Okta Admin Console → Applications → ESG Report Studio API
   - Go to "Sign On" tab → "Sign On Policy"
   
2. **Add MFA rule**:
   - Create a new rule: "Require MFA for privileged users"
   - **People**: Select groups containing privileged users
   - **Actions**: Prompt for factor
   - **Re-authentication**: Every time

3. **Verify MFA claims**: Okta includes MFA information in the `amr` claim.

#### Auth0

1. **Enable MFA**:
   - Go to Auth0 Dashboard → Security → Multi-factor Auth
   - Enable desired factors (SMS, authenticator app, etc.)
   
2. **Configure MFA policies**:
   - Use Auth0 Rules or Actions to enforce MFA for specific users/roles
   - Example Rule:
   ```javascript
   function (user, context, callback) {
     const privilegedRoles = ['Admin', 'Management', 'Compliance Officer'];
     const userRoles = user.app_metadata?.roles || [];
     const hasPrivilegedRole = userRoles.some(r => privilegedRoles.includes(r));
     
     if (hasPrivilegedRole && !context.authentication?.methods?.find(m => m.name === 'mfa')) {
       context.multifactor = {
         provider: 'any',
         allowRememberBrowser: false
       };
     }
     callback(null, user, context);
   }
   ```

3. **Verify MFA claims**: Auth0 includes MFA information in the `amr` claim.

### Application Configuration for MFA

By default, the application looks for MFA claims in the standard `amr` (Authentication Methods Reference) claim. You can customize this if your IdP uses different claim types:

```json
{
  "Authentication": {
    "Oidc": {
      "MfaClaimType": "amr",
      "MfaClaimValue": "mfa"
    }
  }
}
```

**Common MFA claim configurations**:

| Provider | Claim Type | Claim Value | Notes |
|----------|------------|-------------|-------|
| Azure AD | `amr` | `mfa` | Standard claim when MFA is used |
| Okta | `amr` | `mfa` or `otp` | May include multiple methods |
| Auth0 | `amr` | `mfa` | Standard claim |
| Custom | Varies | Varies | Configure based on your IdP |

### Testing MFA Enforcement

1. **Create a test user** with an admin role:
   ```bash
   # Assign admin role via API
   curl -X POST https://localhost:5001/api/users/{user-id}/roles \
        -H "Content-Type: application/json" \
        -d '{"roleIds": ["role-admin"]}'
   ```

2. **Attempt to sign in without MFA**:
   - Result: Authentication should fail with "Multi-Factor Authentication is required for this account"

3. **Attempt to sign in with MFA**:
   - Complete MFA challenge in your IdP
   - Result: Authentication should succeed

4. **Check MFA status**:
   ```bash
   curl -H "Authorization: Bearer {token}" \
        https://localhost:5001/api/auth/me
   ```
   
   Expected response:
   ```json
   {
     "id": "user-id",
     "name": "Admin User",
     "email": "admin@example.com",
     "isActive": true,
     "roleIds": ["role-admin"],
     "mfaVerified": true,
     "requiresMfa": true
   }
   ```

### Managing Privileged Roles

To mark custom roles as requiring MFA:

1. Create or update a role with `RequiresMfa` set to `true`:
   ```bash
   curl -X POST https://localhost:5001/api/roles \
        -H "Content-Type: application/json" \
        -d '{
          "name": "Security Officer",
          "description": "Manages security and access controls",
          "permissions": ["manage-users", "view-audit-logs"],
          "requiresMfa": true
        }'
   ```

2. Assign the role to users who should be required to use MFA.

### Security Considerations

1. **Enforce at IdP level**: MFA is best enforced at the identity provider level for maximum security
2. **Application-level checks**: The application verifies MFA claims but cannot force MFA - this must be configured in your IdP
3. **Audit logging**: All authentication attempts (with and without MFA) are logged for compliance
4. **Session claims**: MFA status is recorded in the session and available for audit queries
5. **Non-privileged users**: Users without privileged roles are not required to use MFA but may still use it

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

### "Multi-Factor Authentication is required for this account"

This error occurs when a user with a privileged role (Admin, Management, or Compliance Officer) signs in without completing MFA.

**Solutions**:
1. **Configure MFA in your IdP**: Ensure MFA is enabled and enforced for privileged users
2. **Complete MFA challenge**: User must complete the MFA prompt during sign-in
3. **Verify MFA claims**: Check that your IdP is including MFA information in the token:
   - Azure AD: Check for `amr` claim with value `mfa`
   - Okta: Check for `amr` claim with value `mfa` or `otp`
   - Auth0: Check for `amr` claim with value `mfa`
4. **Check configuration**: Verify `MfaClaimType` and `MfaClaimValue` match your IdP's claims
5. **Test with non-privileged role**: Verify the issue is specific to privileged roles

**To inspect token claims** (for debugging):
```bash
# Decode your JWT token at https://jwt.io or use:
echo "{your-token}" | cut -d'.' -f2 | base64 -d | jq
```

Look for the `amr` claim in the token payload.

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

**Testing MFA enforcement in development**:
- If your dev IdP doesn't support MFA, you can temporarily use a non-privileged role for testing
- For proper MFA testing, use a staging environment with MFA-enabled IdP

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
