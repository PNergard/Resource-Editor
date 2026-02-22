namespace Nergard.ResourceEditor.Features.Overrides.Models;

/// <summary>
/// DTO for importing overrides from CSV format.
/// </summary>
public class OverrideImportDto
{
    public string ContentType { get; set; } = string.Empty;
    public string Property { get; set; } = string.Empty;
    public string OverrideType { get; set; } = string.Empty;  // "Caption" or "HelpText"
    public string Language { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
