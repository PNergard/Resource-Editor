namespace Nergard.ResourceEditor.Features.Shared.Models;

/// <summary>
/// Configuration options for the Resource Editor.
/// Can be configured per environment in appsettings.json.
/// </summary>
public class ResourceEditorOptions
{
    /// <summary>
    /// Path to the folder containing translation XML files.
    /// </summary>
    public string TranslationFolder { get; set; } = "Resources/Translations";

    /// <summary>
    /// Enable saving translations to XML files.
    /// Set to false in environments without file system write access (e.g., DXP).
    /// Default: true
    /// </summary>
    public bool EnableFileSaving { get; set; } = true;

    /// <summary>
    /// Enable creating and managing localization overrides stored in DDS.
    /// Overrides take precedence over XML translations at runtime.
    /// Default: true
    /// </summary>
    public bool EnableOverrides { get; set; } = true;

    /// <summary>
    /// Show override controls in the editor UI (inline override buttons).
    /// When false, overrides can only be managed via the dedicated Overrides page.
    /// Default: true
    /// </summary>
    public bool ShowOverridesUI { get; set; } = true;

    /// <summary>
    /// Enable automated translation features.
    /// Requires a translation service to be registered (e.g., DeepL via AddDeepLTranslation()).
    /// Default: false
    /// </summary>
    public bool EnableAutomatedTranslation { get; set; } = false;

    /// <summary>
    /// Glob pattern for discovering view/frontend translation files.
    /// These files use a multi-language format with all languages in a single file.
    /// Default: "views_*.xml"
    /// </summary>
    public string ViewFilePattern { get; set; } = "views_*.xml";
}
