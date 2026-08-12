using UnityEngine;

namespace BeeKingdom.BeeQA
{
    public sealed class BeeQASmokeTestModule : BeeQAModuleBase
    {
        public override string Id => "beeqa.smoke";
        public override string DisplayName => "Smoke Test";
        public override string Description => "Vérifie que le contexte BeeQA et son catalogue sont opérationnels.";
        public override string Version => "1.0.0";
        public override string Author => "BeeKingdom QA";
        public override BeeQACategory Category => BeeQACategory.Automation;

        protected override bool ExecuteCore(out string message)
        {
            bool editorContext = Application.isEditor;
            bool catalogReady = BeeQACatalog.Categories.Count == 18;
            bool dashboardContext = BeeQAEntryPoint.IsDebugAvailable;
            bool passed = editorContext && catalogReady && dashboardContext;
            message = passed
                ? "BeeQA, le contexte Unity et le catalogue sont opérationnels."
                : "Contexte BeeQA invalide ou catalogue incomplet.";
            return passed;
        }
    }
}
