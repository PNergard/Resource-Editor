using System.Xml.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Nergard.ResourceEditor.Features.Shared.Models;
using static Nergard.ResourceEditor.Features.Shared.Services.XmlFileHelper;

namespace Nergard.ResourceEditor.Features.Shared.Services;

public class XmlMigrationService(
    ILanguageService languageService,
    IWebHostEnvironment webHostEnvironment,
    IOptions<ResourceEditorOptions> options) : IXmlMigrationService
{
    private string TranslationFolder => Path.Combine(
        webHostEnvironment.ContentRootPath,
        options.Value.TranslationFolder);

    public bool NeedsMigration()
    {
        var folder = TranslationFolder;
        if (!Directory.Exists(folder))
            return false;

        // If any Re* file exists, migration has already been done
        return !Directory.GetFiles(folder, "Re*.xml").Any();
    }

    public async Task<MigrationResult> MigrateAsync(IProgress<MigrationProgress>? progress = null)
    {
        var errors = new List<string>();
        var filesCreated = 0;
        var languages = languageService.GetLanguages();
        const int totalSteps = 5;

        try
        {
            const int stepDelay = 1000;

            // Step 1: ContentTypeNames → ReContentTypeNames (name+desc only)
            progress?.Report(new MigrationProgress("Migrating content type names...", 0, totalSteps));
            await Task.Delay(stepDelay);
            filesCreated += await MigrateContentTypeNamesAsync(languages, errors);

            // Step 2: ContentTypeNames + PropertyNames → RePropertyNames
            progress?.Report(new MigrationProgress("Migrating property names...", 1, totalSteps));
            await Task.Delay(stepDelay);
            filesCreated += await MigratePropertyNamesAsync(languages, errors);

            // Step 3: GroupNames → ReGroupNames
            progress?.Report(new MigrationProgress("Migrating group names...", 2, totalSteps));
            await Task.Delay(stepDelay);
            filesCreated += await MigrateGroupNamesAsync(languages, errors);

            // Step 4: Display → ReDisplayChannelNames
            progress?.Report(new MigrationProgress("Migrating display channel names...", 3, totalSteps));
            await Task.Delay(stepDelay);
            filesCreated += await MigrateDisplayAsync(languages, errors);

            // Step 5: EditorHints → ReEditorHintNames
            progress?.Report(new MigrationProgress("Migrating editor hint names...", 4, totalSteps));
            await Task.Delay(stepDelay);
            filesCreated += await MigrateEditorHintsAsync(languages, errors);

            progress?.Report(new MigrationProgress("Migration complete", totalSteps, totalSteps));
        }
        catch (Exception ex)
        {
            errors.Add($"Unexpected error: {ex.Message}");
        }

        return new MigrationResult(errors.Count == 0, filesCreated, errors);
    }

    private async Task<int> MigrateContentTypeNamesAsync(
        IReadOnlyList<LanguageInfo> languages, List<string> errors)
    {
        var sourceFile = Path.Combine(TranslationFolder, "ContentTypeNames.xml");
        if (!File.Exists(sourceFile)) return 0;

        var doc = XDocument.Load(sourceFile);
        var count = 0;

        foreach (var lang in languages)
        {
            var langElement = FindLanguageElement(doc, lang.Id);
            if (langElement == null) continue;

            var newDoc = CreateLanguageDocument(lang);
            var contentTypes = langElement.Element("contenttypes");
            if (contentTypes != null)
            {
                var newContentTypes = new XElement("contenttypes");
                foreach (var ct in contentTypes.Elements())
                {
                    // Only copy name and description, not properties
                    var newCt = new XElement(ct.Name.LocalName);
                    var nameEl = ct.Element("name");
                    var descEl = ct.Element("description");
                    if (nameEl != null) newCt.Add(new XElement("name", nameEl.Value));
                    if (descEl != null) newCt.Add(new XElement("description", descEl.Value));
                    newContentTypes.Add(newCt);
                }
                newDoc.Root!.Add(newContentTypes);
            }

            await SaveXmlAsync(newDoc, GetNewFilePath("ReContentTypeNames", lang.Id));
            count++;
        }

        return count;
    }

    private async Task<int> MigratePropertyNamesAsync(
        IReadOnlyList<LanguageInfo> languages, List<string> errors)
    {
        // Properties come from two sources:
        // 1. ContentTypeNames.xml (properties nested under each content type)
        // 2. PropertyNames.xml (standalone property definitions)
        var contentTypeFile = Path.Combine(TranslationFolder, "ContentTypeNames.xml");
        var propertyFile = Path.Combine(TranslationFolder, "PropertyNames.xml");

        XDocument? ctDoc = File.Exists(contentTypeFile) ? XDocument.Load(contentTypeFile) : null;
        XDocument? propDoc = File.Exists(propertyFile) ? XDocument.Load(propertyFile) : null;

        if (ctDoc == null && propDoc == null) return 0;

        var count = 0;

        foreach (var lang in languages)
        {
            var newDoc = CreateLanguageDocument(lang);
            var newContentTypes = new XElement("contenttypes");
            var iContentData = GetOrCreateElement(newContentTypes, "icontentdata");
            var allProperties = GetOrCreateElement(iContentData, "properties");

            // All properties go under icontentdata regardless of source content type
            void CollectProperties(XDocument? doc)
            {
                if (doc == null) return;
                var langElement = FindLanguageElement(doc, lang.Id);
                var contentTypes = langElement?.Element("contenttypes");
                if (contentTypes == null) return;

                foreach (var ct in contentTypes.Elements())
                {
                    var propsEl = ct.Element("properties");
                    if (propsEl == null || !propsEl.HasElements) continue;

                    foreach (var prop in propsEl.Elements())
                    {
                        if (allProperties.Element(prop.Name.LocalName) == null)
                            allProperties.Add(new XElement(prop));
                    }
                }
            }

            CollectProperties(ctDoc);
            CollectProperties(propDoc);

            if (allProperties.HasElements)
                newDoc.Root!.Add(newContentTypes);

            await SaveXmlAsync(newDoc, GetNewFilePath("RePropertyNames", lang.Id));
            count++;
        }

        return count;
    }

    private async Task<int> MigrateGroupNamesAsync(
        IReadOnlyList<LanguageInfo> languages, List<string> errors)
    {
        var sourceFile = Path.Combine(TranslationFolder, "GroupNames.xml");
        if (!File.Exists(sourceFile)) return 0;

        var doc = XDocument.Load(sourceFile);
        var count = 0;

        foreach (var lang in languages)
        {
            var langElement = FindLanguageElement(doc, lang.Id);
            if (langElement == null) continue;

            var newDoc = CreateLanguageDocument(lang);

            // Copy headings and propertygroupsettings as-is
            var headings = langElement.Element("headings");
            if (headings != null) newDoc.Root!.Add(new XElement(headings));

            var groupSettings = langElement.Element("propertygroupsettings");
            if (groupSettings != null) newDoc.Root!.Add(new XElement(groupSettings));

            await SaveXmlAsync(newDoc, GetNewFilePath("ReGroupNames", lang.Id));
            count++;
        }

        return count;
    }

    private async Task<int> MigrateDisplayAsync(
        IReadOnlyList<LanguageInfo> languages, List<string> errors)
    {
        var sourceFile = Path.Combine(TranslationFolder, "Display.xml");
        if (!File.Exists(sourceFile)) return 0;

        var doc = XDocument.Load(sourceFile);
        var count = 0;

        foreach (var lang in languages)
        {
            var langElement = FindLanguageElement(doc, lang.Id);
            if (langElement == null) continue;

            var newDoc = CreateLanguageDocument(lang);

            var channels = langElement.Element("displaychannels");
            if (channels != null) newDoc.Root!.Add(new XElement(channels));

            var options = langElement.Element("displayoptions");
            if (options != null) newDoc.Root!.Add(new XElement(options));

            var resolutions = langElement.Element("resolutions");
            if (resolutions != null) newDoc.Root!.Add(new XElement(resolutions));

            await SaveXmlAsync(newDoc, GetNewFilePath("ReDisplayChannelNames", lang.Id));
            count++;
        }

        return count;
    }

    private async Task<int> MigrateEditorHintsAsync(
        IReadOnlyList<LanguageInfo> languages, List<string> errors)
    {
        var sourceFile = Path.Combine(TranslationFolder, "EditorHints.xml");
        if (!File.Exists(sourceFile)) return 0;

        var doc = XDocument.Load(sourceFile);
        var count = 0;

        foreach (var lang in languages)
        {
            var langElement = FindLanguageElement(doc, lang.Id);
            if (langElement == null) continue;

            var newDoc = CreateLanguageDocument(lang);

            // Copy all child elements (blocks, preview, renderingerror)
            foreach (var child in langElement.Elements())
            {
                newDoc.Root!.Add(new XElement(child));
            }

            await SaveXmlAsync(newDoc, GetNewFilePath("ReEditorHintNames", lang.Id));
            count++;
        }

        return count;
    }

    private static XElement? FindLanguageElement(XDocument doc, string languageId)
    {
        return doc.Root?.Elements("language")
            .FirstOrDefault(e => e.Attribute("id")?.Value
                .Equals(languageId, StringComparison.OrdinalIgnoreCase) == true);
    }

    private string GetNewFilePath(string prefix, string languageId) =>
        XmlFileHelper.GetFilePath(TranslationFolder, prefix, languageId);

    private static async Task SaveXmlAsync(XDocument doc, string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (directory != null && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await doc.SaveAsync(stream, SaveOptions.None, CancellationToken.None);
    }
}
