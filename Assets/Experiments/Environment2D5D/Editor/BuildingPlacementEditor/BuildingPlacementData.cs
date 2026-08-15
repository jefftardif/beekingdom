#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using BeeKingdom.Experiments.Environment2D5D;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Experiments.Environment2D5D.EditorTools.BuildingPlacement
{
    [Serializable]
    public sealed class BuildingPlacementRecord
    {
        public string buildingId = "BUILDING_13";
        public string buildingType = "ROYAL_PALACE";
        public float x;
        public float terrainY;
        public float z = GroundSurfaceResolver.BuildingZ;
        public float rotation;
        public float scaleX = 1f;
        public float scaleY = 1f;
        public float layoutReferenceY;

        public BuildingPlacementRecord Clone()
        {
            return new BuildingPlacementRecord
            {
                buildingId = buildingId,
                buildingType = buildingType,
                x = x,
                terrainY = terrainY,
                z = z,
                rotation = rotation,
                scaleX = scaleX,
                scaleY = scaleY,
                layoutReferenceY = layoutReferenceY
            };
        }

        public bool SameAs(BuildingPlacementRecord other)
        {
            if (other == null) return false;
            return Mathf.Approximately(x, other.x)
                && Mathf.Approximately(terrainY, other.terrainY)
                && Mathf.Approximately(z, other.z)
                && Mathf.Approximately(rotation, other.rotation)
                && Mathf.Approximately(scaleX, other.scaleX)
                && Mathf.Approximately(scaleY, other.scaleY);
        }
    }

    public sealed class BuildingCatalogEntry
    {
        public string buildingType;
        public string displayName;
        public string artworkPath;
    }

    public static class BuildingCatalog
    {
        private const string ArtRoot = "Assets/BeeKingdom/Art/Buildings";

        public static readonly BuildingCatalogEntry[] Entries =
        {
            New("NURSERY", "NURSERY_001.png"),
            New("BARRACK", "BARRACK_001.png"),
            New("HONEY_RESERVE", "HONEY_RESERVE_001.png"),
            New("DEFENSE", "DEFENSE_001.png"),
            New("GENETICS", "GENETICS_001.png"),
            New("RESEARCH", "RESEARCH_001.png"),
            New("WAREHOUSE", "WAREHOUSE_001.png"),
            New("TRANSFORMATION", "TRANSFORMATION_001.png"),
            New("INFIRMARY", "INFIRMARY_001.png"),
            New("ALLIANCE_CENTER", "ALLIANCE_CENTER_001.png"),
            New("ACADEMY", "ACADEMY_001.png"),
            New("BANK", "BANK_001.png"),
            New("ROYAL_PALACE", "ROYAL_PALACE.png"),
            New("CHAMPION_HALL", "CHAMPION_HALL_001.png")
        };

        private static BuildingCatalogEntry New(string type, string file)
        {
            return new BuildingCatalogEntry
            {
                buildingType = type,
                displayName = type,
                artworkPath = ArtRoot + "/" + file
            };
        }

        public static BuildingCatalogEntry Find(string buildingType)
        {
            for (int i = 0; i < Entries.Length; i++)
            {
                if (Entries[i].buildingType == buildingType) return Entries[i];
            }
            return null;
        }

        public static int IndexOf(string buildingType)
        {
            for (int i = 0; i < Entries.Length; i++)
            {
                if (Entries[i].buildingType == buildingType) return i;
            }
            return 0;
        }
    }

    public struct ArtworkScan
    {
        public int width;
        public int height;
        public int contactX;
        public int contactYFromTop;
        public int opaqueMinX;
        public int opaqueMaxX;
        public int opaqueMinYFromTop;
        public int opaqueMaxYFromTop;
        public float contactU;
        public float contactV;
        public float opaqueUMin;
        public float opaqueUMax;
        public float opaqueVMin;
        public float opaqueVMax;

        public bool Valid { get { return width > 0 && height > 0; } }

        public float Aspect
        {
            get { return height > 0 ? (float)width / height : 1f; }
        }
    }

    public static class BuildingArtworkScanner
    {
        private const float AlphaThreshold = 8f / 255f;
        private static readonly Dictionary<string, ArtworkScan> _cache = new Dictionary<string, ArtworkScan>();

        private static readonly Dictionary<string, ArtworkScan> ValidatedOverrides =
            new Dictionary<string, ArtworkScan>
            {
                {
                    "Assets/BeeKingdom/Art/Buildings/ROYAL_PALACE.png",
                    new ArtworkScan
                    {
                        width = 1536,
                        height = 1024,
                        contactX = 650,
                        contactYFromTop = 1021,
                        contactU = 0.4231770833333333f,
                        contactV = 0.0029296875f
                    }
                }
            };

        public static ArtworkScan Scan(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return default(ArtworkScan);
            if (_cache.TryGetValue(assetPath, out ArtworkScan cached)) return cached;

            ArtworkScan scan = ScanImpl(assetPath);
            _cache[assetPath] = scan;
            return scan;
        }

        private static ArtworkScan ScanImpl(string assetPath)
        {
            if (ValidatedOverrides.TryGetValue(assetPath, out ArtworkScan overrides))
            {
                ArtworkScan withBounds = ResolveOpaqueBounds(assetPath);
                if (withBounds.Valid)
                {
                    overrides.opaqueMinX = withBounds.opaqueMinX;
                    overrides.opaqueMaxX = withBounds.opaqueMaxX;
                    overrides.opaqueMinYFromTop = withBounds.opaqueMinYFromTop;
                    overrides.opaqueMaxYFromTop = withBounds.opaqueMaxYFromTop;
                    overrides.opaqueUMin = withBounds.opaqueUMin;
                    overrides.opaqueUMax = withBounds.opaqueUMax;
                    overrides.opaqueVMin = withBounds.opaqueVMin;
                    overrides.opaqueVMax = withBounds.opaqueVMax;
                }
                return overrides;
            }

            ArtworkScan scan = ResolveOpaqueBounds(assetPath);
            if (!scan.Valid) return scan;

            scan.contactYFromTop = scan.opaqueMaxYFromTop;
            scan.contactX = (scan.opaqueMinX + scan.opaqueMaxX) / 2;
            if (scan.contactX <= 0) scan.contactX = scan.width / 2;

            scan.contactU = (float)scan.contactX / scan.width;
            scan.contactV = 1f - (float)scan.contactYFromTop / scan.height;

            return scan;
        }

        private static ArtworkScan ResolveOpaqueBounds(string assetPath)
        {
            ArtworkScan scan = default(ArtworkScan);
            if (!File.Exists(assetPath)) return scan;

            byte[] bytes = null;
            try
            {
                bytes = File.ReadAllBytes(assetPath);
            }
            catch (Exception)
            {
                return scan;
            }

            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(tex, bytes) || tex.width <= 0 || tex.height <= 0)
            {
                UnityEngine.Object.DestroyImmediate(tex);
                return scan;
            }

            scan.width = tex.width;
            scan.height = tex.height;

            Color32[] pixels = tex.GetPixels32();
            bool opaque = false;
            for (int y = tex.height - 1; y >= 0; y--)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    if (pixels[y * tex.width + x].a >= AlphaThreshold)
                    {
                        if (x < scan.opaqueMinX) scan.opaqueMinX = x;
                        if (x > scan.opaqueMaxX) scan.opaqueMaxX = x;
                        if (y < scan.opaqueMinYFromTop) scan.opaqueMinYFromTop = y;
                        if (y > scan.opaqueMaxYFromTop) scan.opaqueMaxYFromTop = y;
                        if (!opaque) opaque = true;
                    }
                }
            }

            UnityEngine.Object.DestroyImmediate(tex);

            if (!opaque) return scan;

            scan.opaqueUMin = (float)scan.opaqueMinX / scan.width;
            scan.opaqueUMax = (float)scan.opaqueMaxX / scan.width;
            scan.opaqueVMax = 1f - (float)scan.opaqueMinYFromTop / scan.height;
            scan.opaqueVMin = 1f - (float)scan.opaqueMaxYFromTop / scan.height;

            return scan;
        }
    }
}
#endif
