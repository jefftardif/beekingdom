using System;
using System.Collections.Generic;
using System.Linq;
using BeeKingdom.Localization;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxLivingHiveMobileComfortTests
    {
        public static void RunAllAssertions()
        {
            var tests = new SandboxLivingHiveMobileComfortTests();
            tests.CodecRoundTripsVersionedDevicePreferences();
            tests.CorruptAndUnsupportedPreferencesResetSafely();
            tests.PresenterPersistsAndRestoresBothComfortChoices();
            tests.EconomyModeReducesOnlyAmbientVisualLoad();
            tests.SettingsAreLocalizedAndMobileSafe();
        }

        [Test]
        public void CodecRoundTripsVersionedDevicePreferences()
        {
            var preferences = MobileComfortPreferencesCodec.CreateDefault();
            preferences.revision = -8;
            preferences.reducedMotion = true;
            preferences.economyMode = true;
            preferences.miniChatOpen = true;

            string json = MobileComfortPreferencesCodec.Write(preferences);
            MobileComfortPreferencesReadResult result = MobileComfortPreferencesCodec.Read(json);

            Assert.That(result.Status, Is.EqualTo(MobileComfortPreferencesReadStatus.Valid));
            Assert.That(result.Preferences.version, Is.EqualTo(MobileComfortPreferencesCodec.CurrentVersion));
            Assert.That(result.Preferences.revision, Is.Zero);
            Assert.That(result.Preferences.reducedMotion, Is.True);
            Assert.That(result.Preferences.economyMode, Is.True);
            Assert.That(result.Preferences.miniChatOpen, Is.True);
        }

        [Test]
        public void CorruptAndUnsupportedPreferencesResetSafely()
        {
            var corruptStore = new MemoryComfortStore("{broken-json");
            try
            {
                HiveViewProductUiPresenter.ConfigureMobileComfortPreferencesStoreForTests(corruptStore);
                HiveViewProductUiPresenter.ReloadMobileComfortPreferencesForProof();
                AssertRow(HiveViewProductUiPresenter.MobileComfortPreferencesForProof(), "restore_status:corrupted_reset");
                AssertRow(HiveViewProductUiPresenter.MobileComfortPreferencesForProof(), "reduced_motion:false");
                AssertRow(HiveViewProductUiPresenter.MobileComfortPreferencesForProof(), "economy_mode:false");
                Assert.That(corruptStore.DeleteCount, Is.EqualTo(1));
                Assert.That(corruptStore.WriteCount, Is.EqualTo(1));

                corruptStore.SetRaw("{\"version\":2,\"revision\":5,\"reducedMotion\":true,\"economyMode\":true}");
                HiveViewProductUiPresenter.ReloadMobileComfortPreferencesForProof();
                AssertRow(HiveViewProductUiPresenter.MobileComfortPreferencesForProof(), "restore_status:unsupportedversion_reset");
                AssertRow(HiveViewProductUiPresenter.MobileComfortPreferencesForProof(), "reduced_motion:false");
                AssertRow(HiveViewProductUiPresenter.MobileComfortPreferencesForProof(), "economy_mode:false");
                Assert.That(corruptStore.DeleteCount, Is.EqualTo(2));
                Assert.That(corruptStore.WriteCount, Is.EqualTo(2));
            }
            finally
            {
                RestoreDefaultPresenterPreferences();
            }
        }

        [Test]
        public void PresenterPersistsAndRestoresBothComfortChoices()
        {
            var store = new MemoryComfortStore();
            try
            {
                HiveViewProductUiPresenter.ConfigureMobileComfortPreferencesStoreForTests(store);
                HiveViewProductUiPresenter.ReloadMobileComfortPreferencesForProof();
                AssertRow(HiveViewProductUiPresenter.MobileComfortPreferencesForProof(), "restore_status:missing");

                HiveViewProductUiPresenter.SetMobileComfortPreferencesForProof(true, true, true);
                Assert.That(store.WriteCount, Is.EqualTo(1));
                AssertRow(HiveViewProductUiPresenter.MobileComfortPreferencesForProof(), "revision:1");

                HiveViewProductUiPresenter.ConfigureMobileComfortPreferencesStoreForTests(store);
                HiveViewProductUiPresenter.ReloadMobileComfortPreferencesForProof();
                AssertRow(HiveViewProductUiPresenter.MobileComfortPreferencesForProof(), "restore_status:valid");
                AssertRow(HiveViewProductUiPresenter.MobileComfortPreferencesForProof(), "reduced_motion:true");
                AssertRow(HiveViewProductUiPresenter.MobileComfortPreferencesForProof(), "economy_mode:true");
                AssertRow(HiveViewProductUiPresenter.MobileComfortPreferencesForProof(), "revision:1");
            }
            finally
            {
                RestoreDefaultPresenterPreferences();
            }
        }

        [Test]
        public void EconomyModeReducesOnlyAmbientVisualLoad()
        {
            var store = new MemoryComfortStore();
            try
            {
                HiveViewProductUiPresenter.ConfigureMobileComfortPreferencesStoreForTests(store);
                HiveViewProductUiPresenter.SetMobileComfortPreferencesForProof(false, false, false);
                string[] normal = HiveViewProductUiPresenter.MobileComfortPreferencesForProof();
                AssertRow(normal, "ambient_portrait_budget:5");
                AssertRow(normal, "ambient_landscape_budget:8");
                AssertRow(normal, "motion_trails_enabled:true");

                HiveViewProductUiPresenter.SetMobileComfortPreferencesForProof(false, true, false);
                string[] economy = HiveViewProductUiPresenter.MobileComfortPreferencesForProof();
                AssertRow(economy, "ambient_portrait_budget:3");
                AssertRow(economy, "ambient_landscape_budget:5");
                AssertRow(economy, "motion_trails_enabled:false");
                AssertRow(economy, "active_task_bees_preserved:true");
                AssertRow(economy, "economy_authority_changed:false");

                HiveViewProductUiPresenter.SetMobileComfortPreferencesForProof(true, false, false);
                string[] reduced = HiveViewProductUiPresenter.MobileComfortPreferencesForProof();
                AssertRow(reduced, "motion_trails_enabled:false");
                AssertRow(reduced, "ambient_portrait_budget:5");
            }
            finally
            {
                RestoreDefaultPresenterPreferences();
            }
        }

        [Test]
        public void SettingsAreLocalizedAndMobileSafe()
        {
            string[] keys =
            {
                "settings.mobile.title",
                "settings.mobile.custom",
                "settings.mobile.opened",
                "settings.mobile.intro",
                "settings.mobile.reduced_motion",
                "settings.mobile.reduced_motion.body",
                "settings.mobile.economy",
                "settings.mobile.economy.body",
                "settings.mobile.enabled",
                "settings.mobile.disabled",
                "settings.mobile.device_only"
            };
            foreach (string key in keys)
            {
                Assert.That(BeeLocalization.HasText("fr-CA", key), Is.True, key + " missing in fr-CA");
                Assert.That(BeeLocalization.HasText("en-US", key), Is.True, key + " missing in en-US");
            }

            AssertPanelAndTargetsFit(true, 390f, 844f);
            AssertPanelAndTargetsFit(false, 1600f, 900f);
        }

        private static void AssertPanelAndTargetsFit(bool portrait, float width, float height)
        {
            Rect panel = HiveViewProductUiPresenter.MobileComfortSettingsPanelRectForProof(portrait, width, height);
            Assert.That(panel.x, Is.GreaterThanOrEqualTo(0f));
            Assert.That(panel.y, Is.GreaterThanOrEqualTo(0f));
            Assert.That(panel.xMax, Is.LessThanOrEqualTo(width));
            Assert.That(panel.yMax, Is.LessThanOrEqualTo(height - (portrait ? 88f : 76f)));
            Rect[] targets = HiveViewProductUiPresenter.MobileComfortToggleRectsForProof(portrait, width, height);
            Assert.That(targets, Has.Length.EqualTo(3));
            foreach (Rect target in targets)
            {
                Assert.That(target.width, Is.GreaterThanOrEqualTo(44f));
                Assert.That(target.height, Is.GreaterThanOrEqualTo(44f));
                Assert.That(panel.Contains(target.min), Is.True);
                Assert.That(panel.Contains(new Vector2(target.xMax - 0.01f, target.yMax - 0.01f)), Is.True);
            }
        }

        private static void RestoreDefaultPresenterPreferences()
        {
            HiveViewProductUiPresenter.ConfigureMobileComfortPreferencesStoreForTests(null);
            HiveViewProductUiPresenter.SetMobileComfortPreferencesForProof(false, false, false);
        }

        private static void AssertRow(IEnumerable<string> rows, string expected)
        {
            Assert.That(rows, Does.Contain(expected));
        }

        private sealed class MemoryComfortStore : IMobileComfortPreferencesStore
        {
            private string json;

            public MemoryComfortStore(string initialJson = "")
            {
                json = initialJson ?? string.Empty;
            }

            public int WriteCount { get; private set; }
            public int DeleteCount { get; private set; }
            public string Read() => json;
            public void Write(string value)
            {
                json = value ?? string.Empty;
                WriteCount++;
            }
            public void Delete()
            {
                json = string.Empty;
                DeleteCount++;
            }
            public void SetRaw(string value) => json = value ?? string.Empty;
        }
    }
}
