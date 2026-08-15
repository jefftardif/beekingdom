using UnityEngine;

namespace BeeKingdom.Experiments.Environment2D5D
{
    // BUILDING GROUND ANCHOR — explicit, immutable description of where a building's
    // footprint rests and how it relates to its LAYOUT position.
    //
    // The two MUST never be confused:
    //
    //   LayoutPosition   = (X, LayoutY, Z)   -> reference only, kept from the layout JSON.
    //   GroundContactPoint = (X, terrainY, Z) -> the TRANSFORM world position of the building.
    //
    // Rule: Y layout is NEVER used as the building foot height.
    public readonly struct BuildingGroundAnchor
    {
        public readonly float X;
        public readonly float TerrainY;
        public readonly float Z;

        public BuildingGroundAnchor(float x, float terrainY, float z)
        {
            X = x;
            TerrainY = terrainY;
            Z = z;
        }

        /// <summary>Resolves the anchor for a layout X using the unique GroundSurfaceResolver.</summary>
        public static BuildingGroundAnchor Resolve(float x)
        {
            return new BuildingGroundAnchor(x, GroundSurfaceResolver.TerrainYFromX(x), GroundSurfaceResolver.BuildingZ);
        }

        /// <summary>World position to assign to the building ROOT = the Ground Contact Point.
        /// The artwork base must map to local (0,0,0) of this transform.</summary>
        public Vector3 GroundContactPoint
        {
            get { return new Vector3(X, TerrainY, Z); }
        }

        /// <summary>World position of the LAYOUT reference for the SAME building (kept as-is).
        /// Only used for visualization/authoring, never for placement.</summary>
        public Vector3 LayoutPosition(float layoutY)
        {
            return new Vector3(X, layoutY, Z);
        }

        public override string ToString()
        {
            return "BuildingGroundAnchor(X=" + X.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)
                   + ", TerrainY=" + TerrainY.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)
                   + ", Z=" + Z.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + ")";
        }
    }
}