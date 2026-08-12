using UnityEngine;
using UnityEngine.UI;

public class SimpleSlotMonitor : MonoBehaviour
{
    [Header("Configuration")]
    public GameObject constructingIndicator;

    [Header("Debug")]
    [SerializeField] private bool logStateChanges = false;

    private bool wasActive = false;

    void Start()
    {
        if (constructingIndicator != null)
        {
            LogStatus();
        }
        else
        {
            Debug.LogWarning("Assignez Constructing Indicator dans Inspector!");
        }
    }

    void Update()
    {
        if (constructingIndicator == null) return;
        
        bool isActive = constructingIndicator.activeSelf;
        
        if (isActive != wasActive)
        {
            wasActive = isActive;
            
            if (isActive)
            {
                if (logStateChanges)
                {
                    Debug.Log(gameObject.name + ": Animation ACTIVEE!");
                }
                LogStatus();
            }
            else
            {
                if (logStateChanges)
                {
                    Debug.Log(gameObject.name + ": Animation DESACTIVEE!");
                }
            }
        }
    }

    void LogStatus()
    {
        if (constructingIndicator == null) return;
        if (!logStateChanges) return;
        
        Debug.Log("===============================");
        Debug.Log("Nom: " + constructingIndicator.name);
        Debug.Log("Actif: " + constructingIndicator.activeSelf);
        Debug.Log("Position: " + constructingIndicator.transform.position);
        
        var animator = constructingIndicator.GetComponent<Animator>();
        if (animator != null)
        {
            Debug.Log("Animator trouve: " + animator.enabled);
            if (animator.runtimeAnimatorController != null)
            {
                Debug.Log("Controller: " + animator.runtimeAnimatorController.name);
            }
        }
        else
        {
            Debug.LogWarning("Pas d'Animator sur l'indicateur!");
        }
        
        var image = constructingIndicator.GetComponent<Image>();
        if (image != null)
        {
            if (image.sprite != null)
            {
                Debug.Log("Image trouvee: " + image.sprite.name);
            }
        }
        
        Debug.Log("===============================");
    }

    [ContextMenu("Force Show Animation")]
    public void ForceShow()
    {
        if (constructingIndicator != null)
        {
            constructingIndicator.SetActive(true);
            LogStatus();
        }
        else
        {
            Debug.LogError("Assignez Constructing Indicator d'abord!");
        }
    }
}
