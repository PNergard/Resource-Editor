using Nergard.ResourceEditor.Features.ContentTypes.Models;
using Nergard.ResourceEditor.Features.Shared.Models;

namespace Nergard.ResourceEditor.Features.ContentTypes.Services;

public interface IContentTypeLocalizationService
{
    IReadOnlyList<ContentTypeInfo> GetPageTypes();
    IReadOnlyList<ContentTypeInfo> GetBlockTypes();
    IReadOnlyList<ContentTypeInfo> GetMediaTypes();
    ContentTypeTranslation GetTranslations(string contentTypeName, ContentTypeCategory category);
    void SaveTranslations(ContentTypeTranslation translation);

    /// <summary>
    /// Saves a single property override to the XML file for the specified language.
    /// Used when migrating an override from DDS to XML.
    /// </summary>
    void SavePropertyToXml(string propertyName, string language, string? caption, string? helpText);
}
