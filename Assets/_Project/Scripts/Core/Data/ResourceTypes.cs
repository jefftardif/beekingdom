using System;

namespace BeeKingdom.Core.Data
{
    /// <summary>
    /// Types de ressources disponibles dans Bee Kingdom
    /// </summary>
    public enum ResourceType
    {
        Honey,          // Miel - Monnaie principale
        Pollen,         // Pollen - Ressource secondaire
        Wax,            // Cire - Construction
        RoyalJelly      // Gelée Royale - Premium
    }

    /// <summary>
    /// Structure pour représenter un coût en ressources
    /// Utilisée pour les upgrades, achats, etc.
    /// </summary>
    [Serializable]
    public class ResourceCost
    {
        public ResourceType resourceType;
        public int amount;

        public ResourceCost(ResourceType type, int amt)
        {
            resourceType = type;
            amount = amt;
        }
    }

    /// <summary>
    /// Données d'une ressource
    /// </summary>
    [Serializable]
    public class ResourceData
    {
        public ResourceType type;
        public int currentAmount;
        public int maxCapacity;

        public ResourceData(ResourceType resourceType, int capacity)
        {
            type = resourceType;
            currentAmount = 0;
            maxCapacity = capacity;
        }

        /// <summary>
        /// Vérifie si on a atteint la capacité max
        /// </summary>
        public bool IsFull()
        {
            return currentAmount >= maxCapacity;
        }

        /// <summary>
        /// Retourne le pourcentage de remplissage (0-1)
        /// </summary>
        public float GetFillPercentage()
        {
            return (float)currentAmount / maxCapacity;
        }
    }
}