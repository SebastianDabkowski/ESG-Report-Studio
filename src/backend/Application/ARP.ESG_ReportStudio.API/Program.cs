using ARP.ESG_ReportStudio.API.Models;
using ARP.ESG_ReportStudio.API.Services;
using ARP.ESG_ReportStudio.API.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using SD.ProjectName.Modules.Integrations.Application;
using SD.ProjectName.Modules.Integrations.Domain.Interfaces;
using SD.ProjectName.Modules.Integrations.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configure authentication settings
var authSettings = builder.Configuration.GetSection("Authentication").Get<AuthenticationSettings>();

// Validate authentication configuration if OIDC is intended to be used
if (authSettings == null)
{
    // If no authentication section exists, create default with disabled OIDC
    authSettings = new AuthenticationSettings
    {
        EnableLocalAuth = true,
        Oidc = new OidcSettings { Enabled = false }
    };
}

builder.Services.Configure<AuthenticationSettings>(builder.Configuration.GetSection("Authentication"));

// Add authentication services
if (authSettings.Oidc?.Enabled == true)
{
    if (string.IsNullOrEmpty(authSettings.Oidc.Authority) || string.IsNullOrEmpty(authSettings.Oidc.ClientId))
    {
        throw new InvalidOperationException(
            "OIDC is enabled but required configuration is missing. " +
            "Please configure Authentication:Oidc:Authority and Authentication:Oidc:ClientId in appsettings.json");
    }

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var oidcSettings = authSettings.Oidc;
        options.Authority = oidcSettings.Authority;
        options.Audience = oidcSettings.ClientId;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = oidcSettings.ValidateIssuerSigningKey,
            ValidateIssuer = oidcSettings.ValidateIssuer,
            ValidateAudience = oidcSettings.ValidateAudience,
            ValidAudience = oidcSettings.ClientId,
            ValidIssuer = oidcSettings.Authority,
            NameClaimType = oidcSettings.NameClaimType,
            RoleClaimType = "roles"
        };
        options.RequireHttpsMetadata = oidcSettings.RequireHttpsMetadata;
        
        // Event handlers for token validation
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userProfileSync = context.HttpContext.RequestServices.GetRequiredService<IUserProfileSyncService>();
                var sessionManager = context.HttpContext.RequestServices.GetRequiredService<ISessionManager>();
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                
                if (context.Principal?.Identity?.IsAuthenticated == true)
                {
                    var claims = context.Principal.Claims;
                    var userId = context.Principal.FindFirst(oidcSettings.NameClaimType)?.Value;
                    
                    if (!string.IsNullOrEmpty(userId))
                    {
                        // Sync user profile from claims
                        var user = await userProfileSync.SyncUserFromClaimsAsync(claims);
                        
                        // Check if user is active
                        var isActive = await userProfileSync.IsUserActiveAsync(userId);
                        if (!isActive)
                        {
                            logger.LogWarning("User {UserId} is disabled or not found in system", userId);
                            context.Fail("User is not active in the system");
                            return;
                        }
                        
                        // Check if user has privileged roles requiring MFA
                        var requiresMfa = await userProfileSync.UserRequiresMfaAsync(userId);
                        var mfaVerified = false;
                        
                        if (requiresMfa)
                        {
                            // Verify MFA claims are present
                            var hasMfaClaims = userProfileSync.HasValidMfaClaims(claims);
                            if (!hasMfaClaims)
                            {
                                logger.LogWarning("User {UserId} has privileged role requiring MFA but MFA claims not found in token", userId);
                                context.Fail("Multi-Factor Authentication is required for this account");
                                return;
                            }
                            
                            mfaVerified = true;
                            
                            // Add MFA claim to the principal for audit trail
                            var identity = context.Principal.Identity as System.Security.Claims.ClaimsIdentity;
                            if (identity != null)
                            {
                                identity.AddClaim(new System.Security.Claims.Claim("mfa_verified", "true"));
                                logger.LogInformation("User {UserId} authenticated with MFA", userId);
                            }
                        }
                        
                        // Create a session for this authenticated user
                        var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString();
                        var userAgent = context.HttpContext.Request.Headers["User-Agent"].ToString();
                        var session = await sessionManager.CreateSessionAsync(userId, user.Name, ipAddress, userAgent, mfaVerified);
                        
                        // Add session ID to claims for tracking
                        var sessionIdentity = context.Principal.Identity as System.Security.Claims.ClaimsIdentity;
                        if (sessionIdentity != null)
                        {
                            sessionIdentity.AddClaim(new System.Security.Claims.Claim("session_id", session.SessionId));
                        }
                    }
                }
            },
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(context.Exception, "Authentication failed");
                return Task.CompletedTask;
            }
        };
    });
}
else
{
    // When OIDC is not enabled, add minimal authentication for development
    builder.Services.AddAuthentication();
}

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddSingleton<ARP.ESG_ReportStudio.API.Services.TextDiffService>();
builder.Services.AddSingleton<ARP.ESG_ReportStudio.API.Reporting.InMemoryReportStore>();

