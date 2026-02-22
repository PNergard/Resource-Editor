using System.Xml.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Nergard.ResourceEditor.Features.Shared.Models;
using Nergard.ResourceEditor.Features.Shared.Services;
using Nergard.ResourceEditor.Features.Views.Models;
using static Nergard.ResourceEditor.Features.Shared.Services.XmlFileHelper;

namespace Nergard.ResourceEditor.Features.Views.Services;

public class ViewLocalizationService(
    ILanguageService languageService,
    IWebHostEnvironment webHostEnvironment,
    IOptions<ResourceEditorOptions> options) : IViewLocalizationService
{
    public IReadOnlyList<ViewFileInfo> GetViewFiles()
    {
        var folder = GetTranslationFolder();
        if (!Directory.Exists(folder))
            return [];

        var pattern = options.Value.ViewFilePattern;
        var files = Directory.GetFiles(folder, pattern)
            .OrderBy(f => f)
            .Select(f => new ViewFileInfo
            {
                FileName = Path.GetFileName(f),
                DisplayName = DeriveDisplayName(Path.GetFileName(f))
            })
            .ToList();

        return files;
    }

    public ViewTranslation GetTranslations(string fileName)
    {
        var translation = new ViewTranslation
        {
            FileName = fileName,
            DisplayName = DeriveDisplayName(fileName)
        };

        var languages = languageService.GetLanguages();
        var filePath = GetFilePath(fileName);

        if (!File.Exists(filePath))
            return translation;

        var doc = XDocument.Load(filePath);
        if (doc.Root == null)
            return translation;

        // Discover all sections and keys across all languages
        var structure = DiscoverStructure(doc, languages);

        foreach (var (sectionName, keys) in structure)
        {
            var section = new ViewSection
            {
                Name = sectionName,
                DisplayName = Capitalize(sectionName)
            };

            foreach (var key in keys)
            {
                var entry = new ViewEntry { Key = key };

                foreach (var lang in languages)
                {
                    var langEl = doc.Root.Elements("language")
                        .FirstOrDefault(e => (string?)e.Attribute("id") == lang.Id);

                    var value = langEl?.Element(sectionName)?.Element(key)?.Value ?? string.Empty;
                    entry.Value.Values[lang.Id] = value;
                }

                section.Entries.Add(entry);
            }

            translation.Sections.Add(section);
        }

        translation.MarkAsClean();
        return translation;
    }

    public void SaveTranslations(ViewTranslation translation)
    {
        var languages = languageService.GetLanguages();
        var filePath = GetFilePath(translation.FileName);

        XDocument doc;
        if (File.Exists(filePath))
        {
            doc = XDocument.Load(filePath);
        }
        else
        {
            doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("languages"));
        }

        var root = doc.Root!;

        foreach (var lang in languages)
        {
            var langEl = root.Elements("language")
                .FirstOrDefault(e => (string?)e.Attribute("id") == lang.Id);

            var hasContent = translation.Sections
                .SelectMany(s => s.Entries)
                .Any(e => !string.IsNullOrEmpty(e.Value.Values.GetValueOrDefault(lang.Id, "")));

            // Skip adding a new language element if there's no content for it
            if (langEl == null && !hasContent)
                continue;

            if (langEl == null)
            {
                langEl = new XElement("language",
                    new XAttribute("name", lang.Name),
                    new XAttribute("id", lang.Id));
                root.Add(langEl);
            }

            var modelSectionNames = new HashSet<string>(translation.Sections.Select(s => s.Name));

            foreach (var section in translation.Sections)
            {
                var sectionEl = GetOrCreateElement(langEl, section.Name);

                var modelKeyNames = new HashSet<string>(section.Entries.Select(e => e.Key));

                foreach (var entry in section.Entries)
                {
                    var value = entry.Value.Values.GetValueOrDefault(lang.Id, "");
                    SetElementValue(sectionEl, entry.Key, value);
                }

                // Remove keys that no longer exist in the model
                var keysToRemove = sectionEl.Elements()
                    .Where(e => !modelKeyNames.Contains(e.Name.LocalName))
                    .ToList();
                foreach (var el in keysToRemove)
                    el.Remove();
            }

            // Remove sections that no longer exist in the model
            var sectionsToRemove = langEl.Elements()
                .Where(e => !modelSectionNames.Contains(e.Name.LocalName))
                .ToList();
            foreach (var el in sectionsToRemove)
                el.Remove();
        }

        doc.Save(filePath);
        translation.MarkAsClean();
    }

    private Dictionary<string, List<string>> DiscoverStructure(
        XDocument doc, IReadOnlyList<LanguageInfo> languages)
    {
        var result = new Dictionary<string, List<string>>();

        foreach (var lang in languages)
        {
            var langEl = doc.Root?.Elements("language")
                .FirstOrDefault(e => (string?)e.Attribute("id") == lang.Id);

            if (langEl == null) continue;

            foreach (var sectionEl in langEl.Elements())
            {
                var sectionName = sectionEl.Name.LocalName;

                if (!result.ContainsKey(sectionName))
                    result[sectionName] = [];

                var existing = result[sectionName];

                foreach (var child in sectionEl.Elements())
                {
                    var key = child.Name.LocalName;
                    if (!existing.Contains(key))
                        existing.Add(key);
                }
            }
        }

        return result;
    }

    private string DeriveDisplayName(string fileName)
    {
        // Strip the pattern prefix and .xml suffix
        // e.g., "views_contact.xml" → "Contact"
        var pattern = options.Value.ViewFilePattern;
        var prefix = pattern.IndexOf('*') >= 0
            ? pattern[..pattern.IndexOf('*')]
            : string.Empty;

        var name = fileName;
        if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            name = name[prefix.Length..];

        if (name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            name = name[..^4];

        return Capitalize(name);
    }

    private string GetTranslationFolder()
    {
        return Path.Combine(
            webHostEnvironment.ContentRootPath,
            options.Value.TranslationFolder);
    }

    private string GetFilePath(string fileName)
    {
        return Path.Combine(GetTranslationFolder(), fileName);
    }

    private static string Capitalize(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return char.ToUpperInvariant(value[0]) + value[1..];
    }

}
