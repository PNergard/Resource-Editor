using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using MudBlazor;
using Nergard.ResourceEditor.Features.Shared.Models;
using Nergard.ResourceEditor.Features.Shared.Services;

namespace Nergard.ResourceEditor.Features.Shared.Components;

public abstract class EditorComponentBase<TTranslation> : ComponentBase
{
    [Inject] protected IOptions<ResourceEditorOptions> Options { get; set; } = default!;
    [Inject] protected ITranslationService TranslationService { get; set; } = default!;
    [Inject] protected ILanguageService LanguageService { get; set; } = default!;
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;

    [Parameter, EditorRequired] public TTranslation Translation { get; set; } = default!;
    [Parameter] public IReadOnlyList<LanguageInfo> Languages { get; set; } = [];
    [Parameter] public string SelectedLanguageId { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> SelectedLanguageIdChanged { get; set; }
    [Parameter] public EventCallback OnSave { get; set; }
    [Parameter] public EventCallback OnReset { get; set; }
    [Parameter] public bool IsSaving { get; set; }

    protected bool IsSingleLanguage => !string.IsNullOrEmpty(SelectedLanguageId);

    protected IReadOnlyList<LanguageInfo> FilteredLanguages =>
        IsSingleLanguage
            ? Languages.Where(l => l.Id == SelectedLanguageId).ToList()
            : Languages;

    protected bool IsTranslating { get; set; }
    protected bool IsTranslationAvailable { get; set; }
    protected string DefaultLanguageId { get; private set; } = "en";

    protected override async Task OnInitializedAsync()
    {
        DefaultLanguageId = LanguageService.GetDefaultLanguage().Id;

        if (Options.Value.EnableAutomatedTranslation)
        {
            IsTranslationAvailable = await TranslationService.IsAvailableAsync();
        }
    }

    protected async Task OnLanguageFilterChanged(string value)
    {
        await SelectedLanguageIdChanged.InvokeAsync(value);
    }

    /// <summary>
    /// Translates all empty fields for the current item across all target languages.
    /// </summary>
    protected async Task TranslateMissingFieldsAsync()
    {
        if (!Options.Value.EnableAutomatedTranslation || !IsTranslationAvailable)
            return;

        IsTranslating = true;
        StateHasChanged();

        try
        {
            var targetLanguages = FilteredLanguages
                .Where(l => l.Id != DefaultLanguageId)
                .ToList();

            if (targetLanguages.Count == 0)
            {
                Snackbar.Add("No target languages to translate", Severity.Info);
                return;
            }

            var entries = GetTranslatableEntries();
            if (entries.Count == 0)
            {
                Snackbar.Add("No translatable fields found", Severity.Info);
                return;
            }

            var totalTranslated = 0;
            var totalFailed = 0;

            foreach (var targetLang in targetLanguages)
            {
                var itemsToTranslate = new List<(BatchTranslationItem Item, TranslatableEntry Entry)>();

                foreach (var entry in entries)
                {
                    var sourceValue = entry.Entry.Values.GetValueOrDefault(DefaultLanguageId, "");
                    var targetValue = entry.Entry.Values.GetValueOrDefault(targetLang.Id, "");

                    if (!string.IsNullOrWhiteSpace(sourceValue) && string.IsNullOrWhiteSpace(targetValue))
                    {
                        var context = new TranslationContext(
                            entry.LocalizationKey,
                            entry.ContentTypeName,
                            entry.FieldType);

                        itemsToTranslate.Add((
                            new BatchTranslationItem(sourceValue, context),
                            entry));
                    }
                }

                if (itemsToTranslate.Count == 0)
                    continue;

                // Batch in chunks of 50 (DeepL limit)
                const int batchSize = 50;
                for (var i = 0; i < itemsToTranslate.Count; i += batchSize)
                {
                    var batch = itemsToTranslate.Skip(i).Take(batchSize).ToList();
                    var batchRequest = new BatchTranslationRequest(
                        batch.Select(b => b.Item).ToList(),
                        DefaultLanguageId,
                        targetLang.Id);

                    var batchResult = await TranslationService.TranslateBatchAsync(batchRequest);

                    if (batchResult.Success)
                    {
                        for (var j = 0; j < batchResult.Results.Count; j++)
                        {
                            var result = batchResult.Results[j];
                            if (result.Success && !string.IsNullOrWhiteSpace(result.TranslatedText))
                            {
                                batch[j].Entry.Entry.Values[targetLang.Id] = result.TranslatedText;
                                totalTranslated++;
                            }
                            else
                            {
                                totalFailed++;
                            }
                        }
                    }
                    else
                    {
                        totalFailed += batch.Count;
                    }
                }
            }

            if (totalTranslated > 0)
                Snackbar.Add($"Translated {totalTranslated} field(s)", Severity.Success);

            if (totalFailed > 0)
                Snackbar.Add($"Failed to translate {totalFailed} field(s)", Severity.Warning);

            if (totalTranslated == 0 && totalFailed == 0)
                Snackbar.Add("No empty fields to translate", Severity.Info);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Translation error: {ex.Message}", Severity.Error);
        }
        finally
        {
            IsTranslating = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Returns the list of translatable entries for this editor.
    /// Override in each editor to provide specific fields.
    /// </summary>
    protected virtual List<TranslatableEntry> GetTranslatableEntries() => [];

    /// <summary>
    /// Represents a translatable field that can be batch-translated.
    /// </summary>
    protected record TranslatableEntry(
        TranslationEntry Entry,
        string LocalizationKey,
        string ContentTypeName,
        TranslationFieldType FieldType);
}
