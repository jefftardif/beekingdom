namespace BeeKingdom.Hive
{
    public sealed class BeeLifecycleRules
    {
        public BeeDevelopmentProfile Development { get; }
        public BeeMortalityProfile Mortality { get; }
        public float BaseBiologicalAgeMultiplier { get; }

        public BeeLifecycleRules(BeeDevelopmentProfile development, BeeMortalityProfile mortality, float baseBiologicalAgeMultiplier = 1f)
        {
            Development = development;
            Mortality = mortality;
            BaseBiologicalAgeMultiplier = baseBiologicalAgeMultiplier < 0f ? 0f : baseBiologicalAgeMultiplier;
        }

        public BeeLifecycleStage ResolveStage(double biologicalAgeSeconds)
        {
            if (biologicalAgeSeconds >= Mortality.MaximumAgeSeconds)
            {
                return BeeLifecycleStage.Dead;
            }

            if (biologicalAgeSeconds >= Development.SeniorWorkerAtSeconds) return BeeLifecycleStage.SeniorWorker;
            if (biologicalAgeSeconds >= Development.AdultWorkerAtSeconds) return BeeLifecycleStage.AdultWorker;
            if (biologicalAgeSeconds >= Development.YoungWorkerAtSeconds) return BeeLifecycleStage.YoungWorker;
            if (biologicalAgeSeconds >= Development.PupaAtSeconds) return BeeLifecycleStage.Pupa;
            if (biologicalAgeSeconds >= Development.LarvaAtSeconds) return BeeLifecycleStage.Larva;
            return BeeLifecycleStage.Egg;
        }

        public bool CanTransition(BeeLifecycleStage current, BeeLifecycleStage next)
        {
            if (current == next) return true;
            if (current == BeeLifecycleStage.Dead) return false;
            if (next == BeeLifecycleStage.Dead) return true;
            return (int)next == (int)current + 1;
        }
    }
}
