using BeeKingdom.Audio;
using BeeKingdom.Localization;
using System.Globalization;
using UnityEngine;

namespace BeeKingdom.Playground
{
    // Every premium building uses this ONE approved gold-on-black upgrade emblem.
    // The asset is never recolored, replaced or guessed from a building category.
    internal static class PremiumBuildingUpgradeArtwork
    {
        internal const string ResourcePath = "PremiumBeeIcons/upgrade";
        internal static Texture2D Texture => Resources.Load<Texture2D>(ResourcePath);
        internal static readonly Color SocketColor = new Color(.085f, .052f, .025f, 1f);
    }

    // Same premium header action for Alliance, Caserne and Banque. Screen-space
    // rectangles are deterministic and do not depend on a live player session.
    public static partial class HiveViewProductUiPresenter
    {
        public static Rect PremiumBuildingUpgradeRectForProof(float screenWidth, bool compact)
        {
            float margin = compact ? 10f : 16f;
            float width = compact ? 142f : 178f;
            return new Rect(Mathf.Max(margin, screenWidth - width - margin),
                compact ? 11f : 14f, width, 48f);
        }

        private static bool DrawPremiumBuildingUpgradeHeader(string buildingKey, bool compact)
        {
            Rect button = PremiumBuildingUpgradeRectForProof(Screen.width, compact);
            if (string.Equals(buildingKey, "guard_post", System.StringComparison.Ordinal))
            {
                try
                {
                    BeeKingdom.Tutorial.TutorialTargetRegistry.Instance.RegisterScreenRect(
                        BeeKingdom.Tutorial.FtueTutorialRegistry.TargetUpgradeButton, button);
                }
                catch { }
            }
            bool enabled = !OfficialBuildingUpgradeConfigured() ||
                           OfficialBuildingUpgradeActionEnabled(buildingKey);
            bool oldEnabled = GUI.enabled;
            Color oldColor = GUI.color;
            bool hovered = enabled && Event.current != null
                && button.Contains(Event.current.mousePosition);
            Color surface = enabled
                ? (hovered ? new Color(1f, .75f, .31f, 1f)
                           : new Color(.90f, .61f, .22f, 1f))
                : new Color(.19f, .14f, .09f, .98f);
            DrawActivitiesSurface(button, surface);
            Rect iconSocket = new Rect(button.x + 6f, button.y + 5f, 40f, 38f);
            GUI.color = PremiumBuildingUpgradeArtwork.SocketColor;
            GUI.DrawTexture(iconSocket, Texture2D.whiteTexture);
            Texture2D upgradeIcon = PremiumBuildingUpgradeArtwork.Texture;
            GUI.color = Color.white;
            if (upgradeIcon != null)
                GUI.DrawTexture(new Rect(iconSocket.x + 2f, iconSocket.y + 1f, 36f, 36f),
                    upgradeIcon, ScaleMode.ScaleToFit, true);
            GUI.Label(new Rect(button.x + 48f, button.y + 3f,
                    button.width - 53f, button.height - 6f),
                compact ? BeeLocalization.Text("common.upgrade", "Améliorer") : BeeLocalization.Text("common.upgrade", "Améliorer").ToUpperInvariant(),
                new GUIStyle(centeredTinyLabelStyle)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = compact ? 13 : 16,
                    fontStyle = FontStyle.Bold,
                    clipping = TextClipping.Clip,
                    normal = { textColor = enabled
                        ? new Color(.12f, .075f, .035f, 1f)
                        : new Color(.71f, .66f, .56f, 1f) }
                });
            GUI.enabled = oldEnabled && enabled;
            bool clicked = GUI.Button(button, GUIContent.none, GUIStyle.none);
            GUI.enabled = oldEnabled;
            GUI.color = oldColor;
            if (clicked && enabled)
            {
                AudioManager.Instance?.PlayUIClick();
                TryStartUpgradeWithPrerequisiteRedirectForExternalHost(buildingKey);
                return true;
            }
            return false;
        }

