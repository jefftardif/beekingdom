using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeeKingdom.UI
{
    public enum FeedbackType
    {
        ResourceCollected,
        BuildingCompleted,
        ResearchCompleted,
        RewardReceived,
        PowerGained,
        ExperienceGained,
        TroopsTrained,
        QuestCompleted
    }

    public enum FeedbackIconType
    {
        Honey,
        Wax,
        Pollen,
        Power,
        Experience,
        Generic
    }

    public sealed class FeedbackFloatingText
    {
        public string Text { get; }
        public Vector2 Position { get; }
        public Color Color { get; }
        public float Lifetime { get; }
        public float Elapsed { get; private set; }
        public bool IsAlive => Elapsed < Lifetime;

        public FeedbackFloatingText(string text, Vector2 position, Color color, float lifetime = 1.2f)
        {
            Text = text;
            Position = position;
            Color = color;
            Lifetime = lifetime;
        }

        public void Tick(float dt)
        {
            Elapsed += dt;
        }

        public float Progress01 => Mathf.Clamp01(Elapsed / Lifetime);
        public float Alpha => 1f - Progress01;
        public float YOffset => Mathf.Lerp(0f, -40f, Progress01);
    }

    public sealed class FeedbackIconBurst
    {
        public FeedbackIconType IconType { get; }
        public Vector2 StartPosition { get; }
        public Vector2 TargetPosition { get; }
        public int Count { get; }
        public float Lifetime { get; }
        public float Elapsed { get; private set; }
        public bool IsAlive => Elapsed < Lifetime;

        private readonly List<Vector2> iconPositions = new List<Vector2>();
        private readonly List<float> iconScales = new List<float>();
        private readonly List<float> iconAlphas = new List<float>();

        public FeedbackIconBurst(FeedbackIconType iconType, Vector2 startPos, Vector2 targetPos, int count = 6, float lifetime = 0.9f)
        {
            IconType = iconType;
            StartPosition = startPos;
            TargetPosition = targetPos;
            Count = count;
            Lifetime = lifetime;

            for (int i = 0; i < count; i++)
            {
                float angle = (float)i / count * Mathf.PI * 2f;
                float radius = 12f;
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                iconPositions.Add(startPos + offset);
                iconScales.Add(0.3f);
                iconAlphas.Add(1f);
            }
        }

        public void Tick(float dt)
        {
            Elapsed += dt;
            float t = Progress01;
            for (int i = 0; i < Count; i++)
            {
                Vector2 current = Vector2.Lerp(iconPositions[i], TargetPosition, t);
                iconPositions[i] = current;
                iconScales[i] = Mathf.Lerp(0.6f, 0.15f, t);
                iconAlphas[i] = 1f - t;
            }
        }

        public float Progress01 => Mathf.Clamp01(Elapsed / Lifetime);
        public IReadOnlyList<Vector2> Positions => iconPositions;
        public IReadOnlyList<float> Scales => iconScales;
        public IReadOnlyList<float> Alphas => iconAlphas;
    }

    public sealed class FeedbackPulse
    {
        public Vector2 Position { get; }
        public float Radius { get; }
        public Color Color { get; }
        public float Lifetime { get; }
        public float Elapsed { get; private set; }
        public bool IsAlive => Elapsed < Lifetime;

        public FeedbackPulse(Vector2 position, float radius, Color color, float lifetime = 0.5f)
        {
            Position = position;
            Radius = radius;
            Color = color;
            Lifetime = lifetime;
        }

        public void Tick(float dt)
        {
            Elapsed += dt;
        }

        public float Progress01 => Mathf.Clamp01(Elapsed / Lifetime);
        public float Alpha => 1f - Progress01;
        public float CurrentRadius => Mathf.Lerp(0f, Radius, Progress01);
    }

    public sealed class FeedbackHighlight
    {
        public Rect Rect { get; }
        public Color Color { get; }
        public float Lifetime { get; }
        public float Elapsed { get; private set; }
        public bool IsAlive => Elapsed < Lifetime;

        public FeedbackHighlight(Rect rect, Color color, float lifetime = 0.6f)
        {
            Rect = rect;
            Color = color;
            Lifetime = lifetime;
        }

        public void Tick(float dt)
        {
            Elapsed += dt;
        }

        public float Progress01 => Mathf.Clamp01(Elapsed / Lifetime);
        public float Alpha => 1f - Progress01;
        public float Scale => Mathf.Lerp(1f, 1.03f, Progress01);
    }

    public static class UIFeedbackSystem
    {
        private static readonly List<FeedbackFloatingText> floatingTexts = new List<FeedbackFloatingText>(32);
        private static readonly List<FeedbackIconBurst> iconBursts = new List<FeedbackIconBurst>(16);
        private static readonly List<FeedbackPulse> pulses = new List<FeedbackPulse>(16);
        private static readonly List<FeedbackHighlight> highlights = new List<FeedbackHighlight>(16);

        private static readonly Dictionary<string, float> resourceNotificationCooldowns = new Dictionary<string, float>(8);
        private static readonly Dictionary<string, float> pendingResourceAmounts = new Dictionary<string, float>(8);
        private static readonly Dictionary<string, float> pendingResourceTimers = new Dictionary<string, float>(8);
        private static readonly List<string> pendingResourceFlushKeys = new List<string>(8);
        private const float NotificationCooldown = 0.25f;

        public static void ShowFloatingText(string text, Vector2 position, Color color, float lifetime = 1.2f)
        {
            floatingTexts.Add(new FeedbackFloatingText(text, position, color, lifetime));
        }

        public static void ShowResourceCollected(string resourceName, float amount, Vector2 buildingPos, Vector2 hudPos)
        {
            string key = resourceName;
            float now = UIAnimationLibrary.Now;

            if (resourceNotificationCooldowns.TryGetValue(key, out float lastTime) && now - lastTime < NotificationCooldown)
            {
                pendingResourceAmounts[key] = pendingResourceAmounts.GetValueOrDefault(key) + amount;
                pendingResourceTimers[key] = now;
                return;
            }

            resourceNotificationCooldowns[key] = now;
            ShowFloatingText($"+{amount:0.#}", buildingPos + Vector2.up * 12f, ResourceColor(resourceName));
            SpawnIconBurst(ResourceIcon(resourceName), buildingPos, hudPos);
        }

        public static void ShowBuildingCompleted(Vector2 buildingPos, Vector2 hudPos)
        {
            pulses.Add(new FeedbackPulse(buildingPos, 36f, new Color(1f, 0.85f, 0.25f, 0.8f), 0.6f));
            highlights.Add(new FeedbackHighlight(new Rect(buildingPos.x - 24f, buildingPos.y - 24f, 48f, 48f), new Color(1f, 0.85f, 0.25f, 0.5f), 0.6f));
            ShowFloatingText("Terminé", buildingPos + Vector2.up * 16f, new Color(1f, 0.9f, 0.3f), 1f);
        }

        public static void ShowResearchCompleted(Vector2 buildingPos, Vector2 hudPos)
        {
            ShowBuildingCompleted(buildingPos, hudPos);
        }

        public static void ShowRewardReceived(Vector2 startPos, Vector2 hudPos, FeedbackIconType iconType, string label, float amount)
        {
            ShowFloatingText($"+{amount:0.#} {label}", startPos + Vector2.up * 16f, new Color(1f, 0.85f, 0.25f));
            SpawnIconBurst(iconType, startPos, hudPos, 8);
        }

        public static void ShowPowerGained(float amount, Vector2 startPos, Vector2 hudPos)
        {
            ShowFloatingText($"+{amount:0.#} Puissance", startPos + Vector2.up * 20f, new Color(1f, 0.8f, 0.2f), 1.5f);
        }

        public static void ShowExperienceGained(float amount, Vector2 startPos, Vector2 hudPos)
        {
            ShowFloatingText($"+{amount:0.#} XP", startPos + Vector2.up * 20f, new Color(0.4f, 0.8f, 1f), 1.5f);
        }

        public static void ShowTroopsTrained(int count, string troopType, Vector2 buildingPos, Vector2 hudPos)
        {
            ShowFloatingText($"+{count} {troopType}", buildingPos + Vector2.up * 16f, new Color(0.4f, 1f, 0.5f));
            SpawnIconBurst(FeedbackIconType.Generic, buildingPos, hudPos, count);
        }

        public static void ShowQuestCompleted(Vector2 startPos, Vector2 hudPos)
        {
            ShowFloatingText("Quête terminée", startPos + Vector2.up * 20f, new Color(1f, 0.85f, 0.25f), 1.5f);
            SpawnIconBurst(FeedbackIconType.Generic, startPos, hudPos, 10);
        }

        private static void SpawnIconBurst(FeedbackIconType iconType, Vector2 startPos, Vector2 targetPos, int count = 6)
        {
            iconBursts.Add(new FeedbackIconBurst(iconType, startPos, targetPos, count));
        }

        private static Color ResourceColor(string resourceName)
        {
            switch (resourceName.ToLowerInvariant())
            {
                case "miel": return new Color(1f, 0.78f, 0.12f);
                case "cire": return new Color(1f, 0.82f, 0.28f);
                case "pollen": return new Color(0.78f, 0.9f, 0.32f);
                default: return new Color(1f, 0.85f, 0.25f);
            }
        }

        private static FeedbackIconType ResourceIcon(string resourceName)
        {
            switch (resourceName.ToLowerInvariant())
            {
                case "miel": return FeedbackIconType.Honey;
                case "cire": return FeedbackIconType.Wax;
                case "pollen": return FeedbackIconType.Pollen;
                default: return FeedbackIconType.Generic;
            }
        }

        public static void Tick(float dt)
        {
            for (int i = floatingTexts.Count - 1; i >= 0; i--)
            {
                floatingTexts[i].Tick(dt);
                if (!floatingTexts[i].IsAlive) floatingTexts.RemoveAt(i);
            }
            for (int i = iconBursts.Count - 1; i >= 0; i--)
            {
                iconBursts[i].Tick(dt);
                if (!iconBursts[i].IsAlive) iconBursts.RemoveAt(i);
            }
            for (int i = pulses.Count - 1; i >= 0; i--)
            {
                pulses[i].Tick(dt);
                if (!pulses[i].IsAlive) pulses.RemoveAt(i);
            }
            for (int i = highlights.Count - 1; i >= 0; i--)
            {
                highlights[i].Tick(dt);
                if (!highlights[i].IsAlive) highlights.RemoveAt(i);
            }

            float now = UIAnimationLibrary.Now;
            pendingResourceFlushKeys.Clear();
            foreach (var kvp in pendingResourceTimers)
            {
                if (now - kvp.Value >= NotificationCooldown)
                {
                    string key = kvp.Key;
                    float amount = pendingResourceAmounts[key];
                    ShowFloatingText($"+{amount:0.#}", Vector2.zero, ResourceColor(key));
                    pendingResourceAmounts.Remove(key);
                    pendingResourceFlushKeys.Add(key);
                }
            }
            for (int i = 0; i < pendingResourceFlushKeys.Count; i++)
            {
                string key = pendingResourceFlushKeys[i];
                pendingResourceTimers.Remove(key);
            }
        }

        public static void Draw()
        {
            foreach (var text in floatingTexts)
            {
                float alpha = text.Alpha;
                Vector2 pos = text.Position + Vector2.up * text.YOffset;
                GUI.color = new Color(text.Color.r, text.Color.g, text.Color.b, alpha);
                GUI.Label(new Rect(pos.x - 40f, pos.y - 12f, 80f, 24f), text.Text, new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14, fontStyle = FontStyle.Bold });
            }

            foreach (var burst in iconBursts)
            {
                for (int i = 0; i < burst.Count; i++)
                {
                    if (i < burst.Positions.Count)
                    {
                        float scale = burst.Scales[i];
                        float alpha = burst.Alphas[i];
                        Vector2 pos = burst.Positions[i];
                        float size = 16f * scale;
                        GUI.color = new Color(1f, 1f, 1f, alpha);
                        GUI.DrawTexture(new Rect(pos.x - size * 0.5f, pos.y - size * 0.5f, size, size), Texture2D.whiteTexture);
                    }
                }
            }

            foreach (var pulse in pulses)
            {
                float alpha = pulse.Alpha * 0.5f;
                float radius = pulse.CurrentRadius;
                GUI.color = new Color(pulse.Color.r, pulse.Color.g, pulse.Color.b, alpha);
                GUI.DrawTexture(new Rect(pulse.Position.x - radius, pulse.Position.y - radius, radius * 2f, radius * 2f), Texture2D.whiteTexture);
            }

            foreach (var highlight in highlights)
            {
                float alpha = highlight.Alpha * 0.3f;
                float scale = highlight.Scale;
                Rect r = highlight.Rect;
                Vector2 center = r.center;
                float w = r.width * scale;
                float h = r.height * scale;
                Rect drawRect = new Rect(center.x - w * 0.5f, center.y - h * 0.5f, w, h);
                GUI.color = new Color(highlight.Color.r, highlight.Color.g, highlight.Color.b, alpha);
                GUI.DrawTexture(drawRect, Texture2D.whiteTexture);
            }

            GUI.color = Color.white;
        }
    }
}
