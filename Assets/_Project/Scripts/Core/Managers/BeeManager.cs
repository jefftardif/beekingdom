using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using BeeKingdom.Core.Bees;
using BeeKingdom.Core.Data;

namespace BeeKingdom.Core
{
    /// <summary>
    /// BeeManager - Gère toutes les abeilles du joueur
    /// - Recrutement
    /// - Stockage
    /// - Production passive
    /// - Sauvegarde
    /// </summary>
    public class BeeManager : MonoBehaviour
    {
        #region Singleton

        public static BeeManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Initialiser la liste dès le Awake pour éviter les null
            ownedBees = new List<BeeData>();
        }

        #endregion

        #region Configuration

        [Header("Bee Database")]
        [SerializeField] private BeeSO[] allBees; // Toutes les abeilles disponibles

        #endregion

        #region Data

        private Dictionary<string, BeeSO> beeDatabase; // beeId -> BeeSO
        private List<BeeData> ownedBees; // Abeilles possédées par le joueur
        private float productionTimer;

        #endregion

        #region Events

        public event System.Action<BeeSO, BeeData> OnBeeRecruited;

        #endregion

        #region Initialization

        public void Initialize()
        {
            // Créer la database
            beeDatabase = new Dictionary<string, BeeSO>();
            foreach (BeeSO bee in allBees)
            {
                if (bee != null)
                {
                    beeDatabase[bee.beeId] = bee;
                }
            }

            productionTimer = 0f;
        }

        #endregion

        #region Update Loop

        private void Update()
        {
            // Production passive des abeilles
            ProduceResources(UnityEngine.Time.deltaTime);
        }

        private void ProduceResources(float deltaTime)
        {
            productionTimer += deltaTime;

            if (productionTimer >= 1f)
            {
                productionTimer = 0f;

                // Vérifier que ResourceManager existe
                if (ResourceManager.Instance == null)
                {
                    return;
                }

                // Chaque abeille produit des ressources
                foreach (BeeData beeData in ownedBees)
                {
                    BeeSO beeSO = GetBeeSO(beeData.beeId);
                    if (beeSO != null && beeSO.canProduceResources)
                    {
                        int produced = Mathf.FloorToInt(beeSO.productionRate);
                        if (produced > 0)
                        {
                            ResourceManager.Instance.AddResource(beeSO.producedResourceType, produced, false);
                        }
                    }
                }
            }
        }

        #endregion

        #region Public Methods - Recruit

        /// <summary>
        /// Recrute une abeille (dépense les ressources)
        /// </summary>
        public bool RecruitBee(string beeId)
        {
            BeeSO beeSO = GetBeeSO(beeId);
            if (beeSO == null)
            {
                Debug.LogWarning($"⚠️ Bee {beeId} not found in database!");
                return false;
            }

            // Vérifier si débloqué
            if (!beeSO.isUnlockedByDefault && GameManager.Instance.PlayerLevel < beeSO.requiredPlayerLevel)
            {
                Debug.LogWarning($"⚠️ {beeSO.beeName} requires level {beeSO.requiredPlayerLevel}!");
                return false;
            }

            // Vérifier le coût
            if (!ResourceManager.Instance.CanAfford(beeSO.recruitCost))
            {
                Debug.LogWarning($"⚠️ Cannot afford {beeSO.beeName}!");
                return false;
            }

            // Dépenser les ressources
            if (!ResourceManager.Instance.SpendResources(beeSO.recruitCost))
            {
                return false;
            }

            // Créer l'abeille
            BeeData newBee = new BeeData(beeId, beeSO.maxHealth);
            ownedBees.Add(newBee);

            OnBeeRecruited?.Invoke(beeSO, newBee);

            // Marquer pour sauvegarde
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.MarkDirty();
            }

            return true;
        }

        #endregion

        #region Public Methods - Get Info

        /// <summary>
        /// Obtient le BeeSO depuis la database
        /// </summary>
        public BeeSO GetBeeSO(string beeId)
        {
            if (beeDatabase != null && beeDatabase.ContainsKey(beeId))
            {
                return beeDatabase[beeId];
            }
            return null;
        }

        /// <summary>
        /// Obtenir la liste de toutes les abeilles possédées
        /// </summary>
        public List<BeeData> GetOwnedBees()
        {
            return ownedBees;
        }

        /// <summary>
        /// Obtient le nombre total d'abeilles
        /// </summary>
        public int GetTotalBeeCount()
        {
            return ownedBees.Count;
        }

        /// <summary>
        /// Obtient le nombre d'abeilles d'un type spécifique
        /// </summary>
        public int GetBeeCount(string beeId)
        {
            return ownedBees.Count(b => b.beeId == beeId);
        }

        /// <summary>
        /// Obtient toutes les abeilles possédées
        /// </summary>
        public List<BeeData> GetAllOwnedBees()
        {
            return new List<BeeData>(ownedBees);
        }

        #endregion

        #region Save/Load

        /// <summary>
        /// Charge les abeilles depuis la sauvegarde
        /// </summary>
        public void LoadBees(List<BeeData> savedBees)
        {
            if (savedBees != null)
            {
                ownedBees = savedBees;
            }
        }

        /// <summary>
        /// Obtient les données des abeilles pour la sauvegarde
        /// </summary>
        public List<BeeData> GetBeesForSave()
        {
            return new List<BeeData>(ownedBees);
        }

        #endregion

        #region Debug Methods

#if UNITY_EDITOR
        [ContextMenu("Recruit Worker Bee")]
        private void DebugRecruitWorker()
        {
            RecruitBee("worker_bee");
        }

        [ContextMenu("Show All Bees")]
        private void DebugShowBees()
        {
            Debug.Log("=== OWNED BEES ===");
            foreach (BeeData bee in ownedBees)
            {
                BeeSO beeSO = GetBeeSO(bee.beeId);
                if (beeSO != null)
                {
                    Debug.Log($"🐝 {beeSO.beeName} (HP: {bee.currentHealth}/{beeSO.maxHealth}, Lvl: {bee.level})");
                }
            }
            Debug.Log($"Total: {ownedBees.Count} bees");
            Debug.Log("==================");
        }
#endif

        #endregion
    }
}
