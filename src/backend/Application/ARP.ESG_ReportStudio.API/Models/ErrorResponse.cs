namespace ARP.ESG_ReportStudio.API.Models;

/// <summary>
/// Standardized error response model with correlation ID tracking
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// HTTP status code
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Error title/type
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed error message
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// Correlation ID for request tracking
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Timestamp when the error occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional error details or validation errors
    /// </summary>
    public Dictionary<string, object>? Errors { get; set; }

    /// <summary>
    /// Request path where the error occurred
    /// </summary>
    public string? Path { get; set; }
}
