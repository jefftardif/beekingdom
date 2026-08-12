using System;
using System.Collections.Generic;
using BeeKingdom.Localization;
using BeeKingdom.Networking;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxLivingHiveOfficialDailyRoundTests
    {
        private static readonly Guid PlayerId =
            Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Guid HiveId =
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly DateTimeOffset Day =
            new DateTimeOffset(2026, 7, 23, 0, 0, 0, TimeSpan.Zero);

        public static void RunAllAssertions()
        {
            var tests = new SandboxLivingHiveOfficialDailyRoundTests();
            tests.NotConfiguredProofShowsNoLocalOrInventedState();
            tests.ReadyProjectionUsesOnlyServerFactsAndReward();
            tests.OfflineProjectionNeverEnablesAClaim();
            tests.QuestBadgeUsesOfficialClaimAvailability();
            tests.PortraitAndLandscapeActionsRemainBoundedAtFortyFourPixels();
            tests.OfficialDailyRoundCopyExistsInBothCatalogs();
        }

        [Test]
        public void NotConfiguredProofShowsNoLocalOrInventedState()
        {
            try
            {
                HiveViewProductUiPresenter.UseDailyRoundControllerForProof(
                    new FakePanelController(
                        HiveDailyRoundPresentation.NotConfigured()));
                string[] rows =
                    HiveViewProductUiPresenter.OfficialDailyRoundForProof();

                Assert.That(rows, Does.Contain("daily_round_authority:server"));
                Assert.That(
                    rows,
                    Does.Contain("daily_round_official_has_snapshot:false"));
                Assert.That(
                    rows,
                    Does.Contain("daily_round_official_local_fact_submission:false"));
                Assert.That(
                    rows,
                    Does.Contain("daily_round_official_local_reward_credit:false"));
                Assert.That(
                    rows,
                    Does.Contain("daily_round_official_auto_submit:false"));
            }
            finally
            {
                HiveViewProductUiPresenter.UseDailyRoundControllerForProof(null);
            }
        }

        [Test]
        public void ReadyProjectionUsesOnlyServerFactsAndReward()
        {
            RemoteHiveDailyRoundSnapshot snapshot = Snapshot();
            HiveDailyRoundScreenModel model =
                HiveDailyRoundPresentation.Ready(snapshot, true);

            Assert.That(model.CollectionReceived, Is.True);
            Assert.That(model.OperationLaunched, Is.True);
            Assert.That(model.SnapshotRead, Is.True);
            Assert.That(model.CompletedCount, Is.EqualTo(3));
            Assert.That(model.HoneyReward, Is.EqualTo(120));
            Assert.That(model.PollenReward, Is.EqualTo(60));
            Assert.That(model.CanClaim, Is.True);

            snapshot.Facts[HiveDailyRoundClient.CollectionFact] = false;
            snapshot.HoneyReward = 999;
            Assert.That(model.CollectionReceived, Is.True);
            Assert.That(model.HoneyReward, Is.EqualTo(120));
        }

        [Test]
        public void OfflineProjectionNeverEnablesAClaim()
        {
            HiveDailyRoundScreenModel model =
                HiveDailyRoundPresentation.OfflineReadOnly(
                    Snapshot(),
                    Day.AddHours(12),
                    true);

            Assert.That(model.IsReadOnly, Is.True);
            Assert.That(model.CanClaim, Is.False);
            Assert.That(model.CanRetryClaim, Is.False);
        }

        [Test]
        public void QuestBadgeUsesOfficialClaimAvailability()
        {
            try
            {
                HiveViewProductUiPresenter.UseDailyRoundControllerForProof(
                    new FakePanelController(
                        HiveDailyRoundPresentation.Ready(
                            Snapshot(),
                            true)));
                Assert.That(
                    HiveViewProductUiPresenter.MenuBadgeTextForProof("Quests"),
                    Is.EqualTo("!"));

                RemoteHiveDailyRoundSnapshot claimed = Snapshot();
                claimed.ClaimAvailable = false;
                claimed.ClaimedAtUtc = Day.AddHours(12);
                HiveViewProductUiPresenter.UseDailyRoundControllerForProof(
                    new FakePanelController(
                        HiveDailyRoundPresentation.Ready(
                            claimed,
                            true)));
                Assert.That(
                    HiveViewProductUiPresenter.MenuBadgeTextForProof("Quests"),
                    Is.Empty);
            }
            finally
            {
                HiveViewProductUiPresenter.UseDailyRoundControllerForProof(null);
            }
        }

        [Test]
        public void PortraitAndLandscapeActionsRemainBoundedAtFortyFourPixels()
        {
            RectAssert(
                HiveViewProductUiPresenter
                    .OfficialDailyRoundActionRectsForProof(
                        true,
                        390f,
                        844f),
                390f,
                844f);
            RectAssert(
                HiveViewProductUiPresenter
                    .OfficialDailyRoundActionRectsForProof(
                        false,
                        1600f,
                        900f),
                1600f,
                900f);
        }

        [Test]
        public void OfficialDailyRoundCopyExistsInBothCatalogs()
        {
            string[] keys =
            {
                "daily_round.official.refresh",
                "daily_round.official.authority.title",
                "daily_round.official.authority.body",
                "daily_round.official.day",
                "daily_round.official.task.collect.body",
                "daily_round.official.task.operation.body",
                "daily_round.official.task.ledger.body",
                "daily_round.official.reward",
                "daily_round.official.retry",
                "daily_round.official.not_configured",
                "daily_round.official.loading",
                "daily_round.official.offline",
                "daily_round.official.pending",
                "daily_round.official.ready",
                "daily_round.official.claim.protection",
                "daily_round.official.claim.ready"
            };

            foreach (string key in keys)
            {
                Assert.That(
                    BeeLocalization.HasText("fr-CA", key),
                    Is.True,
                    key + " fr-CA");
                Assert.That(
                    BeeLocalization.HasText("en-US", key),
                    Is.True,
                    key + " en-US");
            }
        }

        private static void RectAssert(
            IReadOnlyList<Rect> rects,
            float width,
            float height)
        {
            Assert.That(rects.Count, Is.EqualTo(5));
            foreach (Rect rect in rects)
            {
                Assert.That(rect.width, Is.GreaterThanOrEqualTo(44f));
                Assert.That(rect.height, Is.GreaterThanOrEqualTo(44f));
                Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(0f));
                Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(0f));
                Assert.That(rect.xMax, Is.LessThanOrEqualTo(width));
                Assert.That(rect.yMax, Is.LessThanOrEqualTo(height));
            }
        }

        private static RemoteHiveDailyRoundSnapshot Snapshot()
        {
            return new RemoteHiveDailyRoundSnapshot
            {
                PlayerId = PlayerId,
                HiveId = HiveId,
                ContractVersion = HiveDailyRoundClient.ContractVersion,
                DayUtc = Day,
                NextResetUtc = Day.AddDays(1),
                ServerTimeUtc = Day.AddHours(12),
                Revision = 7,
                Facts = new Dictionary<string, bool>
                {
                    [HiveDailyRoundClient.CollectionFact] = true,
                    [HiveDailyRoundClient.OperationFact] = true,
                    [HiveDailyRoundClient.SnapshotFact] = true
                },
                CompletedCount = 3,
                HoneyReward = HiveDailyRoundClient.HoneyReward,
                PollenReward = HiveDailyRoundClient.PollenReward,
                ClaimAvailable = true
            };
        }

        private sealed class FakePanelController :
            IHiveDailyRoundPanelController
        {
            public FakePanelController(HiveDailyRoundScreenModel model)
            {
                Model = model;
            }

            public HiveDailyRoundScreenModel Model { get; }
            public bool IsConfigured => true;
            public bool IsBusy => false;
            public void Refresh() { }
            public void Claim() { }
            public void RetryClaim() { }
        }
    }
}
