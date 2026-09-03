using System.Collections.Generic;
using BeeKingdom.Audio;
using UnityEngine;

namespace BeeKingdom.Tutorial
{
    // M040-CL: real Play Mode observed world-space HUD elements (production-ready glow icons,
    // flying bee decorations) drawing on top of the FTUE dialogue panel - all of this project's
    // OnGUI panels stack in Unity's default script execution order, and this one had no explicit
    // priority. [DefaultExecutionOrder] alone did not reliably take effect at runtime (confirmed
    // via MonoImporter.GetExecutionOrder still reporting 0 after recompiles); the execution
    // order was instead pinned directly on the script's .meta via
    // MonoImporter.SetExecutionOrder(script, 32000) + AssetDatabase.SaveAssets(), which does
    // persist and take effect. This keeps that as the source of truth - if this script is ever
    // reimported from scratch, redo that call (Project Settings > Script Execution Order also
    // works and is the normal way to set/inspect it).
    public sealed class TutorialDialoguePresenter : MonoBehaviour
    {
        // M-UI-CHAMPION-PORTRAIT: championId -> portrait resource path. Reuses the existing
        // "ChampionMarchBody_<id>" transparent full-body artwork (Resources/WorldMapWave6Runtime/
        // CombatMarch/, same assets already used by the world-map combat march visuals) instead of
        // generating anything new. Only Striga/Zephyra have this asset today; any championId without
        // an entry (or whose texture fails to load) falls back to the original initial-letter badge -
        // the FTUE must never break because a portrait is missing.
        private static readonly Dictionary<string, string> ChampionPortraitResourcePaths = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            ["striga"] = "WorldMapWave6Runtime/CombatMarch/ChampionMarchBody_striga",
            ["zephyra"] = "WorldMapWave6Runtime/CombatMarch/ChampionMarchBody_zephyra",
        };

        private static readonly Dictionary<string, Texture2D> PortraitCache = new Dictionary<string, Texture2D>(System.StringComparer.OrdinalIgnoreCase);

        // M-UI-CHAMPION-VOICE: one ElevenLabs-generated clip per exact FTUE line, keyed by
        // championId+stepId (unlike ChampionVoiceBarkController's generic barks, this text is
        // fixed and specific - "any clip in the category" would say the wrong words). Convention
        // matches what Jeff was given: Resources/PremiumBeeReference/ChampionVoices/{championId}/
        // ftue/{championId}_{stepId}.mp3. Missing clips (not recorded yet) stay silent - the FTUE
        // must never depend on voice-over to function.
        private const int DialogueGuiDepth = -32000;
        private const string VoiceResourceRoot = "PremiumBeeReference/ChampionVoices";
        private const float VoiceVolumeScale = 1.15f; // same boost as ChampionVoiceBarkController
        private const float VoiceDuckBufferSeconds = 0.6f; // same buffer as ChampionVoiceBarkController
        private static readonly Dictionary<string, AudioClip> VoiceClipCache = new Dictionary<string, AudioClip>(System.StringComparer.Ordinal);

        private string _championId;
        private string _text;
        private System.Action _onContinue;
        private bool _visible;
        private string _lastVoicedKey;

        // M040-CL: script execution order (even forced via MonoImporter.SetExecutionOrder,
        // confirmed persisted/read back as 32000, with a real Play Mode restart) did NOT
        // reliably make this panel draw after every other OnGUI in the scene - the production
        // badge/bees (HiveMapProductionBootstrap, order 0) kept painting on top regardless.
        // Demonstrated architectural impossibility to rely on cross-script OnGUI ordering here.
        // Exposed instead so other draw systems can clip themselves to the real, exact panel
        // rect (GUI.BeginGroup) - true per-pixel occlusion, not a hide/show heuristic.
        public static bool IsAnyDialogueVisible { get; private set; }

        public static Rect GetCurrentPanelRect() => CalculatePanelRect();

