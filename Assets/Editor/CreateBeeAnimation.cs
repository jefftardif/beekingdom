using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Script pour créer automatiquement l'animation des abeilles de construction
/// Usage: Menu Unity -> Tools -> Create Bee Construction Animation
/// </summary>
public class CreateBeeAnimation : MonoBehaviour
{
    [MenuItem("Tools/Create Bee Construction Animation")]
    static void CreateAnimation()
    {
        Debug.Log("🐝 Création de l'animation des abeilles...");

        // 1. Trouver tous les sprites bee_frame_XXXX
        string[] guids = AssetDatabase.FindAssets("bee_frame_ t:Sprite");
        
        if (guids.Length == 0)
        {
            Debug.LogError("❌ Aucun sprite bee_frame trouvé ! Assurez-vous d'avoir importé les 90 PNG.");
            return;
        }

        // 2. Charger et trier les sprites par nom
        List<Sprite> sprites = new List<Sprite>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                sprites.Add(sprite);
            }
        }

        // Trier par nom pour avoir l'ordre correct
        sprites = sprites.OrderBy(s => s.name).ToList();

        Debug.Log($"✅ {sprites.Count} sprites trouvés et triés");

        if (sprites.Count == 0)
        {
            Debug.LogError("❌ Erreur: Aucun sprite chargé !");
            return;
        }

        // 3. Créer l'AnimationClip
        AnimationClip clip = new AnimationClip();
        clip.frameRate = 30; // 30 FPS par défaut

        // 4. Créer les keyframes pour changer le sprite
        EditorCurveBinding spriteBinding = new EditorCurveBinding();
        spriteBinding.type = typeof(Image);
        spriteBinding.path = "";
        spriteBinding.propertyName = "m_Sprite";

        ObjectReferenceKeyframe[] spriteKeyFrames = new ObjectReferenceKeyframe[sprites.Count];
        
        for (int i = 0; i < sprites.Count; i++)
        {
            spriteKeyFrames[i] = new ObjectReferenceKeyframe();
            spriteKeyFrames[i].time = i / clip.frameRate; // Temps de chaque frame
            spriteKeyFrames[i].value = sprites[i];
        }

        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, spriteKeyFrames);

        // 5. Configurer l'animation en loop
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        // 6. Sauvegarder l'AnimationClip
        string animPath = "Assets/Animations/ConstructionBeesAnimation.anim";
        
        // Créer le dossier si nécessaire
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
        {
            AssetDatabase.CreateFolder("Assets", "Animations");
        }

        AssetDatabase.CreateAsset(clip, animPath);
        Debug.Log($"✅ Animation créée: {animPath}");

        // 7. Créer le GameObject avec Image et Animator
        GameObject go = new GameObject("ConstructionBeesIndicator");
        
        // Ajouter Canvas si nécessaire (pour UI)
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            go.transform.SetParent(canvas.transform, false);
        }

        // Ajouter RectTransform et configurer
        RectTransform rectTransform = go.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(100, 100);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;

        // Ajouter Image component
        Image image = go.AddComponent<Image>();
        image.sprite = sprites[0]; // Premier sprite par défaut
        image.preserveAspect = true;

        // Ajouter Animator
        Animator animator = go.AddComponent<Animator>();

        // 8. Créer AnimatorController
        string controllerPath = "Assets/Animations/ConstructionBeesAnimator.controller";
        UnityEditor.Animations.AnimatorController controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        
        // Ajouter l'animation au controller
        UnityEditor.Animations.AnimatorState state = controller.layers[0].stateMachine.AddState("ConstructionBees");
        state.motion = clip;

        animator.runtimeAnimatorController = controller;

        Debug.Log($"✅ AnimatorController créé: {controllerPath}");

        // 9. Créer le Prefab
        string prefabPath = "Assets/Prefabs/UI/ConstructionBeesIndicator.prefab";
        
        // Créer les dossiers si nécessaire
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        }

        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Debug.Log($"✅ Prefab créé: {prefabPath}");

        // 10. Nettoyer la scène (supprimer le GameObject temporaire)
        DestroyImmediate(go);

        // 11. Sauvegarder tous les assets
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("🎉 ANIMATION DES ABEILLES CRÉÉE AVEC SUCCÈS ! 🐝✨");
        Debug.Log($"📦 Prefab disponible dans: {prefabPath}");
        Debug.Log("➡️ Glissez-le dans HexagonBuildingSlot → Constructing Indicator");

        // Sélectionner le prefab dans le Project
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
    }
}
