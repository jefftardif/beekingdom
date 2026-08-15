#if UNITY_EDITOR
using BeeKingdom.Experiments.Environment2D5D;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Experiments.Environment2D5D.EditorTools.BuildingPlacement
{
    public class BuildingPlacementWindow : EditorWindow
    {
        private const string WindowTitle = "Bee Kingdom - Building Placement Editor";
        private bool _proportionalField;
        private bool _groundAnchorField;
        private GUIStyle _sectionStyle;
        private GUIStyle _hintStyle;

        [MenuItem("BeeKingdom/Building Placement Editor")]
        public static void Open()
        {
            BuildingPlacementWindow win = GetWindow<BuildingPlacementWindow>(false, "Building Placement Editor");
            win.minSize = new Vector2(320f, 420f);
            win.Show();
        }

        private void OnEnable()
        {
            _proportionalField = BuildingPlacementSession.Proportional;
            _groundAnchorField = BuildingPlacementSession.GroundAnchor;
            BuildingPlacementSession.Activate();
            BuildingPlacementSession.Changed += OnSessionChanged;

            if (Selection.activeGameObject != null)
            {
                SceneView.FrameLastActiveSceneView();
            }
            SceneView.RepaintAll();
        }

        private void OnDisable()
        {
            BuildingPlacementSession.Changed -= OnSessionChanged;
        }

        private void OnDestroy()
        {
            BuildingPlacementSession.Deactivate();
            BuildingPlacementPreview.Destroy();
        }

        private void OnSessionChanged()
        {
            _proportionalField = BuildingPlacementSession.Proportional;
            _groundAnchorField = BuildingPlacementSession.GroundAnchor;
            Repaint();
            SceneView.RepaintAll();
        }

        private void OnGUI()
        {
            if (_sectionStyle == null)
            {
                _sectionStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
                _hintStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
                _hintStyle.fontSize = 10;
                _hintStyle.normal.textColor = new Color(0.6f, 0.62f, 0.65f);
            }

            BuildingPlacementRecord record = BuildingPlacementSession.Record;

            GUILayout.Space(4f);
            EditorGUILayout.LabelField("BEE KINGDOM - BUILDING PLACEMENT", EditorStyles.boldLabel);

            EditorGUILayout.Space(4f);

            int selected = EditorGUILayout.Popup(
                "Building",
                BuildingPlacementSession.CurrentIndex,
                BuildingCatalogNames());

            if (selected != BuildingPlacementSession.CurrentIndex)
            {
                Debug.Log("[BUILDING_PLACEMENT] SELECTION_CHANGED=" + BuildingCatalogNames()[selected] +
                          " index=" + selected);
                BuildingPlacementSession.LoadBuilding(selected);
                record = BuildingPlacementSession.Record;
            }

            if (record == null)
            {
                EditorGUILayout.HelpBox(
                    "Load a building to start placing. Artwork missing? Check Assets/BeeKingdom/Art/Buildings.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.Space(2f);

            EditorGUI.BeginChangeCheck();
            float newX = EditorGUILayout.FloatField("X", record.x);
            if (EditorGUI.EndChangeCheck())
            {
                BuildingPlacementSession.SetX(newX, true);
                record = BuildingPlacementSession.Record;
            }

            EditorGUILayout.LabelField("Ground Y", record.terrainY.ToString("F3"));
            EditorGUILayout.LabelField("Z", record.z.ToString("F3"));

            GUILayout.Space(2f);

            EditorGUI.BeginChangeCheck();
            float newScale = EditorGUILayout.FloatField("Scale", record.scaleX);
            if (EditorGUI.EndChangeCheck())
            {
                BuildingPlacementSession.SetScale(newScale, true);
                record = BuildingPlacementSession.Record;
            }

            GUILayout.Space(2f);

            EditorGUI.BeginChangeCheck();
            _proportionalField = EditorGUILayout.Toggle("Proportional", _proportionalField);
            if (EditorGUI.EndChangeCheck())
            {
                BuildingPlacementSession.Proportional = _proportionalField;
            }

            EditorGUI.BeginChangeCheck();
            _groundAnchorField = EditorGUILayout.Toggle("Ground Anchor", _groundAnchorField);
            if (EditorGUI.EndChangeCheck())
            {
                BuildingPlacementSession.GroundAnchor = _groundAnchorField;
                if (_groundAnchorField && record != null)
                {
                    BuildingPlacementSession.SetX(record.x, true);
                }
            }

            GUILayout.Space(6f);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("RESET"))
            {
                BuildingPlacementSession.LoadBuilding(BuildingPlacementSession.CurrentIndex);
            }
            if (GUILayout.Button("SAVE"))
            {
                SaveCurrentPlacement();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6f);

            GUILayout.Label("Shortcuts", _sectionStyle);
            GUILayout.Label(
                "  Click building  = select & drag to move\n" +
                "  Drag corner     = resize (Proportional keeps aspect)\n" +
                "  Shift + resize  = keep proportional while resizing\n" +
                "  Esc             = cancel current operation\n" +
                "  Ctrl+Z          = undo last change\n" +
                "  Delete          = remove preview only (never the asset)\n\n" +
                "LOAD loads the official layout position as initial values.\n" +
                "SAVE writes ONLY to the sidecar file BuildingPlacementEditor_Saves.json;\n" +
                "the official layout is never modified.",
                _hintStyle);
        }

        private static string[] BuildingCatalogNames()
        {
            string[] names = new string[BuildingCatalog.Entries.Length];
            for (int i = 0; i < names.Length; i++)
            {
                names[i] = BuildingCatalog.Entries[i].displayName;
            }
            return names;
        }

        public static void SaveCurrentPlacement()
        {
            Debug.Log("[BUILDING_PLACEMENT] WINDOW_SAVE_CLICK building=" +
                      (BuildingPlacementSession.Record != null ? BuildingPlacementSession.Record.buildingType : "null"));
            BuildingPlacementLayoutIO.SaveWithConfirmation(BuildingPlacementSession.Record);
        }
    }
}
#endif