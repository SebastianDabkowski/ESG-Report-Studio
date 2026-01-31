using ARP.ESG_ReportStudio.API.Models;
using ARP.ESG_ReportStudio.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

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
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                
                if (context.Principal?.Identity?.IsAuthenticated == true)
                {
                    var claims = context.Principal.Claims;
                    var userId = context.Principal.FindFirst(oidcSettings.NameClaimType)?.Value;
                    
                    if (!string.IsNullOrEmpty(userId))
                    {
                        // Sync user profile from claims
                        await userProfileSync.SyncUserFromClaimsAsync(claims);
                        
                        // Check if user is active
                        var isActive = await userProfileSync.IsUserActiveAsync(userId);
                        if (!isActive)
                        {
                            logger.LogWarning("User {UserId} is disabled or not found in system", userId);
                            context.Fail("User is not active in the system");
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
