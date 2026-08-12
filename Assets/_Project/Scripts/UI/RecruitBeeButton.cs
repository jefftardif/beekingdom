using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BeeKingdom.Core;
using BeeKingdom.Core.Bees;
using BeeKingdom.Core.Data;

namespace BeeKingdom.UI
{
    /// <summary>
    /// Bouton pour recruter une abeille spécifique
    /// Affiche le coût et se désactive si pas assez de ressources
    /// </summary>
    public class RecruitBeeButton : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private string beeId = "worker_bee";

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI buttonText;

        private Button button;
        private BeeSO beeSO;
        private string baseName;

        private void Awake()
        {
            button = GetComponent<Button>();

            // Trouver le texte si pas assigné
            if (buttonText == null)
            {
                buttonText = GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        private void Start()
        {
            // Connecter le clic immédiatement
            if (button != null)
            {
                button.onClick.AddListener(OnButtonClick);
            }

            // S'abonner aux événements de ressources
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnResourceChanged += OnResourceChanged;
            }

            // Attendre que BeeManager soit prêt
            StartCoroutine(InitializeWhenReady());
        }

        private System.Collections.IEnumerator InitializeWhenReady()
        {
            // Attendre que BeeManager soit initialisé
            while (BeeManager.Instance == null || BeeManager.Instance.GetBeeSO(beeId) == null)
            {
                yield return null;
            }

            // Maintenant on peut initialiser
            beeSO = BeeManager.Instance.GetBeeSO(beeId);
            if (beeSO != null)
            {
                baseName = beeSO.beeName;
                UpdateButtonText();
                UpdateButtonState();
            }
        }

        private void OnDestroy()
        {
            // Se désabonner
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnResourceChanged -= OnResourceChanged;
            }
        }

        private void OnResourceChanged(ResourceType type, int amount)
        {
            // Mettre à jour l'état du bouton quand les ressources changent
            UpdateButtonState();
        }

        private void OnButtonClick()
        {
            if (BeeManager.Instance != null)
            {
                bool success = BeeManager.Instance.RecruitBee(beeId);

                if (!success)
                {
                    Debug.Log("⚠️ Cannot recruit bee (not enough resources or requirements not met)");
                }
                else
                {
                    // Mettre à jour immédiatement après recrutement
                    UpdateButtonState();
                }
            }
        }

        private void UpdateButtonText()
        {
            if (buttonText == null || beeSO == null)
            {
                Debug.LogWarning("⚠️ Cannot update button text - buttonText or beeSO is null");
                return;
            }

            // Construire le texte avec les coûts
            string costText = GetCostText();
            string finalText = $"{baseName}\n{costText}";

            buttonText.text = finalText;
        }

        private string GetCostText()
        {
            if (beeSO == null || beeSO.recruitCost == null || beeSO.recruitCost.Length == 0)
            {
                return "";
            }

            string result = "(";
            for (int i = 0; i < beeSO.recruitCost.Length; i++)
            {
                var cost = beeSO.recruitCost[i];
                string icon = GetResourceIcon(cost.resourceType);
                result += $"{cost.amount} {icon}";

                if (i < beeSO.recruitCost.Length - 1)
                {
                    result += ", ";
                }
            }
            result += ")";

            return result;
        }

        private void UpdateButtonState()
        {
            if (button == null || beeSO == null) return;

            // Vérifier si on peut payer
            bool canAfford = ResourceManager.Instance != null &&
                           ResourceManager.Instance.CanAfford(beeSO.recruitCost);

            // Activer/désactiver le bouton
            button.interactable = canAfford;
        }

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
    }
}
