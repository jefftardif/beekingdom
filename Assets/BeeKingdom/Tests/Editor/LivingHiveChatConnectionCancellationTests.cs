using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Gameplay.Communication;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    // M059D-CL - preuve runtime (session CEO du 2026-09-07, stack trace complete) : OpenAsync ->
    // ConnectAsync -> NegotiateCapabilitiesAsync -> Send -> le transport observe l'annulation du
    // CancellationToken qui lui a ete transmis et leve TaskCanceledException. Dans cette
    // architecture, ce token n'est JAMAIS annule par un timeout reseau (celui-ci ressort en
    // ConnectionError -> RemoteChatTransportException, jamais par annulation) - seul un
    // proprietaire du token (LivingHiveChatRuntime.ReconfigureAsync/ResetAsync, ou le transition
    // token de LivingHiveChatSessionCoordinator) peut le faire, deliberement, pour superseder cette
    // ouverture par une autre. Root cause reelle : plusieurs bootstraps HiveMap independants
    // declenchaient chacun une reactivation complete pour le meme joueur (corrige separement dans
    // MobileAccountSessionRuntimeBootstrap.ActivateChatForActiveSession, qui reutilise desormais un
    // seul LivingHiveChatSessionBinding stable au lieu d'en fabriquer un nouveau a chaque appel).
    // Ces tests couvrent la garantie demandee ici : quand OpenAsync est neanmoins annule par son
    // propre token, il ne doit ni laisser un etat bloque, ni empecher une ouverture suivante de
    // reussir.
    public sealed class LivingHiveChatConnectionCancellationTests
    {
        [Test]
        public async Task OpenAsyncCancelledByItsOwnTokenLeavesAConherentOfflineStateInsteadOfStuckConnecting()
        {
            var rest = new FakeRest();
            var controller = new LivingHiveChatController(new ServerChatProvider(rest, new FakeSession()));

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.DoesNotThrowAsync(async () => await controller.OpenAsync(cts.Token),
                "a cancelled owner token must never fault OpenAsync - it is called fire-and-forget from " +
                "LivingHiveChatBridgeBootstrap.Update(), nothing observes an escaped exception.");

            LivingHiveChatSnapshot snapshot = controller.Snapshot();
            Assert.That(snapshot.Status, Is.EqualTo(LivingHiveChatStatus.Offline),
                "must never stay stuck at Connecting - that is exactly what made chat permanently unavailable for the rest of a CEO session.");
            Assert.That(snapshot.ErrorCode, Is.EqualTo("local_open_superseded"));
            Assert.That(rest.SendAttemptCount, Is.EqualTo(1),
                "the cancellation must be observed at the capabilities request itself, matching the CEO stack trace - not before the transport is even reached.");
            Assert.That(rest.CapabilitiesCallCount, Is.EqualTo(0),
                "a cancelled request must never be counted as a completed capabilities call.");
        }

        [Test]
        public async Task OpenAsyncSucceedsOnTheNextAttemptAfterAPriorCancellation()
        {
            var rest = new FakeRest();
            var controller = new LivingHiveChatController(new ServerChatProvider(rest, new FakeSession()));

            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();
                await controller.OpenAsync(cts.Token);
            }
            Assert.That(controller.Snapshot().Status, Is.EqualTo(LivingHiveChatStatus.Offline));

            await controller.OpenAsync(CancellationToken.None);

            LivingHiveChatSnapshot snapshot = controller.Snapshot();
            Assert.That(snapshot.Status, Is.EqualTo(LivingHiveChatStatus.Polling),
                "a fresh, non-cancelled attempt right after a cancelled one must connect normally - retry must actually be possible.");
            Assert.That(rest.CapabilitiesCallCount, Is.EqualTo(1),
                "exactly the second, non-cancelled attempt must have completed a capabilities call.");
            Assert.That(rest.SendAttemptCount, Is.EqualTo(3),
                "1 cancelled capabilities attempt + 1 successful capabilities call + 1 conversations load call - the cancelled attempt must not have poisoned anything that would block the next real attempt.");
        }

        // ------------------------------------------------------------------------ fixtures
        // Fixtures minimales locales (meme raison que ChatRoyalSendWiringTests : ce fichier vit
        // dans un namespace/assembly de tests different de ServerChatProviderTests et ne peut pas
        // reutiliser ses classes privees imbriquees).

        private sealed class FakeSession : IChatSessionSource
        {
            public Task<ChatSession> GetSessionAsync(CancellationToken ct) => Task.FromResult(new ChatSession("p1", "test-token"));
        }

        private sealed class FakeRest : IChatRestTransport
        {
            public int CapabilitiesCallCount;
            public int SendAttemptCount;

            public Task<ChatTransportResponse<T>> SendAsync<T>(ChatTransportRequest request, CancellationToken ct)
            {
                SendAttemptCount++;
                // Meme comportement que le vrai transport UnityWebRequestChatRestTransport :
                // observe l'annulation du token AVANT toute autre chose.
                ct.ThrowIfCancellationRequested();
                object body;
                if (request.Path == "/chat/v1/capabilities" && request.Method == "GET")
                {
                    CapabilitiesCallCount++;
                    body = new RemoteCapabilities
                    {
                        Provider = "server",
                        Server = true,
                        Realtime = false,
                        ProtocolVersion = "chat-v1",
                        Channels = new List<string> { "Alliance", "Server", "Private", "Leaders", "Group" },
                        Limits = new RemoteChatLimits
                        {
                            BodyMaxCharacters = 500,
                            MessagesPerMinutePerPlayer = 30,
                            MessagesPerTenSecondsPerConversation = 8,
                            PrivateConversationCreatesPerHour = 20,
                            MaxPrivateRecipients = 20
                        },
                        IdempotencyReceiptRetentionDays = 30
                    };
                }
                else if (request.Path.StartsWith("/chat/v1/conversations?", StringComparison.Ordinal) && request.Method == "GET")
                {
                    body = new RemoteConversationPage();
                }
                else
                {
                    body = default(T);
                }
                return Task.FromResult(new ChatTransportResponse<T>
                {
                    StatusCode = 200,
                    Body = (T)body,
                    CacheControl = "no-store, no-cache, max-age=0, must-revalidate",
                    AgeSeconds = 0
                });
            }
        }
    }
}
