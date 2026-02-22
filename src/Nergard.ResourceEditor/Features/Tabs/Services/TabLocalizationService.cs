using System.Xml.Linq;
using EPiServer.DataAbstraction;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Nergard.ResourceEditor.Features.Shared.Models;
using Nergard.ResourceEditor.Features.Shared.Services;
using Nergard.ResourceEditor.Features.Tabs.Models;
using static Nergard.ResourceEditor.Features.Shared.Services.XmlFileHelper;

namespace Nergard.ResourceEditor.Features.Tabs.Services;

public class TabLocalizationService(
    ITabDefinitionRepository tabRepository,
    ILanguageService languageService,
    IWebHostEnvironment webHostEnvironment,
    IOptions<ResourceEditorOptions> options) : ITabLocalizationService
{
    public IReadOnlyList<TabInfo> GetTabs()
    {
        return tabRepository.List()
            .Select(t => new TabInfo(t.ID, t.Name, t.LocalizedName ?? t.Name))
            .OrderBy(t => t.DisplayName)
            .ToList();
    }

    public TabTranslation GetTranslations(string tabName)
    {
        var tab = tabRepository.List().FirstOrDefault(t => t.Name == tabName);
        var translation = new TabTranslation
        {
            TabId = tab?.ID ?? 0,
            TabName = tabName
        };

        var languages = languageService.GetLanguages();

        foreach (var lang in languages)
        {
            var doc = LoadLanguageFile(lang.Id);
            if (doc == null)
            {
                translation.DisplayName.Values[lang.Id] = string.Empty;
                continue;
            }

            var value = GetTabValue(doc.Root, tabName);
            translation.DisplayName.Values[lang.Id] = value;
        }

        translation.MarkAsClean();
        return translation;
    }

    private static string GetTabValue(XElement? langElement, string tabName)
    {
        if (langElement == null) return string.Empty;

        // Structure 1: /headings/heading[@name='TabName']
        var headingElement = langElement
            .Element("headings")?
            .Elements("heading")
            .FirstOrDefault(e => e.Attribute("name")?.Value.Equals(tabName, StringComparison.OrdinalIgnoreCase) == true);

        if (headingElement != null)
            return headingElement.Element("description")?.Value ?? headingElement.Value;

        // Structure 2: /propertygroupsettings/TabName/caption
        var groupElement = langElement
            .Element("propertygroupsettings")?
            .Element(tabName.ToLowerInvariant().Replace(" ", ""));

        if (groupElement != null)
            return groupElement.Element("caption")?.Value ?? string.Empty;

        return string.Empty;
    }

    public void SaveTranslations(TabTranslation translation)
    {
        var languages = languageService.GetLanguages();

        foreach (var lang in languages)
        {
            var existingDoc = LoadLanguageFile(lang.Id);
            var value = translation.DisplayName.Values.GetValueOrDefault(lang.Id, "");
            var hasContent = !string.IsNullOrEmpty(value);

            if (existingDoc == null && !hasContent)
                continue;

            var doc = existingDoc ?? CreateLanguageDocument(lang);
            var root = doc.Root!;
            var headingsElement = GetOrCreateElement(root, "headings");

            var headingElement = headingsElement.Elements("heading")
                .FirstOrDefault(e => e.Attribute("name")?.Value.Equals(translation.TabName, StringComparison.OrdinalIgnoreCase) == true);

            if (headingElement == null)
            {
                headingElement = new XElement("heading",
                    new XAttribute("name", translation.TabName));
                headingsElement.Add(headingElement);
            }

            SetElementValue(headingElement, "description", value);

            doc.Save(GetFilePath(lang.Id));
        }

        translation.MarkAsClean();
    }

    private string TranslationBasePath =>
        Path.Combine(webHostEnvironment.ContentRootPath, options.Value.TranslationFolder);

    private XDocument? LoadLanguageFile(string languageId) =>
        XmlFileHelper.LoadLanguageFile(TranslationBasePath, "ReGroupNames", languageId);

    private string GetFilePath(string languageId) =>
        XmlFileHelper.GetFilePath(TranslationBasePath, "ReGroupNames", languageId);
}
