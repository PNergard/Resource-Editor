namespace Nergard.ResourceEditor.Features.Shared.Models;

public record ContentTypeInfo(
    int Id,
    string Name,
    string DisplayName,
    string? Description,
    ContentTypeCategory Category
);

public enum ContentTypeCategory
{
    Page,
    Block,
    Media
}
