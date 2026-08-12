using BeeKingdom.Gameplay.Communication;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class LivingHiveChatLayoutTests
    {
        [Test]
        public void Portrait390x844ChatButtonAndFloatingWindowFit()
        {
            Rect button = HiveViewProductUiPresenter.ChatRailButtonRectForProof(true, 390f, 844f);
            Rect panel = HiveViewProductUiPresenter.MiniChatFloatingRectForProof(true, 390f, 844f);
            Assert.That(button.width, Is.GreaterThanOrEqualTo(40f));
            Assert.That(button.y, Is.GreaterThanOrEqualTo(844f - 78f));
            Assert.That(panel.x, Is.EqualTo(10f));
            Assert.That(panel.width, Is.EqualTo(370f));
            Assert.That(panel.y, Is.GreaterThanOrEqualTo(126f));
            Assert.That(panel.yMax, Is.LessThanOrEqualTo(button.y - 10f));
        }

        [Test]
        public void Landscape1600x900ChatButtonAndFloatingWindowFit()
        {
            Rect button = HiveViewProductUiPresenter.ChatRailButtonRectForProof(false, 1600f, 900f);
            Rect panel = HiveViewProductUiPresenter.MiniChatFloatingRectForProof(false, 1600f, 900f);
            Assert.That(button.width, Is.GreaterThanOrEqualTo(44f));
            Assert.That(panel.width, Is.LessThanOrEqualTo(520f));
            Assert.That(panel.height, Is.LessThanOrEqualTo(360f));
            Assert.That(panel.y, Is.GreaterThanOrEqualTo(112f));
            Assert.That(panel.yMax, Is.LessThanOrEqualTo(button.y - 10f));
        }

        [Test]
        public void DismissingOverlayDoesNotResetConfiguredChatRuntime()
        {
            HiveViewProductUiPresenter.CommunicationDraftForProof = "Message en cours";
            HiveViewProductUiPresenter.OpenCommunicationPanelForProof();
            bool configuredBefore = LivingHiveChatRuntime.IsConfigured;
            HiveViewProductUiPresenter.DismissCommunicationPanelForProof();
            Assert.That(HiveViewProductUiPresenter.CommunicationPanelOpenForProof, Is.False);
            Assert.That(LivingHiveChatRuntime.IsConfigured, Is.EqualTo(configuredBefore));
            Assert.That(HiveViewProductUiPresenter.CommunicationDraftForProof, Is.EqualTo("Message en cours"));
            HiveViewProductUiPresenter.ClearCommunicationDraftForSessionChange();
            Assert.That(HiveViewProductUiPresenter.CommunicationDraftForProof, Is.Empty);
        }

        [Test]
        public void MiniChatDefaultsToAutoResolvingToAllianceForAllianceMember()
        {
            var store = new MiniChatMemoryStore();
            try
            {
                HiveViewProductUiPresenter.ConfigureMobileComfortPreferencesStoreForTests(store);
                HiveViewProductUiPresenter.ReloadMobileComfortPreferencesForProof();
                Assert.That(HiveViewProductUiPresenter.MiniChatWatchModeForProof, Is.EqualTo("auto"));
                Assert.That(HiveViewProductUiPresenter.MiniChatResolvedChannelForProof(), Is.EqualTo("alliance"));
                Assert.That(HiveViewProductUiPresenter.MiniChatBlinkEnabledForProof, Is.True);
                Assert.That(HiveViewProductUiPresenter.MiniChatUnreadForProof("alliance"), Is.EqualTo(9));
                Assert.That(HiveViewProductUiPresenter.MiniChatUnreadForProof("world"), Is.EqualTo(3));
            }
            finally
            {
                RestoreMiniChatDefaults();
            }
        }

        [Test]
        public void MiniChatWatchModeCyclesAndPersists()
        {
            var store = new MiniChatMemoryStore();
            try
            {
                HiveViewProductUiPresenter.ConfigureMobileComfortPreferencesStoreForTests(store);
                HiveViewProductUiPresenter.ReloadMobileComfortPreferencesForProof();

                HiveViewProductUiPresenter.SetMiniChatWatchModeForProof("world");
                Assert.That(HiveViewProductUiPresenter.MiniChatResolvedChannelForProof(), Is.EqualTo("world"));

                HiveViewProductUiPresenter.ConfigureMobileComfortPreferencesStoreForTests(store);
                HiveViewProductUiPresenter.ReloadMobileComfortPreferencesForProof();
                Assert.That(HiveViewProductUiPresenter.MiniChatWatchModeForProof, Is.EqualTo("world"));

                HiveViewProductUiPresenter.SetMiniChatWatchModeForProof("auto");
                Assert.That(HiveViewProductUiPresenter.MiniChatResolvedChannelForProof(), Is.EqualTo("alliance"));

                HiveViewProductUiPresenter.SetMiniChatBlinkEnabledForProof(false);
                Assert.That(HiveViewProductUiPresenter.MiniChatBlinkEnabledForProof, Is.False);
                Assert.That(store.WriteCount, Is.GreaterThanOrEqualTo(2));
            }
            finally
            {
                RestoreMiniChatDefaults();
            }
        }

        [Test]
        public void MiniChatShowsLatestMessagesFromWatchedChannel()
        {
            string[] recent = HiveViewProductUiPresenter.MiniChatLastMessagesForProof("world");
            Assert.That(recent, Has.Length.GreaterThan(0));
            Assert.That(recent, Has.Length.LessThanOrEqualTo(8));
            foreach (string entry in recent) Assert.That(entry, Does.Contain("|"));
        }

        [Test]
        public void MiniChatFloatingStatePersistsAndUsesWatchedChannel()
        {
            var store = new MiniChatMemoryStore();
            try
            {
                HiveViewProductUiPresenter.ConfigureMobileComfortPreferencesStoreForTests(store);
                HiveViewProductUiPresenter.ReloadMobileComfortPreferencesForProof();
                HiveViewProductUiPresenter.SetMiniChatWatchModeForProof("world");
                HiveViewProductUiPresenter.SetMiniChatOpenForProof(true);
                Assert.That(HiveViewProductUiPresenter.MiniChatOpenForProof, Is.True);
                Assert.That(HiveViewProductUiPresenter.ChatSelectedChannelForProof, Is.EqualTo("world"));

                Rect worldButton = HiveViewProductUiPresenter.WorldMapChatButtonRectForProof(1600f, 900f);
                Assert.That(worldButton.y, Is.GreaterThanOrEqualTo(900f - 128f));
                Assert.That(worldButton.width, Is.LessThanOrEqualTo(190f));
                Rect worldFloating = HiveViewProductUiPresenter.MiniChatFloatingRectForProof(true, 390f, 844f, true);
                Assert.That(worldFloating.yMax, Is.LessThanOrEqualTo(844f - 190f - 8f - 56f - 8f));

                HiveViewProductUiPresenter.OpenChatScreenForProof();
                Assert.That(HiveViewProductUiPresenter.MiniChatOpenForProof, Is.True);
                HiveViewProductUiPresenter.CloseChatScreenForProof();

                HiveViewProductUiPresenter.ConfigureMobileComfortPreferencesStoreForTests(store);
                HiveViewProductUiPresenter.ReloadMobileComfortPreferencesForProof();
                Assert.That(HiveViewProductUiPresenter.MiniChatOpenForProof, Is.True);

                Rect panel = HiveViewProductUiPresenter.MiniChatFloatingRectForProof(false, 1600f, 900f);
                Assert.That(panel.y, Is.GreaterThanOrEqualTo(112f));
                Assert.That(panel.yMax, Is.LessThan(900f - 76f));
                HiveViewProductUiPresenter.SetMiniChatOpenForProof(false);
                Assert.That(HiveViewProductUiPresenter.MiniChatOpenForProof, Is.False);
            }
            finally
            {
                RestoreMiniChatDefaults();
            }
        }

        [Test]
        public void ChatEmojiInsertsAtCaretPosition()
        {
            Assert.That(HiveViewProductUiPresenter.ChatEmojiInsertAtForProof("Bonjour le monde", 7, "😊"), Is.EqualTo("Bonjour😊 le monde"));
            Assert.That(HiveViewProductUiPresenter.ChatEmojiInsertAtForProof("abc", 0, "🔥"), Is.EqualTo("🔥abc"));
            Assert.That(HiveViewProductUiPresenter.ChatEmojiInsertAtForProof("abc", 99, "🔥"), Is.EqualTo("abc🔥"));
            Assert.That(HiveViewProductUiPresenter.ChatEmojiInsertAtForProof(string.Empty, 0, "🐝"), Is.EqualTo("🐝"));
            Assert.That(HiveViewProductUiPresenter.ChatEmojiInsertAtForProof("abc", -3, "🔥"), Is.EqualTo("🔥abc"));
            Assert.That(HiveViewProductUiPresenter.ChatEmojiInsertAtForProof("abc", 1, string.Empty), Is.EqualTo("abc"));
        }

        [Test]
        public void ChatEmojiMultipleInsertionsBuildExpectedDraft()
        {
            string draft = HiveViewProductUiPresenter.ChatEmojiInsertAtForProof("Bonjour", 7, "😊");
            draft = HiveViewProductUiPresenter.ChatEmojiInsertAtForProof(draft, draft.Length, "🎉");
            Assert.That(draft, Is.EqualTo("Bonjour😊🎉"));
        }

        [Test]
        public void ChatEmojiRecentsPushFrontDedupeAndCap()
        {
            var store = new MiniChatMemoryStore();
            try
            {
                HiveViewProductUiPresenter.ConfigureMobileComfortPreferencesStoreForTests(store);
                HiveViewProductUiPresenter.ReloadMobileComfortPreferencesForProof();

                HiveViewProductUiPresenter.ChatEmojiPushRecentForProof("😊");
                HiveViewProductUiPresenter.ChatEmojiPushRecentForProof("🎉");
                HiveViewProductUiPresenter.ChatEmojiPushRecentForProof("😊");
                Assert.That(HiveViewProductUiPresenter.ChatEmojiRecentForProof(), Is.EqualTo(new[] { "😊", "🎉" }));

                for (int i = 0; i < 15; i++) HiveViewProductUiPresenter.ChatEmojiPushRecentForProof("x" + i);
                Assert.That(HiveViewProductUiPresenter.ChatEmojiRecentForProof(), Has.Length.EqualTo(12));
                Assert.That(HiveViewProductUiPresenter.ChatEmojiRecentForProof()[0], Is.EqualTo("x14"));
            }
            finally
            {
                RestoreMiniChatDefaults();
            }
        }

        [Test]
        public void ChatEmojiRecentsPersistAcrossReload()
        {
            var store = new MiniChatMemoryStore();
            try
            {
                HiveViewProductUiPresenter.ConfigureMobileComfortPreferencesStoreForTests(store);
                HiveViewProductUiPresenter.ReloadMobileComfortPreferencesForProof();

                HiveViewProductUiPresenter.ChatEmojiPushRecentForProof("🐝");
                HiveViewProductUiPresenter.ChatEmojiPushRecentForProof("🍯");

                HiveViewProductUiPresenter.ConfigureMobileComfortPreferencesStoreForTests(store);
                HiveViewProductUiPresenter.ReloadMobileComfortPreferencesForProof();
                Assert.That(HiveViewProductUiPresenter.ChatEmojiRecentForProof(), Is.EqualTo(new[] { "🍯", "🐝" }));
            }
            finally
            {
                RestoreMiniChatDefaults();
            }
        }

        [Test]
        public void ChatEmojiPanelSitsAboveComposerWithinScreen()
        {
            Rect panel = HiveViewProductUiPresenter.ChatEmojiPanelRectForProof(10f, 700f, 370f, 844f);
            Assert.That(panel.width, Is.EqualTo(370f));
            Assert.That(panel.height, Is.LessThanOrEqualTo(236f));
            Assert.That(panel.height, Is.GreaterThanOrEqualTo(150f));
            Assert.That(panel.yMax, Is.LessThanOrEqualTo(700f - 6f));
            Assert.That(panel.y, Is.GreaterThanOrEqualTo(0f));

            Rect shortScreen = HiveViewProductUiPresenter.ChatEmojiPanelRectForProof(10f, 290f, 370f, 390f);
            Assert.That(shortScreen.y, Is.GreaterThanOrEqualTo(0f));
        }

        [Test]
        public void ChatEmojiOpensOnRecentsAndSwitchesCategory()
        {
            HiveViewProductUiPresenter.SetChatEmojiCategoryForProof("smileys");
            HiveViewProductUiPresenter.SetChatEmojiPanelOpenForProof(true);
            Assert.That(HiveViewProductUiPresenter.ChatEmojiPanelOpenForProof, Is.True);
            Assert.That(HiveViewProductUiPresenter.ChatEmojiCategoryForProof, Is.EqualTo("recents"));

            HiveViewProductUiPresenter.SetChatEmojiCategoryForProof("objects");
            Assert.That(HiveViewProductUiPresenter.ChatEmojiCategoryForProof, Is.EqualTo("objects"));

            HiveViewProductUiPresenter.SetChatEmojiCategoryForProof("unknown");
            Assert.That(HiveViewProductUiPresenter.ChatEmojiCategoryForProof, Is.EqualTo("recents"));

            HiveViewProductUiPresenter.SetChatEmojiPanelOpenForProof(false);
            Assert.That(HiveViewProductUiPresenter.ChatEmojiPanelOpenForProof, Is.False);
        }

        [Test]
        public void ChatEmojiCatalogHasCategoriesAndEmptyBeeKingdom()
        {
            Assert.That(HiveViewProductUiPresenter.ChatEmojiCatalogCategoryCountForProof, Is.EqualTo(5));
            Assert.That(HiveViewProductUiPresenter.ChatEmojiCatalogEmojisForProof("smileys"), Has.Length.GreaterThan(0));
            Assert.That(HiveViewProductUiPresenter.ChatEmojiCatalogEmojisForProof("emotions"), Has.Length.GreaterThan(0));
            Assert.That(HiveViewProductUiPresenter.ChatEmojiCatalogEmojisForProof("gestures"), Has.Length.GreaterThan(0));
            Assert.That(HiveViewProductUiPresenter.ChatEmojiCatalogEmojisForProof("objects"), Has.Length.GreaterThan(0));
            Assert.That(HiveViewProductUiPresenter.ChatEmojiCatalogEmojisForProof("beekingdom"), Is.Empty);
            Assert.That(HiveViewProductUiPresenter.ChatEmojiCatalogEmojisForProof("recents"), Is.Empty);
        }

        private static void RestoreMiniChatDefaults()
        {
            HiveViewProductUiPresenter.ConfigureMobileComfortPreferencesStoreForTests(null);
            HiveViewProductUiPresenter.ReloadMobileComfortPreferencesForProof();
        }

        private sealed class MiniChatMemoryStore : IMobileComfortPreferencesStore
        {
            private string json = string.Empty;

            public int WriteCount { get; private set; }

            public string Read() => json;

            public void Write(string value)
            {
                json = value ?? string.Empty;
                WriteCount++;
            }

            public void Delete()
            {
                json = string.Empty;
            }
        }
    }
}
