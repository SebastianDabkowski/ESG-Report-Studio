var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSingleton<ARP.ESG_ReportStudio.API.Services.TextDiffService>();
builder.Services.AddSingleton<ARP.ESG_ReportStudio.API.Reporting.InMemoryReportStore>();

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

app.UseAuthorization();

app.MapControllers();

app.Run();
