using Nergard.ResourceEditor.Features.Shared.Models;

namespace Nergard.ResourceEditor.Features.Tabs.Components;

public partial class TabEditor
{
    protected override List<TranslatableEntry> GetTranslatableEntries()
    {
        return
        [
            new(Translation.DisplayName, GetTabKey(), $"Tab: {Translation.TabName}", TranslationFieldType.TabDisplayName)
        ];
    }

    private string GetTabKey()
    {
        var normalizedName = Translation.TabName.ToLowerInvariant().Replace(" ", "");
        return $"/propertygroupsettings/{normalizedName}/caption";
    }
}
