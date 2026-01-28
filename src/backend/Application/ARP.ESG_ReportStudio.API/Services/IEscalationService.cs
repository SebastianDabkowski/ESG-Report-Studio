namespace ARP.ESG_ReportStudio.API.Services;

/// <summary>
/// Interface for escalating overdue items.
/// </summary>
public interface IEscalationService
{
    /// <summary>
    /// Processes escalations for all active reporting periods.
    /// </summary>
    Task ProcessEscalationsAsync(CancellationToken cancellationToken);
}
