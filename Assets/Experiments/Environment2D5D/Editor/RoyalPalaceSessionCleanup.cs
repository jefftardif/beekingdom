#if UNITY_EDITOR
using BeeKingdom.Experiments.Environment2D5D;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Experiments.Environment2D5D.EditorTools
{
    // ROYAL PALACE — SESSION CLEANUP OF LEGACY TESTS + EXCLUSIVE FINAL SCALE TEST.
    //
    // Editor-only (DontSave, nothing serialized). Runs on domain reload (target scene only)
    // and from a menu item. It:
    //   1. Destroys (in memory, DontSave only) EVERY object created by the previous Royal
    //      Palace missions' tools:
    //        - RoyalPalaceSiteTest        -> group  "ROYAL_PALACE_SITE_TEST"
    //        - RoyalPalaceScaleTest       -> group  "ROYAL_PALACE_SCALE_TEST"
    //        - RoyalPalaceFinalScaleTest  -> group  "ROYAL_PALACE_FINAL_SCALE_TEST" (old instance)
    //        - GroundAnchorPrototype      -> root   "GROUND_ANCHOR_PROTOTYPE_ROYAL_PALACE"
    //        - GroundAnchorDiagnostic     -> root   "__GROUND_ANCHOR_DIAG__"
    //        - RoyalPalaceIntegration     -> root   "ROYAL_PALACE_013" (real building at scale
    //                                                1.0; removed so the scene shows ONLY the
    //                                                three final scale variants)
    //   2. Sets the session gate so none of those legacy tools can recreate themselves on
    //      reload (their manual menus still work).
    //   3. Rebuilds the ONLY visible Royal Palace content: the FINAL SCALE TEST (0.35 / 0.40
    //      / 0.45 on site B).
    //
    // Nothing else is touched: placeholders, layouts, LivingHive.unity, PNGs/.meta, import
    // settings, GroundSurfaceResolver and every other permanent asset stay unchanged.
    public static class RoyalPalaceSessionCleanup
    {
        private const string TargetScenePath = "Assets/Experiments/Environment2D5D/Scenes/Environment2D5D_SpatialV3.unity";

        // Roots created by the legacy tools (all DontSave). Destroyed on reload + via menu.
        private static readonly string[] LegacyRootNames =
        {
            "ROYAL_PALACE_SITE_TEST",
            "ROYAL_PALACE_SCALE_TEST",
            "ROYAL_PALACE_FINAL_SCALE_TEST",
            "GROUND_ANCHOR_PROTOTYPE_ROYAL_PALACE",
            "__GROUND_ANCHOR_DIAG__",
            "ROYAL_PALACE_013"
        };

        [InitializeOnLoadMethod]
        private static void AutoEnforceCleanScene()
        {
            if (Application.isPlaying) return;
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != TargetScenePath) return;

            // Gate first: legacy tools must never auto-recreate for this session.
            RoyalPalaceTestGate.LegacyAutoCreateDisabled = true;

            int removed = DestroyLegacyRoots();
            // Only rebuild the final scale test when the gate allows legacy content
            // (disabled => the Building Placement Editor is the only auto-creator).
            if (!RoyalPalaceTestGate.LegacyAutoCreateDisabled)
            {
                RoyalPalaceFinalScaleTest.Rebuild();
            }
            if (removed > 0)
            {
                Debug.Log("[ROYAL_PALACE_CLEANUP] " + removed + " objet(s) ancien(s) supprimé(s) " +
                          "(DontSave, scène intacte) ; auto-création ancienne désactivée pour la session.");
            }
        }

        [MenuItem("BeeKingdom/Experiments/Royal Palace Final Scale Test/Clean & Show Final Scale Test")]
        public static void CleanAndShow()
        {
            RoyalPalaceTestGate.LegacyAutoCreateDisabled = true;
            int removed = DestroyLegacyRoots();
            RoyalPalaceFinalScaleTest.Rebuild();
            Debug.Log("[ROYAL_PALACE_CLEANUP] Nettoyage manuel : " + removed + " objet(s) ancien(s) supprimé(s) ; " +
                      "final scale test (0.35 / 0.40 / 0.45) affiché sur le site B.");
        }

        private static int DestroyLegacyRoots()
        {
            int removed = 0;
            GameObject[] all = Object.FindObjectsOfType<GameObject>();
            for (int i = 0; i < all.Length; i++)
            {
                GameObject go = all[i];
                if (go == null) continue;
                // Only DontSave objects (never permanent scene objects).
                if ((go.hideFlags & HideFlags.DontSave) == 0) continue;
                if (!IsLegacyRoot(go.name)) continue;
                Object.DestroyImmediate(go);
                removed++;
            }
            return removed;
        }

        private static bool IsLegacyRoot(string name)
        {
            for (int i = 0; i < LegacyRootNames.Length; i++)
            {
                if (LegacyRootNames[i] == name) return true;
            }
            return false;
        }
    }
}
#endif
