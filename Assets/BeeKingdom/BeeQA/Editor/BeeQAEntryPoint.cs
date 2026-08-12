using UnityEditor;
using UnityEngine;

namespace BeeKingdom.BeeQA
{
    public static class BeeQAEntryPoint
    {
        public static bool IsDebugAvailable => Application.isEditor || Debug.isDebugBuild;

        [MenuItem("BeeKingdom/BeeQA/Open Dashboard", priority = 10)]
        public static void OpenDashboard()
        {
            if (!IsDebugAvailable) return;
            BeeQADashboardWindow.Open();
        }

        [MenuItem("BeeKingdom/BeeQA/Open Dashboard", validate = true)]
        private static bool ValidateOpenDashboard()
        {
            return IsDebugAvailable;
        }
    }
}
