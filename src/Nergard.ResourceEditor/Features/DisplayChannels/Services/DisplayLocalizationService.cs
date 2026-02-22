using System.Xml.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Nergard.ResourceEditor.Features.DisplayChannels.Models;
using Nergard.ResourceEditor.Features.Shared.Models;
using Nergard.ResourceEditor.Features.Shared.Services;
using static Nergard.ResourceEditor.Features.Shared.Services.XmlFileHelper;

namespace Nergard.ResourceEditor.Features.DisplayChannels.Services;

public class DisplayLocalizationService(
    ILanguageService languageService,
    IWebHostEnvironment webHostEnvironment,
    IOptions<ResourceEditorOptions> options) : IDisplayLocalizationService
{
    public DisplayTranslation GetTranslations()
    {
        var translation = new DisplayTranslation();
        var languages = languageService.GetLanguages();

        // Build structure from first available file to discover keys
        var keys = DiscoverKeys(languages);
        translation.Channels = keys.channels.Select(k => new NamedTranslationEntry { Key = k, DisplayLabel = k }).ToList();
        translation.Options = keys.options.Select(k => new NamedTranslationEntry { Key = k, DisplayLabel = k }).ToList();
        translation.Resolutions = keys.resolutions.Select(k => new NamedTranslationEntry { Key = k, DisplayLabel = k }).ToList();

        foreach (var lang in languages)
        {
            var doc = LoadLanguageFile(lang.Id);
            var root = doc?.Root;

            // Channels: <displaychannels><displaychannel name="x"><name>Y</name>
            foreach (var channel in translation.Channels)
            {
                var channelEl = root?.Element("displaychannels")?
                    .Elements("displaychannel")
                    .FirstOrDefault(e => e.Attribute("name")?.Value == channel.Key);
                channel.Value.Values[lang.Id] = channelEl?.Element("name")?.Value ?? string.Empty;
            }

            // Options: <displayoptions><full>Full</full>
            foreach (var option in translation.Options)
            {
                var value = root?.Element("displayoptions")?.Element(option.Key)?.Value;
                option.Value.Values[lang.Id] = value ?? string.Empty;
            }

            // Resolutions: <resolutions><androidvertical>Android vertical (480x800)</androidvertical>
            foreach (var resolution in translation.Resolutions)
            {
                var value = root?.Element("resolutions")?.Element(resolution.Key)?.Value;
                resolution.Value.Values[lang.Id] = value ?? string.Empty;
            }
        }

        translation.MarkAsClean();
        return translation;
    }

    public void SaveTranslations(DisplayTranslation translation)
    {
        var languages = languageService.GetLanguages();

        foreach (var lang in languages)
        {
            var existingDoc = LoadLanguageFile(lang.Id);
            var hasContent = translation.Channels.Any(c => !string.IsNullOrEmpty(c.Value.Values.GetValueOrDefault(lang.Id, "")))
                          || translation.Options.Any(o => !string.IsNullOrEmpty(o.Value.Values.GetValueOrDefault(lang.Id, "")))
                          || translation.Resolutions.Any(r => !string.IsNullOrEmpty(r.Value.Values.GetValueOrDefault(lang.Id, "")));

            if (existingDoc == null && !hasContent)
                continue;

            var doc = existingDoc ?? CreateLanguageDocument(lang);
            var root = doc.Root!;

            // Channels
            var channelsEl = GetOrCreateElement(root, "displaychannels");
            foreach (var channel in translation.Channels)
            {
                var el = channelsEl.Elements("displaychannel")
                    .FirstOrDefault(e => e.Attribute("name")?.Value == channel.Key);
                if (el == null)
                {
                    el = new XElement("displaychannel", new XAttribute("name", channel.Key));
                    channelsEl.Add(el);
                }
                SetElementValue(el, "name", channel.Value.Values.GetValueOrDefault(lang.Id, ""));
            }

            // Options
            var optionsEl = GetOrCreateElement(root, "displayoptions");
            foreach (var option in translation.Options)
            {
                var el = optionsEl.Element(option.Key);
                if (el != null)
                    el.Value = option.Value.Values.GetValueOrDefault(lang.Id, "");
                else
                    optionsEl.Add(new XElement(option.Key, option.Value.Values.GetValueOrDefault(lang.Id, "")));
            }

            // Resolutions
            var resolutionsEl = GetOrCreateElement(root, "resolutions");
            foreach (var resolution in translation.Resolutions)
            {
                var el = resolutionsEl.Element(resolution.Key);
                if (el != null)
                    el.Value = resolution.Value.Values.GetValueOrDefault(lang.Id, "");
                else
                    resolutionsEl.Add(new XElement(resolution.Key, resolution.Value.Values.GetValueOrDefault(lang.Id, "")));
            }

            doc.Save(GetFilePath(lang.Id));
        }

        translation.MarkAsClean();
    }

    private (List<string> channels, List<string> options, List<string> resolutions) DiscoverKeys(
        IReadOnlyList<LanguageInfo> languages)
    {
        var channels = new HashSet<string>();
        var opts = new HashSet<string>();
        var resolutions = new HashSet<string>();

        foreach (var lang in languages)
        {
            var doc = LoadLanguageFile(lang.Id);
            if (doc?.Root == null) continue;

            foreach (var ch in doc.Root.Element("displaychannels")?.Elements("displaychannel") ?? [])
            {
                var name = ch.Attribute("name")?.Value;
                if (name != null) channels.Add(name);
            }

            foreach (var el in doc.Root.Element("displayoptions")?.Elements() ?? [])
                opts.Add(el.Name.LocalName);

            foreach (var el in doc.Root.Element("resolutions")?.Elements() ?? [])
                resolutions.Add(el.Name.LocalName);
        }

        return (channels.OrderBy(x => x).ToList(), opts.OrderBy(x => x).ToList(), resolutions.OrderBy(x => x).ToList());
    }

    private string TranslationBasePath =>
        Path.Combine(webHostEnvironment.ContentRootPath, options.Value.TranslationFolder);

    private XDocument? LoadLanguageFile(string languageId) =>
        XmlFileHelper.LoadLanguageFile(TranslationBasePath, "ReDisplayChannelNames", languageId);

    private string GetFilePath(string languageId) =>
        XmlFileHelper.GetFilePath(TranslationBasePath, "ReDisplayChannelNames", languageId);
}
