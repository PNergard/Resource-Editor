using Nergard.ResourceEditor.Features.Shared.Models;

namespace Nergard.ResourceEditor.Features.Shared.Services;

public interface ILanguageService
{
    IReadOnlyList<LanguageInfo> GetLanguages();
    LanguageInfo GetDefaultLanguage();
}
