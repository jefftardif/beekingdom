using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BeeKingdom.Core;
using BeeKingdom.Core.Data;

namespace BeeKingdom.UI
{
    /// <summary>
    /// UI pour un slot de bâtiment
    /// Version simple : affiche l'état et un bouton
    /// </summary>
    public class BuildingSlotUI : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private int slotIndex;

        [Header("UI References")]
        [SerializeField] private BuildingMenuUI buildingMenu;
        [SerializeField] private TextMeshProUGUI slotText;
        [SerializeField] private Button actionButton;
        [SerializeField] private TextMeshProUGUI buttonText;

        private BuildingData currentBuilding;

        private void Start()
        {
            if (actionButton != null)
            {
                actionButton.onClick.AddListener(OnButtonClick);
            }

            // S'abonner aux événements
            if (BuildingManager.Instance != null)
            {
                BuildingManager.Instance.OnBuildingConstructed += OnBuildingChanged;
                BuildingManager.Instance.OnBuildingUpgraded += OnBuildingChanged;
                BuildingManager.Instance.OnBuildingDemolished += OnBuildingDemolished;
                BuildingManager.Instance.OnConstructionComplete += OnBuildingChanged;
            }

            UpdateUI();
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
                UpdateUI();
            }
        }

        private void UpdateUI()
        {
            if (BuildingManager.Instance == null) return;

            currentBuilding = BuildingManager.Instance.GetBuildingInSlot(slotIndex);
            
            if (currentBuilding == null) return;

            if (currentBuilding.IsEmpty())
            {
                // Slot vide
                if (slotText != null)
                {
                    slotText.text = $"Slot {slotIndex}: Empty";
                }
                if (buttonText != null)
                {
                    buttonText.text = "Build";
                }
                if (actionButton != null)
                {
                    actionButton.interactable = true;
                }
            }
            else if (currentBuilding.isConstructing)
            {
                // En construction
                float remaining = currentBuilding.GetRemainingConstructionTime();
                int minutes = Mathf.FloorToInt(remaining / 60f);
                int seconds = Mathf.FloorToInt(remaining % 60f);
                
                if (slotText != null)
                {
                    slotText.text = $"Slot {slotIndex}: {currentBuilding.buildingType} Lvl {currentBuilding.level}";
                }
                if (buttonText != null)
                {
                    buttonText.text = $"{minutes:00}:{seconds:00}";
                }
                if (actionButton != null)
                {
                    actionButton.interactable = false;
                }
            }
            else
            {
                // Construit
                if (slotText != null)
                {
                    slotText.text = $"Slot {slotIndex}: {currentBuilding.buildingType} Lvl {currentBuilding.level}";
                }
                if (buttonText != null)
                {
                    buttonText.text = "Upgrade";
                }
                if (actionButton != null)
                {
                    actionButton.interactable = true;
                }
            }
        }

        private void OnButtonClick()
        {
            if (currentBuilding == null) return;

            if (currentBuilding.IsEmpty())
            {
                // Ouvrir le menu de construction
                BuildingMenuUI menu = buildingMenu;
                if (menu != null)
                {
                    menu.ShowMenu(slotIndex);
                }
                else
                {
                    Debug.LogError("BuildingMenuUI not found.");
                }
            }
            else if (!currentBuilding.isConstructing)
            {
                // Upgrade
                BuildingManager.Instance.UpgradeBuilding(slotIndex);
            }
        }

        private void OnBuildingChanged(int slot, BuildingData building)
        {
            if (slot == slotIndex)
            {
                UpdateUI();
            }
        }

        private void OnBuildingDemolished(int slot)
        {
            if (slot == slotIndex)
            {
                UpdateUI();
            }
        }
    }
}
