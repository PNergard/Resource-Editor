using Nergard.ResourceEditor.Features.Overrides.Models;

namespace Nergard.ResourceEditor.Features.Overrides.Services;

/// <summary>
/// Service for managing localization overrides stored in DDS.
/// </summary>
public interface IOverrideService
{
    /// <summary>
    /// Gets an override value for a specific key and language.
    /// Returns null if no override exists.
    /// </summary>
    string? GetOverride(string key, string language);

    /// <summary>
    /// Gets all overrides from the store.
    /// </summary>
    IReadOnlyList<LocalizationOverrideEntity> GetAllOverrides();

    /// <summary>
    /// Gets all overrides for a specific language.
    /// </summary>
    IReadOnlyList<LocalizationOverrideEntity> GetOverridesByLanguage(string language);

    /// <summary>
    /// Saves an override. Creates new or updates existing.
    /// </summary>
    void SaveOverride(string key, string language, string value, string? contentTypeName = null);

    /// <summary>
    /// Deletes an override by key and language.
    /// Returns true if deleted, false if not found.
    /// </summary>
    bool DeleteOverride(string key, string language);

    /// <summary>
    /// Deletes an override by its ID.
    /// Returns true if deleted, false if not found.
    /// </summary>
    bool DeleteOverride(Guid id);

    /// <summary>
    /// Clears the override cache, forcing a reload from DDS.
    /// </summary>
    void InvalidateCache();

    /// <summary>
    /// Deletes all overrides from the store.
    /// Returns the count of deleted overrides.
    /// </summary>
    int DeleteAllOverrides();

    /// <summary>
    /// Gets all overrides in a format suitable for CSV export.
    /// </summary>
    IReadOnlyList<OverrideExportDto> ExportOverrides();

    /// <summary>
    /// Imports overrides from a list of import DTOs.
    /// Returns the count of imported/updated overrides.
    /// </summary>
    int ImportOverrides(IEnumerable<OverrideImportDto> imports);
}
