using System.Globalization;
using EPiServer.Framework.Localization;
using Microsoft.Extensions.Logging;
using Nergard.ResourceEditor.Features.Overrides.Services;

namespace Nergard.ResourceEditor.Features.Overrides.Providers;

/// <summary>
/// Localization provider that checks DDS for overrides before falling back to XML.
/// When inserted at position 0 in the providers list, this provider is checked first.
/// </summary>
public class OverrideLocalizationProvider : LocalizationProvider
{
    private readonly IOverrideService _overrideService;
    private readonly ILogger<OverrideLocalizationProvider>? _logger;

    public OverrideLocalizationProvider(IOverrideService overrideService, ILogger<OverrideLocalizationProvider>? logger = null)
    {
        _overrideService = overrideService;
        _logger = logger;
    }

    public override string? GetString(string originalKey, string[] normalizedKey, CultureInfo culture)
    {
        // Build the key from normalized segments (e.g., ["contenttypes", "standardpage", "name"])
        var key = "/" + string.Join("/", normalizedKey);

        // Try exact culture match first (e.g., "en-US")
        var overrideValue = TryGetOverride(key, culture);

        // If not found and this is a property key, try the icontentdata fallback
        // This allows storing property translations globally under icontentdata
        // while Optimizely may query with specific content type names
        if (overrideValue == null && IsPropertyKey(normalizedKey))
        {
            var icontentdataKey = BuildIContentDataKey(normalizedKey);
            overrideValue = TryGetOverride(icontentdataKey, culture);

            if (overrideValue != null)
            {
                _logger?.LogDebug("Override found via icontentdata fallback for key '{OriginalKey}' -> '{FallbackKey}'", key, icontentdataKey);
            }
        }

        if (overrideValue != null)
        {
            _logger?.LogDebug("Override found for key '{Key}' culture '{Culture}': {Value}", key, culture.Name, overrideValue);
        }

        // Return override if found, otherwise null to fall through to next provider
        return overrideValue;
    }

    private string? TryGetOverride(string key, CultureInfo culture)
    {
        // Try exact culture match first (e.g., "en-US")
        var overrideValue = _overrideService.GetOverride(key, culture.Name);

        // If no exact match, try the two-letter language code (e.g., "en")
        if (overrideValue == null && culture.Name.Contains('-'))
        {
            overrideValue = _overrideService.GetOverride(key, culture.TwoLetterISOLanguageName);
        }

        return overrideValue;
    }

    /// <summary>
    /// Checks if the key is a property localization key.
    /// Format: contenttypes/{typename}/properties/{propname}/{element}
    /// </summary>
    private static bool IsPropertyKey(string[] normalizedKey)
    {
        // Expected: ["contenttypes", "{typename}", "properties", "{propname}", "caption|help"]
        return normalizedKey.Length >= 5
               && normalizedKey[0].Equals("contenttypes", StringComparison.OrdinalIgnoreCase)
               && normalizedKey[2].Equals("properties", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Builds an icontentdata key from a content-type-specific property key.
    /// Example: /contenttypes/standardpage/properties/mainbody/caption
    ///       -> /contenttypes/icontentdata/properties/mainbody/caption
    /// </summary>
    private static string BuildIContentDataKey(string[] normalizedKey)
    {
        // Replace the content type name (index 1) with "icontentdata"
        var fallbackKey = new string[normalizedKey.Length];
        Array.Copy(normalizedKey, fallbackKey, normalizedKey.Length);
        fallbackKey[1] = "icontentdata";
        return "/" + string.Join("/", fallbackKey);
    }

    public override IEnumerable<ResourceItem> GetAllStrings(string originalKey, string[] normalizedKey, CultureInfo culture)
    {
        // We only provide single-key overrides, not collections
        // Return empty to let the default provider handle collection queries
        return Enumerable.Empty<ResourceItem>();
    }

    public override IEnumerable<CultureInfo> AvailableLanguages
    {
        get
        {
            // We don't add new languages, just override existing ones
            return Enumerable.Empty<CultureInfo>();
        }
    }
}
