using Nergard.ResourceEditor.Features.Shared.Models;

namespace Nergard.ResourceEditor.Features.Tabs.Models;

public class TabTranslation
{
    public int TabId { get; set; }
    public string TabName { get; set; } = string.Empty;
    public TranslationEntry DisplayName { get; set; } = new() { Key = "caption", DisplayName = "Display Name" };

    public bool HasChanges => DisplayName.IsDirty;

    public void MarkAsClean()
    {
        DisplayName.MarkAsClean();
    }
}
