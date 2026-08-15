#if UNITY_EDITOR
using System.IO;
using BeeKingdom.Experiments.Environment2D5D;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Experiments.Environment2D5D.EditorTools.BuildingPlacement
{
    public static class BuildingPlacementTests
    {
        private const string OfficialLayoutPath =
            "Assets/Experiments/Environment2D5D/Layout/BuildingPlaceholderLayout_FINAL.json";

        private static int _royalPalaceIndex = BuildingCatalog.IndexOf("ROYAL_PALACE");

        [MenuItem("BeeKingdom/Building Placement Editor/Run Placement Tests")]
        public static void RunAllTests()
        {
            int passed = 0;
            int failed = 0;

            if (Assert("Test 1 - Load Royal Palace from layout", Test1_LoadRoyalPalace)) passed++;
            else failed++;

            if (Assert("Test 2 - Move X, terrainY follows resolver", Test2_MoveFollowsTerrain)) passed++;
            else failed++;

            if (Assert("Test 3 - Proportional resize keeps GCP fixed", Test3_ProportionalResizeKeepsGcp)) passed++;
            else failed++;

            if (Assert("Test 4 - Back to previous scale keeps GCP", Test4_ScaleRestoreKeepsGcp)) passed++;
            else failed++;

            if (Assert("Test 5 - Undo restores previous placement", Test5_Undo)) passed++;
            else failed++;

            if (Assert("Test 6 - Official layout not modified by editor ops", Test6_LayoutUnchanged)) passed++;
            else failed++;

            string sidecarBackup = BackupSidecar();
            try
            {
                if (Assert("Test 7 - Never-saved building loads from layout", Test7_NeverSavedFromLayout)) passed++;
                else failed++;

                if (Assert("Test 8 - Save reload restore (X+Scale, GroundY recalc)", Test8_SavePersistRestore)) passed++;
                else failed++;

                if (Assert("Test 9 - Window workflow SAVE/switch/return via session", Test9_WindowWorkflowSessionSave)) passed++;
                else failed++;
            }
            finally
            {
                RestoreSidecar(sidecarBackup);
            }

            Debug.Log("[BUILDING_PLACEMENT_TESTS] Result: " + passed + " passed, " + failed + " failed.");
            EditorUtility.DisplayDialog("Building Placement Editor - Tests",
                "Tests: " + passed + " passed, " + failed + " failed.", "OK");
        }

        private static bool Assert(string name, System.Func<bool> test)
        {
            bool ok = false;
            try
            {
                ok = test();
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[BUILDING_PLACEMENT_TESTS] " + name + " THREW: " + ex.Message);
                return false;
            }
            Debug.Log(ok
                ? "[BUILDING_PLACEMENT_TESTS] PASS - " + name
                : "[BUILDING_PLACEMENT_TESTS] FAIL - " + name);
            return ok;
        }

        private static BuildingPlacementRecord SetupSession()
        {
            BuildingPlacementSession.LoadBuilding(_royalPalaceIndex);
            return BuildingPlacementSession.Record;
        }

        private static BuildingPlacementRecord SetupSessionFrom(BuildingPlacementRecord source)
        {
            BuildingPlacementSession.LoadBuilding(_royalPalaceIndex);
            BuildingPlacementSession.ApplyPlacement(source.Clone());
            return BuildingPlacementSession.Record;
        }

        private static bool Test1_LoadRoyalPalace()
        {
            BuildingPlacementRecord r = BuildingPlacementLayoutIO.LoadInitial("ROYAL_PALACE");
            if (r == null) return false;

            float expectedY = GroundSurfaceResolver.TerrainYFromX(r.x);
            Debug.Log("[TEST1] layout X=" + r.x.ToString("F4") + ", layoutScale=" + r.scaleX.ToString("F4") +
                      ", terrainY from resolver=" + r.terrainY.ToString("F4"));
            return True(Approx(r.terrainY, expectedY),
                    "TerrainY must come from resolver, got " + r.terrainY.ToString("F4"))
                && True(Approx(r.z, GroundSurfaceResolver.BuildingZ),
                    "Z must be " + GroundSurfaceResolver.BuildingZ + ", got " + r.z.ToString("F4"))
                && True(r.buildingType == "ROYAL_PALACE", "buildingType should be ROYAL_PALACE");
        }

        private static bool Test2_MoveFollowsTerrain()
        {
            SetupSession();
            float newX = 12f;
            float expectedY = GroundSurfaceResolver.TerrainYFromX(newX);

            bool saveG = BuildingPlacementSession.GroundAnchor;
            BuildingPlacementSession.GroundAnchor = true;
            BuildingPlacementSession.SetX(newX, false);
            bool ok = True(Approx(BuildingPlacementSession.Record.terrainY, expectedY),
                "Y should follow TerrainYFromX(" + newX + ")=" + expectedY.ToString("F4") +
                ", got " + BuildingPlacementSession.Record.terrainY.ToString("F4"))
                && True(Approx(BuildingPlacementSession.Record.x, newX),
                    "X should be " + newX + ", got " + BuildingPlacementSession.Record.x.ToString("F4"))
                && True(Approx(BuildingPlacementSession.Record.z, GroundSurfaceResolver.BuildingZ),
                    "Z should be 29.95 after re-anchor");
            BuildingPlacementSession.GroundAnchor = saveG;
            return ok;
        }

        private static bool Test3_ProportionalResizeKeepsGcp()
        {
            BuildingPlacementRecord r = SetupSession();
            Vector3 gcpBefore = new Vector3(r.x, r.terrainY, r.z);

            Vector3[] offsets = BuildingPlacementPreview.GetCornerOffsetsAtScaleOne();
            if (!True(offsets != null && offsets.Length == 4, "corner offsets should be available")) return false;

            Vector3 gcp = new Vector3(r.x, r.terrainY, r.z);
            Vector3 pointer = gcp + offsets[2] * 1.5f;

            bool saveP = BuildingPlacementSession.Proportional;
            BuildingPlacementSession.Proportional = true;
            BuildingPlacementSession.ResizeProportional(CornerIndex.TopRight, pointer, r.scaleX, false);

            bool ok = True(Approx(BuildingPlacementSession.Record.x, gcpBefore.x),
                "GCP X must not move, got " + BuildingPlacementSession.Record.x.ToString("F4"))
                && True(Approx(BuildingPlacementSession.Record.terrainY, gcpBefore.y),
                    "GCP Y must not move, got " + BuildingPlacementSession.Record.terrainY.ToString("F4"))
                && True(Approx(BuildingPlacementSession.Record.z, gcpBefore.z),
                    "GCP Z must not move, got " + BuildingPlacementSession.Record.z.ToString("F4"))
                && True(Approx(BuildingPlacementSession.Record.scaleX, 1.5f * r.scaleX, 0.001f),
                    "Proportional scale should be 1.5x, got " + BuildingPlacementSession.Record.scaleX.ToString("F4"))
                && True(Approx(BuildingPlacementSession.Record.scaleX, BuildingPlacementSession.Record.scaleY),
                    "Proportional keeps W/H equal");

            BuildingPlacementSession.Proportional = saveP;
            return ok;
        }

        private static bool Test4_ScaleRestoreKeepsGcp()
        {
            BuildingPlacementRecord r = SetupSession();
            Vector3 gcpRef = new Vector3(r.x, r.terrainY, r.z);

            Vector3[] offsets = BuildingPlacementPreview.GetCornerOffsetsAtScaleOne();
            if (!True(offsets != null && offsets.Length == 4, "corner offsets should be available")) return false;

            Vector3 gcp = new Vector3(r.x, r.terrainY, r.z);
            bool saveP = BuildingPlacementSession.Proportional;
            BuildingPlacementSession.Proportional = true;
            BuildingPlacementSession.ResizeProportional(CornerIndex.TopRight, gcp + offsets[2] * 2f, r.scaleX, false);
            BuildingPlacementSession.ResizeProportional(CornerIndex.TopRight, gcp + offsets[2] * 1f, r.scaleX, false);

            bool ok = True(Approx(BuildingPlacementSession.Record.scaleX, r.scaleX, 0.001f),
                "Scale should restore to original, got " + BuildingPlacementSession.Record.scaleX.ToString("F4"))
                && True(Approx(BuildingPlacementSession.Record.x, gcpRef.x), "X drift")
                && True(Approx(BuildingPlacementSession.Record.terrainY, gcpRef.y), "Y drift")
                && True(Approx(BuildingPlacementSession.Record.z, gcpRef.z), "Z drift");

            BuildingPlacementSession.Proportional = saveP;
            return ok;
        }

        private static bool Test5_Undo()
        {
            BuildingPlacementRecord r = SetupSession();

            float newX = 8f;
            BuildingPlacementSession.PushUndo(BuildingPlacementSession.Record.Clone());
            BuildingPlacementSession.SetX(newX, false);

            float movedX = BuildingPlacementSession.Record.x;
            BuildingPlacementSession.Undo();

            return True(!Approx(movedX, r.x), "Move should have changed X")
                && True(Approx(BuildingPlacementSession.Record.x, r.x),
                    "Undo should restore previous X, got " + BuildingPlacementSession.Record.x.ToString("F4"))
                && True(Approx(BuildingPlacementSession.Record.terrainY, r.terrainY),
                    "Undo should restore previous TerrainY");
        }

        private static bool Test6_LayoutUnchanged()
        {
            string before = string.Empty;
            if (File.Exists(OfficialLayoutPath)) before = File.ReadAllText(OfficialLayoutPath);

            SetupSession();
            BuildingPlacementSession.SetX(11f, false);
            Vector3[] offsets = BuildingPlacementPreview.GetCornerOffsetsAtScaleOne();
            if (offsets != null && offsets.Length == 4)
            {
                BuildingPlacementSession.ResizeProportional(CornerIndex.TopRight,
                    new Vector3(BuildingPlacementSession.Record.x + offsets[2].x * 2f,
                        BuildingPlacementSession.Record.terrainY, BuildingPlacementSession.Record.z),
                    BuildingPlacementSession.Record.scaleX, false);
            }
            BuildingPlacementSession.Undo();

            string after = string.Empty;
            if (File.Exists(OfficialLayoutPath)) after = File.ReadAllText(OfficialLayoutPath);

            return string.Equals(before, after);
        }

        private static string BackupSidecar()
        {
            string path = BuildingPlacementLayoutIO.SidecarPath;
            if (File.Exists(path)) return File.ReadAllText(path);
            return null;
        }

        private static void RestoreSidecar(string backup)
        {
            string path = BuildingPlacementLayoutIO.SidecarPath;
            if (backup != null)
            {
                File.WriteAllText(path, backup);
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
            }
            AssetDatabase.Refresh();
        }

        private static bool Test7_NeverSavedFromLayout()
        {
            string sidecar = BuildingPlacementLayoutIO.SidecarPath;
            if (File.Exists(sidecar)) File.Delete(sidecar);
            AssetDatabase.Refresh();

            BuildingPlacementRecord r = BuildingPlacementLayoutIO.LoadInitial("ROYAL_PALACE");
            if (r == null) return false;

            Debug.Log("[TEST7] layout values: X=" + r.x.ToString("F4") + " Scale=" + r.scaleX.ToString("F4") +
                      " GroundY=" + r.terrainY.ToString("F4"));
            return True(Approx(r.x, 1.83f), "X should be layout value 1.83, got " + r.x.ToString("F4"))
                && True(Approx(r.scaleX, 0.27f), "Scale should be layout value 0.27, got " + r.scaleX.ToString("F4"))
                && True(Approx(r.terrainY, GroundSurfaceResolver.TerrainYFromX(r.x)),
                    "GroundY must be recomputed from X, got " + r.terrainY.ToString("F4"))
                && True(Approx(r.z, GroundSurfaceResolver.BuildingZ), "Z must be 29.95");
        }

        private static bool Test8_SavePersistRestore()
        {
            string rpType = "ROYAL_PALACE";

            BuildingPlacementRecord modified = BuildingPlacementLayoutIO.LoadInitial(rpType);
            modified.x = 7.25f;
            modified.terrainY = GroundSurfaceResolver.TerrainYFromX(modified.x);
            modified.scaleX = 0.63f;
            modified.scaleY = 0.63f;
            modified.rotation = 0f;
            modified.z = GroundSurfaceResolver.BuildingZ;

            BuildingPlacementLayoutIO.SavePlacement(modified);

            BuildingPlacementRecord sw = BuildingPlacementLayoutIO.LoadInitial("BARRACK");
            if (sw == null || sw.buildingType != "BARRACK") return false;

            BuildingPlacementRecord rel = BuildingPlacementLayoutIO.LoadInitial(rpType);
            if (rel == null) return false;

            bool ok = True(Approx(rel.x, 7.25f), "X must be the saved 7.25, got " + rel.x.ToString("F4"))
                && True(Approx(rel.scaleX, 0.63f), "Scale must be the saved 0.63, got " + rel.scaleX.ToString("F4"))
                && True(Approx(rel.scaleY, 0.63f), "ScaleY must equal Scale")
                && True(Approx(rel.terrainY, GroundSurfaceResolver.TerrainYFromX(rel.x)),
                    "GroundY must be recomputed from saved X, got " + rel.terrainY.ToString("F4"))
                && True(Approx(rel.z, GroundSurfaceResolver.BuildingZ), "Z must stay 29.95");

            Debug.Log("[TEST8] reload: X=" + rel.x.ToString("F4") + " Scale=" + rel.scaleX.ToString("F4") +
                      " GroundY=" + rel.terrainY.ToString("F4"));
            return ok;
        }

        private static bool Test9_WindowWorkflowSessionSave()
        {
            BuildingPlacementSession.LoadBuilding(_royalPalaceIndex);
            if (BuildingPlacementSession.Record == null) return false;

            Debug.Log("[TEST9] step1 select ROYAL_PALACE -> X=" +
                      BuildingPlacementSession.Record.x.ToString("F4") +
                      " Scale=" + BuildingPlacementSession.Record.scaleX.ToString("F4"));

            BuildingPlacementSession.SetX(7.25f, false);
            BuildingPlacementSession.SetScale(0.63f, false);

            Debug.Log("[TEST9] step2 moved+resized session -> X=" +
                      BuildingPlacementSession.Record.x.ToString("F4") +
                      " Scale=" + BuildingPlacementSession.Record.scaleX.ToString("F4"));

            BuildingPlacementLayoutIO.SavePlacement(BuildingPlacementSession.Record);

            int otherIndex = -1;
            for (int i = 0; i < BuildingCatalog.Entries.Length; i++)
            {
                if (i != _royalPalaceIndex)
                {
                    otherIndex = i;
                    break;
                }
            }
            if (otherIndex < 0) return false;

            BuildingPlacementSession.LoadBuilding(otherIndex);
            Debug.Log("[TEST9] step3 switched to " + BuildingCatalog.Entries[otherIndex].buildingType);

            BuildingPlacementSession.LoadBuilding(_royalPalaceIndex);
            Debug.Log("[TEST9] step4 returned to ROYAL_PALACE -> X=" +
                      BuildingPlacementSession.Record.x.ToString("F4") +
                      " Scale=" + BuildingPlacementSession.Record.scaleX.ToString("F4"));

            bool ok = True(Approx(BuildingPlacementSession.Record.x, 7.25f),
                    "After switch+return X must be saved 7.25, got " +
                    BuildingPlacementSession.Record.x.ToString("F4"))
                && True(Approx(BuildingPlacementSession.Record.scaleX, 0.63f),
                    "After switch+return Scale must be saved 0.63, got " +
                    BuildingPlacementSession.Record.scaleX.ToString("F4"))
                && True(Approx(BuildingPlacementSession.Record.terrainY,
                        GroundSurfaceResolver.TerrainYFromX(BuildingPlacementSession.Record.x)),
                    "GroundY must be recomputed from saved X");

            return ok;
        }

        private static bool True(bool cond, string msg)
        {
            if (!cond) Debug.LogError("[TEST] " + msg);
            return cond;
        }

        private static bool Approx(float a, float b, float eps = 0.01f)
        {
            return Mathf.Abs(a - b) <= eps;
        }
    }
}
#endif