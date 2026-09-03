using BeeKingdom.Buildings.Interaction;
using BeeKingdom.Core.Integration;
using UnityEngine;

namespace BeeKingdom.LivingHiveMenu
{
    // HÔTE DE LA FENÊTRE Recherche (Local Preview) pour la scène Environment2D5D_SpatialV3.
    //
    // Implémente IBuildingWindowHost (BuildingWindowContract.cs) et s'enregistre dans
    // BuildingWindowRouter au play-mode via LivingHiveResearchRuntime. Le bâtiment
    // ResearchNode (BuildingTypes.Research) est le point d'entrée : son clic (event
    // BuildingSelectionService.BuildingClicked, L.36 de BuildingSelectionService.cs) ouvre la
    // fenêtre plein écran ; la fermeture (X, back, Échap) la masque et restaure la Ruche.
    //
    // Pendant l'ouverture, le Header et le menu inférieur (racine du port LivingHiveMenu,
    // LivingHiveMenuRuntime.Root) sont masqués par activation/désactivation (jamais
    // détruits) et restaurés à la fermeture — conformément à la mission.
    public sealed class LivingHiveResearchHost : IBuildingWindowHost
    {
        private readonly LivingHiveResearchWindow window;
        private ISelectionService selection;
        private GameObject hudRoot;
        private bool hudHidden;

        public LivingHiveResearchHost(LivingHiveResearchWindow window)
        {
            this.window = window;
            if (this.window != null) this.window.CloseRequested += OnWindowCloseRequested;
        }

        public LivingHiveResearchWindow Window => window;

        public bool IsOpen => window != null && window.IsOpen;

        // Racine du HUD à masquer pendant la fenêtre (Header + menu inférieur du port).
        public GameObject HudRoot
        {
            get { return hudRoot; }
            set
            {
                // Ne jamais recouvrir un état déjà masqué : si la fenêtre est ouverte, la
                // nouvelle racine doit rester masquée jusqu'à la fermeture.
                hudRoot = value;
                if (hudRoot != null && IsOpen && !hudHidden && hudRoot.activeSelf)
                {
                    hudRoot.SetActive(false);
                    hudHidden = true;
                }
            }
        }

        public bool IsHudHiddenForProof => hudHidden && hudRoot != null && !hudRoot.activeSelf;

        public void Register()
        {
            BuildingWindowRouter.Host = this;
        }

        public void Unregister()
        {
            if (window != null) window.CloseRequested -= OnWindowCloseRequested;
            if (BuildingWindowRouter.Host == this) BuildingWindowRouter.Host = null;
        }

        public void Attach(ISelectionService service)
        {
            Detach();
            selection = service;
            if (selection != null) selection.BuildingClicked += OnBuildingClicked;
        }

        public void Detach()
        {
            if (selection != null)
            {
                selection.BuildingClicked -= OnBuildingClicked;
                selection = null;
            }
        }

        private void OnBuildingClicked(BuildingDefinition building)
        {
            if (building == null) return;
            if (!string.Equals(building.BuildingType, BuildingTypes.Research, System.StringComparison.Ordinal)) return;
            // M040-CL: restored the conditional routing this bridge was already built for
            // (LivingHiveResearchBridge.IsOfficialAvailable/OpenOfficialOverlay - wired since
            // M038B but never called from here). The unconditional fallback below was a M016E-CL
            // workaround for a real Unity Editor freeze on opening the official overlay, traced
            // to a Unity-internal stall (EditorResources.Load during a GUISkin reload) most
            // likely caused by SentinelOne EDR intercepting Editor file I/O - being retested now
            // that a SentinelOne exclusion may be in place. If the freeze recurs, revert this and
            // restore the unconditional BuildingWindowRouter.TryOpen(building) call below.
            if (LivingHiveResearchBridge.IsOfficialAvailable)
            {
                LivingHiveResearchBridge.OpenOfficialOverlay();
                return;
            }
            BuildingWindowRouter.TryOpen(building);
        }

        // Implémentation IBuildingWindowHost.
        public void Open(BuildingWindowContext context)
        {
            if (window == null) return;
            window.Open();
            HideHud();
        }

        public void Close()
        {
            if (window == null) return;
            window.Hide();
            ShowHud();
        }

        private void HideHud()
        {
            if (hudRoot == null) return;
            if (hudRoot.activeSelf)
            {
                hudRoot.SetActive(false);
                hudHidden = true;
            }
        }

        private void ShowHud()
        {
            if (hudRoot == null) return;
            if (!hudRoot.activeSelf)
            {
                hudRoot.SetActive(true);
                hudHidden = false;
            }
            else
            {
                hudHidden = false;
            }
        }

        private void OnWindowCloseRequested(string via)
        {
            _ = via;
            ShowHud();
        }
    }
}
