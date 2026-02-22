using System.Text;
using EPiServer.Framework.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using MudBlazor;
using Nergard.ResourceEditor.Features.ContentTypes.Models;
using Nergard.ResourceEditor.Features.ContentTypes.Services;
using Nergard.ResourceEditor.Features.Overrides.Models;
using Nergard.ResourceEditor.Features.Overrides.Services;
using Nergard.ResourceEditor.Features.Shared.Models;
using Nergard.ResourceEditor.Features.Shared.Services;

namespace Nergard.ResourceEditor.Features.Overrides.Components;

public partial class OverrideManager
{
    [Inject] private IOverrideService OverrideService { get; set; } = default!;
    [Inject] private LocalizationService LocalizationService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IContentTypeLocalizationService ContentTypeService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private IOptions<ResourceEditorOptions> Options { get; set; } = default!;
    [Inject] private ITranslationService TranslationService { get; set; } = default!;
    [Inject] private ILanguageService LanguageService { get; set; } = default!;

    [Parameter] public IReadOnlyList<LanguageInfo> Languages { get; set; } = [];

    private bool _isLoading;
    private bool _isSaving;
    private List<LocalizationOverrideEntity> _overrides = [];

    // Filters
    private string _languageFilter = string.Empty;
    private string _searchFilter = string.Empty;

    // Edit dialog state
    private bool _showEditDialog;
    private bool _isEditing;
    private MudForm? _form;
    private OverrideEditModel _editModel = new();

    // Add/Edit dialog - Content Type Selection
    private IReadOnlyList<ContentTypeInfo> _allContentTypes = [];
    private ContentTypeInfo? _selectedContentType;
    private IReadOnlyList<PropertyOption> _contentTypeProperties = [];
    private PropertyOption? _selectedProperty;
    private string _generatedKey = string.Empty;

    // Delete dialog state
    private bool _showDeleteDialog;
    private GroupedOverride? _deleteTarget;

    // Clear all dialog state
    private bool _showClearAllDialog;
    private string _clearAllConfirmText = string.Empty;

    // Import dialog state
    private bool _showImportDialog;
    private IBrowserFile? _importFile;
    private string _importStatus = string.Empty;

    // Automated translation multi-language state
    private bool _isTranslationAvailable;
    private string _defaultLanguageId = string.Empty;
    private List<AddDialogLanguageRow> _addDialogLanguageRows = [];
    private bool _isTranslatingAll;

    private bool EnableFileSaving => Options.Value.EnableFileSaving;

