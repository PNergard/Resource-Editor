using System.Globalization;
using Nergard.ResourceEditor.Features.Shared.Models;
using Nergard.ResourceEditor.Features.Views.Models;

namespace Nergard.ResourceEditor.Features.Views.Components;

public partial class ViewEditor
{
    private bool _allExpanded;

    // Add/Delete dialog state
    private bool _showAddDialog;
    private bool _showDeleteDialog;
    private string _addItemName = "";
    private string? _addToSection; // null = adding a section, non-null = adding key to this section
    private string _addDialogTitle = "";
    private string _addValidationError = "";
    private ViewSection? _deleteSectionTarget;
    private ViewEntry? _deleteEntryTarget;
    private ViewSection? _deleteEntrySection;

    private bool EnableStructureEditing => Options.Value.EnableFileSaving;

    private void ToggleAllExpanded()
    {
        _allExpanded = !_allExpanded;
    }

    private void OpenAddSectionDialog()
    {
        _addItemName = "";
        _addToSection = null;
        _addDialogTitle = "Add Section";
        _addValidationError = "";
        _showAddDialog = true;
    }

    private void OpenAddKeyDialog(ViewSection section)
    {
        _addItemName = "";
        _addToSection = section.Name;
        _addDialogTitle = $"Add Key to {section.DisplayName}";
        _addValidationError = "";
        _showAddDialog = true;
    }

    private void ConfirmAdd()
    {
        var name = _addItemName.Trim();
        if (string.IsNullOrEmpty(name))
            return;

        if (_addToSection == null)
        {
            // Adding a section
            if (Translation.Sections.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                _addValidationError = $"A section named '{name}' already exists.";
                return;
            }

            var section = new ViewSection
            {
                Name = name,
                DisplayName = Capitalize(name),
                Entries = []
            };
            Translation.Sections.Add(section);
            Translation.StructureChanged = true;
        }
        else
        {
            // Adding a key to an existing section
            var section = Translation.Sections.FirstOrDefault(s => s.Name == _addToSection);
            if (section == null)
                return;

            if (section.Entries.Any(e => e.Key.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                _addValidationError = $"A key named '{name}' already exists in this section.";
                return;
            }

            var entry = new ViewEntry
            {
                Key = name,
                Value = new TranslationEntry()
            };

            foreach (var lang in Languages)
            {
                entry.Value.Values[lang.Id] = "";
            }

            section.Entries.Add(entry);
            Translation.StructureChanged = true;
        }

        _showAddDialog = false;
    }

    private void OpenDeleteKeyDialog(ViewSection section, ViewEntry entry)
    {
        _deleteSectionTarget = null;
        _deleteEntryTarget = entry;
        _deleteEntrySection = section;
        _showDeleteDialog = true;
    }

    private void OpenDeleteSectionDialog(ViewSection section)
    {
        _deleteSectionTarget = section;
        _deleteEntryTarget = null;
        _deleteEntrySection = null;
        _showDeleteDialog = true;
    }

    private void ExecuteDelete()
    {
        if (_deleteSectionTarget != null)
        {
            Translation.Sections.Remove(_deleteSectionTarget);
            Translation.StructureChanged = true;
        }
        else if (_deleteEntryTarget != null && _deleteEntrySection != null)
        {
            _deleteEntrySection.Entries.Remove(_deleteEntryTarget);
            Translation.StructureChanged = true;
        }

        _showDeleteDialog = false;
        _deleteSectionTarget = null;
        _deleteEntryTarget = null;
        _deleteEntrySection = null;
    }

    protected override List<TranslatableEntry> GetTranslatableEntries()
    {
        var entries = new List<TranslatableEntry>();

        foreach (var section in Translation.Sections)
        {
            foreach (var entry in section.Entries)
            {
                entries.Add(new(
                    entry.Value,
                    GetViewKey(section, entry),
                    $"View: {section.Name}/{entry.Key}",
                    TranslationFieldType.ViewValue));
            }
        }

        return entries;
    }

    private static string GetViewKey(ViewSection section, ViewEntry entry)
    {
        return $"/{section.Name.ToLowerInvariant()}/{entry.Key.ToLowerInvariant()}";
    }

    private static string Capitalize(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return char.ToUpperInvariant(value[0]) + value[1..];
    }
}
