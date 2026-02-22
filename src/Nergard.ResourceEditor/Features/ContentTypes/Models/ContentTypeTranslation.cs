using Nergard.ResourceEditor.Features.Shared.Models;

namespace Nergard.ResourceEditor.Features.ContentTypes.Models;

public class ContentTypeTranslation
{
    public string ContentTypeName { get; set; } = string.Empty;
    public ContentTypeCategory Category { get; set; }
    public TranslationEntry Name { get; set; } = new() { Key = "name", DisplayName = "Name" };
    public TranslationEntry Description { get; set; } = new() { Key = "description", DisplayName = "Description" };
    public List<PropertyTranslation> Properties { get; set; } = [];

    public bool HasChanges => Name.IsDirty || Description.IsDirty || Properties.Any(p => p.HasChanges);

    public void MarkAsClean()
    {
        Name.MarkAsClean();
        Description.MarkAsClean();
        foreach (var prop in Properties)
        {
            prop.MarkAsClean();
        }
    }
}
