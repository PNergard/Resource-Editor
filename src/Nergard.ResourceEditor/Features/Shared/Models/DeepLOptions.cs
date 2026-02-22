namespace Nergard.ResourceEditor.Features.Shared.Models;

/// <summary>
/// Configuration options for the DeepL translation service.
/// Configure in appsettings.json under "ResourceEditor:DeepL".
/// </summary>
public class DeepLOptions
{
    /// <summary>
    /// DeepL API key (required).
    /// Obtain from: https://www.deepl.com/account/summary
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Use DeepL Free API endpoint (api-free.deepl.com).
    /// Set to false for DeepL Pro (api.deepl.com).
    /// Default: true
    /// </summary>
    public bool UseFreeApi { get; set; } = true;

    /// <summary>
    /// Request timeout in seconds.
    /// Default: 30
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
