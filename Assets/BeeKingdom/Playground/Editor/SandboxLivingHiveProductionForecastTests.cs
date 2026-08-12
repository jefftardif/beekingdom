using System;
using System.Collections.Generic;
using BeeKingdom.Localization;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxLivingHiveProductionForecastTests
    {
        public static void RunAllAssertions()
        {
            var tests = new SandboxLivingHiveProductionForecastTests();
            tests.CalculatorComputesDeterministicTimeUntilFull();
            tests.CalculatorBoundsInvalidValuesAndFindsEarliestFullStock();
            tests.LedgerExposesAllForecastInputsWithoutChangingAuthority();
            tests.ForecastNavigationNeverCollectsOrCreditsResources();
            tests.ForecastCopyAndPanelAreLocalizedAndMobileSafe();
        }

        [Test]
        public void CalculatorComputesDeterministicTimeUntilFull()
        {
            ManualProductionForecast forecast = ManualProductionForecast.Calculate(50d, 100d, 100d);

            Assert.That(forecast.State, Is.EqualTo(ManualProductionForecastState.Producing));
            Assert.That(forecast.Pending, Is.EqualTo(50d));
            Assert.That(forecast.Capacity, Is.EqualTo(100d));
            Assert.That(forecast.RatePerHour, Is.EqualTo(100d));
            Assert.That(forecast.Fill01, Is.EqualTo(0.5d));
            Assert.That(forecast.SecondsUntilFull, Is.EqualTo(1800d));
        }

        [Test]
        public void CalculatorBoundsInvalidValuesAndFindsEarliestFullStock()
        {
            ManualProductionForecast bounded = ManualProductionForecast.Calculate(-20d, 100d, 50d);
            ManualProductionForecast full = ManualProductionForecast.Calculate(500d, 100d, 0d);
            ManualProductionForecast unavailable = ManualProductionForecast.Calculate(10d, 100d, double.NaN);

            Assert.That(bounded.Pending, Is.Zero);
            Assert.That(bounded.SecondsUntilFull, Is.EqualTo(7200d));
            Assert.That(full.Pending, Is.EqualTo(100d));
            Assert.That(full.State, Is.EqualTo(ManualProductionForecastState.Full));
            Assert.That(full.SecondsUntilFull, Is.Zero);
            Assert.That(unavailable.State, Is.EqualTo(ManualProductionForecastState.Unavailable));
            Assert.That(unavailable.SecondsUntilFull, Is.EqualTo(-1d));
            Assert.That(
                ManualProductionForecast.TryEarliestSecondsUntilFull(new[] { bounded, unavailable, full }, out double earliest),
                Is.True);
            Assert.That(earliest, Is.Zero);
        }

        [Test]
        public void LedgerExposesAllForecastInputsWithoutChangingAuthority()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
            HiveViewProductUiPresenter.SetManualProductionForProof("honey_storage", 840f, 61650f);
            HiveViewProductUiPresenter.SetManualProductionForProof("wax_workshop", 420f, 61650f);
            HiveViewProductUiPresenter.SetManualProductionForProof("warehouse_cells", 630f, 61650f);

            string[] rows = HiveViewProductUiPresenter.HiveLedgerForProof();
            AssertRow(rows, "ledger_forecast_display_authority:device_derived");
            AssertRow(rows, "ledger_forecast_official_source:server_snapshot_required");
            AssertRow(rows, "ledger_official_authority:server");
            AssertRow(rows, "ledger_direct_collection:false");
            AssertRow(rows, "ledger_honey_forecast_state:producing");
            AssertRow(rows, "ledger_wax_forecast_state:producing");
            AssertRow(rows, "ledger_pollen_forecast_state:producing");
            AssertPositive(rows, "ledger_honey_capacity");
            AssertPositive(rows, "ledger_honey_rate_per_hour");
            AssertPositive(rows, "ledger_honey_seconds_until_full");
            AssertPositive(rows, "ledger_wax_seconds_until_full");
            AssertPositive(rows, "ledger_pollen_seconds_until_full");
            AssertPositive(rows, "ledger_earliest_seconds_until_full");
        }

        [Test]
        public void ForecastNavigationNeverCollectsOrCreditsResources()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
            HiveViewProductUiPresenter.SetManualProductionForProof("honey_storage", 840f, 61650f);
            string[] beforeLedger = HiveViewProductUiPresenter.HiveLedgerForProof();
            string beforeAvailable = Value(beforeLedger, "ledger_honey_available");
            string beforePending = Value(beforeLedger, "ledger_honey_pending");
            int collectionsBefore = IntValue(HiveViewProductUiPresenter.ManualProductionCollectionForProof(), "manual_collection_count");

            HiveViewProductUiPresenter.OpenHiveLedgerResourceForProof("honey_storage");

            string[] afterLedger = HiveViewProductUiPresenter.HiveLedgerForProof();
            Assert.That(Value(afterLedger, "ledger_honey_available"), Is.EqualTo(beforeAvailable));
            Assert.That(Value(afterLedger, "ledger_honey_pending"), Is.EqualTo(beforePending));
            Assert.That(
                IntValue(HiveViewProductUiPresenter.ManualProductionCollectionForProof(), "manual_collection_count"),
                Is.EqualTo(collectionsBefore));
            AssertRow(afterLedger, "ledger_last_navigation:honey_storage");
        }

        [Test]
        public void ForecastCopyAndPanelAreLocalizedAndMobileSafe()
        {
            string[] keys =
            {
                "ledger.forecast.disclosure",
                "ledger.forecast.pending_capacity",
                "ledger.forecast.rate_status",
                "ledger.forecast.unavailable",
                "ledger.forecast.full",
                "ledger.forecast.full_in",
                "ledger.forecast.full_now",
                "ledger.forecast.next_full",
                "ledger.forecast.duration.day_hour",
                "ledger.forecast.duration.hour_minute",
                "ledger.forecast.duration.minute"
            };
            foreach (string key in keys)
            {
                Assert.That(BeeLocalization.HasText("fr-CA", key), Is.True, key + " missing in fr-CA");
                Assert.That(BeeLocalization.HasText("en-US", key), Is.True, key + " missing in en-US");
            }

            AssertPanelFits(true, 390f, 844f);
            AssertPanelFits(false, 1600f, 900f);
            AssertRow(HiveViewProductUiPresenter.HiveLedgerForProof(), "ledger_min_touch_size:44");
        }

        private static void AssertPanelFits(bool portrait, float width, float height)
        {
            Rect panel = HiveViewProductUiPresenter.HiveLedgerPanelRectForProof(portrait, width, height);
            Assert.That(panel.xMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(panel.yMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(panel.xMax, Is.LessThanOrEqualTo(width));
            Assert.That(panel.yMax, Is.LessThanOrEqualTo(height - (portrait ? 78f : 70f)));
        }

        private static void AssertPositive(IEnumerable<string> rows, string key)
        {
            Assert.That(double.Parse(Value(rows, key), System.Globalization.CultureInfo.InvariantCulture), Is.GreaterThan(0d), key);
        }

        private static int IntValue(IEnumerable<string> rows, string key)
        {
            return int.Parse(Value(rows, key), System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string Value(IEnumerable<string> rows, string key)
        {
            string prefix = key + ":";
            foreach (string row in rows)
                if (row.StartsWith(prefix, StringComparison.Ordinal)) return row.Substring(prefix.Length);
            Assert.Fail("Missing proof row " + key);
            return string.Empty;
        }

        private static void AssertRow(IEnumerable<string> rows, string expected)
        {
            Assert.That(rows, Does.Contain(expected));
        }
    }
}
