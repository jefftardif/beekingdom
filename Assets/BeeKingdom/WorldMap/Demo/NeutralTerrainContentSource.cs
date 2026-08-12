using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BeeKingdom.WorldMap
{
    // Source de contenu NEUTRE pour la scene de demonstration de la fondation.
    // Aucune regle de gameplay : chaque chunk rend une grille de placeholders plats
    // (couleurs par parite) pour visualiser le streaming. Les futurs systemes
    // remplaceront cette source par leur propre contenu via la meme interface.
    public sealed class NeutralTerrainContentSource : MonoBehaviour, IWorldChunkContentSource
    {
        [SerializeField] private int gridDivisions = 4;
        [SerializeField] private string parentName = "WorldChunkContent";

        private readonly Dictionary<ChunkCoordinate, List<GameObject>> rendered = new Dictionary<ChunkCoordinate, List<GameObject>>();
        private Transform contentParent;
        private Sprite whiteSprite;
        private Material spriteMaterial;

        public Task<WorldChunkContent> LoadAsync(ChunkCoordinate chunk, long chunkSize, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureResources();

            int divisions = Mathf.Max(1, gridDivisions);
            long cellSize = chunkSize / divisions;
            WorldPosition origin = WorldCoordinateSystem.ChunkOrigin(chunk, chunkSize);
            List<GameObject> created = new List<GameObject>(divisions * divisions);
            int parity = (int)((chunk.X & 1L) ^ (chunk.Y & 1L));
            Color baseColor = parity == 0
                ? new Color(0.16f, 0.27f, 0.17f, 1f)
                : new Color(0.13f, 0.22f, 0.15f, 1f);

            for (int ix = 0; ix < divisions; ix++)
            {
                for (int iy = 0; iy < divisions; iy++)
                {
                    GameObject cell = new GameObject("cell-" + ix + "-" + iy);
                    cell.transform.SetParent(contentParent, false);
                    SpriteRenderer renderer = cell.AddComponent<SpriteRenderer>();
                    renderer.sprite = whiteSprite;
                    renderer.material = spriteMaterial;
                    renderer.color = baseColor;
                    cell.transform.position = new Vector3(
                        origin.X + (ix + 0.5f) * cellSize,
                        origin.Y + (iy + 0.5f) * cellSize,
                        0f);
                    cell.transform.localScale = new Vector3(cellSize, cellSize, 1f);
                    created.Add(cell);
                }
            }

            rendered[chunk] = created;
            return Task.FromResult(new WorldChunkContent());
        }

        public void Unload(ChunkCoordinate chunk, WorldChunkContent content)
        {
            if (!rendered.TryGetValue(chunk, out List<GameObject> created))
            {
                return;
            }

            foreach (GameObject go in created)
            {
                if (go != null)
                {
                    Destroy(go);
                }
            }

            created.Clear();
            rendered.Remove(chunk);
        }

        private void EnsureResources()
        {
            if (whiteSprite == null)
            {
                Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                for (int y = 0; y < texture.height; y++)
                {
                    for (int x = 0; x < texture.width; x++)
                    {
                        texture.SetPixel(x, y, Color.white);
                    }
                }

                texture.Apply();
                whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 1f);
            }

            if (spriteMaterial == null)
            {
                spriteMaterial = new Material(Shader.Find("Sprites/Default"));
            }

            if (contentParent == null)
            {
                GameObject parent = GameObject.Find(parentName);
                if (parent == null)
                {
                    parent = new GameObject(parentName);
                }

                contentParent = parent.transform;
            }
        }
    }
}
