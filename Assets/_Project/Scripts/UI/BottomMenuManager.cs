using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace BeeKingdom.UI
{
    /// <summary>
    /// Gestionnaire du menu inférieur avec onglets (style Ant Legion)
    /// Gère la navigation entre différentes sections du jeu
    /// </summary>
    public class BottomMenuManager : MonoBehaviour
    {
        [Header("Menu Panels")]
        [SerializeField] private GameObject buildingsPanel;
        [SerializeField] private GameObject beesPanel;
        [SerializeField] private GameObject questsPanel;
        [SerializeField] private GameObject researchPanel;
        [SerializeField] private GameObject guildPanel;
        [SerializeField] private GameObject morePanel;

        [Header("Menu Buttons")]
        [SerializeField] private BottomMenuButton buildingsButton;
        [SerializeField] private BottomMenuButton beesButton;
        [SerializeField] private BottomMenuButton questsButton;
        [SerializeField] private BottomMenuButton researchButton;
        [SerializeField] private BottomMenuButton guildButton;
        [SerializeField] private BottomMenuButton moreButton;

        private List<GameObject> allPanels;
        private List<BottomMenuButton> allButtons;
        private MenuTab currentTab = MenuTab.Buildings;

        public enum MenuTab
        {
            Buildings,
            Bees,
            Quests,
            Research,
            Guild,
            More
        }

        private void Awake()
        {
            // Initialiser les listes
            allPanels = new List<GameObject>
            {
                buildingsPanel,
                beesPanel,
                questsPanel,
                researchPanel,
                guildPanel,
                morePanel
            };

            allButtons = new List<BottomMenuButton>
            {
                buildingsButton,
                beesButton,
                questsButton,
                researchButton,
                guildButton,
                moreButton
            };

            // Assigner les callbacks
            if (buildingsButton != null) buildingsButton.OnButtonClicked += () => ShowTab(MenuTab.Buildings);
            if (beesButton != null) beesButton.OnButtonClicked += () => ShowTab(MenuTab.Bees);
            if (questsButton != null) questsButton.OnButtonClicked += () => ShowTab(MenuTab.Quests);
            if (researchButton != null) researchButton.OnButtonClicked += () => ShowTab(MenuTab.Research);
            if (guildButton != null) guildButton.OnButtonClicked += () => ShowTab(MenuTab.Guild);
            if (moreButton != null) moreButton.OnButtonClicked += () => ShowTab(MenuTab.More);
        }

        private void Start()
        {
            // Afficher le premier onglet par défaut
            ShowTab(MenuTab.Buildings);
        }

        /// <summary>
        /// Afficher un onglet spécifique
        /// </summary>
        public void ShowTab(MenuTab tab)
        {
            currentTab = tab;

            // Désactiver tous les panels
            foreach (var panel in allPanels)
            {
                if (panel != null)
                {
                    panel.SetActive(false);
                }
            }

            // Désélectionner tous les boutons
            foreach (var button in allButtons)
            {
                if (button != null)
                {
                    button.SetSelected(false);
                }
            }

            // Activer le panel et bouton correspondant
            switch (tab)
            {
                case MenuTab.Buildings:
                    if (buildingsPanel != null) buildingsPanel.SetActive(true);
                    if (buildingsButton != null) buildingsButton.SetSelected(true);
                    break;

                case MenuTab.Bees:
                    if (beesPanel != null) beesPanel.SetActive(true);
                    if (beesButton != null) beesButton.SetSelected(true);
                    break;

                case MenuTab.Quests:
                    if (questsPanel != null) questsPanel.SetActive(true);
                    if (questsButton != null) questsButton.SetSelected(true);
                    break;

                case MenuTab.Research:
                    if (researchPanel != null) researchPanel.SetActive(true);
                    if (researchButton != null) researchButton.SetSelected(true);
                    break;

                case MenuTab.Guild:
                    if (guildPanel != null) guildPanel.SetActive(true);
                    if (guildButton != null) guildButton.SetSelected(true);
                    break;

                case MenuTab.More:
                    if (morePanel != null) morePanel.SetActive(true);
                    if (moreButton != null) moreButton.SetSelected(true);
                    break;
            }
        }

        /// <summary>
        /// Obtenir l'onglet actuel
        /// </summary>
        public MenuTab GetCurrentTab()
        {
            return currentTab;
        }

        /// <summary>
        /// Afficher/Cacher le menu entier
        /// </summary>
        public void SetMenuVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
