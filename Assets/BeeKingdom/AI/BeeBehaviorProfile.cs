using BeeKingdom.Hive;

namespace BeeKingdom.AI
{
    public sealed class BeeBehaviorProfile
    {
        public BeeBehaviorState ResolveState(ColonyTaskType taskType)
        {
            if (taskType == ColonyTaskType.HarvestNectar || taskType == ColonyTaskType.HarvestPollen) return BeeBehaviorState.Harvesting;
            if (taskType == ColonyTaskType.CollectWater) return BeeBehaviorState.Harvesting;
            if (taskType == ColonyTaskType.FeedLarvae) return BeeBehaviorState.Feeding;
            if (taskType == ColonyTaskType.BuildCell) return BeeBehaviorState.Building;
            if (taskType == ColonyTaskType.RepairHive) return BeeBehaviorState.Repairing;
            if (taskType == ColonyTaskType.DefendHive) return BeeBehaviorState.Guarding;
            if (taskType == ColonyTaskType.Explore) return BeeBehaviorState.Exploring;
            if (taskType == ColonyTaskType.Idle) return BeeBehaviorState.Idle;
            return BeeBehaviorState.Waiting;
        }
    }
}
