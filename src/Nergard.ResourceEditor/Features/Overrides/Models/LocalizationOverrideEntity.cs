using EPiServer.Data;
using EPiServer.Data.Dynamic;

namespace Nergard.ResourceEditor.Features.Overrides.Models;

/// <summary>
/// DDS entity for storing localization overrides.
/// Overrides take precedence over XML-based translations.
/// </summary>
[EPiServerDataStore(AutomaticallyRemapStore = true, AutomaticallyCreateStore = true)]
public class LocalizationOverrideEntity : IDynamicData
{
    public Identity Id { get; set; } = Identity.NewIdentity();

    /// <summary>
    /// The localization key (e.g., "/contenttypes/standardpage/name")
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The language code (e.g., "en", "sv")
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// The override value for this key/language combination
    /// </summary>
    public string OverrideValue { get; set; } = string.Empty;

    /// <summary>
    /// Optional: The content type this override applies to (for reference/filtering)
    /// </summary>
    public string? ContentTypeName { get; set; }

    /// <summary>
    /// The user who last modified this override
    /// </summary>
    public string ModifiedBy { get; set; } = string.Empty;

    /// <summary>
    /// When this override was last modified
    /// </summary>
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
}
