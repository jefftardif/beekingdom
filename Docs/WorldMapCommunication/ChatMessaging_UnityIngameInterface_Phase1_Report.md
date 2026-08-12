# Bee Kingdom - Chat Messaging Unity Ingame Interface Phase 1

**Date:** 2026-07-16  
**Scope:** Unity ingame chat UI foundation  
**Live server:** unchanged  
**Unity scene changes:** none

## Summary

Phase 1 adds a reusable Unity `MonoBehaviour` chat panel backed by the existing `IChatProvider` abstraction.

The panel uses `LocalChatProvider` by default so the ingame interface can be validated inside any scene without requiring a production token, server credentials, or SignalR package integration.

## Files Added

- `Assets/BeeKingdom/Gameplay/Communication/ChatIngamePanel.cs`
- `Assets/BeeKingdom/Gameplay/Communication/ChatIngamePanel.cs.meta`

## Current Behavior

- Toggle overlay with `F9`.
- Displays four channel tabs:
  - `Alliance`
  - `Global`
  - `Prive`
  - `Dirigeants`
- Creates default local conversations through `IChatProvider`.
- Renders message history for the selected conversation.
- Sends messages through `IChatProvider.SendMessage`.
- Uses stable `ClientRequestId` generation per send attempt.
- Marks the rendered conversation read on a best-effort basis.
- Extracts simple mentions from `@playerId` tokens when the mentioned player is a participant.
- Shows provider, connection state, server capability flag, and last status/event line.

## Integration Instructions

To test in a Unity scene:

1. Create or select a persistent UI/game object.
2. Add component `BeeKingdom.Gameplay.Communication.ChatIngamePanel`.
3. Enter Play Mode.
4. Use `F9` to show/hide the panel.
5. Send messages in the local fixture channels.

No scene was modified in this phase, so the component must be attached manually or by a later scene builder pass.

## Provider Boundary

The panel depends on:

```csharp
IChatProvider
```

It does not depend directly on `LocalChatProvider` except for default self-bootstrapping when no provider is injected.

The future server client should call:

```csharp
chatIngamePanel.SetProvider(serverChatProvider, authenticatedPlayerId);
```

Expected next provider:

- REST auth/session from the existing game account flow;
- `GET /chat/v1/capabilities`;
- `POST /chat/v1/conversations`;
- `GET /chat/v1/conversations/{conversationId}/messages`;
- `POST /chat/v1/conversations/{conversationId}/messages`;
- `POST /chat/v1/conversations/{conversationId}/read`;
- SignalR or polling refresh behind the same `IChatProvider.Subscribe` event surface.

## Validation

Command run:

```text
dotnet build BeeKingdom.Gameplay.csproj
dotnet test BeeKingdom.Tests.csproj --no-restore
dotnet build BeeKingdom.Tests.csproj
```

Result:

- `BeeKingdom.Gameplay` build: PASS, 0 errors, 1 pre-existing warning in `Assets/BeeKingdom/Core/Config/GameConfigAsset.cs` about `configId` never assigned.
- `BeeKingdom.Tests` dotnet test: PASS exit code, no useful runner output from the Unity-generated project.
- `BeeKingdom.Tests` build: PASS, 0 errors, 19 pre-existing unassigned serialized-field warnings in config/service projects.

## Limits

- This is an immediate ingame interface foundation, not the final production visual pass.
- It uses IMGUI because existing playground/debug scenes use the same lightweight overlay pattern.
- No Unity scene or prefab was modified.
- No production credentials or test account passwords were added.
- No REST/SignalR Unity provider is included yet.
- No APK, PNG, Wave5, BearDen, or map assets were touched.

## Next Actions

1. Attach the component to the intended gameplay scene or add a dedicated scene builder step.
2. Implement `ServerChatProvider : IChatProvider` for `https://chat.dravii.com`.
3. Reuse the existing auth/session token instead of embedding credentials in Unity.
4. Add realtime transport:
   - preferred: SignalR Unity-compatible package if acceptable;
   - fallback: REST polling first, then raw WebSocket only if SignalR dependency is unsuitable.
5. Replace the IMGUI panel with production UI Toolkit/uGUI once the live provider contract is stable.
