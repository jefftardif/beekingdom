using System.Net;
using System.Text;
using BeeKingdom.Chat.Configuration;
using BeeKingdom.Chat.Translations;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Tests;

public sealed class DeepLChatTranslationProviderTests
{
    [Test]
    public async Task Successful_response_returns_translated_text_and_sends_expected_request()
    {
        FakeHandler handler = new(request =>
        {
            Assert.That(request.Headers.Authorization?.ToString(), Is.EqualTo("DeepL-Auth-Key test-key:fx"));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"translations":[{"detected_source_language":"FR","text":"Hello there"}]}""", Encoding.UTF8, "application/json")
            };
        });
        DeepLChatTranslationProvider provider = CreateProvider(handler, apiKey: "test-key:fx");

        string translated = await provider.TranslateAsync(new(Guid.NewGuid(), "Bonjour", "fr-CA", "en-US", "deepl-v2"), CancellationToken.None);

        Assert.That(translated, Is.EqualTo("Hello there"));
        Assert.That(handler.LastRequestUri, Is.EqualTo(new Uri("https://api-free.deepl.com/v2/translate")));
        string body = handler.LastRequestBody ?? string.Empty;
        Assert.Multiple(() =>
        {
            Assert.That(body, Does.Contain("source_lang=FR"));
            Assert.That(body, Does.Contain("target_lang=EN-US"));
        });
    }

    [Test]
    public void Non_success_status_code_throws_provider_unavailable()
    {
        FakeHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        DeepLChatTranslationProvider provider = CreateProvider(handler, apiKey: "test-key:fx");

        InvalidOperationException? error = Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.TranslateAsync(new(Guid.NewGuid(), "Bonjour", "fr-CA", "en-US", "deepl-v2"), CancellationToken.None));
        Assert.That(error!.Message, Is.EqualTo("translation_provider_unavailable"));
    }

    [Test]
    public void Empty_translation_array_throws_provider_unavailable()
    {
        FakeHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"translations":[]}""", Encoding.UTF8, "application/json")
        });
        DeepLChatTranslationProvider provider = CreateProvider(handler, apiKey: "test-key:fx");

        Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.TranslateAsync(new(Guid.NewGuid(), "Bonjour", "fr-CA", "en-US", "deepl-v2"), CancellationToken.None));
    }

    [Test]
    public async Task Pro_key_without_fx_suffix_uses_paid_endpoint()
    {
        FakeHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"translations":[{"detected_source_language":"FR","text":"Hi"}]}""", Encoding.UTF8, "application/json")
        });
        DeepLChatTranslationProvider provider = CreateProvider(handler, apiKey: "pro-key");

        await provider.TranslateAsync(new(Guid.NewGuid(), "Salut", "fr-CA", "en-US", "deepl-v2"), CancellationToken.None);

        Assert.That(handler.LastRequestUri, Is.EqualTo(new Uri("https://api.deepl.com/v2/translate")));
    }

    [TestCase("fr-CA", ExpectedResult = "FR")]
    [TestCase("en-US", ExpectedResult = "EN")]
    [TestCase("es-ES", ExpectedResult = "ES")]
    public string Source_language_never_carries_a_region(string locale) => DeepLChatTranslationProvider.ToDeepLSourceLanguage(locale);

    [TestCase("en-US", ExpectedResult = "EN-US")]
    [TestCase("en-GB", ExpectedResult = "EN-GB")]
    [TestCase("en", ExpectedResult = "EN-US")]
    [TestCase("pt-BR", ExpectedResult = "PT-BR")]
    [TestCase("pt", ExpectedResult = "PT-PT")]
    [TestCase("fr-CA", ExpectedResult = "FR")]
    [TestCase("es-ES", ExpectedResult = "ES")]
    public string Target_language_keeps_region_only_for_english_and_portuguese(string locale) => DeepLChatTranslationProvider.ToDeepLTargetLanguage(locale);

    private static DeepLChatTranslationProvider CreateProvider(FakeHandler handler, string apiKey)
    {
        HttpClient httpClient = new(handler);
        ChatTranslationOptions options = new() { Provider = "deepl", ApiKey = apiKey };
        return new DeepLChatTranslationProvider(httpClient, Options.Create(options));
    }

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            LastRequestBody = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return respond(request);
        }
    }
}
