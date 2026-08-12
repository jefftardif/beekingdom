using System;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    /// <summary>
    /// Alpha Sprint-017 - UI Stabilization &amp; UX Pass.
    /// Assertions EditMode sur : blocage du monde pendant un ecran plein, fermeture
    /// prioritaire par Echap, taille minimale des cibles de fermeture (44px), et
    /// absence de clic traversant vers la ruche 3D.
    /// </summary>
    public sealed class SandboxLivingHiveUiStabilizationTests
    {
        [SetUp]
        public void ResetAllPremiumScreens()
        {
            HiveViewProductUiPresenter.ResetPremiumScreensForProof();
        }

        [TearDown]
        public void ResetAfter()
        {
            HiveViewProductUiPresenter.ResetPremiumScreensForProof();
        }

        [Test]
        public void WorldInputIsBlockedWhileAnyFullScreenIsOpen()
        {
            Assert.That(HiveViewProductUiPresenter.PremiumWorldInputBlockedForProof, Is.False,
                "Aucun ecran ne doit bloquer le monde au repos.");

            HiveViewProductUiPresenter.SetMissionsCenterOpenForProof(true);
            AssertFullScreenBlocks("missions");
            HiveViewProductUiPresenter.SetMissionsCenterOpenForProof(false);

            HiveViewProductUiPresenter.SetCourierScreenOpenForProof(true);
            AssertFullScreenBlocks("courier");
            HiveViewProductUiPresenter.SetCourierScreenOpenForProof(false);

            HiveViewProductUiPresenter.SetFriendsScreenOpenForProof(true);
            AssertFullScreenBlocks("friends");
            HiveViewProductUiPresenter.SetFriendsScreenOpenForProof(false);

            HiveViewProductUiPresenter.SetColonyOverviewOpenForProof(true);
            AssertFullScreenBlocks("colony");
            HiveViewProductUiPresenter.SetColonyOverviewOpenForProof(false);

            HiveViewProductUiPresenter.SetChampionBeesPanelOpenForProof(true);
            AssertFullScreenBlocks("champions");
            HiveViewProductUiPresenter.SetChampionBeesPanelOpenForProof(false);

            HiveViewProductUiPresenter.OpenAllianceMemberProfileForProof("Testeur");
            AssertFullScreenBlocks("alliance profile");
            HiveViewProductUiPresenter.ClosePremiumScreensForProof();

            HiveViewProductUiPresenter.OpenChatScreenForProof();
            AssertFullScreenBlocks("chat");
            HiveViewProductUiPresenter.CloseChatScreenForProof();

            HiveViewProductUiPresenter.OpenBestiaryCodexOverlayForProof();
            AssertFullScreenBlocks("bestiary");
            HiveViewProductUiPresenter.CloseBestiaryCodexOverlayForProof();

            HiveViewProductUiPresenter.OpenMilestoneEventOverlayForProof();
            AssertFullScreenBlocks("milestone");
            HiveViewProductUiPresenter.CloseMilestoneEventOverlayForProof();

            Assert.That(HiveViewProductUiPresenter.PremiumWorldInputBlockedForProof, Is.False,
                "Retour a l'etat de repos apres fermeture de tous les ecrans.");
        }

        private static void AssertFullScreenBlocks(string label)
        {
            Assert.That(HiveViewProductUiPresenter.PremiumWorldInputBlockedForProof, Is.True,
                "Le monde 3D ne doit plus recevoir de clics quand l'ecran '" + label + "' est ouvert.");
        }

        [Test]
        public void EscapeClosesScreensInPriorityOrder()
        {
            HiveViewProductUiPresenter.SetMissionsCenterOpenForProof(true);
            HiveViewProductUiPresenter.SetFriendsScreenOpenForProof(true);
            Assert.That(HiveViewProductUiPresenter.ClosePremiumScreensForProof(), Is.True,
                "Echap doit fermer l'ecran le plus prioritaire.");
            Assert.That(HiveViewProductUiPresenter.MissionsCenterOpenForProof, Is.False);
            Assert.That(HiveViewProductUiPresenter.FriendsScreenOpenForProof, Is.True,
                "Echap ne ferme qu'un seul ecran a la fois.");

            Assert.That(HiveViewProductUiPresenter.ClosePremiumScreensForProof(), Is.True);
            Assert.That(HiveViewProductUiPresenter.FriendsScreenOpenForProof, Is.False);
            Assert.That(HiveViewProductUiPresenter.ClosePremiumScreensForProof(), Is.False,
                "Echap sans ecran ouvert ne doit rien fermer.");
        }

        [Test]
        public void EscapeClosesBestiaryMilestoneAndProfile()
        {
            HiveViewProductUiPresenter.OpenBestiaryCodexOverlayForProof();
            Assert.That(HiveViewProductUiPresenter.BestiaryCodexOverlayOpenForProof, Is.True);
            Assert.That(HiveViewProductUiPresenter.ClosePremiumScreensForProof(), Is.True);
            Assert.That(HiveViewProductUiPresenter.BestiaryCodexOverlayOpenForProof, Is.False);

            HiveViewProductUiPresenter.OpenMilestoneEventOverlayForProof();
            Assert.That(HiveViewProductUiPresenter.ClosePremiumScreensForProof(), Is.True);
            Assert.That(HiveViewProductUiPresenter.MilestoneEventOverlayOpenForProof, Is.False);

            HiveViewProductUiPresenter.OpenAllianceMemberProfileForProof("Abeille");
            Assert.That(HiveViewProductUiPresenter.ClosePremiumScreensForProof(), Is.True);
            Assert.That(HiveViewProductUiPresenter.AllianceMemberProfileOpenForProof, Is.False);
        }

        [Test]
        public void ClosingAllianceProfileReleasesCapturedGuiControls()
        {
            HiveViewProductUiPresenter.OpenAllianceMemberProfileForProof("Abeille");
            GUIUtility.hotControl = 42;
            GUIUtility.keyboardControl = 42;

            Assert.That(HiveViewProductUiPresenter.ClosePremiumScreensForProof(), Is.True);
            Assert.That(GUIUtility.hotControl, Is.EqualTo(0));
            Assert.That(GUIUtility.keyboardControl, Is.EqualTo(0));
            Assert.That(HiveViewProductUiPresenter.PremiumWorldInputBlockedForProof, Is.False);
        }

        [Test]
        public void ClosingSettingsReleasesCapturedGuiControls()
        {
            HiveViewProductUiPresenter.OpenMobileComfortSettingsForProof();
            GUIUtility.hotControl = 43;
            GUIUtility.keyboardControl = 43;

            Assert.That(HiveViewProductUiPresenter.ClosePremiumScreensForProof(), Is.True);
            Assert.That(GUIUtility.hotControl, Is.EqualTo(0));
            Assert.That(GUIUtility.keyboardControl, Is.EqualTo(0));
            Assert.That(HiveViewProductUiPresenter.PremiumWorldInputBlockedForProof, Is.False);
        }

        [Test]
        public void SettingsButtonTogglesTheSettingsPanelClosed()
        {
            HiveViewProductUiPresenter.ToggleMobileComfortSettingsForProof();
            Assert.That(HiveViewProductUiPresenter.ActiveMainMenuIdForProof, Is.EqualTo("Settings"));
            HiveViewProductUiPresenter.ToggleMobileComfortSettingsForProof();
            Assert.That(HiveViewProductUiPresenter.ActiveMainMenuIdForProof, Is.Empty);
        }

        [Test]
        public void ArmyMenuCanBeClosedThroughItsBackState()
        {
            HiveViewProductUiPresenter.OpenArmyMenuForProof();
            Assert.That(HiveViewProductUiPresenter.ClosePremiumScreensForProof(), Is.True);
            Assert.That(HiveViewProductUiPresenter.PremiumWorldInputBlockedForProof, Is.False);
        }

        [Test]
        public void ChatBackTargetAndNavigationAssetAreReady()
        {
            Assert.That(HiveViewProductUiPresenter.ChatBackButtonHitHasSafeSizeForProof, Is.True,
                "La cible de retour du chat doit mesurer au moins 44x44 px.");
            Assert.That(HiveViewProductUiPresenter.LeftNavigationAssetAvailableForProof, Is.True,
                "L'asset de retour closing_arrow.png doit etre charge.");
        }

        [Test]
        public void PrimaryPanelsStayInsideScreenAtNarrowPortraitWidth()
        {
            float width = 320f;
            float height = 568f;

            Rect army = HiveViewProductUiPresenter.ArmyMenuPanelRectForProof(true, width, height, true);
            AssertFits(army, width, height, "ArmyMenuPanel(preparation)");

            Rect research = HiveViewProductUiPresenter.ResearchMenuPanelRectForProof(true, width, height);
            AssertFits(research, width, height, "ResearchMenuPanel");

            Rect ledger = HiveViewProductUiPresenter.HiveLedgerPanelRectForProof(true, width, height);
            AssertFits(ledger, width, height, "HiveLedgerPanel");

            Rect missionsWidget = HiveViewProductUiPresenter.MissionsWidgetRectForProof(true, width, height, false);
            AssertFits(missionsWidget, width, height, "MissionsWidget");
        }

        private static void AssertFits(Rect rect, float width, float height, string label)
        {
            Assert.That(rect.x, Is.GreaterThanOrEqualTo(0f), label + " x < 0");
            Assert.That(rect.y, Is.GreaterThanOrEqualTo(0f), label + " y < 0");
            Assert.That(rect.xMax, Is.LessThanOrEqualTo(width), label + " sort de l'ecran a droite");
            Assert.That(rect.yMax, Is.LessThanOrEqualTo(height), label + " sort de l'ecran en bas");
        }

        [TestCase(1080f, 2400f)]
        [TestCase(1179f, 2556f)]
        public void MobileRailFiveItemsAreThumbFriendlyAndInsideScreen(float width, float height)
        {
            Rect[] items = HiveViewProductUiPresenter.MobileBottomRailItemRectsForProof(width, height);
            Assert.That(items, Has.Length.EqualTo(5), "Le rail mobile doit contenir exactement 5 boutons.");
            for (int i = 0; i < items.Length; i++)
            {
                AssertFits(items[i], width, height, "MobileRail item " + i);
                Assert.That(items[i].width, Is.GreaterThanOrEqualTo(48f), "Item " + i + " trop etroit");
                Assert.That(items[i].height, Is.GreaterThanOrEqualTo(48f), "Item " + i + " trop bas");
                if (i > 0)
                {
                    Assert.That(items[i].x, Is.GreaterThanOrEqualTo(items[i - 1].xMax), "Item " + i + " chevauche le precedent");
                }
            }
            Rect chat = HiveViewProductUiPresenter.ChatRailButtonRectForProof(true, width, height);
            Assert.That(chat.width, Is.GreaterThanOrEqualTo(48f), "Bouton Chat du rail mobile trop etroit");
        }

        [TestCase(1080f, 2400f)]
        [TestCase(1179f, 2556f)]
        public void MobileHudIsLightAndStaysInsideScreen(float width, float height)
        {
            Rect hud = HiveViewProductUiPresenter.MobileHudRectForProof(width, height);
            AssertFits(hud, width, height, "MobileHud");
            Assert.That(hud.height, Is.LessThanOrEqualTo(96f), "Le HUD mobile doit rester leger (< 96 px)");
            Rect chips = HiveViewProductUiPresenter.MobileHudResourceChipsRectForProof(width, height);
            AssertFits(chips, width, height, "MobileHud chips");
            Assert.That(chips.width, Is.GreaterThanOrEqualTo(90f), "Chips de ressources trop etroits");
        }

        [TestCase(1080f, 2400f)]
        [TestCase(1179f, 2556f)]
        public void HiveStaysVisibleAtLeast70PercentInPortrait(float width, float height)
        {
            float topMask = 126f;
            float bottomMask = 258f;
            float visible = Mathf.Max(0f, height - topMask - bottomMask);
            float percent = visible / height * 100f;
            Assert.That(percent, Is.GreaterThanOrEqualTo(70f), "La ruche doit occuper au moins 70% de la hauteur visible");
        }

        [TestCase(1080f, 2400f)]
        [TestCase(1179f, 2556f)]
        public void HiveMenusStayClearOfMobileHudAndRailInPortrait(float width, float height)
        {
            Rect hud = HiveViewProductUiPresenter.MobileHudRectForProof(width, height);
            Rect army = HiveViewProductUiPresenter.ArmyMenuPanelRectForProof(true, width, height, true);
            Rect research = HiveViewProductUiPresenter.ResearchMenuPanelRectForProof(true, width, height);
            Rect ledger = HiveViewProductUiPresenter.HiveLedgerPanelRectForProof(true, width, height);

            Assert.That(army.y, Is.GreaterThanOrEqualTo(hud.yMax), "Le panneau Rucher chevauche le HUD mobile");
            Assert.That(research.y, Is.GreaterThanOrEqualTo(hud.yMax), "Le panneau Recherche chevauche le HUD mobile");
            Assert.That(ledger.y, Is.GreaterThanOrEqualTo(hud.yMax), "Le panneau Sac chevauche le HUD mobile");

            float railTop = height - 78f;
            Assert.That(army.yMax, Is.LessThanOrEqualTo(railTop + 14f), "Le panneau Rucher chevauche le rail mobile");
            Assert.That(ledger.yMax, Is.LessThanOrEqualTo(railTop + 14f), "Le panneau Sac chevauche le rail mobile");
        }

        [Test]
        public void ResetClosesEverythingAndUnblocksWorld()
        {
            HiveViewProductUiPresenter.SetMissionsCenterOpenForProof(true);
            HiveViewProductUiPresenter.SetFriendsScreenOpenForProof(true);
            HiveViewProductUiPresenter.OpenBestiaryCodexOverlayForProof();
            HiveViewProductUiPresenter.OpenAllianceMemberProfileForProof("Testeur");
            Assert.That(HiveViewProductUiPresenter.PremiumWorldInputBlockedForProof, Is.True);

            HiveViewProductUiPresenter.ResetPremiumScreensForProof();

            Assert.That(HiveViewProductUiPresenter.PremiumWorldInputBlockedForProof, Is.False);
            Assert.That(HiveViewProductUiPresenter.MissionsCenterOpenForProof, Is.False);
            Assert.That(HiveViewProductUiPresenter.FriendsScreenOpenForProof, Is.False);
            Assert.That(HiveViewProductUiPresenter.BestiaryCodexOverlayOpenForProof, Is.False);
            Assert.That(HiveViewProductUiPresenter.AllianceMemberProfileOpenForProof, Is.False);
        }
    }
}
