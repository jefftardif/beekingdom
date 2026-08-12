namespace BeeKingdom.Core.Effects
{
    public sealed class GameplayEffectDiagnostics
    {
        public int RegisteredEffects { get; private set; }
        public int AppliedEffects { get; private set; }
        public int RemovedEffects { get; private set; }
        public int ExpiredEffects { get; private set; }
        public int SuspendedEffects { get; private set; }

        public void RecordRegistered(int count) { RegisteredEffects = count; }
        public void RecordApplied() { AppliedEffects++; }
        public void RecordRemoved() { RemovedEffects++; }
        public void RecordExpired() { ExpiredEffects++; }
        public void RecordSuspended() { SuspendedEffects++; }
    }
}
