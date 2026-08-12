using BeeKingdom.UI;
using UnityEngine;

namespace BeeKingdom.Rewards
{
    public static class RewardClaimView
    {
        private static RewardBundle bundle;
        private static bool open;

        public static bool IsOpen => open;

        public static void Open(RewardBundle value)
        {
            if (value == null || value.Rewards.Count == 0) return;
            bundle = value;
            open = true;
            UIAnimationLibrary.BeginWindowOpen("reward_claim_window", 0.22f);
        }

        public static void Close()
        {
            open = false;
            bundle = null;
            UIAnimationLibrary.BeginWindowClose("reward_claim_window", 0.18f);
        }

        public static void Draw(bool compact)
        {
            if (!open || bundle == null) return;

            float width = compact ? Mathf.Min(Screen.width - 24f, 420f) : Mathf.Min(Screen.width * 0.52f, 620f);
            float height = compact ? Mathf.Min(Screen.height - 24f, 620f) : Mathf.Min(Screen.height - 80f, 560f);
            Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            panel = UIAnimationLibrary.ApplyWindowAnimation(panel, "reward_claim_window");
            DrawPanel(panel, new Color(0.035f, 0.028f, 0.018f, 0.985f), new Color(1f, 0.70f, 0.18f, 0.96f));
            GUI.Label(new Rect(panel.x + 20f, panel.y + 18f, panel.width - 40f, 32f), "Récompenses", new GUIStyle(GUI.skin.label) { fontSize = compact ? 20 : 26, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
            GUI.Label(new Rect(panel.x + 20f, panel.y + 54f, panel.width - 40f, 20f), bundle.Source, new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 12 });

            float y = panel.y + 92f;
            for (int i = 0; i < bundle.Rewards.Count; i++)
            {
                Reward reward = bundle.Rewards[i];
                Rect row = new Rect(panel.x + 20f, y, panel.width - 40f, 48f);
                DrawPanel(row, new Color(0.09f, 0.065f, 0.035f, 0.96f), new Color(0.70f, 0.48f, 0.16f, 0.70f));
                GUI.Label(new Rect(row.x + 12f, row.y + 6f, row.width - 24f, 18f), reward.Type.ToString(), GUI.skin.label);
                GUI.Label(new Rect(row.x + 12f, row.y + 25f, row.width - 24f, 17f), reward.Id + "  x" + reward.Amount, GUI.skin.label);
                y += 56f;
                if (y > panel.yMax - 70f) break;
            }

            Rect validate = new Rect(panel.x + 20f, panel.yMax - 54f, panel.width - 40f, 40f);
            DrawPanel(validate, new Color(0.62f, 0.34f, 0.055f, 0.96f), new Color(1f, 0.80f, 0.18f, 1f));
            GUI.Label(validate, "Valider", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
            if (GUI.Button(validate, string.Empty, GUIStyle.none)) Close();
        }

        private static void DrawPanel(Rect rect, Color fill, Color border)
        {
            Color previous = GUI.color;
            GUI.color = border;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = fill;
            GUI.DrawTexture(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f), Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }

    public sealed class RewardClaimPresentation : IRewardPresentation
    {
        public void Present(RewardBundle bundle)
        {
            RewardClaimView.Open(bundle);
        }
    }
}
