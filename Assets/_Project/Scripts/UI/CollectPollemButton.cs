using UnityEngine;
using BeeKingdom.Core;
using BeeKingdom.Core.Data;

namespace BeeKingdom.UI
{
    /// <summary>
    /// Bouton simple pour collecter du pollen
    /// </summary>
    public class CollectPollenButton : MonoBehaviour
    {
        [Header("Collection Settings")]
        [SerializeField] private int pollenAmount = 10;

        /// <summary>
        /// Appelé quand le bouton est cliqué
        /// Cette méthode sera connectée au bouton dans l'Inspector
        /// </summary>
        public void OnButtonClick()
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.AddResource(ResourceType.Pollen, pollenAmount);
            }
            else
            {
                Debug.LogError("ResourceManager not found!");
            }
        }
    }
}
