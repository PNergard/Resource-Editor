using Nergard.ResourceEditor.Features.Shared.Models;

namespace Nergard.ResourceEditor.Features.Shared.Services;

/// <summary>
/// Default no-operation translation service.
/// Used when no real translation service is registered or automated translation is disabled.
/// </summary>
internal class NoOpTranslationService : ITranslationService
{
    private const string NotConfiguredMessage = "No translation service configured";

    public Task<TranslationResult> TranslateAsync(
        TranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new TranslationResult(
            string.Empty,
            false,
            NotConfiguredMessage));
    }

    public Task<BatchTranslationResult> TranslateBatchAsync(
        BatchTranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        var results = request.Items
            .Select(item => new BatchTranslationItemResult(
                string.Empty,
                item.Context,
                false,
                NotConfiguredMessage))
            .ToList();

        return Task.FromResult(new BatchTranslationResult(
            results,
            false,
            NotConfiguredMessage));
    }

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }
}
