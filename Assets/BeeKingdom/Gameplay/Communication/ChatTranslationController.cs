using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Gameplay.Communication
{
    public enum TranslationDisplayMode { Original, Loading, Translated, Error }

    public sealed class TranslationDisplayState
    {
        public string MessageId { get; set; }
        public TranslationDisplayMode Mode { get; set; }
        public string VisibleText { get; set; }
        public string OriginalText { get; set; }
        public string SourceLocale { get; set; }
        public string TargetLocale { get; set; }
        public string ModelVersion { get; set; }
        public RemoteChatError Error { get; set; }
    }

    public sealed class ChatTranslationController
    {
        private readonly ServerChatProvider provider;
        private readonly Dictionary<string, TranslationDisplayState> states = new Dictionary<string, TranslationDisplayState>(StringComparer.Ordinal);

        public ChatTranslationController(ServerChatProvider provider) { this.provider = provider ?? throw new ArgumentNullException(nameof(provider)); }

        public TranslationDisplayState Get(RemoteChatMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (!states.TryGetValue(message.MessageId, out TranslationDisplayState state))
                states[message.MessageId] = state = Original(message);
            return state;
        }

        public async Task<TranslationDisplayState> TranslateAsync(RemoteChatMessage message, string targetLocale, string modelVersion, CancellationToken cancellationToken)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (string.IsNullOrWhiteSpace(targetLocale) || string.IsNullOrWhiteSpace(modelVersion)) throw new ArgumentException("Target locale and model version are required.");
            TranslationDisplayState state = Get(message);
            state.Mode = TranslationDisplayMode.Loading;
            state.VisibleText = message.OriginalBody;
            state.TargetLocale = targetLocale;
            state.ModelVersion = modelVersion;
            state.Error = RemoteChatError.None;
            try
            {
                MessageTranslation translation = await provider.TranslateAsync(message.MessageId, targetLocale, modelVersion, cancellationToken);
                if (translation == null || !string.Equals(translation.Status, "completed", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(translation.TranslatedText))
                    throw new RemoteChatTransportException(RemoteChatError.InvalidResponse, "Translation did not complete.");
                state.Mode = TranslationDisplayMode.Translated;
                state.VisibleText = translation.TranslatedText;
                state.SourceLocale = translation.SourceLocale;
                state.TargetLocale = translation.TargetLocale;
                state.ModelVersion = translation.ModelVersion;
                return state;
            }
            catch (OperationCanceledException)
            {
                states[message.MessageId] = Original(message);
                throw;
            }
            catch (RemoteChatTransportException exception)
            {
                state.Mode = TranslationDisplayMode.Error;
                state.VisibleText = message.OriginalBody;
                state.Error = exception.Error;
                return state;
            }
            catch (Exception)
            {
                state.Mode = TranslationDisplayMode.Error;
                state.VisibleText = message.OriginalBody;
                state.Error = RemoteChatError.Transport;
                return state;
            }
        }

        public TranslationDisplayState ShowOriginal(RemoteChatMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            return states[message.MessageId] = Original(message);
        }

        private static TranslationDisplayState Original(RemoteChatMessage message) => new TranslationDisplayState { MessageId = message.MessageId, Mode = TranslationDisplayMode.Original, VisibleText = message.OriginalBody, OriginalText = message.OriginalBody };
    }
}
