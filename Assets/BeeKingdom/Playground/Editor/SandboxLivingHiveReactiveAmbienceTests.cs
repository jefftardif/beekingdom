using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxLivingHiveReactiveAmbienceTests
    {
        public static void RunAllAssertions()
        {
            var tests = new SandboxLivingHiveReactiveAmbienceTests();
            tests.UpgradeTakesPriorityOverFullProduction();
            tests.OnlyAcknowledgedUpgradeAndFullProductionHaveSpecializedStates();
            tests.EconomyAndReducedMotionStayBoundedAndDeterministic();
            tests.ProofKeepsServerAuthorityAndManualCollection();
            tests.NeutralAndUnknownProductionStatesStayHidden();
        }

        [Test]
        public void UpgradeTakesPriorityOverFullProduction()
        {
            HiveReactiveAmbienceState state = HiveReactiveAmbienceCatalog.Resolve("wax_workshop", "honey_storage");
            Assert.That(state.Kind, Is.EqualTo(HiveReactiveAmbienceKind.UpgradeActive));
            Assert.That(state.ZoneId, Is.EqualTo("wax_workshop"));
            Assert.That(state.IsVisible, Is.True);
        }

        [Test]
        public void OnlyAcknowledgedUpgradeAndFullProductionHaveSpecializedStates()
        {
            CollectionAssert.AreEquivalent(
                new[] { "Neutral", "UpgradeActive", "ProductionFull" },
                System.Enum.GetNames(typeof(HiveReactiveAmbienceKind)));
            Assert.That(HiveReactiveAmbienceCatalog.Resolve(string.Empty, "warehouse_cells").Kind, Is.EqualTo(HiveReactiveAmbienceKind.ProductionFull));
            Assert.That(HiveReactiveAmbienceCatalog.Resolve(string.Empty, string.Empty).Kind, Is.EqualTo(HiveReactiveAmbienceKind.Neutral));
        }

        [Test]
        public void EconomyAndReducedMotionStayBoundedAndDeterministic()
        {
            foreach (HiveReactiveAmbienceKind kind in System.Enum.GetValues(typeof(HiveReactiveAmbienceKind)).Cast<HiveReactiveAmbienceKind>())
            {
                HiveReactiveAmbienceDefinition definition = HiveReactiveAmbienceCatalog.DefinitionFor(kind);
                Assert.That(definition.VisibleCueCount(false), Is.InRange(0, 3), kind.ToString());
                Assert.That(definition.VisibleCueCount(true), Is.InRange(0, 1), kind.ToString());
                Assert.That(definition.MotionPhase(1f, 0, true), Is.EqualTo(definition.MotionPhase(40f, 0, true)), kind.ToString());
                if (kind != HiveReactiveAmbienceKind.Neutral)
                    Assert.That(definition.MotionPhase(1f, 0, false), Is.Not.EqualTo(definition.MotionPhase(2f, 0, false)), kind.ToString());
            }
        }

        [Test]
        public void ProofKeepsServerAuthorityAndManualCollection()
        {
            string[] rows = HiveViewProductUiPresenter.ReactiveHiveAmbienceForProof(
                "wax_workshop",
                "honey_storage",
                false,
                true,
                4f);
            AssertRow(rows, "reactive_ambience_enabled:true");
            AssertRow(rows, "reactive_ambience_kind:upgradeactive");
            AssertRow(rows, "reactive_ambience_motion_mode:reduced_static");
            AssertRow(rows, "reactive_ambience_render_authority:device");
            AssertRow(rows, "reactive_ambience_official_state_source:server_acknowledged_upgrade_or_production_snapshot");
            AssertRow(rows, "reactive_ambience_current_authority:local_preview_non_official");
            AssertRow(rows, "reactive_ambience_weather_enabled:false");
            AssertRow(rows, "reactive_ambience_brood_care_policy:generic_or_hidden");
            AssertRow(rows, "reactive_ambience_defense_alert_policy:generic_or_hidden");
            AssertRow(rows, "reactive_ambience_manual_collection_unchanged:true");
            AssertRow(rows, "reactive_ambience_mutates_gameplay:false");
            AssertRow(rows, "reactive_ambience_changes_protected_art:false");
        }

        [Test]
        public void NeutralAndUnknownProductionStatesStayHidden()
        {
            string[] neutral = HiveViewProductUiPresenter.ReactiveHiveAmbienceForProof(
                string.Empty,
                "guard_post",
                true,
                false,
                2f);
            AssertRow(neutral, "reactive_ambience_enabled:false");
            AssertRow(neutral, "reactive_ambience_kind:neutral");
            AssertRow(neutral, "reactive_ambience_zone:none");
            AssertRow(neutral, "reactive_ambience_cue_count:0");
            AssertRow(neutral, "reactive_ambience_motion_mode:none");
        }

        private static void AssertRow(IEnumerable<string> rows, string expected)
        {
            Assert.That(rows, Does.Contain(expected));
        }
    }
}