        // The champion portrait deliberately overflows above the panel's own top edge (see
        // boxHeight below, 1.6x the panel height) so the champion reads as standing beside the
        // dialogue. Other OnGUI drawers clipping themselves away from just GetCurrentPanelRect()
        // (M040X-CL) correctly stopped covering the text/buttons row, but still painted over
        // that portrait overflow above it (real Play Mode observed: Striga's head cut off by
        // the Barrack panel). This returns the full region - text row + portrait overflow -
        // that must stay clear for the dialogue to render completely.
        public static Rect GetCurrentOcclusionRect()
        {
            Rect panel = CalculatePanelRect();
            float boxHeight = panel.height * 1.6f;
            float top = panel.yMax - boxHeight - 2f;
            return Rect.MinMaxRect(panel.x, Mathf.Min(top, panel.yMin), panel.xMax, panel.yMax);
        }

        public void Show(string championId, string text, System.Action onContinue, string stepId = null)
        {
            _championId = string.IsNullOrEmpty(championId) ? "Zephyra" : championId;
            _text = text ?? string.Empty;
            _onContinue = onContinue;
            _visible = true;
            IsAnyDialogueVisible = true;
            PlayVoiceIfAvailable(_championId, stepId);
        }

        public void Hide()
        {
            _visible = false;
            _onContinue = null;
            _lastVoicedKey = null;
            IsAnyDialogueVisible = false;
        }

        private void OnDestroy()
        {
            if (_visible) Hide();
        }

        private static Rect CalculatePanelRect()
        {
            float h = Mathf.Min(180f, Screen.height * 0.28f);
            return new Rect(12f, Screen.height - h - 12f, Screen.width - 24f, h);
        }

        private static Texture2D ChampionPortrait(string championId)
        {
            if (string.IsNullOrEmpty(championId)) return null;
            if (PortraitCache.TryGetValue(championId, out Texture2D cached)) return cached;
            Texture2D texture = ChampionPortraitResourcePaths.TryGetValue(championId, out string path)
                ? Resources.Load<Texture2D>(path)
                : null;
            PortraitCache[championId] = texture;
            return texture;
        }

        private void PlayVoiceIfAvailable(string championId, string stepId)
        {
            if (string.IsNullOrEmpty(championId) || string.IsNullOrEmpty(stepId)) return;
            string key = championId.ToLowerInvariant() + "|" + stepId;
            if (key == _lastVoicedKey) return; // avoid restarting the same line if Show() re-fires for the still-active step
            _lastVoicedKey = key;

            if (!VoiceClipCache.TryGetValue(key, out AudioClip clip))
            {
                string resourcePath = VoiceResourceRoot + "/" + championId.ToLowerInvariant() + "/ftue/" + championId.ToLowerInvariant() + "_" + stepId;
                clip = Resources.Load<AudioClip>(resourcePath);
                VoiceClipCache[key] = clip;
            }
            if (clip == null) return; // no ElevenLabs clip recorded yet for this line - silence, never blocks the FTUE

            // Same music-ducking as ChampionVoiceBarkController - without it the voice gets buried
            // under the background music (reported by Jeff during live testing).
            MusicManager.Instance?.DuckForVoice(clip.length + VoiceDuckBufferSeconds);
            AudioManager.EnsureInstance().PlaySound(clip, VoiceVolumeScale);
        }

