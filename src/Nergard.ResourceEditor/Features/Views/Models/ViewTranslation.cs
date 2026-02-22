using Nergard.ResourceEditor.Features.Shared.Models;

namespace Nergard.ResourceEditor.Features.Views.Models;

public class ViewTranslation
{
    public string FileName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<ViewSection> Sections { get; set; } = [];

    public bool StructureChanged { get; set; }

    public bool HasChanges => StructureChanged || Sections.Any(s => s.Entries.Any(e => e.Value.IsDirty));

    public void MarkAsClean()
    {
        StructureChanged = false;
        foreach (var section in Sections)
            foreach (var entry in section.Entries)
                entry.Value.MarkAsClean();
    }
}

public class ViewSection
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<ViewEntry> Entries { get; set; } = [];
}

public class ViewEntry
{
    public string Key { get; set; } = string.Empty;
    public TranslationEntry Value { get; set; } = new();
}
