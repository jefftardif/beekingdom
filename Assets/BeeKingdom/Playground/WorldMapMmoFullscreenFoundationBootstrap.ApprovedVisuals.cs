using UnityEngine;

namespace BeeKingdom.Playground
{
    public sealed partial class WorldMapMmoFullscreenFoundationBootstrap
    {
        // Keep original sprite assets and GUIDs intact. Missing approved artwork
        // falls back to the original texture without changing game state.
        private static Texture2D LoadApprovedWorldMapVisual(string originalPath)
        {
            string approvedPath;
            switch (originalPath)
            {
                case "WorldMapRuntimeEntitiesWave1/H1/hive_neutral_l1":
                    approvedPath = "WorldMapApprovedVisuals/hive-starter";
                    break;
                case "WorldMapRuntimeEntitiesWave6Premium/R2/resource_honey_medium":
                    approvedPath = "WorldMapApprovedVisuals/honey-medium";
                    break;
                case "WorldMapRuntimeEntitiesWave1/M1/beast_t2_shield_beetle":
                    approvedPath = "WorldMapApprovedVisuals/shield-beetle";
                    break;
                default:
                    return Resources.Load<Texture2D>(originalPath);
            }

            Texture2D approved = Resources.Load<Texture2D>(approvedPath);
            return approved != null ? approved : Resources.Load<Texture2D>(originalPath);
        }
    }
}
