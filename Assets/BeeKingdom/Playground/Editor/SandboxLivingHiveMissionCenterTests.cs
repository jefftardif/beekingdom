using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxLivingHiveMissionCenterTests
    {
        public static void RunAllAssertions()
        {
            var tests = new SandboxLivingHiveMissionCenterTests();
            tests.CatalogStructureIsCoherent();
            tests.SectionIdsRoundTrip();
            tests.ProgressSeedingDrivesCompletion();
            tests.ClaimRulesAreEnforced();
            tests.PinningRespectsMaximumOfThree();
            tests.PinsAndWidgetHiddenPersistAcrossReload();
            tests.WidgetRectFitsEverySurface();
            tests.ResetClearsMissionsState();
        }

        [Test]
        public void CatalogStructureIsCoherent()
        {
            string[] all = HiveViewProductUiPresenter.MissionCatalogAllIdsForProof();
            Assert.That(all, Has.Length.EqualTo(21));
            Assert.That(all, Is.Unique);

            HashSet<string> unique = new HashSet<string>(all);
            Assert.That(unique.Count, Is.EqualTo(21));
            Assert.That(HiveViewProductUiPresenter.MissionCatalogChapterCountForProof, Is.EqualTo(3));

            for (int i = 0; i < MissionCatalog.AllChapters.Length; i++)
            {
                MissionChapterDefinition chapter = MissionCatalog.AllChapters[i];
                Assert.That(chapter.ObjectiveIds, Has.Length.GreaterThan(0));
                for (int j = 0; j < chapter.ObjectiveIds.Length; j++)
                {
                    Assert.That(MissionCatalog.Find(chapter.ObjectiveIds[j]), Is.Not.Null,
                        "chapter objective " + chapter.ObjectiveIds[j] + " unresolved");
                }
            }

            Assert.That(MissionCatalog.MaxPinnedMissions, Is.EqualTo(3));
            Assert.That(MissionCatalog.Find("q_collect"), Is.Not.Null);
            Assert.That(MissionCatalog.Find("unknown_mission"), Is.Null);
        }

        [Test]
        public void SectionIdsRoundTrip()
        {
            Assert.That(HiveViewProductUiPresenter.MissionIdsForSectionForProof("quotidiennes"), Has.Length.EqualTo(7));
            Assert.That(HiveViewProductUiPresenter.MissionIdsForSectionForProof("hebdomadaires"), Has.Length.EqualTo(5));
            Assert.That(HiveViewProductUiPresenter.MissionIdsForSectionForProof("defis"), Has.Length.EqualTo(4));
            Assert.That(HiveViewProductUiPresenter.MissionIdsForSectionForProof("succes"), Has.Length.EqualTo(5));
            Assert.That(HiveViewProductUiPresenter.MissionIdsForSectionForProof("histoire"), Has.Length.Zero);

            Assert.That(HiveViewProductUiPresenter.MissionsSectionForProof, Is.EqualTo("quotidiennes"));
            HiveViewProductUiPresenter.SetMissionsSectionForProof("defis");
            Assert.That(HiveViewProductUiPresenter.MissionsSectionForProof, Is.EqualTo("defis"));
            HiveViewProductUiPresenter.SetMissionsSectionForProof("succes");
            Assert.That(HiveViewProductUiPresenter.MissionsSectionForProof, Is.EqualTo("succes"));
            HiveViewProductUiPresenter.SetMissionsSectionForProof("quotidiennes");
            Assert.That(HiveViewProductUiPresenter.MissionsSectionForProof, Is.EqualTo("quotidiennes"));
        }

        [Test]
        public void ProgressSeedingDrivesCompletion()
        {
            try
            {
                Assert.That(HiveViewProductUiPresenter.MissionProgressForProof("q_collect"), Is.EqualTo(0));
                Assert.That(HiveViewProductUiPresenter.MissionIsCompleteForProof("q_collect"), Is.False);

                HiveViewProductUiPresenter.SeedMissionProgressForProof("q_collect", 499);
                Assert.That(HiveViewProductUiPresenter.MissionProgressForProof("q_collect"), Is.EqualTo(499));
                Assert.That(HiveViewProductUiPresenter.MissionIsCompleteForProof("q_collect"), Is.False);

                HiveViewProductUiPresenter.SeedMissionProgressForProof("q_collect", 500);
                Assert.That(HiveViewProductUiPresenter.MissionIsCompleteForProof("q_collect"), Is.True);
            }
            finally
            {
                HiveViewProductUiPresenter.ClearMissionProgressForProof();
            }
        }

        [Test]
        public void ClaimRulesAreEnforced()
        {
            try
            {
                HiveViewProductUiPresenter.SeedMissionProgressForProof("q_collect", 100);
                Assert.That(HiveViewProductUiPresenter.MissionIsCompleteForProof("q_collect"), Is.False);
                HiveViewProductUiPresenter.ClaimMissionForProof("q_collect");
                Assert.That(HiveViewProductUiPresenter.MissionIsClaimedForProof("q_collect"), Is.False,
                    "incomplete mission must not be claimable");

                HiveViewProductUiPresenter.SeedMissionProgressForProof("q_collect", 500);
                int before = HiveViewProductUiPresenter.MissionsClaimCommitCountForProof;
                HiveViewProductUiPresenter.ClaimMissionForProof("q_collect");
                Assert.That(HiveViewProductUiPresenter.MissionIsClaimedForProof("q_collect"), Is.True);
                Assert.That(HiveViewProductUiPresenter.MissionClaimedCountForProof, Is.EqualTo(1));
                Assert.That(HiveViewProductUiPresenter.MissionsClaimCommitCountForProof, Is.EqualTo(before + 1));

                int afterFirst = HiveViewProductUiPresenter.MissionsClaimCommitCountForProof;
                HiveViewProductUiPresenter.ClaimMissionForProof("q_collect");
                Assert.That(HiveViewProductUiPresenter.MissionsClaimCommitCountForProof, Is.EqualTo(afterFirst),
                    "double claim must not commit again");
            }
            finally
            {
                HiveViewProductUiPresenter.ResetMissionsStateForProof();
            }
        }

        [Test]
        public void PinningRespectsMaximumOfThree()
        {
            try
            {
                Assert.That(HiveViewProductUiPresenter.MissionPinnedForProof("q_collect"), Is.False);
                HiveViewProductUiPresenter.ToggleMissionPinnedForProof("q_collect");
                HiveViewProductUiPresenter.ToggleMissionPinnedForProof("w_collect");
                HiveViewProductUiPresenter.ToggleMissionPinnedForProof("d_level_10");
                Assert.That(HiveViewProductUiPresenter.MissionPinnedForProof("q_collect"), Is.True);
                Assert.That(HiveViewProductUiPresenter.MissionPinnedForProof("w_collect"), Is.True);
                Assert.That(HiveViewProductUiPresenter.MissionPinnedForProof("d_level_10"), Is.True);
                Assert.That(HiveViewProductUiPresenter.PinnedMissionIdsForProof, Has.Length.EqualTo(3));

                HiveViewProductUiPresenter.ToggleMissionPinnedForProof("q_build");
                Assert.That(HiveViewProductUiPresenter.PinnedMissionIdsForProof, Has.Length.EqualTo(3));
                Assert.That(HiveViewProductUiPresenter.MissionPinnedForProof("q_build"), Is.False);

                HiveViewProductUiPresenter.ToggleMissionPinnedForProof("q_collect");
                Assert.That(HiveViewProductUiPresenter.PinnedMissionIdsForProof, Has.Length.EqualTo(2));
                Assert.That(HiveViewProductUiPresenter.MissionPinnedForProof("q_collect"), Is.False);

                HiveViewProductUiPresenter.ToggleMissionPinnedForProof("does_not_exist");
                Assert.That(HiveViewProductUiPresenter.MissionPinnedForProof("does_not_exist"), Is.False);
            }
            finally
            {
                HiveViewProductUiPresenter.ResetMissionsStateForProof();
            }
        }

        [Test]
        public void PinsAndWidgetHiddenPersistAcrossReload()
        {
            var store = new MemoryMissionStore();
            try
            {
                HiveViewProductUiPresenter.ConfigureMobileComfortPreferencesStoreForTests(store);
                HiveViewProductUiPresenter.ReloadMobileComfortPreferencesForProof();
                Assert.That(HiveViewProductUiPresenter.PinnedMissionIdsForProof, Is.Empty);
                Assert.That(HiveViewProductUiPresenter.MissionsWidgetHiddenForProof, Is.False);

                HiveViewProductUiPresenter.ToggleMissionPinnedForProof("q_collect");
                HiveViewProductUiPresenter.ToggleMissionPinnedForProof("q_build");
                HiveViewProductUiPresenter.SetMissionsWidgetHiddenForProof(true);
                Assert.That(HiveViewProductUiPresenter.MissionsWidgetHiddenForProof, Is.True);

                HiveViewProductUiPresenter.ConfigureMobileComfortPreferencesStoreForTests(store);
                HiveViewProductUiPresenter.ReloadMobileComfortPreferencesForProof();

                Assert.That(HiveViewProductUiPresenter.PinnedMissionIdsForProof, Has.Length.EqualTo(2));
                Assert.That(HiveViewProductUiPresenter.MissionPinnedForProof("q_collect"), Is.True);
                Assert.That(HiveViewProductUiPresenter.MissionPinnedForProof("q_build"), Is.True);
                Assert.That(HiveViewProductUiPresenter.MissionPinnedForProof("q_champion"), Is.False);
                Assert.That(HiveViewProductUiPresenter.MissionsWidgetHiddenForProof, Is.True);
            }
            finally
            {
                HiveViewProductUiPresenter.ConfigureMobileComfortPreferencesStoreForTests(null);
                HiveViewProductUiPresenter.ResetMissionsStateForProof();
            }
        }

        [Test]
        public void WidgetRectFitsEverySurface()
        {
            AssertWidgetInside(new Rect(0f, 0f, 390f, 844f), true, false);
            AssertWidgetInside(new Rect(0f, 0f, 1600f, 900f), false, false);
            AssertWidgetInside(new Rect(0f, 0f, 390f, 844f), true, true);
            AssertWidgetInside(new Rect(0f, 0f, 1600f, 900f), false, true);

            Rect hive = HiveViewProductUiPresenter.MissionsWidgetRectForProof(true, 390f, 844f, false);
            Rect world = HiveViewProductUiPresenter.MissionsWidgetRectForProof(true, 390f, 844f, true);
            Assert.That(hive.width, Is.GreaterThan(0f));
            Assert.That(hive.height, Is.GreaterThan(0f));
            Assert.That(world.width, Is.GreaterThan(0f));
            Assert.That(world.height, Is.GreaterThan(0f));
        }

        [Test]
        public void ResetClearsMissionsState()
        {
            HiveViewProductUiPresenter.SetMissionsCenterOpenForProof(true);
            HiveViewProductUiPresenter.SeedMissionProgressForProof("q_collect", 500);
            HiveViewProductUiPresenter.ClaimMissionForProof("q_collect");
            HiveViewProductUiPresenter.ToggleMissionPinnedForProof("q_collect");
            HiveViewProductUiPresenter.SetMissionsWidgetHiddenForProof(true);

            HiveViewProductUiPresenter.ResetMissionsStateForProof();
            Assert.That(HiveViewProductUiPresenter.MissionsCenterOpenForProof, Is.False);
            Assert.That(HiveViewProductUiPresenter.MissionIsClaimedForProof("q_collect"), Is.False);
            Assert.That(HiveViewProductUiPresenter.PinnedMissionIdsForProof, Is.Empty);
            Assert.That(HiveViewProductUiPresenter.MissionsWidgetHiddenForProof, Is.False);
        }

        private static void AssertWidgetInside(Rect screen, bool portrait, bool worldMap)
        {
            Rect widget = HiveViewProductUiPresenter.MissionsWidgetRectForProof(portrait, screen.width, screen.height, worldMap);
            Assert.That(widget.x, Is.GreaterThanOrEqualTo(0f));
            Assert.That(widget.y, Is.GreaterThanOrEqualTo(0f));
            Assert.That(widget.xMax, Is.LessThanOrEqualTo(screen.width));
            Assert.That(widget.yMax, Is.LessThanOrEqualTo(screen.height));
        }

        private sealed class MemoryMissionStore : IMobileComfortPreferencesStore
        {
            private string json;

            public string Read() => json;
            public void Write(string value) => json = value ?? string.Empty;
            public void Delete() => json = string.Empty;
        }
    }
}
