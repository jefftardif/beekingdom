using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class HiveToCanonicalWorldMapNavigationSmoke
    {
        private const string SandboxScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string CanonicalWorldMapSceneName = "WorldMapWave6Wave5Method12288Preview";
        private const string OutputDirectory = "C:/projets/beekingdomgame-master/Docs/BuilderA/HiveToCanonicalWorldMapNavigationFix";
        private const string ManifestPath = OutputDirectory + "/BuilderA_HiveToCanonicalWorldMapNavigationFix_RuntimeSmoke.md";
        private const string StateRequested = "BeeKingdom.Playground.HiveToCanonicalWorldMapNavigationSmoke.Requested";
        private const string StatePhase = "BeeKingdom.Playground.HiveToCanonicalWorldMapNavigationSmoke.Phase";
        private const string StateFrames = "BeeKingdom.Playground.HiveToCanonicalWorldMapNavigationSmoke.Frames";
        private const string StateResult = "BeeKingdom.Playground.HiveToCanonicalWorldMapNavigationSmoke.Result";

        static HiveToCanonicalWorldMapNavigationSmoke()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            Attach();
        }

        public static void Run()
        {
            Directory.CreateDirectory(OutputDirectory);
            if (File.Exists(ManifestPath)) File.Delete(ManifestPath);
            SessionState.SetBool(StateRequested, true);
            SessionState.SetInt(StatePhase, 0);
            SessionState.SetInt(StateFrames, 0);
            SessionState.SetString(StateResult, string.Empty);
            Attach();
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        private static void Attach()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                SessionState.SetInt(StatePhase, 1);
                SessionState.SetInt(StateFrames, 0);
            }
            else if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetInt(StatePhase, 0) == 3)
            {
                bool pass = string.Equals(SessionState.GetString(StateResult, string.Empty), "PASS", StringComparison.Ordinal);
                Clear();
                EditorApplication.delayCall += () => EditorApplication.Exit(pass ? 0 : 1);
            }
        }

        private static void OnUpdate()
        {
            if (!SessionState.GetBool(StateRequested, false))
            {
                EditorApplication.update -= OnUpdate;
                return;
            }

            int phase = SessionState.GetInt(StatePhase, 0);
            if (phase == 0 || !EditorApplication.isPlaying) return;

            int frames = SessionState.GetInt(StateFrames, 0) + 1;
            SessionState.SetInt(StateFrames, frames);

            try
            {
                if (phase == 1 && frames >= 10)
                {
                    InvokeCanonicalWorldMapHelper();
                    SessionState.SetInt(StatePhase, 2);
                    SessionState.SetInt(StateFrames, 0);
                    return;
                }

                if (phase == 2)
                {
                    Scene activeScene = SceneManager.GetActiveScene();
                    WorldMapMmoFullscreenFoundationBootstrap bootstrap = UnityEngine.Object.FindFirstObjectByType<WorldMapMmoFullscreenFoundationBootstrap>();
                    WorldMapMmoFullscreenFoundationBootstrap.Wave6ProofSnapshot wave6 = bootstrap != null
                        ? bootstrap.CurrentWave6ProofSnapshot()
                        : default;
                    if (activeScene.name == CanonicalWorldMapSceneName
                        && bootstrap != null
                        && bootstrap.isActiveAndEnabled
                        && wave6.ManifestReady
                        && wave6.VisibleTilesReady)
                    {
                        WriteManifest("PASS", activeScene.name, bootstrap.name, frames, wave6.LoadedVisibleTiles, wave6.RequiredVisibleTiles);
                        SessionState.SetString(StateResult, "PASS");
                        SessionState.SetInt(StatePhase, 3);
                        EditorApplication.ExitPlaymode();
                        return;
                    }

                    if (frames > 180)
                    {
                        string bootstrapState = bootstrap == null ? "missing" : bootstrap.name;
                        WriteManifest("FAIL", activeScene.name, bootstrapState, frames, wave6.LoadedVisibleTiles, wave6.RequiredVisibleTiles);
                        SessionState.SetString(StateResult, "FAIL");
                        SessionState.SetInt(StatePhase, 3);
                        EditorApplication.ExitPlaymode();
                    }
                }
            }
            catch (Exception ex)
            {
                WriteManifest("FAIL", SceneManager.GetActiveScene().name, ex.GetType().Name + ": " + ex.Message, frames, 0, 0);
                SessionState.SetString(StateResult, "FAIL");
                SessionState.SetInt(StatePhase, 3);
                EditorApplication.ExitPlaymode();
            }
        }

        private static void InvokeCanonicalWorldMapHelper()
        {
            MethodInfo method = typeof(HiveViewProductUiPresenter).GetMethod("OpenCanonicalWorldMap", BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null) throw new MissingMethodException(typeof(HiveViewProductUiPresenter).FullName, "OpenCanonicalWorldMap");
            method.Invoke(null, null);
        }

        private static void WriteManifest(string status, string activeScene, string bootstrap, int frames, int loadedTiles, int requiredTiles)
        {
            Directory.CreateDirectory(OutputDirectory);
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("# BuilderA Hive To Canonical WorldMap Runtime Smoke");
            builder.AppendLine();
            builder.AppendLine("- status: `" + status + "`");
            builder.AppendLine("- invoked_helper: `HiveViewProductUiPresenter.OpenCanonicalWorldMap`");
            builder.AppendLine("- expected_scene: `" + CanonicalWorldMapSceneName + "`");
            builder.AppendLine("- expected_scene_path: `" + SplashDevelopmentSceneConfig.WorldMapScenePath + "`");
            builder.AppendLine("- active_scene: `" + activeScene + "`");
            builder.AppendLine("- bootstrap: `" + bootstrap + "`");
            builder.AppendLine("- frames_waited_after_load: `" + frames.ToString(System.Globalization.CultureInfo.InvariantCulture) + "`");
            builder.AppendLine("- canonical_world_map_scene_loaded: `" + (status == "PASS" ? "true" : "false") + "`");
            builder.AppendLine("- bootstrap_wave6_present: `" + (status == "PASS" ? "true" : "false") + "`");
            builder.AppendLine("- wave6_visible_tiles: `" + loadedTiles.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/" + requiredTiles.ToString(System.Globalization.CultureInfo.InvariantCulture) + "`");
            File.WriteAllText(ManifestPath, builder.ToString());
        }

        private static void Clear()
        {
            SessionState.SetBool(StateRequested, false);
            SessionState.SetInt(StatePhase, 0);
            SessionState.SetInt(StateFrames, 0);
            SessionState.SetString(StateResult, string.Empty);
            EditorApplication.update -= OnUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }
    }
}
