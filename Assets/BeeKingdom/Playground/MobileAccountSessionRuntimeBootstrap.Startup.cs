using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public static partial class MobileAccountSessionRuntimeBootstrap
    {
        private static HiveGameplayBindingIdentity gameplayBinding;
        private static HiveStartupReadiness startupReadiness;
        private static HiveStartupStepState startupEnsureState, startupBankState;
        private static RemoteRoyalReserveSnapshot startupBankSnapshot;
        private static CancellationTokenSource startupLifetime;
        private static int startupGeneration;
        private static bool startupAuthenticationInitializing;

        public static bool StartupAuthenticationInitializing => startupAuthenticationInitializing;
        public static bool StartupRequiresOfficialData => activeConfiguration != null && activeConfiguration.OfficialGameplayEnabled && client != null &&
            client.State == MobileAccountSessionState.Authenticated && client.ServerGameplayAuthorityGranted;
        public static HiveStartupSnapshot StartupSnapshot => startupReadiness?.Poll();
        public static bool StartupDataReady => StartupSnapshot?.Ready == true;

        private static void ResetStartupSession()
        {
            unchecked { startupGeneration++; }
            startupLifetime?.Cancel();
            startupLifetime?.Dispose();
            startupLifetime = null;
            gameplayBinding = null;
            startupReadiness = null;
            startupBankSnapshot = null;
            startupEnsureState = startupBankState = HiveStartupStepState.Waiting;
        }

        private static bool CanReuseGameplayBinding(Guid playerId, Guid hiveId, string server, bool online)
        {
            return gameplayBinding != null && gameplayBinding.Matches(client, playerId, hiveId, server, online)
                && buildingUpgradeController != null && buildingUpgradeController.IsConfigured
                && offlineProductionController != null && offlineProductionController.IsConfigured
                && stockController != null && stockController.IsConfigured;
        }

        private static void BeginStartupDataTracking()
        {
            startupLifetime = new CancellationTokenSource();
            startupEnsureState = startupBankState = HiveStartupStepState.Waiting;
            // Capture this generation's controllers. A subsequent login cannot satisfy this barrier.
            var buildings = buildingUpgradeController; var production = offlineProductionController;
            var population = doctrineRecruitmentController; var research = researchController;
            var army = combatPatrolController; var alliance = allianceCenterController;
            var brood = broodVitalityController; var quests = questChainController;
            var mail = courierMailboxController; var inventory = speedUpController;
            var daily = dailyRoundController; var stock = stockController;
            startupReadiness = new HiveStartupReadiness(new[]
            {
                new HiveStartupStep("hive", true, () => startupEnsureState),
                new HiveStartupStep("buildings", true, () => HiveStartupReadiness.FromModel(
                    buildings.Model?.State.ToString(), buildings.Model?.BuildingLevels?.Count > 0)),
                new HiveStartupStep("resources", true, () => HiveStartupReadiness.FromModel(
                    production.Model?.State.ToString(), production.Model?.FindLine("honey_storage") != null
                        && production.Model.FindLine("wax_workshop") != null
                        && production.Model.FindLine("warehouse_cells") != null)),
                new HiveStartupStep("population", true, () => HiveStartupReadiness.FromModel(population.Model?.State.ToString())),
                new HiveStartupStep("research", false, () => HiveStartupReadiness.FromModel(research.Model?.State.ToString())),
                new HiveStartupStep("army", false, () => HiveStartupReadiness.FromModel(army.Model?.State.ToString())),
                new HiveStartupStep("alliance", false, () => HiveStartupReadiness.FromModel(alliance.Model?.State.ToString())),
                new HiveStartupStep("bank", false, () => startupBankState),
                new HiveStartupStep("nursery", false, () => HiveStartupReadiness.FromModel(brood.Model?.State.ToString())),
                new HiveStartupStep("quests", false, () => HiveStartupReadiness.FromModel(quests.Model?.State.ToString())),
                new HiveStartupStep("mail", false, () => HiveStartupReadiness.FromModel(mail.Model?.State.ToString())),
                new HiveStartupStep("inventory", false, () => HiveStartupReadiness.FromModel(inventory.Model?.State.ToString())),
                new HiveStartupStep("daily", false, () => HiveStartupReadiness.FromModel(daily.Model?.State.ToString())),
                // Detailed stock ledger is optional: HUD balances come from the authoritative production snapshot.
                new HiveStartupStep("stock", false, () => HiveStartupReadiness.FromModel(stock.Model?.State.ToString()))
            }, () => Time.realtimeSinceStartupAsDouble, 45);
        }

        private static void RefreshStartupReads()
        {
            // Existing read paths only: no harvest, reward claim, care, recruitment or bank transfer.
            combatPatrolController.Refresh();
            strategicPathController.Refresh();
            worldResourceCollectionController.Refresh();
            worldPresenceController.Refresh();
            bestiaryCodexController.Refresh();
            milestoneEventController.Refresh();
            questChainController.Refresh();
            courierMailboxController.Refresh();
            speedUpController.Refresh();
            rewardLedgerController.Refresh();
            gameplayController.Refresh();
            squadReservationController.Refresh();
            offlineProductionController.Refresh();
            buildingUpgradeController.Refresh();
            researchController.Refresh();
            allianceCenterController.Refresh();
            stockController.Refresh();
            dailyRoundController.Refresh();
            broodVitalityController.Refresh();
            doctrineRecruitmentController.Refresh();
            WarmStartupBankAsync(startupGeneration, royalReserveClient, gameplayHiveId, startupLifetime.Token);
        }

        private static async void WarmStartupBankAsync(int generation, IRoyalReserveClient bank, Guid hiveId, CancellationToken token)
        {
            try
            {
                var snapshot = await bank.ReadAsync(hiveId, token);
                if (generation != startupGeneration || token.IsCancellationRequested || !ReferenceEquals(bank, royalReserveClient)) return;
                startupBankSnapshot = snapshot;
                startupBankState = HiveStartupStepState.Ready;
            }
            catch (OperationCanceledException) { }
            catch (Exception)
            {
                if (generation == startupGeneration) startupBankState = HiveStartupStepState.Unavailable;
            }
        }

        internal static RemoteRoyalReserveSnapshot TakeStartupBankSnapshot()
        {
            var snapshot = startupBankSnapshot;
            startupBankSnapshot = null; // use once; subsequent openings refresh using the normal bank view
            return snapshot != null && snapshot.PlayerId == gameplayPlayerId && snapshot.HiveId == gameplayHiveId
                ? snapshot : null;
        }

        public static void RetryStartupSynchronization()
        {
            // Explicit retry only. It cancels this failed generation before starting another.
            gameplayBinding = null;
            TryConfigureGameplayForActiveSession();
        }
    }
}