        private void OnGUI()
        {
            if (!_visible) return;
            int previousDepth = GUI.depth;
            GUI.depth = DialogueGuiDepth;

            // bottom dialogue bar — mobile first, tap compatible
            Rect panel = CalculatePanelRect();
            float h = panel.height;
            Color prev = GUI.color;
            // GUI.Box tints the skin's default box texture, which bakes in its own partial
            // alpha (edges/corners) - the underlying screen kept bleeding through no matter how
            // opaque GUI.color was set. Drawing a flat white texture guarantees a solid fill.
            GUI.color = new Color(0.05f, 0.045f, 0.03f, 1f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = prev;

            Texture2D portraitTexture = ChampionPortrait(_championId);
            float reservedWidth;
            if (portraitTexture != null)
            {
                // Premium guide treatment: a large, aspect-preserved portrait anchored to the
                // panel's bottom-left corner, allowed to overflow above the panel top edge so the
                // champion reads as a character standing beside the dialogue rather than a small
                // icon boxed inside it. GUI.DrawTexture with ScaleToFit never distorts the artwork
                // and never intercepts input (IMGUI textures aren't raycast targets), so it can't
                // steal clicks from Suite/Passer or the gameplay behind the panel.
                //
                // M040X-CL: that overflow is only safe when nothing else is drawn above the
                // panel - real Play Mode observed another IMGUI modal (e.g. the Barrack, opened
                // right as this exact step's target) painting over the overflowing part, since
                // Unity gives no reliable way to guarantee this dialogue draws after every other
                // OnGUI (script execution order and uGUI Canvas sortingOrder both proven not to
                // help - see Docs/AI/Missions/M040X-CL-FTUE-Overlay-Occlusion-Fix.md). Falls back
                // to a portrait fully contained within the panel whenever any modal is open.
                bool contained = BeeKingdom.Playground.HiveViewProductUiPresenter.AnyModalOpenForExternalHost;
                float boxWidth = Mathf.Clamp(Screen.width * 0.30f, 170f, 300f);
                float boxHeight = contained ? h - 4f : h * 1.6f;
                Rect portraitBox = new Rect(panel.x + 4f, panel.yMax - boxHeight - 2f, boxWidth, boxHeight);
                GUI.DrawTexture(portraitBox, portraitTexture, ScaleMode.ScaleToFit, true);
                reservedWidth = boxWidth;
            }
            else
            {
                // Unknown champion / missing asset — never break the FTUE, keep the original
                // compact initial-letter badge.
                Rect portrait = new Rect(panel.x + 10f, panel.y + 10f, 64f, 64f);
                GUI.color = new Color(0.18f, 0.15f, 0.08f, 1f);
                GUI.Box(portrait, GUIContent.none);
                GUI.color = Color.white;
                GUI.Label(portrait, _championId.Substring(0, 1).ToUpper(), new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 22, fontStyle = FontStyle.Bold });
                GUI.color = prev;
                reservedWidth = portrait.width + 6f;
            }

            // Accent line only runs across the text/buttons side of the panel — starting it at
            // panel.x would cut straight across the portrait (visible through its transparent
            // gaps), which reads as a rendering glitch rather than a deliberate frame.
            GUI.color = new Color(1f, 0.65f, 0.15f, 0.9f);
            GUI.DrawTexture(new Rect(panel.x + reservedWidth, panel.y, panel.width - reservedWidth, 2f), Texture2D.whiteTexture);
            GUI.color = prev;

            float textLeft = panel.x + reservedWidth + 14f;
            float textWidth = panel.width - reservedWidth - 14f - 100f;

            Rect nameRect = new Rect(textLeft, panel.y + 10f, textWidth, 20f);
            GUI.color = new Color(1f, 0.72f, 0.22f, 1f);
            GUI.Label(nameRect, _championId.ToUpperInvariant(), new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold });
            GUI.color = prev;

            Rect textRect = new Rect(textLeft, nameRect.yMax + 2f, textWidth, h - (nameRect.height + 12f) - 10f);
            GUI.Label(textRect, _text, new GUIStyle(GUI.skin.label){wordWrap=true, fontSize=13});

            Rect btn = new Rect(panel.xMax - 84f, panel.yMax - 36f, 72f, 28f);
            GUI.color = new Color(0.95f, 0.68f, 0.15f, 1f);
            if (GUI.Button(btn, "Suite"))
            {
                var cb = _onContinue;
                Hide();
                cb?.Invoke();
            }
            GUI.color = prev;

            // skip for QA (Editor only)
#if UNITY_EDITOR
            Rect skip = new Rect(panel.xMax - 170f, panel.yMax - 36f, 78f, 28f);
            if (GUI.Button(skip, "Passer"))
            {
                Hide();
                _onContinue?.Invoke();
            }
#endif
            GUI.depth = previousDepth;
        }
    }
}
