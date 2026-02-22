using Nergard.ResourceEditor.Features.Shared.Models;
using Nergard.ResourceEditor.Features.Tabs.Models;

namespace Nergard.ResourceEditor.Features.Tabs.Services;

public interface ITabLocalizationService
{
    IReadOnlyList<TabInfo> GetTabs();
    TabTranslation GetTranslations(string tabName);
    void SaveTranslations(TabTranslation translation);
}
