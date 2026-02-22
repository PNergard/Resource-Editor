using EPiServer.Data.Dynamic;
using EPiServer.Framework.Cache;
using Microsoft.AspNetCore.Http;
using Nergard.ResourceEditor.Features.Overrides.Models;

namespace Nergard.ResourceEditor.Features.Overrides.Services;

/// <summary>
/// Service implementation for managing localization overrides with DDS storage and caching.
/// </summary>
public class OverrideService : IOverrideService
{
    private const string CacheKey = "Nergard:LocalizationOverrides";
    private static readonly TimeSpan CacheTimeout = TimeSpan.FromHours(24);

    private readonly DynamicDataStoreFactory _storeFactory;
    private readonly ISynchronizedObjectInstanceCache _cache;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public OverrideService(
        DynamicDataStoreFactory storeFactory,
        ISynchronizedObjectInstanceCache cache,
        IHttpContextAccessor httpContextAccessor)
    {
        _storeFactory = storeFactory;
        _cache = cache;
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetOverride(string key, string language)
    {
        var normalizedKey = NormalizeKey(key);
        var normalizedLanguage = language.ToLowerInvariant();

        var allOverrides = GetCachedOverrides();
        var lookupKey = CreateLookupKey(normalizedKey, normalizedLanguage);

        return allOverrides.TryGetValue(lookupKey, out var value) ? value : null;
    }

    public IReadOnlyList<LocalizationOverrideEntity> GetAllOverrides()
    {
        var store = GetStore();
        return store.Items<LocalizationOverrideEntity>().ToList();
    }

    public IReadOnlyList<LocalizationOverrideEntity> GetOverridesByLanguage(string language)
    {
        var normalizedLanguage = language.ToLowerInvariant();
        var store = GetStore();
        return store.Items<LocalizationOverrideEntity>()
            .Where(o => o.Language.ToLowerInvariant() == normalizedLanguage)
            .ToList();
    }

    public void SaveOverride(string key, string language, string value, string? contentTypeName = null)
    {
        var normalizedKey = NormalizeKey(key);
        var normalizedLanguage = language.ToLowerInvariant();
        var store = GetStore();

        // Find existing override
        var existing = store.Items<LocalizationOverrideEntity>()
            .FirstOrDefault(o => o.Key == normalizedKey && o.Language == normalizedLanguage);

        if (existing != null)
        {
            existing.OverrideValue = value;
            existing.ContentTypeName = contentTypeName;
            existing.ModifiedBy = GetCurrentUser();
            existing.ModifiedDate = DateTime.UtcNow;
            store.Save(existing);
        }
        else
        {
            var entity = new LocalizationOverrideEntity
            {
                Key = normalizedKey,
                Language = normalizedLanguage,
                OverrideValue = value,
                ContentTypeName = contentTypeName,
                ModifiedBy = GetCurrentUser(),
                ModifiedDate = DateTime.UtcNow
            };
            store.Save(entity);
        }

        InvalidateCache();
    }

    public bool DeleteOverride(string key, string language)
    {
        var normalizedKey = NormalizeKey(key);
        var normalizedLanguage = language.ToLowerInvariant();
        var store = GetStore();

        var existing = store.Items<LocalizationOverrideEntity>()
            .FirstOrDefault(o => o.Key == normalizedKey && o.Language == normalizedLanguage);

        if (existing == null)
            return false;

        store.Delete(existing.Id);
        InvalidateCache();
        return true;
    }

    public bool DeleteOverride(Guid id)
    {
        var store = GetStore();
        var existing = store.Items<LocalizationOverrideEntity>()
            .FirstOrDefault(o => o.Id.ExternalId == id);

        if (existing == null)
            return false;

        store.Delete(existing.Id);
        InvalidateCache();
        return true;
    }

    public void InvalidateCache()
    {
        _cache.Remove(CacheKey);
    }

    private Dictionary<string, string> GetCachedOverrides()
    {
        var cached = _cache.Get(CacheKey) as Dictionary<string, string>;
        if (cached != null)
            return cached;

        var overrides = LoadAllOverridesFromDDS();
        var evictionPolicy = new CacheEvictionPolicy(CacheTimeout, CacheTimeoutType.Sliding);
        _cache.Insert(CacheKey, overrides, evictionPolicy);
        return overrides;
    }

    private Dictionary<string, string> LoadAllOverridesFromDDS()
    {
        var store = GetStore();
        var overrides = store.Items<LocalizationOverrideEntity>().ToList();

        var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in overrides)
        {
            var lookupKey = CreateLookupKey(item.Key, item.Language);
            dictionary[lookupKey] = item.OverrideValue;
        }

        return dictionary;
    }

