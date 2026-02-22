using System.Xml.Linq;
using Nergard.ResourceEditor.Features.Shared.Models;

namespace Nergard.ResourceEditor.Features.Shared.Services;

/// <summary>
/// Shared XML helper methods used by all localization services.
/// </summary>
public static class XmlFileHelper
{
    public static XElement GetOrCreateElement(XElement parent, string name)
    {
        var element = parent.Element(name);
        if (element == null)
        {
            element = new XElement(name);
            parent.Add(element);
        }
        return element;
    }

    public static void SetElementValue(XElement parent, string name, string value)
    {
        var element = parent.Element(name);
        if (element != null)
            element.Value = value;
        else
            parent.Add(new XElement(name, value));
    }

    public static XDocument CreateLanguageDocument(LanguageInfo lang)
    {
        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("language",
                new XAttribute("name", lang.Name),
                new XAttribute("id", lang.Id)));
    }

    public static XDocument? LoadLanguageFile(string basePath, string prefix, string languageId)
    {
        var path = GetFilePath(basePath, prefix, languageId);
        return File.Exists(path) ? XDocument.Load(path) : null;
    }

    public static string GetFilePath(string basePath, string prefix, string languageId)
    {
        return Path.Combine(basePath, $"{prefix}_{languageId}.xml");
    }
}
