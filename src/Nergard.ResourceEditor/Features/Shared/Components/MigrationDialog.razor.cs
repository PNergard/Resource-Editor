using Microsoft.AspNetCore.Components;
using Nergard.ResourceEditor.Features.Shared.Services;

namespace Nergard.ResourceEditor.Features.Shared.Components;

public partial class MigrationDialog
{
    [Inject] private IXmlMigrationService MigrationService { get; set; } = default!;

    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public EventCallback OnComplete { get; set; }

    private string _currentStep = "Preparing migration...";
    private int _completed;
    private int _total = 5;
    private bool _isRunning;
    private bool _isDone;
    private MigrationResult? _result;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && IsVisible && !_isRunning)
        {
            _isRunning = true;
            await RunMigrationAsync();
        }
    }

    private async Task RunMigrationAsync()
    {
        try
        {
            var progress = new Progress<MigrationProgress>(p =>
            {
                _currentStep = p.CurrentStep;
                _completed = p.Completed;
                _total = p.Total;
                InvokeAsync(StateHasChanged);
            });

            _result = await MigrationService.MigrateAsync(progress);
        }
        catch (Exception ex)
        {
            _result = new MigrationResult(false, 0, [ex.Message]);
        }
        finally
        {
            _isDone = true;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task HandleClose()
    {
        await OnComplete.InvokeAsync();
    }
}
