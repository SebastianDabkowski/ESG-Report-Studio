namespace ARP.ESG_ReportStudio.API.Services;

/// <summary>
/// Service for retrieving localized labels and formatting content according to locale settings.
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Get a localized label by key.
    /// </summary>
    /// <param name="key">Label key (e.g., "label.table-of-contents")</param>
    /// <param name="language">Language code (e.g., "en-US", "de-DE"). Defaults to English if not specified.</param>
    /// <returns>Localized label text</returns>
    string GetLabel(string key, string? language = null);
    
    /// <summary>
    /// Get all labels for a specific language.
    /// </summary>
    /// <param name="language">Language code (e.g., "en-US", "de-DE")</param>
    /// <returns>Dictionary of all label keys and values for the language</returns>
    Dictionary<string, string> GetAllLabels(string language);
    
    /// <summary>
    /// Check if a language is supported.
    /// </summary>
    /// <param name="language">Language code to check</param>
    /// <returns>True if the language is supported</returns>
    bool IsSupportedLanguage(string language);
    
    /// <summary>
    /// Get list of all supported languages.
    /// </summary>
    /// <returns>List of supported language codes</returns>
    IReadOnlyList<string> GetSupportedLanguages();
    
    /// <summary>
    /// Format a date according to locale settings.
    /// </summary>
    /// <param name="date">ISO 8601 date string or DateTime</param>
    /// <param name="language">Language code for formatting</param>
    /// <returns>Formatted date string</returns>
    string FormatDate(string date, string? language = null);
    
    /// <summary>
    /// Format a number according to locale settings.
    /// </summary>
    /// <param name="value">Numeric value to format</param>
    /// <param name="language">Language code for formatting</param>
    /// <returns>Formatted number string</returns>
    string FormatNumber(decimal value, string? language = null);
}
