using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using MudBlazor;
using Nergard.ResourceEditor.Features.ContentTypes.Models;
using Nergard.ResourceEditor.Features.ContentTypes.Services;
using Nergard.ResourceEditor.Features.Shared.Helpers;
using Nergard.ResourceEditor.Features.Shared.Models;
using Nergard.ResourceEditor.Features.Shared.Services;
using Nergard.ResourceEditor.Features.Tabs.Models;
using Nergard.ResourceEditor.Features.Tabs.Services;

namespace Nergard.ResourceEditor.Features.Languages.Components;

public partial class LanguageOverview
{
    [Inject] private IContentTypeLocalizationService ContentTypeService { get; set; } = default!;
    [Inject] private ITabLocalizationService TabService { get; set; } = default!;
    [Inject] private ITranslationStatusEvaluator StatusEvaluator { get; set; } = default!;
    [Inject] private ISharedPropertyService SharedPropertyService { get; set; } = default!;
    [Inject] private ITranslationStatusService TranslationStatusService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ITranslationService TranslationService { get; set; } = default!;
    [Inject] private IOptions<ResourceEditorOptions> Options { get; set; } = default!;
    [Inject] private ILanguageService LanguageService { get; set; } = default!;

    [Parameter, EditorRequired] public LanguageInfo Language { get; set; } = default!;
    [Parameter] public IReadOnlyList<ContentTypeInfo> PageTypes { get; set; } = [];
    [Parameter] public IReadOnlyList<ContentTypeInfo> BlockTypes { get; set; } = [];
    [Parameter] public IReadOnlyList<ContentTypeInfo> MediaTypes { get; set; } = [];
    [Parameter] public IReadOnlyList<TabInfo> Tabs { get; set; } = [];
    [Parameter] public EventCallback<(string Type, string Name)> OnNavigateToItem { get; set; }
    [Parameter] public EventCallback OnSaved { get; set; }

    private List<ContentTypeTranslationEntry> _entries = [];
    private List<PropertyRow> _propertyRows = [];
    private List<TabRow> _tabRows = [];
    private bool _showAllTypes = true;
    private bool _showAllProperties = true;
    private string _selectedTypeCategory = "All";
    private string _selectedPropertyCategory = "All";
    private bool _typesExpanded = true;
    private bool _propertiesExpanded = true;
    private bool _tabsExpanded = true;
    private bool _isSaving;
    private bool _isLoading = true;
    private bool _isTabLoading;
    private bool _isTranslating;
    private bool _translationEnabled;

    private List<ContentTypeTranslationEntry> FilteredEntries
    {
        get
        {
            var result = _entries.AsEnumerable();
            if (!_showAllTypes)
                result = result.Where(e => e.Status != Color.Success);
            if (_selectedTypeCategory != "All")
                result = result.Where(e => GetCategoryLabel(e.TypeInfo.Category) == _selectedTypeCategory);
            return result.ToList();
        }
    }

    private List<PropertyRow> FilteredPropertyRows
    {
        get
        {
            var result = _propertyRows.AsEnumerable();
            if (!_showAllProperties)
                result = result.Where(r => r.Status != Color.Success);
            if (_selectedPropertyCategory != "All")
                result = result.Where(r => r.Category == _selectedPropertyCategory);
            return result.ToList();
        }
    }

    private bool HasChanges => _entries.Any(e => e.Translation.HasChanges)
                              || _tabRows.Any(t => t.Translation.HasChanges);

    private int IncompleteTypeCount => _entries.Count(e => e.Status != Color.Success);
    private int IncompletePropertyCount => _propertyRows.Count(r => r.Status != Color.Success);
    private int IncompleteTabCount => _tabRows.Count(t => t.Status != Color.Success);

    private int TotalTypeCount => _entries.Count;
    private int CompleteTypeCount => _entries.Count - IncompleteTypeCount;
    private int TotalPropertyCount => _propertyRows.Count;
    private int CompletePropertyCount => _propertyRows.Count - IncompletePropertyCount;
    private int TotalTabCount => _tabRows.Count;
    private int CompleteTabCount => _tabRows.Count - IncompleteTabCount;

