#if UNITY_EDITOR
using System.Collections.Generic;
using BeeKingdom.Experiments.Environment2D5D;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Experiments.Environment2D5D.EditorTools.BuildingPlacement
{
    public enum CornerIndex
    {
        BottomLeft = 0,
        BottomRight = 1,
        TopRight = 2,
        TopLeft = 3
    }

    // Edge-midpoint handles: drag Left/Right to change only scaleX (width), drag Top/
    // Bottom to change only scaleY (height) — a deliberate one-axis deform, unlike the
    // corner handles which always affect both axes (proportionally or freely).
    public enum EdgeIndex
    {
        Left = 0,
        Right = 1,
        Top = 2,
        Bottom = 3
    }

    public static class BuildingPlacementSession
    {
        private const int MaxUndo = 64;
        private const float MinScale = 0.02f;
        private const float MaxScale = 50f;

        private static readonly List<BuildingPlacementRecord> _undo = new List<BuildingPlacementRecord>();

        private static int _currentIndex = BuildingCatalog.IndexOf("ROYAL_PALACE");
        private static BuildingPlacementRecord _record;
        private static bool _proportional = true;
        private static bool _groundAnchor = true;
        private static bool _active;
        private static bool _legacyCleaned;
        private static bool _previewDeleted;

        public delegate void ChangeHandler();
        public static event ChangeHandler Changed;

        public static bool Active { get { return _active; } }
        public static bool Proportional { get { return _proportional; } set { _proportional = value; } }
        public static bool GroundAnchor { get { return _groundAnchor; } set { _groundAnchor = value; } }

        public static int CurrentIndex
        {
            get { return _currentIndex; }
            set { SetCurrentIndex(value); }
        }

        public static BuildingCatalogEntry CurrentEntry
        {
            get { return BuildingCatalog.Entries[_currentIndex]; }
        }

        public static BuildingPlacementRecord Record
        {
            get { return _record; }
        }

        public static void Activate()
        {
            // Symmetric with BuildOverview(), which deactivates this session when the
            // read-only Overview builds: if the Overview's static "BUILDING_PLACEMENT_
            // OVERVIEW" copies are left in the scene while this interactive session
            // starts, the two coexist and the session's own preview (the one the drag
            // handles actually move) gets visually lost among the Overview's frozen
            // duplicates, e.g. dragging Royal Palace here has no visible effect because
            // the Overview's separate, un-moving Royal Palace copy is still rendered.
            BuildingPlacementOverview.DestroyOverview();

            if (_record == null)
            {
                LoadBuilding(_currentIndex);
            }
            else
            {
                BuildingPlacementPreview.RebuildIfNeeded();
            }
            _active = true;
            BuildingPlacementPreview.SetVisible(true);
            EnsureLegacyClean();
            NotifyChanged();
        }

        public static void Deactivate()
        {
            if (!_active) return;
            _active = false;
            BuildingPlacementPreview.SetVisible(false);
            NotifyChanged();
        }

        public static void LoadBuilding(int index)
        {
            EnsureLegacyClean();

            _currentIndex = Mathf.Clamp(index, 0, BuildingCatalog.Entries.Length - 1);
            _record = BuildingPlacementLayoutIO.LoadInitial(CurrentEntry.buildingType);
            _undo.Clear();
            _previewDeleted = false;

            BuildingPlacementPreview.Build(CurrentEntry);
            BuildingPlacementPreview.UpdateTransform(_record);
            BuildingPlacementPreview.SetVisible(true);
            _active = true;
            NotifyChanged();
        }

        public static void RebuildIfNeeded()
        {
            if (!_active) return;
            if (_previewDeleted) return;
            EnsureLegacyClean();
            if (!BuildingPlacementPreview.Root)
            {
                BuildingPlacementPreview.Build(CurrentEntry);
                BuildingPlacementPreview.UpdateTransform(_record);
                BuildingPlacementPreview.SetVisible(true);
            }
        }

        public static void ApplyPlacement(BuildingPlacementRecord record)
        {
            if (record == null) return;
            _record = record;
            BuildingPlacementPreview.UpdateTransform(record);
            NotifyChanged();
        }

        public static void SetX(float newX, bool commitUndo)
        {
            if (_record == null) return;

            float oldX = _record.x;
            if (Mathf.Approximately(oldX, newX)) return;

            PushUndoIfNeeded(commitUndo, _record.Clone());

            // GroundSurfaceResolver's curve is calibrated for the single sloped-ground
            // SpatialV3 building test, not for HiveMap layouts where each hex compartment
            // has its own independently authored TerrainY (not a function of X). Following
            // that curve while dragging in a HiveMap context snaps buildings to wildly
            // wrong heights, so Ground Anchor only recomputes terrainY outside HiveMap
            // (UseOfficialLayoutFallback == true); in HiveMap context, dragging X leaves
            // the already-loaded TerrainY untouched.
            if (_groundAnchor && BuildingPlacementLayoutIO.UseOfficialLayoutFallback)
            {
                _record.x = newX;
                _record.terrainY = GroundSurfaceResolver.TerrainYFromX(newX);
                _record.z = GroundSurfaceResolver.BuildingZ;
            }
            else
            {
                _record.x = newX;
            }

            BuildingPlacementPreview.UpdateTransform(_record);
            NotifyChanged();
        }

        public static void SetScale(float uniformScale, bool commitUndo)
        {
            if (_record == null) return;
            float clamped = Mathf.Clamp(uniformScale, MinScale, MaxScale);
            if (Mathf.Approximately(clamped, _record.scaleX) &&
                Mathf.Approximately(clamped, _record.scaleY)) return;

            PushUndoIfNeeded(commitUndo, _record.Clone());

            if (_proportional)
            {
                _record.scaleX = clamped;
                _record.scaleY = clamped;
            }
            else
            {
                _record.scaleX = clamped;
            }

            BuildingPlacementPreview.UpdateTransform(_record);
            NotifyChanged();
        }

        public static void ResizeProportional(CornerIndex corner, Vector3 pointerWorld, float startScale, bool commitUndo)
        {
            if (_record == null) return;

            Vector3 gcp = new Vector3(_record.x, _record.terrainY, _record.z);
            Vector3[] offsets = BuildingPlacementPreview.GetCornerOffsetsAtScaleOne();
            if (offsets == null) return;

            Vector2 d1 = new Vector2(offsets[(int)corner].x, offsets[(int)corner].y);
            float baseDist = d1.magnitude;
            if (baseDist < 0.0001f) return;

            Vector2 dp = new Vector2(pointerWorld.x - gcp.x, pointerWorld.y - gcp.y);

            float newScale = Mathf.Clamp(dp.magnitude / baseDist, MinScale, MaxScale);
            PushUndoIfNeeded(commitUndo, _record.Clone());
            _record.scaleX = newScale;
            _record.scaleY = newScale;
            BuildingPlacementPreview.UpdateTransform(_record);
            NotifyChanged();
        }

        public static void ResizeFree(CornerIndex corner, Vector3 pointerWorld, float startScaleX, float startScaleY, bool commitUndo)
        {
            if (_record == null) return;

            Vector3 gcp = new Vector3(_record.x, _record.terrainY, _record.z);
            Vector3[] offsets = BuildingPlacementPreview.GetCornerOffsetsAtScaleOne();
            if (offsets == null) return;

            Vector2 d1 = new Vector2(offsets[(int)corner].x, offsets[(int)corner].y);
            Vector2 dp = new Vector2(pointerWorld.x - gcp.x, pointerWorld.y - gcp.y);

            float newX = startScaleX;
            float newY = startScaleY;
            if (Mathf.Abs(d1.x) > 0.0001f)
            {
                newX = dp.x / d1.x;
            }
            if (Mathf.Abs(d1.y) > 0.0001f)
            {
                newY = dp.y / d1.y;
            }

            PushUndoIfNeeded(commitUndo, _record.Clone());
            _record.scaleX = Mathf.Clamp(newX, MinScale, MaxScale);
            _record.scaleY = Mathf.Clamp(newY, MinScale, MaxScale);
            BuildingPlacementPreview.UpdateTransform(_record);
            NotifyChanged();
        }

        // Single-axis deform via an edge-midpoint handle: Left/Right only ever touch
        // scaleX, Top/Bottom only ever touch scaleY. Uses the SAME corner-offset-at-
        // scale-1 math as ResizeFree/ResizeProportional (offset -> absolute scale from
        // pointer distance), just reading only the axis that edge controls.
        public static void ResizeEdge(EdgeIndex edge, Vector3 pointerWorld, bool commitUndo)
        {
            if (_record == null) return;

            Vector3 gcp = new Vector3(_record.x, _record.terrainY, _record.z);
            Vector3[] offsets = BuildingPlacementPreview.GetCornerOffsetsAtScaleOne();
            if (offsets == null) return;

            bool horizontal = edge == EdgeIndex.Left || edge == EdgeIndex.Right;
            CornerIndex referenceCorner = edge == EdgeIndex.Left ? CornerIndex.TopLeft
                : edge == EdgeIndex.Right ? CornerIndex.TopRight
                : edge == EdgeIndex.Top ? CornerIndex.TopLeft
                : CornerIndex.BottomLeft;
            Vector3 o1 = offsets[(int)referenceCorner];

            PushUndoIfNeeded(commitUndo, _record.Clone());

            if (horizontal)
            {
                if (Mathf.Abs(o1.x) > 0.0001f)
                {
                    float dx = pointerWorld.x - gcp.x;
                    _record.scaleX = Mathf.Clamp(dx / o1.x, MinScale, MaxScale);
                }
            }
            else
            {
                if (Mathf.Abs(o1.y) > 0.0001f)
                {
                    float dy = pointerWorld.y - gcp.y;
                    _record.scaleY = Mathf.Clamp(dy / o1.y, MinScale, MaxScale);
                }
            }

            BuildingPlacementPreview.UpdateTransform(_record);
            NotifyChanged();
        }

        public static void Undo()
        {
            if (_undo.Count == 0) return;
            BuildingPlacementRecord prev = _undo[_undo.Count - 1];
            _undo.RemoveAt(_undo.Count - 1);
            ApplyPlacement(prev);
        }

        public static void PushUndo(BuildingPlacementRecord snapshot)
        {
            if (snapshot == null) return;
            _undo.Add(snapshot);
            if (_undo.Count > MaxUndo) _undo.RemoveAt(0);
        }

        public static void DeletePreview()
        {
            if (!_active) return;
            _undo.Clear();
            _previewDeleted = true;
            BuildingPlacementPreview.Destroy();
            NotifyChanged();
        }

        public static Vector3 ResolvePointer(SceneView view)
        {
            if (view == null) return Vector3.zero;

            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

            Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, GroundSurfaceResolver.BuildingZ));
            float dist;
            if (plane.Raycast(ray, out dist))
            {
                return ray.GetPoint(dist);
            }

            Camera cam = view.camera;
            if (cam != null)
            {
                Plane camPlane = new Plane(-cam.transform.forward, new Vector3(0f, 0f, GroundSurfaceResolver.BuildingZ));
                if (camPlane.Raycast(ray, out dist))
                {
                    return ray.GetPoint(dist);
                }
            }

            return Vector3.zero;
        }

        private static void PushUndoIfNeeded(bool commit, BuildingPlacementRecord snapshot)
        {
            if (commit) PushUndo(snapshot);
        }

        private static void SetCurrentIndex(int index)
        {
            if (!_active)
            {
                _currentIndex = Mathf.Clamp(index, 0, BuildingCatalog.Entries.Length - 1);
                return;
            }
            LoadBuilding(index);
        }

        private static void EnsureLegacyClean()
        {
            if (_legacyCleaned) return;
            _legacyCleaned = true;

            RoyalPalaceTestGate.LegacyAutoCreateDisabled = true;

            string[] roots =
            {
                "ROYAL_PALACE_SITE_TEST",
                "ROYAL_PALACE_SCALE_TEST",
                "ROYAL_PALACE_FINAL_SCALE_TEST",
                "GROUND_ANCHOR_PROTOTYPE_ROYAL_PALACE",
                "__GROUND_ANCHOR_DIAG__",
                "ROYAL_PALACE_013"
            };

            GameObject[] all = Object.FindObjectsOfType<GameObject>();
            int removed = 0;
            for (int i = 0; i < all.Length; i++)
            {
                GameObject go = all[i];
                if (go == null) continue;
                if ((go.hideFlags & HideFlags.DontSave) == 0) continue;
                for (int r = 0; r < roots.Length; r++)
                {
                    if (go.name == roots[r])
                    {
                        Object.DestroyImmediate(go);
                        removed++;
                        break;
                    }
                }
            }

            if (removed > 0)
            {
                Debug.Log("[BUILDING_PLACEMENT] " + removed + " objet(s) des anciens outils supprimé(s) " +
                          "(DontSave, scène intacte) ; l'éditeur de placement reste seul.");
            }
        }

        private static void NotifyChanged()
        {
            ChangeHandler handler = Changed;
            if (handler != null) handler();
        }
    }
}
#endif