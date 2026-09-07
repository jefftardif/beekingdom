using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Gameplay.Communication;
using NUnit.Framework;

namespace BeeKingdom.Playground.Editor
{
    // M059D-CL - "Envoyer"/Entree du compositeur de Chat Royal ("Nouvelle discussion" et
    // groupes) n'appelait jamais LivingHiveChatRuntime.SendAsync. Preuve historique (git log -S
    // sur toute l'historique du depot) : ChatSendCurrent() n'a jamais change depuis le tout
    // premier commit, jamais cable sur le vrai backend - contrairement a SendAllianceChatMessage
    // (le tiroir de chat Alliance, fonctionnel et inchange depuis M043Q-T), qui utilise deja ce
    // meme point d'entree. Le message "semblait" s'envoyer parce qu'il etait ajoute a une liste
    // locale, jamais transmis au serveur.
    //
    // Corrige en cablant ChatSendCurrent sur EXACTEMENT ce meme point d'entree deja fonctionnel
    // quand chatUsingServerData est vrai - aucun nouveau provider, aucun runtime parallele. Ces
    // tests prouvent le branchement au niveau de l'ecran (chose que ServerChatProviderTests, qui
    // teste uniquement ServerChatProvider/LivingHiveChatController en isolation, ne peut pas
    // couvrir) : une conversation reelle atteint bien le transport, une conversation demo garde
    // exactement son ancien comportement local, et les deux ne se dupliquent jamais.
    public sealed class ChatRoyalSendWiringTests
    {
        [TearDown]
        public async Task ResetChatRuntime()
        {
            await LivingHiveChatRuntime.ResetAsync();
            HiveViewProductUiPresenter.SetChatSendTestStateForProof(false, string.Empty);
        }

        [Test]
        public async Task SendingFromARealConversationReachesTheServerProviderNotTheLocalSimulator()
        {
            var rest = new FakeRest();
            rest.MessagePages.Enqueue(new RemoteMessagePage());
            var controller = new LivingHiveChatController(new ServerChatProvider(rest, new FakeSession()));
            await controller.SelectKnownConversationAsync("c1", "Discussion", "Private", CancellationToken.None);
            await LivingHiveChatRuntime.ReconfigureAsync(controller);

            HiveViewProductUiPresenter.SetChatSendTestStateForProof(true, "c1");
            int localCountBefore = HiveViewProductUiPresenter.ChatLocalMessageCountForProof("c1");

            HiveViewProductUiPresenter.ChatSendCurrentForProof("hello bob");

            Assert.That(rest.LastSendRequest, Is.Not.Null,
                "the real transport must receive the send request - this is exactly what was missing before the fix.");
            Assert.That(rest.LastSendRequest.Body, Is.EqualTo("hello bob"));
            Assert.That(HiveViewProductUiPresenter.ChatLocalMessageCountForProof("c1"), Is.EqualTo(localCountBefore),
                "a real conversation must never also get a duplicate local-simulator message.");
        }

        [Test]
        public void SendingFromADemoConversationKeepsTheOldLocalBehaviorAndNeverTouchesTheTransport()
        {
            // "alliance-general" is one of the built-in seeded demo conversations
            // (HiveViewProductUiPresenter.BuildChatConversations/BuildChatMessages) - it already
            // exists as a real key so the local add is actually persisted, not silently dropped
            // into a throwaway list for an unknown conversation id.
            const string demoConversationId = "alliance-general";
            var rest = new FakeRest();
            HiveViewProductUiPresenter.SetChatSendTestStateForProof(false, demoConversationId);
            int localCountBefore = HiveViewProductUiPresenter.ChatLocalMessageCountForProof(demoConversationId);

            HiveViewProductUiPresenter.ChatSendCurrentForProof("hello demo");

            Assert.That(HiveViewProductUiPresenter.ChatLocalMessageCountForProof(demoConversationId), Is.EqualTo(localCountBefore + 1),
                "the offline/demo mode must keep working exactly as before - no regression.");
            Assert.That(rest.LastSendRequest, Is.Null, "demo mode must never reach any transport.");
        }

        [Test]
        public async Task EmptyComposerTextSendsNothingAndDoesNotThrow()
        {
            var rest = new FakeRest();
            rest.MessagePages.Enqueue(new RemoteMessagePage());
            var controller = new LivingHiveChatController(new ServerChatProvider(rest, new FakeSession()));
            await controller.SelectKnownConversationAsync("c1", "Discussion", "Private", CancellationToken.None);
            await LivingHiveChatRuntime.ReconfigureAsync(controller);
            HiveViewProductUiPresenter.SetChatSendTestStateForProof(true, "c1");

            Assert.DoesNotThrow(() => HiveViewProductUiPresenter.ChatSendCurrentForProof("   "));
            Assert.That(rest.LastSendRequest, Is.Null);
        }

        // ------------------------------------------------------------------------ fixtures
        // Memes fixtures minimales que ServerChatProviderTests (FakeRest/FakeSession), reprises
        // ici car ce fichier vit dans un namespace/assembly de tests different et ne peut pas
        // reutiliser les classes privees imbriquees de cet autre fichier.

        private sealed class FakeSession : IChatSessionSource
        {
            public Task<ChatSession> GetSessionAsync(CancellationToken ct) => Task.FromResult(new ChatSession("p1", "test-token"));
        }

        private sealed class FakeRest : IChatRestTransport
        {
            public int StatusCode = 200;
            public RemoteSendMessageRequest LastSendRequest;
            public readonly Queue<RemoteMessagePage> MessagePages = new Queue<RemoteMessagePage>();
            public readonly RemoteMessagePage Page = new RemoteMessagePage();

            public Task<ChatTransportResponse<T>> SendAsync<T>(ChatTransportRequest request, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                object body = null;
                if (request.Path.Contains("/messages?") && request.Method == "GET")
                {
                    body = MessagePages.Count > 0 ? MessagePages.Dequeue() : Page;
                }
                else if (request.Path.EndsWith("/messages") && request.Method == "POST")
                {
                    LastSendRequest = (RemoteSendMessageRequest)request.Body;
                    string conversationId = Uri.UnescapeDataString(request.Path.Split('/')[4]);
                    body = new RemoteSendResult
                    {
                        Message = new RemoteChatMessage
                        {
                            ConversationId = conversationId,
                            MessageId = "m1",
                            Sequence = 1,
                            ClientRequestId = LastSendRequest.ClientRequestId,
                            OriginalBody = LastSendRequest.Body,
                            SenderId = "p1"
                        },
                        ServerSequence = 1
                    };
                }
                return Task.FromResult(new ChatTransportResponse<T> { StatusCode = StatusCode, Body = (T)body });
            }
        }
    }
}
