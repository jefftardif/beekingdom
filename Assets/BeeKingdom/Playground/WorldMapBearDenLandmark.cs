using System;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public sealed class WorldMapBearDenLandmark : IDisposable
    {
        public const string ResourcePath = "WorldMapWave5Runtime/Landmarks/BearDen/bear_den_dormant_v1";
        public const string ExpectedSourceSha256 = "316e172a341b4f56dfdc690adf416913d80fc377f9f8d788f69000d1f9a5fb8c";
        public const int AnchorRow = 5;
        public const int AnchorColumn = 2;
        public const float AnchorLocalX = 256f;
        public const float AnchorLocalY = 471f;
        public const float WorldWidth = 767.5f;
        public const float WorldHeight = 512f;
        public const float PivotX = 0.50f;
        public const float PivotY = 0.08f;
        public const float NoSpawnRadiusTiles = 0.85f;

        public Texture2D Texture { get; private set; }
        public bool IsLoaded => Texture != null;
        public bool IsVisible { get; private set; } = true;
        public bool BearVisible => false;
        public bool ActiveEvent => false;
        public bool RoadVisible => false;
        public Vector2 WorldAnchor => WorldMapWave5StreamingTileProvider.TileAnchorWorld(AnchorRow, AnchorColumn, AnchorLocalX, AnchorLocalY);
        public float NoSpawnRadiusWorld => NoSpawnRadiusTiles * WorldMapWave5StreamingTileProvider.TileSize;

        public Rect WorldRect
        {
            get
            {
                Vector2 anchor = WorldAnchor;
                return new Rect(
                    anchor.x - WorldWidth * PivotX,
                    anchor.y - WorldHeight * (1f - PivotY),
                    WorldWidth,
                    WorldHeight);
            }
        }

        public bool Load()
        {
            Texture = Resources.Load<Texture2D>(ResourcePath);
            if (Texture == null || Texture.width != 1535 || Texture.height != 1024)
            {
                if (Texture != null) Resources.UnloadAsset(Texture);
                Texture = null;
                Debug.LogWarning("[WorldMap Wave5] Bear Den landmark is missing or has unexpected dimensions.");
                return false;
            }

            Texture.wrapMode = TextureWrapMode.Clamp;
            Texture.filterMode = FilterMode.Bilinear;
            Texture.anisoLevel = 1;
            IsVisible = true;
            return true;
        }

        public bool ToggleVisibility()
        {
            IsVisible = !IsVisible;
            return IsVisible;
        }

        public void SetVisibility(bool visible)
        {
            IsVisible = visible;
        }

        public bool ExcludesSpawn(Vector2 worldPosition)
        {
            return Vector2.Distance(worldPosition, WorldAnchor) < NoSpawnRadiusWorld;
        }

        public void Dispose()
        {
            if (Texture != null) Resources.UnloadAsset(Texture);
            Texture = null;
        }
    }
}
