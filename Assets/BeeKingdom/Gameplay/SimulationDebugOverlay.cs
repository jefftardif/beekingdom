using UnityEngine;

namespace BeeKingdom.Gameplay
{
    public sealed class SimulationDebugOverlay : MonoBehaviour
    {
        [SerializeField] private bool showInDevelopmentBuilds = true;
        private PlayableHiveState state;

        public void Bind(PlayableHiveState playableState)
        {
            state = playableState;
        }

        private void OnGUI()
        {
            if (state == null || (!Debug.isDebugBuild && !showInDevelopmentBuilds))
            {
                return;
            }

            IntegrationDiagnostics diagnostics = state.Diagnostics;
            GUILayout.BeginArea(new Rect(12, 12, 260, 170), GUI.skin.box);
            GUILayout.Label("Bee Kingdom Simulation");
            GUILayout.Label("Population: " + diagnostics.Population);
            GUILayout.Label("Resources: " + diagnostics.TotalResources.ToString("0.0"));
            GUILayout.Label("Active tasks: " + diagnostics.ActiveTasks);
            GUILayout.Label("Sim time: " + diagnostics.SimulatedSeconds.ToString("0.0") + "s");
            GUILayout.Label("Simulation FPS: " + (diagnostics.AverageTickSeconds > 0d ? (1d / diagnostics.AverageTickSeconds).ToString("0.0") : "0"));
            GUILayout.Label("Events/s: " + diagnostics.EventsPerSecond);
            GUILayout.Label("Avg tick: " + diagnostics.AverageTickSeconds.ToString("0.000") + "s");
            GUILayout.EndArea();
        }
    }
}
