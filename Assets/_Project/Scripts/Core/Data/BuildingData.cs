using System;
using UnityEngine;

namespace BeeKingdom.Core.Data
{
    /// <summary>
    /// Données d'un bâtiment construit dans la ruche
    /// Sérialisable pour la sauvegarde
    /// </summary>
    [Serializable]
    public class BuildingData
    {
        public int slotIndex;                    // Position dans la ruche (0-19)
        public BuildingType buildingType;        // Type de bâtiment
        public int level;                        // Niveau actuel
        public bool isConstructing;              // En construction ?
        public DateTime constructionStartTime;   // Quand la construction a commencé
        public float constructionDuration;       // Durée totale (secondes)

        /// <summary>
        /// Constructeur pour un slot vide
        /// </summary>
        public BuildingData()
        {
        }

        public BuildingData(int slotIndex)
        {
            this.slotIndex = slotIndex;
            this.buildingType = BuildingType.Empty;
            this.level = 0;
            this.isConstructing = false;
        }

        /// <summary>
        /// Constructeur pour démarrer une construction
        /// </summary>
        public BuildingData(int slotIndex, BuildingType type, float duration)
        {
            this.slotIndex = slotIndex;
            this.buildingType = type;
            this.level = 1;
            this.isConstructing = true;
            this.constructionStartTime = DateTime.Now;
            this.constructionDuration = duration;
        }

        public BuildingData(int slotIndex, BuildingType type, int level, bool isConstructing, DateTime constructionStartTime, float constructionDuration)
        {
            this.slotIndex = slotIndex;
            this.buildingType = type;
            this.level = level;
            this.isConstructing = isConstructing;
            this.constructionStartTime = constructionStartTime;
            this.constructionDuration = constructionDuration;
        }

        /// <summary>
        /// Vérifie si la construction est terminée
        /// </summary>
        public bool IsConstructionComplete()
        {
            if (!isConstructing) return true;

            TimeSpan elapsed = DateTime.Now - constructionStartTime;
            return elapsed.TotalSeconds >= constructionDuration;
        }

        /// <summary>
        /// Obtient le temps restant de construction en secondes
        /// </summary>
        public float GetRemainingConstructionTime()
        {
            if (!isConstructing) return 0f;

            TimeSpan elapsed = DateTime.Now - constructionStartTime;
            float remaining = constructionDuration - (float)elapsed.TotalSeconds;
            return Mathf.Max(0f, remaining);
        }

        /// <summary>
        /// Obtient le pourcentage de complétion (0-1)
        /// </summary>
        public float GetConstructionProgress()
        {
            if (!isConstructing) return 1f;
            if (constructionDuration <= 0) return 1f;

            TimeSpan elapsed = DateTime.Now - constructionStartTime;
            return Mathf.Clamp01((float)elapsed.TotalSeconds / constructionDuration);
        }

        /// <summary>
        /// Termine instantanément la construction
        /// </summary>
        public void CompleteConstruction()
        {
            isConstructing = false;
        }

        /// <summary>
        /// Démarre un upgrade vers le niveau suivant
        /// </summary>
        public void StartUpgrade(float duration)
        {
            level++;
            isConstructing = true;
            constructionStartTime = DateTime.Now;
            constructionDuration = duration;
        }

        /// <summary>
        /// Détruit le bâtiment (retourne au slot vide)
        /// </summary>
        public void Demolish()
        {
            buildingType = BuildingType.Empty;
            level = 0;
            isConstructing = false;
        }

        /// <summary>
        /// Vérifie si le slot est vide
        /// </summary>
        public bool IsEmpty()
        {
            return buildingType == BuildingType.Empty;
        }
    }
}
