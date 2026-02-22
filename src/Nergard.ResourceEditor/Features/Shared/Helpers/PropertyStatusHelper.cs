using MudBlazor;
using Nergard.ResourceEditor.Features.ContentTypes.Models;

namespace Nergard.ResourceEditor.Features.Shared.Helpers;

/// <summary>
/// Evaluates translation completeness for properties across one or more languages.
/// </summary>
public static class PropertyStatusHelper
{
    /// <summary>
    /// Returns a status color based on how complete the property translations are
    /// for the given language IDs (label + description per language).
    /// </summary>
    public static Color GetPropertyStatus(PropertyTranslation prop, IReadOnlyList<string> languageIds)
    {
        var total = languageIds.Count * 2; // label + description per language
        if (total == 0) return Color.Success;

        var complete = 0;
        foreach (var langId in languageIds)
        {
            if (prop.Label.Values.TryGetValue(langId, out var label)
                && !string.IsNullOrWhiteSpace(label)
                && !label.Equals(prop.PropertyName, StringComparison.OrdinalIgnoreCase))
                complete++;

            if (prop.Description.Values.TryGetValue(langId, out var desc)
                && !string.IsNullOrWhiteSpace(desc))
                complete++;
        }

        var ratio = (double)complete / total;
        if (ratio >= 1.0) return Color.Success;
        if (ratio >= 0.5) return Color.Warning;
        return Color.Error;
    }
}
