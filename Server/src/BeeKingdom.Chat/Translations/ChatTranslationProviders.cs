namespace BeeKingdom.Chat.Translations;

public sealed class UnavailableChatTranslationProvider : IChatTranslationProvider
{
    public Task<string> TranslateAsync(ChatTranslationInput input, CancellationToken cancellationToken)
        => throw new InvalidOperationException("translation_provider_unavailable");
}

public sealed class DelegateChatTranslationProvider(Func<ChatTranslationInput, CancellationToken, Task<string>> translate) : IChatTranslationProvider
{
    public Task<string> TranslateAsync(ChatTranslationInput input, CancellationToken cancellationToken) => translate(input, cancellationToken);
}
