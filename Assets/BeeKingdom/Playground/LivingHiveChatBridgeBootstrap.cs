using System;
using System.Collections.Generic;
using System.Linq;
using BeeKingdom.Core.Integration;
using BeeKingdom.Gameplay.Communication;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground
{
    // Wires the real LivingHiveChatController (server-backed chat, requires an
    // authenticated MobileAccountSessionRuntimeBootstrap session) into
    // BeeKingdom.Core.Integration.LivingHiveChatBridge, so BeeKingdom.LivingHiveMenu's
    // Communication button (Assets/Experiments/Environment2D5D/LivingHiveMenu) can display
    // real messages and send real messages without needing a direct reference to the chat
    // assembly (see LivingHiveChatBridge.cs for why that reference isn't possible).
    //
    // Same auto-bootstrap strategy as BuildingRuntimeViewBootstrap / LivingHiveMenuRuntime:
    // a RuntimeInitializeOnLoadMethod creates this at runtime only when the active scene is
    // an "Environment2D5D*" scene, no scene wiring required.
    public sealed class LivingHiveChatBridgeBootstrap : MonoBehaviour
    {
        private const string RuntimeRootName = "LivingHive Chat Bridge Runtime";
        private const float PollIntervalSeconds = 1f;

        private bool openRequested;
        private float pollTimer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (!Application.isPlaying) return;
            Scene active = SceneManager.GetActiveScene();
            if (!IsEnvironmentScene(active)) return;
            if (FindFirstObjectByType<LivingHiveChatBridgeBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, active);
            root.AddComponent<LivingHiveChatBridgeBootstrap>();
        }

        private static bool IsEnvironmentScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return false;
            return scene.name.StartsWith("Environment2D5D", StringComparison.Ordinal);
        }

        private void Start()
        {
            LivingHiveChatBridge.SetSendHandler(body => LivingHiveChatRuntime.SendAsync(body));
            // Mini-chat / "CHAT ROYAL" overlay toggle (local demo data, matches
            // SandboxPlayground's Communication button) — separate from the server-backed
            // Send/Messages above. See HiveViewProductUiPresenter's "External host bridge"
            // region for why this reuse is safe (no EnsureSceneObjects() dependency).
            LivingHiveChatBridge.SetToggleOverlayHandler(HiveViewProductUiPresenter.ToggleCommunicationOverlayForExternalHost);
            // Chat-only activation (see TryActivateChatOnlyForActiveSession): nothing else
            // in the HiveMap scene triggers it, unlike the real LivingHive flow where
            // HiveViewProductUiPresenter.Start() calls the full
            // TryConfigureGameplayForActiveSession() after login. Deliberately NOT calling
            // that fuller method here: it also wires ~15 unrelated gameplay systems
            // (research, production, combat, daily round...) into HiveViewProductUiPresenter
            // that this scene never uses.
            MobileAccountSessionRuntimeBootstrap.TryActivateChatOnlyForActiveSession();
            PublishCurrentSnapshot();
        }

        private void Update()
        {
            pollTimer += Time.unscaledDeltaTime;
            if (pollTimer < PollIntervalSeconds) return;
            pollTimer = 0f;

            if (LivingHiveChatRuntime.IsConfigured && !openRequested)
            {
                openRequested = true;
                _ = LivingHiveChatRuntime.OpenAsync();
            }

            PublishCurrentSnapshot();
        }

        private void OnGUI()
        {
            HiveViewProductUiPresenter.DrawCommunicationOverlayForExternalHost(Screen.width < 900);
        }

        private static void PublishCurrentSnapshot()
        {
            if (!LivingHiveChatRuntime.IsConfigured)
            {
                LivingHiveChatBridge.PublishSnapshot(false, "Non connecte", Array.Empty<ChatBridgeMessage>());
                return;
            }

            LivingHiveChatSnapshot snapshot = LivingHiveChatRuntime.Snapshot;
            Guid ownPlayerId = MobileAccountSessionRuntimeBootstrap.GameplayPlayerId;
            List<ChatBridgeMessage> mapped = snapshot.Messages
                .Select(message => new ChatBridgeMessage(
                    string.IsNullOrWhiteSpace(message.SenderDisplayName) ? "?" : message.SenderDisplayName,
                    message.VisibleBody,
                    message.CreatedAt,
                    string.Equals(message.SenderPlayerId, ownPlayerId.ToString("D"), StringComparison.Ordinal)))
                .ToList();

            bool ready = snapshot.Status == LivingHiveChatStatus.Online || snapshot.Status == LivingHiveChatStatus.Polling;
            LivingHiveChatBridge.PublishSnapshot(ready, StatusLabel(snapshot.Status), mapped);
        }

        private static string StatusLabel(LivingHiveChatStatus status)
        {
            switch (status)
            {
                case LivingHiveChatStatus.Online: return "En ligne";
                case LivingHiveChatStatus.Polling: return "Connecte (mode secours)";
                case LivingHiveChatStatus.Connecting: return "Connexion...";
                case LivingHiveChatStatus.AuthenticationRequired: return "Connexion requise";
                case LivingHiveChatStatus.Offline: return "Hors ligne";
                case LivingHiveChatStatus.Unavailable: return "Indisponible";
                case LivingHiveChatStatus.Error: return "Erreur de connexion";
                default: return "Non connecte";
            }
        }
    }
}
