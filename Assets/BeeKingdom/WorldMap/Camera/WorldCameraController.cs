using System;
using UnityEngine;

namespace BeeKingdom.WorldMap
{
    // Controleur de camera de la carte : deplacement fluide, inertie, zoom pivot,
    // limites configurables, recentrage. Le MonoBehaviour est une fine couche : toute
    // la physique vit dans WorldCameraMath, testable sans scene.
    public sealed class WorldCameraController : MonoBehaviour, IWorldFocusProvider
    {
        [SerializeField] private Camera targetCamera;

        private CameraSettings settings;
        private WorldCameraState state;
        private WorldInputProcessor input;
        private IWorldInputSource inputSource;
        private WorldVector2? recenterTarget;

        public CameraSettings Settings => settings;
        public WorldVector2 Position => state.Position;
        public float Zoom => state.Zoom;
        public WorldPosition FocusPosition => new WorldPosition((long)Math.Round(state.Position.X), (long)Math.Round(state.Position.Y));

        public event Action<WorldVector2> CameraPositionChanged;
        public event Action<float> CameraZoomChanged;

        // A appeler par le WorldManager (ou la scene) avec la configuration du monde.
        public void Initialize(CameraSettings cameraSettings)
        {
            if (cameraSettings == null)
            {
                throw new ArgumentNullException(nameof(cameraSettings));
            }

            settings = cameraSettings;
            state.Position = new WorldVector2(0d, 0d);
            state.Zoom = cameraSettings.ZoomMin;
            state.Velocity = new WorldVector2(0d, 0d);
            ApplyToCamera();
        }

        public void RecenterHome()
        {
            recenterTarget = new WorldVector2(0d, 0d);
        }

        // Restaure un etat sauvegarde (position + zoom), sans inertie.
        public void RestoreState(WorldVector2 position, float zoom)
        {
            if (settings == null)
            {
                return;
            }

            state.Position = WorldCameraMath.ClampPosition(position, settings);
            state.Zoom = WorldCameraMath.ClampZoom(zoom, settings);
            state.Velocity = new WorldVector2(0d, 0d);
            ApplyToCamera();
        }

        public void Recenter(WorldPosition target)
        {
            recenterTarget = new WorldVector2(target.X, target.Y);
        }

        public void CancelRecenter()
        {
            recenterTarget = null;
        }

        protected void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }
        }

        protected void OnEnable()
        {
            if (settings == null)
            {
                return;
            }

            inputSource = GetComponent<IWorldInputSource>();
            if (inputSource != null)
            {
                input = new WorldInputProcessor(inputSource, new UnityWorldInputClock(), settings);
                input.Gesture += HandleGesture;
            }
        }

        protected void OnDisable()
        {
            if (input != null)
            {
                input.Gesture -= HandleGesture;
                input = null;
            }

            inputSource = null;
        }

        protected void Update()
        {
            if (settings == null)
            {
                return;
            }

            float deltaSeconds = Time.deltaTime;

            if (input != null)
            {
                input.Process();
            }

            if (inputSource != null)
            {
                ApplyKeyboardMove(deltaSeconds);
            }

            if (recenterTarget.HasValue)
            {
                double factor = 1d - Math.Exp(-deltaSeconds / settings.RecenterDuration);
                state.Position = new WorldVector2(
                    state.Position.X + (recenterTarget.Value.X - state.Position.X) * factor,
                    state.Position.Y + (recenterTarget.Value.Y - state.Position.Y) * factor);
                state.Velocity = new WorldVector2(0d, 0d);
                if (WorldCameraMath.Magnitude(recenterTarget.Value - state.Position) < 0.01d)
                {
                    state.Position = recenterTarget.Value;
                    recenterTarget = null;
                }
            }
            else
            {
                state.Position = new WorldVector2(
                    state.Position.X + state.Velocity.X * deltaSeconds,
                    state.Position.Y + state.Velocity.Y * deltaSeconds);
                state.Velocity = WorldCameraMath.DecayVelocity(state.Velocity, settings.DecelerationTime, deltaSeconds);
            }

            state.Position = WorldCameraMath.ClampPosition(state.Position, settings);
            state.Zoom = WorldCameraMath.ClampZoom(state.Zoom, settings);
            ApplyToCamera();
        }

        // Zoom pivot : le point monde sous le pointeur reste stable.
        public void ApplyZoom(float zoomFactor, Vector2 pivotScreen)
        {
            if (settings == null || zoomFactor <= 0f)
            {
                return;
            }

            float targetZoom = WorldCameraMath.ClampZoom(state.Zoom * zoomFactor, settings);
            if (Mathf.Approximately(targetZoom, state.Zoom))
            {
                return;
            }

            Vector2 screenSize = inputSource != null ? inputSource.ScreenSize : new Vector2(Screen.width, Screen.height);
            WorldVector2 pivotWorld = WorldCameraMath.ScreenToWorld(pivotScreen, screenSize, state.Position, state.Zoom);
            state.Position = WorldCameraMath.ZoomAboutPivot(state.Position, state.Zoom, targetZoom, pivotWorld);
            state.Zoom = targetZoom;
            CameraZoomChanged?.Invoke(state.Zoom);
        }

        private void ApplyKeyboardMove(float deltaSeconds)
        {
            if (!inputSource.MoveLeft && !inputSource.MoveRight && !inputSource.MoveUp && !inputSource.MoveDown)
            {
                return;
            }

            double dx = 0d;
            double dy = 0d;
            if (inputSource.MoveLeft) dx -= settings.MoveSpeed;
            if (inputSource.MoveRight) dx += settings.MoveSpeed;
            if (inputSource.MoveUp) dy += settings.MoveSpeed;
            if (inputSource.MoveDown) dy -= settings.MoveSpeed;
            state.Position = new WorldVector2(state.Position.X + dx * deltaSeconds, state.Position.Y + dy * deltaSeconds);
            state.Velocity = new WorldVector2(0d, 0d);
        }

        private void HandleGesture(WorldPointerGesture gesture)
        {
            switch (gesture.Kind)
            {
                case WorldPointerGestureKind.DragStart:
                    CancelRecenter();
                    state.Velocity = new WorldVector2(0d, 0d);
                    break;
                case WorldPointerGestureKind.Drag:
                    state.Position = new WorldVector2(
                        state.Position.X - gesture.ScreenDelta.x / (inputSource.ScreenSize.y * 0.5) * state.Zoom,
                        state.Position.Y - gesture.ScreenDelta.y / (inputSource.ScreenSize.y * 0.5) * state.Zoom);
                    break;
                case WorldPointerGestureKind.Zoom:
                    ApplyZoom(gesture.ZoomFactor, gesture.ScreenPoint);
                    break;
            }
        }

        private void ApplyToCamera()
        {
            if (targetCamera == null)
            {
                return;
            }

            targetCamera.orthographicSize = state.Zoom;
            targetCamera.transform.position = new Vector3((float)state.Position.X, (float)state.Position.Y, targetCamera.transform.position.z);
            CameraPositionChanged?.Invoke(state.Position);
        }

        private sealed class UnityWorldInputClock : IWorldInputClock
        {
            public double NowSeconds => Time.realtimeSinceStartup;
        }
    }
}
