using Nergard.ResourceEditor.Features.Shared.Models;

namespace Nergard.ResourceEditor.Features.ContentTypes.Models;

public class PropertyTranslation
{
    public string PropertyName { get; set; } = string.Empty;
    public TranslationEntry Label { get; set; } = new() { Key = "caption", DisplayName = "Label" };
    public TranslationEntry Description { get; set; } = new() { Key = "description", DisplayName = "Description" };

    public bool HasChanges => Label.IsDirty || Description.IsDirty;

    public void MarkAsClean()
    {
        Label.MarkAsClean();
        Description.MarkAsClean();
    }
}
