using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using BeeKingdom.Core;
using BeeKingdom.Core.Data;

namespace BeeKingdom.UI
{
    /// <summary>
    /// Zone cliquable sur un hexagone de la ruche
    /// Style Ant Legion - chaque hexagone est interactif
    /// 
    /// ✅ VERSION CORRIGÉE - Icône visible pendant upgrade!
    /// </summary>
    public class HexagonBuildingSlot : MonoBehaviour, IPointerClickHandler
    {
        [Header("Configuration")]
        [SerializeField] private int slotIndex;

        [Header("Visual Indicators")]
        [SerializeField] private GameObject emptyIndicator;      // Montré quand slot vide
        [SerializeField] private GameObject constructingIndicator; // Montré pendant construction
        [SerializeField] private GameObject completedIndicator;   // Montré quand construit
        [SerializeField] private Image buildingIcon;              // Icône du bâtiment
        [SerializeField] private TextMeshProUGUI timerText;       // Timer de construction
        [SerializeField] private TextMeshProUGUI buildingNameText; // Nom du bâtiment
        [SerializeField] private TextMeshProUGUI levelText;       // Niveau

        [Header("UI References")]
        [SerializeField] private BuildingMenuUI buildingMenu;
        [SerializeField] private BuildingInfoUI buildingInfoUI;

        [Header("Hover Effect")]
        [SerializeField] private Image hoverOverlay;              // Overlay au survol
        [SerializeField] private Color hoverColor = new Color(1f, 1f, 1f, 0.2f);

        private BuildingData currentBuilding;

        private void Start()
        {

            if (constructingIndicator != null)
            {
                constructingIndicator.transform.SetAsLastSibling();
            }
            // S'abonner aux événements
            if (BuildingManager.Instance != null)
            {
                BuildingManager.Instance.OnBuildingConstructed += OnBuildingChanged;
                BuildingManager.Instance.OnBuildingUpgraded += OnBuildingChanged;
                BuildingManager.Instance.OnBuildingDemolished += OnBuildingDemolished;
                BuildingManager.Instance.OnConstructionComplete += OnBuildingChanged;
            }

            // Configurer hover overlay
            if (hoverOverlay != null)
            {
                hoverOverlay.color = hoverColor;
                hoverOverlay.gameObject.SetActive(false);
            }

            UpdateVisuals();
        }

        private void OnDestroy()
        {
            if (BuildingManager.Instance != null)
            {
                BuildingManager.Instance.OnBuildingConstructed -= OnBuildingChanged;
                BuildingManager.Instance.OnBuildingUpgraded -= OnBuildingChanged;
                BuildingManager.Instance.OnBuildingDemolished -= OnBuildingDemolished;
                BuildingManager.Instance.OnConstructionComplete -= OnBuildingChanged;
            }
        }

