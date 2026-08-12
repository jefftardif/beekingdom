using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

namespace BeeKingdom.UI
{
    /// <summary>
    /// Bouton individuel du menu inférieur
    /// Gère l'apparence et les événements de clic
    /// </summary>
    public class BottomMenuButton : MonoBehaviour, IPointerClickHandler
    {
        [Header("Visual Elements")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private GameObject notificationBadge; // Badge pour notifications (optionnel)

        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        [SerializeField] private Color selectedColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color normalTextColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        [SerializeField] private Color selectedTextColor = new Color(1f, 0.9f, 0.3f, 1f); // Doré

        [Header("Scale Effect")]
        [SerializeField] private bool useScaleEffect = true;
        [SerializeField] private float selectedScale = 1.1f;
        [SerializeField] private float scaleSpeed = 5f;

        private bool isSelected = false;
        private float targetScale = 1f;

        // Event pour le clic
        public Action OnButtonClicked;

        private void Update()
        {
            // Animation de scale
            if (useScaleEffect)
            {
                float currentScale = transform.localScale.x;
                float newScale = Mathf.Lerp(currentScale, targetScale, Time.deltaTime * scaleSpeed);
                transform.localScale = Vector3.one * newScale;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnButtonClicked?.Invoke();
        }

        /// <summary>
        /// Définir l'état sélectionné du bouton
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;

            // Changer les couleurs
            if (iconImage != null)
            {
                iconImage.color = selected ? selectedColor : normalColor;
            }

            if (labelText != null)
            {
                labelText.color = selected ? selectedTextColor : normalTextColor;
            }

            // Animation de scale
            targetScale = selected ? selectedScale : 1f;

            // Effet de background (optionnel)
            if (backgroundImage != null)
            {
                backgroundImage.color = selected ? new Color(1f, 1f, 1f, 0.2f) : new Color(1f, 1f, 1f, 0f);
            }
        }

        /// <summary>
        /// Afficher/Cacher le badge de notification
        /// </summary>
        public void SetNotificationVisible(bool visible)
        {
            if (notificationBadge != null)
            {
                notificationBadge.SetActive(visible);
            }
        }

        /// <summary>
        /// Changer l'icône du bouton
        /// </summary>
        public void SetIcon(Sprite icon)
        {
            if (iconImage != null)
            {
                iconImage.sprite = icon;
            }
        }

        /// <summary>
        /// Changer le texte du bouton
        /// </summary>
        public void SetLabel(string text)
        {
            if (labelText != null)
            {
                labelText.text = text;
            }
        }
    }
}
