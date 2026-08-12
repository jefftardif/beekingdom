using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BeeKingdom.Core;
using BeeKingdom.Core.Data;

namespace BeeKingdom.UI
{
    public class BuildingInfoUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI buildingNameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI productionText;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private TextMeshProUGUI upgradeButtonText;
        [SerializeField] private Button closeButton;

        private int currentSlotIndex;
        private BuildingData currentBuilding;

        private void Start()
        {
            // S'abonner aux événements des boutons
            if (upgradeButton != null)
                upgradeButton.onClick.AddListener(OnUpgradeClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(ClosePanel);

            // Cacher par défaut
            gameObject.SetActive(false);
        }

        public void ShowPanel(int slotIndex, BuildingData building)
        {
            currentSlotIndex = slotIndex;
            currentBuilding = building;

            gameObject.SetActive(true);

            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (currentBuilding == null) return;

            var buildingSO = BuildingManager.Instance.GetBuildingSO(currentBuilding.buildingType.ToString());
            if (buildingSO == null) return;

            // Nom du bâtiment
            if (buildingNameText != null)
                buildingNameText.text = buildingSO.buildingName; // Utilisez le vrai nom !

            // Niveau
            if (levelText != null)
                levelText.text = $"Level {currentBuilding.level}";

            // Production (calculée avec la méthode du SO)
            if (productionText != null)
            {
                if (buildingSO.canProduceResources)
                {
                    float production = buildingSO.GetProductionForLevel(currentBuilding.level);
                    productionText.text = $"Production: +{production:F1} {buildingSO.producedResourceType}/s";
                }
                else if (buildingSO.isStorage)
                {
                    int storage = buildingSO.GetStorageForLevel(currentBuilding.level);
                    productionText.text = $"Storage: +{storage} {buildingSO.storedResourceType}";
                }
                else
                {
                    productionText.text = ""; // Pas de production
                }
            }

            // Bouton upgrade
            UpdateUpgradeButton();
        }


        private void UpdateUpgradeButton()
        {
            if (upgradeButton == null) return;

            var buildingSO = BuildingManager.Instance.GetBuildingSO(currentBuilding.buildingType.ToString());
            if (buildingSO == null) return;

            // Niveau suivant
            int nextLevel = currentBuilding.level + 1;

            // Utiliser la méthode qui existe déjà dans BuildingSO !
            ResourceCost[] upgradeCosts = buildingSO.GetCostForLevel(nextLevel);

            // Trouver le coût en Honey
            int honeyCost = 0;
            if (upgradeCosts != null && upgradeCosts.Length > 0)
            {
                foreach (var cost in upgradeCosts)
                {
                    if (cost.resourceType == ResourceType.Honey)
                    {
                        honeyCost = cost.amount;
                        break;
                    }
                }
            }

            // Mettre à jour le texte
            if (upgradeButtonText != null)
                upgradeButtonText.text = $"Upgrade to Lv.{nextLevel}\n({honeyCost} 🍯)";

            // Vérifier si on peut upgrader
            bool canAfford = ResourceManager.Instance.GetResource(ResourceType.Honey) >= honeyCost;
            bool canUpgrade = nextLevel <= buildingSO.maxLevel && canAfford;

            upgradeButton.interactable = canUpgrade;

            // Si niveau max atteint
            if (nextLevel > buildingSO.maxLevel && upgradeButtonText != null)
            {
                upgradeButtonText.text = "MAX LEVEL";
            }
        }

        private void OnUpgradeClicked()
        {
            if (BuildingManager.Instance != null)
            {
                BuildingManager.Instance.UpgradeBuilding(currentSlotIndex);

                // ✅ Ferme seulement si pas en construction
                if (currentBuilding != null && !currentBuilding.isConstructing)
                {
                    ClosePanel();
                }
                else
                {
                    // Optionnel: afficher un message "Upgrade en cours..."
                    ClosePanel(); // ou garde le popup ouvert avec un message
                }
            }
        }

        public void ClosePanel()
        {
            gameObject.SetActive(false);
        }
    }
}
//```

//---

//## 📎 ÉTAPE 4: ASSIGNEZ LE SCRIPT
//```
//1.Hierarchy → BuildingInfoPanel
//2. Inspector → Add Component → BuildingInfoUI
//3. Glissez les références:
//   -Building Name Text: BuildingNameText
//   - Level Text: LevelText
//   - Production Text: ProductionText
//   - Upgrade Button: UpgradeButton     
//   - Upgrade Button Text: UpgradeButton → Text (TMP)
//   - Close Button: CloseButton
