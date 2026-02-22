using Microsoft.AspNetCore.Components;
using MudBlazor;
using Nergard.ResourceEditor.Features.ContentTypes.Models;
using Nergard.ResourceEditor.Features.ContentTypes.Services;
using Nergard.ResourceEditor.Features.DisplayChannels.Models;
using Nergard.ResourceEditor.Features.DisplayChannels.Services;
using Nergard.ResourceEditor.Features.EditorHints.Models;
using Nergard.ResourceEditor.Features.EditorHints.Services;
using Nergard.ResourceEditor.Features.Shared.Constants;
using Nergard.ResourceEditor.Features.Views.Models;
using Nergard.ResourceEditor.Features.Views.Services;
using Nergard.ResourceEditor.Features.Shared.Models;
using Nergard.ResourceEditor.Features.Shared.Services;
using Nergard.ResourceEditor.Features.Tabs.Models;
using Nergard.ResourceEditor.Features.Tabs.Services;

namespace Nergard.ResourceEditor.Components;

public partial class ResourceEditor
{
    [Inject] private IContentTypeLocalizationService ContentTypeService { get; set; } = default!;
    [Inject] private ITabLocalizationService TabService { get; set; } = default!;
    [Inject] private ILanguageService LanguageService { get; set; } = default!;
    [Inject] private ITranslationStatusService StatusService { get; set; } = default!;
    [Inject] private IDisplayLocalizationService DisplayService { get; set; } = default!;
    [Inject] private IEditorHintLocalizationService EditorHintService { get; set; } = default!;
    [Inject] private IViewLocalizationService ViewService { get; set; } = default!;
    [Inject] private IXmlMigrationService MigrationService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private enum EditorMode { None, ContentType, Tab, Language, Display, EditorHint, View, Override }

    private bool _isLoading;
    private bool _isSaving;
    private bool _showMigrationDialog;

    private EditorMode _currentMode = EditorMode.None;
    private string? _selectedItemName;
    private ContentTypeCategory? _selectedCategory;

    private IReadOnlyList<ContentTypeInfo> _pageTypes = [];
    private IReadOnlyList<ContentTypeInfo> _blockTypes = [];
    private IReadOnlyList<ContentTypeInfo> _mediaTypes = [];
    private IReadOnlyList<TabInfo> _tabs = [];
    private IReadOnlyList<ViewFileInfo> _viewFiles = [];
    private IReadOnlyList<LanguageInfo> _languages = [];

    private ContentTypeTranslation? _contentTypeTranslation;
    private TabTranslation? _tabTranslation;
    private DisplayTranslation? _displayTranslation;
    private EditorHintTranslation? _editorHintTranslation;
    private ViewTranslation? _viewTranslation;
    private string _selectedLanguageId = string.Empty;
    private LanguageInfo? _selectedLanguage;

    // Unsaved changes dialog state
    private bool _showUnsavedDialog;
    private (string Type, string Name)? _pendingSelection;

    protected override void OnInitialized()
    {
        if (MigrationService.NeedsMigration())
        {
            _showMigrationDialog = true;
        }
        else
        {
            LoadData();
        }
    }

    private void HandleMigrationComplete()
    {
        _showMigrationDialog = false;
        LoadData();
        StateHasChanged();
    }

    private void LoadData()
    {
        _isLoading = true;

        try
        {
            _languages = LanguageService.GetLanguages();
            _pageTypes = ContentTypeService.GetPageTypes();
            _blockTypes = ContentTypeService.GetBlockTypes();
            _mediaTypes = ContentTypeService.GetMediaTypes();
            _tabs = TabService.GetTabs();
            _viewFiles = ViewService.GetViewFiles();
        }
        catch (Exception ex)
        {
            Snackbar.Add(string.Format(UIStrings.MessageSaveError, ex.Message), Severity.Error);
        }
        finally
        {
            _isLoading = false;
            _selectedLanguageId = _languages.FirstOrDefault(l => l.IsDefault)?.Id ?? string.Empty;
        }
    }

    private void HandleLanguageFilterChanged(string languageId)
    {
        _selectedLanguageId = languageId;
    }

    private void HandleItemSelected((string Type, string Name) selection)
    {
        if (HasUnsavedChanges())
        {
            _pendingSelection = selection;
            _showUnsavedDialog = true;
            return;
        }

        PerformNavigation(selection);
    }

    private bool HasUnsavedChanges()
    {
        return (_contentTypeTranslation?.HasChanges ?? false)
            || (_tabTranslation?.HasChanges ?? false)
            || (_displayTranslation?.HasChanges ?? false)
            || (_editorHintTranslation?.HasChanges ?? false)
            || (_viewTranslation?.HasChanges ?? false);
    }

    private void PerformNavigation((string Type, string Name) selection)
    {
        _selectedItemName = selection.Name;

        switch (selection.Type)
        {
            case NavigationTypes.Page:
                LoadContentTypeTranslation(selection.Name, ContentTypeCategory.Page);
                break;
            case NavigationTypes.Block:
                LoadContentTypeTranslation(selection.Name, ContentTypeCategory.Block);
                break;
            case NavigationTypes.Media:
                LoadContentTypeTranslation(selection.Name, ContentTypeCategory.Media);
                break;
            case NavigationTypes.Tab:
                LoadTabTranslation(selection.Name);
                break;
            case NavigationTypes.Display:
                LoadDisplayTranslation();
                break;
            case NavigationTypes.EditorHint:
                LoadEditorHintTranslation();
                break;
            case NavigationTypes.View:
                LoadViewTranslation(selection.Name);
                break;
            case NavigationTypes.Override:
                LoadOverrideManager();
                break;
        }
    }

