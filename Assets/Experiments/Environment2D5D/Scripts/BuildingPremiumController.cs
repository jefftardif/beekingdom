using UnityEngine;
using UnityEngine.InputSystem;

namespace BeeKingdom.Experiments.Environment2D5D
{
    // BUILDING TEST premium building controller (lab only, does not touch any validated
    // system): the building is glued to the BUILDING_TEST_ANCHOR world point — its ground
    // point follows the anchor's world-locked position every frame (read-only on the
    // anchor), so moving the anchor in MARKER mode moves the building exactly like a
    // world object. Height (Q/E) scales the building uniformly. The artwork quad is RIGID
    // 2D: fixed depth in front of the backdrop plane, identity rotation, no perspective.
    // The secondary debug UI (OnGUI, top-right) shows BUILDING TEST / anchor position /
    // building world position / scale.
    public class BuildingPremiumController : MonoBehaviour
    {
        [Header("References")]
        public AnchorMarker buildingAnchor;
        public Transform visualRoot;

        [Header("Controls")]
        public float minHeightScale = 0.6f;
        public float maxHeightScale = 1.6f;
        public float heightStep = 0.05f;
        public float rotationStep = 15f;

        [Header("State")]
        public float heightScale = 1f;

        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _titleStyle;

        private void Awake()
        {
            if (!visualRoot) visualRoot = transform;
        }

        private void LateUpdate()
        {
            if (!buildingAnchor) return;
            Vector3 p = buildingAnchor.transform.position;

            // RIGID 2D: the artwork quad keeps a FIXED depth slightly in front of the
            // backdrop plane. With the orthographic camera (parallel rays) depth has no
            // visual effect: no plane math, no perspective glue. In ortho the canvas is
            // a flat plane at BackdropZ; the building quad stands in front of it so the
            // painter's map stays world-fixed and the building is GLUED to its anchor
            // (world point on the map) — translation and uniform zoom only.
            visualRoot.position = new Vector3(p.x, p.y, AnchorMarker.BackdropZ - 0.05f);
            visualRoot.localScale = Vector3.one * heightScale;
            visualRoot.rotation = Quaternion.identity;
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null || !buildingAnchor || !buildingAnchor.IsSelected) return;

            if (kb.qKey.isPressed)
            {
                heightScale = Mathf.Max(minHeightScale, heightScale - heightStep * Time.deltaTime * 3f);
            }
            if (kb.eKey.isPressed)
            {
                heightScale = Mathf.Min(maxHeightScale, heightScale + heightStep * Time.deltaTime * 3f);
            }
        }

        private void OnGUI()
        {
            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.box);
                _boxStyle.padding = new RectOffset(10, 10, 8, 8);
                _labelStyle = new GUIStyle(GUI.skin.label);
                _labelStyle.fontSize = 12;
                _labelStyle.alignment = TextAnchor.MiddleLeft;
                _titleStyle = new GUIStyle(GUI.skin.label);
                _titleStyle.fontSize = 13;
                _titleStyle.fontStyle = FontStyle.Bold;
                _titleStyle.alignment = TextAnchor.MiddleCenter;
                _titleStyle.normal.textColor = new Color(1f, 0.75f, 0.35f);
            }

            float w = 430f;
            GUI.Box(new Rect(Screen.width - w - 15f, 10f, w, 190f), "", _boxStyle);

            float x = Screen.width - w - 5f;
            float y = 20f;

            GUI.Label(new Rect(x, y, w - 20f, 22f), "BUILDING TEST", _titleStyle);
            y += 26f;

            Vector3 a = buildingAnchor ? buildingAnchor.transform.position : Vector3.zero;
            Vector3 b = visualRoot ? visualRoot.position : Vector3.zero;

            GUI.Label(new Rect(x, y, w - 20f, 20f), $"Anchor: ({a.x:F2}, {a.y:F2}, {a.z:F2})", _labelStyle);
            y += 20f;
            GUI.Label(new Rect(x, y, w - 20f, 20f), $"Building world: ({b.x:F2}, {b.y:F2}, {b.z:F2})", _labelStyle);
            y += 20f;
            GUI.Label(new Rect(x, y, w - 20f, 20f), $"Scale: {heightScale:F2}x", _labelStyle);
            y += 26f;

            GUI.Label(new Rect(x, y, w - 20f, 40f),
                "B = select BUILDING anchor | WASD/X/Y = move anchor (MARKER mode)\nQ/E = height scale | wheel = zoom",
                _labelStyle);
        }
    }
}