        // Only explicit recruitment commands may change the CTA caption to
        // an in-flight label. A passive data refresh must never say "Chargement".
        private static string BarrackTrainingButtonCaption(
            HiveDoctrineRecruitmentScreenModel model, string family)
        {
            if (model == null) return BarrackText("stable.preparing", "Préparation de l'entraînement");
            if (model.IsPending)
                return model.CanRetry(family) ? BarrackText("stable.verify", "Vérifier la commande") : BarrackText("stable.pending", "Commande en attente");
            if (model.State == HiveDoctrineRecruitmentScreenState.Mutating)
                return string.Equals(model.PendingKind, "claim", System.StringComparison.Ordinal)
                    ? BarrackText("stable.claiming", "Récupération des troupes…") : BarrackText("stable.training", "Entraînement…");
            HiveDoctrineRecruitmentOperationModel operation = model.ActiveOperation;
            if (operation != null)
            {
                if (!string.Equals(operation.Family, family, System.StringComparison.Ordinal))
                    return BarrackText("stable.queue_busy", "File d'entraînement occupée");
                if (operation.AwaitingCompletion)
                    return BarrackFormat("stable.claim", "Réclamer +{0}", operation.BatchSize);
                return BarrackText("stable.training", "Entraînement…");
            }
            HiveDoctrineRecruitmentOfferModel offer = model.FindOffer(family);
            if (offer != null)
                return BarrackFormat("stable.form", "Former +{0}", offer.BatchSize);
            return model.State == HiveDoctrineRecruitmentScreenState.Loading
                ? BarrackText("stable.preparing", "Préparation de l'entraînement") : BarrackText("stable.unavailable", "Entraînement indisponible");
        }

        private static bool BarrackTrainingButtonActionable(
            HiveDoctrineRecruitmentScreenModel model, string family)
        {
            if (model == null) return false;
            if (model.IsPending) return model.CanRetry(family);
            if (model.State != HiveDoctrineRecruitmentScreenState.Ready) return false;
            HiveDoctrineRecruitmentOperationModel operation = model.ActiveOperation;
            if (operation != null)
                return string.Equals(operation.Family, family, System.StringComparison.Ordinal)
                    && model.CanClaim();
            return model.CanStart(family);
        }

        // Separate the button's visual availability from the transient busy flag
        // of an invisible background refresh. Input still remains blocked while
        // a request is in flight, so an outdated roster can never start a mutation.
        private static bool DrawBarrackStableTrainingButton(
            Rect rect, string label, bool actionable, bool refreshing)
        {
            bool oldEnabled = GUI.enabled;
            Color oldColor = GUI.color;
            bool hovered = actionable && !refreshing
                && Event.current != null && rect.Contains(Event.current.mousePosition);
            Color fill = actionable
                ? hovered ? new Color(.98f, .70f, .27f, 1f)
                          : new Color(.91f, .58f, .18f, 1f)
                : new Color(.19f, .135f, .075f, .98f);
            DrawAllianceActivitiesSurface(rect, fill);
            GUI.enabled = true;
            GUI.Label(rect, label ?? string.Empty,
                new GUIStyle(centeredTinyLabelStyle)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = Mathf.Clamp(Mathf.RoundToInt(rect.height * .32f), 15, 19),
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = actionable
                        ? new Color(.14f, .085f, .03f, 1f)
                        : new Color(.86f, .79f, .67f, 1f) }
                });
            GUI.enabled = oldEnabled && actionable && !refreshing;
            bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);
            GUI.enabled = oldEnabled;
            GUI.color = oldColor;
            return clicked && actionable && !refreshing;
        }
        private static string BarrackText(string key, string frenchFallback)
        {
            return BeeLocalization.Text("barrack.premium." + key, frenchFallback);
        }

        private static string BarrackFormat(string key, string frenchFallback, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture,
                BarrackText(key, frenchFallback), args);
        }

        private static string BarrackTroopLabel(string family)
        {
            switch (family)
            {
                case "guardians": return BarrackText("family.guardians", "Gardiennes");
                case "wingrunners": return BarrackText("family.wingrunners", "Voltigeuses");
                case "darters": return BarrackText("family.darters", "Lanceuses");
                default: return family ?? string.Empty;
            }
        }
        // The Alliance chat uses the same framed round avatar treatment as Chat Royal.
        // The chat wire exposes names and IDs, not downloadable profile photos; avoid
        // falsely assigning a local portrait to someone else's account.
        private static void DrawAllianceMessageAvatar(Rect rect, string senderName)
        {
            DrawRoundAvatarBase(rect);
            GUI.Label(rect, ChatInitials(senderName),
                new GUIStyle(titleStyle)
                {
                    fontSize = Mathf.RoundToInt(rect.height * .32f),
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(1f, .84f, .47f, 1f) }
                });
        }
    }
}