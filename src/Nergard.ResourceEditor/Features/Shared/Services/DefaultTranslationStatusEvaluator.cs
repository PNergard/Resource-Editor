using Microsoft.Extensions.Options;
using MudBlazor;
using Nergard.ResourceEditor.Features.ContentTypes.Models;
using Nergard.ResourceEditor.Features.Shared.Models;
using Nergard.ResourceEditor.Features.Tabs.Models;

namespace Nergard.ResourceEditor.Features.Shared.Services;

public class DefaultTranslationStatusEvaluator : ITranslationStatusEvaluator
{
    private readonly TranslationStatusOptions _options;

    public DefaultTranslationStatusEvaluator(IOptions<TranslationStatusOptions> options)
    {
        _options = options.Value;
    }

    public TranslationStatusResult EvaluateContentType(ContentTypeTranslation translation, string languageId)
    {
        // Content type level: name + description
        var ctTotal = 2;
        var ctTranslated = 0;

        if (IsTranslatedName(translation.Name.Values.GetValueOrDefault(languageId, ""), translation.ContentTypeName))
            ctTranslated++;
        if (IsTranslatedDescription(translation.Description.Values.GetValueOrDefault(languageId, "")))
            ctTranslated++;

        // Property level: label + description per property
        var propTotal = translation.Properties.Count * 2;
        var propTranslated = 0;
        var propItemsTotal = translation.Properties.Count;
        var propItemsComplete = 0;

        foreach (var prop in translation.Properties)
        {
            var labelOk = IsTranslatedName(prop.Label.Values.GetValueOrDefault(languageId, ""), prop.PropertyName);
            var descOk = IsTranslatedDescription(prop.Description.Values.GetValueOrDefault(languageId, ""));

            if (labelOk) propTranslated++;
            if (descOk) propTranslated++;
            if (labelOk && descOk) propItemsComplete++;
        }

        var contentTypeColor = GetColorFromPercentage(ctTotal, ctTranslated);
        var propertyColor = propTotal > 0 ? GetColorFromPercentage(propTotal, propTranslated) : Color.Success;
        var overallColor = WorseOf(contentTypeColor, propertyColor);

        return new TranslationStatusResult
        {
            ContentTypeColor = contentTypeColor,
            PropertyTotal = propTotal,
            PropertyComplete = propTranslated,
            PropertyColor = propertyColor,
            OverallColor = overallColor,
            PropertyItemsTotal = propItemsTotal,
            PropertyItemsComplete = propItemsComplete
        };
    }

    public TabStatusResult EvaluateTab(TabTranslation translation, string languageId)
    {
        var value = translation.DisplayName.Values.GetValueOrDefault(languageId, "");
        var isTranslated = IsTranslatedName(value, translation.TabName);

        return new TabStatusResult
        {
            StatusColor = isTranslated ? Color.Success : Color.Error
        };
    }

    private static bool IsTranslatedName(string value, string originalName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return !value.Equals(originalName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTranslatedDescription(string value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    private Color GetColorFromPercentage(int total, int translated)
    {
        if (total == 0)
            return Color.Success;

        var percentage = (double)translated / total;

        if (percentage >= _options.GreenThreshold)
            return Color.Success;

        if (percentage >= _options.YellowThreshold)
            return Color.Warning;

        return Color.Error;
    }

    private static Color WorseOf(Color a, Color b)
    {
        return ColorRank(a) > ColorRank(b) ? a : b;
    }

    private static int ColorRank(Color c) => c switch
    {
        Color.Error => 2,
        Color.Warning => 1,
        _ => 0
    };
}
