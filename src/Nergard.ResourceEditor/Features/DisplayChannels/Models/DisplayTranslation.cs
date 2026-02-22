using Nergard.ResourceEditor.Features.Shared.Models;

namespace Nergard.ResourceEditor.Features.DisplayChannels.Models;

public class DisplayTranslation
{
    public List<NamedTranslationEntry> Channels { get; set; } = [];
    public List<NamedTranslationEntry> Options { get; set; } = [];
    public List<NamedTranslationEntry> Resolutions { get; set; } = [];

    public bool HasChanges => Channels.Any(c => c.Value.IsDirty)
        || Options.Any(o => o.Value.IsDirty)
        || Resolutions.Any(r => r.Value.IsDirty);

    public void MarkAsClean()
    {
        foreach (var c in Channels) c.Value.MarkAsClean();
        foreach (var o in Options) o.Value.MarkAsClean();
        foreach (var r in Resolutions) r.Value.MarkAsClean();
    }
}

public class NamedTranslationEntry
{
    public string Key { get; set; } = string.Empty;
    public string DisplayLabel { get; set; } = string.Empty;
    public TranslationEntry Value { get; set; } = new();
}
