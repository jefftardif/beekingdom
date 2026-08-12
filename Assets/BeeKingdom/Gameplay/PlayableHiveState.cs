using System.Collections.Generic;
using BeeKingdom.AI;
using BeeKingdom.Economy;
using BeeKingdom.Hive;

namespace BeeKingdom.Gameplay
{
    public sealed class PlayableHiveState
    {
        private readonly List<string> beeIds = new List<string>();

        public string HiveId { get; set; }
        public string QueenId { get; set; }
        public IReadOnlyList<string> BeeIds => beeIds;
        public HiveManager HiveManager { get; set; }
        public QueenManager QueenManager { get; set; }
        public BeeLifecycleManager LifecycleManager { get; set; }
        public TaskManager TaskManager { get; set; }
        public BeeAIManager AIManager { get; set; }
        public ResourceFlowManager ResourceFlowManager { get; set; }
        public HiveInventoryManager InventoryManager { get; set; }
        public HiveGrowthManager GrowthManager { get; set; }
        public ColonySimulationController Controller { get; set; }
        public IntegrationDiagnostics Diagnostics { get; } = new IntegrationDiagnostics();

        public void AddBee(string beeId)
        {
            if (!beeIds.Contains(beeId))
            {
                beeIds.Add(beeId);
            }
        }
    }
}
