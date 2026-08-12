using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using BeeKingdom.Core.Data;
using BeeKingdom.Core.Buildings;

namespace BeeKingdom.Core
{
    /// <summary>
    /// BuildingManager - Gère tous les bâtiments de la ruche
    /// - 20 slots constructibles
    /// - Construction et upgrades
    /// - Production passive
    /// - Sauvegarde
    /// 
    /// ✅ VERSION CORRIGÉE - FIX POUR AFFICHAGE NOM ET UPGRADE
    /// </summary>
    public class BuildingManager : MonoBehaviour
    {
        #region Singleton

        public static BuildingManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Initialiser les slots
            if (buildingSlots == null || buildingSlots.Length == 0)
            {
                buildingSlots = new BuildingData[TOTAL_SLOTS];
                for (int i = 0; i < TOTAL_SLOTS; i++)
                {
                    buildingSlots[i] = new BuildingData(i);
                }
            }
        }

        #endregion

        #region Configuration

        private const int TOTAL_SLOTS = 20;  // 20 emplacements dans la ruche

        [Header("Building Database")]
        [SerializeField] private BuildingSO[] allBuildings;  // Tous les bâtiments disponibles

        #endregion

        #region Data

        private Dictionary<string, BuildingSO> buildingDatabase;  // buildingId -> BuildingSO
        private BuildingData[] buildingSlots;                     // 20 slots de la ruche
        private float productionTimer = 0f;

        #endregion

        #region Events

        public event System.Action<int, BuildingData> OnBuildingConstructed;  // slot, building
        public event System.Action<int, BuildingData> OnBuildingUpgraded;     // slot, building
        public event System.Action<int> OnBuildingDemolished;                 // slot
        public event System.Action<int, BuildingData> OnConstructionComplete; // slot, building

        #endregion

        #region Initialization

        public void Initialize()
        {
            // Créer la database
            buildingDatabase = new Dictionary<string, BuildingSO>();
            foreach (BuildingSO building in allBuildings)
            {
                if (building != null)
                {
                    buildingDatabase[building.buildingId] = building;
                }
            }

            productionTimer = 0f;
        }

        #endregion

        #region Update Loop

        private void Update()
        {
            // Vérifier les constructions en cours
            CheckConstructions();

            // Production passive des bâtiments
            ProduceResources(UnityEngine.Time.deltaTime);
        }

        /// <summary>
        /// Vérifie si des constructions sont terminées
        /// </summary>
        private void CheckConstructions()
        {
            for (int i = 0; i < buildingSlots.Length; i++)
            {
                BuildingData building = buildingSlots[i];

                if (building.isConstructing && building.IsConstructionComplete())
                {
                    // Construction terminée !
                    building.CompleteConstruction();
                    OnConstructionComplete?.Invoke(i, building);

                    // Appliquer les bonus (stockage, etc.)
                    ApplyBuildingBonus(building);
                }
            }
        }

        /// <summary>
        /// Production passive des bâtiments
        /// </summary>
        private void ProduceResources(float deltaTime)
        {
            productionTimer += deltaTime;

            if (productionTimer >= 1f)
            {
                productionTimer = 0f;

                // Chaque bâtiment qui produit des ressources
                foreach (BuildingData building in buildingSlots)
                {
                    if (building.IsEmpty() || building.isConstructing) continue;

                    BuildingSO buildingSO = GetBuildingSO(building.buildingType);
                    if (buildingSO != null && buildingSO.canProduceResources)
                    {
                        float production = buildingSO.GetProductionForLevel(building.level);
                        int produced = Mathf.FloorToInt(production);

                        if (produced > 0 && ResourceManager.Instance != null)
                        {
                            ResourceManager.Instance.AddResource(
                                buildingSO.producedResourceType,
                                produced,
                                false  // Pas de log pour chaque production
                            );
                        }
                    }
                }
            }
        }

        #endregion

        #region Public Methods - Build & Upgrade

        /// <summary>
        /// Construit un nouveau bâtiment dans un slot
        /// </summary>
        public bool BuildBuilding(int slotIndex, string buildingId)
        {
            // Vérifications
            if (slotIndex < 0 || slotIndex >= TOTAL_SLOTS)
            {
                Debug.LogWarning($"⚠️ Invalid slot index: {slotIndex}");
                return false;
            }

            if (!buildingSlots[slotIndex].IsEmpty())
            {
                Debug.LogWarning($"⚠️ Slot {slotIndex} is not empty!");
                return false;
            }

            BuildingSO buildingSO = GetBuildingSO(buildingId);
            if (buildingSO == null)
            {
                Debug.LogWarning($"⚠️ Building {buildingId} not found!");
                return false;
            }

            // Vérifier le niveau de Queen's Chamber requis
            // TODO: Implémenter la vérification du niveau

            // Vérifier le coût
            ResourceCost[] cost = buildingSO.GetCostForLevel(1);
            if (!ResourceManager.Instance.CanAfford(cost))
            {
                Debug.LogWarning($"⚠️ Cannot afford {buildingSO.buildingName}!");
                return false;
            }

            // Dépenser les ressources
            if (!ResourceManager.Instance.SpendResources(cost))
            {
                return false;
            }

            // Créer le bâtiment
            float duration = buildingSO.GetDurationForLevel(1);
            buildingSlots[slotIndex] = new BuildingData(slotIndex, buildingSO.buildingType, duration);

            OnBuildingConstructed?.Invoke(slotIndex, buildingSlots[slotIndex]);

            // Marquer pour sauvegarde
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.MarkDirty();
            }

