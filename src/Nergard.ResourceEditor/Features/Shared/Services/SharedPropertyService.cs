using EPiServer.Core;
using EPiServer.DataAbstraction;

namespace Nergard.ResourceEditor.Features.Shared.Services;

public class SharedPropertyService(IContentTypeRepository contentTypeRepository) : ISharedPropertyService
{
    private IReadOnlyDictionary<string, IReadOnlyList<string>>? _cache;

    public IReadOnlyDictionary<string, IReadOnlyList<string>> GetSharedProperties()
    {
        return _cache ??= BuildSharedPropertyLookup();
    }

    public bool IsShared(string propertyName)
    {
        return GetSharedProperties().ContainsKey(propertyName);
    }

    public IReadOnlyList<string> GetContentTypesForProperty(string propertyName)
    {
        return GetSharedProperties().TryGetValue(propertyName, out var types)
            ? types
            : [];
    }

    public void Invalidate()
    {
        _cache = null;
    }

    private IReadOnlyDictionary<string, IReadOnlyList<string>> BuildSharedPropertyLookup()
    {
        var propertyToTypes = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var contentType in contentTypeRepository.List())
        {
            if (contentType.ModelType == null)
                continue;

            if (!typeof(PageData).IsAssignableFrom(contentType.ModelType) &&
                !typeof(BlockData).IsAssignableFrom(contentType.ModelType) &&
                !typeof(MediaData).IsAssignableFrom(contentType.ModelType))
                continue;

            foreach (var prop in contentType.PropertyDefinitions)
            {
                if (!propertyToTypes.TryGetValue(prop.Name, out var types))
                {
                    types = [];
                    propertyToTypes[prop.Name] = types;
                }

                types.Add(contentType.Name);
            }
        }

        // Only keep properties that appear on 2+ content types
        return propertyToTypes
            .Where(kvp => kvp.Value.Count > 1)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyList<string>)kvp.Value.OrderBy(n => n).ToList(),
                StringComparer.OrdinalIgnoreCase);
    }
}
