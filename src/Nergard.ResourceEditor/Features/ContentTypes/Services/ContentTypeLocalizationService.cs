using System.Xml.Linq;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Nergard.ResourceEditor.Features.ContentTypes.Models;
using Nergard.ResourceEditor.Features.Shared.Models;
using Nergard.ResourceEditor.Features.Shared.Services;
using static Nergard.ResourceEditor.Features.Shared.Services.XmlFileHelper;

namespace Nergard.ResourceEditor.Features.ContentTypes.Services;

public class ContentTypeLocalizationService(
    IContentTypeRepository contentTypeRepository,
    ILanguageService languageService,
    IWebHostEnvironment webHostEnvironment,
    IOptions<ResourceEditorOptions> options) : IContentTypeLocalizationService
{
    public IReadOnlyList<ContentTypeInfo> GetPageTypes()
    {
        return GetContentTypes(ContentTypeCategory.Page, typeof(PageData));
    }

    public IReadOnlyList<ContentTypeInfo> GetBlockTypes()
    {
        return GetContentTypes(ContentTypeCategory.Block, typeof(BlockData));
    }

    public IReadOnlyList<ContentTypeInfo> GetMediaTypes()
    {
        return GetContentTypes(ContentTypeCategory.Media, typeof(MediaData));
    }

    private IReadOnlyList<ContentTypeInfo> GetContentTypes(ContentTypeCategory category, Type baseType)
    {
        return contentTypeRepository.List()
            .Where(ct => ct.ModelType != null && baseType.IsAssignableFrom(ct.ModelType))
            .Select(ct => new ContentTypeInfo(
                ct.ID,
                ct.Name,
                ct.LocalizedName ?? ct.Name,
                ct.LocalizedDescription,
                category))
            .OrderBy(ct => ct.DisplayName)
            .ToList();
    }

    public ContentTypeTranslation GetTranslations(string contentTypeName, ContentTypeCategory category)
    {
        var contentType = contentTypeRepository.List()
            .FirstOrDefault(ct => ct.Name.Equals(contentTypeName, StringComparison.OrdinalIgnoreCase));

        var translation = new ContentTypeTranslation
        {
            ContentTypeName = contentTypeName,
            Category = category
        };

        if (contentType != null)
        {
            foreach (var propDef in contentType.PropertyDefinitions.OrderBy(p => p.Name))
            {
                translation.Properties.Add(new PropertyTranslation
                {
                    PropertyName = propDef.Name
                });
            }
        }

        var languages = languageService.GetLanguages();

        foreach (var lang in languages)
        {
            // Read content type name + description from ReContentTypeNames_{lang}.xml
            var ctDoc = LoadLanguageFile("ReContentTypeNames", lang.Id);
            var typeElement = ctDoc?.Root?
                .Element("contenttypes")?
                .Element(contentTypeName.ToLowerInvariant());

            translation.Name.Values[lang.Id] = typeElement?.Element("name")?.Value ?? string.Empty;
            translation.Description.Values[lang.Id] = typeElement?.Element("description")?.Value ?? string.Empty;

            // Read properties from RePropertyNames_{lang}.xml
            // Primary location: contenttypes/{typename}/properties (Optimizely standard format)
            // Fallback: icontentdata/properties (legacy shared format)
            var propDoc = LoadLanguageFile("RePropertyNames", lang.Id);
            var contentTypesElement = propDoc?.Root?.Element("contenttypes");
            var typeSpecificProperties = contentTypesElement?
                .Element(contentTypeName.ToLowerInvariant())?
                .Element("properties");
            var globalProperties = contentTypesElement?
                .Element("icontentdata")?
                .Element("properties");

            foreach (var prop in translation.Properties)
            {
                var propKey = prop.PropertyName.ToLowerInvariant();
                var propElement = typeSpecificProperties?.Element(propKey)
                                  ?? globalProperties?.Element(propKey);
                prop.Label.Values[lang.Id] = propElement?.Element("caption")?.Value ?? string.Empty;
                prop.Description.Values[lang.Id] = propElement?.Element("help")?.Value ?? string.Empty;
            }
        }

        translation.MarkAsClean();
        return translation;
    }

    public void SaveTranslations(ContentTypeTranslation translation)
    {
        var languages = languageService.GetLanguages();

        foreach (var lang in languages)
        {
            var nameValue = translation.Name.Values.GetValueOrDefault(lang.Id, "");
            var descValue = translation.Description.Values.GetValueOrDefault(lang.Id, "");

            // Save content type name + description
            var existingCtDoc = LoadLanguageFile("ReContentTypeNames", lang.Id);
            var hasCtContent = !string.IsNullOrEmpty(nameValue) || !string.IsNullOrEmpty(descValue);

            if (existingCtDoc != null || hasCtContent)
            {
                var ctDoc = existingCtDoc ?? CreateLanguageDocument(lang);
                var ctRoot = ctDoc.Root!;
                var contentTypesElement = GetOrCreateElement(ctRoot, "contenttypes");
                var typeElement = GetOrCreateElement(contentTypesElement, translation.ContentTypeName.ToLowerInvariant());

                SetElementValue(typeElement, "name", nameValue);
                SetElementValue(typeElement, "description", descValue);

                ctDoc.Save(GetFilePath("ReContentTypeNames", lang.Id));
            }

            // Save properties under icontentdata (shared across all content types)
            // Structure: /contenttypes/icontentdata/properties/{propname}/caption + help
            if (translation.Properties.Count > 0)
            {
                var existingPropDoc = LoadLanguageFile("RePropertyNames", lang.Id);
                var hasPropContent = translation.Properties.Any(p =>
                    !string.IsNullOrEmpty(p.Label.Values.GetValueOrDefault(lang.Id, "")) ||
                    !string.IsNullOrEmpty(p.Description.Values.GetValueOrDefault(lang.Id, "")));

                if (existingPropDoc != null || hasPropContent)
                {
                    var propDoc = existingPropDoc ?? CreateLanguageDocument(lang);
                    var propRoot = propDoc.Root!;
                    var propContentTypes = GetOrCreateElement(propRoot, "contenttypes");
                    var icontentdata = GetOrCreateElement(propContentTypes, "icontentdata");
                    var propertiesElement = GetOrCreateElement(icontentdata, "properties");

                    foreach (var prop in translation.Properties)
                    {
                        var propElement = GetOrCreateElement(propertiesElement, prop.PropertyName.ToLowerInvariant());
                        SetElementValue(propElement, "caption", prop.Label.Values.GetValueOrDefault(lang.Id, ""));
                        SetElementValue(propElement, "help", prop.Description.Values.GetValueOrDefault(lang.Id, ""));
                    }

                    propDoc.Save(GetFilePath("RePropertyNames", lang.Id));
                }
            }
        }

        translation.MarkAsClean();
    }

    public void SavePropertyToXml(string propertyName, string language, string? caption, string? helpText)
    {
        var lang = languageService.GetLanguages()
            .FirstOrDefault(l => l.Id.Equals(language, StringComparison.OrdinalIgnoreCase));

        if (lang == null)
        {
            throw new ArgumentException($"Language '{language}' not found", nameof(language));
        }

        var propDoc = LoadLanguageFile("RePropertyNames", lang.Id)
                      ?? CreateLanguageDocument(lang);

        var propRoot = propDoc.Root!;
        var propContentTypes = GetOrCreateElement(propRoot, "contenttypes");
        var icontentdata = GetOrCreateElement(propContentTypes, "icontentdata");
        var propertiesElement = GetOrCreateElement(icontentdata, "properties");
        var propElement = GetOrCreateElement(propertiesElement, propertyName.ToLowerInvariant());

        if (!string.IsNullOrEmpty(caption))
        {
            SetElementValue(propElement, "caption", caption);
        }
        if (!string.IsNullOrEmpty(helpText))
        {
            SetElementValue(propElement, "help", helpText);
        }

        propDoc.Save(GetFilePath("RePropertyNames", lang.Id));
    }

    private string TranslationBasePath =>
        Path.Combine(webHostEnvironment.ContentRootPath, options.Value.TranslationFolder);

    private XDocument? LoadLanguageFile(string prefix, string languageId) =>
        XmlFileHelper.LoadLanguageFile(TranslationBasePath, prefix, languageId);

    private string GetFilePath(string prefix, string languageId) =>
        XmlFileHelper.GetFilePath(TranslationBasePath, prefix, languageId);
}
