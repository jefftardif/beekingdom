using System;
using UnityEngine;

namespace BeeKingdom.WorldMap
{
    // Racine d'orchestration de la carte du monde (composition root de la scene).
    // Monte la grille, le loader, le streamer, le pool, la selection, le LOD et la
    // sauvegarde ; fait avancer le streaming a chaque frame. Ne contient aucune
    // regle de gameplay et ne connait aucun autre systeme du jeu.
    public sealed class WorldManager : MonoBehaviour
    {
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private bool loadCameraPositionOnStart = true;

        private IWorldChunkContentSource contentSource;
        private WorldCameraController cameraController;
        private IWorldInputSource inputSource;
        private bool initialized;

        public WorldConfiguration Configuration { get; private set; }
        public WorldGrid Grid { get; private set; }
        public WorldChunkLoader Loader { get; private set; }
        public WorldStreamer Streamer { get; private set; }
        public WorldObjectPool ObjectPool { get; private set; }
        public WorldSelection Selection { get; private set; }
        public WorldLOD Lod { get; private set; }
        public WorldSave Save { get; private set; }
        public WorldCameraController Camera => cameraController;

        // Point d'extension : repere l'objet du monde sous une position monde
        // (picking). Nul par defaut : les futurs systemes le fourniront.
        public Func<WorldPosition, WorldObject> ObjectPicker { get; set; }

        public event Action<WorldPosition> WorldTap;

        protected void Awake()
        {
            if (autoInitialize)
            {
                Initialize(WorldConfiguration.CreateDefault());
            }
        }

        public void Initialize(WorldConfiguration configuration, IWorldChunkContentSource source = null)
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            contentSource = source ?? GetComponent<IWorldChunkContentSource>() ?? EmptyWorldChunkContentSource.Instance;
            cameraController = FindObjectOfType<WorldCameraController>();
            inputSource = cameraController != null ? cameraController.GetComponent<IWorldInputSource>() : null;

            Grid = new WorldGrid(configuration);
            Loader = new WorldChunkLoader(Grid, contentSource, configuration.Streaming);
            Streamer = new WorldStreamer(Grid, Loader, configuration.Streaming, FocusProvider());
            ObjectPool = new WorldObjectPool(configuration.Pool);
            Selection = new WorldSelection();
            Lod = new WorldLOD(configuration.Lod);
            Save = new WorldSave(new PlayerPrefsWorldMapSaveStore());

            if (cameraController != null)
            {
                cameraController.Initialize(configuration.Camera);
                if (loadCameraPositionOnStart)
                {
                    ApplySavedCameraState();
                }
            }

            WireInput();
        }

        protected void Update()
        {
            if (!initialized)
            {
                return;
            }

            Streamer.Tick();
        }

        protected void OnDestroy()
        {
            if (Streamer != null)
            {
                Streamer.UnloadAll();
            }
        }

        // Cree un objet du monde enregistre sur la grille (chunk cree a la demande,
        // charge ou non). La vue eventuelle viendra par ObjectPool dans les sprints
        // de contenu.
        public WorldObject SpawnObject(WorldObjectKind kind, WorldPosition position, string tag = null)
        {
            return Grid.RegisterObject(new WorldObject(WorldObjectId.New(), kind, position, tag));
        }

        public bool TryGetObject(WorldObjectId id, out WorldObject worldObject)
        {
            return Grid.TryGetObject(id, out worldObject);
        }

        private IWorldFocusProvider FocusProvider()
        {
            // La camera est le focus du streaming ; sans camera, focus fixe.
            if (cameraController != null)
            {
                return cameraController;
            }

            return new StaticFocusProvider();
        }

        private void ApplySavedCameraState()
        {
            if (!Save.TryLoad(out WorldMapSaveData data) || cameraController == null)
            {
                return;
            }

            float zoom = WorldCameraMath.ClampZoom(data.CameraZoom, Configuration.Camera);
            WorldVector2 position = WorldCameraMath.ClampPosition(data.CameraPosition, Configuration.Camera);
            cameraController.RestoreState(position, zoom);
        }

        private void WireInput()
        {
            WorldInputProcessor processor = inputSource != null
                ? new WorldInputProcessor(inputSource, new UnityClock(), Configuration.Camera)
                : null;
            if (processor == null)
            {
                return;
            }

            processor.Gesture += HandleGesture;
        }

        private void HandleGesture(WorldPointerGesture gesture)
        {
            if (gesture.Kind != WorldPointerGestureKind.Tap && gesture.Kind != WorldPointerGestureKind.DoubleTap)
            {
                return;
            }

            if (cameraController == null || inputSource == null)
            {
                return;
            }

            WorldVector2 worldPoint = WorldCameraMath.ScreenToWorld(gesture.ScreenPoint, inputSource.ScreenSize, cameraController.Position, cameraController.Zoom);
            WorldPosition worldPosition = new WorldPosition((long)Math.Round(worldPoint.X), (long)Math.Round(worldPoint.Y));
            WorldTap?.Invoke(worldPosition);

            if (ObjectPicker != null)
            {
                WorldObject picked = ObjectPicker(worldPosition);
                if (picked != null)
                {
                    Selection.Select(picked.Id);
                }
                else if (gesture.Kind == WorldPointerGestureKind.Tap)
                {
                    Selection.Clear();
                }
            }
        }

        private sealed class StaticFocusProvider : IWorldFocusProvider
        {
            public WorldPosition FocusPosition { get; } = new WorldPosition(0, 0);
        }

        private sealed class UnityClock : IWorldInputClock
        {
            public double NowSeconds => Time.realtimeSinceStartup;
        }
    }
}
