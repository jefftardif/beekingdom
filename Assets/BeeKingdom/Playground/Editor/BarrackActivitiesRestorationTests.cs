using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BeeKingdom.Buildings.Interaction;
using BeeKingdom.Networking;
using NUnit.Framework;
using UnityEngine;
namespace BeeKingdom.Playground.Editor
{
    public sealed class BarrackActivitiesRestorationTests
    {
        private static string Presenter => File.ReadAllText(Path.Combine(Application.dataPath,
            "BeeKingdom/Playground/HiveViewProductUiPresenter.cs"));
        private static string Host => File.ReadAllText(Path.Combine(Application.dataPath,
            "BeeKingdom/Playground/HiveMapBarrackBootstrap.cs"));
        private static string ArmyHost => File.ReadAllText(Path.Combine(Application.dataPath,
            "BeeKingdom/Playground/HiveMapArmyBootstrap.cs"));

        [Test]
        public void RecoveredScreen_IsActivitiesStyledAndOffersTwoNamedTabs()
        {
            string source = Presenter;
            StringAssert.Contains("string[] ids = { \"training\", \"army\" }", source);
            StringAssert.Contains("ENTRAÎNEMENT", source);
            StringAssert.Contains("ARMÉES", source);
            StringAssert.Contains("DrawBarrackTabs(tabs, compact)", source);
            StringAssert.Contains("DrawOfficialBarrackContentActivities(content, compact)", source);
            StringAssert.Contains("DrawBarrackArmyTab(content, compact)", source);
            StringAssert.Contains("DrawAllianceActivitiesSurface(squadCard)", source);
            StringAssert.Contains("DrawAllianceActivitiesSurface(levelCard", source);
        }

        [Test]
        public void BuildingTap_StillUsesRealTrainingAndKeepsFastClaim()
        {
            StringAssert.Contains("BuildingTypes.Barrack", Host);
            StringAssert.Contains("TryClaimReadyTrainingOnTapForExternalHost()", Host);
            StringAssert.Contains("OpenBarrackOverlayForExternalHost()", Host);
            StringAssert.Contains("TickBarrackTrainingForExternalHost()", Host);
            StringAssert.Contains("RunOfficialDoctrineRecruitmentAction(officialSelectedTroopFamily)", Presenter);
            StringAssert.Contains("RefreshDoctrineRecruitmentIfDueOrJustFinished()", Presenter);
        }

        [Test]
        public void LegacyArmyEntry_OpensRecoveredArmyTabNotSecondModal()
        {
            StringAssert.Contains("BarrackArmyTabOpenForExternalHost", ArmyHost);
            StringAssert.Contains("OpenBarrackArmyTabForExternalHost()", ArmyHost);
            StringAssert.Contains("ModalOpenForExternalHost = false", ArmyHost);
            StringAssert.Contains("SquadReservationControllerForHiveMap", Presenter);
        }

        [Test]
        public void NewPatrol_RequiresChampionAndUsesServerProtectedCommit()
        {
            string source = Presenter;
            StringAssert.Contains("barrackArmyDraftChampionBeeId", source);
            StringAssert.Contains("championReady && !ctrl.IsBusy", source);
            StringAssert.Contains("new HiveSquadCompositionSnapshot", source);
            StringAssert.Contains("ctrl.Commit(", source);
            StringAssert.Contains("ChampionBeeIds", source);
            string client = File.ReadAllText(Path.Combine(Application.dataPath,
                "BeeKingdom/Networking/HivePerimeterSortieClient.cs"));
            StringAssert.Contains("ChampionBeeIds = championCopy", client);
            string controller = File.ReadAllText(Path.Combine(Application.dataPath,
                "BeeKingdom/Playground/HiveOfficialSquadReservationPresentation.cs"));
            StringAssert.Contains("string.IsNullOrWhiteSpace(champion)", controller);
            StringAssert.Contains("CommitToken(composition, champion)", controller);
        }

        [Test]
        public void ServerErrorIsNotAnEmptyOrEditablePatrol()
        {
            var model = HiveOfficialSquadReservationPresentation.Project(
                HiveSquadReservationScreenState.Error, null, true, "game.unavailable");
            Assert.That(model.HasReservation, Is.False);
            Assert.That(model.CanCommit(new HiveSquadCompositionSnapshot(12, 2, 0, 0)), Is.False);
            StringAssert.Contains("SERVICE DE PATROUILLE INDISPONIBLE", Presenter);
            StringAssert.Contains("model.State == HiveSquadReservationScreenState.Error", Presenter);
            StringAssert.Contains("if (pending) ctrl.Retry()", Presenter);
            StringAssert.Contains("else ctrl.Refresh()", Presenter);
        }

