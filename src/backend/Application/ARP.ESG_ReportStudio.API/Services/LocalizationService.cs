using System.Globalization;
using System.Text.Json;

namespace ARP.ESG_ReportStudio.API.Services;

/// <summary>
/// Implementation of ILocalizationService using JSON-based label files.
/// Labels are loaded from Localization/Labels/{language}.json files.
/// </summary>
public sealed class LocalizationService : ILocalizationService
{
    private readonly Dictionary<string, Dictionary<string, string>> _labelCache = new();
    private readonly string _localizationPath;
    private const string DefaultLanguage = "en-US";
    
    public LocalizationService(IWebHostEnvironment environment)
    {
        _localizationPath = Path.Combine(environment.ContentRootPath, "Localization", "Labels");
        LoadLabels();
    }
    
    private void LoadLabels()
    {
        // Load all JSON files from the Localization/Labels directory
        if (!Directory.Exists(_localizationPath))
        {
            Directory.CreateDirectory(_localizationPath);
            return;
        }
        
        var jsonFiles = Directory.GetFiles(_localizationPath, "*.json");
        foreach (var file in jsonFiles)
        {
            var language = Path.GetFileNameWithoutExtension(file);
            var jsonContent = File.ReadAllText(file);
            var labels = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
            
            if (labels != null)
            {
                _labelCache[language] = labels;
            }
        }
    }
    
    public string GetLabel(string key, string? language = null)
    {
        var lang = language ?? DefaultLanguage;
        
        // Try requested language first
        if (_labelCache.TryGetValue(lang, out var labels) && labels.TryGetValue(key, out var label))
        {
            return label;
        }
        
        // Fallback to default language
        if (lang != DefaultLanguage && _labelCache.TryGetValue(DefaultLanguage, out var defaultLabels) 
            && defaultLabels.TryGetValue(key, out var defaultLabel))
        {
            return defaultLabel;
        }
        
        // Return key as fallback if label not found
        return key;
    }
    
    public Dictionary<string, string> GetAllLabels(string language)
    {
        if (_labelCache.TryGetValue(language, out var labels))
        {
            return new Dictionary<string, string>(labels);
        }
        
        // Return default language labels if requested language not found
        if (_labelCache.TryGetValue(DefaultLanguage, out var defaultLabels))
        {
            return new Dictionary<string, string>(defaultLabels);
        }
        
        return new Dictionary<string, string>();
    }
    
    public bool IsSupportedLanguage(string language)
    {
        return _labelCache.ContainsKey(language);
    }
    
    public IReadOnlyList<string> GetSupportedLanguages()
    {
        return _labelCache.Keys.ToList();
    }
    
    public string FormatDate(string date, string? language = null)
    {
        var lang = language ?? DefaultLanguage;
        
        if (DateTime.TryParse(date, out var dateTime))
        {
            try
            {
                var cultureInfo = CultureInfo.GetCultureInfo(lang);
                return dateTime.ToString("d", cultureInfo);
            }
            catch (CultureNotFoundException)
            {
                // Fallback to default language
                return dateTime.ToString("d", CultureInfo.GetCultureInfo(DefaultLanguage));
            }
        }
        
        return date;
    }
    
    public string FormatNumber(decimal value, string? language = null)
    {
        var lang = language ?? DefaultLanguage;
        
        try
        {
            var cultureInfo = CultureInfo.GetCultureInfo(lang);
            return value.ToString("N2", cultureInfo);
        }
        catch (CultureNotFoundException)
        {
            // Fallback to default language
            return value.ToString("N2", CultureInfo.GetCultureInfo(DefaultLanguage));
        }
    }
}
