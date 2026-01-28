using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public NotificationsController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Get all notifications for a user.
    /// </summary>
    /// <param name="userId">User ID to get notifications for</param>
    /// <param name="unreadOnly">If true, only return unread notifications</param>
    /// <returns>List of notifications</returns>
    [HttpGet]
    public ActionResult<IReadOnlyList<OwnerNotification>> GetNotifications(
        [FromQuery] string userId, 
        [FromQuery] bool unreadOnly = false)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest(new { error = "userId is required." });
        }

        var notifications = _store.GetNotifications(userId, unreadOnly);
        return Ok(notifications);
    }

    /// <summary>
    /// Mark a notification as read.
    /// </summary>
    /// <param name="id">Notification ID</param>
    /// <returns>Success status</returns>
    [HttpPut("{id}/read")]
    public ActionResult MarkAsRead(string id)
    {
        var success = _store.MarkNotificationAsRead(id);
        
        if (!success)
        {
            return NotFound(new { error = $"Notification with ID '{id}' not found." });
        }

        return Ok(new { message = "Notification marked as read." });
    }
}