        [Test]
        public void WoundedOrAlreadyReservedBees_CannotBeReservedAgain()
        {
            var snapshot = Snapshot(20, 2);
            var state = HiveOfficialSquadReservationPresentation.Project(
                HiveSquadReservationScreenState.Ready, snapshot, true);
            Assert.That(state.RosterGuardians, Is.EqualTo(20));
            Assert.That(state.AvailableGuardians, Is.EqualTo(2));
            Assert.That(state.CanCommit(new HiveSquadCompositionSnapshot(12, 3, 0, 0)), Is.False);
            Assert.That(state.CanCommit(new HiveSquadCompositionSnapshot(12, 2, 0, 0)), Is.True);
        }

        [Test]
        public void ServerOwnedChampionIsDisplayedWithoutLocalFabrication()
        {
            var snapshot = Snapshot(12, 8);
            snapshot.ReservationId = "reservation-test";
            snapshot.Reserved["guardians"] = 4;
            snapshot.ChampionBeeIds = new List<string> { "zephyra", "zephyra" };
            var state = HiveOfficialSquadReservationPresentation.Project(
                HiveSquadReservationScreenState.Ready, snapshot, true);
            Assert.That(state.HasReservation, Is.True);
            CollectionAssert.AreEqual(new[] { "zephyra" }, state.ChampionBeeIds);
            snapshot.ChampionBeeIds = null;
            state = HiveOfficialSquadReservationPresentation.Project(
                HiveSquadReservationScreenState.Ready, snapshot, true);
            Assert.That(state.ChampionBeeIds, Is.Empty);
        }

        [Test]
        public void PhoneLayout_HasReachableChampionAndWorldMapNavigation()
        {
            StringAssert.Contains("GUI.BeginScrollView(content, barrackArmyScroll", Presenter);
            StringAssert.Contains("float innerHeight = Mathf.Max(content.height, 932f)", Presenter);
            StringAssert.Contains("championCard.yMax + 14f", Presenter);
            StringAssert.Contains("GUI.EndScrollView()", Presenter);
            StringAssert.Contains("CloseBarrackOverlayForExternalHost();", Presenter);
            StringAssert.Contains("TryOpenScene(SplashDevelopmentSceneConfig.WorldMapScenePath", Presenter);
        }

        [Test]
        public void OpeningBarrack_ResetsTrainingTabRatherThanShowingOldArmyDraft()
        {
            string source = Presenter;
            int begin = source.IndexOf("public static void OpenBarrackOverlayForExternalHost()", StringComparison.Ordinal);
            int end = source.IndexOf("public static void OpenBarrackArmyTabForExternalHost()",begin,StringComparison.Ordinal);
            Assert.That(begin, Is.GreaterThan(0));
            StringAssert.Contains("barrackSelectedTab = \"training\"",source.Substring(begin,end-begin));
            Assert.That(BuildingCatalog.GetByBuildingType(BuildingTypes.Barrack), Is.Not.Null);
        }

        [Test]
        public void FullscreenBarrack_BlocksUnderlayAndWorldInput()
        {
            var f = typeof(HiveViewProductUiPresenter).GetField(
                "barrackOverlayOpenForExternalHost",BindingFlags.NonPublic|BindingFlags.Static);
            bool previous = (bool)f.GetValue(null);
            try
            {
                f.SetValue(null,true);
                Assert.That(HiveMapOverlayInputGateBootstrap.IsAnyOverlayBlocking(),Is.True);
                Assert.That(HiveViewProductUiPresenter.AnyModalOpenForExternalHost,Is.True);
                var regions = new List<Rect>();
                HiveMapUiOcclusion.GetWorldPresentationVisibleRegions(regions);
                Assert.That(regions,Is.Empty);
            }
            finally { f.SetValue(null,previous); }
        }

        private static RemoteSquadReservationSnapshot Snapshot(long rosterGuardians,long availableGuardians)
        {
            return new RemoteSquadReservationSnapshot
            {
                PlayerId = Guid.NewGuid(), HiveId = Guid.NewGuid(),
                Capacity = 12, RosterRevision = 1, ReservationRevision = 0,
                Roster = new Dictionary<string,long> { ["guardians"]=rosterGuardians,["wingrunners"]=0,["darters"]=0 },
                Available = new Dictionary<string,long> { ["guardians"]=availableGuardians,["wingrunners"]=0,["darters"]=0 },
                Reserved = new Dictionary<string,long> { ["guardians"]=0,["wingrunners"]=0,["darters"]=0 },
                ChampionBeeIds = new List<string>()
            };
        }
    }
}