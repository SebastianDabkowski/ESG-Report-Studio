namespace ARP.ESG_ReportStudio.API.Services;

/// <summary>
/// Mock email service that logs emails to console instead of actually sending them.
/// This is for MVP demonstration purposes.
/// </summary>
public sealed class MockEmailService : IEmailService
{
    private readonly ILogger<MockEmailService> _logger;

    public MockEmailService(ILogger<MockEmailService> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendEmailAsync(string recipientEmail, string recipientName, string subject, string body)
    {
        // In MVP, we log the email instead of sending it
        _logger.LogInformation(
            "Mock Email Sent:\n" +
            "  To: {RecipientName} <{RecipientEmail}>\n" +
            "  Subject: {Subject}\n" +
            "  Body:\n{Body}",
            recipientName, recipientEmail, subject, body);

        // Simulate successful sending
        return Task.FromResult(true);
    }
}
