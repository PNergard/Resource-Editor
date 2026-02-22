using MudBlazor;
using Nergard.ResourceEditor.Features.ContentTypes.Models;
using Nergard.ResourceEditor.Features.ContentTypes.Services;
using Nergard.ResourceEditor.Features.Shared.Models;
using Nergard.ResourceEditor.Features.Tabs.Models;
using Nergard.ResourceEditor.Features.Tabs.Services;

namespace Nergard.ResourceEditor.Features.Shared.Services;

public class TranslationStatusService(
    ILanguageService languageService,
    IContentTypeLocalizationService contentTypeService,
    ITabLocalizationService tabService,
    ITranslationStatusEvaluator evaluator) : ITranslationStatusService
{
    private IReadOnlyList<LanguageStatusSummary>? _cache;

    public IReadOnlyList<LanguageStatusSummary> GetLanguageSummaries()
    {
        return _cache ??= ComputeSummaries();
    }

    public void Invalidate()
    {
        _cache = null;
    }

    private IReadOnlyList<LanguageStatusSummary> ComputeSummaries()
    {
        var languages = languageService.GetLanguages();

        var contentTypeTranslations = new List<ContentTypeTranslation>();
        foreach (var ct in contentTypeService.GetPageTypes())
            contentTypeTranslations.Add(contentTypeService.GetTranslations(ct.Name, ContentTypeCategory.Page));
        foreach (var ct in contentTypeService.GetBlockTypes())
            contentTypeTranslations.Add(contentTypeService.GetTranslations(ct.Name, ContentTypeCategory.Block));
        foreach (var ct in contentTypeService.GetMediaTypes())
            contentTypeTranslations.Add(contentTypeService.GetTranslations(ct.Name, ContentTypeCategory.Media));

        var tabs = tabService.GetTabs();
        var tabTranslations = tabs.Select(t => tabService.GetTranslations(t.Name)).ToList();

        return languages.Select(lang => BuildSummary(lang, contentTypeTranslations, tabTranslations)).ToList();
    }

    private LanguageStatusSummary BuildSummary(
        LanguageInfo lang,
        List<ContentTypeTranslation> contentTypes,
        List<TabTranslation> tabTranslations)
    {
        var contentTypesTotal = contentTypes.Count;
        var contentTypesComplete = 0;
        var propsTotal = 0;
        var propsComplete = 0;

        foreach (var ct in contentTypes)
        {
            var result = evaluator.EvaluateContentType(ct, lang.Id);
            if (result.ContentTypeColor == Color.Success)
                contentTypesComplete++;
            propsTotal += result.PropertyItemsTotal;
            propsComplete += result.PropertyItemsComplete;
        }

        var tabsTotal = tabTranslations.Count;
        var tabsComplete = 0;

        foreach (var tab in tabTranslations)
        {
            var result = evaluator.EvaluateTab(tab, lang.Id);
            if (result.StatusColor == Color.Success)
                tabsComplete++;
        }

        return new LanguageStatusSummary
        {
            LanguageId = lang.Id,
            LanguageName = lang.Name,
            IsDefault = lang.IsDefault,
            ContentTypesTotal = contentTypesTotal,
            ContentTypesComplete = contentTypesComplete,
            PropertiesTotal = propsTotal,
            PropertiesComplete = propsComplete,
            TabsTotal = tabsTotal,
            TabsComplete = tabsComplete
        };
    }
}
