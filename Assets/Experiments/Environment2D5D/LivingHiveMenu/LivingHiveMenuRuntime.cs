using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.LivingHiveMenu
{
    // BOOTSTRAP AUTONOME du menu inférieur LivingHive réimplanté en uGUI dans la scène
    // Environment2D5D_SpatialV3.
    //
    // Même stratégie que BuildingRuntimeViewBootstrap : aucun câblage dans la scène, un
    // RuntimeInitializeOnLoadMethod crée au runtime la racine du menu uniquement quand la
    // scène active est une scène 2.5D (préfixe "Environment2D5D"). La scène source
    // LivingHive et le monolithe HiveViewProductUiPresenter ne sont ni modifiés ni importés.
    public static class LivingHiveMenuRuntime
    {
        public const string RuntimeRootName = "LivingHive Menu Runtime";

        public static GameObject Root { get; private set; }
        public static LivingHiveMenuCanvas CanvasComponent { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (!Application.isPlaying) return;
            Scene active = SceneManager.GetActiveScene();
            if (!IsEnvironmentScene(active)) return;
            EnsureRuntime(active);
        }

        public static bool IsEnvironmentScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return false;
            if (scene.name.StartsWith("Environment2D5D", System.StringComparison.Ordinal)) return true;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root == null || root.name == null) continue;
                if (root.name.StartsWith("Environment2D5D", System.StringComparison.Ordinal)) return true;
            }
            return false;
        }

        public static void EnsureRuntime(Scene scene)
        {
            if (Root != null) return;
            Root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(Root, scene);
            CanvasComponent = Root.AddComponent<LivingHiveMenuCanvas>();
            CanvasComponent.Build();
        }

        public static void ResetRuntimeForProof()
        {
            if (Root != null)
            {
                if (Application.isPlaying) Object.Destroy(Root);
                else Object.DestroyImmediate(Root);
            }
            Root = null;
            CanvasComponent = null;
        }
    }
}