#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Experiments.Environment2D5D.EditorTools.BuildingPlacement
{
    [InitializeOnLoad]
    public static class BuildingPlacementSceneViewController
    {
        private const float HandleScreenRadius = 10f;
        private const float MinDragDistance = 2f;

        private enum Interaction
        {
            None,
            Move,
            Resize,
            ResizeEdge
        }

        private static Interaction _interaction;
        private static CornerIndex _corner;
        private static EdgeIndex _edge;
        private static float _startScaleX;
        private static float _startScaleY;
        private static bool _dragging;
        private static float _dragStartPointerX;
        private static float _dragStartPointerY;
        private static float _dragStartBuildingX;
        private static float _dragStartBuildingY;
        private static float _dragFinalX;
        private static float _dragFinalY;

        static BuildingPlacementSceneViewController()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        // Both the read-only Overview ("BUILDING_PLACEMENT_OVERVIEW", HideFlags.DontSave)
        // and the interactive session's preview are plain scene GameObjects created by
        // editor tooling — Unity doesn't destroy DontSave objects on its own, and without
        // a domain/scene reload on Play (common project setting), they carry straight into
        // Play mode and render alongside BuildingRuntimeViewBootstrap's own runtime-spawned
        // buildings, doubling every building on screen. Cleared right before Play actually
        // starts so nothing from the editor tool leaks into the game view.
        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingEditMode) return;
            BuildingPlacementSession.Deactivate();
            BuildingPlacementOverview.DestroyOverview();
        }

        public static bool IsInteracting
        {
            get { return _interaction != Interaction.None; }
        }

        private static void OnSelectionChanged()
        {
            if (!BuildingPlacementSession.Active) return;
            SceneView.RepaintAll();
        }

        private static void OnHierarchyChanged()
        {
            BuildingPlacementSession.RebuildIfNeeded();
        }

        public static void OnSceneGUI(SceneView view)
        {
            if (!BuildingPlacementSession.Active) return;
            if (!BuildingPlacementPreview.Root)
            {
                BuildingPlacementSession.RebuildIfNeeded();
            }
            if (!BuildingPlacementPreview.Root) return;

            BuildingPlacementRecord record = BuildingPlacementSession.Record;
            if (record == null) return;

            DrawBuildingOutline(record);
            DrawGcpVisual(record);
            DrawHandles(record);
            DrawEdgeHandles(record);
            DrawInfoLabel(record);

            HandleInteractionInput(view, record);
        }

        private static void HandleInteractionInput(SceneView view, BuildingPlacementRecord record)
        {
            Event e = Event.current;
            if (e == null) return;

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                CancelInteraction();
                e.Use();
                return;
            }

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Delete)
            {
                CancelInteraction();
                BuildingPlacementSession.DeletePreview();
                e.Use();
                return;
            }

            if (e.type == EventType.KeyDown && (e.control || e.command) && e.keyCode == KeyCode.Z)
            {
                if (_interaction == Interaction.None)
                {
                    BuildingPlacementSession.Undo();
                }
                else
                {
                    CancelInteraction();
                }
                e.Use();
                return;
            }

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button != 0) return;

                    int cornerIndex = HitTestCorner(record);
                    if (cornerIndex >= 0)
                    {
                        _interaction = Interaction.Resize;
                        _corner = (CornerIndex)cornerIndex;
                        _startScaleX = record.scaleX;
                        _startScaleY = record.scaleY;
                        _dragging = false;
                        e.Use();
                        view.Repaint();
                        return;
                    }

                    int edgeIndex = HitTestEdge(record);
                    if (edgeIndex >= 0)
                    {
                        _interaction = Interaction.ResizeEdge;
                        _edge = (EdgeIndex)edgeIndex;
                        _startScaleX = record.scaleX;
                        _startScaleY = record.scaleY;
                        _dragging = false;
                        e.Use();
                        view.Repaint();
                        return;
                    }

                    if (HitTestBuilding(record, view))
                    {
                        _interaction = Interaction.Move;
                        Vector3 dragPointer = BuildingPlacementSession.ResolvePointer(view);
                        _dragStartPointerX = dragPointer.x;
                        _dragStartPointerY = dragPointer.y;
                        _dragStartBuildingX = record.x;
                        _dragStartBuildingY = record.terrainY;
                        _dragFinalX = record.x;
                        _dragFinalY = record.terrainY;

                        _dragging = false;
                        e.Use();
                        view.Repaint();
                        return;
                    }
                    break;

                case EventType.MouseDrag:
                    if (e.button != 0 || _interaction == Interaction.None) return;

                    Vector3 wp = BuildingPlacementSession.ResolvePointer(view);

                    switch (_interaction)
                    {
                        case Interaction.Move:
                            if (!_dragging)
                            {
                                _dragging = true;
                                BuildingPlacementSession.PushUndo(record.Clone());
                            }

                            float newX = _dragStartBuildingX + (wp.x - _dragStartPointerX);
                            float newY = _dragStartBuildingY + (wp.y - _dragStartPointerY);
                            _dragFinalX = newX;
                            _dragFinalY = newY;

                            BuildingPlacementRecord moved = record.Clone();
                            moved.x = newX;
                            moved.terrainY = newY;
                            BuildingPlacementSession.ApplyPlacement(moved);
                            break;

                        case Interaction.Resize:
                            if (!_dragging)
                            {
                                Vector3 c0 = BuildingPlacementPreview.GetCornerWorldPositions(record)[(int)_corner];
                                if ((wp - c0).sqrMagnitude < MinDragDistance * MinDragDistance) return;
                                _dragging = true;
                                BuildingPlacementSession.PushUndo(record.Clone());
                            }

                            bool shiftHeld = (e.shift || e.modifiers.HasFlag(EventModifiers.Shift));
                            bool forceProportional = BuildingPlacementSession.Proportional || shiftHeld;

                            if (forceProportional)
                            {
                                BuildingPlacementSession.ResizeProportional(_corner, wp, _startScaleX, false);
                            }
                            else
                            {
                                BuildingPlacementSession.ResizeFree(_corner, wp, _startScaleX, _startScaleY, false);
                            }
                            break;

                        case Interaction.ResizeEdge:
                            if (!_dragging)
                            {
                                Vector3 e0 = BuildingPlacementPreview.GetEdgeMidpointWorldPosition(record, _edge);
                                if ((wp - e0).sqrMagnitude < MinDragDistance * MinDragDistance) return;
                                _dragging = true;
                                BuildingPlacementSession.PushUndo(record.Clone());
                            }

                            // Edge handles are always a deliberate one-axis deform:
                            // Shift/Proportional don't apply here (that toggle governs the
                            // corner handles, which affect both axes at once).
                            BuildingPlacementSession.ResizeEdge(_edge, wp, false);
                            break;
                    }

                    e.Use();
                    view.Repaint();
                    break;

                case EventType.MouseUp:
                    if (e.button != 0) return;
                    if (_interaction == Interaction.None) return;

                    if (_interaction == Interaction.Move)
                    {
                        bool hasRoot = BuildingPlacementPreview.Root != null;
                        Vector3 previewBefore = hasRoot ? BuildingPlacementPreview.Root.transform.position : Vector3.zero;
                        BuildingPlacementRecord before = BuildingPlacementSession.Record;

                        Debug.Log("[BUILDING_PLACEMENT] MOVE_MOUSE_UP");
                        Debug.Log("[BUILDING_PLACEMENT] PREVIEW_POSITION_BEFORE_COMMIT=" +
                                  F(previewBefore.x) + "," + F(previewBefore.y) + "," + F(previewBefore.z));
                        Debug.Log("[BUILDING_PLACEMENT] RECORD_POSITION_BEFORE_COMMIT=" +
                                  F(before != null ? before.x : float.NaN) + "," +
                                  F(before != null ? before.terrainY : float.NaN));

                        BuildingPlacementRecord committed = before != null ? before.Clone() : new BuildingPlacementRecord();
                        committed.x = _dragFinalX;
                        committed.terrainY = _dragFinalY;
                        committed.z = GroundSurfaceResolver.BuildingZ;

                        BuildingPlacementSession.ApplyPlacement(committed);

                        Vector3 previewAfter = BuildingPlacementPreview.Root != null
                            ? BuildingPlacementPreview.Root.transform.position
                            : Vector3.zero;
                        BuildingPlacementRecord after = BuildingPlacementSession.Record;
                        Debug.Log("[BUILDING_PLACEMENT] RECORD_POSITION_AFTER_COMMIT=" +
                                  F(after != null ? after.x : float.NaN) + "," +
                                  F(after != null ? after.terrainY : float.NaN));
                        Debug.Log("[BUILDING_PLACEMENT] PREVIEW_POSITION_AFTER_COMMIT=" +
                                  F(previewAfter.x) + "," + F(previewAfter.y) + "," + F(previewAfter.z));
                    }

                    _interaction = Interaction.None;
                    _dragging = false;
                    e.Use();
                    view.Repaint();
                    break;
            }
        }

        private static void CancelInteraction()
        {
            if (_interaction == Interaction.None) return;
            if (_dragging)
            {
                BuildingPlacementSession.Undo();
            }
            _interaction = Interaction.None;
            _dragging = false;
        }

        private static string F(float v)
        {
            return v.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static int HitTestCorner(BuildingPlacementRecord record)
        {
            Vector3[] corners = BuildingPlacementPreview.GetCornerWorldPositions(record);
            if (corners == null) return -1;

            Vector2 mouse = Event.current.mousePosition;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 gui = HandleUtility.WorldToGUIPoint(corners[i]);
                if ((gui - mouse).sqrMagnitude <= HandleScreenRadius * HandleScreenRadius)
                {
                    return i;
                }
            }
            return -1;
        }

        private static int HitTestEdge(BuildingPlacementRecord record)
        {
            Vector2 mouse = Event.current.mousePosition;
            for (int i = 0; i < 4; i++)
            {
                EdgeIndex edge = (EdgeIndex)i;
                Vector3 world = BuildingPlacementPreview.GetEdgeMidpointWorldPosition(record, edge);
                Vector2 gui = HandleUtility.WorldToGUIPoint(world);
                if ((gui - mouse).sqrMagnitude <= HandleScreenRadius * HandleScreenRadius)
                {
                    return i;
                }
            }
            return -1;
        }

        private static bool HitTestBuilding(BuildingPlacementRecord record, SceneView view)
        {
            Vector3[] corners = BuildingPlacementPreview.GetCornerWorldPositions(record);
            if (corners == null) return false;

            Vector3 world = BuildingPlacementSession.ResolvePointer(view);

            float minX = Mathf.Min(corners[0].x, corners[2].x);
            float maxX = Mathf.Max(corners[0].x, corners[2].x);
            float minY = Mathf.Min(corners[0].y, corners[2].y);
            float maxY = Mathf.Max(corners[0].y, corners[2].y);

            return world.x >= minX && world.x <= maxX && world.y >= minY && world.y <= maxY;
        }

        private static void DrawBuildingOutline(BuildingPlacementRecord record)
        {
            Vector3[] corners = BuildingPlacementPreview.GetCornerWorldPositions(record);
            if (corners == null) return;

            Handles.color = new Color(0.2f, 0.65f, 1f, 0.85f);
            Handles.DrawLine(corners[0], corners[1], 2f);
            Handles.DrawLine(corners[1], corners[2], 2f);
            Handles.DrawLine(corners[2], corners[3], 2f);
            Handles.DrawLine(corners[3], corners[0], 2f);
        }

        private static void DrawGcpVisual(BuildingPlacementRecord record)
        {
            Vector3 gcp = new Vector3(record.x, record.terrainY, record.z);

            Handles.color = new Color(0.95f, 0.6f, 0.1f, 1f);
            float size = HandleUtility.GetHandleSize(gcp) * 0.08f;
            Handles.DrawSolidDisc(gcp, Vector3.forward, size);

            Handles.color = new Color(0.95f, 0.6f, 0.1f, 0.9f);
            Vector3 left = BuildingPlacementPreview.GetGroundLineLeft(record);
            Vector3 right = BuildingPlacementPreview.GetGroundLineRight(record);
            Handles.DrawLine(left, right, 2f);
        }

        private static void DrawHandles(BuildingPlacementRecord record)
        {
            Vector3[] corners = BuildingPlacementPreview.GetCornerWorldPositions(record);
            if (corners == null) return;

            Color fill = new Color(1f, 1f, 1f, 0.9f);
            Color border = new Color(0f, 0.15f, 0.3f, 1f);

            for (int i = 0; i < corners.Length; i++)
            {
                float size = HandleUtility.GetHandleSize(corners[i]) * 0.14f;
                Handles.color = fill;
                Handles.DrawSolidRectangleWithOutline(
                    new[]
                    {
                        corners[i] + new Vector3(-size, -size, 0f),
                        corners[i] + new Vector3(size, -size, 0f),
                        corners[i] + new Vector3(size, size, 0f),
                        corners[i] + new Vector3(-size, size, 0f)
                    },
                    fill,
                    border);
            }
        }

        // Edge-midpoint handles: a distinct color/shape from the corner squares so it
        // reads as "drag this to stretch one axis only" — a bar elongated ALONG the edge
        // (thin the way you'd drag it), the same visual convention as Figma/Photoshop
        // edge handles.
        private static void DrawEdgeHandles(BuildingPlacementRecord record)
        {
            Color fill = new Color(0.45f, 0.9f, 0.55f, 0.9f);
            Color border = new Color(0f, 0.25f, 0.1f, 1f);

            for (int i = 0; i < 4; i++)
            {
                EdgeIndex edge = (EdgeIndex)i;
                Vector3 mid = BuildingPlacementPreview.GetEdgeMidpointWorldPosition(record, edge);
                float size = HandleUtility.GetHandleSize(mid) * 0.14f;
                bool vertical = edge == EdgeIndex.Left || edge == EdgeIndex.Right;
                float halfLong = size * 1.4f;
                float halfShort = size * 0.5f;
                float hx = vertical ? halfShort : halfLong;
                float hy = vertical ? halfLong : halfShort;

                Handles.color = fill;
                Handles.DrawSolidRectangleWithOutline(
                    new[]
                    {
                        mid + new Vector3(-hx, -hy, 0f),
                        mid + new Vector3(hx, -hy, 0f),
                        mid + new Vector3(hx, hy, 0f),
                        mid + new Vector3(-hx, hy, 0f)
                    },
                    fill,
                    border);
            }
        }

        private static void DrawInfoLabel(BuildingPlacementRecord record)
        {
            Vector3[] corners = BuildingPlacementPreview.GetCornerWorldPositions(record);
            if (corners == null) return;

            Vector3 topCenter = new Vector3(
                (corners[0].x + corners[2].x) * 0.5f,
                Mathf.Max(corners[1].y, corners[3].y) + 0.6f,
                record.z);

            Handles.BeginGUI();
            Vector2 gui = HandleUtility.WorldToGUIPoint(topCenter);
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 11;

            GUI.Label(
                new Rect(gui.x - 40f, gui.y - 8f, 120f, 18f),
                record.buildingType + "  S=" + record.scaleX.ToString("0.00"),
                style);
            Handles.EndGUI();
        }
    }
}
#endif