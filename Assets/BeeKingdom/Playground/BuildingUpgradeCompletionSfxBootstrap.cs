using System;
using BeeKingdom.Audio;
using BeeKingdom.Gameplay.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground
{
    // M057-CL - Audio final : joue le SFX officiel de fin de chantier (upgrade.mp3, choisi et
    // installe par le CEO, voir AudioManager.PlayBuildingUpgradeComplete) exactement quand une
    // amelioration de batiment est REELLEMENT validee par le serveur - jamais au clic initial
    // sur un batiment AwaitingCompletion, jamais si la validation echoue ou si le serveur
    // rejette l'operation.
    //
    // Le seul signal authentique de succes est l'evenement BuildingCompleted, publie UNIQUEMENT
    // par HiveBuildingUpgradePanelController.CompleteCoreAsync (HiveBuildingUpgradePresentation.cs)
    // APRES une reponse serveur reussie - jamais sur les chemins d'erreur (HivePerimeterClientException,
    // OperationCanceledException, Exception generique), jamais au demarrage d'une amelioration,
    // et une seule fois par completion reelle (la boucle de retry ne republie qu'a la sortie
    // reussie). Ce bootstrap n'ajoute donc aucune logique de validation propre : il se contente
    // d'ecouter ce signal deja fiable.
    //
    // Generique par construction : BuildingCompleted transporte le BuildingId reel de
    // l'operation serveur, donc ce bootstrap fonctionne pour n'importe quel batiment sans liste
    // figee par nom - pas seulement celui utilise pendant les tests du CEO.
    //
    // Volontairement un simple abonnement GameEventBus plutot qu'un appel direct depuis le
    // controller de presentation : HiveBuildingUpgradePanelController est teste en EditMode sans
    // scene Unity (BuildingUpgradeFrameworkTests, HiveMapUpgradeProgressWiringTests...) et ne
    // doit donc jamais dependre d'AudioManager, qui est un singleton MonoBehaviour. AudioManager
    // reste la SEULE infrastructure audio utilisee ici (PlaySound -> sfxSource.PlayOneShot),
    // donc le volume/mute SFX existants s'appliquent sans aucun code parallele.
    //
    // Meme strategie d'auto-amorcage que les autres bootstraps Environment2D5D
    // (HiveMapBuildingUpgradeProgressBootstrap, etc.) : AutoStart() seul ne suffit PAS, il ne se
    // declenche qu'une fois sur la scene active au demarrage du Play Mode (jamais
    // "Environment2D5D*" en pratique) - troisieme fois que ce piege est documente dans ce
    // fichier (voir M038B-CL/M049C-CL/M059-CL dans HiveMapRuntimeBootstrapInitializer.cs), donc
    // InitializeForScene DOIT aussi etre cable dans l'installeur de production.
    public sealed class BuildingUpgradeCompletionSfxBootstrap : MonoBehaviour
    {
        private const string RuntimeRootName = "Building Upgrade Completion SFX Runtime";

        private GameEventSubscription subscription;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (!Application.isPlaying) return;
            Scene active = SceneManager.GetActiveScene();
            if (!IsEnvironmentScene(active)) return;
            if (FindFirstObjectByType<BuildingUpgradeCompletionSfxBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, active);
            root.AddComponent<BuildingUpgradeCompletionSfxBootstrap>();
        }

        private static bool IsEnvironmentScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return false;
            return scene.name.StartsWith("Environment2D5D", StringComparison.Ordinal);
        }

        public static void InitializeForScene(Scene scene)
        {
            if (!Application.isPlaying) return;
            if (!IsEnvironmentScene(scene)) return;
            if (FindFirstObjectByType<BuildingUpgradeCompletionSfxBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<BuildingUpgradeCompletionSfxBootstrap>();
        }

        private void Awake()
        {
            subscription = GameEventBus.Shared.Subscribe<BuildingCompleted>(OnBuildingCompleted);
        }

        private void OnDestroy()
        {
            subscription?.Dispose();
            subscription = null;
        }

        private void OnBuildingCompleted(BuildingCompleted eventData)
        {
            AudioManager.Instance?.PlayBuildingUpgradeComplete();
        }
    }
}
