using Nergard.ResourceEditor.Features.EditorHints.Models;
using Nergard.ResourceEditor.Features.Shared.Models;

namespace Nergard.ResourceEditor.Features.EditorHints.Components;

public partial class EditorHintEditor
{
    private readonly HashSet<string> _expandedSections = new();

    private bool IsSectionExpanded(string sectionName) => _expandedSections.Contains(sectionName);

    private void ToggleSectionExpanded(string sectionName)
    {
        if (!_expandedSections.Remove(sectionName))
            _expandedSections.Add(sectionName);
    }

    protected override List<TranslatableEntry> GetTranslatableEntries()
    {
        var entries = new List<TranslatableEntry>();

        foreach (var section in Translation.Sections)
        {
            foreach (var entry in section.Entries)
            {
                entries.Add(new(
                    entry.Value,
                    GetHintKey(section, entry),
                    $"Hint: {section.Name}/{entry.Key}",
                    TranslationFieldType.EditorHintValue));
            }
        }

        return entries;
    }

    private static string GetEntryLabel(EditorHintEntry entry)
    {
        return string.IsNullOrEmpty(entry.ParentKey)
            ? entry.Key
            : $"{entry.ParentKey} / {entry.Key}";
    }

    private static string GetHintKey(EditorHintSection section, EditorHintEntry entry)
    {
        var basePath = $"/{section.Name.ToLowerInvariant()}";

        if (!string.IsNullOrEmpty(entry.ParentKey))
        {
            basePath += $"/{entry.ParentKey.ToLowerInvariant()}";
        }

        return $"{basePath}/{entry.Key.ToLowerInvariant()}";
    }
}
