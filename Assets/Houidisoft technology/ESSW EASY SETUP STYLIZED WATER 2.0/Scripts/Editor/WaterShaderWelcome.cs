using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ESSW.Editorcontroller;

namespace ESSW.Welcome
{
    public class WaterShaderWelcome : EditorWindow
    {
        private const string PREF_KEY = "ESSW_DoNotShowWelcome";
        private const string STORE_URL =
            "https://assetstore.unity.com/publishers/105962";

        private static Texture2D banner;
        private static bool doNotShowAgain;

        private static readonly Vector2 WINDOW_SIZE = new Vector2(560, 620);

        [MenuItem("Tools/ESSW/Welcome")]
        public static void ShowWindow()
        {
            LoadAssets();

            var window = GetWindow<WaterShaderWelcome>("ESSW Stylized Water");
            window.minSize = WINDOW_SIZE;
            window.maxSize = WINDOW_SIZE;
            window.Show();
        }

        [InitializeOnLoadMethod]
        static void InitOnLoad()
        {
            if (!EditorPrefs.GetBool(PREF_KEY, false))
                EditorApplication.delayCall += ShowOnce;
        }

        static void ShowOnce()
        {
            EditorApplication.delayCall -= ShowOnce;
            ShowWindow();
        }

        static void LoadAssets()
        {
            if (banner != null) return;

            string scriptPath = AssetDatabase.GetAssetPath(
                MonoScript.FromScriptableObject(CreateInstance<WaterShaderWelcome>()));

            string bannerPath = Path.Combine(
                Path.GetDirectoryName(scriptPath) ?? "Assets",
                "Images/banner.png").Replace("\\", "/");

            banner = AssetDatabase.LoadAssetAtPath<Texture2D>(bannerPath);
        }

        void OnEnable()
        {
            doNotShowAgain = EditorPrefs.GetBool(PREF_KEY, false);
        }

        void OnGUI()
        {
            DrawBanner();
            DrawAddWaterSection();
            DrawHealthChecker();
            DrawStoreButton();
            DrawFooter();
        }

        // ================= UI =================

        void DrawBanner()
        {
            if (banner)
            {
                Rect r = GUILayoutUtility.GetRect(
                    position.width,
                    banner.height,
                    GUILayout.ExpandWidth(true));

                GUI.DrawTexture(r, banner, ScaleMode.ScaleAndCrop);
            }
            else
            {
                GUILayout.Space(140);
            }
        }

        void DrawAddWaterSection()
        {
            GUILayout.Space(12);
            GUILayout.Label("Add Water to Your Scene", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "You can add water from the Unity menu:\n\n" +
                "GameObject → 3D Object → Add Water → Low Poly / High Poly\n\n" +
                "Or use the quick buttons below:",
                MessageType.Info);

            GUILayout.Space(6);

            if (GUILayout.Button("➕ Add Water (Low Poly)", GUILayout.Height(34)))
                WaterToolMenu.AddWaterSurface("LowPolyObject");

            if (GUILayout.Button("➕ Add Water (High Poly)", GUILayout.Height(34)))
                WaterToolMenu.AddWaterSurface("HighPolyObject");
        }

        void DrawHealthChecker()
        {
            GUILayout.Space(14);
            GUILayout.Label("Setup & Health Check", EditorStyles.boldLabel);

#if UNITY_2023_1_OR_NEWER
            var waters = Object.FindObjectsByType<WaterShaderController>(
                FindObjectsSortMode.None);
#else
            var waters = Object.FindObjectsOfType<WaterShaderController>(true);
#endif
            if (waters.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "No water object found in the scene.",
                    MessageType.Warning);
                return;
            }

            bool missingMaterial = false;
            bool missingGradient = false;

            foreach (var w in waters)
            {
                if (w.waterMaterial == null)
                    missingMaterial = true;

                var so = new SerializedObject(w);
                var grad = so.FindProperty("gradientTexture");
                if (grad == null || grad.objectReferenceValue == null)
                    missingGradient = true;
            }

            bool opaqueMissing = false;
            bool depthMissing = false;
            UniversalRenderPipelineAsset urp = null;

            if (GraphicsSettings.currentRenderPipeline
                is UniversalRenderPipelineAsset urpAsset)
            {
                urp = urpAsset;
                opaqueMissing = !urp.supportsCameraOpaqueTexture;
                depthMissing = !urp.supportsCameraDepthTexture;
            }

            if (missingMaterial)
                EditorGUILayout.HelpBox(
                    "One or more water objects are missing a material.",
                    MessageType.Error);

            if (missingGradient)
                EditorGUILayout.HelpBox(
                    "Gradient texture has not been generated.\n" +
                    "Select the water object and use the Generate Gradient button.",
                    MessageType.Warning);

            if (opaqueMissing || depthMissing)
            {
                EditorGUILayout.HelpBox(
                    "URP settings are not configured correctly.\n" +
                    "Opaque Texture and Depth Texture must be enabled.",
                    MessageType.Warning);

                GUILayout.Space(4);

                if (GUILayout.Button("Fix URP Settings", GUILayout.Height(28)))
                {
                    FixURPSettings(urp);
                }
            }

            if (!missingMaterial && !missingGradient && !opaqueMissing && !depthMissing)
                EditorGUILayout.HelpBox(
                    "Everything looks good! Your water setup is ready.",
                    MessageType.Info);
        }

        void DrawStoreButton()
        {
            GUILayout.Space(16);

            if (GUILayout.Button(
                "🛒 Visit Our Store for More Useful Assets",
                GUILayout.Height(40)))
            {
                Application.OpenURL(STORE_URL);
            }
        }

        void DrawFooter()
        {
            GUILayout.FlexibleSpace();
            GUILayout.Space(10);

            doNotShowAgain =
                EditorGUILayout.ToggleLeft("Don't show this again", doNotShowAgain);

            GUILayout.Space(6);

            if (GUILayout.Button("Close", GUILayout.Height(28)))
            {
                if (doNotShowAgain)
                    EditorPrefs.SetBool(PREF_KEY, true);

                Close();
            }
        }

        // ================= Helpers =================

        static void FixURPSettings(UniversalRenderPipelineAsset urp)
        {
            if (urp == null) return;

            Undo.RecordObject(urp, "Fix URP Water Settings");

            urp.supportsCameraOpaqueTexture = true;
            urp.supportsCameraDepthTexture = true;

            EditorUtility.SetDirty(urp);
            AssetDatabase.SaveAssets();

            Debug.Log("ESSW: URP Opaque Texture and Depth Texture have been enabled.");
        }
    }
}