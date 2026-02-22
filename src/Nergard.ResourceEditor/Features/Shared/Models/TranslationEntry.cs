namespace Nergard.ResourceEditor.Features.Shared.Models;

public class TranslationEntry
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Dictionary<string, string> Values { get; set; } = new();
    public Dictionary<string, string> OriginalValues { get; set; } = new();

    public bool IsDirty
    {
        get
        {
            if (Values.Count != OriginalValues.Count)
                return true;

            foreach (var kvp in Values)
            {
                if (!OriginalValues.TryGetValue(kvp.Key, out var original) || original != kvp.Value)
                    return true;
            }

            return false;
        }
    }

    public void MarkAsClean()
    {
        OriginalValues = new Dictionary<string, string>(Values);
    }
}
