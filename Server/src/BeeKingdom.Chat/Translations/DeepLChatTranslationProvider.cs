using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BeeKingdom.Chat.Configuration;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Chat.Translations;

public sealed class DeepLChatTranslationProvider : IChatTranslationProvider
{
    private readonly HttpClient httpClient;
    private readonly ChatTranslationOptions options;

    public DeepLChatTranslationProvider(HttpClient httpClient, IOptions<ChatTranslationOptions> options)
    {
        this.httpClient = httpClient;
        this.options = options.Value;
        if (string.IsNullOrWhiteSpace(this.options.ApiKey))
        {
            throw new InvalidOperationException("DeepL translation provider requires ChatTranslation:ApiKey to be configured.");
        }
    }

    public async Task<string> TranslateAsync(ChatTranslationInput input, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, ResolveEndpoint());
        request.Headers.TryAddWithoutValidation("Authorization", $"DeepL-Auth-Key {options.ApiKey}");
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["text"] = input.OriginalText,
            ["source_lang"] = ToDeepLSourceLanguage(input.SourceLocale),
            ["target_lang"] = ToDeepLTargetLanguage(input.TargetLocale)
        });

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            throw new InvalidOperationException("translation_provider_unavailable", exception);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("translation_provider_unavailable");
            }

            DeepLTranslateResponse? payload;
            try
            {
                payload = await response.Content.ReadFromJsonAsync<DeepLTranslateResponse>(cancellationToken: cancellationToken);
            }
            catch (Exception exception) when (exception is System.Text.Json.JsonException)
            {
                throw new InvalidOperationException("translation_provider_unavailable", exception);
            }

            string? translated = payload?.Translations?.FirstOrDefault()?.Text;
            if (string.IsNullOrWhiteSpace(translated))
            {
                throw new InvalidOperationException("translation_provider_unavailable");
            }

            return translated;
        }
    }

    private string ResolveEndpoint()
    {
        if (!string.IsNullOrWhiteSpace(options.Endpoint))
        {
            return options.Endpoint;
        }

        bool freeTier = options.ApiKey!.EndsWith(":fx", StringComparison.OrdinalIgnoreCase);
        return freeTier ? "https://api-free.deepl.com/v2/translate" : "https://api.deepl.com/v2/translate";
    }

    // DeepL never accepts a regional source language (e.g. "fr-CA" -> "FR", not "FR-CA").
    public static string ToDeepLSourceLanguage(string bcp47Locale)
    {
        return bcp47Locale.Split('-')[0].ToUpperInvariant();
    }

    // DeepL only distinguishes regions for English and Portuguese target languages.
    public static string ToDeepLTargetLanguage(string bcp47Locale)
    {
        string[] parts = bcp47Locale.Split('-');
        string language = parts[0].ToUpperInvariant();
        if (parts.Length < 2)
        {
            return language switch
            {
                "EN" => "EN-US",
                "PT" => "PT-PT",
                _ => language
            };
        }

        string region = parts[1].ToUpperInvariant();
        return language switch
        {
            "EN" => region is "US" or "GB" ? $"EN-{region}" : "EN-US",
            "PT" => region is "BR" or "PT" ? $"PT-{region}" : "PT-PT",
            _ => language
        };
    }

    private sealed record DeepLTranslateResponse([property: JsonPropertyName("translations")] List<DeepLTranslation>? Translations);
    private sealed record DeepLTranslation(
        [property: JsonPropertyName("detected_source_language")] string? DetectedSourceLanguage,
        [property: JsonPropertyName("text")] string? Text);
}
