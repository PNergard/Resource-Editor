using MudBlazor;

namespace Nergard.ResourceEditor.Features.Shared.Models;

public class TranslationStatusResult
{
    public Color ContentTypeColor { get; set; }
    public int PropertyTotal { get; set; }
    public int PropertyComplete { get; set; }
    public Color PropertyColor { get; set; }
    public Color OverallColor { get; set; }

    /// <summary>Number of properties (items, not fields).</summary>
    public int PropertyItemsTotal { get; set; }

    /// <summary>Properties where both label AND description are translated.</summary>
    public int PropertyItemsComplete { get; set; }
}

public class TabStatusResult
{
    public Color StatusColor { get; set; }
}
