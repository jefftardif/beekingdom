using UnityEngine;
using System;
using System.Collections.Generic;
using BeeKingdom.Core.Data;

namespace BeeKingdom.Core
{
    /// <summary>
    /// ResourceManager - Gère toutes les ressources du jeu
    /// - Miel, Pollen, Cire, Gelée Royale
    /// - Production passive (idle)
    /// - Calcul offline (temps passé fermé)
    /// - Capacités de stockage
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        #region Singleton

        public static ResourceManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Initialiser le dictionnaire immédiatement
            resources = new Dictionary<ResourceType, ResourceData>
    {
        { ResourceType.Honey, new ResourceData(ResourceType.Honey, honeyMaxCapacity) },
        { ResourceType.Pollen, new ResourceData(ResourceType.Pollen, pollenMaxCapacity) },
        { ResourceType.Wax, new ResourceData(ResourceType.Wax, waxMaxCapacity) },
        { ResourceType.RoyalJelly, new ResourceData(ResourceType.RoyalJelly, royalJellyMaxCapacity) }
    };
        }

        #endregion

        #region Configuration

        [Header("Initial Capacities")]
        [SerializeField] private int honeyMaxCapacity = 10000;
        [SerializeField] private int pollenMaxCapacity = 1000;
        [SerializeField] private int waxMaxCapacity = 100;
        [SerializeField] private int royalJellyMaxCapacity = 50;

        [Header("Production Rates (per second)")]
        [SerializeField] private float honeyProductionRate = 10f;
        //[SerializeField] private float pollenProductionRate = 0f; // Pas de prod auto pour pollen
        [SerializeField] private float waxProductionRate = 0.1f;

        [Header("Offline Production")]
        [SerializeField] private float maxOfflineHours = 8f; // Max 8 heures de production offline

        #endregion

        #region Data

        private Dictionary<ResourceType, ResourceData> resources;
        private DateTime lastUpdateTime;
        private float productionTimer;

        #endregion


        #region Events

        // Événements pour notifier les changements de ressources
        public event Action<ResourceType, int> OnResourceChanged;
        public event Action<ResourceType, int> OnResourceAdded;
        public event Action<ResourceType, int> OnResourceSpent;
        public event Action<ResourceType> OnResourceCapacityReached;

        #endregion

        #region Initialization

        public void Initialize()
        {
            lastUpdateTime = DateTime.Now;
            productionTimer = 0f;
        }

        #endregion

        #region Update Loop

        private void Update()
        {
            // Production passive de ressources
            ProduceResources(UnityEngine.Time.deltaTime);
        }

        /// <summary>
        /// Produit des ressources passivement chaque frame
        /// </summary>
        private void ProduceResources(float deltaTime)
        {
            productionTimer += deltaTime;

            // Produire chaque seconde
            if (productionTimer >= 1f)
            {
                productionTimer = 0f;

                // Production de miel
                if (honeyProductionRate > 0)
                {
                    int honeyProduced = Mathf.FloorToInt(honeyProductionRate);
                    AddResource(ResourceType.Honey, honeyProduced, true);
                }

                // Production de cire
                if (waxProductionRate > 0)
                {
                    int waxProduced = Mathf.FloorToInt(waxProductionRate);
                    AddResource(ResourceType.Wax, waxProduced, true);
                }
            }
        }

        #endregion

        #region Public Methods - Get Resources

        /// <summary>
        /// Obtient la quantité actuelle d'une ressource
        /// </summary>
        public int GetResource(ResourceType type)
        {
            if (resources.ContainsKey(type))
            {
                return resources[type].currentAmount;
            }
            return 0;
        }

        /// <summary>
        /// Obtient la capacité maximum d'une ressource
        /// </summary>
        public int GetMaxCapacity(ResourceType type)
        {
            if (resources.ContainsKey(type))
            {
                return resources[type].maxCapacity;
            }
            return 0;
        }

        /// <summary>
        /// Obtient toutes les ressources (pour sauvegarde)
        /// </summary>
        public Dictionary<ResourceType, int> GetAllResources()
        {
            Dictionary<ResourceType, int> allResources = new Dictionary<ResourceType, int>();
            foreach (var kvp in resources)
            {
                allResources[kvp.Key] = kvp.Value.currentAmount;
            }
            return allResources;
        }

        /// <summary>
        /// Obtient le ResourceData complet
        /// </summary>
        public ResourceData GetResourceData(ResourceType type)
        {
            if (resources.ContainsKey(type))
            {
                return resources[type];
            }
            return null;
        }

        #endregion

        #region Public Methods - Add Resources

        /// <summary>
        /// Ajoute une ressource
        /// </summary>
        public void AddResource(ResourceType type, int amount, bool showLog = true)
        {
            if (!resources.ContainsKey(type)) return;
            if (amount <= 0) return;

            ResourceData data = resources[type];
            int oldAmount = data.currentAmount;

            // Ajouter avec cap de capacité max
            data.currentAmount = Mathf.Min(data.currentAmount + amount, data.maxCapacity);
            int actualAdded = data.currentAmount - oldAmount;

            if (actualAdded > 0)
            {
                // Déclencher événements
                OnResourceAdded?.Invoke(type, actualAdded);
                OnResourceChanged?.Invoke(type, data.currentAmount);
                // Marquer comme modifié pour la sauvegarde
                if (SaveManager.Instance != null)
                {
                    SaveManager.Instance.MarkDirty();
                }

                // Vérifier si capacité atteinte
                if (data.IsFull())
                {
                    OnResourceCapacityReached?.Invoke(type);
                }
            }
        }

        public void SetResource(ResourceType type, int amount, int maxCapacity)
        {
            if (!resources.ContainsKey(type)) return;

            ResourceData data = resources[type];
            data.maxCapacity = Mathf.Max(0, maxCapacity);
            data.currentAmount = Mathf.Clamp(amount, 0, data.maxCapacity);

            OnResourceChanged?.Invoke(type, data.currentAmount);
        }

        /// <summary>
        /// Ajoute plusieurs ressources à la fois
        /// </summary>
        public void AddResources(ResourceCost[] costs)
        {
            foreach (ResourceCost cost in costs)
            {
                AddResource(cost.resourceType, cost.amount);
            }
        }

        #endregion

        #region Public Methods - Spend Resources

        /// <summary>
        /// Dépense une ressource (retourne true si succès)
        /// </summary>
        public bool SpendResource(ResourceType type, int amount)
        {
            if (!resources.ContainsKey(type)) return false;
            if (amount <= 0) return false;

            ResourceData data = resources[type];

            // Vérifier si on a assez
            if (data.currentAmount < amount)
            {
                Debug.LogWarning($"⚠️ Not enough {type}! Need {amount}, have {data.currentAmount}");
                return false;
            }

            // Dépenser
            data.currentAmount -= amount;

            // Déclencher événements
            OnResourceSpent?.Invoke(type, amount);
            OnResourceChanged?.Invoke(type, data.currentAmount);
          //Marquer comme modifié pour la sauvegarde
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.MarkDirty();
            }
            return true;
        }

        /// <summary>
        /// Vérifie si on peut payer un coût
        /// </summary>
        public bool CanAfford(ResourceCost[] costs)
        {
            foreach (ResourceCost cost in costs)
            {
                if (GetResource(cost.resourceType) < cost.amount)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Vérifie si on peut payer un coût unique
        /// </summary>
        public bool CanAfford(ResourceType type, int amount)
        {
            return GetResource(type) >= amount;
        }

        /// <summary>
        /// Dépense plusieurs ressources (retourne true si tout a été dépensé)
        /// </summary>
        public bool SpendResources(ResourceCost[] costs)
        {
            // D'abord vérifier si on peut tout payer
            if (!CanAfford(costs))
            {
                Debug.LogWarning("⚠️ Cannot afford resources!");
                return false;
            }

            // Dépenser toutes les ressources
            foreach (ResourceCost cost in costs)
            {
                SpendResource(cost.resourceType, cost.amount);
            }

            return true;
        }


        /// <summary>
        /// Obtenir le taux de production actuel pour une ressource (par seconde)
        /// </summary>
        /// <summary>
        /// Obtenir le taux de production actuel pour une ressource (par seconde)
        /// Calcule en fonction des abeilles possédées
        /// </summary>
        /// <summary>
        /// Obtenir le taux de production actuel pour une ressource (par seconde)
        /// Calcule en fonction des abeilles possédées
        /// </summary>
        public int GetProductionRate(ResourceType type)
        {
            float totalRate = 0f;

            // Production de base (10 Honey/s)
            if (type == ResourceType.Honey)
            {
                totalRate += 10f; // Production passive de base
            }

            // Production des abeilles
            if (BeeManager.Instance != null)
            {
                var allBees = BeeManager.Instance.GetOwnedBees();
                foreach (var beeData in allBees)
                {
                    var beeSO = BeeManager.Instance.GetBeeSO(beeData.beeId);
                    if (beeSO != null && beeSO.canProduceResources && beeSO.producedResourceType == type)
                    {
                        totalRate += beeSO.productionRate;
                    }
                }
            }

            return Mathf.FloorToInt(totalRate);
        }



        #endregion

        #region Capacity Management

        /// <summary>
        /// Augmente la capacité max d'une ressource
        /// </summary>
        public void IncreaseCapacity(ResourceType type, int increaseAmount)
        {
            if (resources.ContainsKey(type))
            {
                resources[type].maxCapacity += increaseAmount;
                OnResourceChanged?.Invoke(type, resources[type].currentAmount);
            }
        }

        #endregion

        #region Production Rate Management

        /// <summary>
        /// Obtient le taux de production actuel du miel
        /// </summary>
        public float GetHoneyProductionRate()
        {
            return honeyProductionRate;
        }

        /// <summary>
        /// Modifie le taux de production du miel
        /// </summary>
        public void SetHoneyProductionRate(float newRate)
        {
            honeyProductionRate = newRate;
        }

        /// <summary>
        /// Ajoute au taux de production du miel
        /// </summary>
        public void AddHoneyProductionRate(float additionalRate)
        {
            honeyProductionRate += additionalRate;
        }

        #endregion

        #region Offline Production

        /// <summary>
        /// Calcule et applique la production pendant que le jeu était fermé
        /// Appelé par GameManager au démarrage
        /// </summary>
        public void CalculateOfflineProduction(DateTime lastPlayTime)
        {
            TimeSpan timePassed = DateTime.Now - lastPlayTime;
            float secondsOffline = (float)timePassed.TotalSeconds;

            // Cap à maxOfflineHours
            float maxOfflineSeconds = maxOfflineHours * 3600f;
            secondsOffline = Mathf.Min(secondsOffline, maxOfflineSeconds);

            if (secondsOffline < 60) // Moins d'une minute, on ignore
            {
                return;
            }

            // Calculer production offline
            int honeyProduced = Mathf.FloorToInt(honeyProductionRate * secondsOffline);
            int waxProduced = Mathf.FloorToInt(waxProductionRate * secondsOffline);

            // Ajouter les ressources
            if (honeyProduced > 0)
            {
                AddResource(ResourceType.Honey, honeyProduced);
            }

            if (waxProduced > 0)
            {
                AddResource(ResourceType.Wax, waxProduced);
            }

            // TODO: Afficher une popup avec les gains offline
        }

        /// <summary>
        /// Formate un temps en secondes en format lisible
        /// </summary>
        private string FormatTime(float seconds)
        {
            if (seconds < 60)
                return $"{seconds:F0} seconds";
            else if (seconds < 3600)
                return $"{seconds / 60:F0} minutes";
            else
                return $"{seconds / 3600:F1} hours";
        }

        #endregion

        #region Save/Load

        /// <summary>
        /// Charge les ressources depuis les données sauvegardées
        /// </summary>
        public void LoadResources(Dictionary<ResourceType, int> savedResources)
        {
            foreach (var kvp in savedResources)
            {
                if (resources.ContainsKey(kvp.Key))
                {
                    resources[kvp.Key].currentAmount = kvp.Value;
                    OnResourceChanged?.Invoke(kvp.Key, kvp.Value);
                }
            }
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Retourne l'emoji correspondant à une ressource
        /// </summary>
        private string GetResourceIcon(ResourceType type)
        {
            return type switch
            {
                ResourceType.Honey => "🍯",
                ResourceType.Pollen => "🌼",
                ResourceType.Wax => "🕯️",
                ResourceType.RoyalJelly => "💎",
                _ => "❓"
            };
        }

        #endregion

        #region Debug Methods

#if UNITY_EDITOR
        [ContextMenu("Add 1000 Honey")]
        private void DebugAddHoney()
        {
            AddResource(ResourceType.Honey, 1000);
        }

        [ContextMenu("Add 100 Pollen")]
        private void DebugAddPollen()
        {
            AddResource(ResourceType.Pollen, 100);
        }

        [ContextMenu("Add 50 Wax")]
        private void DebugAddWax()
        {
            AddResource(ResourceType.Wax, 50);
        }

        [ContextMenu("Add 10 Royal Jelly")]
        private void DebugAddRoyalJelly()
        {
            AddResource(ResourceType.RoyalJelly, 10);
        }

        [ContextMenu("Show All Resources")]
        private void DebugShowResources()
        {
            Debug.Log("=== CURRENT RESOURCES ===");
            foreach (var kvp in resources)
            {
                ResourceData data = kvp.Value;
                Debug.Log($"{GetResourceIcon(kvp.Key)} {kvp.Key}: {data.currentAmount}/{data.maxCapacity} ({data.GetFillPercentage() * 100:F1}%)");
            }
            Debug.Log("========================");
        }
#endif

        #endregion
    }
}
