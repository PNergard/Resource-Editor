namespace Nergard.ResourceEditor.Features.Shared.Models;

/// <summary>
/// Context for automated translation requests.
/// Provides semantic information about what is being translated.
/// </summary>
public record TranslationContext(
    string LocalizationKey,
    string ContentTypeName,
    TranslationFieldType FieldType
);

/// <summary>
/// Describes the type of field being translated.
/// Can be used by translation services to adjust translation style.
/// </summary>
public enum TranslationFieldType
{
    ContentTypeName,
    ContentTypeDescription,
    PropertyLabel,
    PropertyDescription,
    TabDisplayName,
    DisplayChannel,
    DisplayOption,
    DisplayResolution,
    EditorHintValue,
    ViewValue
}
