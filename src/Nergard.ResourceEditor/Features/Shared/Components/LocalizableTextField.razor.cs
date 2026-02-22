using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using MudBlazor;
using Nergard.ResourceEditor.Features.Overrides.Services;
using Nergard.ResourceEditor.Features.Shared.Models;
using Nergard.ResourceEditor.Features.Shared.Services;

namespace Nergard.ResourceEditor.Features.Shared.Components;

public partial class LocalizableTextField
{
    [Inject] private IOverrideService OverrideService { get; set; } = default!;
    [Inject] private IOptions<ResourceEditorOptions> Options { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ITranslationService TranslationService { get; set; } = default!;
    [Inject] private ILanguageService LanguageService { get; set; } = default!;

    /// <summary>
    /// The XML translation value (two-way bound).
    /// </summary>
    [Parameter]
    public string Value { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    /// <summary>
    /// The label to display (typically the language name).
    /// </summary>
    [Parameter]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// The localization key for this field (e.g., "/contenttypes/standardpage/name").
    /// </summary>
    [Parameter, EditorRequired]
    public string LocalizationKey { get; set; } = string.Empty;

    /// <summary>
    /// The language code (e.g., "en", "sv").
    /// </summary>
    [Parameter, EditorRequired]
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Number of lines for multiline input.
    /// </summary>
    [Parameter]
    public int Lines { get; set; } = 1;

    /// <summary>
    /// MudBlazor variant.
    /// </summary>
    [Parameter]
    public Variant Variant { get; set; } = Variant.Outlined;

    /// <summary>
    /// MudBlazor margin.
    /// </summary>
    [Parameter]
    public Margin Margin { get; set; } = Margin.Dense;

    /// <summary>
    /// Optional content type name for reference in override storage.
    /// </summary>
    [Parameter]
    public string? ContentTypeName { get; set; }

    /// <summary>
    /// Optional property name for display in override dialog.
    /// </summary>
    [Parameter]
    public string? PropertyName { get; set; }

    /// <summary>
    /// Event triggered when an override is saved or deleted.
    /// </summary>
    [Parameter]
    public EventCallback OnOverrideChanged { get; set; }

    /// <summary>
    /// The value in the source/default language to translate from.
    /// </summary>
    [Parameter]
    public string SourceValue { get; set; } = string.Empty;

    /// <summary>
    /// The type of field being translated (for translation context).
    /// </summary>
    [Parameter]
    public TranslationFieldType FieldType { get; set; } = TranslationFieldType.ContentTypeName;

    /// <summary>
    /// Whether to show the translate button. Controlled by the parent editor.
    /// </summary>
    [Parameter]
    public bool ShowTranslate { get; set; }

    private string? _overrideValue;
    private bool _isTranslating;
    private bool _showOverrideDialog;
    private string _editOverrideValue = string.Empty;

    // Multi-language expansion state
    private bool _showLanguageExpansion;
    private bool _isTranslatingAll;
    private List<LanguageOverrideRow> _languageRows = [];

    private bool HasOverride => !string.IsNullOrEmpty(_overrideValue);
    private bool ShowMultiLanguage => Options.Value.EnableAutomatedTranslation;
    private MaxWidth DialogMaxWidth => ShowMultiLanguage ? MaxWidth.Medium : MaxWidth.Small;

    protected override void OnInitialized()
    {
        LoadOverride();
    }

    protected override void OnParametersSet()
    {
        // Reload override when key or language changes
        LoadOverride();
    }

    private void LoadOverride()
    {
        if (Options.Value.EnableOverrides && !string.IsNullOrEmpty(LocalizationKey) && !string.IsNullOrEmpty(Language))
        {
            _overrideValue = OverrideService.GetOverride(LocalizationKey, Language);
        }
        else
        {
            _overrideValue = null;
        }
    }

    private async Task OnValueChanged(string value)
    {
        Value = value;
        await ValueChanged.InvokeAsync(value);
    }

    private void OpenOverrideDialog()
    {
        _editOverrideValue = _overrideValue ?? Value ?? string.Empty;
        _showLanguageExpansion = false;
        _languageRows = [];
        _showOverrideDialog = true;
    }

    private void CloseOverrideDialog()
    {
        _showOverrideDialog = false;
    }

    private async Task SaveOverride()
    {
        if (string.IsNullOrWhiteSpace(_editOverrideValue))
            return;

        try
        {
            OverrideService.SaveOverride(LocalizationKey, Language, _editOverrideValue, ContentTypeName);
            _overrideValue = _editOverrideValue;

            // Save non-empty language rows
            var extraCount = 0;
            foreach (var row in _languageRows)
            {
                if (!string.IsNullOrWhiteSpace(row.EditValue))
                {
                    OverrideService.SaveOverride(LocalizationKey, row.Language.Id, row.EditValue, ContentTypeName);
                    extraCount++;
                }
            }

            _showOverrideDialog = false;

            var message = extraCount > 0
                ? $"Override saved for {1 + extraCount} language(s)"
                : "Override saved";
            Snackbar.Add(message, Severity.Success);
            await OnOverrideChanged.InvokeAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error saving override: {ex.Message}", Severity.Error);
        }
    }

    private async Task DeleteOverride()
    {
        try
        {
            OverrideService.DeleteOverride(LocalizationKey, Language);
            _overrideValue = null;
            _showOverrideDialog = false;

            Snackbar.Add("Override deleted", Severity.Success);
            await OnOverrideChanged.InvokeAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error deleting override: {ex.Message}", Severity.Error);
        }
    }

    private void LoadLanguageRows()
    {
        var allLanguages = LanguageService.GetLanguages();
        _languageRows = allLanguages
            .Where(l => !l.Id.Equals(Language, StringComparison.OrdinalIgnoreCase))
            .Select(l =>
            {
                var existing = OverrideService.GetOverride(LocalizationKey, l.Id);
                return new LanguageOverrideRow
                {
                    Language = l,
                    ExistingValue = existing,
                    EditValue = existing ?? string.Empty
                };
            })
            .ToList();
    }

    private async Task TranslateAllEmpty()
    {
        if (string.IsNullOrWhiteSpace(_editOverrideValue))
        {
            Snackbar.Add("Enter a value in the primary field first", Severity.Warning);
            return;
        }

        _isTranslatingAll = true;
        StateHasChanged();

        try
        {
            var defaultLanguage = LanguageService.GetDefaultLanguage();

            foreach (var row in _languageRows)
            {
                if (!string.IsNullOrWhiteSpace(row.EditValue))
                    continue;

                row.IsTranslating = true;
                StateHasChanged();

                try
                {
                    var context = new TranslationContext(
                        LocalizationKey,
                        ContentTypeName ?? string.Empty,
                        FieldType);

                    var request = new TranslationRequest(
                        _editOverrideValue,
                        defaultLanguage.Id,
                        row.Language.Id,
                        context);

                    var result = await TranslationService.TranslateAsync(request);

                    if (result.Success && !string.IsNullOrWhiteSpace(result.TranslatedText))
                    {
                        row.EditValue = result.TranslatedText;
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

    private async Task TranslateField()
    {
        if (string.IsNullOrWhiteSpace(SourceValue))
        {
            Snackbar.Add("No source text to translate from", Severity.Warning);
            return;
        }

        _isTranslating = true;
        StateHasChanged();

        try
        {
            var defaultLanguage = LanguageService.GetDefaultLanguage();
            var context = new TranslationContext(
                LocalizationKey,
                ContentTypeName ?? string.Empty,
                FieldType);

            var request = new TranslationRequest(
                SourceValue,
                defaultLanguage.Id,
                Language,
                context);

            var result = await TranslationService.TranslateAsync(request);

            if (result.Success && !string.IsNullOrWhiteSpace(result.TranslatedText))
            {
                await OnValueChanged(result.TranslatedText);
            }
            else
            {
                Snackbar.Add($"Translation failed: {result.ErrorMessage}", Severity.Error);
            }
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

    private class LanguageOverrideRow
    {
        public LanguageInfo Language { get; set; } = default!;
        public string? ExistingValue { get; set; }
        public string EditValue { get; set; } = string.Empty;
        public bool IsTranslating { get; set; }
    }
}
