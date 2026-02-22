using Nergard.ResourceEditor.Features.Shared.Models;

namespace Nergard.ResourceEditor.Features.Shared.Services;

/// <summary>
/// Service for automated text translation.
/// Consumers can provide their own implementation by registering it
/// before calling AddResourceEditor().
/// </summary>
public interface ITranslationService
{
    /// <summary>
    /// Translate a single text from source to target language.
    /// </summary>
    Task<TranslationResult> TranslateAsync(
        TranslationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Translate multiple texts in a single batch request.
    /// More efficient for bulk operations as it reduces API round-trips.
    /// </summary>
    Task<BatchTranslationResult> TranslateBatchAsync(
        BatchTranslationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the translation service is configured and available.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
