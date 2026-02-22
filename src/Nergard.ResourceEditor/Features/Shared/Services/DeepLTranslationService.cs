using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nergard.ResourceEditor.Features.Shared.Models;

namespace Nergard.ResourceEditor.Features.Shared.Services;

/// <summary>
/// DeepL-based translation service implementation.
/// Supports both Free and Pro API tiers with batch translation.
/// </summary>
public class DeepLTranslationService : ITranslationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DeepLOptions _options;
    private readonly ILogger<DeepLTranslationService> _logger;

    private const string FreeApiUrl = "https://api-free.deepl.com/v2/translate";
    private const string ProApiUrl = "https://api.deepl.com/v2/translate";

    /// <summary>
    /// Language code mapping from Optimizely codes to DeepL codes.
    /// </summary>
    private static readonly Dictionary<string, string> LanguageMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        { "no", "NB" },   // Norwegian → Norwegian Bokmål
        { "en", "EN" },
        { "sv", "SV" },
        { "da", "DA" },
        { "fi", "FI" },
        { "de", "DE" },
        { "fr", "FR" },
        { "es", "ES" },
        { "it", "IT" },
        { "nl", "NL" },
        { "pl", "PL" },
        { "pt", "PT-PT" }, // Portuguese (Portugal) - DeepL requires region
        { "ru", "RU" },
        { "ja", "JA" },
        { "zh", "ZH" }
    };

    public DeepLTranslationService(
        IHttpClientFactory httpClientFactory,
        IOptions<DeepLOptions> options,
        ILogger<DeepLTranslationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<TranslationResult> TranslateAsync(
        TranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
            return new TranslationResult(string.Empty, false, "DeepL API key not configured");

        try
        {
            var response = await SendTranslationRequest(
                [request.SourceText],
                request.SourceLanguage,
                request.TargetLanguage,
                cancellationToken);

            if (response?.Translations is { Length: > 0 })
            {
                var translatedText = response.Translations[0].Text;
                _logger.LogDebug(
                    "Translated '{SourceText}' ({SourceLang} → {TargetLang}): '{TranslatedText}'",
                    request.SourceText,
                    request.SourceLanguage,
                    request.TargetLanguage,
                    translatedText);
                return new TranslationResult(translatedText, true);
            }

            return new TranslationResult(string.Empty, false, "No translation returned from DeepL");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeepL translation failed for: {SourceText}", request.SourceText);
            return new TranslationResult(string.Empty, false, ex.Message);
        }
    }

    public async Task<BatchTranslationResult> TranslateBatchAsync(
        BatchTranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            var errorResults = request.Items
                .Select(item => new BatchTranslationItemResult(
                    string.Empty, item.Context, false, "DeepL API key not configured"))
                .ToList();
            return new BatchTranslationResult(errorResults, false, "DeepL API key not configured");
        }

        if (request.Items.Count == 0)
            return new BatchTranslationResult([], true);

        try
        {
            var texts = request.Items.Select(i => i.SourceText).ToArray();

            var response = await SendTranslationRequest(
                texts,
                request.SourceLanguage,
                request.TargetLanguage,
                cancellationToken);

            if (response?.Translations != null)
            {
                var results = request.Items
                    .Select((item, index) =>
                    {
                        if (index < response.Translations.Length)
                        {
                            return new BatchTranslationItemResult(
                                response.Translations[index].Text,
                                item.Context,
                                true);
                        }
                        return new BatchTranslationItemResult(
                            string.Empty, item.Context, false, "No translation returned");
                    })
                    .ToList();

                _logger.LogInformation(
                    "Batch translated {Count} items ({SourceLang} → {TargetLang})",
                    results.Count(r => r.Success),
                    request.SourceLanguage,
                    request.TargetLanguage);

                return new BatchTranslationResult(results, true);
            }

            var fallbackResults = request.Items
                .Select(item => new BatchTranslationItemResult(
                    string.Empty, item.Context, false, "No translations returned from DeepL"))
                .ToList();
            return new BatchTranslationResult(fallbackResults, false, "No translations returned");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeepL batch translation failed ({Count} items)", request.Items.Count);
            var errorResults = request.Items
                .Select(item => new BatchTranslationItemResult(
                    string.Empty, item.Context, false, ex.Message))
                .ToList();
            return new BatchTranslationResult(errorResults, false, ex.Message);
        }
    }

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(IsConfigured());
    }

    private bool IsConfigured() => !string.IsNullOrWhiteSpace(_options.ApiKey);

    private async Task<DeepLResponse?> SendTranslationRequest(
        string[] texts,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        var apiUrl = _options.UseFreeApi ? FreeApiUrl : ProApiUrl;
        var sourceLang = MapLanguageCode(sourceLanguage);
        var targetLang = MapLanguageCode(targetLanguage);

        var requestBody = new DeepLRequest
        {
            Text = texts,
            SourceLang = sourceLang,
            TargetLang = targetLang
        };

        var httpClient = _httpClientFactory.CreateClient("DeepL");
        httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, apiUrl);
        httpRequest.Headers.Add("Authorization", $"DeepL-Auth-Key {_options.ApiKey}");
        httpRequest.Content = JsonContent.Create(requestBody);

        var httpResponse = await httpClient.SendAsync(httpRequest, cancellationToken);

        if (!httpResponse.IsSuccessStatusCode)
        {
            var error = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("DeepL API error: {StatusCode} - {Error}", httpResponse.StatusCode, error);
            throw new HttpRequestException($"DeepL API returned {httpResponse.StatusCode}: {error}");
        }

        return await httpResponse.Content.ReadFromJsonAsync<DeepLResponse>(cancellationToken: cancellationToken);
    }

    private static string MapLanguageCode(string optimizelyCode)
    {
        return LanguageMapping.TryGetValue(optimizelyCode, out var deeplCode)
            ? deeplCode
            : optimizelyCode.ToUpperInvariant();
    }

    // DeepL API DTOs

    private class DeepLRequest
    {
        [JsonPropertyName("text")]
        public string[] Text { get; set; } = [];

        [JsonPropertyName("source_lang")]
        public string? SourceLang { get; set; }

        [JsonPropertyName("target_lang")]
        public string TargetLang { get; set; } = string.Empty;
    }

    private class DeepLResponse
    {
        [JsonPropertyName("translations")]
        public DeepLTranslation[]? Translations { get; set; }
    }

    private class DeepLTranslation
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("detected_source_language")]
        public string? DetectedSourceLanguage { get; set; }
    }
}
