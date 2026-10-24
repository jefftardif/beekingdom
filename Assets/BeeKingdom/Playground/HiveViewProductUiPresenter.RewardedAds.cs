using System;
using BeeKingdom.Localization;
using BeeKingdom.Networking;
using UnityEngine;

#if UNITY_ANDROID && !UNITY_EDITOR
using Unity.Services.LevelPlay;
#endif

namespace BeeKingdom.Playground
{
    public static partial class HiveViewProductUiPresenter
    {
        private static RewardedAdsQuestPanelController rewardedAdsQuestController;
        private static bool rewardedAdsPrivacyChoice;

        public static void ConfigureRewardedAdsQuestControllerForRuntime(
            RewardedAdsQuestPanelController controller)
        {
            rewardedAdsQuestController = controller;
            rewardedAdsPrivacyChoice = false;
        }

        public static void ResetRewardedAdsQuestControllerForRuntime()
        {
            rewardedAdsQuestController = null;
            rewardedAdsPrivacyChoice = false;
        }

        private static void DrawRewardedAdsDailyContent(Rect panel, bool compact, float top)
        {
            float x = panel.x + 18f;
            float width = panel.width - 36f;
            float y = top;
            RewardedAdsQuestPanelController controller = rewardedAdsQuestController;
            RewardedAdsQuestSnapshot model = controller?.Model;
            if (controller != null && controller.IsConfigured && model == null &&
                !controller.IsBusy && string.IsNullOrEmpty(controller.StatusKey))
                controller.Refresh();

            GUI.Label(new Rect(x, y, width, 27f),
                BeeLocalization.Text("ads.quest.title", "Les cadeaux de la ruche"),
                new GUIStyle(smallStyle) { fontSize = compact ? 14 : 17 });
            GUI.Label(new Rect(x, y + 31f, width, 38f),
                BeeLocalization.Text("ads.quest.subtitle",
                    "Trois vidéos facultatives. Aucune publicité pendant les combats."),
                new GUIStyle(tinyLabelStyle) { wordWrap = true, fontSize = compact ? 10 : 12 });

            int count = model == null ? 0 : Mathf.Clamp(model.CompletedCount, 0, 3);
            DrawProgressBar(new Rect(x, y + 78f, width, 10f), count / 3f);
            GUI.Label(new Rect(x, y + 94f, width, 26f), count + " / 3",
                new GUIStyle(badgeStyle) { alignment = TextAnchor.MiddleRight });

            string[] itemLabels =
            {
                BeeLocalization.Text("ads.quest.video1", "Vidéo 1 : miel et pollen"),
                BeeLocalization.Text("ads.quest.video2", "Vidéo 2 : miel et pollen"),
                BeeLocalization.Text("ads.quest.video3", "Vidéo 3 : coffre bonus")
            };
            float rowHeight = compact ? 49f : 58f;
            float gap = compact ? 5f : 8f;
            float rowsTop = y + 126f;
            for (int i = 0; i < 3; ++i)
            {
                Rect rect = new Rect(x, rowsTop + i * (rowHeight + gap),
                    width, rowHeight);
                bool done = count > i;
                DrawPremiumPanel(rect,
                    new Color(0.034f, 0.030f, 0.024f, 0.92f),
                    done ? new Color(0.40f, 0.72f, 0.32f, 0.92f) :
                        new Color(0.85f, 0.60f, 0.22f, 0.64f));
                GUI.Label(new Rect(rect.x + 14f, rect.y + 6f,
                        rect.width - 80f, rect.height - 12f), itemLabels[i],
                    new GUIStyle(smallStyle)
                    {
                        wordWrap = true, alignment = TextAnchor.MiddleLeft,
                        fontSize = compact ? 11 : 13
                    });
                GUI.Label(new Rect(rect.xMax - 69f, rect.y + 7f, 60f,
                        rect.height - 14f),
                    done ? BeeLocalization.Text("ads.quest.done", "Reçu") :
                        (i == count ? BeeLocalization.Text("ads.quest.ready", "À voir") :
                        BeeLocalization.Text("ads.quest.locked", "Ensuite")),
                    new GUIStyle(tinyLabelStyle)
                    {
                        alignment = TextAnchor.MiddleRight, wordWrap = true
                    });
            }

            float footerY = rowsTop + 3 * (rowHeight + gap) + 8f;
            if (model == null || !model.Enabled)
            {
                GUI.Label(new Rect(x, footerY, width * 0.69f, 45f),
                    controller == null ?
                        BeeLocalization.Text("ads.quest.login", "Connexion requise.") :
                        BeeLocalization.Text("ads.quest.unavailable",
                            "Les récompenses vidéo seront bientôt disponibles."),
                    new GUIStyle(tinyLabelStyle) { wordWrap = true });
                if (controller != null && !controller.IsBusy &&
                    GUI.Button(new Rect(x + width * 0.72f, footerY,
                        width * 0.28f, 37f),
                        BeeLocalization.Text("ads.quest.refresh", "Actualiser")))
                    controller.Refresh();
            }
            else if (count >= 3)
            {
                GUI.Label(new Rect(x, footerY, width, 38f),
                    BeeLocalization.Text("ads.quest.complete",
                        "Quête terminée ! Retour demain après la réinitialisation UTC."),
                    new GUIStyle(smallStyle) { wordWrap = true });
            }
            else
            {
                rewardedAdsPrivacyChoice = GUI.Toggle(
                    new Rect(x, footerY, width, 37f),
                    rewardedAdsPrivacyChoice,
                    BeeLocalization.Text("ads.quest.choice",
                        "Je souhaite regarder une vidéo publicitaire facultative."));
                float buttonY = footerY + 42f;
                Rect watch = new Rect(x, buttonY, width * 0.67f - 5f, 48f);
                Rect refresh = new Rect(x + width * 0.67f + 5f,
                    buttonY, width * 0.33f - 5f, 48f);

                bool canWatch = rewardedAdsPrivacyChoice && controller != null &&
                    !controller.IsBusy && !controller.PendingServerConfirmation;
                bool prior = GUI.enabled;
                GUI.enabled = canWatch;
                if (GUI.Button(watch,
                    BeeLocalization.Text("ads.quest.watch", "Regarder une vidéo"),
                    new GUIStyle(GUI.skin.button) { fontSize = compact ? 10 : 13 }))
                {
#if UNITY_ANDROID && !UNITY_EDITOR
                    // No personalized data processing is claimed by this opt-in.
                    // Proper certified CMP integration is still a release gate
                    // for markets where consent management is required.
                    LevelPlayPrivacySettings.SetGDPRConsent(false);
#endif
                    controller.BeginOrWatch(privacyChoicesResolved: true);
                }
                GUI.enabled = prior;
                if (GUI.Button(refresh,
                    BeeLocalization.Text("ads.quest.refresh", "Actualiser"),
                    new GUIStyle(GUI.skin.button) { fontSize = compact ? 10 : 13 }))
                    controller?.Refresh();
            }

            string status = controller?.StatusKey ?? string.Empty;
            if (string.Equals(status, "pending_server", StringComparison.Ordinal))
                GUI.Label(new Rect(x, panel.yMax - 31f, width, 24f),
                    BeeLocalization.Text("ads.quest.server_pending",
                        "Confirmation sécurisée du serveur en cours..."),
                    tinyLabelStyle);
            else if (string.Equals(status, "temporarily_unavailable", StringComparison.Ordinal))
                GUI.Label(new Rect(x, panel.yMax - 31f, width, 24f),
                    BeeLocalization.Text("ads.quest.offline",
                        "Le service est momentanément indisponible."),
                    tinyLabelStyle);
            else if (string.Equals(status, "loading_ad", StringComparison.Ordinal))
                GUI.Label(new Rect(x, panel.yMax - 31f, width, 24f),
                    BeeLocalization.Text("ads.quest.loading",
                        "Préparation de votre publicité..."),
                    tinyLabelStyle);
        }
    }
}
