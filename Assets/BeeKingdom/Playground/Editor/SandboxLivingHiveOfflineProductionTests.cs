using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BeeKingdom.Localization;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxLivingHiveOfflineProductionTests
    {
        public static void RunAllAssertions()
        {
            var tests = new SandboxLivingHiveOfflineProductionTests();
            tests.CodecAccruesIntoBuildingCachesWithoutCreditingPlayerStocks();
            tests.CodecCapsFutureLeapsAndNeverAccruesAcrossClockRollback();
            tests.CodecRejectsForeignProfilesAndSanitizesBoundedEntries();
            tests.PresenterRestoresOfflineProductionAndKeepsCollectionManual();
            tests.ReturnNoticeIsLocalizedAndMobileSafe();
        }

        [Test]
        public void CodecAccruesIntoBuildingCachesWithoutCreditingPlayerStocks()
        {
            IReadOnlyList<LocalPreviewManualProductionRule> rules = FixedRules();
            long now = new DateTime(2026, 7, 22, 12, 0, 0, DateTimeKind.Utc).Ticks;
            LocalPreviewManualProductionJournal journal = LocalPreviewManualProductionJournalCodec.CreateDefault(
                "profile-a",
                now - TimeSpan.FromMinutes(30).Ticks,
                rules);
            LocalPreviewManualProductionJournalCodec.SetPending(journal, "honey_storage", 100f, rules);
            LocalPreviewManualProductionJournalCodec.SetPending(journal, "wax_workshop", 100f, rules);
            LocalPreviewManualProductionJournalCodec.SetPending(journal, "warehouse_cells", 190f, rules);

            LocalPreviewManualProductionAccrualResult result = LocalPreviewManualProductionJournalCodec.Accrue(
                journal,
                now,
                rules);

            Assert.That(result.Status, Is.EqualTo(LocalPreviewManualProductionAccrualStatus.Accrued));
            Assert.That(result.RecognizedSeconds, Is.EqualTo(1800d).Within(0.001d));
            Assert.That(result.AccruedFor("honey_storage"), Is.EqualTo(180f).Within(0.01f));
            Assert.That(result.AccruedFor("wax_workshop"), Is.EqualTo(360f).Within(0.01f));
            Assert.That(result.AccruedFor("warehouse_cells"), Is.EqualTo(10f).Within(0.01f));
            Assert.That(LocalPreviewManualProductionJournalCodec.PendingFor(journal, "honey_storage"), Is.EqualTo(280f).Within(0.01f));
            Assert.That(LocalPreviewManualProductionJournalCodec.PendingFor(journal, "wax_workshop"), Is.EqualTo(460f).Within(0.01f));
            Assert.That(LocalPreviewManualProductionJournalCodec.PendingFor(journal, "warehouse_cells"), Is.EqualTo(200f).Within(0.01f));
            Assert.That(journal.lastAccrualUtcTicks, Is.EqualTo(now));
        }

        [Test]
        public void CodecCapsFutureLeapsAndNeverAccruesAcrossClockRollback()
        {
            IReadOnlyList<LocalPreviewManualProductionRule> rules = FixedRules();
            long now = new DateTime(2026, 7, 22, 12, 0, 0, DateTimeKind.Utc).Ticks;
            LocalPreviewManualProductionJournal futureLeap = LocalPreviewManualProductionJournalCodec.CreateDefault(
                "profile-a",
                now - TimeSpan.FromDays(30).Ticks,
                rules);
            LocalPreviewManualProductionAccrualResult capped = LocalPreviewManualProductionJournalCodec.Accrue(
                futureLeap,
                now,
                rules,
                3600d);
            Assert.That(capped.Status, Is.EqualTo(LocalPreviewManualProductionAccrualStatus.FutureLeapCapped));
            Assert.That(capped.RecognizedSeconds, Is.EqualTo(3600d));
            Assert.That(LocalPreviewManualProductionJournalCodec.PendingFor(futureLeap, "honey_storage"), Is.EqualTo(360f).Within(0.01f));

            LocalPreviewManualProductionJournal rollback = LocalPreviewManualProductionJournalCodec.CreateDefault(
                "profile-a",
                now + TimeSpan.FromHours(2).Ticks,
                rules);
            LocalPreviewManualProductionJournalCodec.SetPending(rollback, "honey_storage", 125f, rules);
            long protectedMarker = rollback.lastAccrualUtcTicks;
            LocalPreviewManualProductionAccrualResult rejected = LocalPreviewManualProductionJournalCodec.Accrue(
                rollback,
                now,
                rules);
            Assert.That(rejected.Status, Is.EqualTo(LocalPreviewManualProductionAccrualStatus.ClockRollback));
            Assert.That(rejected.RecognizedSeconds, Is.Zero);
            Assert.That(LocalPreviewManualProductionJournalCodec.PendingFor(rollback, "honey_storage"), Is.EqualTo(125f));
            Assert.That(rollback.lastAccrualUtcTicks, Is.EqualTo(protectedMarker));
        }

        [Test]
        public void CodecRejectsForeignProfilesAndSanitizesBoundedEntries()
        {
            IReadOnlyList<LocalPreviewManualProductionRule> rules = FixedRules();
            long now = new DateTime(2026, 7, 22, 12, 0, 0, DateTimeKind.Utc).Ticks;
            LocalPreviewManualProductionJournal foreign = LocalPreviewManualProductionJournalCodec.CreateDefault("profile-a", now, rules);
            LocalPreviewManualProductionReadResult mismatch = LocalPreviewManualProductionJournalCodec.Read(
                new MemoryProductionStore(JsonUtility.ToJson(foreign)),
                "profile-b",
                now,
                rules);
            Assert.That(mismatch.Status, Is.EqualTo(LocalPreviewManualProductionReadStatus.ProfileMismatch));
            Assert.That(mismatch.Journal.profileId, Is.EqualTo("profile-b"));
            Assert.That(mismatch.Journal.entries.All(entry => entry.pending == 0f), Is.True);

            LocalPreviewManualProductionReadResult corrupt = LocalPreviewManualProductionJournalCodec.Read(
                new MemoryProductionStore("{broken-json"),
                "profile-b",
                now,
                rules);
            Assert.That(corrupt.Status, Is.EqualTo(LocalPreviewManualProductionReadStatus.Corrupt));

            var unsupported = LocalPreviewManualProductionJournalCodec.CreateDefault("profile-b", now, rules);
            unsupported.version = LocalPreviewManualProductionJournalCodec.CurrentVersion + 1;
            LocalPreviewManualProductionReadResult unsupportedResult = LocalPreviewManualProductionJournalCodec.Read(
                new MemoryProductionStore(JsonUtility.ToJson(unsupported)),
                "profile-b",
                now,
                rules);
            Assert.That(unsupportedResult.Status, Is.EqualTo(LocalPreviewManualProductionReadStatus.UnsupportedVersion));

            var unsafeJournal = new LocalPreviewManualProductionJournal
            {
                profileId = "profile-b",
                revision = -3,
                lastAccrualUtcTicks = -5,
                entries = new List<LocalPreviewManualProductionEntry>
                {
                    new LocalPreviewManualProductionEntry { hotspotId = "honey_storage", pending = 100f },
                    new LocalPreviewManualProductionEntry { hotspotId = "honey_storage", pending = 250f },
                    new LocalPreviewManualProductionEntry { hotspotId = "unknown", pending = 99999f },
                    new LocalPreviewManualProductionEntry { hotspotId = "wax_workshop", pending = -20f }
                }
            };
            var boundedStore = new MemoryProductionStore(JsonUtility.ToJson(unsafeJournal));
            LocalPreviewManualProductionReadResult bounded = LocalPreviewManualProductionJournalCodec.Read(
                boundedStore,
                "profile-b",
                now,
                rules);
            Assert.That(bounded.Status, Is.EqualTo(LocalPreviewManualProductionReadStatus.Sanitized));
            Assert.That(bounded.Journal.revision, Is.Zero);
            Assert.That(bounded.Journal.lastAccrualUtcTicks, Is.Zero);
            Assert.That(bounded.Journal.entries.Count, Is.EqualTo(3));
            Assert.That(LocalPreviewManualProductionJournalCodec.PendingFor(bounded.Journal, "honey_storage"), Is.EqualTo(250f));
            Assert.That(LocalPreviewManualProductionJournalCodec.PendingFor(bounded.Journal, "wax_workshop"), Is.Zero);
            Assert.That(boundedStore.WriteCount, Is.EqualTo(1));
        }

        [Test]
        public void PresenterRestoresOfflineProductionAndKeepsCollectionManual()
        {
            var store = new MemoryProductionStore();
            try
            {
                HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
                HiveViewProductUiPresenter.UseLocalPreviewManualProductionStoreForProof(store);
                long now = DateTime.UtcNow.Ticks;
                HiveViewProductUiPresenter.RestoreLocalPreviewManualProductionAtForProof(now);
                HiveViewProductUiPresenter.SetManualProductionForProof("honey_storage", 0f, 61650f);
                HiveViewProductUiPresenter.PersistLocalPreviewManualProductionAtForProof(now);
                int honeyBefore = ProofInt(HiveViewProductUiPresenter.BroodCareForProof(), "honey");
                int collectionsBefore = ProofInt(HiveViewProductUiPresenter.ManualProductionCollectionForProof(), "manual_collection_count");

                long returnAt = now + TimeSpan.FromMinutes(30).Ticks;
                HiveViewProductUiPresenter.RestoreLocalPreviewManualProductionAtForProof(returnAt);
                string[] restored = HiveViewProductUiPresenter.LocalPreviewManualProductionForProof();
                AssertRow(restored, "production_honey_pending:1270");
                AssertRow(restored, "production_return_honey:1270");
                AssertRow(restored, "production_return_notice_visible:true");
                AssertRow(restored, "production_auto_credit:false");
                Assert.That(ProofInt(HiveViewProductUiPresenter.BroodCareForProof(), "honey"), Is.EqualTo(honeyBefore));

                HiveViewProductUiPresenter.NavigateToManualProductionReturnForProof();
                string[] routed = HiveViewProductUiPresenter.LocalPreviewManualProductionForProof();
                AssertRow(routed, "production_return_last_route:honey_storage");
                AssertRow(routed, "production_return_notice_visible:false");
                Assert.That(ProofInt(HiveViewProductUiPresenter.ManualProductionCollectionForProof(), "manual_collection_count"), Is.EqualTo(collectionsBefore));

                float collected = HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage");
                Assert.That(collected, Is.EqualTo(1270f).Within(0.1f));
                HiveViewProductUiPresenter.RestoreLocalPreviewManualProductionAtForProof(returnAt);
                AssertRow(HiveViewProductUiPresenter.LocalPreviewManualProductionForProof(), "production_honey_pending:0");
                Assert.That(store.WriteCount, Is.GreaterThanOrEqualTo(4));
            }
            finally
            {
                HiveViewProductUiPresenter.UseLocalPreviewManualProductionStoreForProof(null);
            }
        }

        [Test]
        public void ReturnNoticeIsLocalizedAndMobileSafe()
        {
            string[] keys =
            {
                "ui.production.return.title",
                "ui.production.return.body",
                "ui.production.return.disclosure",
                "ui.production.return.view",
                "ui.production.return.opened",
                "ui.production.building.disclosure",
                "ui.production.building.ready"
            };
            foreach (string key in keys)
            {
                Assert.That(BeeLocalization.HasText("fr-CA", key), Is.True, key + " missing in fr-CA");
                Assert.That(BeeLocalization.HasText("en-US", key), Is.True, key + " missing in en-US");
            }

            HiveViewProductUiPresenter.PrepareLocalPreviewManualProductionCaptureForProof("return_fr");
            Rect portrait = HiveViewProductUiPresenter.ManualProductionReturnPanelRectForProof(true, 390f, 844f);
            Assert.That(portrait.x, Is.GreaterThanOrEqualTo(0f));
            Assert.That(portrait.y, Is.GreaterThanOrEqualTo(0f));
            Assert.That(portrait.xMax, Is.LessThanOrEqualTo(390f));
            Assert.That(portrait.yMax, Is.LessThanOrEqualTo(844f - 94f));
            Assert.That(portrait.width, Is.GreaterThanOrEqualTo(320f));
            Assert.That(portrait.height, Is.GreaterThanOrEqualTo(124f));

            Rect landscape = HiveViewProductUiPresenter.ManualProductionReturnPanelRectForProof(false, 1600f, 900f);
            Assert.That(landscape.xMax, Is.LessThanOrEqualTo(1600f));
            Assert.That(landscape.yMax, Is.LessThanOrEqualTo(900f - 90f));
            AssertRow(HiveViewProductUiPresenter.LocalPreviewManualProductionForProof(), "production_collection:manual_only");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewManualProductionForProof(), "production_device_cache_protected:false");
        }

        private static IReadOnlyList<LocalPreviewManualProductionRule> FixedRules()
        {
            return new[]
            {
                new LocalPreviewManualProductionRule("honey_storage", 360f, 1000f),
                new LocalPreviewManualProductionRule("wax_workshop", 720f, 500f),
                new LocalPreviewManualProductionRule("warehouse_cells", 180f, 200f)
            };
        }

        private static void AssertRow(IEnumerable<string> rows, string expected)
        {
            Assert.That(rows, Does.Contain(expected));
        }

        private static int ProofInt(IEnumerable<string> rows, string key)
        {
            string prefix = key + ":";
            string row = rows.Single(value => value.StartsWith(prefix, StringComparison.Ordinal));
            return int.Parse(row.Substring(prefix.Length), CultureInfo.InvariantCulture);
        }

        private sealed class MemoryProductionStore : ILocalPreviewManualProductionJournalStore
        {
            private string json;

            public MemoryProductionStore(string initialJson = "")
            {
                json = initialJson ?? string.Empty;
            }

            public int WriteCount { get; private set; }
            public string Read() => json;
            public void Write(string value)
            {
                json = value ?? string.Empty;
                WriteCount++;
            }
            public void Delete() => json = string.Empty;
        }
    }
}
