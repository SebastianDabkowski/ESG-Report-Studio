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

    /// <summary>
    /// Sends a notification to approvers when approval is requested.
    /// </summary>
    /// <param name="approvalRequest">The approval request</param>
    /// <param name="period">The reporting period</param>
    /// <param name="requester">The user requesting approval</param>
    /// <param name="approvers">List of users who need to approve</param>
    Task SendApprovalRequestNotificationAsync(
        ApprovalRequest approvalRequest,
        ReportingPeriod period,
        User requester,
        List<User> approvers);

    /// <summary>
    /// Sends a notification when an approval decision is made.
    /// </summary>
    /// <param name="approvalRecord">The approval record with the decision</param>
    /// <param name="approvalRequest">The parent approval request</param>
    /// <param name="period">The reporting period</param>
    /// <param name="approver">The user who made the decision</param>
    /// <param name="requester">The user who requested the approval</param>
    Task SendApprovalDecisionNotificationAsync(
        ApprovalRecord approvalRecord,
        ApprovalRequest approvalRequest,
        ReportingPeriod period,
        User approver,
        User requester);
}
