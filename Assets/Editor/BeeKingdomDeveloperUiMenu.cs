#if UNITY_EDITOR
using BeeKingdom.Playground;
using UnityEditor;
using UnityEngine;

public static class BeeKingdomDeveloperUiMenu
{
    private const string MenuPath = "Bee Kingdom/Development/Show Developer Controls";

    [MenuItem(MenuPath)]
    public static void Toggle()
    {
        bool enabled = SplashDevelopmentSceneConfig.IsDevelopmentMenuVisible();
        EditorPrefs.SetBool(SplashDevelopmentSceneConfig.DeveloperMenuPreferenceKey, !enabled);
        Debug.Log(!enabled
            ? "BeeKingdom QA controls enabled for this editor only."
            : "BeeKingdom QA controls hidden from gameplay.");
    }

    [MenuItem(MenuPath, true)]
    public static bool Validate()
    {
        Menu.SetChecked(MenuPath, SplashDevelopmentSceneConfig.IsDevelopmentMenuVisible());
        return true;
    }
}
#endif
