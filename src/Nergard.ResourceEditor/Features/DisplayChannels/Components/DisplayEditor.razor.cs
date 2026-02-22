using Nergard.ResourceEditor.Features.Shared.Models;

namespace Nergard.ResourceEditor.Features.DisplayChannels.Components;

public partial class DisplayEditor
{
    private bool _channelsExpanded;
    private bool _optionsExpanded;
    private bool _resolutionsExpanded;

    private void ToggleChannelsExpanded() => _channelsExpanded = !_channelsExpanded;
    private void ToggleOptionsExpanded() => _optionsExpanded = !_optionsExpanded;
    private void ToggleResolutionsExpanded() => _resolutionsExpanded = !_resolutionsExpanded;

    protected override List<TranslatableEntry> GetTranslatableEntries()
    {
        var entries = new List<TranslatableEntry>();

        foreach (var channel in Translation.Channels)
            entries.Add(new(channel.Value, GetChannelKey(channel.Key), $"Channel: {channel.Key}", TranslationFieldType.DisplayChannel));

        foreach (var option in Translation.Options)
            entries.Add(new(option.Value, GetOptionKey(option.Key), $"Option: {option.Key}", TranslationFieldType.DisplayOption));

        foreach (var resolution in Translation.Resolutions)
            entries.Add(new(resolution.Value, GetResolutionKey(resolution.Key), $"Resolution: {resolution.Key}", TranslationFieldType.DisplayResolution));

        return entries;
    }

    private static string GetChannelKey(string channelKey)
    {
        return $"/displaychannels/{channelKey.ToLowerInvariant()}";
    }

    private static string GetOptionKey(string optionKey)
    {
        return $"/displayoptions/{optionKey.ToLowerInvariant()}";
    }

    private static string GetResolutionKey(string resolutionKey)
    {
        return $"/displayresolutions/{resolutionKey.ToLowerInvariant()}";
    }
}
