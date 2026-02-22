using Nergard.ResourceEditor.Features.ContentTypes.Models;
using Nergard.ResourceEditor.Features.Shared.Models;
using Nergard.ResourceEditor.Features.Tabs.Models;

namespace Nergard.ResourceEditor.Features.Shared.Services;

public interface ITranslationStatusEvaluator
{
    TranslationStatusResult EvaluateContentType(ContentTypeTranslation translation, string languageId);
    TabStatusResult EvaluateTab(TabTranslation translation, string languageId);
}
