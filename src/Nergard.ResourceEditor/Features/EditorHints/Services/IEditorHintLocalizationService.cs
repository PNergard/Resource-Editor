using Nergard.ResourceEditor.Features.EditorHints.Models;

namespace Nergard.ResourceEditor.Features.EditorHints.Services;

public interface IEditorHintLocalizationService
{
    EditorHintTranslation GetTranslations();
    void SaveTranslations(EditorHintTranslation translation);
}
