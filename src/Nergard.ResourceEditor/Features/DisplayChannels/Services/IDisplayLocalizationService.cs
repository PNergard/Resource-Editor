using Nergard.ResourceEditor.Features.DisplayChannels.Models;

namespace Nergard.ResourceEditor.Features.DisplayChannels.Services;

public interface IDisplayLocalizationService
{
    DisplayTranslation GetTranslations();
    void SaveTranslations(DisplayTranslation translation);
}
