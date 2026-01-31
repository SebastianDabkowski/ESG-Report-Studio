using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using ARP.ESG_ReportStudio.API.Services;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// Controller for localization and language support.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/localization")]
public sealed class LocalizationController : ControllerBase
{
    private readonly ILocalizationService _localizationService;

    public LocalizationController(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    /// <summary>
    /// Get list of supported languages.
    /// </summary>
    /// <returns>List of language codes</returns>
    [HttpGet("languages")]
    public ActionResult<IReadOnlyList<string>> GetSupportedLanguages()
    {
        var languages = _localizationService.GetSupportedLanguages();
        return Ok(languages);
    }

    /// <summary>
    /// Get all labels for a specific language.
    /// </summary>
    /// <param name="language">Language code (e.g., "en-US", "de-DE")</param>
    /// <returns>Dictionary of label keys and values</returns>
    [HttpGet("languages/{language}/labels")]
    public ActionResult<Dictionary<string, string>> GetLabels(string language)
    {
        if (!_localizationService.IsSupportedLanguage(language))
        {
            return NotFound(new { error = $"Language '{language}' is not supported." });
        }

        var labels = _localizationService.GetAllLabels(language);
        return Ok(labels);
    }
}