    private void CancelNavigation()
    {
        _showUnsavedDialog = false;
        _pendingSelection = null;
    }

    private void ConfirmNavigation()
    {
        _showUnsavedDialog = false;
        if (_pendingSelection.HasValue)
        {
            PerformNavigation(_pendingSelection.Value);
            _pendingSelection = null;
        }
        else
        {
            ResetToDashboard();
        }
    }

    private void LoadContentTypeTranslation(string name, ContentTypeCategory category)
    {
        _currentMode = EditorMode.ContentType;
        _selectedCategory = category;
        _contentTypeTranslation = ContentTypeService.GetTranslations(name, category);
        ClearOtherTranslations(EditorMode.ContentType);
    }

    private void LoadTabTranslation(string name)
    {
        _currentMode = EditorMode.Tab;
        _selectedCategory = null;
        _tabTranslation = TabService.GetTranslations(name);
        ClearOtherTranslations(EditorMode.Tab);
    }

    private void LoadDisplayTranslation()
    {
        _currentMode = EditorMode.Display;
        _selectedCategory = null;
        _displayTranslation = DisplayService.GetTranslations();
        ClearOtherTranslations(EditorMode.Display);
    }

    private void LoadEditorHintTranslation()
    {
        _currentMode = EditorMode.EditorHint;
        _selectedCategory = null;
        _editorHintTranslation = EditorHintService.GetTranslations();
        ClearOtherTranslations(EditorMode.EditorHint);
    }

    private void LoadOverrideManager()
    {
        _currentMode = EditorMode.Override;
        _selectedCategory = null;
        _selectedItemName = NavigationTypes.Override;
        ClearOtherTranslations(EditorMode.Override);
    }

    private void ClearOtherTranslations(EditorMode keepMode)
    {
        if (keepMode != EditorMode.ContentType) _contentTypeTranslation = null;
        if (keepMode != EditorMode.Tab) _tabTranslation = null;
        if (keepMode != EditorMode.Display) _displayTranslation = null;
        if (keepMode != EditorMode.EditorHint) _editorHintTranslation = null;
        if (keepMode != EditorMode.View) _viewTranslation = null;
    }

    private async Task SaveAsync(Action saveAction)
    {
        _isSaving = true;
        StateHasChanged();

        try
        {
            await Task.Run(saveAction);
            StatusService.Invalidate();
            Snackbar.Add(UIStrings.MessageSaveSuccess, Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add(string.Format(UIStrings.MessageSaveError, ex.Message), Severity.Error);
        }
        finally
        {
            _isSaving = false;
            StateHasChanged();
        }
    }

    private Task SaveTranslationAsync<T>(T? translation, Action<T> saveAction) where T : class
        => translation != null ? SaveAsync(() => saveAction(translation)) : Task.CompletedTask;

    private void HandleResetContentType()
    {
        if (_selectedItemName == null || _selectedCategory == null) return;
        _contentTypeTranslation = ContentTypeService.GetTranslations(_selectedItemName, _selectedCategory.Value);
    }

    private void HandleResetTab()
    {
        if (_selectedItemName == null) return;
        _tabTranslation = TabService.GetTranslations(_selectedItemName);
    }

    private void HandleResetDisplay()
    {
        _displayTranslation = DisplayService.GetTranslations();
    }

    private void HandleResetEditorHint()
    {
        _editorHintTranslation = EditorHintService.GetTranslations();
    }

    private void LoadViewTranslation(string fileName)
    {
        _currentMode = EditorMode.View;
        _selectedCategory = null;
        _viewTranslation = ViewService.GetTranslations(fileName);
        ClearOtherTranslations(EditorMode.View);
    }

    private void HandleResetView()
    {
        if (_selectedItemName == null) return;
        _viewTranslation = ViewService.GetTranslations(_selectedItemName);
    }

    private void HandleDashboardClicked()
    {
        if (HasUnsavedChanges())
        {
            _pendingSelection = null;
            _showUnsavedDialog = true;
            return;
        }

        ResetToDashboard();
    }

    private void HandleLanguageSelected(string languageId)
    {
        _selectedLanguageId = languageId;
        _selectedLanguage = _languages.FirstOrDefault(l => l.Id == languageId);

        if (_selectedLanguage != null)
        {
            _currentMode = EditorMode.Language;
            _selectedItemName = null;
            _selectedCategory = null;
            ClearOtherTranslations(EditorMode.Language);
        }
    }

    private void HandleNavigateFromLanguageView((string Type, string Name) selection)
    {
        PerformNavigation(selection);
    }

    private void HandleLanguageViewSaved()
    {
        StatusService.Invalidate();
    }

    private void HandleViewSelected(string fileName)
    {
        HandleItemSelected((NavigationTypes.View, fileName));
    }

    private void ResetToDashboard()
    {
        _currentMode = EditorMode.None;
        _selectedItemName = null;
        _selectedCategory = null;
        _contentTypeTranslation = null;
        _tabTranslation = null;
        _displayTranslation = null;
        _editorHintTranslation = null;
        _viewTranslation = null;
    }
}
