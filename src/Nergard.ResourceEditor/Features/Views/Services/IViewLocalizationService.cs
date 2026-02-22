using Nergard.ResourceEditor.Features.Views.Models;

namespace Nergard.ResourceEditor.Features.Views.Services;

public interface IViewLocalizationService
{
    IReadOnlyList<ViewFileInfo> GetViewFiles();
    ViewTranslation GetTranslations(string fileName);
    void SaveTranslations(ViewTranslation translation);
}
