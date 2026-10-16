using System;
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
        // {language}/{championId}_{stepId}.mp3. Missing clips (not recorded yet) stay silent - the FTUE
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
        private Vector2 _textScroll;

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
            DialogueLayout layout = CalculateLayout(Screen.width, Screen.height, Screen.safeArea);
            Rect portrait = CalculatePortraitDrawRect(layout);
            return Rect.MinMaxRect(Mathf.Min(layout.Panel.xMin, portrait.xMin),
                Mathf.Min(layout.Panel.yMin - 26f * layout.Scale, portrait.yMin),
                Mathf.Max(layout.Panel.xMax, portrait.xMax), layout.Panel.yMax + 18f * layout.Scale);
        }
        public void Show(string championId, string text, System.Action onContinue, string stepId = null)
        {
            _championId = string.IsNullOrEmpty(championId) ? "Zephyra" : championId;
            _text = text ?? string.Empty;
            _textScroll = Vector2.zero;
            _onContinue = onContinue;
            _visible = true;
            IsAnyDialogueVisible = true;
            PlayVoiceIfAvailable(_championId, stepId);
        }

        // M056A-CL : invoque quand le joueur ferme la bulle SANS completer l'etape. Cable par
        // FtueTutorialBootstrap.Awake vers DismissPresentation() pour retirer aussi la fleche
        // jaune et le bloqueur d'input plein ecran. Volontairement un Action et non une
        // reference directe au bootstrap : les deux presenters sont des composants freres
        // ajoutes dynamiquement, sans couplage entre eux.
        public Action DismissRequested;

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
            return CalculateLayout(Screen.width, Screen.height, Screen.safeArea).Panel;
        }

        private struct DialogueLayout
        {
            public Rect Panel, Portrait, Name, Text, Continue, Skip;
            public float Scale;
            public bool Compact;
        }

        // Landscape is designed at 1600x900, not at a 390-pixel phone width.
        // The old shortest-side / 390 factor enlarged the bar to 62% of a desktop.
        // Portrait keeps its independent mobile layout and scrollable long copy.
        private static DialogueLayout CalculateLayout(float width, float height, Rect safeArea)
        {
            if (safeArea.width <= 0f || safeArea.height <= 0f)
                safeArea = new Rect(0, 0, width, height);
            Rect safe = new Rect(safeArea.x, height - safeArea.yMax, safeArea.width, safeArea.height);
            bool compact = safe.width < safe.height;
            float scale = compact ? Mathf.Max(0.75f, safe.width / 390f)
                : Mathf.Clamp(Mathf.Min(safe.width / 1600f, safe.height / 900f), 0.65f, 3f);
            float margin = (compact ? 12f : 16f) * scale;
            float panelWidth = compact ? safe.width - 2f * margin
                : Mathf.Min(1240f * scale, safe.width - 2f * margin);
            float panelHeight = Mathf.Min((compact ? 300f : 290f) * scale, safe.height * 0.6f);
            float bottom = compact ? margin : Mathf.Min(90f * scale, safe.height * 0.105f);
            Rect panel = new Rect(safe.center.x - panelWidth * 0.5f,
                safe.yMax - bottom - panelHeight, panelWidth, panelHeight);
            float padding = (compact ? 20f : 24f) * scale;
            float portraitWidth = compact ? 64f * scale : Mathf.Min(360f * scale, panelWidth * 0.29f);
            float contentLeft = compact ? panel.x + padding : panel.x + portraitWidth + 30f * scale;
            Rect portrait = compact
                ? new Rect(panel.x + padding, panel.y + 14f * scale, portraitWidth, 64f * scale)
                : new Rect(panel.x + 8f * scale, panel.y + 12f * scale, portraitWidth, panel.height - 24f * scale);
            Rect name = compact
                ? new Rect(portrait.xMax + 14f * scale, panel.y + 26f * scale,
                    panel.xMax - padding - portrait.xMax - 14f * scale, 32f * scale)
                : new Rect(contentLeft, panel.y + 24f * scale, panel.xMax - padding - contentLeft,
                    Mathf.Max(26f, 36f * scale));
            float actionHeight = Mathf.Max(48f, (compact ? 52f : 58f) * scale);
            float actionWidth = (compact ? 126f : 206f) * scale;
            Rect action = new Rect(panel.xMax - padding - actionWidth,
                panel.yMax - (compact ? 20f : 22f) * scale - actionHeight, actionWidth, actionHeight);
            float skipWidth = (compact ? 98f : 136f) * scale;
            float textTop = compact ? panel.y + 96f * scale : name.yMax + 10f * scale;
            return new DialogueLayout
            {
                Panel = panel, Scale = scale, Compact = compact, Portrait = portrait, Name = name,
                Text = new Rect(contentLeft, textTop, panel.xMax - padding - contentLeft,
                    action.y - 12f * scale - textTop),
                Continue = action,
                Skip = new Rect(action.x - 12f * scale - skipWidth, action.y, skipWidth, action.height)
            };
        }

        private static Rect CalculatePortraitDrawRect(DialogueLayout layout)
        {
            if (layout.Compact || BeeKingdom.Playground.HiveViewProductUiPresenter.AnyModalOpenForExternalHost)
                return layout.Portrait;
            return new Rect(layout.Panel.x - 40f * layout.Scale, layout.Panel.y - 32f * layout.Scale,
                layout.Portrait.width + 70f * layout.Scale, layout.Panel.height + 30f * layout.Scale);
        }
        private void ContinueOrDismiss()
        {
            if (!_visible) return;
            Action callback = _onContinue;
            Hide();
            if (callback != null) callback.Invoke();
            else DismissRequested?.Invoke();
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
            string language = BeeKingdom.Playground.ChampionVoiceBarkController.AudioLanguage;
            string key = championId.ToLowerInvariant() + "|" + language + "|" + stepId;
            if (key == _lastVoicedKey) return; // avoid restarting the same line if Show() re-fires for the still-active step
            _lastVoicedKey = key;

            if (!VoiceClipCache.TryGetValue(key, out AudioClip clip))
            {
                string resourcePath = VoiceResourceRoot + "/" + championId.ToLowerInvariant() + "/" + language + "/" + championId.ToLowerInvariant() + "_" + stepId;
                clip = Resources.Load<AudioClip>(resourcePath);
                VoiceClipCache[key] = clip;
            }
            if (clip == null) return; // no ElevenLabs clip recorded yet for this line - silence, never blocks the FTUE

            // Same music-ducking as ChampionVoiceBarkController - without it the voice gets buried
            // under the background music (reported by Jeff during live testing).
            MusicManager.Instance?.DuckForVoice(clip.length + VoiceDuckBufferSeconds);
            AudioManager.EnsureInstance().PlaySound(clip, VoiceVolumeScale);
        }

        private GUIStyle _nameStyle;
        private GUIStyle _textStyle;
        private int _styleSize;

        private void EnsureStyles(DialogueLayout layout)
        {
            int size = Mathf.Max(16, Mathf.RoundToInt((layout.Compact ? 18f : 26f) * layout.Scale));
            if (_textStyle != null && _styleSize == size) return;
            _styleSize = size;
            _nameStyle = new GUIStyle(GUI.skin.label)
            {
                font = TutorialPremiumSkin.TitleFont, fontSize = Mathf.Max(18, Mathf.RoundToInt(28f * layout.Scale)),
                fontStyle = FontStyle.Normal, alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(0, 0, 0, 0)
            };
            _nameStyle.normal.textColor = new Color(1f, 0.8f, 0.37f);
            _textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = size, wordWrap = true, richText = false,
                alignment = TextAnchor.UpperLeft, padding = new RectOffset(0, 0, 0, 0)
            };
            _textStyle.normal.textColor = new Color(0.96f, 0.945f, 0.9f);
        }

        private void OnGUI()
        {
            if (!_visible) return;
            int previousDepth = GUI.depth;
            Color previousColor = GUI.color;
            Color previousContentColor = GUI.contentColor;
            Color previousBackgroundColor = GUI.backgroundColor;
            bool previousEnabled = GUI.enabled;
            try
            {
                GUI.depth = DialogueGuiDepth;
                GUI.color = Color.white;
                GUI.contentColor = Color.white;
                GUI.backgroundColor = Color.white;
                GUI.enabled = true;
                DialogueLayout layout = CalculateLayout(Screen.width, Screen.height, Screen.safeArea);
                EnsureStyles(layout);
                TutorialPremiumSkin.DrawPanel(layout.Panel, layout.Scale);

                Texture2D portraitTexture = ChampionPortrait(_championId);
                if (portraitTexture != null)
                    GUI.DrawTexture(CalculatePortraitDrawRect(layout), portraitTexture, ScaleMode.ScaleToFit, true);
                else
                {
                    Rect badge = layout.Portrait;
                    badge.height = Mathf.Min(badge.height, 72f * layout.Scale);
                    TutorialPremiumSkin.DrawBadge(badge, _championId.Substring(0, 1).ToUpperInvariant(), layout.Scale);
                }

                string speaker = _championId.ToUpperInvariant();
                GUI.Label(layout.Name, speaker, _nameStyle);
                if (!layout.Compact)
                    TutorialPremiumSkin.DrawNameRule(layout.Name,
                        _nameStyle.CalcSize(new GUIContent(speaker)).x, layout.Scale);

                Rect textRect = layout.Text;
                float contentWidth = textRect.width;
                float contentHeight = _textStyle.CalcHeight(new GUIContent(_text), contentWidth);
                if (contentHeight <= textRect.height)
                {
                    _textScroll = Vector2.zero;
                    GUI.Label(textRect, _text, _textStyle);
                }
                else
                {
                    contentWidth -= Mathf.Max(18f, 18f * layout.Scale);
                    contentHeight = _textStyle.CalcHeight(new GUIContent(_text), contentWidth);
                    if (Event.current.type == EventType.MouseDrag && textRect.Contains(Event.current.mousePosition))
                    {
                        _textScroll.y = Mathf.Clamp(_textScroll.y - Event.current.delta.y,
                            0f, Mathf.Max(0f, contentHeight - textRect.height));
                        Event.current.Use();
                    }
                    _textScroll = GUI.BeginScrollView(textRect, _textScroll,
                        new Rect(0, 0, contentWidth, contentHeight), false, true);
                    GUI.Label(new Rect(0, 0, contentWidth, contentHeight), _text, _textStyle);
                    GUI.EndScrollView();
                }

                string actionKey = _onContinue != null ? "common.continue" : "common.close";
                if (TutorialPremiumSkin.DrawButton(layout.Continue,
                    BeeKingdom.Localization.BeeLocalization.Text(actionKey), true, layout.Scale))
                    ContinueOrDismiss();
#if UNITY_EDITOR
                // QA-only skip preserves its existing behavior; no change to live FTUE progression.
                if (TutorialPremiumSkin.DrawButton(layout.Skip,
                    BeeKingdom.Localization.BeeLocalization.Text("common.skip", "Passer"), false, layout.Scale))
                    ContinueOrDismiss();
#endif
                // Blank panel areas are not click-through targets for the world underneath.
                Event current = Event.current;
                if (layout.Panel.Contains(current.mousePosition) &&
                    (current.type == EventType.MouseDown || current.type == EventType.MouseUp ||
                     current.type == EventType.ScrollWheel)) current.Use();
            }
            finally
            {
                GUI.color = previousColor;
                GUI.contentColor = previousContentColor;
                GUI.backgroundColor = previousBackgroundColor;
                GUI.enabled = previousEnabled;
                GUI.depth = previousDepth;
            }
        }
    }
}