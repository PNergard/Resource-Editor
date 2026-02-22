namespace Nergard.ResourceEditor.Features.Shared.Models;

public class LanguageStatusSummary
{
    public string LanguageId { get; set; } = string.Empty;
    public string LanguageName { get; set; } = string.Empty;
    public bool IsDefault { get; set; }

    public int ContentTypesTotal { get; set; }
    public int ContentTypesComplete { get; set; }

    public int PropertiesTotal { get; set; }
    public int PropertiesComplete { get; set; }

    public int TabsTotal { get; set; }
    public int TabsComplete { get; set; }
}