    private DynamicDataStore GetStore()
    {
        return _storeFactory.CreateStore(typeof(LocalizationOverrideEntity));
    }

    private static string NormalizeKey(string key)
    {
        // Ensure key starts with / and is lowercase
        var normalized = key.ToLowerInvariant();
        if (!normalized.StartsWith("/"))
            normalized = "/" + normalized;
        return normalized;
    }

    private static string CreateLookupKey(string key, string language)
    {
        return $"{key}|{language}";
    }

    private string GetCurrentUser()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";
    }

    public int DeleteAllOverrides()
    {
        var store = GetStore();
        var allOverrides = store.Items<LocalizationOverrideEntity>().ToList();
        var count = allOverrides.Count;

        foreach (var entity in allOverrides)
        {
            store.Delete(entity.Id);
        }

        InvalidateCache();
        return count;
    }

    public IReadOnlyList<OverrideExportDto> ExportOverrides()
    {
        var store = GetStore();
        // Materialize DDS query first, then project - DDS can't translate method calls in Select
        var entities = store.Items<LocalizationOverrideEntity>().ToList();
        return entities
            .Select(e => new OverrideExportDto
            {
                ContentType = e.ContentTypeName ?? "",
                Property = ExtractPropertyName(e.Key),
                OverrideType = DetermineOverrideType(e.Key),
                Language = e.Language,
                Value = e.OverrideValue
            })
            .ToList();
    }

    public int ImportOverrides(IEnumerable<OverrideImportDto> imports)
    {
        var count = 0;
        foreach (var import in imports)
        {
            var key = GenerateLocalizationKey(import.Property, import.OverrideType);
            SaveOverride(key, import.Language, import.Value, import.ContentType);
            count++;
        }
        return count;
    }

    /// <summary>
    /// Extracts the property name from a localization key.
    /// Example: "/contenttypes/icontentdata/properties/mainbody/caption" -> "mainbody"
    /// </summary>
    internal static string ExtractPropertyName(string key)
    {
        var parts = key.Split('/');
        // Key format: /contenttypes/icontentdata/properties/{propname}/caption or /help
        if (parts.Length >= 5 && parts[^1] is "caption" or "help")
        {
            return parts[^2];
        }
        // Fallback: return the key itself if format doesn't match
        return key;
    }

    /// <summary>
    /// Determines the override type (Caption or HelpText) from the key suffix.
    /// </summary>
    private static string DetermineOverrideType(string key)
    {
        if (key.EndsWith("/caption", StringComparison.OrdinalIgnoreCase))
            return "Caption";
        if (key.EndsWith("/help", StringComparison.OrdinalIgnoreCase))
            return "HelpText";
        return "Unknown";
    }

    /// <summary>
    /// Generates the full localization key from property name and override type.
    /// </summary>
    private static string GenerateLocalizationKey(string propertyName, string overrideType)
    {
        var suffix = overrideType.Equals("Caption", StringComparison.OrdinalIgnoreCase) ? "caption" : "help";
        return $"/contenttypes/icontentdata/properties/{propertyName.ToLowerInvariant()}/{suffix}";
    }
}
