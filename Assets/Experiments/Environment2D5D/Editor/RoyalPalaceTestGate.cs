#if UNITY_EDITOR
using UnityEditor;

namespace BeeKingdom.Experiments.Environment2D5D.EditorTools
{
    // ROYAL PALACE — SESSION GATE FOR LEGACY TEST AUTO-CREATION.
    //
    // Session-scoped (SessionState) flag used by the legacy Royal Palace editor tools to
    // stop their [InitializeOnLoadMethod] auto-creation for the CURRENT editor session:
    // once disabled, RoyalPalaceSiteTest, RoyalPalaceScaleTest, GroundAnchorPrototype and
    // RoyalPalaceIntegration no longer recreate their DontSave objects on domain reload.
    //
    // The flag defaults to TRUE (legacy auto-creation disabled) so that, after this cleanup
    // session, none of the old tests can silently reappear. The manual MENU items of those
    // tools still work (only their reload auto-creation is gated). SessionState survives
    // domain reloads and resets when the editor restarts, matching the "for this session"
    // requirement.
    //
    // The final scale test (RoyalPalaceFinalScaleTest) is deliberately NOT gated: it is the
    // only content meant to be visible.
    public static class RoyalPalaceTestGate
    {
        private const string DisableLegacyAutoCreateKey = "RP.DisableLegacyRoyalPalaceAutoCreate";

        public static bool LegacyAutoCreateDisabled
        {
            get { return SessionState.GetBool(DisableLegacyAutoCreateKey, true); }
            set { SessionState.SetBool(DisableLegacyAutoCreateKey, value); }
        }
    }
}
#endif
