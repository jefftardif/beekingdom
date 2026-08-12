using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxLivingHiveSpecializedBeeActivityTests
    {
        public static void RunAllAssertions()
        {
            var tests = new SandboxLivingHiveSpecializedBeeActivityTests();
            tests.CatalogMapsFiveDistinctBuildingBehaviors();
            tests.CueBudgetsStayBoundedAndEconomyUsesOneCue();
            tests.ReducedMotionIsStaticWhileAmbientMotionAdvances();
            tests.PresentationProofKeepsServerAuthorityAndProtectedArt();
            tests.SpecializedCuesReuseAmbientBeeBudgetsAndHideUnknownZones();
        }

        [Test]
        public void CatalogMapsFiveDistinctBuildingBehaviors()
        {
            string[] zones =
            {
                "honey_storage",
                "wax_workshop",
                "warehouse_cells",
                "nursery_cluster",
                "guard_post"
            };
            Assert.That(HiveSpecializedBeeActivityCatalog.All.Count, Is.EqualTo(zones.Length));
            CollectionAssert.AreEquivalent(
                zones,
                HiveSpecializedBeeActivityCatalog.All.Select(definition => definition.ZoneId).ToArray());
            Assert.That(
                HiveSpecializedBeeActivityCatalog.All.Select(definition => definition.Kind).Distinct().Count(),
                Is.EqualTo(zones.Length));
            foreach (string zone in zones)
            {
                Assert.That(HiveSpecializedBeeActivityCatalog.TryResolve(zone, out HiveSpecializedBeeActivityDefinition definition), Is.True, zone);
                Assert.That(definition.ResourceIconId, Is.Not.Empty, zone);
            }
        }

        [Test]
        public void CueBudgetsStayBoundedAndEconomyUsesOneCue()
        {
            foreach (HiveSpecializedBeeActivityDefinition definition in HiveSpecializedBeeActivityCatalog.All)
            {
                Assert.That(definition.CueCount, Is.InRange(1, 3), definition.ZoneId);
                Assert.That(definition.VisibleCueCount(false), Is.EqualTo(definition.CueCount), definition.ZoneId);
                Assert.That(definition.VisibleCueCount(true), Is.EqualTo(1), definition.ZoneId);
                Assert.That(definition.MotionSpeed, Is.InRange(0.01d, 1d), definition.ZoneId);
                Assert.That(definition.Accent.a, Is.GreaterThan(0f), definition.ZoneId);
            }
        }

        [Test]
        public void ReducedMotionIsStaticWhileAmbientMotionAdvances()
        {
            Assert.That(HiveSpecializedBeeActivityCatalog.TryResolve("wax_workshop", out HiveSpecializedBeeActivityDefinition definition), Is.True);
            float staticA = definition.MotionPhase(1f, 1, true);
            float staticB = definition.MotionPhase(49f, 1, true);
            float movingA = definition.MotionPhase(1f, 1, false);
            float movingB = definition.MotionPhase(2f, 1, false);
            Assert.That(staticA, Is.EqualTo(staticB));
            Assert.That(movingA, Is.Not.EqualTo(movingB));
            Assert.That(staticA, Is.InRange(0f, 1f));
            Assert.That(movingA, Is.InRange(0f, 1f));
        }

        [Test]
        public void PresentationProofKeepsServerAuthorityAndProtectedArt()
        {
            string[] rows = HiveViewProductUiPresenter.SpecializedBeeActivityForProof(
                "nursery_cluster",
                false,
                true,
                25f);
            AssertRow(rows, "specialized_activity_enabled:true");
            AssertRow(rows, "specialized_activity_kind:broodnursing");
            AssertRow(rows, "specialized_activity_motion_mode:reduced_static");
            AssertRow(rows, "specialized_activity_additional_bees:0");
            AssertRow(rows, "specialized_activity_shares_ambient_budget:true");
            AssertRow(rows, "specialized_activity_selected_emphasis:presentation_only");
            AssertRow(rows, "specialized_activity_render_authority:device");
            AssertRow(rows, "specialized_activity_official_state_source:server_snapshot_required");
            AssertRow(rows, "specialized_activity_ambiguous_server_substate:generic_or_hidden");
            AssertRow(rows, "specialized_activity_mutates_gameplay:false");
            AssertRow(rows, "specialized_activity_changes_protected_art:false");
        }

        [Test]
        public void SpecializedCuesReuseAmbientBeeBudgetsAndHideUnknownZones()
        {
            try
            {
                HiveViewProductUiPresenter.SetMobileComfortPreferencesForProof(false, false, false);
                string[] normal = HiveViewProductUiPresenter.LivingHiveAmbientTrafficForProof();
                AssertRow(normal, "landscape_budget:8");
                AssertRow(normal, "portrait_budget:5");

                HiveViewProductUiPresenter.SetMobileComfortPreferencesForProof(false, true, false);
                string[] economy = HiveViewProductUiPresenter.LivingHiveAmbientTrafficForProof();
                AssertRow(economy, "landscape_budget:5");
                AssertRow(economy, "portrait_budget:3");
                AssertRow(
                    HiveViewProductUiPresenter.SpecializedBeeActivityForProof("warehouse_cells", true, false, 1f),
                    "specialized_activity_cue_count:1");

                string[] unknown = HiveViewProductUiPresenter.SpecializedBeeActivityForProof("alliance_future_hall", false, false, 1f);
                AssertRow(unknown, "specialized_activity_enabled:false");
                AssertRow(unknown, "specialized_activity_unknown_policy:hidden");
            }
            finally
            {
                HiveViewProductUiPresenter.SetMobileComfortPreferencesForProof(false, false, false);
            }
        }

        private static void AssertRow(IEnumerable<string> rows, string expected)
        {
            Assert.That(rows, Does.Contain(expected));
        }
    }
}
