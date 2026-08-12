using UnityEngine;

namespace BeeKingdom.Core
{
    /// <summary>
    /// Contrôle la caméra pour naviguer dans la ruche avec pan et zoom
    /// Version améliorée avec boundaries dynamiques basées sur le niveau de zoom
    /// </summary>
    public class HiveCameraController : MonoBehaviour
    {
        [Header("Zoom Settings")]
        [SerializeField] private float minZoom = 0.8f;
        [SerializeField] private float maxZoom = 1.3f;
        [SerializeField] private float zoomSpeed = 0.5f;
        [SerializeField] private float currentZoom = 1f;

        [Header("Pan Settings")]
        [SerializeField] private float panSpeed = 1f;
        [SerializeField] private bool invertPan = false;

        [Header("Boundaries - Dynamic (adjust with zoom)")]
        [SerializeField] private Vector2 baseBoundaryMin = new Vector2(-500f, -500f);
        [SerializeField] private Vector2 baseBoundaryMax = new Vector2(500f, 500f);
        [SerializeField] private bool useBoundaries = true;

        [Header("Smoothing")]
        [SerializeField] private float smoothTime = 0.1f;
        [SerializeField] private bool useSmoothing = true;

        [Header("References")]
        [SerializeField] private RectTransform hiveContainer;

        // Private variables for smooth movement
        private Vector3 targetPosition;
        private Vector3 velocity = Vector3.zero;

        // Touch/Mouse tracking
        private Vector3 lastInputPosition;
        private bool isDragging = false;

        private void Start()
        {
            if (hiveContainer == null)
            {
                Debug.LogError("⚠️ HiveContainer reference is missing!");
                return;
            }

            targetPosition = hiveContainer.localPosition;
        }

        private void Update()
        {
            if (hiveContainer == null) return;

            // Handle zoom (mouse wheel or pinch)
            HandleZoom();

            // Handle pan (mouse drag or touch drag)
            HandlePan();

            // Apply smooth movement
            ApplyMovement();
        }

        private void HandleZoom()
        {
            float zoomDelta = 0f;

            // Mouse wheel zoom
            zoomDelta = Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;

            // Touch pinch zoom
            if (Input.touchCount == 2)
            {
                Touch touch0 = Input.GetTouch(0);
                Touch touch1 = Input.GetTouch(1);

                Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
                Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;

                float prevMagnitude = (touch0PrevPos - touch1PrevPos).magnitude;
                float currentMagnitude = (touch0.position - touch1.position).magnitude;

                float difference = currentMagnitude - prevMagnitude;
                zoomDelta = difference * zoomSpeed * 0.01f;
            }

            // Apply zoom
            if (zoomDelta != 0f)
            {
                currentZoom = Mathf.Clamp(currentZoom + zoomDelta, minZoom, maxZoom);
                hiveContainer.localScale = Vector3.one * currentZoom;

                // Recalculate boundaries when zoom changes
                ClampPositionToBoundaries();
            }
        }

        private void HandlePan()
        {
            // Mouse input
            if (Input.GetMouseButtonDown(0))
            {
                lastInputPosition = Input.mousePosition;
                isDragging = true;
            }
            else if (Input.GetMouseButton(0) && isDragging)
            {
                Vector3 delta = Input.mousePosition - lastInputPosition;
                delta *= panSpeed;
                if (invertPan) delta = -delta;

                targetPosition += new Vector3(delta.x, delta.y, 0);
                lastInputPosition = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
            }

            // Touch input (single finger drag)
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Began)
                {
                    lastInputPosition = touch.position;
                    isDragging = true;
                }
                else if (touch.phase == TouchPhase.Moved && isDragging)
                {
                    Vector3 delta = (Vector3)touch.position - lastInputPosition;
                    delta *= panSpeed;
                    if (invertPan) delta = -delta;

                    targetPosition += new Vector3(delta.x, delta.y, 0);
                    lastInputPosition = touch.position;
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    isDragging = false;
                }
            }

            // Clamp to boundaries
            if (useBoundaries)
            {
                ClampPositionToBoundaries();
            }
        }

        private void ApplyMovement()
        {
            if (useSmoothing)
            {
                hiveContainer.localPosition = Vector3.SmoothDamp(
                    hiveContainer.localPosition,
                    targetPosition,
                    ref velocity,
                    smoothTime
                );
            }
            else
            {
                hiveContainer.localPosition = targetPosition;
            }
        }

        /// <summary>
        /// Calcule les boundaries dynamiques basées sur le niveau de zoom
        /// Plus on zoom, plus on peut panner (car on voit moins de l'image)
        /// Plus on dézoome, moins on peut panner (car on voit plus de l'image)
        /// </summary>
        private void ClampPositionToBoundaries()
        {
            // Calcul dynamique des boundaries selon le zoom
            // Formule : boundary = baseBoundary * currentZoom
            // - Si zoom = 1.0 → boundaries normales
            // - Si zoom = 0.8 (dézoom) → boundaries * 0.8 = plus restrictif (correct)
            // - Si zoom = 1.3 (zoom in) → boundaries * 1.3 = plus permissif (correct)

            Vector2 effectiveBoundaryMin = baseBoundaryMin * currentZoom;
            Vector2 effectiveBoundaryMax = baseBoundaryMax * currentZoom;

            targetPosition.x = Mathf.Clamp(targetPosition.x, effectiveBoundaryMin.x, effectiveBoundaryMax.x);
            targetPosition.y = Mathf.Clamp(targetPosition.y, effectiveBoundaryMin.y, effectiveBoundaryMax.y);
        }

        /// <summary>
        /// Définir le zoom par code
        /// </summary>
        public void SetZoom(float zoom)
        {
            currentZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
            hiveContainer.localScale = Vector3.one * currentZoom;
            ClampPositionToBoundaries();
        }

        /// <summary>
        /// Recentrer la caméra
        /// </summary>
        public void ResetPosition()
        {
            targetPosition = Vector3.zero;
            ClampPositionToBoundaries();
        }

        /// <summary>
        /// Obtenir le zoom actuel
        /// </summary>
        public float GetCurrentZoom()
        {
            return currentZoom;
        }
    }
}