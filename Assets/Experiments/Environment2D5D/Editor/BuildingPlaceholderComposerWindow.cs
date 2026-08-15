#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BeeKingdom.Experiments.Environment2D5D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Experiments.Environment2D5D.EditorTools
{
    public sealed class BuildingPlaceholderComposerWindow : EditorWindow
    {
        private const string ScenePath = "Assets/Experiments/Environment2D5D/Scenes/Environment2D5D_SpatialV3.unity";
        private const string LayoutJsonPath = "Assets/Experiments/Environment2D5D/Config/BuildingLayout_Test.json";
        private const string PlaceholderSpritePath = "Assets/BeeKingdom/Art/Buildings/BUILDING_PLACEHOLDER_001.png";
        private const string PlaceholderRootName = "__TEST_BUILDING_PLACEHOLDERS__";
        private const string LegacyScalePreviewRootName = "__TEST_BUILDING_SCALE_PREVIEW__";

        private const int RequiredSlotCount = 14;
        private const float DefaultUniformScale = 0.27f;
        private const float SpawnZ = 29.95f;
        private const float CompositionSpawnY = 18f;

        [MenuItem("BeeKingdom/Experiments/Building Placeholder Composer")]
        public static void OpenWindow()
        {
            GetWindow<BuildingPlaceholderComposerWindow>("Placeholder Composer");
        }

        [MenuItem("BeeKingdom/Experiments/Building Placeholder Composer/Add Placeholder")]
        private static void MenuAddPlaceholder()
        {
            Debug.Log("BUILDING PLACEHOLDER COMPOSER: Add Placeholder callback ENTERED");
            AddPlaceholder();
        }

        [MenuItem("BeeKingdom/Experiments/Building Placeholder Composer/Add 14 Placeholders")]
        private static void MenuAddFourteenPlaceholders()
        {
            Debug.Log("BUILDING PLACEHOLDER COMPOSER: Add 14 Placeholders callback ENTERED");
            AddFourteenPlaceholders();
        }

        [MenuItem("BeeKingdom/Experiments/Building Placeholder Composer/Clear All Test Placeholders")]
        private static void MenuClearAllTestPlaceholders()
        {
            ClearAllTestPlaceholders();
        }

        [MenuItem("BeeKingdom/Experiments/Building Placeholder Composer/Select All Placeholders")]
        private static void MenuSelectAllPlaceholders()
        {
            SelectAllPlaceholders();
        }

        [MenuItem("BeeKingdom/Experiments/Building Placeholder Composer/Reset Placeholder Scale")]
        private static void MenuResetPlaceholderScale()
        {
            ResetPlaceholderScale();
        }

        [MenuItem("BeeKingdom/Experiments/Building Placeholder Composer/Save / Export Building Layout")]
        private static void MenuExportLayout()
        {
            SaveBuildingLayout();
        }

        [MenuItem("BeeKingdom/Experiments/Building Placeholder Composer/Load Building Layout")]
        private static void MenuLoadLayout()
        {
            LoadBuildingLayout();
        }

        [MenuItem("BeeKingdom/Experiments/Building Placeholder Composer/Run Batch Validation")]
        public static void RunBatchValidation()
        {
            if (!OpenScene())
            {
                return;
            }

            CleanupLegacyScalePreviewOnly();
            ClearAllTestPlaceholders();
            AddFourteenPlaceholders();

            var root = EnsurePlaceholderRoot();
            if (root.transform.childCount < RequiredSlotCount)
            {
                Debug.LogError("[PLACEHOLDER_COMPOSER] Validation failed: expected 14 placeholders.");
                return;
            }

            var first = root.transform.Find("BUILDING_PLACEHOLDER_01");
            var second = root.transform.Find("BUILDING_PLACEHOLDER_02");
            if (first == null || second == null)
            {
                Debug.LogError("[PLACEHOLDER_COMPOSER] Validation failed: missing base placeholders.");
                return;
            }

            first.localPosition += new Vector3(2f, 1f, 0f);
            first.localEulerAngles = new Vector3(0f, 0f, 17f);
            first.localScale = new Vector3(0.31f, 0.29f, 0.27f);

            var firstMarker = first.GetComponent<TestBuildingPlaceholder>();
            firstMarker.BuildingType = BuildingType.BARRACK;

            bool independentMove = second.localPosition != first.localPosition;
            bool independentScale = second.localScale != first.localScale;

            SaveBuildingLayout();

            Vector3 expectedPosition = first.localPosition;
            Vector3 expectedRotation = first.localEulerAngles;
            Vector3 expectedScale = first.localScale;

            first.localPosition = new Vector3(99f, 99f, 99f);
            first.localEulerAngles = Vector3.zero;
            first.localScale = Vector3.one;

            bool loaded = LoadBuildingLayout();
            bool loadRestored = loaded
                                && first.localPosition == expectedPosition
                                && first.localEulerAngles == expectedRotation
                                && first.localScale == expectedScale;

            bool hadBuildingPremium = FindByName("BuildingPremium") != null;
            bool hadAnchorA = FindByName("AnchorMarker_A") != null;
            ClearAllTestPlaceholders();
            bool stillBuildingPremium = FindByName("BuildingPremium") != null;
            bool stillAnchorA = FindByName("AnchorMarker_A") != null;
            bool clearSafe = hadBuildingPremium && hadAnchorA && stillBuildingPremium && stillAnchorA;

            Debug.Log("[PLACEHOLDER_COMPOSER] Validation: independentMove=" + independentMove
                      + " independentScale=" + independentScale
                      + " loadRestored=" + loadRestored
                      + " clearSafe=" + clearSafe
                      + " jsonPath=" + LayoutJsonPath);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Manual-only composition tool for Environment2D5D_SpatialV3. Move/Rotate/Scale is fully manual.", MessageType.Info);

            if (GUILayout.Button("Add Placeholder"))
            {
                AddPlaceholder();
            }

            if (GUILayout.Button("Add 14 Placeholders"))
            {
                AddFourteenPlaceholders();
            }

            if (GUILayout.Button("Duplicate Selected Placeholder"))
            {
                DuplicateSelectedPlaceholder();
            }

            if (GUILayout.Button("Delete Selected Test Placeholder(s)"))
            {
                DeleteSelectedTestPlaceholders();
            }

            GUILayout.Space(6f);

            if (GUILayout.Button("Select All Placeholders"))
            {
                SelectAllPlaceholders();
            }

            if (GUILayout.Button("Reset Placeholder Scale"))
            {
                ResetPlaceholderScale();
            }

            if (GUILayout.Button("Clear All Test Placeholders"))
            {
                ClearAllTestPlaceholders();
            }

            GUILayout.Space(6f);

            if (GUILayout.Button("Save / Export Building Layout"))
            {
                SaveBuildingLayout();
            }

            if (GUILayout.Button("Load Building Layout"))
            {
                LoadBuildingLayout();
            }

            GUILayout.Space(6f);

            if (GUILayout.Button("Cleanup Legacy Scale Preview (__TEST_BUILDING_SCALE_PREVIEW__)"))
            {
                CleanupLegacyScalePreviewOnly();
            }
        }

        private static bool OpenScene()
        {
            var active = SceneManager.GetActiveScene();
            if (active.path == ScenePath)
            {
                return true;
            }

            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single).IsValid();
        }

        private static Sprite LoadPlaceholderSprite()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(PlaceholderSpritePath);
            Sprite selected = null;
            float largestArea = -1f;
            int spriteCount = 0;

            foreach (var asset in assets)
            {
                var sprite = asset as Sprite;
                if (sprite == null)
                {
                    continue;
                }

                spriteCount++;
                float area = sprite.rect.width * sprite.rect.height;
                if (area <= 1f)
                {
                    continue;
                }

                if (area > largestArea)
                {
                    largestArea = area;
                    selected = sprite;
                }
            }

            if (selected == null && spriteCount > 0)
            {
                foreach (var asset in assets)
                {
                    var sprite = asset as Sprite;
                    if (sprite == null)
                    {
                        continue;
                    }

                    float area = sprite.rect.width * sprite.rect.height;
                    if (area > largestArea)
                    {
                        largestArea = area;
                        selected = sprite;
                    }
                }
            }

            if (selected == null)
            {
                Debug.LogError("[PLACEHOLDER_COMPOSER] Missing sprite: " + PlaceholderSpritePath);
            }

            if (selected != null)
            {
                Debug.Log("BUILDING PLACEHOLDER COMPOSER: Using placeholder sprite '" + selected.name
                          + "' rect=" + selected.rect.width.ToString(CultureInfo.InvariantCulture)
                          + "x" + selected.rect.height.ToString(CultureInfo.InvariantCulture));

                var texture = selected.texture;
                string textureWidth = texture != null
                    ? texture.width.ToString(CultureInfo.InvariantCulture)
                    : "<null>";
                string textureHeight = texture != null
                    ? texture.height.ToString(CultureInfo.InvariantCulture)
                    : "<null>";

            }

            return selected;
        }

        private static GameObject EnsurePlaceholderRoot()
        {
            var root = FindByName(PlaceholderRootName);
            if (root != null)
            {
                return root;
            }

            root = new GameObject(PlaceholderRootName);
            Undo.RegisterCreatedObjectUndo(root, "Create " + PlaceholderRootName);
            return root;
        }

        private static GameObject AddPlaceholder()
        {
            Debug.Log("BUILDING PLACEHOLDER COMPOSER: Add Placeholder step -> callback entered");
            if (!OpenScene())
            {
                Debug.LogError("BUILDING PLACEHOLDER COMPOSER: Add Placeholder step -> scene validation FAILED");
                return null;
            }

            Debug.Log("BUILDING PLACEHOLDER COMPOSER: Add Placeholder step -> scene validated");

            var root = EnsurePlaceholderRoot();
            Debug.Log("BUILDING PLACEHOLDER COMPOSER: Add Placeholder step -> parent found/created: " + root.name);
            var sprite = LoadPlaceholderSprite();
            if (sprite == null)
            {
                Debug.LogError("BUILDING PLACEHOLDER COMPOSER: Add Placeholder step -> sprite loaded FAILED");
                return null;
            }

            Debug.Log("BUILDING PLACEHOLDER COMPOSER: Add Placeholder step -> sprite loaded");

            int slotIndex = NextAvailableSlotIndex(root);
            var created = CreatePlaceholder(root, sprite, slotIndex, slotIndex - 1);
            Debug.Log("BUILDING PLACEHOLDER COMPOSER: Add Placeholder step -> GameObject created: " + created.name);
            Selection.activeGameObject = created;
            Debug.Log("BUILDING PLACEHOLDER COMPOSER: Add Placeholder step -> selection assigned");
            RevealSelectionInEditor();
            Debug.Log("BUILDING PLACEHOLDER COMPOSER: Created " + created.name);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("BUILDING PLACEHOLDER COMPOSER: Add Placeholder step -> scene marked dirty");
            return created;
        }

        private static void AddFourteenPlaceholders()
        {
            Debug.Log("BUILDING PLACEHOLDER COMPOSER: Add 14 step -> callback entered");
            if (!OpenScene())
            {
                Debug.LogError("BUILDING PLACEHOLDER COMPOSER: Add 14 step -> scene validation FAILED");
                return;
            }

            Debug.Log("BUILDING PLACEHOLDER COMPOSER: Add 14 step -> scene validated");

            var root = EnsurePlaceholderRoot();
            Debug.Log("BUILDING PLACEHOLDER COMPOSER: Add 14 step -> parent found/created: " + root.name);
            var sprite = LoadPlaceholderSprite();
            if (sprite == null)
            {
                Debug.LogError("BUILDING PLACEHOLDER COMPOSER: Add 14 step -> sprite loaded FAILED");
                return;
            }

            Debug.Log("BUILDING PLACEHOLDER COMPOSER: Add 14 step -> sprite loaded");

            var selected = new List<UnityEngine.Object>();
            for (int i = 1; i <= RequiredSlotCount; i++)
            {
                string name = PlaceholderName(i);
                var existing = root.transform.Find(name);
                if (existing == null)
                {
                    var created = CreatePlaceholder(root, sprite, i, i - 1);
                    Debug.Log("BUILDING PLACEHOLDER COMPOSER: Add 14 step -> GameObject created: " + created.name);
                    selected.Add(created);
                }
                else
                {
                    EnsureMarker(existing.gameObject, i);
                    Debug.Log("BUILDING PLACEHOLDER COMPOSER: Add 14 step -> existing placeholder reused: " + existing.name);
                    selected.Add(existing.gameObject);
                }
            }

            Selection.objects = selected.ToArray();
            if (selected.Count > 0)
            {
                Selection.activeObject = selected[0];
            }
            Debug.Log("BUILDING PLACEHOLDER COMPOSER: Add 14 step -> selection assigned (count=" + selected.Count + ")");
            RevealSelectionInEditor();
            Debug.Log("BUILDING PLACEHOLDER COMPOSER: Created 14 placeholders");
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("BUILDING PLACEHOLDER COMPOSER: Add 14 step -> scene marked dirty");
        }

        private static void RevealSelectionInEditor()
        {
            if (Selection.activeObject != null)
            {
                EditorGUIUtility.PingObject(Selection.activeObject);
            }

            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.FrameSelected();
            }
        }

        private static GameObject CreatePlaceholder(GameObject root, Sprite sprite, int slotIndex, int gridIndex)
        {
            var go = new GameObject(PlaceholderName(slotIndex));
            Undo.RegisterCreatedObjectUndo(go, "Add test placeholder");
            Debug.Log("BUILDING PLACEHOLDER COMPOSER: create step -> undo registered");
            go.transform.SetParent(root.transform, false);
            Debug.Log("BUILDING PLACEHOLDER COMPOSER: create step -> parent assigned");

            Vector3 origin = SpawnOrigin(root.transform);
            go.transform.localPosition = origin + new Vector3((gridIndex % 5) * 2.0f, -(gridIndex / 5) * 2.0f, 0f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * DefaultUniformScale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.enabled = true;
            sr.color = Color.white;
            sr.sortingLayerID = 0;
            sr.sortingOrder = 1000;
            Debug.Log("BUILDING PLACEHOLDER COMPOSER: create step -> SpriteRenderer added");

            EnsureMarker(go, slotIndex);
            Debug.Log("BUILDING PLACEHOLDER COMPOSER: create step -> component added (TestBuildingPlaceholder)");
            return go;
        }

        private static Vector3 SpawnOrigin(Transform root)
        {
            float x = 0f;
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                x = sceneView.pivot.x;
            }

            Vector3 worldOrigin = new Vector3(x, CompositionSpawnY, SpawnZ);
            return root.InverseTransformPoint(worldOrigin);
        }

        private static void EnsureMarker(GameObject go, int slotIndex)
        {
            var marker = go.GetComponent<TestBuildingPlaceholder>();
            if (marker == null)
            {
                marker = Undo.AddComponent<TestBuildingPlaceholder>(go);
            }

            marker.Id = SlotId(slotIndex);
        }

        private static int NextAvailableSlotIndex(GameObject root)
        {
            var used = new HashSet<int>();
            foreach (Transform child in root.transform)
            {
                int idx = ParseSlotIndex(child.name);
                if (idx > 0)
                {
                    used.Add(idx);
                }
            }

            for (int i = 1; i <= 999; i++)
            {
                if (!used.Contains(i))
                {
                    return i;
                }
            }

            return 1000;
        }

        private static void DuplicateSelectedPlaceholder()
        {
            if (!OpenScene())
            {
                return;
            }

            var selected = Selection.activeGameObject;
            if (selected == null)
            {
                return;
            }

            var root = EnsurePlaceholderRoot();
            if (!IsUnderRoot(selected.transform, root.transform))
            {
                return;
            }

            var clone = UnityEngine.Object.Instantiate(selected, selected.transform.parent);
            clone.name = selected.name + "_DUP";
            clone.transform.localPosition += new Vector3(1.5f, -1.0f, 0f);
            Undo.RegisterCreatedObjectUndo(clone, "Duplicate placeholder");
            Selection.activeGameObject = clone;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private static void DeleteSelectedTestPlaceholders()
        {
            if (!OpenScene())
            {
                return;
            }

            var root = FindByName(PlaceholderRootName);
            if (root == null)
            {
                return;
            }

            foreach (var obj in Selection.gameObjects)
            {
                if (obj == null || obj == root)
                {
                    continue;
                }

                if (IsUnderRoot(obj.transform, root.transform))
                {
                    Undo.DestroyObjectImmediate(obj);
                }
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private static void ClearAllTestPlaceholders()
        {
            if (!OpenScene())
            {
                return;
            }

            var root = FindByName(PlaceholderRootName);
            if (root == null)
            {
                return;
            }

            Undo.DestroyObjectImmediate(root);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private static void SelectAllPlaceholders()
        {
            if (!OpenScene())
            {
                return;
            }

            var root = FindByName(PlaceholderRootName);
            if (root == null)
            {
                Selection.objects = Array.Empty<UnityEngine.Object>();
                return;
            }

            var selected = new List<UnityEngine.Object>();
            foreach (Transform t in root.transform)
            {
                selected.Add(t.gameObject);
            }

            Selection.objects = selected.ToArray();
        }

        private static void ResetPlaceholderScale()
        {
            if (!OpenScene())
            {
                return;
            }

            var root = FindByName(PlaceholderRootName);
            if (root == null)
            {
                return;
            }

            foreach (Transform t in root.transform)
            {
                Undo.RecordObject(t, "Reset placeholder scale");
                t.localScale = Vector3.one * DefaultUniformScale;
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private static bool SaveBuildingLayout()
        {
            if (!OpenScene())
            {
                return false;
            }

            var root = EnsurePlaceholderRoot();
            var ordered = new List<GameObject>(RequiredSlotCount);
            for (int i = 1; i <= RequiredSlotCount; i++)
            {
                var t = root.transform.Find(PlaceholderName(i));
                if (t == null)
                {
                    Debug.LogError("[PLACEHOLDER_COMPOSER] Cannot export layout: missing " + PlaceholderName(i));
                    return false;
                }

                ordered.Add(t.gameObject);
            }

            string fullPath = Path.GetFullPath(LayoutJsonPath);
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = BuildLayoutJson(ordered);
            File.WriteAllText(fullPath, json, new UTF8Encoding(false));
            AssetDatabase.Refresh();
            Debug.Log("[PLACEHOLDER_COMPOSER] Exported building layout: " + LayoutJsonPath);
            return true;
        }

        private static bool LoadBuildingLayout()
        {
            if (!OpenScene())
            {
                return false;
            }

            string fullPath = Path.GetFullPath(LayoutJsonPath);
            if (!File.Exists(fullPath))
            {
                Debug.LogError("[PLACEHOLDER_COMPOSER] Layout file not found: " + LayoutJsonPath);
                return false;
            }

            string json = File.ReadAllText(fullPath);
            var file = JsonUtility.FromJson<LayoutFile>(json);
            if (file == null || file.layout == null)
            {
                Debug.LogError("[PLACEHOLDER_COMPOSER] Layout file is invalid: " + LayoutJsonPath);
                return false;
            }

            var root = EnsurePlaceholderRoot();
            var sprite = LoadPlaceholderSprite();
            if (sprite == null)
            {
                return false;
            }

            foreach (var entry in file.layout)
            {
                if (string.IsNullOrEmpty(entry.placeholder))
                {
                    continue;
                }

                var child = root.transform.Find(entry.placeholder);
                GameObject go = child != null
                    ? child.gameObject
                    : CreatePlaceholder(root, sprite, ParseSlotIndex(entry.placeholder), ParseSlotIndex(entry.placeholder) - 1);

                go.name = entry.placeholder;

                Undo.RecordObject(go.transform, "Load building layout");
                go.transform.localPosition = entry.position.ToVector3();
                go.transform.localEulerAngles = entry.rotation.ToVector3();
                go.transform.localScale = entry.scale.ToVector3();

                int slot = ParseSlotIndex(entry.placeholder);
                var marker = go.GetComponent<TestBuildingPlaceholder>();
                if (marker == null)
                {
                    marker = Undo.AddComponent<TestBuildingPlaceholder>(go);
                }

                marker.Id = string.IsNullOrEmpty(entry.id) ? SlotId(slot) : entry.id;
                if (Enum.TryParse(entry.buildingType, true, out BuildingType parsedType))
                {
                    marker.BuildingType = parsedType;
                }
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[PLACEHOLDER_COMPOSER] Loaded building layout: " + LayoutJsonPath);
            return true;
        }

        private static string BuildLayoutJson(List<GameObject> placeholders)
        {
            var sb = new StringBuilder(4096);
            sb.AppendLine("{");
            sb.AppendLine("  \"version\": 1,");
            sb.AppendLine("  \"scene\": \"Environment2D5D_SpatialV3\",");
            sb.AppendLine("  \"layout\": [");

            for (int i = 0; i < placeholders.Count; i++)
            {
                GameObject go = placeholders[i];
                int slot = ParseSlotIndex(go.name);
                var marker = go.GetComponent<TestBuildingPlaceholder>();
                if (marker == null)
                {
                    marker = go.AddComponent<TestBuildingPlaceholder>();
                    marker.Id = SlotId(slot);
                }

                string id = string.IsNullOrEmpty(marker.Id) ? SlotId(slot) : marker.Id;

                sb.AppendLine("    {");
                sb.AppendLine("      \"id\": \"" + JsonEscape(id) + "\",");
                sb.AppendLine("      \"placeholder\": \"" + JsonEscape(go.name) + "\",");
                sb.AppendLine("      \"buildingType\": \"" + marker.BuildingType + "\",");
                AppendVector(sb, "position", go.transform.localPosition, "      ");
                sb.AppendLine(",");
                AppendVector(sb, "rotation", go.transform.localEulerAngles, "      ");
                sb.AppendLine(",");
                AppendVector(sb, "scale", go.transform.localScale, "      ");
                sb.AppendLine();
                sb.Append("    }");
                if (i < placeholders.Count - 1)
                {
                    sb.Append(',');
                }

                sb.AppendLine();
            }

            sb.AppendLine("  ]");
            sb.Append('}');
            return sb.ToString();
        }

        private static void AppendVector(StringBuilder sb, string name, Vector3 value, string indent)
        {
            sb.AppendLine(indent + "\"" + name + "\": {");
            sb.AppendLine(indent + "  \"x\": " + FloatText(value.x) + ",");
            sb.AppendLine(indent + "  \"y\": " + FloatText(value.y) + ",");
            sb.Append(indent + "  \"z\": " + FloatText(value.z));
            sb.AppendLine();
            sb.Append(indent + "}");
        }

        private static string FloatText(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string JsonEscape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static void CleanupLegacyScalePreviewOnly()
        {
            if (!OpenScene())
            {
                return;
            }

            var legacy = FindByName(LegacyScalePreviewRootName);
            if (legacy == null)
            {
                return;
            }

            Undo.DestroyObjectImmediate(legacy);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[PLACEHOLDER_COMPOSER] Removed legacy preview root: " + LegacyScalePreviewRootName);
        }

        private static bool IsUnderRoot(Transform item, Transform root)
        {
            Transform current = item;
            while (current != null)
            {
                if (current == root)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static string PlaceholderName(int slot)
        {
            return "BUILDING_PLACEHOLDER_" + Math.Max(slot, 1).ToString("00");
        }

        private static string SlotId(int slot)
        {
            return "BUILDING_" + Math.Max(slot, 1).ToString("00");
        }

        private static int ParseSlotIndex(string placeholderName)
        {
            const string prefix = "BUILDING_PLACEHOLDER_";
            if (string.IsNullOrEmpty(placeholderName) || !placeholderName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            string suffix = placeholderName.Substring(prefix.Length);
            if (int.TryParse(suffix, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) && parsed > 0)
            {
                return parsed;
            }

            return 1;
        }

        private static GameObject FindByName(string name)
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var found = FindRecursive(root.transform, name);
                if (found != null)
                {
                    return found.gameObject;
                }
            }

            return null;
        }

        private static Transform FindRecursive(Transform t, string name)
        {
            if (t.name == name)
            {
                return t;
            }

            for (int i = 0; i < t.childCount; i++)
            {
                var found = FindRecursive(t.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        [Serializable]
        private sealed class LayoutFile
        {
            public int version;
            public string scene;
            public LayoutEntry[] layout;
        }

        [Serializable]
        private sealed class LayoutEntry
        {
            public string id;
            public string placeholder;
            public string buildingType;
            public Vector3Data position;
            public Vector3Data rotation;
            public Vector3Data scale;
        }

        [Serializable]
        private sealed class Vector3Data
        {
            public float x;
            public float y;
            public float z;

            public Vector3 ToVector3()
            {
                return new Vector3(x, y, z);
            }
        }
    }
}
#endif
