namespace BeeKingdom.Hive
{
    public sealed class BeeLifecycleStateMachine
    {
        private readonly BeeLifecycleRules rules;

        public BeeLifecycleStateMachine(BeeLifecycleRules rules)
        {
            this.rules = rules;
        }

        public bool TryTransition(BeeLifecycleStage current, BeeLifecycleStage next, out BeeLifecycleStage result)
        {
            if (rules.CanTransition(current, next))
            {
                result = next;
                return true;
            }

            result = current;
            return false;
        }

        public BeeLifecycleStage ResolveStage(double biologicalAgeSeconds)
        {
            return rules.ResolveStage(biologicalAgeSeconds);
        }
    }
}
