using System.Xml.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Nergard.ResourceEditor.Features.EditorHints.Models;
using Nergard.ResourceEditor.Features.Shared.Models;
using Nergard.ResourceEditor.Features.Shared.Services;
using static Nergard.ResourceEditor.Features.Shared.Services.XmlFileHelper;

namespace Nergard.ResourceEditor.Features.EditorHints.Services;

public class EditorHintLocalizationService(
    ILanguageService languageService,
    IWebHostEnvironment webHostEnvironment,
    IOptions<ResourceEditorOptions> options) : IEditorHintLocalizationService
{
    private static readonly string[] SectionNames = ["blocks", "preview", "renderingerror"];

    private static readonly Dictionary<string, string> SectionDisplayNames = new()
    {
        ["blocks"] = "Blocks",
        ["preview"] = "Preview",
        ["renderingerror"] = "Rendering Errors"
    };

    public EditorHintTranslation GetTranslations()
    {
        var translation = new EditorHintTranslation();
        var languages = languageService.GetLanguages();

        // Discover structure from all language files
        var structure = DiscoverStructure(languages);

        foreach (var (sectionName, entries) in structure)
        {
            var section = new EditorHintSection
            {
                Name = sectionName,
                DisplayName = SectionDisplayNames.GetValueOrDefault(sectionName, sectionName)
            };

            foreach (var (parentKey, childKey) in entries)
            {
                var entry = new EditorHintEntry
                {
                    ParentKey = parentKey,
                    Key = childKey
                };

                foreach (var lang in languages)
                {
                    var doc = LoadLanguageFile(lang.Id);
                    var sectionEl = doc?.Root?.Element(sectionName);

                    if (string.IsNullOrEmpty(parentKey))
                    {
                        // Direct child of section (e.g., preview/heading)
                        entry.Value.Values[lang.Id] = sectionEl?.Element(childKey)?.Value ?? string.Empty;
                    }
                    else
                    {
                        // Nested: section/parent/child (e.g., blocks/buttonblockcontrol/buttondefaulttext)
                        entry.Value.Values[lang.Id] = sectionEl?
                            .Element(parentKey)?
                            .Element(childKey)?.Value ?? string.Empty;
                    }
                }

                section.Entries.Add(entry);
            }

            translation.Sections.Add(section);
        }

        translation.MarkAsClean();
        return translation;
    }

    public void SaveTranslations(EditorHintTranslation translation)
    {
        var languages = languageService.GetLanguages();

        foreach (var lang in languages)
        {
            var existingDoc = LoadLanguageFile(lang.Id);
            var hasContent = translation.Sections
                .SelectMany(s => s.Entries)
                .Any(e => !string.IsNullOrEmpty(e.Value.Values.GetValueOrDefault(lang.Id, "")));

            if (existingDoc == null && !hasContent)
                continue;

            var doc = existingDoc ?? CreateLanguageDocument(lang);
            var root = doc.Root!;

            foreach (var section in translation.Sections)
            {
                var sectionEl = GetOrCreateElement(root, section.Name);

                foreach (var entry in section.Entries)
                {
                    var value = entry.Value.Values.GetValueOrDefault(lang.Id, "");

                    if (string.IsNullOrEmpty(entry.ParentKey))
                    {
                        SetElementValue(sectionEl, entry.Key, value);
                    }
                    else
                    {
                        var parentEl = GetOrCreateElement(sectionEl, entry.ParentKey);
                        SetElementValue(parentEl, entry.Key, value);
                    }
                }
            }

            doc.Save(GetFilePath(lang.Id));
        }

        translation.MarkAsClean();
    }

    private Dictionary<string, List<(string ParentKey, string ChildKey)>> DiscoverStructure(
        IReadOnlyList<LanguageInfo> languages)
    {
        var result = new Dictionary<string, List<(string, string)>>();

        foreach (var lang in languages)
        {
            var doc = LoadLanguageFile(lang.Id);
            if (doc?.Root == null) continue;

            foreach (var sectionName in SectionNames)
            {
                var sectionEl = doc.Root.Element(sectionName);
                if (sectionEl == null) continue;

                if (!result.ContainsKey(sectionName))
                    result[sectionName] = [];

                var existing = result[sectionName];

                foreach (var child in sectionEl.Elements())
                {
                    if (child.HasElements)
                    {
                        // Nested: parent/child structure
                        foreach (var grandchild in child.Elements())
                        {
                            var key = (child.Name.LocalName, grandchild.Name.LocalName);
                            if (!existing.Contains(key))
                                existing.Add(key);
                        }
                    }
                    else
                    {
                        // Direct value
                        var key = ("", child.Name.LocalName);
                        if (!existing.Contains(key))
                            existing.Add(key);
                    }
                }
            }

            // Also discover any non-standard sections
            foreach (var el in doc.Root.Elements())
            {
                var name = el.Name.LocalName;
                if (SectionNames.Contains(name)) continue;

                if (!result.ContainsKey(name))
                    result[name] = [];

                var existing = result[name];
                foreach (var child in el.Elements())
                {
                    if (child.HasElements)
                    {
                        foreach (var grandchild in child.Elements())
                        {
                            var key = (child.Name.LocalName, grandchild.Name.LocalName);
                            if (!existing.Contains(key))
                                existing.Add(key);
                        }
                    }
                    else
                    {
                        var key = ("", child.Name.LocalName);
                        if (!existing.Contains(key))
                            existing.Add(key);
                    }
                }
            }
        }

        return result;
    }

    private string TranslationBasePath =>
        Path.Combine(webHostEnvironment.ContentRootPath, options.Value.TranslationFolder);

    private XDocument? LoadLanguageFile(string languageId) =>
        XmlFileHelper.LoadLanguageFile(TranslationBasePath, "ReEditorHintNames", languageId);

    private string GetFilePath(string languageId) =>
        XmlFileHelper.GetFilePath(TranslationBasePath, "ReEditorHintNames", languageId);
}
