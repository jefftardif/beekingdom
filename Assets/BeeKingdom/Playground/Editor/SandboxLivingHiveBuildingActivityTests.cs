using System;
using System.Collections.Generic;
using System.Linq;
using BeeKingdom.Localization;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxLivingHiveBuildingActivityTests
    {
        private static readonly string[] LocalizationKeys =
        {
            "building.activity.honey",
            "building.activity.wax",
            "building.activity.pollen",
            "building.activity.nursery",
            "building.activity.guard",
            "building.activity.chamber",
            "building.activity.upgrade",
            "building.activity.worker_training",
            "building.activity.guard_training",
            "building.activity.full",
            "building.activity.production",
            "building.activity.nursery_status",
            "building.activity.guard_status"
        };

        public static void RunAllAssertions()
        {
            var tests = new SandboxLivingHiveBuildingActivityTests();
            tests.CatalogMapsFiveSpecializedBuildingActivitiesAndFallback();
            tests.ActivitySignalsStayWithinMobileBudgets();
            tests.BannerReflectsRealStateWithoutChangingGameplayAuthority();
            tests.ReducedMotionAndEconomyModeOnlyChangePresentation();
            tests.ActivityCopyAndBannersAreLocalizedAndMobileSafe();
        }

        [Test]
        public void CatalogMapsFiveSpecializedBuildingActivitiesAndFallback()
        {
            string[] expectedHotspots =
            {
                "honey_storage",
                "wax_workshop",
                "warehouse_cells",
                "nursery_cluster",
                "guard_post"
            };

            Assert.That(HiveBuildingActivityCatalog.All.Count, Is.EqualTo(expectedHotspots.Length));
            CollectionAssert.AreEquivalent(
                expectedHotspots,
                HiveBuildingActivityCatalog.All.Select(definition => definition.HotspotId).ToArray());
            Assert.That(
                HiveBuildingActivityCatalog.All.Select(definition => definition.Kind).Distinct().Count(),
                Is.EqualTo(expectedHotspots.Length));

            HiveBuildingActivityDefinition fallback = HiveBuildingActivityCatalog.Resolve("research_node");
            Assert.That(fallback.Kind, Is.EqualTo(HiveBuildingActivityKind.ChamberMaintenance));
            Assert.That(fallback.LocalizationKey, Is.EqualTo("building.activity.chamber"));
        }

        [Test]
        public void ActivitySignalsStayWithinMobileBudgets()
        {
            foreach (HiveBuildingActivityDefinition definition in HiveBuildingActivityCatalog.All)
            {
                Assert.That(definition.SignalCount, Is.InRange(1, 3), definition.HotspotId);
                Assert.That(definition.MotionSpeed, Is.InRange(0.01d, 1d), definition.HotspotId);
                Assert.That(definition.IconId, Is.Not.Empty, definition.HotspotId);
                Assert.That(definition.LocalizationKey, Is.Not.Empty, definition.HotspotId);
            }
        }

        [Test]
        public void BannerReflectsRealStateWithoutChangingGameplayAuthority()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
            HiveViewProductUiPresenter.SetMobileComfortPreferencesForProof(false, false, false);
            Assert.That(BeeLocalization.SetLocale("fr-CA"), Is.True);
            HiveViewProductUiPresenter.SetManualProductionForProof("honey_storage", 840f, 61650f);

            string[] producing = HiveViewProductUiPresenter.BuildingActivityBannerForProof("honey_storage");
            AssertRow(producing, "building_activity_kind:nectarstorage");
            Assert.That(Value(producing, "building_activity_status"), Does.StartWith("Nectar en stockage · remplissage "));
            AssertRow(producing, "building_activity_render_authority:device");
            AssertRow(producing, "building_activity_official_state_source:server_snapshot_required");
            AssertRow(producing, "building_activity_mutates_gameplay:false");
            AssertRow(producing, "building_activity_changes_protected_art:false");

            HiveViewProductUiPresenter.SetManualProductionForProof("honey_storage", 999999f, 61650f);
            string[] full = HiveViewProductUiPresenter.BuildingActivityBannerForProof("honey_storage");
            Assert.That(Value(full, "building_activity_status"), Is.EqualTo("Nectar en stockage · plein, collecte manuelle"));
        }

        [Test]
        public void ReducedMotionAndEconomyModeOnlyChangePresentation()
        {
            try
            {
                float staticA = HiveViewProductUiPresenter.BuildingActivityMotionPhaseForProof("warehouse_cells", 1f, 1, true);
                float staticB = HiveViewProductUiPresenter.BuildingActivityMotionPhaseForProof("warehouse_cells", 47f, 1, true);
                float movingA = HiveViewProductUiPresenter.BuildingActivityMotionPhaseForProof("warehouse_cells", 1f, 1, false);
                float movingB = HiveViewProductUiPresenter.BuildingActivityMotionPhaseForProof("warehouse_cells", 2f, 1, false);
                Assert.That(staticA, Is.EqualTo(staticB));
                Assert.That(movingA, Is.Not.EqualTo(movingB));

                HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
                HiveViewProductUiPresenter.SetMobileComfortPreferencesForProof(false, false, false);
                AssertRow(
                    HiveViewProductUiPresenter.BuildingActivityBannerForProof("warehouse_cells"),
                    "building_activity_signal_count:3");

                HiveViewProductUiPresenter.SetMobileComfortPreferencesForProof(true, false, false);
                AssertRow(
                    HiveViewProductUiPresenter.BuildingActivityBannerForProof("warehouse_cells"),
                    "building_activity_motion_mode:reduced_static");

                HiveViewProductUiPresenter.SetMobileComfortPreferencesForProof(false, true, false);
                string[] economy = HiveViewProductUiPresenter.BuildingActivityBannerForProof("warehouse_cells");
                AssertRow(economy, "building_activity_signal_count:1");
                AssertRow(economy, "building_activity_mutates_gameplay:false");
            }
            finally
            {
                HiveViewProductUiPresenter.SetMobileComfortPreferencesForProof(false, false, false);
            }
        }

        [Test]
        public void ActivityCopyAndBannersAreLocalizedAndMobileSafe()
        {
            foreach (string key in LocalizationKeys)
            {
                Assert.That(BeeLocalization.HasText("fr-CA", key), Is.True, key + " missing in fr-CA");
                Assert.That(BeeLocalization.HasText("en-US", key), Is.True, key + " missing in en-US");
            }

            AssertBannerFits(true, 390f, 844f);
            AssertBannerFits(false, 1280f, 720f);
            AssertBannerFits(false, 1600f, 900f);
        }

        private static void AssertBannerFits(bool portrait, float width, float height)
        {
            Rect banner = HiveViewProductUiPresenter.BuildingActivityBannerRectForProof(portrait, width, height);
            Assert.That(banner.width, Is.GreaterThan(0f));
            Assert.That(banner.height, Is.EqualTo(18f));
            Assert.That(banner.xMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(banner.yMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(banner.xMax, Is.LessThanOrEqualTo(width));
            Assert.That(banner.yMax, Is.LessThanOrEqualTo(height - (portrait ? 78f : 70f)));
        }

        private static string Value(IEnumerable<string> rows, string key)
        {
            string prefix = key + ":";
            foreach (string row in rows)
            {
                if (row.StartsWith(prefix, StringComparison.Ordinal)) return row.Substring(prefix.Length);
            }
            Assert.Fail("Missing proof row " + key);
            return string.Empty;
        }

        private static void AssertRow(IEnumerable<string> rows, string expected)
        {
            Assert.That(rows, Does.Contain(expected));
        }
    }
}
