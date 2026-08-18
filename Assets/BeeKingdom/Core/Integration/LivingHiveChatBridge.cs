using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BeeKingdom.Core.Integration
{
    // Cross-assembly bridge for the LivingHive chat feature. The real implementation
    // (Assets/BeeKingdom/Gameplay/Communication + Assets/BeeKingdom/Playground) lives in
    // Unity's default Assembly-CSharp, which no .asmdef package can reference at compile
    // time. BeeKingdom.Core has no dependencies of its own, so any package that already
    // references it (e.g. BeeKingdom.LivingHiveMenu) can read/act on chat state here
    // without needing a direct reference to the chat assembly.
    //
    // The chat bootstrap (default assembly) is the only writer: it calls PublishSnapshot
    // whenever the real LivingHiveChatController's state changes, and SetSendHandler once
    // to install the real send function. Everyone else only reads.
    public readonly struct ChatBridgeMessage
    {
        public readonly string SenderDisplayName;
        public readonly string Body;
        public readonly DateTimeOffset CreatedAt;
        public readonly bool IsOwnMessage;

        public ChatBridgeMessage(string senderDisplayName, string body, DateTimeOffset createdAt, bool isOwnMessage)
        {
            SenderDisplayName = senderDisplayName ?? string.Empty;
            Body = body ?? string.Empty;
            CreatedAt = createdAt;
            IsOwnMessage = isOwnMessage;
        }
    }

    public static class LivingHiveChatBridge
    {
        private static IReadOnlyList<ChatBridgeMessage> messages = Array.Empty<ChatBridgeMessage>();
        private static Func<string, Task> sendHandler;

        public static bool IsReady { get; private set; }
        public static string StatusText { get; private set; } = "Non connecte";
        public static IReadOnlyList<ChatBridgeMessage> Messages => messages;

        public static event Action Changed;

        public static void PublishSnapshot(bool ready, string statusText, IReadOnlyList<ChatBridgeMessage> newMessages)
        {
            IsReady = ready;
            StatusText = statusText ?? string.Empty;
            messages = newMessages ?? Array.Empty<ChatBridgeMessage>();
            Changed?.Invoke();
        }

        public static void SetSendHandler(Func<string, Task> handler)
        {
            sendHandler = handler;
        }

        // Fire-and-forget: callers (e.g. a rail button click handler) don't need to await.
        public static void Send(string body)
        {
            if (string.IsNullOrWhiteSpace(body) || sendHandler == null) return;
            _ = sendHandler(body);
        }

        // Toggle for HiveViewProductUiPresenter's own mini-chat / full "CHAT ROYAL" IMGUI
        // overlay (local demo data, matches SandboxPlayground's Communication button
        // exactly) — separate from the server-backed Send/Messages/StatusText above. The
        // Communication rail button calls ToggleOverlay(); the Playground bootstrap installs
        // the handler that forwards to HiveViewProductUiPresenter.
        private static Action toggleOverlayHandler;

        public static void SetToggleOverlayHandler(Action handler)
        {
            toggleOverlayHandler = handler;
        }

        public static void ToggleOverlay()
        {
            toggleOverlayHandler?.Invoke();
        }
    }
}
