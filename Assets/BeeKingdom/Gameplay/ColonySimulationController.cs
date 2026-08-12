using System;
using BeeKingdom.Core.Simulation;
using BeeKingdom.Economy;
using BeeKingdom.Hive;

namespace BeeKingdom.Gameplay
{
    public sealed class ColonySimulationController : ISimulationSystem
    {
        private readonly PlayableHiveState state;
        private readonly StarterHiveProfile hiveProfile;
        private readonly StarterResourceProfile resourceProfile;
        private double elapsedSeconds;
        private double eggAccumulator;
        private int newbornCounter;

        public Type SystemType => typeof(ColonySimulationController);
        public string Name => nameof(ColonySimulationController);
        public SimulationPhase Phase => SimulationPhase.PostSimulation;
        public int Priority => 500;
        public System.Collections.Generic.IReadOnlyList<Type> RunsAfter => new[] { typeof(HiveGrowthManager), typeof(TaskManager) };
        public System.Collections.Generic.IReadOnlyList<Type> RunsBefore => Array.Empty<Type>();

        public ColonySimulationController(PlayableHiveState state, StarterHiveProfile hiveProfile, StarterResourceProfile resourceProfile)
        {
            this.state = state;
            this.hiveProfile = hiveProfile;
            this.resourceProfile = resourceProfile;
        }

        public void Execute(in SimulationExecutionContext context)
        {
            try
            {
                double delta = context.DeltaSeconds * hiveProfile.SimulationSpeed;
                elapsedSeconds += delta;
                state.QueenManager.Execute(context);
                state.LifecycleManager.Execute(context);
                state.GrowthManager.Execute(context);
                ProduceEggs(delta);
                ProduceAndStoreResources(delta);
                AssignOneTask();
                RecordDiagnostics(context);
            }
            catch
            {
                state.Diagnostics.RecordError();
                throw;
            }
        }

        public string CreateSavePayload()
        {
            return "hive=" + state.HiveId + ";queen=" + state.QueenId + ";population=" + state.BeeIds.Count + ";seconds=" + elapsedSeconds.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        }

        private void ProduceEggs(double deltaSeconds)
        {
            eggAccumulator += state.QueenManager.ProduceEggs(state.QueenId, deltaSeconds);
            while (eggAccumulator >= 1d)
            {
                eggAccumulator -= 1d;
                string beeId = "newborn-" + (++newbornCounter);
                state.LifecycleManager.CreateBee(beeId, state.HiveId, elapsedSeconds, BeeLifecycleRole.Worker, 100, 100, "starter-born");
                state.HiveManager.AddBee(state.HiveId, beeId);
                state.AIManager.CreateBrain(beeId, 100, 100);
                state.AddBee(beeId);
            }
        }

        private void ProduceAndStoreResources(double deltaSeconds)
        {
            double workers = Math.Max(1, state.BeeIds.Count - 1);
            Store(ResourceType.Nectar, workers * 0.01d * deltaSeconds);
            Store(ResourceType.Pollen, workers * 0.006d * deltaSeconds);
            Store(ResourceType.Wax, workers * 0.002d * deltaSeconds);
        }

        private void Store(ResourceType type, double amount)
        {
            if (amount <= 0d)
            {
                return;
            }

            StorageReservation reservation = state.InventoryManager.ReserveSpace(type, Math.Min(amount, resourceProfile.CellCapacity), new StoragePosition(0, 0), StoragePolicy.Balanced);
            if (reservation.IsValid)
            {
                state.InventoryManager.Deposit(reservation, elapsedSeconds);
            }

            state.ResourceFlowManager.Produce("foragers", "colony-reserve", type, amount, elapsedSeconds);
        }

        private void AssignOneTask()
        {
            foreach (TaskInstance task in state.TaskManager.GetAvailableTasks())
            {
                foreach (string beeId in state.BeeIds)
                {
                    if (beeId == state.QueenId)
                    {
                        continue;
                    }

                    TaskReservation reservation = state.TaskManager.ReserveTask(task.TaskId, beeId, elapsedSeconds, task.Definition.EstimatedDurationSeconds);
                    if (!reservation.IsValid)
                    {
                        continue;
                    }

                    if (state.TaskManager.AssignTask(task.TaskId, beeId))
                    {
                        state.AIManager.AssignTask(beeId, task);
                    }

                    return;
                }
            }
        }

        private void RecordDiagnostics(in SimulationExecutionContext context)
        {
            state.Diagnostics.RecordPopulation(state.BeeIds.Count);
            state.Diagnostics.RecordResources(state.InventoryManager.QueryInventory().TotalAmount);
            state.Diagnostics.RecordTasks(state.TaskManager.GetStatistics().QueuedTasks + state.TaskManager.GetStatistics().AssignedTasks);
            state.Diagnostics.RecordTick(elapsedSeconds, context.DeltaSeconds);
        }
    }
}
