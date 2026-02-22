using Microsoft.AspNetCore.Components;
using Nergard.ResourceEditor.Features.Shared.Models;
using Nergard.ResourceEditor.Features.Shared.Services;
using Nergard.ResourceEditor.Features.Views.Models;
using Nergard.ResourceEditor.Features.Views.Services;

namespace Nergard.ResourceEditor.Features.Dashboard.Components;

public partial class Dashboard
{
    [Inject] private ITranslationStatusService StatusService { get; set; } = default!;
    [Inject] private IViewLocalizationService ViewService { get; set; } = default!;
    [Inject] private ILanguageService LanguageService { get; set; } = default!;

    [Parameter] public EventCallback<string> OnLanguageSelected { get; set; }
    [Parameter] public EventCallback<string> OnViewSelected { get; set; }

    private IReadOnlyList<LanguageStatusSummary> _summaries = [];
    private List<ViewStatusSummary> _viewSummaries = [];

    protected override void OnInitialized()
    {
        _summaries = StatusService.GetLanguageSummaries();
        _viewSummaries = ComputeViewSummaries();
    }

    private List<ViewStatusSummary> ComputeViewSummaries()
    {
        var viewFiles = ViewService.GetViewFiles();
        var languages = LanguageService.GetLanguages();
        var result = new List<ViewStatusSummary>();

        foreach (var file in viewFiles)
        {
            var translation = ViewService.GetTranslations(file.FileName);
            var totalKeys = translation.Sections.Sum(s => s.Entries.Count);

            var langStatuses = new List<ViewLanguageStatus>();
            foreach (var lang in languages)
            {
                var completedKeys = translation.Sections
                    .SelectMany(s => s.Entries)
                    .Count(e => e.Value.Values.TryGetValue(lang.Id, out var val) && !string.IsNullOrEmpty(val));

                langStatuses.Add(new ViewLanguageStatus(lang.Id, lang.Name, lang.IsDefault, completedKeys, totalKeys));
            }

            result.Add(new ViewStatusSummary(file.FileName, file.DisplayName, totalKeys, langStatuses));
        }

        return result;
    }

    private static double GetPercent(int complete, int total)
        => total == 0 ? 100 : Math.Round((double)complete / total * 100, 0);

    private static double GetLanguageOverallPercent(LanguageStatusSummary s)
    {
        var totalItems = s.ContentTypesTotal + s.PropertiesTotal + s.TabsTotal;
        var completeItems = s.ContentTypesComplete + s.PropertiesComplete + s.TabsComplete;
        return totalItems == 0 ? 100 : Math.Round((double)completeItems / totalItems * 100, 0);
    }

    private static double GetViewOverallPercent(ViewStatusSummary v)
    {
        var totalItems = v.Languages.Sum(l => l.TotalKeys);
        var completeItems = v.Languages.Sum(l => l.CompletedKeys);
        return totalItems == 0 ? 100 : Math.Round((double)completeItems / totalItems * 100, 0);
    }

    private record ViewStatusSummary(string FileName, string DisplayName, int TotalKeys, List<ViewLanguageStatus> Languages);
    private record ViewLanguageStatus(string LanguageId, string LanguageName, bool IsDefault, int CompletedKeys, int TotalKeys);
}
