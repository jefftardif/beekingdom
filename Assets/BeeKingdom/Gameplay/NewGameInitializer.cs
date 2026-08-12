using BeeKingdom.AI;
using BeeKingdom.Economy;
using BeeKingdom.Hive;

namespace BeeKingdom.Gameplay
{
    public sealed class NewGameInitializer
    {
        public PlayableHiveState CreateNewGame(StarterHiveProfile hiveProfile, StarterPopulationProfile populationProfile, StarterResourceProfile resourceProfile)
        {
            hiveProfile = hiveProfile ?? StarterHiveProfile.CreateDefault();
            populationProfile = populationProfile ?? StarterPopulationProfile.CreateDefault();
            resourceProfile = resourceProfile ?? StarterResourceProfile.CreateDefault();

            ResourceFlowManager resourceFlow = new ResourceFlowManager();
            HiveInventoryManager inventory = new HiveInventoryManager(resourceFlow);
            TaskManager tasks = new TaskManager();
            HiveManager hives = new HiveManager();
            QueenManager queens = new QueenManager();
            BeeLifecycleManager lifecycle = new BeeLifecycleManager(populationProfile.LifecycleRules);
            BeeAIManager ai = new BeeAIManager();
            HiveGrowthManager growth = new HiveGrowthManager(null, tasks);

            PlayableHiveState state = new PlayableHiveState
            {
                HiveId = hiveProfile.HiveId,
                QueenId = hiveProfile.QueenBeeId,
                HiveManager = hives,
                QueenManager = queens,
                LifecycleManager = lifecycle,
                TaskManager = tasks,
                AIManager = ai,
                ResourceFlowManager = resourceFlow,
                InventoryManager = inventory,
                GrowthManager = growth
            };

            hives.CreateHive(hiveProfile.HiveId, hiveProfile.OwnerId, hiveProfile.QueenBeeId, hiveProfile.Capacity);
            queens.CreateQueen(hiveProfile.QueenBeeId, hiveProfile.HiveId, new QueenHealth(100, 100), populationProfile.Energy, 1f, hiveProfile.QueenBaseEggsPerMinute);
            CreateBee(state, hiveProfile.QueenBeeId, BeeLifecycleRole.Queen, populationProfile);
            CreateBees(state, "worker", BeeLifecycleRole.Worker, populationProfile.InitialWorkers, populationProfile);
            CreateBees(state, "nurse", BeeLifecycleRole.Nurse, populationProfile.InitialNurses, populationProfile);
            CreateBees(state, "builder", BeeLifecycleRole.Builder, populationProfile.InitialBuilders, populationProfile);
            CreateBees(state, "scout", BeeLifecycleRole.Scout, populationProfile.InitialScouts, populationProfile);
            CreateBees(state, "soldier", BeeLifecycleRole.Soldier, populationProfile.InitialSoldiers, populationProfile);

            SeedResources(resourceProfile, resourceFlow, inventory);
            SeedChambers(hiveProfile, growth, resourceProfile);

            state.Controller = new ColonySimulationController(state, hiveProfile, resourceProfile);
            state.Diagnostics.RecordPopulation(state.BeeIds.Count);
            state.Diagnostics.RecordResources(inventory.QueryInventory().TotalAmount);
            return state;
        }

        private static void CreateBees(PlayableHiveState state, string prefix, BeeLifecycleRole role, int count, StarterPopulationProfile profile)
        {
            for (int i = 0; i < count; i++)
            {
                CreateBee(state, prefix + "-" + (i + 1), role, profile);
            }
        }

        private static void CreateBee(PlayableHiveState state, string beeId, BeeLifecycleRole role, StarterPopulationProfile profile)
        {
            state.LifecycleManager.CreateBee(beeId, state.HiveId, 0d, role, profile.Health, profile.Energy, "starter");
            if (role != BeeLifecycleRole.Queen)
            {
                state.HiveManager.AddBee(state.HiveId, beeId);
                state.AIManager.CreateBrain(beeId, profile.Energy, profile.Health);
            }

            state.AddBee(beeId);
        }

        private static void SeedResources(StarterResourceProfile profile, ResourceFlowManager resourceFlow, HiveInventoryManager inventory)
        {
            int index = 0;
            foreach (var pair in profile.Amounts)
            {
                string cellId = "starter-storage-" + (++index);
                inventory.CreateCell(cellId, new StoragePosition(index, 0), pair.Key, profile.CellCapacity, pair.Key + "-cluster");
                StorageReservation reservation = inventory.ReserveSpace(pair.Key, pair.Value, new StoragePosition(index, 0), StoragePolicy.Nearest);
                inventory.Deposit(reservation, 0d);
                resourceFlow.Store("colony-reserve", pair.Key, pair.Value, 0d);
            }
        }

        private static void SeedChambers(StarterHiveProfile profile, HiveGrowthManager growth, StarterResourceProfile resources)
        {
            string previous = null;
            foreach (HiveChamberType chamberType in profile.StartingChambers)
            {
                HiveExpansionPlan plan = growth.PlanExpansion(new HiveExpansionRequest(chamberType, 20, 1000d, 28d, true, profile.UnlockedTechnologyIds));
                ConstructionSite site = growth.CreateChamber(plan, previous);
                growth.Execute(SimulationContextFactory.Create(plan.RequiredWorkSeconds));
                previous = site.ChamberId;
            }
        }
    }
}
