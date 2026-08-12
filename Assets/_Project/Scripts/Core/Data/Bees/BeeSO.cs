using UnityEngine;
using BeeKingdom.Core.Data;

namespace BeeKingdom.Core.Bees
{
    /// <summary>
    /// ScriptableObject définissant un type d'abeille
    /// Contient toutes les stats et comportements d'une abeille
    /// </summary>
    [CreateAssetMenu(fileName = "New Bee", menuName = "BeeKingdom/Bee", order = 0)]
    public class BeeSO : ScriptableObject
    {
        [Header("Identity")]
        public string beeName = "Worker Bee";
        public string beeId = "worker_bee";
        [TextArea(3, 5)]
        public string description = "A hardworking bee that collects pollen.";
        public Sprite icon; // Pour plus tard

        [Header("Stats")]
        public int maxHealth = 100;
        public int attackDamage = 10;
        public float attackSpeed = 1f; // Attaques par seconde
        public float moveSpeed = 5f;
        public int defense = 5;

        [Header("Production (si applicable)")]
        public bool canProduceResources = true;
        public ResourceType producedResourceType = ResourceType.Pollen;
        public float productionRate = 5f; // Par seconde

        [Header("Costs")]
        public ResourceCost[] recruitCost; // Coût pour recruter cette abeille
        public ResourceCost[] upgradeCost; // Coût pour améliorer (pour plus tard)

        [Header("Rarity")]
        public BeeRarity rarity = BeeRarity.Common;

        [Header("Unlock Requirements")]
        public int requiredPlayerLevel = 1;
        public bool isUnlockedByDefault = true;
    }

    /// <summary>
    /// Rareté des abeilles (pour system de progression)
    /// </summary>
    public enum BeeRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
}