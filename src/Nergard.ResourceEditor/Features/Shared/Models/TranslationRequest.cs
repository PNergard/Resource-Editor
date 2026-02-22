namespace Nergard.ResourceEditor.Features.Shared.Models;

/// <summary>
/// Request for translating a single text from source language to target language.
/// </summary>
public record TranslationRequest(
    string SourceText,
    string SourceLanguage,
    string TargetLanguage,
    TranslationContext Context
);

/// <summary>
/// Result of a single translation request.
/// </summary>
public record TranslationResult(
    string TranslatedText,
    bool Success,
    string? ErrorMessage = null
);

/// <summary>
/// Request for batch translation (multiple texts to same target language).
/// </summary>
public record BatchTranslationRequest(
    List<BatchTranslationItem> Items,
    string SourceLanguage,
    string TargetLanguage
);

/// <summary>
/// A single item within a batch translation request.
/// </summary>
public record BatchTranslationItem(
    string SourceText,
    TranslationContext Context
);

/// <summary>
/// Result of a batch translation request.
/// </summary>
public record BatchTranslationResult(
    List<BatchTranslationItemResult> Results,
    bool Success,
    string? ErrorMessage = null
);

/// <summary>
/// Result of a single item within a batch translation.
/// </summary>
public record BatchTranslationItemResult(
    string TranslatedText,
    TranslationContext Context,
    bool Success,
    string? ErrorMessage = null
);
