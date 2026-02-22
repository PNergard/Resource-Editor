using Microsoft.AspNetCore.Components;
using Nergard.ResourceEditor.Features.Shared.Models;
using Nergard.ResourceEditor.Features.Views.Models;

namespace Nergard.ResourceEditor.Features.Shared.Components;

public partial class LeftMenu
{
    [Parameter] public IReadOnlyList<ContentTypeInfo> PageTypes { get; set; } = [];
    [Parameter] public IReadOnlyList<ContentTypeInfo> BlockTypes { get; set; } = [];
    [Parameter] public IReadOnlyList<ContentTypeInfo> MediaTypes { get; set; } = [];
    [Parameter] public IReadOnlyList<TabInfo> Tabs { get; set; } = [];
    [Parameter] public IReadOnlyList<ViewFileInfo> ViewFiles { get; set; } = [];
    [Parameter] public string? SelectedItem { get; set; }
    [Parameter] public EventCallback<(string Type, string Name)> OnItemSelected { get; set; }
    [Parameter] public EventCallback OnDashboardClicked { get; set; }

    private bool _pagesExpanded;
    private bool _blocksExpanded;
    private bool _mediaExpanded;
    private bool _tabsExpanded;
    private bool _viewsExpanded;

    private async Task SelectItem(string type, string name)
    {
        await OnItemSelected.InvokeAsync((type, name));
    }
}
