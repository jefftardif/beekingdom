using BeeKingdom.Buildings.Interaction;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.LivingHiveMenu
{
    // BOOTSTRAP AUTONOME de la fenêtre Recherche plein écran (Local Preview) pour la scène
    // Environment2D5D_SpatialV3.
    //
    // Même stratégie que LivingHiveMenuRuntime / BuildingRuntimeViewBootstrap : aucun câblage
    // dans la scène, un RuntimeInitializeOnLoadMethod crée au runtime la fenêtre + l'hôte,
    // enregistre l'hôte dans BuildingWindowRouter et le branche sur les clics de bâtiment
    // (BuildingSelectionService.BuildingClicked) UNIQUEMENT quand la scène active est une
    // scène 2.5D (préfixe "Environment2D5D"). La scène LivingHive et le monolithe ne sont ni
    // modifiés ni importés.
    public static class LivingHiveResearchRuntime
    {
        public const string RuntimeRootName = "LivingHive Research Runtime";

        public static GameObject Root { get; private set; }
        public static LivingHiveResearchWindow Window { get; private set; }
        public static LivingHiveResearchHost Host { get; private set; }
        public static bool IsModalOpen => Window != null && Window.IsOpen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (!Application.isPlaying) return;
            Scene active = SceneManager.GetActiveScene();
            if (!LivingHiveMenuRuntime.IsEnvironmentScene(active)) return;
            EnsureRuntime(active);
        }

        public static void EnsureRuntime(Scene scene)
        {
            if (Root != null) return;
            Root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(Root, scene);
            Window = Root.AddComponent<LivingHiveResearchWindow>();
            Window.Build();
            Host = new LivingHiveResearchHost(Window);
            Host.HudRoot = LivingHiveMenuRuntime.Root;
            Host.Register();

            // Clic bâtiment -> ouverture Recherche. Le contrôleur réutilise le
            // BuildingInteractionController existant (créé par BuildingRuntimeViewBootstrap).
            BuildingInteractionController controller = BuildingInteractionController.FindOrCreate(scene);
            if (controller != null) Host.Attach(controller.Selection);
        }

        public static void ResetRuntimeForProof()
        {
            if (Host != null) Host.Unregister();
            Host = null;
            if (Root != null)
            {
                if (Application.isPlaying) Object.Destroy(Root);
                else Object.DestroyImmediate(Root);
            }
            Root = null;
            Window = null;
        }
    }
}
