namespace ARP.ESG_ReportStudio.API.Services;

/// <summary>
/// Interface for sending email notifications.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a reminder email to the specified recipient.
    /// </summary>
    /// <param name="recipientEmail">Email address of the recipient</param>
    /// <param name="recipientName">Name of the recipient</param>
    /// <param name="subject">Email subject</param>
    /// <param name="body">Email body</param>
    /// <returns>True if email was sent successfully, false otherwise</returns>
    Task<bool> SendEmailAsync(string recipientEmail, string recipientName, string subject, string body);
}
