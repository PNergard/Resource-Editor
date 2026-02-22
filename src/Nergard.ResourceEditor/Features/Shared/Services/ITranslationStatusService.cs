using Nergard.ResourceEditor.Features.Shared.Models;

namespace Nergard.ResourceEditor.Features.Shared.Services;

public interface ITranslationStatusService
{
    IReadOnlyList<LanguageStatusSummary> GetLanguageSummaries();
    void Invalidate();
}
