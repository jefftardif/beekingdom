using System;
using System.Collections.Generic;
using System.Reflection;
using BeeKingdom.Localization;
using BeeKingdom.Networking;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeeKingdom.Playground.Editor
{
    public sealed class HiveMapActivitiesPremiumTests
    {
        private GameObject root;
        private HiveMapActivitiesBootstrap view;
        private string previousLocale;

        [SetUp]
        public void SetUp()
        {
            previousLocale = BeeLocalization.CurrentLocale;
            root = new GameObject("Activities test");
            view = root.AddComponent<HiveMapActivitiesBootstrap>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(root);
            BeeLocalization.SetLocale(previousLocale);
        }

        [TestCase("fr-CA")]
        [TestCase("en-US")]
        public void TechnicalKeysNeverBecomePlayerCopy(string locale)
        {
            BeeLocalization.SetLocale(locale);
            foreach (string key in new[] { "building_upgrade", "troop_recruit", "world_resource", "strategic_path", "vip_tier", "future_unknown_key", "q1_building_upgrade", "q5_world_resource_collect" })
            {
                string label = (string)Call("ObjectiveTitle", key);
                Assert.That(label, Is.Not.Empty);
                Assert.That(label, Does.Not.Contain("_"));
                Assert.That(label, Is.Not.EqualTo(key));
            }
            Assert.That(Call("ResourceTitle", "future_currency"), Is.Not.EqualTo("future_currency"));
        }

        [TestCase(false, false, true)]
        [TestCase(true, false, false)]
        [TestCase(false, true, false)]
        public void DailyClaimButtonFollowsServerAvailability(bool busy, bool claimed, bool enabled)
        {
            Call("BuildShell", Screen.safeArea);
            var model = Daily(claimed);
            Call("DrawDaily", root.transform, 0f, 0f, 350f, model, busy);
            Button[] buttons = root.GetComponentsInChildren<Button>();
            Assert.That(buttons[buttons.Length - 1].interactable, Is.EqualTo(enabled));
        }

        [TestCase("fr-CA", 310f)]
        [TestCase("en-US", 390f)]
        public void QuestRewardsAndStatesRemainReadableOnNarrowCards(string locale, float width)
        {
            BeeLocalization.SetLocale(locale);
            Call("BuildShell", Screen.safeArea);
            Call("DrawQuests", root.transform, 0f, 0f, width, Quests(), false);
            Canvas.ForceUpdateCanvases();
            foreach (TextMeshProUGUI label in root.GetComponentsInChildren<TextMeshProUGUI>())
            {
                label.ForceMeshUpdate();
                Assert.That(label.text, Does.Not.Contain("q1_"));
                Assert.That(label.text, Does.Not.Contain("honey"));
                Assert.That(label.preferredHeight, Is.LessThanOrEqualTo(label.rectTransform.rect.height + 4), label.text);
            }
        }

        [TestCase(390, 844, 0, 34, 390, 766, 1)]
        [TestCase(844, 390, 44, 21, 756, 369, 1)]
        [TestCase(1920, 1080, 80, 30, 1760, 1020, 2)]
        public void SafeAreaIncludesEveryInset(float width, float height, float x, float y, float safeWidth, float safeHeight, float scale)
        {
            Rect rect = (Rect)Call("SafeCanvasRect", new Vector2(width, height), new Rect(x, y, safeWidth, safeHeight), scale);
            Assert.That(rect.x * scale, Is.EqualTo(x));
            Assert.That(rect.y * scale, Is.EqualTo(height - y - safeHeight));
            Assert.That(rect.width * scale, Is.EqualTo(safeWidth));
            Assert.That(rect.height * scale, Is.EqualTo(safeHeight));
        }

        [Test]
        public void OfflineDailyCannotClaim()
        {
            Call("BuildShell", Screen.safeArea);
            Call("DrawDaily", root.transform, 0f, 0f, 350f,
                HiveDailyRoundPresentation.OfflineReadOnly(DailySnapshot(), DateTimeOffset.UtcNow, true), false);
            Button[] buttons = root.GetComponentsInChildren<Button>();
            Assert.That(buttons[buttons.Length - 1].interactable, Is.False);
        }

        [TestCase(1280, 720)]
        [TestCase(1920, 1080)]
        [TestCase(2560, 1440)]
        public void DesktopKeepsTwoActivityColumns(float width, float height)
        {
            float scale = (float)Call("CanvasScale", new Vector2(width, height));
            Assert.That(width / scale, Is.GreaterThanOrEqualTo(740));
        }

        [TestCase(390, 844)]
        [TestCase(1080, 2340)]
        public void PortraitKeepsReadableTouchLayout(float width, float height)
        {
            float scale = (float)Call("CanvasScale", new Vector2(width, height));
            Assert.That(width / scale, Is.EqualTo(390).Within(1));
        }

        [Test]
        public void FullscreenShellHasScrollNavigationAndBlocksBackground()
        {
            Call("BuildShell", Screen.safeArea);
            Canvas canvas = root.GetComponentInChildren<Canvas>();
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(canvas.GetComponent<GraphicRaycaster>(), Is.Not.Null);
            ScrollRect scroll = root.GetComponentInChildren<ScrollRect>();
            Assert.That(scroll.verticalScrollbar, Is.Not.Null);
            Assert.That(scroll.horizontal, Is.False);
            Assert.That(canvas.transform.Find("Backdrop").GetComponent<RawImage>().raycastTarget, Is.True);
        }

        [TestCase(true, false, true)]
        [TestCase(true, true, false)]
        [TestCase(false, false, false)]
        public void EventClaimUsesModelAndBusyGuard(bool canClaim, bool busy, bool expected)
        {
            Call("BuildShell", Screen.safeArea);
            Call("DrawEvent", root.transform, 0f, 0f, 350f,
                new HiveMilestoneEventScreenModel { State = HiveMilestoneEventScreenState.Ready, CanClaim = canClaim }, busy);
            Button[] buttons = root.GetComponentsInChildren<Button>();
            Assert.That(buttons[buttons.Length - 1].interactable, Is.EqualTo(expected));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void KingdomClaimButtonsRespectIndividualEligibilityAndBusy(bool busy)
        {
            Call("BuildShell", Screen.safeArea);
            Call("DrawQuests", root.transform, 0f, 0f, 900f, Quests(), busy);
            Button[] buttons = root.GetComponentsInChildren<Button>();
            Assert.That(buttons[buttons.Length - 3].interactable, Is.False);
            Assert.That(buttons[buttons.Length - 2].interactable, Is.EqualTo(!busy));
            Assert.That(buttons[buttons.Length - 1].interactable, Is.False);
        }

        [Test]
        public void PremiumArtworkShipsWithTheScreen()
        {
            foreach (string key in new[] { "quests", "honey", "pollen", "build", "map", "alliance", "queen", "bees", "book", "wax", "propolis" })
                Assert.That(Resources.Load<Texture2D>("PremiumBeeIcons/" + key), Is.Not.Null, key);
            Assert.That(Resources.Load<Texture2D>("PremiumBeeReference/BuildingBanners/alliance_future_hall"), Is.Not.Null);
            Assert.That(Resources.Load<TMP_FontAsset>("Cinzel-Regular SDF"), Is.Not.Null);
        }

        internal static HiveDailyRoundScreenModel Daily(bool claimed = false)
        {
            return HiveDailyRoundPresentation.Ready(DailySnapshot(claimed), true);
        }

        internal static RemoteHiveDailyRoundSnapshot DailySnapshot(bool claimed = false)
        {
            return new RemoteHiveDailyRoundSnapshot
            {
                PlayerId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                HiveId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                ContractVersion = HiveDailyRoundClient.ContractVersion,
                DayUtc = DateTimeOffset.UtcNow.Date,
                NextResetUtc = DateTimeOffset.UtcNow.Date.AddDays(1),
                ServerTimeUtc = DateTimeOffset.UtcNow,
                Revision = 1,
                Facts = new Dictionary<string, bool> { { HiveDailyRoundClient.CollectionFact, true }, { HiveDailyRoundClient.OperationFact, true }, { HiveDailyRoundClient.SnapshotFact, true } },
                CompletedCount = 3,
                HoneyReward = HiveDailyRoundClient.HoneyReward,
                PollenReward = HiveDailyRoundClient.PollenReward,
                ClaimAvailable = !claimed,
                ClaimedAtUtc = claimed ? DateTimeOffset.UtcNow : (DateTimeOffset?)null
            };
        }

        internal static QuestChainScreenModel Quests() => new QuestChainScreenModel
        {
            State = QuestChainScreenState.Ready,
            Objectives = new[]
            {
                new RemoteQuestChainObjective { ObjectiveKey = "q1_building_upgrade", Done = true, Claimed = true, RewardResourceKey = "honey", RewardAmount = 500 },
                new RemoteQuestChainObjective { ObjectiveKey = "q2_troop_recruit", Done = true, CanClaim = true, RewardResourceKey = "pollen", RewardAmount = 250 },
                new RemoteQuestChainObjective { ObjectiveKey = "q3_research_complete", RewardResourceKey = "honey", RewardAmount = 500 }
            }
        };

        private object Call(string name, params object[] args) => typeof(HiveMapActivitiesBootstrap)
            .GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Invoke(view, args);
    }
}
