using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Buildings.Interaction
{
    public interface IBuildingInteractionController
    {
        bool IsEnabled { get; set; }
        BuildingInteractionRegistry Registry { get; }
        ISelectionService Selection { get; }
        void Enable();
        void Disable();
    }

    public sealed class BuildingInteractionController : MonoBehaviour, IBuildingInteractionController
    {
        // Layer dédié exclusivement aux zones de clic des bâtiments runtime (voir
        // ProjectSettings/TagManager.asset). Le raycast d'interaction ne considère QUE ce
        // layer, donc le décor (WATER_*, TREES_01, MOUNTAIN_01...) ne peut jamais avaler
        // un clic avant le collider d'un bâtiment (ex: TREES_01 devant ROYAL_PALACE).
        public const int BuildingLayerIndex = 9;

        [SerializeField] private Camera _raycastCamera;
        [SerializeField] private bool _enabled = true;

        private readonly BuildingInteractionRegistry _registry = new BuildingInteractionRegistry();
        private readonly BuildingSelectionService _selection = new BuildingSelectionService();

        public bool IsEnabled
        {
            get { return _enabled; }
            set { _enabled = value; }
        }

        public BuildingInteractionRegistry Registry
        {
            get { return _registry; }
        }

        public ISelectionService Selection
        {
            get { return _selection; }
        }

        public void Enable()
        {
            _enabled = true;
        }

        public void Disable()
        {
            _enabled = false;
        }

        public void ConfigureCamera(Camera camera)
        {
            _raycastCamera = camera;
        }

        private Camera EffectiveCamera()
        {
            if (_raycastCamera != null) return _raycastCamera;
            return Camera.main;
        }

        public static int InteractionLayer
        {
            get
            {
                int layer = LayerMask.NameToLayer("BuildingInteraction");
                return layer >= 0 ? layer : BuildingLayerIndex;
            }
        }

        public static int InteractionLayerMask
        {
            get { return 1 << InteractionLayer; }
        }

        public void HandlePointer()
        {
            if (!_enabled) return;
            Camera camera = EffectiveCamera();
            if (camera == null || !Input.GetMouseButtonDown(0)) return;

            // uGUI overlays (the LivingHiveMenu rail/header, the Research full-screen
            // window, ...) draw on their own Canvas/GraphicRaycaster, independent of the
            // IMGUI-overlay boolean flags HiveMapOverlayInputGateBootstrap tracks - without
            // this check, a click meant for one of those uGUI panels also reaches the 3D
            // building underneath since this raycast never consulted the EventSystem at all.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, 500f, InteractionLayerMask, QueryTriggerInteraction.Ignore)) return;

            BuildingInteractionComponent interaction =
                hit.collider != null ? hit.collider.GetComponent<BuildingInteractionComponent>() : null;
            if (interaction == null) return;

            BuildingDefinition definition = interaction.Definition;
            if (definition == null) return;

            _selection.NotifyClicked(definition);
            _selection.Select(definition);
        }

        private void Update()
        {
            HandlePointer();
        }

        public static BuildingInteractionController FindOrCreate(Scene scene)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene active = SceneManager.GetSceneAt(i);
                if (active != scene) continue;
                foreach (GameObject root in active.GetRootGameObjects())
                {
                    BuildingInteractionController controller = root.GetComponentInChildren<BuildingInteractionController>(true);
                    if (controller != null) return controller;
                }
            }
            return null;
        }
    }
}