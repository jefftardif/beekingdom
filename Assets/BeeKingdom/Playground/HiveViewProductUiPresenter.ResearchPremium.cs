using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BeeKingdom.Localization;
using UnityEngine;

namespace BeeKingdom.Playground
{
    // Staged premium Research window. Keep existing server controller and
    // original view intact until the Champion Hall QA session has ended.
    public static partial class HiveViewProductUiPresenter
    {
        private static bool premiumResearchFullscreenEnabled = true;

        private static string PremiumResearchText(string key, string frenchFallback) =>
            BeeLocalization.Text("research.premium." + key, frenchFallback);

        private static string PremiumResearchTitle(string researchId)
        {
            LocalPreviewResearchDefinition definition = LocalPreviewResearchCatalog.All
                .FirstOrDefault(item => string.Equals(item.ResearchId, researchId, StringComparison.Ordinal));
            return definition == null ? (researchId ?? string.Empty)
                : BeeLocalization.Text(definition.TitleKey, definition.ResearchId);
        }

        private static void DrawResearchFullscreenPremium(bool compact)
        {
            if (activeHiveMenu != HiveMenuMode.Research) return;

            Event evt = Event.current;
            if (evt != null && evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape)
            {
                CloseResearchFullscreen();
                evt.Use();
                return;
            }

            // Opaque fullscreen: queues, ambient bees and clickable world
            // objects must never visually bleed through the research window.
            Rect fullscreen = new Rect(0f, 0f, Screen.width, Screen.height);
            GUI.color = new Color(.010f, .008f, .006f, 1f);
            GUI.DrawTexture(fullscreen, Texture2D.whiteTexture);
            GUI.color = Color.white;

            float pad = compact ? 10f : 22f;
            float headerHeight = compact ? 100f : 116f;
            Rect header = new Rect(0f, 0f, Screen.width, headerHeight);
            Texture2D banner = Resources.Load<Texture2D>(
                "PremiumBeeReference/BuildingBanners/research_node");
            if (banner != null)
                GUI.DrawTexture(header, banner, ScaleMode.ScaleAndCrop, true);
            else
                DrawPremiumPanel(header, new Color(.11f, .069f, .024f, 1f),
                    new Color(.86f, .58f, .16f, .8f));
            GUI.color = new Color(.006f, .005f, .004f, .70f);
            GUI.DrawTexture(header, Texture2D.blackTexture);
            GUI.color = Color.white;

            // Header follows Activities/Barracks/Alliance proportions. Back
            // never overlaps title; exactly one shared upgrade emblem.
            if (DrawPremiumBackButton(new Rect(pad, 18f, 48f, 48f)))
            {
                CloseResearchFullscreen();
                return;
            }
            float titleX = compact ? 102f : 130f;
            float rightReserve = compact ? 165f : 213f;
            float titleWidth = Mathf.Max(90f, Screen.width - titleX - rightReserve);
            GUI.Label(new Rect(titleX, 15f, titleWidth, 36f),
                PremiumResearchText("title", "RECHERCHES"),
                new GUIStyle(titleStyle)
                {
                    fontSize = compact ? 21 : 31,
                    fontStyle = FontStyle.Bold,
                    clipping = TextClipping.Clip,
                    normal = { textColor = new Color(1f, .81f, .42f) }
                });
            GUI.Label(new Rect(titleX, 54f, titleWidth, 43f),
                PremiumResearchText("subtitle", "Étudiez, développez et renforcez votre colonie"),
                new GUIStyle(smallStyle)
                {
                    fontSize = compact ? 12 : 16, wordWrap = true,
                    normal = { textColor = new Color(.90f, .84f, .73f) }
                });
            DrawPremiumBuildingUpgradeHeader("research_node", compact);
            GUI.color = new Color(.98f, .70f, .22f);
            GUI.DrawTexture(new Rect(0f, headerHeight - 2f, Screen.width, 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            bool official = OfficialResearchConfigured();
            HiveResearchScreenModel model = official ? OfficialResearchModel() : null;
            IReadOnlyList<LocalPreviewResearchDefinition> definitions = LocalPreviewResearchCatalog.All;
            float summaryHeight = compact ? 101f : 109f;
            Rect summary = new Rect(pad, headerHeight + 12f,
                Screen.width - pad * 2f, summaryHeight);
            DrawPremiumPanel(summary,
                new Color(.11f, .073f, .034f, .99f),
                new Color(.59f, .42f, .19f, .81f));

            int completed = official && model != null ? model.Completed.Count : 0;
            string count = official && model != null
                ? completed.ToString(CultureInfo.CurrentCulture)
                : "—";
            GUI.Label(new Rect(summary.x + 15f, summary.y + 7f,
                    summary.width * .44f, 30f),
                string.Format(CultureInfo.CurrentCulture,
                    PremiumResearchText("completed", "Études terminées : {0}/{1}"),
                    count, definitions.Count),
                new GUIStyle(badgeStyle)
                {
                    fontSize = compact ? 14 : 19,
                    fontStyle = FontStyle.Bold
                });

            string connectionStatus = !official
                ? PremiumResearchText("local", "Démonstration locale — non officielle")
                : model == null || model.State == HiveResearchScreenState.Loading
                    ? PremiumResearchText("syncing", "Synchronisation du serveur...")
                    : model.IsReadOnly
                        ? PremiumResearchText("offline", "Hors ligne — consultation uniquement")
                        : model.State == HiveResearchScreenState.Error
                            ? OfficialResearchErrorText(model.ErrorCode)
                            : PremiumResearchText("online", "Données officielles synchronisées");
            GUI.Label(new Rect(summary.x + 15f, summary.y + 37f,
                    summary.width - 30f, 24f), connectionStatus,
                new GUIStyle(smallStyle)
                {
                    fontSize = compact ? 12 : 15,
                    normal = { textColor = new Color(.86f, .79f, .67f) }
                });

            HiveResearchOperationModel active = model?.ActiveOperation;
            if (active != null)
            {
                string label = string.Format(CultureInfo.CurrentCulture,
                    PremiumResearchText("active", "En cours : {0}"),
                    PremiumResearchTitle(active.ResearchId));
                if (active.IsAwaitingCompletion)
                    label = string.Format(CultureInfo.CurrentCulture,
                        PremiumResearchText("claimable", "Étude à valider : {0}"),
                        PremiumResearchTitle(active.ResearchId));
                else if (researchController != null)
                    label += " · " + FormatBuildingUpgradeDuration(
                        model.Remaining(researchController.Elapsed));
                GUI.Label(new Rect(summary.x + 15f, summary.y + 62f,
                        summary.width - 30f, 24f), label,
                    new GUIStyle(smallStyle)
                    {
                        fontSize = compact ? 12 : 16,
                        normal = { textColor = new Color(1f, .83f, .43f) }
                    });
                float progress = active.IsAwaitingCompletion ? 1f
                    : researchController == null ? 0f
                    : (float)model.Progress01(researchController.Elapsed);
                DrawProgressBar(new Rect(summary.x + 15f, summary.yMax - 11f,
                    summary.width - 30f, 5f), progress);
            }
            else
            {
                GUI.Label(new Rect(summary.x + 15f, summary.y + 63f,
                        summary.width - 30f, 24f),
                    PremiumResearchText("no_active", "Aucune étude active dans la file"),
                    new GUIStyle(smallStyle) { fontSize = compact ? 12 : 15 });
            }

            float filterTop = summary.yMax + 10f;
            Rect filterArea = new Rect(pad, filterTop, Screen.width - pad * 2f, 48f);
            string[] filterIds = { "all", "forage", "resources", "defense" };
            string[] filterNames =
            {
                PremiumResearchText("tab.all", "Toutes"),
                PremiumResearchText("tab.foraging", "Butinage"),
                PremiumResearchText("tab.resources", "Ressources"),
                PremiumResearchText("tab.reserves", "Réserves")
            };
            float filterWidth = compact ? 126f
                : (filterArea.width - 3f * 6f) / filterIds.Length;
            float filterContentWidth = compact ? filterIds.Length * (filterWidth + 6f) : filterArea.width;
            researchFullscreenFilterScroll = GUI.BeginScrollView(
                filterArea, researchFullscreenFilterScroll,
                new Rect(0f, 0f, filterContentWidth, filterArea.height - 2f),
                false, false);
            for (int i = 0; i < filterIds.Length; i++)
            {
                Rect tab = new Rect(i * (filterWidth + 6f), 2f, filterWidth, 42f);
                bool selected = researchFullscreenFilter == filterIds[i];
                if (DrawAllianceActivitiesButton(tab, filterNames[i], true, selected))
                {
                    researchFullscreenFilter = filterIds[i];
                    researchFullscreenScroll = Vector2.zero;
                }
            }
            GUI.EndScrollView();

            float bodyTop = filterArea.yMax + 10f;
            Rect content = new Rect(pad, bodyTop,
                Screen.width - pad * 2f, Screen.height - bodyTop - 14f);
            DrawPremiumPanel(content, new Color(.034f, .024f, .015f, .99f),
                new Color(.45f, .31f, .13f, .75f));

            List<LocalPreviewResearchDefinition> visible =
                new List<LocalPreviewResearchDefinition>(definitions.Count);
            foreach (LocalPreviewResearchDefinition definition in definitions)
                if (ResearchMatchesFullscreenFilter(definition.ResearchId))
                    visible.Add(definition);

            float inset = compact ? 10f : 15f;
            Rect viewport = new Rect(content.x + inset, content.y + inset,
                content.width - inset * 2f, content.height - inset * 2f);
            int columns = compact || viewport.width < 970f ? 1 : 2;
            float gap = 12f;
            float height = compact ? 223f : 216f;
            float cardWidth = (viewport.width - (columns - 1) * gap - 19f) / columns;
            int rows = (visible.Count + columns - 1) / columns;
            float canvasHeight = Mathf.Max(viewport.height, rows * (height + gap) + 8f);
            researchFullscreenScroll = GUI.BeginScrollView(
                viewport, researchFullscreenScroll,
                new Rect(0f, 0f, viewport.width - 3f, canvasHeight), false, true);
            if (visible.Count == 0)
                GUI.Label(new Rect(16f, 12f, viewport.width - 32f, 60f),
                    PremiumResearchText("empty", "Aucune étude dans cette catégorie"),
                    new GUIStyle(smallStyle)
                    { fontSize = compact ? 15 : 18, wordWrap = true });
            for (int i = 0; i < visible.Count; i++)
            {
                int x = i % columns;
                int y = i / columns;
                Rect card = new Rect(x * (cardWidth + gap),
                    y * (height + gap), cardWidth, height);
                DrawPremiumResearchCard(card, visible[i], compact, official, model, viewport, researchFullscreenScroll);
            }
            GUI.EndScrollView();
        }

        private static void DrawPremiumResearchCard(Rect card,
            LocalPreviewResearchDefinition definition, bool compact,
            bool official, HiveResearchScreenModel model, Rect viewport, Vector2 scrollOffset)
        {
            string id = definition.ResearchId;
            bool completed = official
                ? model != null && model.IsCompleted(id)
                : LocalPreviewResearchCatalog.Contains(
                    localPreviewQueueJournal.completedResearchIds, id);
            bool running = official
                ? model?.ActiveOperation != null &&
                  string.Equals(model.ActiveOperation.ResearchId, id, StringComparison.Ordinal)
                : IsResearchRunning() && string.Equals(localPreviewResearchId, id, StringComparison.Ordinal);

            Color border = completed ? new Color(.43f, .79f, .49f)
                : running ? new Color(.99f, .76f, .32f)
                : new Color(.58f, .41f, .17f);
            DrawPremiumPanel(card, new Color(.082f, .051f, .026f, .985f), border);
            DrawGameIcon(new Rect(card.x + 14f, card.y + 15f,
                39f, 39f), definition.IconId, Color.white);
            string title = BeeLocalization.Text(definition.TitleKey, id);
            GUI.Label(new Rect(card.x + 62f, card.y + 10f,
                card.width - 76f, 30f), title,
                new GUIStyle(badgeStyle)
                {
                    fontSize = compact ? 16 : 19,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(1f, .80f, .37f) }
                });
            GUI.Label(new Rect(card.x + 62f, card.y + 41f,
                card.width - 76f, 43f),
                BeeLocalization.Text(definition.SummaryKey, id),
                new GUIStyle(smallStyle)
                {
                    fontSize = compact ? 12 : 14,
                    wordWrap = true,
                    normal = { textColor = new Color(.86f, .79f, .69f) }
                });

            string status = completed
                ? PremiumResearchText("status.completed", "Terminée")
                : running && model?.ActiveOperation?.IsAwaitingCompletion == true
                    ? PremiumResearchText("status.ready", "À valider")
                    : running ? PremiumResearchText("status.running", "En cours")
                    : official && model == null
                        ? PremiumResearchText("status.wait", "Synchronisation")
                        : official && model?.OfferFor(id) == null
                            ? PremiumResearchText("status.unavailable", "Non disponible")
                            : official && model?.ActiveOperation != null
                                ? PremiumResearchText("status.occupied", "File occupée")
                                : official && model != null && !model.PrerequisitesMet(id)
                                    ? PremiumResearchText("status.locked", "Prérequis manquant")
                                    : official && model != null && !string.IsNullOrEmpty(model.ShortageResource(id))
                                        ? PremiumResearchText("status.resources", "Ressources insuffisantes")
                                        : PremiumResearchText("status.available", "Disponible");
            GUI.Label(new Rect(card.x + 14f, card.y + 91f,
                card.width - 28f, 22f), status,
                new GUIStyle(smallStyle)
                {
                    fontSize = compact ? 12 : 14,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = border }
                });

            string effect;
            string cost;
            string reason;
            string button;
            bool actionable;
            if (official)
            {
                HiveResearchOfferModel offer = model?.OfferFor(id);
                cost = offer == null ? string.Empty
                    : OfficialResearchCostText(offer) + " · " +
                      FormatBuildingUpgradeDuration(offer.Duration);
                effect = OfficialResearchEffectText(id);
                reason = OfficialResearchStatusText(id);
                button = OfficialResearchActionLabel(id);
                actionable = OfficialResearchActionEnabled(id) && !completed && !running;
                // A completed running operation must remain completable.
                if (running && model?.CanComplete(id) == true)
                    actionable = OfficialResearchActionEnabled(id);
            }
            else
            {
                cost = ResearchCostText(definition);
                effect = BeeLocalization.Text(definition.ResultKey, id);
                reason = ResearchDisabledReason(definition);
                button = completed
                    ? BeeLocalization.Text("research.button.completed", "Terminée")
                    : running
                        ? BeeLocalization.Text("research.official.status.running", "En cours")
                        : BeeLocalization.Text("research.button.start", "Lancer");
                actionable = !completed && !running && string.IsNullOrWhiteSpace(reason);
            }

            GUI.Label(new Rect(card.x + 14f, card.y + 116f,
                card.width - 28f, 35f), effect,
                new GUIStyle(smallStyle)
                {
                    fontSize = compact ? 11 : 13, wordWrap = true,
                    normal = { textColor = new Color(.89f, .81f, .68f) }
                });
            if (!running) GUI.Label(new Rect(card.x + 14f, card.y + 151f,
                card.width - 28f, 21f), cost,
                new GUIStyle(smallStyle)
                {
                    fontSize = compact ? 11 : 12, wordWrap = true,
                    normal = { textColor = new Color(.95f, .77f, .39f) }
                });

            float actionW = Mathf.Min(compact ? 164f : 176f, card.width * .41f);
            Rect action = new Rect(card.xMax - actionW - 12f, card.yMax - 47f,
                actionW, 39f);
            // Preserve the real FTUE research-start arrow. This premium card
            // lives inside a scroll view: convert its action to screen pixels.
            if (string.Equals(id, BeeKingdom.Tutorial.FtueTutorialRegistry.FirstResearchId,
                    StringComparison.Ordinal))
            {
                Rect screenAction = new Rect(
                    viewport.x + action.x - scrollOffset.x,
                    viewport.y + action.y - scrollOffset.y,
                    action.width, action.height);
                if (screenAction.Overlaps(viewport))
                {
                    try
                    {
                        BeeKingdom.Tutorial.TutorialTargetRegistry.Instance.RegisterScreenRect(
                            BeeKingdom.Tutorial.FtueTutorialRegistry.TargetResearchStartButton,
                            screenAction);
                    }
                    catch { }
                }
            }
            if (DrawAllianceActivitiesButton(action, button, actionable, true))
            {
                if (official) RunOfficialResearchAction(id);
                else StartPreviewResearch(id);
            }
            if (running && official && model?.ActiveOperation != null)
            {
                float progress = model.ActiveOperation.IsAwaitingCompletion ? 1f
                    : researchController == null ? 0f
                    : (float)model.Progress01(researchController.Elapsed);
                DrawProgressBar(new Rect(card.x + 14f, card.yMax - 14f,
                    card.width - actionW - 40f, 6f), progress);
                if (!model.ActiveOperation.IsAwaitingCompletion)
                {
                    double duration = (model.ActiveOperation.CompletesAtUtc -
                        model.ActiveOperation.StartedAtUtc).TotalSeconds;
                    DrawAllianceHelpAction(new Rect(card.x + 14f, card.yMax - 56f,
                            card.width - actionW - 40f, 28f),
                        MobileAccountSessionRuntimeBootstrap.GameplayHiveId,
                        BeeKingdom.Networking.RemoteAllianceHelpCategories.Research,
                        id, duration, true);
                }
            }
            else if (!string.IsNullOrWhiteSpace(reason) && !completed)
            {
                GUI.Label(new Rect(card.x + 14f, card.yMax - 42f,
                    card.width - actionW - 36f, 32f), reason,
                    new GUIStyle(smallStyle)
                    {
                        fontSize = compact ? 10 : 12,
                        wordWrap = true,
                        normal = { textColor = new Color(.74f, .68f, .58f) }
                    });
            }
        }
    }
}