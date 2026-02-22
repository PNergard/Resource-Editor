using Microsoft.AspNetCore.Components;
using MudBlazor;
using Nergard.ResourceEditor.Features.ContentTypes.Models;
using Nergard.ResourceEditor.Features.Shared.Helpers;
using Nergard.ResourceEditor.Features.Shared.Models;
using Nergard.ResourceEditor.Features.Shared.Services;

namespace Nergard.ResourceEditor.Features.ContentTypes.Components;

public partial class ContentTypeEditor
{
    [Inject] private ISharedPropertyService SharedPropertyService { get; set; } = default!;

    private bool _propertiesExpanded;

    private void TogglePropertiesExpanded()
    {
        _propertiesExpanded = !_propertiesExpanded;
    }

    protected override List<TranslatableEntry> GetTranslatableEntries()
    {
        var entries = new List<TranslatableEntry>
        {
            new(Translation.Name, GetNameKey(), Translation.ContentTypeName, TranslationFieldType.ContentTypeName),
            new(Translation.Description, GetDescriptionKey(), Translation.ContentTypeName, TranslationFieldType.ContentTypeDescription)
        };

        foreach (var prop in Translation.Properties)
        {
            entries.Add(new(prop.Label, GetPropertyLabelKey(prop.PropertyName), Translation.ContentTypeName, TranslationFieldType.PropertyLabel));
            entries.Add(new(prop.Description, GetPropertyDescriptionKey(prop.PropertyName), Translation.ContentTypeName, TranslationFieldType.PropertyDescription));
        }

        return entries;
    }

    private string GetSharedTooltip(string propertyName)
    {
        var types = SharedPropertyService.GetContentTypesForProperty(propertyName);
        return $"Shared property — used by: {string.Join(", ", types)}";
    }

    private Color GetPropertyStatus(PropertyTranslation prop)
    {
        var langIds = FilteredLanguages.Select(l => l.Id).ToList();
        return PropertyStatusHelper.GetPropertyStatus(prop, langIds);
    }

    private string GetNameKey()
    {
        return $"/contenttypes/{Translation.ContentTypeName.ToLowerInvariant()}/name";
    }

    private string GetDescriptionKey()
    {
        return $"/contenttypes/{Translation.ContentTypeName.ToLowerInvariant()}/description";
    }

    private string GetPropertyLabelKey(string propertyName)
    {
        return $"/contenttypes/icontentdata/properties/{propertyName.ToLowerInvariant()}/caption";
    }

    private string GetPropertyDescriptionKey(string propertyName)
    {
        return $"/contenttypes/icontentdata/properties/{propertyName.ToLowerInvariant()}/help";
    }
}
