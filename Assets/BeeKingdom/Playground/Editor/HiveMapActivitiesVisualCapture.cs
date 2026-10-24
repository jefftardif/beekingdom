using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BeeKingdom.Localization;
using BeeKingdom.Networking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BeeKingdom.Playground.Editor
{
    // Explicit editor-only fixture. Never auto-started in the game and never calls a server.
    [InitializeOnLoad]
    public static class HiveMapActivitiesVisualCapture
    {
        private const string Active = "M124B.Capture.Active";
        private static HiveMapActivitiesBootstrap view;
        private static int frame;
        private static int shot;
        private static string output;
        private static readonly int[] Widths = { 390, 390, 844, 844, 390, 390, 1920, 1280 };
        private static readonly int[] Heights = { 844, 844, 390, 390, 844, 844, 1080, 720 };
        private static readonly string[] Names = { "portrait", "portrait-kingdom", "landscape", "landscape-kingdom", "offline", "unavailable", "desktop-1080p", "desktop-720p" };

        static HiveMapActivitiesVisualCapture()
        {
            EditorApplication.update += Tick;
        }

        public static void Run()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetBool(Active, true);
            SetSize(390, 844);
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Active, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            try
            {
                frame++;
                if (frame == 1)
                {
                    output = Path.GetFullPath("Docs/AI/Missions/M124C-Visual");
                    Directory.CreateDirectory(output);
                    var camera = new GameObject("Capture camera").AddComponent<Camera>();
                    camera.clearFlags = CameraClearFlags.SolidColor;
                    camera.backgroundColor = Color.black;
                    view = new GameObject("Activities visual fixture").AddComponent<HiveMapActivitiesBootstrap>();
                    view.enabled = false;
                }
                if (frame == 20)
                {
                    BeeLocalization.SetLocale(shot >= 2 && shot <= 3 ? "en-US" : "fr-CA");
                    var milestone = new HiveMilestoneEventScreenModel
                    {
                        State = HiveMilestoneEventScreenState.Ready,
                        RequiredObjectiveCount = 5,
                        WindowEndsAtUtc = DateTimeOffset.Now.AddDays(3),
                        Objectives = new[]
                        {
                            new RemoteHiveMilestoneObjective { ObjectiveKey = "building_upgrade", Done = true },
                            new RemoteHiveMilestoneObjective { ObjectiveKey = "troop_recruit", Done = true },
                            new RemoteHiveMilestoneObjective { ObjectiveKey = "world_resource", Done = false },
                            new RemoteHiveMilestoneObjective { ObjectiveKey = "strategic_path", Done = false },
                            new RemoteHiveMilestoneObjective { ObjectiveKey = "vip_tier", Done = false }
                        },
                        Reward = new Dictionary<string, long> { { "honey", 2500 }, { "pollen", 1000 } }
                    };
                    HiveDailyRoundScreenModel daily = HiveMapActivitiesPremiumTests.Daily();
                    if (shot == 4)
                    {
                        daily = HiveDailyRoundPresentation.OfflineReadOnly(HiveMapActivitiesPremiumTests.DailySnapshot(), DateTimeOffset.UtcNow, true);
                        milestone.State = HiveMilestoneEventScreenState.Error;
                    }
                    if (shot == 5)
                    {
                        daily = new UnavailableHiveDailyRoundPanelController().Model;
                        milestone = new HiveMilestoneEventScreenModel();
                    }
                    var quests = shot == 5 ? new QuestChainScreenModel() : HiveMapActivitiesPremiumTests.Quests();
                    typeof(HiveMapActivitiesBootstrap).GetMethod("RenderSnapshot", BindingFlags.NonPublic | BindingFlags.Instance)
                        .Invoke(view, new object[] { daily, milestone, quests, 0, shot == 1 || shot == 3 ? 0f : 1f,
                            shot >= 6 ? new Rect(0, 0, Screen.width, Screen.height) :
                            Screen.width > Screen.height ? new Rect(44, 21, Screen.width - 88, Screen.height - 21) : new Rect(0, 34, Screen.width, Screen.height - 78) });
                    Canvas.ForceUpdateCanvases();
                }
                if (frame == 40)
                {
                    // Batch GameView can resize asynchronously after rendering the fixture.
                    // Rebuild at the requested size rather than saving a stale, partial canvas.
                    if (Screen.width != Widths[shot] || Screen.height != Heights[shot])
                    {
                        SetSize(Widths[shot], Heights[shot]);
                        frame = 2;
                        EditorApplication.QueuePlayerLoopUpdate();
                        return;
                    }
                    foreach (Canvas renderedCanvas in view.GetComponentsInChildren<Canvas>(true))
                        Debug.Log("M124B canvas " + renderedCanvas.enabled + " active=" + renderedCanvas.gameObject.activeInHierarchy + " rect=" + ((RectTransform)renderedCanvas.transform).rect + " pos=" + renderedCanvas.transform.position);
                    foreach (var label in view.GetComponentsInChildren<TMPro.TextMeshProUGUI>())
                        if (label.text == "Activités" || label.text == "Activities") Debug.Log("M124B title pos=" + label.transform.position + " rect=" + label.rectTransform.rect);
                    CaptureCanvas(Path.Combine(output, Names[shot] + ".png"));
                    File.AppendAllText(Path.Combine(output, "capture.txt"), Names[shot] + " " + Screen.width + "x" + Screen.height + " PlayMode=" + Application.isPlaying + "\n");
                }
                if (frame >= 70)
                {
                    shot++;
                    if (shot == Names.Length)
                    {
                        SessionState.SetBool(Active, false);
                        EditorApplication.isPlaying = false;
                        EditorApplication.delayCall += () => EditorApplication.Exit(0);
                    }
                    else { SetSize(Widths[shot], Heights[shot]); frame = 2; }
                }
                EditorApplication.QueuePlayerLoopUpdate();
            }
            catch (Exception error)
            {
                SessionState.SetBool(Active, false);
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }

        private static void SetSize(int width, int height)
        {
            Assembly assembly = typeof(UnityEditor.Editor).Assembly;
            Type sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            Type sizeType = assembly.GetType("UnityEditor.GameViewSize");
            Type sizeEnum = assembly.GetType("UnityEditor.GameViewSizeType");
            Type groupEnum = assembly.GetType("UnityEditor.GameViewSizeGroupType");
            object sizes = typeof(ScriptableSingleton<>).MakeGenericType(sizesType)
                .GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
            object group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { Enum.Parse(groupEnum, "Standalone") });
            object size = sizeType.GetConstructor(new[] { sizeEnum, typeof(int), typeof(int), typeof(string) })
                .Invoke(new object[] { Enum.Parse(sizeEnum, "FixedResolution"), width, height, "M124B" });
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
            int index = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, Array.Empty<object>()) - 1;
            Type gameViewType = assembly.GetType("UnityEditor.GameView");
            EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
            gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(gameView, index);
            gameView.Show();
            gameView.Focus();
        }

        private static void CaptureCanvas(string path)
        {
            // Batch GameView omits overlay canvases. Render the same runtime hierarchy through a capture camera.
            Canvas canvas = view.GetComponentInChildren<Canvas>();
            Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            var target = new RenderTexture(Screen.width, Screen.height, 24);
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            UnityEngine.Object.Destroy(image);
            RenderTexture.active = previous;
            camera.targetTexture = null;
            UnityEngine.Object.Destroy(target);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
    }
}
