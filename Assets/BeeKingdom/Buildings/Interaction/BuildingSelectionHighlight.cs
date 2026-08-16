using UnityEngine;

namespace BeeKingdom.Buildings.Interaction
{
    public interface IBuildingVisualFeedback
    {
        void Show(BuildingDefinition definition, GameObject target);
        void Hide();
        bool IsShowing { get; }
    }

    // Détourage de sélection suivant la silhouette alpha de l'artwork.
    //
    // Réutilise l'approche MOVE (BuildingPlacementEditor) : le bâtiment runtime est une
    // quad texturée par le PNG matérialisé (ArtworkUnlit, alpha). Pour le feedback de
    // sélection on CLONE cette quad (même MeshFilter, même texture) et on l'affiche avec
    // le shader BeeKingdom/Experiments/ArtworkOutline qui n'émet que les texels voisins
    // d'un pixel transparent : l'écran montre un contour lumineux épousant exactement la
    // silhouette, jamais un rectangle.
    public sealed class BuildingSelectionHighlight : MonoBehaviour, IBuildingVisualFeedback
    {
        private const string OverlayName = "SelectionOverlay";
        private const string OutlineShaderName = "BeeKingdom/Experiments/ArtworkOutline";
        private const float AlphaCutoff = 8f / 255f;
        private const float OutlineWidthTexels = 2f;

        private static readonly Color HighlightColor = new Color(1f, 0.86f, 0.3f, 1f);

        private GameObject _overlay;
        private Material _material;

        public bool IsShowing
        {
            get { return _overlay != null; }
        }

        public void Show(BuildingDefinition definition, GameObject target)
        {
            Hide();
            if (definition == null || target == null) return;

            Renderer artwork = target.GetComponentInChildren<MeshRenderer>(true);
            MeshFilter artworkMesh = artwork != null ? artwork.GetComponent<MeshFilter>() : null;
            Texture artworkTexture = artwork != null && artwork.sharedMaterial != null
                ? artwork.sharedMaterial.mainTexture
                : null;

            if (artworkMesh != null && artworkMesh.sharedMesh != null && artworkTexture != null)
            {
                CreateSilhouetteOutline(artworkMesh.sharedMesh, artworkTexture, artwork.transform);
            }
            else
            {
                CreateDevFallback(target.transform);
            }
        }

        private void CreateSilhouetteOutline(Mesh mesh, Texture texture, Transform artworkTransform)
        {
            // Hôte = le "Visual" (parent de la quad d'artwork) : même position, rotation et
            // échelle que l'art matérialisé => la silhouette du contour coïncide au pixel près.
            Transform host = artworkTransform.parent != null ? artworkTransform.parent : artworkTransform;

            _overlay = new GameObject(OverlayName);
            _overlay.hideFlags = HideFlags.HideAndDontSave;
            _overlay.transform.SetParent(host, false);
            _overlay.transform.localPosition = new Vector3(0f, 0f, 0.05f);
            _overlay.transform.localRotation = Quaternion.identity;
            _overlay.transform.localScale = Vector3.one;

            _overlay.AddComponent<MeshFilter>().sharedMesh = mesh;

            MeshRenderer renderer = _overlay.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = GetOutlineMaterial(texture);
            renderer.enabled = true;
        }

        private void CreateDevFallback(Transform parent)
        {
            // Repli uniquement (tests / artwork absent) : quad doré au centre de la cible.
            _overlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _overlay.name = OverlayName;
            _overlay.hideFlags = HideFlags.HideAndDontSave;
            _overlay.transform.SetParent(parent, false);
            _overlay.transform.localPosition = Vector3.zero;

            Renderer renderer = _overlay.GetComponent<Renderer>();
            renderer.sharedMaterial = GetOutlineMaterial(null);
            renderer.enabled = true;
        }

        private Material GetOutlineMaterial(Texture texture)
        {
            if (_material == null)
            {
                Shader shader = Shader.Find(OutlineShaderName);
                _material = new Material(shader != null ? shader : Shader.Find("Unlit/Color"));
                _material.name = "SelectionOverlayMaterial";
                if (_material.HasProperty("_AlphaCutoff")) _material.SetFloat("_AlphaCutoff", AlphaCutoff);
                if (_material.HasProperty("_OutlineWidth")) _material.SetFloat("_OutlineWidth", OutlineWidthTexels);
            }
            _material.mainTexture = texture;
            _material.color = HighlightColor;
            _material.renderQueue = 3001;
            return _material;
        }

        public void Hide()
        {
            if (_overlay != null)
            {
                if (Application.isPlaying) Destroy(_overlay);
                else DestroyImmediate(_overlay);
                _overlay = null;
            }
        }

        private void OnDestroy()
        {
            Hide();
        }
    }
}