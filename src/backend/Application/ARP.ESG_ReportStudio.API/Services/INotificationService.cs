using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Services;

/// <summary>
/// Interface for sending notifications about ownership changes.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends a notification when a user is assigned as owner of a section.
    /// </summary>
    /// <param name="section">The section being assigned</param>
    /// <param name="newOwner">The new owner</param>
    /// <param name="changedBy">The user who made the change</param>
    /// <param name="changeNote">Optional note about the change</param>
    Task SendSectionAssignedNotificationAsync(
        ReportSection section, 
        User newOwner, 
        User changedBy, 
        string? changeNote = null);

    /// <summary>
    /// Sends a notification when a user is removed as owner of a section.
    /// </summary>
    /// <param name="section">The section being unassigned</param>
    /// <param name="previousOwner">The previous owner</param>
    /// <param name="changedBy">The user who made the change</param>
    /// <param name="changeNote">Optional note about the change</param>
    Task SendSectionRemovedNotificationAsync(
        ReportSection section, 
        User previousOwner, 
        User changedBy, 
        string? changeNote = null);

    /// <summary>
    /// Sends a notification when a user is assigned as owner of a data point.
    /// </summary>
    /// <param name="dataPoint">The data point being assigned</param>
    /// <param name="newOwner">The new owner</param>
    /// <param name="changedBy">The user who made the change</param>
    Task SendDataPointAssignedNotificationAsync(
        DataPoint dataPoint, 
        User newOwner, 
        User changedBy);

    /// <summary>
    /// Sends a notification when a user is removed as owner of a data point.
    /// </summary>
    /// <param name="dataPoint">The data point being unassigned</param>
    /// <param name="previousOwner">The previous owner</param>
    /// <param name="changedBy">The user who made the change</param>
    Task SendDataPointRemovedNotificationAsync(
        DataPoint dataPoint, 
        User previousOwner, 
        User changedBy);
}
