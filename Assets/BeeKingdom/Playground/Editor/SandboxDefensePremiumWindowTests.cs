using System;
using System.Collections.Generic;
using BeeKingdom.Localization;
using BeeKingdom.Networking;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxDefensePremiumWindowTests
    {
        private static RemoteSquadReservationSnapshot Squad(
            long guardians = 40, long wingrunners = 54, long darters = 16)
        {
            return new RemoteSquadReservationSnapshot
            {
                PlayerId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                HiveId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                RosterRevision = 4,
                ReservationRevision = 2,
                Capacity = 36,
                Roster = new Dictionary<string, long>
                {
                    ["guardians"] = guardians,
                    ["wingrunners"] = wingrunners,
                    ["darters"] = darters
                },
                Available = new Dictionary<string, long>
                {
                    ["guardians"] = guardians - 32,
                    ["wingrunners"] = wingrunners - 4,
                    ["darters"] = darters
                },
                Reserved = new Dictionary<string, long>
                {
                    ["guardians"] = 32,
                    ["wingrunners"] = 4,
                    ["darters"] = 0
                },
                ReservationId = "official-reservation-test",
                ChampionBeeIds = new List<string> { "zephyra" }
            };
        }

        [Test]
        public void MissingOfficialDataNeverMasqueradesAsZeroDefenseRoster()
        {
            HiveDefenseOverviewSnapshot state = HiveDefenseOverviewPresentation.Project(
                null, null);
            Assert.That(state.RosterKnown, Is.False);
            Assert.That(state.CombatKnown, Is.False);
            Assert.That(state.HasPreparedPatrol, Is.False);
        }

        [Test]
        public void PreparedSquadIsAccountedOnceAcrossDefenseAndBarracks()
        {
            HiveSquadReservationScreenModel squad =
                HiveOfficialSquadReservationPresentation.Project(
                    HiveSquadReservationScreenState.Ready, Squad(), true);
            var combat = new CombatPatrolScreenModel
            {
                State = CombatPatrolScreenState.ReadyToLaunch
            };
            HiveDefenseOverviewSnapshot defense =
                HiveDefenseOverviewPresentation.Project(squad, combat);
            Assert.That(defense.RosterKnown, Is.True);
            Assert.That(defense.CombatKnown, Is.True);
            Assert.That(defense.RosterTotal, Is.EqualTo(110));
            Assert.That(defense.ReadyTotal, Is.EqualTo(74));
            Assert.That(defense.ReservedTotal, Is.EqualTo(36));
            Assert.That(defense.HasPreparedPatrol, Is.True);
            Assert.That(defense.ReservedChampionIds, Contains.Item("zephyra"));
            Assert.That(defense.ActivePatrols, Is.Zero);
            Assert.That(defense.RecoveringTroops, Is.Zero);
            Assert.That(defense.ReadyTotal + defense.ReservedTotal,
                Is.EqualTo(defense.RosterTotal));
        }

        [Test]
        public void AnOfflineRosterIsReadOnlyAndRetainsItsRealNumbers()
        {
            HiveSquadReservationScreenModel offline =
                HiveOfficialSquadReservationPresentation.Project(
                    HiveSquadReservationScreenState.OfflineReadOnly, Squad(), true,
                    readOnlyOffline: true, cachedAtUtc: DateTimeOffset.UtcNow);
            HiveDefenseOverviewSnapshot projected =
                HiveDefenseOverviewPresentation.Project(offline, null);
            Assert.That(projected.RosterKnown, Is.True);
            Assert.That(projected.OfflineRoster, Is.True);
            Assert.That(projected.ReadyTotal, Is.EqualTo(74));
            Assert.That(projected.CombatKnown, Is.False);
            Assert.That(projected.ReservedChampionIds.Count, Is.EqualTo(1));
        }

        [Test]
        public void LoadingRosterDoesNotInventSafeTroopCounts()
        {
            HiveSquadReservationScreenModel loading =
                HiveOfficialSquadReservationPresentation.Project(
                    HiveSquadReservationScreenState.Loading, Squad(), true);
            var defense = HiveDefenseOverviewPresentation.Project(loading, null);
            Assert.That(defense.RosterKnown, Is.False);
            Assert.That(defense.CombatKnown, Is.False);
        }

        [Test]
        public void DefenseFullScreenSafeAreaAndScrollAlwaysFit()
        {
            foreach (Vector2 size in new[] {
                new Vector2(1920f, 1080f),
                new Vector2(900f, 1600f),
                new Vector2(390f, 844f) })
            {
                Rect header = HiveMapPremiumBuildingWindow.HeaderRectForProof(
                    size.x, size.y);
                Rect viewport = HiveMapPremiumBuildingWindow.ScrollViewportForProof(
                    size.x, size.y);
                Rect track = HiveMapPremiumBuildingWindow.ScrollTrackForProof(
                    size.x, size.y);
                Assert.That(header.width, Is.EqualTo(size.x));
                Assert.That(viewport.yMin, Is.GreaterThan(header.yMax));
                Assert.That(viewport.yMax, Is.LessThanOrEqualTo(size.y));
                Assert.That(track.xMax, Is.LessThanOrEqualTo(size.x));
                Assert.That(track.yMin, Is.GreaterThanOrEqualTo(viewport.yMin));
                Assert.That(track.yMax, Is.LessThanOrEqualTo(viewport.yMax));
            }
        }

        [Test]
        public void AllDefenseWindowTextsHaveRealFrenchAndEnglishCatalogEntries()
        {
            string original = BeeLocalization.CurrentLocale;
            string[] keys = {
                "subtitle", "card.status", "level", "level_unknown",
                "upgrade_active", "passive_unavailable", "level_description",
                "card.roster", "roster.total", "roster.ready",
                "roster.reserved", "status.awaiting", "status.offline",
                "status.official", "family.guardians",
                "family.wingrunners", "family.darters", "family.counts",
                "card.patrol", "patrol.active", "patrol.recovering",
                "patrol.none_prepared", "patrol.prepared", "patrol.no_champion",
                "patrol_note", "card.future", "future.siege",
                "future.shields", "future.warning", "card.management",
                "management.note", "open_barracks"
            };
            try
            {
                foreach (string locale in new[] { "fr-CA", "en-US" })
                {
                    Assert.That(BeeLocalization.SetLocale(locale), Is.True);
                    foreach (string suffix in keys)
                    {
                        string key = "defense.premium." + suffix;
                        Assert.That(BeeLocalization.HasText(locale, key), Is.True, key);
                        Assert.That(BeeLocalization.Text(key),
                            Is.Not.Empty, key);
                    }
                }
                Assert.That(BeeLocalization.SetLocale("fr-CA"), Is.True);
                Assert.That(BeeLocalization.Text("defense.premium.card.status"),
                    Is.EqualTo("ÉTAT DE DÉFENSE"));
                Assert.That(BeeLocalization.Text("defense.premium.card.patrol"),
                    Is.EqualTo("PATROUILLES ET SOINS"));
                Assert.That(BeeLocalization.SetLocale("en-US"), Is.True);
                Assert.That(BeeLocalization.Text("defense.premium.card.status"),
                    Is.EqualTo("DEFENSE STATUS"));
            }
            finally { BeeLocalization.SetLocale(original); }
        }
    }
}