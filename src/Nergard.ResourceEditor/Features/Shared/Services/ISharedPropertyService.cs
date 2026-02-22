namespace Nergard.ResourceEditor.Features.Shared.Services;

/// <summary>
/// Identifies properties that exist on multiple content types (pages, blocks, media).
/// Properties with the same name across types will share translations.
/// </summary>
public interface ISharedPropertyService
{
    /// <summary>
    /// Returns a dictionary mapping property names to the list of content type names
    /// that define that property. Only includes properties found on 2+ content types.
    /// </summary>
    IReadOnlyDictionary<string, IReadOnlyList<string>> GetSharedProperties();

    /// <summary>
    /// Returns true if the given property name exists on more than one content type.
    /// </summary>
    bool IsShared(string propertyName);

    /// <summary>
    /// Returns the content type names that share the given property,
    /// or an empty list if the property is not shared.
    /// </summary>
    IReadOnlyList<string> GetContentTypesForProperty(string propertyName);

    /// <summary>
    /// Clears the cached data, forcing a rebuild on next access.
    /// </summary>
    void Invalidate();
}
