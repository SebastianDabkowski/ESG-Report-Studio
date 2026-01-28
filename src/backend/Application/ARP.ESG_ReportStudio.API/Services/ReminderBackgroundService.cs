namespace ARP.ESG_ReportStudio.API.Services;

/// <summary>
/// Background service that periodically checks for incomplete data points and sends reminders.
/// </summary>
public sealed class ReminderBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReminderBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Check every hour

    public ReminderBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ReminderBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Reminder Background Service starting");

        // Wait a bit before the first check to allow the app to fully start
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing reminders");
            }

            // Wait for the next check
            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Reminder Background Service stopping");
    }

    private async Task ProcessRemindersAsync(CancellationToken cancellationToken)
    {
        // Create a scope to get scoped services
        using var scope = _serviceProvider.CreateScope();
        var reminderService = scope.ServiceProvider.GetRequiredService<ReminderService>();
        
        await reminderService.ProcessRemindersAsync(cancellationToken);
    }
}
