using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeeKingdom.Tutorial
{
    public interface ITutorialTargetProvider
    {
        string TargetId { get; }
        bool TryGetWorldPosition(out Vector3 worldPos);
        bool TryGetScreenRect(Camera cam, out Rect screenRect);
        Transform TargetTransform { get; }
    }

    public sealed class TutorialTargetRegistry
    {
        private static TutorialTargetRegistry _instance;
        public static TutorialTargetRegistry Instance => _instance ??= new TutorialTargetRegistry();

        private readonly Dictionary<string, ITutorialTargetProvider> _providers = new Dictionary<string, ITutorialTargetProvider>(StringComparer.Ordinal);
        private readonly Dictionary<string, Func<RectTransform>> _uiProviders = new Dictionary<string, Func<RectTransform>>(StringComparer.Ordinal);
        // M038C-CL: the actual IMGUI-drawn Rect for a target, published by the SAME code
        // that draws the real control (e.g. DrawBarrackTopBar's upgradeBadge) — every frame
        // it draws, so this is always the true on-screen geometry, never a guessed fraction.
        // Frame-stamped: a Rect not refreshed this frame or last means that control isn't
        // currently on screen (its panel closed, or was never opened), so callers must not
        // treat a stale value as "found" — see TryGetTargetPosition below.
        private readonly Dictionary<string, (Rect rect, int frame)> _screenRects = new Dictionary<string, (Rect, int)>(StringComparer.Ordinal);

        public void RegisterScreenRect(string targetId, Rect guiRect)
        {
            if (string.IsNullOrEmpty(targetId)) return;
            _screenRects[targetId] = (guiRect, Time.frameCount);
        }

        public bool TryResolveScreenRect(string targetId, out Rect rect)
        {
            rect = default;
            if (string.IsNullOrEmpty(targetId)) return false;
            if (!_screenRects.TryGetValue(targetId, out var entry)) return false;
            if (Time.frameCount - entry.frame > 1) return false; // stale — not drawn this frame/last, panel is closed
            rect = entry.rect;
            return true;
        }

        public void Register(ITutorialTargetProvider provider)
        {
            if (provider == null || string.IsNullOrEmpty(provider.TargetId)) return;
            _providers[provider.TargetId] = provider;
        }

        public void Unregister(string targetId)
        {
            if (string.IsNullOrEmpty(targetId)) return;
            _providers.Remove(targetId);
        }

        public void RegisterUi(string targetId, Func<RectTransform> resolver)
        {
            if (string.IsNullOrEmpty(targetId) || resolver == null) return;
            _uiProviders[targetId] = resolver;
        }

        public void UnregisterUi(string targetId) => _uiProviders.Remove(targetId);

        public bool TryResolve(string targetId, out ITutorialTargetProvider provider) => _providers.TryGetValue(targetId, out provider);

        public bool TryResolveUi(string targetId, out RectTransform rect)
        {
            rect = null;
            if (_uiProviders.TryGetValue(targetId, out var fn))
            {
                try { rect = fn(); } catch { rect = null; }
                return rect != null;
            }
            return false;
        }

        public bool TryGetTargetPosition(string targetId, Camera cam, out Vector2 screenPos, out RectTransform uiRect)
        {
            screenPos = default;
            uiRect = null;
            if (string.IsNullOrEmpty(targetId)) return false;
            // Real IMGUI-published geometry first — the actual control being drawn this frame.
            if (TryResolveScreenRect(targetId, out Rect guiRect))
            {
                screenPos = new Vector2(guiRect.center.x, guiRect.center.y);
                return true;
            }
            // UI first
            if (TryResolveUi(targetId, out uiRect) && uiRect != null)
            {
                Vector3[] corners = new Vector3[4];
                uiRect.GetWorldCorners(corners);
                Vector3 center = (corners[0] + corners[2]) * 0.5f;
                if (cam != null) screenPos = cam.WorldToScreenPoint(center);
                else screenPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                screenPos.y = Screen.height - screenPos.y; // for OnGUI
                return true;
            }
            if (TryResolve(targetId, out var provider) && provider.TryGetWorldPosition(out Vector3 wpos))
            {
                if (cam != null)
                {
                    Vector3 sp = cam.WorldToScreenPoint(wpos);
                    screenPos = new Vector2(sp.x, Screen.height - sp.y);
                    return sp.z > 0;
                }
                return false;
            }
            // Fallback: scan building interaction components (no registration required) — mobile first, no Find by name.
            // M038B-CL fix: BuildingInteractionComponent.BuildingType holds the UPPER_SNAKE_CASE runtime
            // constant (e.g. "ROYAL_PALACE", "BARRACK", "RESEARCH" - see BuildingTypes.cs), never the
            // lowercase legacy/hotspot key ("administration_core", "guard_post", "research_node") that
            // every FtueTutorialRegistry target id uses. A naive ToLowerInvariant() compare here NEVER
            // matched anything real - confirmed live in Play Mode: the arrow's target/visible flags were
            // both correct but nothing ever rendered because this scan silently found zero matches for
            // every single Part1/Part2 building target. BuildingMappingTable already holds the canonical
            // translation (used elsewhere for exactly this), so route through it instead of guessing.
            if (targetId.StartsWith("building.", StringComparison.Ordinal))
            {
                string buildingKey = targetId.Substring("building.".Length);
                var all = UnityEngine.Object.FindObjectsByType<BeeKingdom.Buildings.Interaction.BuildingInteractionComponent>(FindObjectsInactive.Include);
                for (int i = 0; i < all.Length; i++)
                {
                    var c = all[i];
                    if (c == null || string.IsNullOrEmpty(c.BuildingType)) continue;
                    if (!BeeKingdom.Buildings.Interaction.BuildingMappingTable.TryGetByBuildingType(c.BuildingType, out var mapped)) continue;
                    if (!string.Equals(mapped.LegacyKey, buildingKey, StringComparison.Ordinal)) continue;
                    Vector3 fallbackWpos = c.transform.position + Vector3.up * 0.7f;
                    if (cam != null)
                    {
                        Vector3 sp = cam.WorldToScreenPoint(fallbackWpos);
                        screenPos = new Vector2(sp.x, Screen.height - sp.y);
                        return sp.z > 0;
                    }
                }
            }
            if (targetId == FtueTutorialRegistry.TargetUpgradeButton)
            {
                // Fallback for IMGUI upgrade button — bottom center, resolution independent
                screenPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.75f);
                return true;
            }
            // M038-CL: same IMGUI-fallback precedent as TargetUpgradeButton above — these panels are
            // GUILayout/OnGUI, not uGUI RectTransform, so there is no RectTransform to register.
            if (targetId == FtueTutorialRegistry.TargetResearchStartButton || targetId == FtueTutorialRegistry.TargetTrainingStartButton)
            {
                screenPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.75f);
                return true;
            }
            if (targetId == FtueTutorialRegistry.TargetArmyMenu)
            {
                screenPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.92f);
                return true;
            }
            return false;
        }

        public void ClearForTests()
        {
            _providers.Clear();
            _uiProviders.Clear();
            _screenRects.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() => _instance = null;
    }

    // Adapter for building GameObjects (BoxCollider hit zones)
    public sealed class BuildingTutorialTarget : ITutorialTargetProvider
    {
        public string TargetId { get; }
        private readonly Transform _transform;
        public Transform TargetTransform => _transform;
        public BuildingTutorialTarget(string targetId, Transform t)
        {
            TargetId = targetId;
            _transform = t;
        }
        public bool TryGetWorldPosition(out Vector3 worldPos)
        {
            if (_transform != null) { worldPos = _transform.position + Vector3.up * 0.7f; return true; }
            worldPos = default; return false;
        }
        public bool TryGetScreenRect(Camera cam, out Rect screenRect)
        {
            screenRect = default;
            if (_transform == null || cam == null) return false;
            Vector3 sp = cam.WorldToScreenPoint(_transform.position);
            if (sp.z <= 0) return false;
            screenRect = new Rect(sp.x - 40, Screen.height - sp.y - 40, 80, 80);
            return true;
        }
    }
}
