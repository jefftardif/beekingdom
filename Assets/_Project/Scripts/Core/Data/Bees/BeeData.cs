using System;

namespace BeeKingdom.Core.Bees
{
    /// <summary>
    /// Données runtime d'une abeille possédée par le joueur
    /// Instance d'un BeeSO
    /// </summary>
    [Serializable]
    public class BeeData
    {
        public string beeId;           // ID du BeeSO
        public string instanceId;      // ID unique de cette instance
        public int currentHealth;
        public int level;
        public float experience;

        // Pour la sauvegarde
        public BeeData(string beeId, int maxHealth)
        {
            this.beeId = beeId;
            this.instanceId = Guid.NewGuid().ToString();
            this.currentHealth = maxHealth;
            this.level = 1;
            this.experience = 0f;
        }
    }
}