            return true;
        }

        /// <summary>
        /// Améliore un bâtiment existant
        /// ✅ UPGRADE AVEC ANIMATION - Comme la construction initiale!
        /// </summary>
        public void UpgradeBuilding(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= buildingSlots.Length)
            {
                Debug.LogError($"❌ Invalid slot index: {slotIndex}");
                return;
            }

            BuildingData building = buildingSlots[slotIndex];

            if (building == null || building.IsEmpty())
            {
                Debug.LogError($"❌ No building in slot {slotIndex}");
                return;
            }

            if (building.isConstructing)
            {
                Debug.LogWarning($"⚠️ Building is still under construction");
                return;
            }

            BuildingSO buildingSO = GetBuildingSO(building.buildingType);
            if (buildingSO == null)
            {
                Debug.LogError($"❌ BuildingSO not found for {building.buildingType}");
                return;
            }

            int nextLevel = building.level + 1;
            if (nextLevel > buildingSO.maxLevel)
            {
                Debug.LogWarning($"⚠️ Building already at max level!");
                return;
            }

            ResourceCost[] upgradeCosts = buildingSO.GetCostForLevel(nextLevel);

            if (!ResourceManager.Instance.CanAfford(upgradeCosts))
            {
                Debug.LogWarning($"⚠️ Cannot afford upgrade!");
                return;
            }

            if (!ResourceManager.Instance.SpendResources(upgradeCosts))
            {
                Debug.LogError($"❌ Failed to spend resources");
                return;
            }

            // ✅ DÉMARRER L'ANIMATION D'UPGRADE
            // Incrémenter le level maintenant (sera visible après l'animation)
            building.level = nextLevel;

            // Mettre en mode "construction" pour l'animation
            float duration = buildingSO.GetDurationForLevel(nextLevel);
            building.isConstructing = true;
            building.constructionStartTime = System.DateTime.Now;
            building.constructionDuration = duration;

            // Déclencher événement pour mettre à jour les visuels (afficher l'animation!)
            OnBuildingUpgraded?.Invoke(slotIndex, building);

            // Marquer pour sauvegarde
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.MarkDirty();
            }
        }


        /// <summary>
        /// Termine instantanément une construction (speed up)
        /// </summary>
        public void InstantComplete(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= TOTAL_SLOTS) return;

            BuildingData building = buildingSlots[slotIndex];
            if (building.isConstructing)
            {
                building.CompleteConstruction();  // ✅ JUSTE CETTE LIGNE
                ApplyBuildingBonus(building);
                OnConstructionComplete?.Invoke(slotIndex, building);

            }
        }

        /// <summary>
        /// Détruit un bâtiment
        /// </summary>
        public void DemolishBuilding(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= TOTAL_SLOTS) return;

            BuildingData building = buildingSlots[slotIndex];
            if (building.IsEmpty()) return;

            // Retirer les bonus
            RemoveBuildingBonus(building);

            building.Demolish();
            OnBuildingDemolished?.Invoke(slotIndex);

            // Marquer pour sauvegarde
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.MarkDirty();
            }
        }

        #endregion

        #region Public Methods - Get Info

        /// <summary>
        /// ✅ MÉTHODE CORRIGÉE - Accepte buildingId ("queens_chamber") ET enum name ("QueensChamber")
        /// Obtient le BuildingSO depuis la database
        /// </summary>
        public BuildingSO GetBuildingSO(string buildingIdOrEnumName)
        {
            if (buildingDatabase == null) return null;

            // Essayer d'abord avec la clé directe (buildingId snake_case)
            if (buildingDatabase.ContainsKey(buildingIdOrEnumName))
            {
                return buildingDatabase[buildingIdOrEnumName];
            }

            // Sinon, chercher par enum name (PascalCase)
            foreach (var kvp in buildingDatabase)
            {
                if (kvp.Value.buildingType.ToString() == buildingIdOrEnumName)
                {
                    return kvp.Value;
                }
            }

            Debug.LogWarning($"⚠️ Building '{buildingIdOrEnumName}' not found in database");
            return null;
        }

        /// <summary>
        /// Obtient le BuildingSO depuis le type enum directement
        /// </summary>
        public BuildingSO GetBuildingSO(BuildingType type)
        {
            if (buildingDatabase == null) return null;

            foreach (var kvp in buildingDatabase)
            {
                if (kvp.Value.buildingType == type)
                {
                    return kvp.Value;
                }
            }
            return null;
        }

        public List<BuildingSO> GetAvailableBuildings()
        {
            if (buildingDatabase == null)
            {
                return new List<BuildingSO>();
            }

            return buildingDatabase.Values
                .Where(building => building != null)
                .OrderBy(building => building.requiredQueenLevel)
                .ThenBy(building => building.buildingName)
                .ToList();
        }

        /// <summary>
        /// Obtient les données d'un slot
        /// </summary>
        public BuildingData GetBuildingInSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= TOTAL_SLOTS) return null;
            return buildingSlots[slotIndex];
        }

        /// <summary>
        /// Obtient tous les bâtiments construits d'un type
        /// </summary>
        public List<BuildingData> GetBuildingsOfType(BuildingType type)
        {
            return buildingSlots.Where(b => b.buildingType == type && !b.IsEmpty()).ToList();
        }

        /// <summary>
        /// Compte le nombre de bâtiments d'un type
        /// </summary>
        public int CountBuildingsOfType(BuildingType type)
        {
            return buildingSlots.Count(b => b.buildingType == type && !b.IsEmpty());
        }

        /// <summary>
        /// Obtient le nombre total de slots disponibles
        /// </summary>
        public int GetTotalSlots()
        {
            return TOTAL_SLOTS;
        }

        /// <summary>
        /// Obtient le nombre de slots vides
        /// </summary>
        public int GetEmptySlots()
        {
            return buildingSlots.Count(b => b.IsEmpty());
        }

        #endregion

        #region Building Bonuses

        /// <summary>
        /// Applique les bonus d'un bâtiment (stockage, etc.)
        /// </summary>
        private void ApplyBuildingBonus(BuildingData building)
        {
            BuildingSO buildingSO = GetBuildingSO(building.buildingType);
            if (buildingSO == null) return;

            // Bonus de stockage
            if (buildingSO.isStorage && ResourceManager.Instance != null)
            {
                int capacity = buildingSO.GetStorageForLevel(building.level);
                ResourceManager.Instance.IncreaseCapacity(buildingSO.storedResourceType, capacity);

            }
        }

        /// <summary>
        /// Retire les bonus d'un bâtiment
        /// </summary>
        private void RemoveBuildingBonus(BuildingData building)
        {
            BuildingSO buildingSO = GetBuildingSO(building.buildingType);
            if (buildingSO == null) return;

            // Retirer le bonus de stockage
            if (buildingSO.isStorage && ResourceManager.Instance != null)
            {
                int capacity = buildingSO.GetStorageForLevel(building.level);
                ResourceManager.Instance.IncreaseCapacity(buildingSO.storedResourceType, -capacity);

            }
        }

        #endregion

        #region Save/Load

        /// <summary>
        /// Charge les bâtiments depuis la sauvegarde
        /// </summary>
        public void LoadBuildings(BuildingData[] savedBuildings)
        {
            if (savedBuildings != null && savedBuildings.Length == TOTAL_SLOTS)
            {
                buildingSlots = savedBuildings;

                // Appliquer les bonus de tous les bâtiments construits
                foreach (BuildingData building in buildingSlots)
                {
                    if (!building.IsEmpty() && !building.isConstructing)
                    {
                        ApplyBuildingBonus(building);
                    }
                }
            }
        }

        /// <summary>
        /// Obtient les données des bâtiments pour la sauvegarde
        /// </summary>
        public BuildingData[] GetBuildingsForSave()
        {
            return buildingSlots;
        }

        #endregion

        #region Debug Methods

