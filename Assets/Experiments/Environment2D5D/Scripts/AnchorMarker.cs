using UnityEngine;

namespace BeeKingdom.Experiments.Environment2D5D
{
    public class AnchorMarker : MonoBehaviour
    {
        [Header("Identity")]
        public string markerId = "A";
        public string displayName = "MARKER";

        [Header("References")]
        public Transform labelTransform;
        public Transform cameraRef;
        public Transform surfaceMarker;

        [Header("Visual")]
        public float poleHeight = 6f;

        // Actual height of the backdrop plane (derived from the imported texture size, set
        // by the scene setup; used for the exact UV <-> world mapping).
        public float planeHeight = PlaneHeight;

        [SerializeField] private Vector3 _lockedPosition;

        private TextMesh _label;
        private Material _poleMaterial;
        private Color _baseColor;

        // The backdrop is a single flat vertical plane at BackdropZ carrying the untouched
        // image. The painted landscape has NO real depth: the surface IS the plane.
        // Mapping (bijective, matches PrototypeSceneSetup mesh): plane 100 x 60 world units,
        // u=0 left / u=1 right / v=0 bottom / v=1 top.
        public const float PlaneWidth = 100f;
        public const float PlaneHeight = 60f;
        public const float BackdropZ = 30f;

        public static Vector3 SurfacePointFromUV(Vector2 uv)
        {
            return new Vector3(uv.x * PlaneWidth - PlaneWidth * 0.5f, uv.y * PlaneHeight, BackdropZ);
        }

        public static Vector2 UVFromSurfacePoint(Vector3 p)
        {
            return new Vector2((p.x + PlaneWidth * 0.5f) / PlaneWidth, p.y / PlaneHeight);
        }

        public Vector2 SurfaceUV
        {
            get
            {
                Vector3 p = transform.position;
                return new Vector2((p.x + PlaneWidth * 0.5f) / PlaneWidth, p.y / Mathf.Max(0.0001f, planeHeight));
            }
        }

        public bool IsSelected { get; private set; }
        public Vector3 LockedWorldPosition => _lockedPosition;

        private void Awake()
        {
            if (_lockedPosition == Vector3.zero && transform.position.sqrMagnitude > 0.0001f)
            {
                LockToWorld(transform.position);
            }
        }

        public void Initialize(string id, string name, Color color, Material poleMaterial, TextMesh label, Transform cameraTransform)
        {
            markerId = id;
            displayName = name;
            _baseColor = color;
            _poleMaterial = poleMaterial;
            _label = label;
            cameraRef = cameraTransform;
            ApplyEmission(false);
        }

        public void LockToWorld(Vector3 position)
        {
            _lockedPosition = position;
            transform.position = position;
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            ApplyEmission(selected);
        }

        public void MoveLocal(Vector3 delta)
        {
            _lockedPosition += delta;
            transform.position = _lockedPosition;
        }

        public void RefreshLabel()
        {
            if (!_label) return;
            Vector3 p = transform.position;
            float distToBackdrop = cameraRef ? Mathf.Max(0.1f, Mathf.Abs(BackdropZ - cameraRef.position.z)) : 1f;
            float distToMarker = cameraRef ? Mathf.Max(0.1f, Mathf.Abs(p.z - cameraRef.position.z)) : 1f;
            float scalePct = distToMarker / distToBackdrop * 100f;
            _label.text = $"{displayName} [{markerId}]\n({p.x:F1}, {p.y:F1}, {p.z:F1})\nDepth Z: {p.z:F1} | Scale: {scalePct:F0}%";
        }

        private void ApplyEmission(bool selected)
        {
            if (!_poleMaterial) return;
            _poleMaterial.EnableKeyword("_EMISSION");
            _poleMaterial.SetColor("_EmissionColor", (selected ? new Color(1f, 1f, 0.25f) : _baseColor) * (selected ? 4f : 1.2f));
        }

        private void LateUpdate()
        {
            // WORLD LOCK: the anchor is created once at a fixed world position and stays
            // strictly immobile. If anything ever moves it, snap it back.
            if (transform.position != _lockedPosition)
            {
                transform.position = _lockedPosition;
            }

            // Label only billboards toward the camera (rotation only, never position).
            if (labelTransform && cameraRef)
            {
                labelTransform.rotation = cameraRef.rotation;
            }

            // Surface repère: a small crosshair GLUED to the painting at the anchor's base
            // point (on the backdrop surface). It is independent from this anchor's transform:
            // both always coincide on screen because both are world points of the SAME surface.
            // If the anchor ever leaves the surface, the repère stays on the painting and the
            // separation becomes visible (and Distance Anchor->Surface > 0 in the HUD).
            if (surfaceMarker)
            {
                Vector3 p = transform.position;
                surfaceMarker.position = new Vector3(p.x, p.y, BackdropZ + 0.03f);
                surfaceMarker.rotation = Quaternion.identity;
            }
        }
    }
}