        private void Update()
        {
            // Mettre à jour le timer si en construction
            if (currentBuilding != null && currentBuilding.isConstructing)
            {
                UpdateTimer();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (currentBuilding == null)
            {
                Debug.LogError("Current building data is missing.");
                return;
            }

            if (currentBuilding.IsEmpty())
            {
                if (buildingMenu != null)
                {
                    buildingMenu.ShowMenu(slotIndex);
                }
            }
            else if (currentBuilding.isConstructing)
            {
                return;
            }
            else
            {
                if (buildingInfoUI != null)
                {
                    buildingInfoUI.ShowPanel(slotIndex, currentBuilding);
                }
                else
                {
                    Debug.LogError("BuildingInfoUI is not assigned.");
                }
            }
        }

        /// <summary>
        /// ✅ MÉTHODE CORRIGÉE - Garde l'icône visible pendant upgrade!
        /// </summary>
        private void UpdateVisuals()
        {
            if (BuildingManager.Instance == null) return;

            currentBuilding = BuildingManager.Instance.GetBuildingInSlot(slotIndex);

            if (currentBuilding == null) return;

            // Détecter si c'est un upgrade (level > 1)
            bool isUpgrade = !currentBuilding.IsEmpty() && currentBuilding.level > 1;

            // Désactiver tous les indicateurs
            if (emptyIndicator != null) emptyIndicator.SetActive(false);
            if (constructingIndicator != null) constructingIndicator.SetActive(false);
            if (completedIndicator != null) completedIndicator.SetActive(false);
            if (timerText != null) timerText.gameObject.SetActive(false);
            if (buildingNameText != null) buildingNameText.gameObject.SetActive(false);
            if (levelText != null) levelText.gameObject.SetActive(false);
            if (buildingIcon != null) buildingIcon.gameObject.SetActive(false);

            if (currentBuilding.IsEmpty())
            {
                // Slot vide
                if (emptyIndicator != null)
                {
                    emptyIndicator.SetActive(true);
                }
            }
            else if (currentBuilding.isConstructing)
            {
                // Afficher l'animation de construction
                if (constructingIndicator != null)
                {
                    constructingIndicator.SetActive(true);
                }

                // Afficher le timer
                if (timerText != null)
                {
                    timerText.gameObject.SetActive(true);
                    UpdateTimer();
                }

                // Afficher le nom du bâtiment
                if (buildingNameText != null)
                {
                    buildingNameText.gameObject.SetActive(true);
                    buildingNameText.text = currentBuilding.buildingType.ToString();
                }

                // ✨ NOUVEAU: Si c'est un UPGRADE, garder l'icône visible!
                if (isUpgrade && buildingIcon != null)
                {
                    buildingIcon.gameObject.SetActive(true);

                    // S'assurer que le sprite est à jour
                    var buildingSO = BuildingManager.Instance.GetBuildingSO(currentBuilding.buildingType.ToString());
                    if (buildingSO != null && buildingSO.icon != null)
                    {
                        buildingIcon.sprite = buildingSO.icon;
                    }
                }

                // Afficher le level pendant l'upgrade aussi
                if (isUpgrade && levelText != null)
                {
                    levelText.gameObject.SetActive(true);
                    levelText.text = $"Lv.{currentBuilding.level}";
                }
            }
            else
            {
                // Construit (pas en construction)
                if (completedIndicator != null)
                {
                    completedIndicator.SetActive(true);
                }

                if (buildingIcon != null)
                {
                    buildingIcon.gameObject.SetActive(true);

                    // Assigner le sprite depuis le BuildingSO
                    var buildingSO = BuildingManager.Instance.GetBuildingSO(currentBuilding.buildingType.ToString());
                    if (buildingSO != null && buildingSO.icon != null)
                    {
                        buildingIcon.sprite = buildingSO.icon;
                    }
                }

                if (buildingNameText != null)
                {
                    buildingNameText.gameObject.SetActive(true);
                    buildingNameText.text = currentBuilding.buildingType.ToString();
                }

                if (levelText != null)
                {
                    levelText.gameObject.SetActive(true);
                    levelText.text = $"Lv.{currentBuilding.level}";
                }
            }
        }

        private void UpdateTimer()
        {
            if (currentBuilding == null || timerText == null) return;

            float remaining = currentBuilding.GetRemainingConstructionTime();
            int minutes = Mathf.FloorToInt(remaining / 60f);
            int seconds = Mathf.FloorToInt(remaining % 60f);

            timerText.text = $"{minutes:00}:{seconds:00}";
        }

        private void OnBuildingChanged(int slot, BuildingData building)
        {
            if (slot == slotIndex)
            {
                UpdateVisuals();
            }
        }

        private void OnBuildingDemolished(int slot)
        {
            if (slot == slotIndex)
            {
                UpdateVisuals();
            }
        }

        // Hover effects (optionnel, pour feedback visuel)
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (hoverOverlay != null)
            {
                hoverOverlay.gameObject.SetActive(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (hoverOverlay != null)
            {
                hoverOverlay.gameObject.SetActive(false);
            }
        }

        // Pour le debug - afficher dans l'éditeur
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, GetComponent<RectTransform>().sizeDelta);
        }
    }
}