// Add user profile sync service
builder.Services.AddScoped<IUserProfileSyncService, UserProfileSyncService>();

// Add session management services
builder.Services.AddSingleton<ISessionManager, SessionManager>();
builder.Services.AddHostedService<SessionCleanupService>();

// Add notification services
builder.Services.AddScoped<ARP.ESG_ReportStudio.API.Services.INotificationService, ARP.ESG_ReportStudio.API.Services.OwnerAssignmentNotificationService>();

// Add reminder services
builder.Services.AddSingleton<ARP.ESG_ReportStudio.API.Services.IEmailService, ARP.ESG_ReportStudio.API.Services.MockEmailService>();
builder.Services.AddScoped<ARP.ESG_ReportStudio.API.Services.ReminderService>();
builder.Services.AddScoped<ARP.ESG_ReportStudio.API.Services.IEscalationService, ARP.ESG_ReportStudio.API.Services.EscalationService>();
builder.Services.AddHostedService<ARP.ESG_ReportStudio.API.Services.ReminderBackgroundService>();

// Add PDF export service
builder.Services.AddScoped<ARP.ESG_ReportStudio.API.Services.IPdfExportService, ARP.ESG_ReportStudio.API.Services.PdfExportService>();

// Add DOCX export service
builder.Services.AddScoped<ARP.ESG_ReportStudio.API.Services.IDocxExportService, ARP.ESG_ReportStudio.API.Services.DocxExportService>();

// Add localization service
builder.Services.AddSingleton<ARP.ESG_ReportStudio.API.Services.ILocalizationService, ARP.ESG_ReportStudio.API.Services.LocalizationService>();

// Add Integrations module services
builder.Services.AddDbContext<IntegrationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection") ?? "Server=(localdb)\\mssqllocaldb;Database=ESGReportStudio;Trusted_Connection=True;MultipleActiveResultSets=true",
        sqlOptions => sqlOptions.MigrationsAssembly("SD.ProjectName.Modules.Integrations")));
builder.Services.AddScoped<IConnectorRepository, ConnectorRepository>();
builder.Services.AddScoped<IIntegrationLogRepository, IntegrationLogRepository>();
builder.Services.AddScoped<IHREntityRepository, HREntityRepository>();
builder.Services.AddScoped<IHRSyncRecordRepository, HRSyncRecordRepository>();
builder.Services.AddScoped<IFinanceEntityRepository, FinanceEntityRepository>();
builder.Services.AddScoped<IFinanceSyncRecordRepository, FinanceSyncRecordRepository>();
builder.Services.AddScoped<ConnectorService>();
builder.Services.AddScoped<IntegrationExecutionService>();
builder.Services.AddScoped<HRSyncService>();
builder.Services.AddScoped<FinanceSyncService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("DevCors");

// Add correlation ID middleware early in the pipeline
app.UseCorrelationId();

app.UseAuthentication();
app.UseMiddleware<ARP.ESG_ReportStudio.API.Middleware.SessionActivityMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();
