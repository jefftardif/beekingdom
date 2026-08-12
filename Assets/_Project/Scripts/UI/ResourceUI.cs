using UnityEngine;
using TMPro;
using BeeKingdom.Core;
using BeeKingdom.Core.Data;

namespace BeeKingdom.UI
{
    /// <summary>
    /// ResourceUI - Affiche et met à jour les ressources à l'écran
    /// </summary>
    public class ResourceUI : MonoBehaviour
    {
        [Header("Text References")]
        [SerializeField] private TextMeshProUGUI honeyText;
        [SerializeField] private TextMeshProUGUI pollenText;
        [SerializeField] private TextMeshProUGUI waxText;
        [SerializeField] private TextMeshProUGUI royalJellyText;

        [Header("Update Settings")]
        [SerializeField] private bool updateEveryFrame = true;
        [SerializeField] private float updateInterval = 0.5f; // Mise à jour toutes les 0.5 secondes

        private float updateTimer;

        #region Unity Lifecycle

        private void Start()
        {
            // Attendre un frame avant de s'initialiser
            StartCoroutine(InitializeAfterDelay());
        }


        private System.Collections.IEnumerator InitializeAfterDelay()
        {
            // Attendre que tous les managers soient initialisés
            yield return null;

            // S'abonner aux événements du ResourceManager
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnResourceChanged += OnResourceChanged;
            }

            // Mise à jour initiale
            UpdateAllResources();
        }

        private void OnDestroy()
        {
            // Se désabonner des événements
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnResourceChanged -= OnResourceChanged;
            }
        }

        private void Update()
        {
            if (updateEveryFrame)
            {
                UpdateAllResources();
            }
            else
            {
                updateTimer += Time.deltaTime;
                if (updateTimer >= updateInterval)
                {
                    updateTimer = 0f;
                    UpdateAllResources();
                }
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Appelé quand une ressource change
        /// </summary>
        private void OnResourceChanged(ResourceType type, int newAmount)
        {
            UpdateResourceDisplay(type);
        }

        #endregion

        #region Update Methods

        /// <summary>
        /// Met à jour l'affichage de toutes les ressources
        /// </summary>
        public void UpdateAllResources()
        {
            if (ResourceManager.Instance == null) return;

            UpdateResourceDisplay(ResourceType.Honey);
            UpdateResourceDisplay(ResourceType.Pollen);
            UpdateResourceDisplay(ResourceType.Wax);
            UpdateResourceDisplay(ResourceType.RoyalJelly);
        }

        /// <summary>
        /// Met à jour l'affichage d'une ressource spécifique
        /// </summary>
        private void UpdateResourceDisplay(ResourceType type)
        {
            if (ResourceManager.Instance == null) return;

            int current = ResourceManager.Instance.GetResource(type);
            int max = ResourceManager.Instance.GetMaxCapacity(type);

            string displayText = FormatResourceText(type, current, max);

            // Mettre à jour le bon TextMeshPro selon le type
            switch (type)
            {
                case ResourceType.Honey:
                    if (honeyText != null)
                        honeyText.text = displayText;
                    break;

                case ResourceType.Pollen:
                    if (pollenText != null)
                        pollenText.text = displayText;
                    break;

                case ResourceType.Wax:
                    if (waxText != null)
                        waxText.text = displayText;
                    break;

                case ResourceType.RoyalJelly:
                    if (royalJellyText != null)
                        royalJellyText.text = displayText;
                    break;
            }
        }

        /// <summary>
        /// Formate le texte d'une ressource
        /// </summary>
        private string FormatResourceText(ResourceType type, int current, int max)
        {
            string resourceName = GetResourceName(type);

            // Obtenir le taux de production
            int productionRate = ResourceManager.Instance.GetProductionRate(type);

            if (productionRate > 0)
            {
                return $"{resourceName}: {current} / {max} (+{productionRate}/s)";
            }
            else
            {
                return $"{resourceName}: {current} / {max}";
            }
        }

        /// <summary>
        /// Retourne le nom formaté d'une ressource
        /// </summary>
        private string GetResourceName(ResourceType type)
        {
            return type switch
            {
                ResourceType.Honey => "Honey",
                ResourceType.Pollen => "Pollen",
                ResourceType.Wax => "Wax",
                ResourceType.RoyalJelly => "Royal Jelly",
                _ => type.ToString()
            };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Permet de changer le mode de mise à jour
        /// </summary>
        public void SetUpdateMode(bool everyFrame)
        {
            updateEveryFrame = everyFrame;
        }

        #endregion
    }
}