    /// <summary>
    /// Groups overrides by property + language, combining caption and help text into one row
    /// </summary>
    private IEnumerable<GroupedOverride> FilteredGroupedOverrides
    {
        get
        {
            var result = _overrides.AsEnumerable();

            if (!string.IsNullOrEmpty(_languageFilter))
            {
                result = result.Where(o => o.Language.Equals(_languageFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(_searchFilter))
            {
                result = result.Where(o =>
                    o.Key.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) ||
                    o.OverrideValue.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) ||
                    (o.ContentTypeName?.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            // Group by base key (without /caption or /help suffix) and language
            var grouped = result
                .GroupBy(o => new { BaseKey = GetBaseKey(o.Key), o.Language })
                .Select(g =>
                {
                    var captionOverride = g.FirstOrDefault(o => o.Key.EndsWith("/caption", StringComparison.OrdinalIgnoreCase));
                    var helpOverride = g.FirstOrDefault(o => o.Key.EndsWith("/help", StringComparison.OrdinalIgnoreCase));
                    var anyOverride = captionOverride ?? helpOverride ?? g.First();

                    return new GroupedOverride
                    {
                        PropertyName = Services.OverrideService.ExtractPropertyName(anyOverride.Key),
                        ContentTypeName = anyOverride.ContentTypeName,
                        Language = anyOverride.Language,
                        CaptionValue = captionOverride?.OverrideValue,
                        HelpTextValue = helpOverride?.OverrideValue,
                        CaptionEntity = captionOverride,
                        HelpTextEntity = helpOverride,
                        ModifiedDate = new[] { captionOverride?.ModifiedDate, helpOverride?.ModifiedDate }
                            .Where(d => d.HasValue)
                            .Max() ?? DateTime.MinValue,
                        ModifiedBy = (captionOverride?.ModifiedDate > helpOverride?.ModifiedDate
                            ? captionOverride?.ModifiedBy
                            : helpOverride?.ModifiedBy) ?? anyOverride.ModifiedBy
                    };
                })
                .OrderBy(g => g.ContentTypeName)
                .ThenBy(g => g.PropertyName)
                .ThenBy(g => g.Language);

            return grouped;
        }
    }

    private static string GetBaseKey(string key)
    {
        if (key.EndsWith("/caption", StringComparison.OrdinalIgnoreCase))
            return key[..^8];
        if (key.EndsWith("/help", StringComparison.OrdinalIgnoreCase))
            return key[..^5];
        return key;
    }

    protected override async Task OnInitializedAsync()
    {
        LoadOverrides();
        LoadContentTypes();

        if (Options.Value.EnableAutomatedTranslation)
        {
            _defaultLanguageId = LanguageService.GetDefaultLanguage().Id;
            _isTranslationAvailable = await TranslationService.IsAvailableAsync();
        }
    }

    private bool ShowAddDialogMultiLanguage => !_isEditing && _isTranslationAvailable && Options.Value.EnableAutomatedTranslation;

    private void LoadContentTypes()
    {
        var pages = ContentTypeService.GetPageTypes();
        var blocks = ContentTypeService.GetBlockTypes();
        var media = ContentTypeService.GetMediaTypes();
        _allContentTypes = pages.Concat(blocks).Concat(media).OrderBy(ct => ct.DisplayName).ToList();
    }

    private void LoadOverrides()
    {
        _isLoading = true;
        try
        {
            _overrides = OverrideService.GetAllOverrides().ToList();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading overrides: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void OnLanguageFilterChanged(string value)
    {
        _languageFilter = value;
    }

    private void OpenAddDialog()
    {
        _isEditing = false;
        _selectedContentType = null;
        _selectedProperty = null;
        _contentTypeProperties = [];
        _generatedKey = string.Empty;
        _editModel = new OverrideEditModel
        {
            Language = Languages.FirstOrDefault(l => l.IsDefault)?.Id ?? Languages.FirstOrDefault()?.Id ?? ""
        };

        if (ShowAddDialogMultiLanguage)
        {
            _addDialogLanguageRows = Languages
                .Where(l => !l.Id.Equals(_editModel.Language, StringComparison.OrdinalIgnoreCase))
                .Select(l => new AddDialogLanguageRow { Language = l })
                .ToList();
        }
        else
        {
            _addDialogLanguageRows = [];
        }

        _showEditDialog = true;
    }

    private void OpenEditDialog(GroupedOverride grouped)
    {
        _isEditing = true;

        // Find content type by name
        _selectedContentType = _allContentTypes.FirstOrDefault(ct =>
            ct.Name.Equals(grouped.ContentTypeName, StringComparison.OrdinalIgnoreCase));

        // Load properties for the content type
        if (_selectedContentType != null)
        {
            var translation = ContentTypeService.GetTranslations(_selectedContentType.Name, _selectedContentType.Category);
            _contentTypeProperties = translation.Properties
                .Select(p => new PropertyOption(p.PropertyName, p.PropertyName))
                .OrderBy(p => p.Name)
                .ToList();

            // Find the matching property
            _selectedProperty = _contentTypeProperties.FirstOrDefault(p =>
                p.Name.Equals(grouped.PropertyName, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            // Content type not found - create a placeholder
            _contentTypeProperties = [new PropertyOption(grouped.PropertyName, grouped.PropertyName)];
            _selectedProperty = _contentTypeProperties.First();
        }

        _editModel = new OverrideEditModel
        {
            Language = grouped.Language,
            CaptionValue = grouped.CaptionValue ?? string.Empty,
            HelpTextValue = grouped.HelpTextValue ?? string.Empty,
            ContentTypeName = grouped.ContentTypeName
        };

        UpdateGeneratedKey();
        _showEditDialog = true;
    }

    private void CloseEditDialog()
    {
        _showEditDialog = false;
        _editModel = new();
        _selectedContentType = null;
        _selectedProperty = null;
        _contentTypeProperties = [];
        _generatedKey = string.Empty;
    }

    private void OnContentTypeSelected(ContentTypeInfo? contentType)
    {
        _selectedContentType = contentType;
        _selectedProperty = null;
        _contentTypeProperties = [];

        if (contentType != null)
        {
            var translation = ContentTypeService.GetTranslations(contentType.Name, contentType.Category);
            _contentTypeProperties = translation.Properties
                .Select(p => new PropertyOption(p.PropertyName, p.PropertyName))
                .OrderBy(p => p.Name)
                .ToList();
        }

        UpdateGeneratedKey();
    }

    private void OnPropertySelected(PropertyOption? prop)
    {
        _selectedProperty = prop;
        UpdateGeneratedKey();
        PrePopulateExistingOverride();
    }

    private void UpdateGeneratedKey()
    {
        if (_selectedProperty == null)
        {
            _generatedKey = string.Empty;
            return;
        }
        _generatedKey = $"/contenttypes/icontentdata/properties/{_selectedProperty.Name.ToLowerInvariant()}/[caption|help]";
    }

    private void OnLanguageSelected(string language)
    {
        _editModel.Language = language;
        PrePopulateExistingOverride();
    }

    private void PrePopulateExistingOverride()
    {
        if (_isEditing || _selectedProperty == null || string.IsNullOrEmpty(_editModel.Language))
            return;

        var baseKey = $"/contenttypes/icontentdata/properties/{_selectedProperty.Name.ToLowerInvariant()}";
        var captionValue = OverrideService.GetOverride($"{baseKey}/caption", _editModel.Language);
        var helpValue = OverrideService.GetOverride($"{baseKey}/help", _editModel.Language);

        if (!string.IsNullOrEmpty(captionValue))
            _editModel.CaptionValue = captionValue;
        if (!string.IsNullOrEmpty(helpValue))
            _editModel.HelpTextValue = helpValue;
    }

    private async Task SaveOverride()
    {
        if (_form != null)
        {
            await _form.Validate();
            if (!_form.IsValid)
                return;
        }

        _isSaving = true;
        StateHasChanged();

        try
        {
            if (_selectedProperty == null)
            {
                Snackbar.Add("Please select a property", Severity.Warning);
                return;
            }

            var baseKey = $"/contenttypes/icontentdata/properties/{_selectedProperty.Name.ToLowerInvariant()}";
            var count = 0;

            // Handle caption
            if (!string.IsNullOrWhiteSpace(_editModel.CaptionValue))
            {
                await Task.Run(() => OverrideService.SaveOverride(
                    $"{baseKey}/caption",
                    _editModel.Language,
                    _editModel.CaptionValue,
                    _selectedContentType?.Name));
                count++;
            }
            else if (_isEditing)
            {
                // If editing and caption is now empty, delete it
                await Task.Run(() => OverrideService.DeleteOverride($"{baseKey}/caption", _editModel.Language));
            }

            // Handle help text
            if (!string.IsNullOrWhiteSpace(_editModel.HelpTextValue))
            {
                await Task.Run(() => OverrideService.SaveOverride(
                    $"{baseKey}/help",
                    _editModel.Language,
                    _editModel.HelpTextValue,
                    _selectedContentType?.Name));
                count++;
            }
            else if (_isEditing)
            {
                // If editing and help text is now empty, delete it
                await Task.Run(() => OverrideService.DeleteOverride($"{baseKey}/help", _editModel.Language));
            }

            if (count == 0 && !_isEditing)
            {
                Snackbar.Add("Please enter at least a caption or help text value", Severity.Warning);
                return;
            }

            // Save multi-language rows (Add dialog only)
            if (!_isEditing && ShowAddDialogMultiLanguage)
            {
                foreach (var row in _addDialogLanguageRows)
                {
                    if (!string.IsNullOrWhiteSpace(row.CaptionValue))
                    {
                        await Task.Run(() => OverrideService.SaveOverride(
                            $"{baseKey}/caption",
                            row.Language.Id,
                            row.CaptionValue,
                            _selectedContentType?.Name));
                        count++;
                    }

                    if (!string.IsNullOrWhiteSpace(row.HelpTextValue))
                    {
                        await Task.Run(() => OverrideService.SaveOverride(
                            $"{baseKey}/help",
                            row.Language.Id,
                            row.HelpTextValue,
                            _selectedContentType?.Name));
                        count++;
                    }
                }
            }

            Snackbar.Add(_isEditing ? "Override updated successfully" : $"Created {count} override(s) successfully", Severity.Success);

            CloseEditDialog();
            LoadOverrides();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error saving override: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSaving = false;
            StateHasChanged();
        }
    }

    private void ConfirmDelete(GroupedOverride grouped)
    {
        _deleteTarget = grouped;
        _showDeleteDialog = true;
    }

    private async Task DeleteOverride()
    {
        if (_deleteTarget == null) return;

        try
        {
            var count = 0;

            // Delete caption override if exists
            if (_deleteTarget.CaptionEntity != null)
            {
                await Task.Run(() => OverrideService.DeleteOverride(
                    _deleteTarget.CaptionEntity.Key,
                    _deleteTarget.CaptionEntity.Language));
                count++;
            }

            // Delete help text override if exists
            if (_deleteTarget.HelpTextEntity != null)
            {
                await Task.Run(() => OverrideService.DeleteOverride(
                    _deleteTarget.HelpTextEntity.Key,
                    _deleteTarget.HelpTextEntity.Language));
                count++;
            }

            Snackbar.Add($"Deleted {count} override(s) successfully", Severity.Success);
            _showDeleteDialog = false;
            _deleteTarget = null;
            LoadOverrides();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error deleting override: {ex.Message}", Severity.Error);
        }
    }

    private async Task ClearAllOverrides()
    {
        if (_clearAllConfirmText != "DELETE")
        {
            Snackbar.Add("Please type DELETE to confirm", Severity.Warning);
            return;
        }

        try
        {
            var count = await Task.Run(() => OverrideService.DeleteAllOverrides());
            Snackbar.Add($"Deleted {count} override(s)", Severity.Success);
            _showClearAllDialog = false;
            _clearAllConfirmText = string.Empty;
            LoadOverrides();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error deleting overrides: {ex.Message}", Severity.Error);
        }
    }

    private async Task ExportToCsv()
    {
        try
        {
            var exports = OverrideService.ExportOverrides();
            var sb = new StringBuilder();
            sb.AppendLine("ContentType,Property,OverrideType,Language,Value");

            foreach (var export in exports)
            {
                var escapedValue = EscapeCsvValue(export.Value);
                var escapedContentType = EscapeCsvValue(export.ContentType);
                var escapedProperty = EscapeCsvValue(export.Property);
                sb.AppendLine($"{escapedContentType},{escapedProperty},{export.OverrideType},{export.Language},{escapedValue}");
            }

            var fileName = $"LocalizationOverrides_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            await JSRuntime.InvokeVoidAsync("downloadFile", fileName, sb.ToString());
            Snackbar.Add($"Exported {exports.Count} override(s) to CSV", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error exporting: {ex.Message}", Severity.Error);
        }
    }

    private async Task ImportFromCsv()
    {
        if (_importFile == null)
        {
            _importStatus = "Please select a file";
            return;
        }

        try
        {
            _importStatus = "Importing...";
            StateHasChanged();

            using var stream = _importFile.OpenReadStream();
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            var imports = new List<OverrideImportDto>();
            for (var i = 1; i < lines.Length; i++) // Skip header
            {
                var parts = ParseCsvLine(lines[i].Trim());
                if (parts.Count >= 5)
                {
                    imports.Add(new OverrideImportDto
                    {
                        ContentType = parts[0],
                        Property = parts[1],
                        OverrideType = parts[2],
                        Language = parts[3],
                        Value = parts[4]
                    });
                }
            }

            var count = await Task.Run(() => OverrideService.ImportOverrides(imports));
            Snackbar.Add($"Imported {count} override(s)", Severity.Success);
            _showImportDialog = false;
            _importFile = null;
            _importStatus = string.Empty;
            LoadOverrides();
        }
        catch (Exception ex)
        {
            _importStatus = $"Error: {ex.Message}";
        }
    }

    private async Task SaveToXml(GroupedOverride grouped)
    {
        try
        {
            ContentTypeService.SavePropertyToXml(
                grouped.PropertyName,
                grouped.Language,
                grouped.CaptionValue,
                grouped.HelpTextValue);

            var count = 0;

            // Delete caption override if exists
            if (grouped.CaptionEntity != null)
            {
                OverrideService.DeleteOverride(grouped.CaptionEntity.Key, grouped.CaptionEntity.Language);
                count++;
            }

            // Delete help text override if exists
            if (grouped.HelpTextEntity != null)
            {
                OverrideService.DeleteOverride(grouped.HelpTextEntity.Key, grouped.HelpTextEntity.Language);
                count++;
            }

            Snackbar.Add($"Moved {count} override(s) to XML file", Severity.Success);
            LoadOverrides();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error saving to XML: {ex.Message}", Severity.Error);
        }
    }

    private static string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var inQuotes = false;
        var current = new StringBuilder();

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result;
    }

    private string GetLanguageName(string languageId)
    {
        return Languages.FirstOrDefault(l => l.Id.Equals(languageId, StringComparison.OrdinalIgnoreCase))?.Name ?? languageId;
    }

    private static string TruncateValue(string? value, int maxLength = 30)
    {
        if (string.IsNullOrEmpty(value))
            return "-";
        return value.Length > maxLength ? value[..(maxLength - 3)] + "..." : value;
    }

    private async Task TranslateAllEmptyAsync()
    {
        if (string.IsNullOrWhiteSpace(_editModel.CaptionValue) && string.IsNullOrWhiteSpace(_editModel.HelpTextValue))
        {
            Snackbar.Add("Enter a caption or help text in the primary language first", Severity.Warning);
            return;
        }

        _isTranslatingAll = true;
        StateHasChanged();

        try
        {
            foreach (var row in _addDialogLanguageRows)
            {
                var hasEmptyCaption = string.IsNullOrWhiteSpace(row.CaptionValue) && !string.IsNullOrWhiteSpace(_editModel.CaptionValue);
                var hasEmptyHelp = string.IsNullOrWhiteSpace(row.HelpTextValue) && !string.IsNullOrWhiteSpace(_editModel.HelpTextValue);

                if (!hasEmptyCaption && !hasEmptyHelp)
                    continue;

                row.IsTranslating = true;
                StateHasChanged();

                try
                {
                    var items = new List<BatchTranslationItem>();

                    if (hasEmptyCaption)
                    {
                        items.Add(new BatchTranslationItem(
                            _editModel.CaptionValue,
                            new TranslationContext(string.Empty, _selectedContentType?.Name ?? string.Empty, TranslationFieldType.PropertyLabel)));
                    }

                    if (hasEmptyHelp)
                    {
                        items.Add(new BatchTranslationItem(
                            _editModel.HelpTextValue,
                            new TranslationContext(string.Empty, _selectedContentType?.Name ?? string.Empty, TranslationFieldType.PropertyDescription)));
                    }

                    var batchRequest = new BatchTranslationRequest(items, _defaultLanguageId, row.Language.Id);
                    var result = await TranslationService.TranslateBatchAsync(batchRequest);

                    if (result.Success)
                    {
                        var idx = 0;
                        if (hasEmptyCaption && idx < result.Results.Count && result.Results[idx].Success)
                        {
                            row.CaptionValue = result.Results[idx].TranslatedText;
                            idx++;
                        }

                        if (hasEmptyHelp && idx < result.Results.Count && result.Results[idx].Success)
                        {
                            row.HelpTextValue = result.Results[idx].TranslatedText;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Snackbar.Add($"Translation error for {row.Language.Name}: {ex.Message}", Severity.Error);
                }
                finally
                {
                    row.IsTranslating = false;
                }
            }
        }
        finally
        {
            _isTranslatingAll = false;
            StateHasChanged();
        }
    }

    private class AddDialogLanguageRow
    {
        public LanguageInfo Language { get; set; } = default!;
        public string CaptionValue { get; set; } = string.Empty;
        public string HelpTextValue { get; set; } = string.Empty;
        public bool IsTranslating { get; set; }
    }

    private class OverrideEditModel
    {
        public string Key { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string OverrideValue { get; set; } = string.Empty;
        public string? ContentTypeName { get; set; }
        public string CaptionValue { get; set; } = string.Empty;
        public string HelpTextValue { get; set; } = string.Empty;
    }

    private record PropertyOption(string Name, string DisplayName);

    /// <summary>
    /// Groups caption and help text overrides for the same property+language into one row
    /// </summary>
    private class GroupedOverride
    {
        public string PropertyName { get; set; } = string.Empty;
        public string? ContentTypeName { get; set; }
        public string Language { get; set; } = string.Empty;
        public string? CaptionValue { get; set; }
        public string? HelpTextValue { get; set; }
        public LocalizationOverrideEntity? CaptionEntity { get; set; }
        public LocalizationOverrideEntity? HelpTextEntity { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string ModifiedBy { get; set; } = string.Empty;

        public string DisplayName => string.IsNullOrEmpty(ContentTypeName)
            ? PropertyName
            : $"{ContentTypeName} → {PropertyName}";
    }
}
