using UnityEngine;
using UnityEngine.InputSystem;

namespace BeeKingdom.Experiments.Environment2D5D
{
    // FRONTAL 2D BACKDROP (final architecture): the reference image is displayed flat,
    // perpendicular to the view — no perspective, no trapezoid, no tilt, no shear — and
    // yet it behaves like a real painting standing on the anchor plane (z = BackdropZ):
    // the quad has a FIXED world size equal to the plane (100 x planeH), so zoom, pan
    // and pitch move the image together with the anchors (they never detach).
    //
    // Architecture (user-mandated): VISUAL BACKDROP = frontal 2D image only;
    // ANCHOR / SURFACE = independent invisible world-space system. The anchors stay
    // world points ON the depth surface (z = 30) and are rendered by the same
    // perspective camera ON TOP of the image.
    //
    // Each frame the quad (child of the camera, identity rotation) is placed so that its
    // WORLD CENTER is pinned to the anchor plane center (0, planeH/2, BackdropZ): the
    // image is a fixed world painting. When the camera pans, the image and the anchors
    // move together on screen (anchors stay glued to their image elements). The quad
    // remains perpendicular to the view (billboard) so the image stays perfectly flat:
    // no perspective, no trapezoid, no pitch deformation. Zoom scales the image around
    // its own center like the anchors. Natural framing: at default distance the image
    // essentially fills the view; at close zoom the view is cropped by the painting's
    // frame; at far zoom-out the whole painting is visible with background around it.
    // Anchors' surface points project onto (almost) their exact image pixels
    // (pitch-dependent residual only, ~0 at pitch 0).
    //
    // Grid (debug, HIDDEN by default): X toggles a yellow rectangle + 25/50/75% lines
    // to verify that all four sides stay perfectly straight and parallel.
    public class FrontalBackdrop : MonoBehaviour
    {
        public Texture2D image;
        public Camera targetCamera;

        [Header("Grid (debug, hidden by default)")]
        public bool showGrid = false;
        public Color gridColor = new Color(1f, 0.9f, 0.2f, 0.75f);

        private float _imageAspect = AnchorMarker.PlaneWidth / AnchorMarker.PlaneHeight;

        private void Awake()
        {
            if (image && image.width > 0 && image.height > 0)
            {
                _imageAspect = (float)image.width / image.height;
            }
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.xKey.wasPressedThisFrame) showGrid = !showGrid;
        }

        private void LateUpdate()
        {
            if (!targetCamera) targetCamera = GetComponentInParent<Camera>();
            if (!targetCamera) return;

            Transform cam = targetCamera.transform;

            // FIXED WORLD SIZE = the anchor plane size (100 x planeH): the image behaves
            // like a real painting standing on the depth surface. It zooms, pans and
            // pitches TOGETHER with the anchors (a zoom moves the camera, the painting
            // grows on screen exactly like the anchors). No screen-glue: the image is
            // never resized to the frustum, so it never stays fixed while the anchors grow.
            // Natural consequences: at extreme close-up the view is cropped by the frame,
            // at far zoom-out the whole painting is visible with background around it.
            // planeH is computed from the RUNTIME texture dimensions (same value the
            // anchors' UV->world mapping uses), keeping the UV mapping exact.
            float planeH = AnchorMarker.PlaneWidth;
            if (image && image.height > 0 && image.width > 0)
            {
                planeH = AnchorMarker.PlaneWidth * ((float)image.height / image.width);
            }

            // PINNED WORLD POSITION = the anchor plane center (0, planeH/2, BackdropZ):
            // the image's world center never moves. When the camera PANS (WASD), the
            // image moves on screen EXACTLY like the anchors (both are fixed world
            // points at the same depth), so the anchors stay glued to their image
            // elements. The quad stays a camera child (billboard: perpendicular to the
            // view, local rotation identity) so the image remains perfectly rectangular,
            // frontal, flat — no perspective, no trapezoid, no pitch deformation. Zoom
            // scales the image around its own center, exactly like the anchors.
            Vector3 pinned = new Vector3(0f, planeH * 0.5f, AnchorMarker.BackdropZ);
            transform.localPosition = cam.InverseTransformPoint(pinned);
            transform.localScale = new Vector3(AnchorMarker.PlaneWidth, planeH, 1f);
        }

        private Rect ImageScreenRect()
        {
            float screenW = Screen.width;
            float screenH = Screen.height;
            float screenAspect = screenW / screenH;
            float w, h;
            if (screenAspect >= _imageAspect)
            {
                h = screenH;
                w = h * _imageAspect;
            }
            else
            {
                w = screenW;
                h = w / _imageAspect;
            }
            return new Rect((screenW - w) * 0.5f, (screenH - h) * 0.5f, w, h);
        }

        private void OnGUI()
        {
            if (!showGrid) return;

            Rect r = ImageScreenRect();
            GUI.color = gridColor;
            Texture2D tex = Texture2D.whiteTexture;
            const float border = 3f;

            // Rectangle around the image: 4 perfectly straight sides.
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, border), tex);
            GUI.DrawTexture(new Rect(r.x, r.y + r.height - border, r.width, border), tex);
            GUI.DrawTexture(new Rect(r.x, r.y, border, r.height), tex);
            GUI.DrawTexture(new Rect(r.x + r.width - border, r.y, border, r.height), tex);

            // Grid at 25/50/75%: must stay parallel to the frame (no trapezoid).
            for (int i = 1; i < 4; i++)
            {
                float gx = r.x + r.width * i / 4f;
                GUI.DrawTexture(new Rect(gx, r.y, 1f, r.height), tex);
            }
            for (int i = 1; i < 4; i++)
            {
                float gy = r.y + r.height * i / 4f;
                GUI.DrawTexture(new Rect(r.x, gy, r.width, 1f), tex);
            }

            GUI.color = Color.white;
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 12;
            style.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(Screen.width * 0.5f - 260f, Screen.height - 30f, 520f, 24f),
                "FRONTAL BACKDROP - image droite, entière, 2D (aucune perspective) | X = hide/show",
                style);
        }
    }
}
