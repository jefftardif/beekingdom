using UnityEngine;
using UnityEngine.InputSystem;

namespace BeeKingdom.Experiments.Environment2D5D
{
    public class BuildingPerspectiveCamera : MonoBehaviour
    {
        [Header("View Rig")]
        public float fov = 55f;
        public float pitch = 12f;
        public float distance = 54f;
        public Vector2 anchor = new Vector2(0f, 30f);

        public const float BackdropZ = 30f;

        [Header("Presets (pitch / anchorY)")]
        public float lowPitch = 10f;
        public float lowAnchorY = 22f;
        public float mediumPitch = 25f;
        public float mediumAnchorY = 30f;
        public float highPitch = 40f;
        public float highAnchorY = 38f;
        public float defaultPitch = 12f;
        public float defaultAnchorY = 30f;
        public float defaultDistance = 54f;

        [Header("Speeds")]
        public float panSpeed = 20f;
        public float pitchSpeed = 25f;
        public float zoomStep = 3f;

        [Header("Limits")]
        public float minDistance = 8f;
        public float maxDistance = 220f;
        public float minPitch = -20f;
        public float maxPitch = 75f;
        public float minAnchorY = -20f;
        public float maxAnchorY = 80f;
        public float minAnchorX = -90f;
        public float maxAnchorX = 90f;
        public float maxCameraZ = 27f;

        [Header("References")]
        public Camera cameraComponent;

        [Header("Input")]
        public bool inputEnabled = true;

        private float _pitch;
        private float _distance;
        private Vector2 _anchor;

        public float CurrentPitch => _pitch;
        public float CurrentDistance => _distance;
        public float CurrentFOV => cameraComponent ? cameraComponent.fieldOfView : fov;
        public Vector2 CurrentAnchor => _anchor;

        private void Awake()
        {
            _pitch = defaultPitch;
            _distance = defaultDistance;
            _anchor = new Vector2(0f, defaultAnchorY);
            if (cameraComponent) cameraComponent.fieldOfView = fov;
        }

        private void Update()
        {
            if (inputEnabled) HandleInput();
            ApplyTransform();
        }

        private void HandleInput()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            float dt = Time.deltaTime;

            Vector2 pan = Vector2.zero;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) pan.x -= 1;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) pan.x += 1;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) pan.y += 1;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) pan.y -= 1;
            if (pan != Vector2.zero) _anchor += pan * panSpeed * dt;

            if (kb.rKey.isPressed) _pitch -= pitchSpeed * dt;
            if (kb.fKey.isPressed) _pitch += pitchSpeed * dt;

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f) _distance += Mathf.Sign(scroll) * zoomStep;
            }

            if (kb.digit1Key.wasPressedThisFrame) MoveToPreset(lowPitch, lowAnchorY);
            if (kb.digit2Key.wasPressedThisFrame) MoveToPreset(mediumPitch, mediumAnchorY);
            if (kb.digit3Key.wasPressedThisFrame) MoveToPreset(highPitch, highAnchorY);
            if (kb.digit0Key.wasPressedThisFrame) ResetView();
        }

        public void MoveToPreset(float presetPitch, float presetAnchorY)
        {
            _pitch = presetPitch;
            _anchor.y = presetAnchorY;
        }

        // Additive validation API (no behavior change): drives the SAME internal state
        // (anchor/pitch/distance) that Update() uses, so ApplyTransform keeps ownership
        // of the final transform and its clamps.
        public void ApplyPose(Vector2 anchorPos, float pitchDeg, float distanceUnits)
        {
            _anchor = anchorPos;
            _pitch = pitchDeg;
            _distance = distanceUnits;
        }

        public void GetPose(out Vector2 anchorPos, out float pitchDeg, out float distanceUnits)
        {
            anchorPos = _anchor;
            pitchDeg = _pitch;
            distanceUnits = _distance;
        }

        public void ResetView()
        {
            _pitch = defaultPitch;
            _distance = defaultDistance;
            _anchor = new Vector2(0f, defaultAnchorY);
        }

        private void ApplyTransform()
        {
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
            _distance = Mathf.Clamp(_distance, minDistance, maxDistance);
            _anchor.x = Mathf.Clamp(_anchor.x, minAnchorX, maxAnchorX);
            _anchor.y = Mathf.Clamp(_anchor.y, minAnchorY, maxAnchorY);

            // RIGID 2D CHASSIS (official architecture): ORTHOGRAPHIC projection,
            // camera rotation locked to identity. Parallel rays => zero perspective:
            // panning (WASD / ApplyPose) translates the map and the buildings on
            // screen PIXEL-IDENTICALLY (no size change, no tilt, no shear, no parallax);
            // zooming scales the whole frame by ONE uniform factor around the screen
            // center. distance maps to orthoSize (same vertical world-window as the
            // former fov55 perspective framing); pitch only re-centers the window
            // vertically; the camera never rotates.
            float rad = _pitch * Mathf.Deg2Rad;
            Vector3 aim = new Vector3(_anchor.x, _anchor.y, BackdropZ);
            Vector3 forward = new Vector3(0f, -Mathf.Sin(rad), Mathf.Cos(rad));
            Vector3 pos = aim - forward * _distance;
            pos.z = Mathf.Min(pos.z, maxCameraZ);
            transform.position = pos;
            transform.rotation = Quaternion.identity;

            if (cameraComponent)
            {
                cameraComponent.orthographic = true;
                cameraComponent.orthographicSize = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) * _distance;
            }
        }
    }
}
