using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BeeKingdom.Core;
using BeeKingdom.Core.Buildings;
using System.Collections.Generic;

namespace BeeKingdom.UI
{
    public class BuildingMenuUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private Button buildingButtonPrefab;
        [SerializeField] private Button closeButton;

        private int currentSlotIndex = -1;
        private readonly List<Button> spawnedButtons = new List<Button>();

        private void Start()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(HideMenu);
            }

            HideMenu();
        }

        public void ShowMenu(int slotIndex)
        {
            currentSlotIndex = slotIndex;

            if (menuPanel != null)
            {
                menuPanel.SetActive(true);
            }

            PopulateButtons();
        }

        public void HideMenu()
        {
            if (menuPanel != null)
            {
                menuPanel.SetActive(false);
            }

            currentSlotIndex = -1;
        }

        private void PopulateButtons()
        {
            foreach (Button btn in spawnedButtons)
            {
                if (btn != null)
                {
                    Destroy(btn.gameObject);
                }
            }
            spawnedButtons.Clear();

            if (BuildingManager.Instance == null || buttonContainer == null || buildingButtonPrefab == null)
            {
                return;
            }

            foreach (BuildingSO building in BuildingManager.Instance.GetAvailableBuildings())
            {
                CreateBuildingButton(building);
            }
        }

        private void CreateBuildingButton(BuildingSO building)
        {
            if (building == null) return;

            Button btn = Instantiate(buildingButtonPrefab, buttonContainer);

            TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                string costText = GetCostText(building);
                btnText.text = $"{building.buildingName}\n{costText}";
            }

            btn.onClick.AddListener(() => OnBuildingSelected(building.buildingId));
            spawnedButtons.Add(btn);
        }

        private string GetCostText(BuildingSO building)
        {
            var costs = building.GetCostForLevel(1);
            if (costs == null || costs.Length == 0) return "";

            string result = "(";
            for (int i = 0; i < costs.Length; i++)
            {
                result += $"{costs[i].amount} {GetResourceIcon(costs[i].resourceType)}";
                if (i < costs.Length - 1) result += ", ";
            }
            result += ")";

            return result;
        }

        private string GetResourceIcon(BeeKingdom.Core.Data.ResourceType type)
        {
            return type switch
            {
                BeeKingdom.Core.Data.ResourceType.Honey => "Honey",
                BeeKingdom.Core.Data.ResourceType.Pollen => "Pollen",
                BeeKingdom.Core.Data.ResourceType.Wax => "Wax",
                BeeKingdom.Core.Data.ResourceType.RoyalJelly => "Royal Jelly",
                _ => "?"
            };
        }

        private void OnBuildingSelected(string buildingId)
        {
            if (currentSlotIndex < 0 || BuildingManager.Instance == null) return;

            bool success = BuildingManager.Instance.BuildBuilding(currentSlotIndex, buildingId);
            if (success)
            {
                HideMenu();
            }
        }
    }
}
