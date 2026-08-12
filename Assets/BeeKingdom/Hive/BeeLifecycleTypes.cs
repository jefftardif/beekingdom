namespace BeeKingdom.Hive
{
    public enum BeeLifecycleStage
    {
        Egg,
        Larva,
        Pupa,
        YoungWorker,
        AdultWorker,
        SeniorWorker,
        Dead
    }

    public enum BeeLifecycleRole
    {
        Worker,
        Queen,
        Drone,
        Soldier,
        Nurse,
        Builder,
        Scout
    }

    public enum BeeMortalityCause
    {
        OldAge,
        Starvation,
        Disease,
        Combat,
        Event,
        Script
    }
}
