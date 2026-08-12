using UnityEngine;
using BeeKingdom.Core.Data;

namespace BeeKingdom.Core.Buildings
{
    /// <summary>
    /// ScriptableObject définissant un type de bâtiment
    /// Créez un asset pour chaque bâtiment du jeu
    /// </summary>
    [CreateAssetMenu(fileName = "New Building", menuName = "Bee Kingdom/Buildings/Building")]
    public class BuildingSO : ScriptableObject
    {
        [Header("Identity")]
        public string buildingId;                       // ID unique (ex: "queens_chamber")
        public string buildingName;                     // Nom affiché
        public BeeKingdom.Core.Data.BuildingType buildingType;
        [TextArea(3, 5)]
        public string description;                      // Description

        [Header("Visual")]
        public Sprite icon;                             // Icône du bâtiment
        public string emoji = "🏰";                     // Emoji temporaire

        [Header("Requirements")]
        public int requiredQueenLevel = 1;              // Niveau de Queen's Chamber requis
        public bool isUnlockedByDefault = true;         // Débloqué dès le début ?

        [Header("Base Stats (Level 1)")]
        public ResourceCost[] buildCost;                // Coût de construction
        public float buildDuration = 10f;               // Durée construction (secondes)

        [Header("Production (if applicable)")]
        public bool canProduceResources = false;        // Produit des ressources ?
        public Data.ResourceType producedResourceType;  // Type produit
        public float productionRate = 0f;               // Quantité/seconde

        [Header("Storage (if applicable)")]
        public bool isStorage = false;                  // Augmente le stockage ?
        public Data.ResourceType storedResourceType;    // Type stocké
        public int storageCapacityBonus = 0;            // +Capacité par niveau

        [Header("Upgrades")]
        public int maxLevel = 10;                       // Niveau maximum
        public float costMultiplierPerLevel = 1.5f;     // Multiplieur de coût par niveau
        public float durationMultiplierPerLevel = 1.2f; // Multiplieur de durée
        public float productionIncreasePerLevel = 1.0f; // +Production par niveau
        public int storageIncreasePerLevel = 100;       // +Stockage par niveau

        #region Cost Calculations

        /// <summary>
        /// Calcule le coût pour un niveau spécifique
        /// </summary>
        public ResourceCost[] GetCostForLevel(int level)
        {
            if (level == 1)
            {
                return buildCost;
            }

            // Appliquer le multiplicateur
            ResourceCost[] costs = new ResourceCost[buildCost.Length];
            float multiplier = Mathf.Pow(costMultiplierPerLevel, level - 1);

            for (int i = 0; i < buildCost.Length; i++)
            {
                costs[i] = new ResourceCost(
     buildCost[i].resourceType,
     Mathf.RoundToInt(buildCost[i].amount * multiplier)
 );
            }

            return costs;
        }

        /// <summary>
        /// Calcule la durée de construction pour un niveau
        /// </summary>
        public float GetDurationForLevel(int level)
        {
            if (level == 1)
            {
                return buildDuration;
            }

            return buildDuration * Mathf.Pow(durationMultiplierPerLevel, level - 1);
        }

        /// <summary>
        /// Calcule la production pour un niveau
        /// </summary>
        public float GetProductionForLevel(int level)
        {
            if (!canProduceResources) return 0f;

            return productionRate + (productionIncreasePerLevel * (level - 1));
        }

        /// <summary>
        /// Calcule la capacité de stockage pour un niveau
        /// </summary>
        public int GetStorageForLevel(int level)
        {
            if (!isStorage) return 0;

            return storageCapacityBonus + (storageIncreasePerLevel * (level - 1));
        }

        #endregion

        #region Validation

        private void OnValidate()
        {
            // Générer un ID si vide
            if (string.IsNullOrEmpty(buildingId) && !string.IsNullOrEmpty(buildingName))
            {
                buildingId = buildingName.ToLower().Replace(" ", "_");
            }
        }

        #endregion
    }
}