    private static double GetPercent(int complete, int total)
        => total == 0 ? 100 : Math.Round((double)complete / total * 100, 0);

    protected override async Task OnParametersSetAsync()
    {
        _isLoading = true;
        StateHasChanged();

        if (Options.Value.EnableAutomatedTranslation)
        {
            _translationEnabled = await TranslationService.IsAvailableAsync();
        }

        await Task.Run(LoadTranslations);
        _isLoading = false;
    }

    private void LoadTranslations()
    {
        var entries = new List<ContentTypeTranslationEntry>();
        var propertyRows = new List<PropertyRow>();

        var allTypes = PageTypes
            .Concat(BlockTypes)
            .Concat(MediaTypes)
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var typeInfo in allTypes)
        {
            var translation = ContentTypeService.GetTranslations(typeInfo.Name, typeInfo.Category);
            var result = StatusEvaluator.EvaluateContentType(translation, Language.Id);
            entries.Add(new ContentTypeTranslationEntry(typeInfo, translation, result.OverallColor));

            var category = GetCategoryLabel(typeInfo.Category);

            foreach (var prop in translation.Properties)
            {
                propertyRows.Add(new PropertyRow(
                    ContentTypeName: translation.ContentTypeName,
                    Category: category,
                    Property: prop,
                    Status: GetPropertyStatus(prop),
                    IsShared: SharedPropertyService.IsShared(prop.PropertyName)
                ));
            }
        }

