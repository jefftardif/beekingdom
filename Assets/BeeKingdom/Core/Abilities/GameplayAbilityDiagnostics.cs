namespace BeeKingdom.Core.Abilities
{
    public sealed class GameplayAbilityDiagnostics
    {
        public int RegisteredAbilities { get; private set; }
        public int RequestedAbilities { get; private set; }
        public int CompletedAbilities { get; private set; }
        public int CancelledAbilities { get; private set; }
        public int InterruptedAbilities { get; private set; }
        public int FailedAbilities { get; private set; }

        public void RecordRegistered(int count)
        {
            RegisteredAbilities = count;
        }

        public void RecordRequested()
        {
            RequestedAbilities++;
        }

        public void RecordCompleted()
        {
            CompletedAbilities++;
        }

        public void RecordCancelled()
        {
            CancelledAbilities++;
        }

        public void RecordInterrupted()
        {
            InterruptedAbilities++;
        }

        public void RecordFailed()
        {
            FailedAbilities++;
        }
    }
}