#if UNITY_EDITOR
        [ContextMenu("Build Queens Chamber in Slot 0")]
        private void DebugBuildQueensChamber()
        {
            BuildBuilding(0, "queens_chamber");
        }

        [ContextMenu("Build Honey Storage in Slot 1")]
        private void DebugBuildHoneyStorage()
        {
            BuildBuilding(1, "honey_storage");
        }

        [ContextMenu("Complete All Constructions")]
        private void DebugCompleteAll()
        {
            for (int i = 0; i < TOTAL_SLOTS; i++)
            {
                InstantComplete(i);
            }
        }

        [ContextMenu("Show All Buildings")]
        private void DebugShowBuildings()
        {
            Debug.Log("=== BUILDINGS IN HIVE ===");
            for (int i = 0; i < buildingSlots.Length; i++)
            {
                BuildingData building = buildingSlots[i];
                if (!building.IsEmpty())
                {
                    string status = building.isConstructing ? 
                        $"(Constructing: {building.GetRemainingConstructionTime():F0}s remaining)" : 
                        "(Complete)";
                    Debug.Log($"Slot {i}: {building.buildingType} Level {building.level} {status}");
                }
            }
            Debug.Log($"Empty slots: {GetEmptySlots()}/{TOTAL_SLOTS}");
            Debug.Log("========================");
        }
#endif

        #endregion
    }
}
