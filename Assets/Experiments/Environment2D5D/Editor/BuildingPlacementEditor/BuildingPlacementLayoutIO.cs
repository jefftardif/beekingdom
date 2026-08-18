#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using BeeKingdom.Buildings.Placement;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Experiments.Environment2D5D.EditorTools.BuildingPlacement
{
    [Serializable]
    internal sealed class LayoutFile
    {
        public LayoutPlaceholderEntry[] placeholders;
    }

    [Serializable]
    internal sealed class LayoutPlaceholderEntry
    {
        public string id;
        public string buildingType;
        public LayoutVec3 position;
        public LayoutVec3 rotation;
        public LayoutVec3 scale;
    }

    [Serializable]
    internal sealed class LayoutVec3
    {
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    internal sealed class PlacementSaveFile
    {
        public List<PlacementSaveEntry> placements = new List<PlacementSaveEntry>();
    }

    [Serializable]
    internal sealed class PlacementSaveEntry
    {
        public string buildingId;
        public string buildingType;
        public float X;
        public float TerrainY;
        public float Z;
        public float Rotation;
        public float Scale;
    }

    public static class BuildingPlacementLayoutIO
    {
        private const string OfficialLayoutPath =
            "Assets/Experiments/Environment2D5D/Layout/BuildingPlaceholderLayout_FINAL.json";
        private const string DefaultSidecarSavePath =
            "Assets/Experiments/Environment2D5D/Config/BuildingPlacementEditor_Saves.json";

        public static string SidecarPath
        {
            get
            {
                var context = GetActiveHiveMapContext();
                if (context != null && !string.IsNullOrEmpty(context.sidecarPath))
                {
                    return context.sidecarPath;
                }
                return DefaultSidecarSavePath;
            }
        }

        public static bool UseOfficialLayoutFallback
        {
            get
            {
                var context = GetActiveHiveMapContext();
                return context == null || context.useOfficialLayoutFallback;
            }
        }

        private static HiveMapPlacementContext GetActiveHiveMapContext()
        {
            // Cherche un contexte HiveMap dans la scène courante (Editor only)
            return UnityEngine.Object.FindFirstObjectByType<HiveMapPlacementContext>();
        }

        public static BuildingPlacementRecord LoadInitial(string buildingType)
        {
            BuildingPlacementRecord record;
            if (UseOfficialLayoutFallback)
            {
                record = BuildFromOfficialLayout(buildingType);
            }
            else
            {
                // Nouveau contexte HiveMap : record neutre sans layout officiel
                record = new BuildingPlacementRecord
                {
                    buildingType = buildingType,
                    z = GroundSurfaceResolver.BuildingZ,
                    scaleX = 1f,
                    scaleY = 1f
                };
                BuildingCatalogEntry entry = BuildingCatalog.Find(buildingType);
                record.buildingId = entry != null ? "BUILDING_" + buildingType.Replace("_", "") : "BUILDING_" + buildingType;
                record.x = 0f;
                record.terrainY = GroundSurfaceResolver.TerrainYFromX(record.x);
                record.layoutReferenceY = record.terrainY;
            }

            PlacementSaveEntry saved = LoadSavedEntry(buildingType);
            if (saved != null)
            {
                record.buildingId = string.IsNullOrEmpty(saved.buildingId) ? record.buildingId : saved.buildingId;
                record.x = saved.X;
                record.z = saved.Z;
                record.rotation = saved.Rotation;
                if (saved.Scale > 0f)
                {
                    record.scaleX = saved.Scale;
                    record.scaleY = saved.Scale;
                }
                record.terrainY = saved.TerrainY;
            }
            else
            {
                record.terrainY = GroundSurfaceResolver.TerrainYFromX(record.x);
            }

            Debug.Log("[BUILDING_PLACEMENT] LOAD_SOURCE=" + (saved != null ? "SIDECAR" : (UseOfficialLayoutFallback ? "OFFICIAL_LAYOUT" : "NEUTRAL")) +
                      " LOAD_BUILDING=" + buildingType +
                      " LOAD_X=" + F(record.x, 3) +
                      " LOAD_TERRAINY=" + F(record.terrainY, 3) +
                      " LOAD_SCALE=" + F(record.scaleX, 3));
            return record;
        }

        private static BuildingPlacementRecord BuildFromOfficialLayout(string buildingType)
        {
            BuildingPlacementRecord record = new BuildingPlacementRecord
            {
                buildingType = buildingType,
                z = GroundSurfaceResolver.BuildingZ,
                scaleX = 1f,
                scaleY = 1f
            };

            BuildingCatalogEntry entry = BuildingCatalog.Find(buildingType);
            record.buildingId = entry != null ? "BUILDING_" + buildingType.Replace("_", "") : "BUILDING_" + buildingType;
            record.x = 0f;
            record.terrainY = GroundSurfaceResolver.TerrainYFromX(record.x);
            record.layoutReferenceY = record.terrainY;

            LayoutFile layout = LoadOfficialLayout();
            if (layout != null && layout.placeholders != null)
            {
                for (int i = 0; i < layout.placeholders.Length; i++)
                {
                    LayoutPlaceholderEntry p = layout.placeholders[i];
                    if (p == null) continue;
                    if (!string.Equals(p.buildingType, buildingType, StringComparison.OrdinalIgnoreCase)) continue;

                    record.buildingId = string.IsNullOrEmpty(p.id) ? record.buildingId : p.id;
                    record.x = p.position != null ? p.position.x : record.x;
                    record.z = p.position != null ? p.position.z : record.z;
                    record.rotation = p.rotation != null ? p.rotation.y : record.rotation;
                    if (p.scale != null && p.scale.x > 0f)
                    {
                        record.scaleX = p.scale.x;
                        record.scaleY = p.scale.x;
                    }
                    record.layoutReferenceY = p.position != null ? p.position.y : record.terrainY;
                    break;
                }
            }

            return record;
        }

        public static void SaveWithConfirmation(BuildingPlacementRecord record)
        {
            if (record == null) return;

            bool confirm = EditorUtility.DisplayDialog(
                "SAVE PLACEMENT",
                "Save " + record.buildingType + " placement?\n" +
                "  X=" + F(record.x, 3) + "\n" +
                "  TerrainY=" + F(record.terrainY, 3) + "\n" +
                "  Z=" + F(record.z, 3) + "\n" +
                "  Rotation=" + F(record.rotation, 2) + "\n" +
                "  Scale=" + F(record.scaleX, 3) + "\n\n" +
                "Writes to BuildingPlacementEditor_Saves.json (sidecar).\n" +
                "The OFFICIAL layout (BuildingPlaceholderLayout_FINAL) is NOT modified.",
                "SAVE PLACEMENT", "Cancel");

            if (!confirm) return;

            SavePlacement(record);
        }

        public static void SavePlacement(BuildingPlacementRecord record)
        {
            if (record == null)
            {
                Debug.Log("[BUILDING_PLACEMENT] SAVE_COMPLETED=false (record null)");
                return;
            }

            string path = SidecarPath;

            Debug.Log("[BUILDING_PLACEMENT] SAVE_CLICK");
            Debug.Log("[BUILDING_PLACEMENT] SAVE_PATH=" + path);
            Debug.Log("[BUILDING_PLACEMENT] SAVE_BUILDING=" + record.buildingType);
            Debug.Log("[BUILDING_PLACEMENT] SAVE_X=" + F(record.x, 3));
            Debug.Log("[BUILDING_PLACEMENT] SAVE_SCALE=" + F(record.scaleX, 3));

            PlacementSaveFile file = new PlacementSaveFile();

            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    PlacementSaveFile existing = JsonUtility.FromJson<PlacementSaveFile>(json);
                    if (existing != null && existing.placements != null) file = existing;
                }
                catch (Exception)
                {
                    file = new PlacementSaveFile();
                }
            }

            for (int i = 0; i < file.placements.Count; i++)
            {
                if (file.placements[i] != null &&
                    string.Equals(file.placements[i].buildingType, record.buildingType, StringComparison.OrdinalIgnoreCase))
                {
                    file.placements.RemoveAt(i);
                    break;
                }
            }

            file.placements.Add(new PlacementSaveEntry
            {
                buildingId = record.buildingId,
                buildingType = record.buildingType,
                X = record.x,
                TerrainY = record.terrainY,
                Z = record.z,
                Rotation = record.rotation,
                Scale = record.scaleX
            });

            string outJson = JsonUtility.ToJson(file, true);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, outJson);
            AssetDatabase.Refresh();

            Debug.Log("[BUILDING_PLACEMENT] SAVE_COMPLETED=true " +
                      record.buildingType + " @ X=" + F(record.x, 3) +
                      " TerrainY=" + F(record.terrainY, 3) + " Scale=" + F(record.scaleX, 3));
        }

        private static PlacementSaveEntry LoadSavedEntry(string buildingType)
        {
            string path = SidecarPath;
            if (!File.Exists(path)) return null;
            try
            {
                string json = File.ReadAllText(path);
                PlacementSaveFile file = JsonUtility.FromJson<PlacementSaveFile>(json);
                if (file == null || file.placements == null) return null;
                for (int i = 0; i < file.placements.Count; i++)
                {
                    if (file.placements[i] != null &&
                        string.Equals(file.placements[i].buildingType, buildingType, StringComparison.OrdinalIgnoreCase))
                    {
                        return file.placements[i];
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
            return null;
        }

        private static LayoutFile LoadOfficialLayout()
        {
            if (!File.Exists(OfficialLayoutPath)) return null;
            try
            {
                string json = File.ReadAllText(OfficialLayoutPath);
                return JsonUtility.FromJson<LayoutFile>(json);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string F(float v, int decimals)
        {
            return v.ToString("F" + decimals, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
#endif