        propertyRows = propertyRows
            .OrderBy(r => r.Status == Color.Success ? 1 : 0)
            .ThenBy(r => r.Property.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.ContentTypeName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var tabRows = new List<TabRow>();
        foreach (var tab in Tabs)
        {
            var tabTranslation = TabService.GetTranslations(tab.Name);
            tabRows.Add(new TabRow(tab, tabTranslation, GetTabStatus(tabTranslation)));
        }

        tabRows = tabRows
            .OrderBy(t => t.Status == Color.Success ? 1 : 0)
            .ThenBy(t => t.TabInfo.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _entries = entries;
        _propertyRows = propertyRows;
        _tabRows = tabRows;
    }

    private Color GetPropertyStatus(PropertyTranslation prop)
        => PropertyStatusHelper.GetPropertyStatus(prop, [Language.Id]);

    private string GetCategoryLabel(ContentTypeCategory category) => category switch
    {
        ContentTypeCategory.Page => "Page",
        ContentTypeCategory.Block => "Block",
        ContentTypeCategory.Media => "Media",
        _ => "Unknown"
    };

    private string GetSharedTooltip(string propertyName)
    {
        var types = SharedPropertyService.GetContentTypesForProperty(propertyName);
        return $"Shared property — used by: {string.Join(", ", types)}";
    }

    // Shared property syncing

    private string GetPropertyLabelValue(PropertyTranslation prop)
    {
        prop.Label.Values.TryGetValue(Language.Id, out var val);
        return val ?? "";
    }

    private void SetPropertyLabelValue(PropertyTranslation prop, string value)
    {
        prop.Label.Values[Language.Id] = value;

        if (SharedPropertyService.IsShared(prop.PropertyName))
        {
            foreach (var entry in _entries)
            {
                foreach (var other in entry.Translation.Properties)
                {
                    if (other != prop && other.PropertyName == prop.PropertyName)
                    {
                        other.Label.Values[Language.Id] = value;
                    }
                }
            }
        }
    }

    private string GetPropertyDescValue(PropertyTranslation prop)
    {
        prop.Description.Values.TryGetValue(Language.Id, out var val);
        return val ?? "";
    }

    private void SetPropertyDescValue(PropertyTranslation prop, string value)
    {
        prop.Description.Values[Language.Id] = value;

        if (SharedPropertyService.IsShared(prop.PropertyName))
        {
            foreach (var entry in _entries)
            {
                foreach (var other in entry.Translation.Properties)
                {
                    if (other != prop && other.PropertyName == prop.PropertyName)
                    {
                        other.Description.Values[Language.Id] = value;
                    }
                }
            }
        }
    }

    private async Task OnActiveTabChanged(int index)
    {
        _isTabLoading = true;
        StateHasChanged();
        await Task.Delay(1);
        _isTabLoading = false;
    }

    private Color GetTabStatus(TabTranslation tab)
    {
        if (tab.DisplayName.Values.TryGetValue(Language.Id, out var val)
            && !string.IsNullOrWhiteSpace(val)
            && !val.Equals(tab.TabName, StringComparison.OrdinalIgnoreCase))
            return Color.Success;
        return Color.Error;
    }

    private void ToggleTypesExpanded()
    {
        _typesExpanded = !_typesExpanded;
    }

    private void TogglePropertiesExpanded()
    {
        _propertiesExpanded = !_propertiesExpanded;
    }

    private void ToggleTabsExpanded()
    {
        _tabsExpanded = !_tabsExpanded;
    }

    private async Task TranslateIncompleteAsync()
    {
        if (!_translationEnabled || Language.IsDefault)
            return;

        _isTranslating = true;
        StateHasChanged();

        try
        {
            var defaultLanguage = LanguageService.GetDefaultLanguage();
            var itemsToTranslate = new List<(BatchTranslationItem Item, Action<string> ApplyResult)>();

            // Collect empty fields from content type names and descriptions
            foreach (var entry in _entries)
            {
                var nameSource = entry.Translation.Name.Values.GetValueOrDefault(defaultLanguage.Id, "");
                var nameTarget = entry.Translation.Name.Values.GetValueOrDefault(Language.Id, "");
                if (!string.IsNullOrWhiteSpace(nameSource) && string.IsNullOrWhiteSpace(nameTarget))
                {
                    var key = $"/contenttypes/{entry.Translation.ContentTypeName.ToLowerInvariant()}/name";
                    var context = new TranslationContext(key, entry.Translation.ContentTypeName, TranslationFieldType.ContentTypeName);
                    var translation = entry.Translation; // capture for closure
                    itemsToTranslate.Add((
                        new BatchTranslationItem(nameSource, context),
                        value => translation.Name.Values[Language.Id] = value));
                }

                var descSource = entry.Translation.Description.Values.GetValueOrDefault(defaultLanguage.Id, "");
                var descTarget = entry.Translation.Description.Values.GetValueOrDefault(Language.Id, "");
                if (!string.IsNullOrWhiteSpace(descSource) && string.IsNullOrWhiteSpace(descTarget))
                {
                    var key = $"/contenttypes/{entry.Translation.ContentTypeName.ToLowerInvariant()}/description";
                    var context = new TranslationContext(key, entry.Translation.ContentTypeName, TranslationFieldType.ContentTypeDescription);
                    var translation = entry.Translation;
                    itemsToTranslate.Add((
                        new BatchTranslationItem(descSource, context),
                        value => translation.Description.Values[Language.Id] = value));
                }
            }

            // Collect empty property fields (use shared-property-aware setters)
            foreach (var row in _propertyRows)
            {
                var labelSource = row.Property.Label.Values.GetValueOrDefault(defaultLanguage.Id, "");
                var labelTarget = row.Property.Label.Values.GetValueOrDefault(Language.Id, "");
                if (!string.IsNullOrWhiteSpace(labelSource) && string.IsNullOrWhiteSpace(labelTarget))
                {
                    var key = $"/contenttypes/icontentdata/properties/{row.Property.PropertyName.ToLowerInvariant()}/caption";
                    var context = new TranslationContext(key, row.ContentTypeName, TranslationFieldType.PropertyLabel);
                    var prop = row.Property;
                    itemsToTranslate.Add((
                        new BatchTranslationItem(labelSource, context),
                        value => SetPropertyLabelValue(prop, value)));
                }

                var descSource = row.Property.Description.Values.GetValueOrDefault(defaultLanguage.Id, "");
                var descTarget = row.Property.Description.Values.GetValueOrDefault(Language.Id, "");
                if (!string.IsNullOrWhiteSpace(descSource) && string.IsNullOrWhiteSpace(descTarget))
                {
                    var key = $"/contenttypes/icontentdata/properties/{row.Property.PropertyName.ToLowerInvariant()}/help";
                    var context = new TranslationContext(key, row.ContentTypeName, TranslationFieldType.PropertyDescription);
                    var prop = row.Property;
                    itemsToTranslate.Add((
                        new BatchTranslationItem(descSource, context),
                        value => SetPropertyDescValue(prop, value)));
                }
            }

            // Collect empty tab fields
            foreach (var tab in _tabRows)
            {
                var displaySource = tab.Translation.DisplayName.Values.GetValueOrDefault(defaultLanguage.Id, "");
                var displayTarget = tab.Translation.DisplayName.Values.GetValueOrDefault(Language.Id, "");
                if (!string.IsNullOrWhiteSpace(displaySource) && string.IsNullOrWhiteSpace(displayTarget))
                {
                    var normalizedName = tab.Translation.TabName.ToLowerInvariant().Replace(" ", "");
                    var key = $"/propertygroupsettings/{normalizedName}/caption";
                    var context = new TranslationContext(key, $"Tab: {tab.Translation.TabName}", TranslationFieldType.TabDisplayName);
                    var translation = tab.Translation;
                    itemsToTranslate.Add((
                        new BatchTranslationItem(displaySource, context),
                        value => translation.DisplayName.Values[Language.Id] = value));
                }
            }

            if (itemsToTranslate.Count == 0)
            {
                Snackbar.Add("No incomplete fields to translate", Severity.Info);
                return;
            }

            // Batch translate in chunks of 50
            const int batchSize = 50;
            var totalTranslated = 0;

            for (var i = 0; i < itemsToTranslate.Count; i += batchSize)
            {
                var batch = itemsToTranslate.Skip(i).Take(batchSize).ToList();
                var batchRequest = new BatchTranslationRequest(
                    batch.Select(b => b.Item).ToList(),
                    defaultLanguage.Id,
                    Language.Id);

                var result = await TranslationService.TranslateBatchAsync(batchRequest);

                if (result.Success)
                {
                    for (var j = 0; j < result.Results.Count; j++)
                    {
                        if (result.Results[j].Success && !string.IsNullOrWhiteSpace(result.Results[j].TranslatedText))
                        {
                            batch[j].ApplyResult(result.Results[j].TranslatedText);
                            totalTranslated++;
                        }
                    }
                }
            }

            if (totalTranslated > 0)
                Snackbar.Add($"Translated {totalTranslated} field(s)", Severity.Success);
            else
                Snackbar.Add("No fields were translated", Severity.Info);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Translation error: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isTranslating = false;
            StateHasChanged();
        }
    }

    private async Task HandleSave()
    {
        _isSaving = true;
        StateHasChanged();

        try
        {
            await Task.Run(() =>
            {
                foreach (var entry in _entries.Where(e => e.Translation.HasChanges))
                {
                    ContentTypeService.SaveTranslations(entry.Translation);
                }
                foreach (var tab in _tabRows.Where(t => t.Translation.HasChanges))
                {
                    TabService.SaveTranslations(tab.Translation);
                }
            });

            Snackbar.Add("Changes saved successfully", Severity.Success);
            TranslationStatusService.Invalidate();
            await OnSaved.InvokeAsync();

            LoadTranslations();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error saving changes: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSaving = false;
            StateHasChanged();
        }
    }

    private async Task HandleReset()
    {
        _isLoading = true;
        StateHasChanged();
        await Task.Run(LoadTranslations);
        _isLoading = false;
    }

    private record ContentTypeTranslationEntry(
        ContentTypeInfo TypeInfo,
        ContentTypeTranslation Translation,
        Color Status);

    private record PropertyRow(
        string ContentTypeName,
        string Category,
        PropertyTranslation Property,
        Color Status,
        bool IsShared);

    private record TabRow(
        TabInfo TabInfo,
        TabTranslation Translation,
        Color Status);
}
