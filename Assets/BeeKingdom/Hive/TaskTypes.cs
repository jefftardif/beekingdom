namespace BeeKingdom.Hive
{
    public enum ColonyTaskType
    {
        HarvestNectar,
        HarvestPollen,
        CollectWater,
        FeedLarvae,
        ProduceWax,
        ProduceHoney,
        BuildCell,
        RepairHive,
        CleanHive,
        DefendHive,
        Explore,
        Idle
    }

    public enum TaskLifecycleState
    {
        Created,
        Queued,
        Reserved,
        Assigned,
        Executing,
        Completed,
        Cancelled,
        Failed
    }
}
