namespace ARP.ESG_ReportStudio.API.Services;

/// <summary>
/// Background service to periodically clean up expired sessions.
/// </summary>
public sealed class SessionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionCleanupService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // Check every 5 minutes

    public SessionCleanupService(
        IServiceProvider serviceProvider,
        ILogger<SessionCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Session cleanup service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_checkInterval, stoppingToken);

                using var scope = _serviceProvider.CreateScope();
                var sessionManager = scope.ServiceProvider.GetRequiredService<ISessionManager>();

                var expiredCount = await sessionManager.CleanupExpiredSessionsAsync();

                if (expiredCount > 0)
                {
                    _logger.LogInformation("Cleaned up {Count} expired sessions", expiredCount);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping the service
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in session cleanup service");
            }
        }

        _logger.LogInformation("Session cleanup service stopped");
    }
}
