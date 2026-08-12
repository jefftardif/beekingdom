using UnityEngine;
using TMPro;
using BeeKingdom.Core;
using BeeKingdom.Core.Bees;

namespace BeeKingdom.UI
{
    /// <summary>
    /// UI pour afficher et recruter des abeilles
    /// </summary>
    public class BeeUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI beeCountText;

        private void Start()
        {
            // S'abonner aux événements
            if (BeeManager.Instance != null)
            {
                BeeManager.Instance.OnBeeRecruited += OnBeeRecruited;
            }

            // Mise à jour initiale
            UpdateBeeCount();
        }

        private void OnDestroy()
        {
            // Se désabonner
            if (BeeManager.Instance != null)
            {
                BeeManager.Instance.OnBeeRecruited -= OnBeeRecruited;
            }
        }

        private void OnBeeRecruited(BeeSO beeSO, BeeData beeData)
        {
            UpdateBeeCount();
        }

        private void UpdateBeeCount()
        {
            if (BeeManager.Instance != null && beeCountText != null)
            {
                int count = BeeManager.Instance.GetTotalBeeCount();
                beeCountText.text = $"Bees: {count}";
            }
        }
    }
}