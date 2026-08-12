using UnityEngine;
using UnityEngine.UI;

namespace BeeKingdom.UI
{
    /// <summary>
    /// Helper pour positionner facilement les hexagones sur l'image de ruche
    /// À utiliser en mode Editor pour placement rapide
    /// </summary>
    [ExecuteInEditMode]
    public class HiveLayoutHelper : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("L'image de fond de la ruche")]
        public Image hiveBackground;
        
        [Header("Hexagon Settings")]
        [Tooltip("Taille des hexagones")]
        public Vector2 hexagonSize = new Vector2(80f, 80f);
        
        [Tooltip("Espacement entre hexagones")]
        public float hexagonSpacing = 10f;
        
        [Header("Layout Pattern")]
        [Tooltip("Nombre de hexagones par rangée")]
        public int[] hexagonsPerRow = new int[] { 4, 5, 5, 4, 2 };
        
        [Tooltip("Offset vertical entre rangées")]
        public float rowOffset = 90f;
        
        [Tooltip("Offset horizontal pour rangées décalées")]
        public float alternateRowOffset = 45f;
        
        [Header("Debug")]
        [Tooltip("Afficher les positions dans la Scene view")]
        public bool showDebugGizmos = true;
        
        [Tooltip("Couleur des gizmos")]
        public Color gizmoColor = Color.yellow;

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;
            
            Gizmos.color = gizmoColor;
            
            Vector3[] positions = CalculateHexagonPositions();
            
            for (int i = 0; i < positions.Length; i++)
            {
                // Dessiner un cercle pour chaque position
                DrawGizmoCube(positions[i], hexagonSize);
                
#if UNITY_EDITOR
                // Afficher le numéro du slot
                UnityEditor.Handles.Label(positions[i], $"Slot {i}");
#endif
            }
        }

        private void DrawGizmoCube(Vector3 center, Vector2 size)
        {
            Vector3 topLeft = center + new Vector3(-size.x / 2, size.y / 2, 0);
            Vector3 topRight = center + new Vector3(size.x / 2, size.y / 2, 0);
            Vector3 bottomLeft = center + new Vector3(-size.x / 2, -size.y / 2, 0);
            Vector3 bottomRight = center + new Vector3(size.x / 2, -size.y / 2, 0);
            
            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);
        }

        /// <summary>
        /// Calcule les positions pour tous les hexagones
        /// </summary>
        public Vector3[] CalculateHexagonPositions()
        {
            int totalHexagons = 0;
            foreach (int count in hexagonsPerRow)
            {
                totalHexagons += count;
            }
            
            Vector3[] positions = new Vector3[totalHexagons];
            int currentIndex = 0;
            
            float startY = ((hexagonsPerRow.Length - 1) * rowOffset) / 2f;
            
            for (int row = 0; row < hexagonsPerRow.Length; row++)
            {
                int hexInRow = hexagonsPerRow[row];
                float rowWidth = (hexInRow - 1) * (hexagonSize.x + hexagonSpacing);
                float startX = -rowWidth / 2f;
                
                // Décalage pour rangées alternées (pattern hexagonal)
                float xOffset = (row % 2 == 1) ? alternateRowOffset : 0f;
                
                float y = startY - (row * rowOffset);
                
                for (int col = 0; col < hexInRow; col++)
                {
                    float x = startX + (col * (hexagonSize.x + hexagonSpacing)) + xOffset;
                    positions[currentIndex] = new Vector3(x, y, 0);
                    currentIndex++;
                }
            }
            
            return positions;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Bouton d'éditeur pour auto-positionner tous les slots
        /// </summary>
        [ContextMenu("Auto-Position All Hexagon Slots")]
        public void AutoPositionHexagonSlots()
        {
            HexagonBuildingSlot[] slots = GetComponentsInChildren<HexagonBuildingSlot>(true);
            
            if (slots.Length == 0)
            {
                Debug.LogWarning("⚠️ No HexagonBuildingSlot found in children!");
                return;
            }
            
            Vector3[] positions = CalculateHexagonPositions();
            
            int slotsToPosition = Mathf.Min(slots.Length, positions.Length);
            
            for (int i = 0; i < slotsToPosition; i++)
            {
                RectTransform rectTransform = slots[i].GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    UnityEditor.Undo.RecordObject(rectTransform, "Auto-Position Hexagon Slot");
                    rectTransform.anchoredPosition = positions[i];
                    rectTransform.sizeDelta = hexagonSize;
                }
            }
            
            Debug.Log($"✅ Positioned {slotsToPosition} hexagon slots!");
        }

        [ContextMenu("Create All Hexagon Slots")]
        public void CreateAllHexagonSlots()
        {
            // Trouver ou créer le container
            Transform container = transform.Find("HiveSlots");
            if (container == null)
            {
                GameObject containerObj = new GameObject("HiveSlots");
                containerObj.transform.SetParent(transform);
                RectTransform rectTransform = containerObj.AddComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.sizeDelta = Vector2.zero;
                rectTransform.anchoredPosition = Vector2.zero;
                container = containerObj.transform;
            }
            
            Vector3[] positions = CalculateHexagonPositions();
            
            for (int i = 0; i < positions.Length; i++)
            {
                // Vérifier si le slot existe déjà
                Transform existing = container.Find($"HexSlot_{i:D2}");
                if (existing != null)
                {
                    Debug.Log($"⚠️ Slot {i} already exists, skipping...");
                    continue;
                }
                
                // Créer le slot
                GameObject slotObj = new GameObject($"HexSlot_{i:D2}");
                slotObj.transform.SetParent(container);
                
                // RectTransform
                RectTransform rectTransform = slotObj.AddComponent<RectTransform>();
                rectTransform.anchoredPosition = positions[i];
                rectTransform.sizeDelta = hexagonSize;
                
                // Image (pour le raycast)
                Image image = slotObj.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0f); // Transparent
                image.raycastTarget = true;
                
                // HexagonBuildingSlot
                HexagonBuildingSlot slot = slotObj.AddComponent<HexagonBuildingSlot>();
                
                // TODO: Auto-assign slotIndex via reflection ou serialized property
                
                Debug.Log($"✅ Created HexSlot_{i:D2} at position {positions[i]}");
            }
            
            Debug.Log($"✅ Created {positions.Length} hexagon slots!");
        }
#endif
    }
}
