using Nergard.ResourceEditor.Features.Shared.Models;

namespace Nergard.ResourceEditor.Features.EditorHints.Models;

public class EditorHintTranslation
{
    public List<EditorHintSection> Sections { get; set; } = [];

    public bool HasChanges => Sections.Any(s => s.Entries.Any(e => e.Value.IsDirty));

    public void MarkAsClean()
    {
        foreach (var section in Sections)
            foreach (var entry in section.Entries)
                entry.Value.MarkAsClean();
    }
}

public class EditorHintSection
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<EditorHintEntry> Entries { get; set; } = [];
}

public class EditorHintEntry
{
    public string ParentKey { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public TranslationEntry Value { get; set; } = new();
}
