using BeeKingdom.Gameplay.Progression;

namespace BeeKingdom.BeeQA
{
    public sealed class BeeQASpeedUpQAModule : BeeQAModuleBase
    {
        public override string Id => "beeqa.speedup";
        public override string DisplayName => "SpeedUp QA";
        public override string Description => "Vérifie les cas limites Smart SpeedUp, le filtrage et la consommation.";
        public override string Version => "1.0.0";
        public override BeeQACategory Category => BeeQACategory.SpeedUps;

        protected override bool ExecuteCore(out string message)
        {
            const string universalId = "universal_60s";
            const string researchId = "research_900s";
            int universalBefore = SpeedUpInventory.GetCount(universalId);
            int researchBefore = SpeedUpInventory.GetCount(researchId);
            SpeedUpInventory.Add(universalId, 2);
            SpeedUpInventory.Add(researchId, 1);
            try
            {
                bool lower = Completes(SpeedUpCategory.Research, "research", 30L);
                bool equal = Completes(SpeedUpCategory.Research, "research", 60L);
                bool greater = Completes(SpeedUpCategory.Research, "research", 120L);
                bool specialized = ContainsCategory(SpeedUpCategory.Research, "research", 900L, SpeedUpCategory.Research);
                bool combined = Completes(SpeedUpCategory.Research, "research", 120L);
                bool consumed = ConsumesPlannedStacks(universalId, universalBefore);
                bool dialog = OpenAndCloseDialog();
                message = lower && equal && greater && specialized && combined && consumed && dialog
                    ? "Cas Smart SpeedUp et dialogue validés."
                    : "Un cas Smart SpeedUp a échoué.";
                return lower && equal && greater && specialized && combined && consumed && dialog;
            }
            finally
            {
                SpeedUpInventory.Remove(universalId, SpeedUpInventory.GetCount(universalId) - universalBefore);
                SpeedUpInventory.Remove(researchId, SpeedUpInventory.GetCount(researchId) - researchBefore);
                SpeedUpDialog.Close();
            }
        }

        private static bool Completes(SpeedUpCategory category, string targetId, long seconds)
        {
            SmartSpeedUpPlan plan = SmartSpeedUpCalculator.ComputePlan(new SpeedUpDialogContext(category, targetId, seconds));
            return plan != null && plan.CompletesTarget && plan.RemainingAfterSeconds == 0L;
        }

        private static bool ContainsCategory(SpeedUpCategory category, string targetId, long seconds, SpeedUpCategory expected)
        {
            SmartSpeedUpPlan plan = SmartSpeedUpCalculator.ComputePlan(new SpeedUpDialogContext(category, targetId, seconds));
            if (plan == null || !plan.CompletesTarget) return false;
            for (int i = 0; i < plan.Entries.Count; i++)
                if (plan.Entries[i].Item.Category == expected) return true;
            return false;
        }

        private static bool OpenAndCloseDialog()
        {
            SpeedUpDialog.Open(new SpeedUpDialogContext(SpeedUpCategory.Research, "research", 900L));
            bool open = SpeedUpDialog.IsOpen && SpeedUpDialog.Context.Category == SpeedUpCategory.Research;
            SpeedUpDialog.Close();
            return open && !SpeedUpDialog.IsOpen;
        }

        private static bool ConsumesPlannedStacks(string itemId, int before)
        {
            SmartSpeedUpPlan plan = SmartSpeedUpCalculator.ComputePlan(new SpeedUpDialogContext(SpeedUpCategory.Research, "research", 120L));
            if (!SmartSpeedUpCalculator.ApplyPlan(plan, out long remaining) || remaining != 0L) return false;
            return SpeedUpInventory.GetCount(itemId) == before;
        }
    }
}
