using System;
using System.Collections.Generic;
using System.Globalization;
using BeeKingdom.Localization;
using BeeKingdom.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeeKingdom.Playground
{
    // This is a display-only companion to the approved Activities-v4 window.
    // Academy owns neither Research nor Barracks mutations. Both are shown
    // only as account-scoped, read-only signals with links to their real screens.
    public sealed partial class HiveMapPremiumBuildingWindow
    {
        private bool academyMode;
        private float nextAcademyLabelsAt;
        private float nextAcademyRefreshAt;
        private TextMeshProUGUI academyLevelLabel;
        private TextMeshProUGUI academySyncLabel;
        private TextMeshProUGUI academyUpgradeLabel;
        private TextMeshProUGUI academyUpgradeCostLabel;
        private TextMeshProUGUI academyPalaceLabel;
        private TextMeshProUGUI academyPalaceNoteLabel;
        private TextMeshProUGUI academyBarracksLabel;
        private TextMeshProUGUI academyResearchLabel;
        private TextMeshProUGUI academyRecordsLabel;
        private readonly Dictionary<string, TextMeshProUGUI> academyCourseStatusLabels =
            new Dictionary<string, TextMeshProUGUI>(StringComparer.Ordinal);
        private readonly Dictionary<string, Button> academyCourseButtons =
            new Dictionary<string, Button>(StringComparer.Ordinal);

        public bool AcademyActiveForProof => windowOpen && academyMode;

        public static int AcademyProgramColumnsForProof(float cardWidth)
            => cardWidth >= 920f ? 2 : 1;
        public static float AcademyProgramHeightForProof(float cardWidth)
            => cardWidth < 540f ? 349f : 260f;

        private static string AcademyText(string key)
            => BeeLocalization.Text("academy.premium." + key);

        private static void UpdateAcademyText(TextMeshProUGUI label, string value)
        {
            if (label != null &&
                !string.Equals(label.text, value, StringComparison.Ordinal))
                label.text = value;
        }

        private void RefreshAcademySources()
        {
            if (!windowOpen || !academyMode) return;
            AcademyTrainingPanelController academy =
                MobileAccountSessionRuntimeBootstrap.AcademyControllerForHiveMap;
            if (academy != null && academy.IsConfigured && !academy.IsBusy)
                academy.Refresh();
            // Read the existing authenticated building progression, never
            // duplicate, simulate or modify a building upgrade.
            HiveViewProductUiPresenter.RefreshOfficialAcademyBuildingForExternalHost();

            // These are the OWNERS of their respective state, not Academy
            // operations. Avoid interrupting an in-flight start/claim.
            IHiveDoctrineRecruitmentPanelController barracks =
                MobileAccountSessionRuntimeBootstrap.DoctrineRecruitmentControllerForHiveMap;
            if (barracks != null && barracks.IsConfigured &&
                !barracks.IsBusy && !(barracks.Model?.IsPending ?? false))
                barracks.Refresh();

            IHiveResearchPanelController research =
                MobileAccountSessionRuntimeBootstrap.ResearchControllerForHiveMap;
            if (research != null && research.IsConfigured &&
                !research.IsBusy && research.Model != null &&
                research.Model.State != HiveResearchScreenState.Starting &&
                research.Model.State != HiveResearchScreenState.Completing)
                research.RefreshQuietly();
        }

        private void OpenAcademyBarracks()
        {
            closeAction?.Invoke();
            HiveViewProductUiPresenter.OpenBarrackOverlayForExternalHost();
        }

        private void OpenAcademyResearch()
        {
            closeAction?.Invoke();
            HiveViewProductUiPresenter.OpenResearchOverlayForExternalHost();
            IHiveResearchPanelController controller =
                MobileAccountSessionRuntimeBootstrap.ResearchControllerForHiveMap;
            if (controller != null && controller.IsConfigured && !controller.IsBusy)
                controller.Refresh();
        }

        private void RenderAcademyCards(Transform parent, float width,
            float left, out float cursor, float viewportHeight)
        {
            cursor = 14f;
            academyCourseStatusLabels.Clear();
            academyCourseButtons.Clear();
            academyRecordsLabel = null;
            bool compact = width < 540f;
            bool twoColumns = AcademyProgramColumnsForProof(width) == 2;
            float gap = 16f;
            float inner = Mathf.Max(1f, width - 36f);

            float stateHeight = compact ? 241f : 185f;
            RectTransform state = CardAt(parent,
                new Rect(left, cursor, width, stateHeight),
                AcademyText("card.state"), "research");
            academyLevelLabel = Label(state,
                new Rect(18f, 73f, inner, compact ? 45f : 35f),
                AcademyText("level.awaiting"),
                compact ? 19f : 23f, Gold, true);
            academySyncLabel = Label(state,
                new Rect(18f, compact ? 123f : 117f, inner,
                    compact ? 47f : 26f),
                AcademyText("sync.loading"), 14f, Paper);
            Label(state,
                new Rect(18f, compact ? 182f : 149f, inner,
                    compact ? 53f : 27f),
                AcademyText("state.note"), 13f, Muted);
            cursor += stateHeight + 19f;

            float buildHeight = compact ? 234f : 188f;
            RectTransform construction = CardAt(parent,
                new Rect(left, cursor, width, buildHeight),
                AcademyText("card.construction"), "upgrade");
            academyUpgradeLabel = Label(construction,
                new Rect(18f, 75f, inner, compact ? 64f : 40f),
                AcademyText("upgrade.awaiting"), 17f, Gold, true);
            academyUpgradeCostLabel = Label(construction,
                new Rect(18f, compact ? 145f : 119f, inner,
                    compact ? 81f : 62f),
                AcademyText("upgrade.construction_link"), 14f, Paper);
            cursor += buildHeight + 19f;

            float palaceHeight = compact ? 225f : 179f;
            RectTransform palace = CardAt(parent,
                new Rect(left, cursor, width, palaceHeight),
                AcademyText("card.palace"), "queen-core");
            academyPalaceLabel = Label(palace,
                new Rect(18f, 74f, inner, compact ? 83f : 47f),
                AcademyText("palace.awaiting"), 15f, Gold, true);
            academyPalaceNoteLabel = Label(palace,
                new Rect(18f, compact ? 164f : 128f, inner,
                    compact ? 53f : 44f),
                AcademyText("palace.note"), 13f, Muted);
            cursor += palaceHeight + 19f;

            float overviewHeight = compact ? 217f : 144f;
            RectTransform introduction = CardAt(parent,
                new Rect(left, cursor, width, overviewHeight),
                AcademyText("card.mission"), "bee");
            Label(introduction,
                new Rect(18f, 75f, inner, compact ? 129f : 62f),
                AcademyText("mission"), 15f, Paper);
            cursor += overviewHeight + 19f;

            string[] programmes =
                { "tactics", "logistics", "exploration", "mentorship" };
            string[] icons = { "shield", "hive", "flower", "queen-bee" };
            float courseHeight = AcademyProgramHeightForProof(width);
            float courseWidth = twoColumns ? (width - gap) * .5f : width;
            for (int i = 0; i < programmes.Length; i++)
            {
                string kind = programmes[i];
                int row = twoColumns ? i / 2 : i;
                int column = twoColumns ? i % 2 : 0;
                RectTransform programme = CardAt(parent,
                    new Rect(left + column * (courseWidth + gap),
                        cursor + row * (courseHeight + gap),
                        courseWidth, courseHeight),
                    AcademyText("course." + kind + ".title"), icons[i]);
                Label(programme,
                    new Rect(18f, 77f, courseWidth - 36f,
                        compact ? 94f : 62f),
                    AcademyText("course." + kind + ".description"),
                    14f, Paper);
                academyCourseStatusLabels[kind] = Label(programme,
                    new Rect(18f, compact ? 177f : 140f, courseWidth - 36f,
                        compact ? 100f : 62f),
                    AcademyText("course.awaiting"), 13f, Gold, true);
                float courseButtonWidth = Mathf.Min(221f,courseWidth - 36f);
                Button enrollmentButton = ButtonAt(programme,
                    new Rect(Mathf.Max(18f,courseWidth - courseButtonWidth - 18f),
                        courseHeight - 54f,courseButtonWidth,43f),
                    AcademyText("course.start"),true,() => StartAcademyCourse(kind));
                enrollmentButton.interactable = false;
                academyCourseButtons[kind] = enrollmentButton;
            }
            cursor += (twoColumns ? 2 : programmes.Length) *
                (courseHeight + gap) + 2f;

            // These are ONLY observable real operations in OTHER buildings.
            // Their buttons navigate to the authoritative owners of mutations.
            float activityHeight = compact ? 295f : 239f;
            float activityWidth = twoColumns ? (width - gap) * .5f : width;
            RectTransform training = CardAt(parent,
                new Rect(left, cursor, activityWidth, activityHeight),
                AcademyText("card.training"), "guard");
            academyBarracksLabel = Label(training,
                new Rect(18f, 76f, activityWidth - 36f,
                    compact ? 78f : 58f),
                AcademyText("training.awaiting"), 15f, Paper, true);
            Label(training,
                new Rect(18f, compact ? 164f : 138f,
                    activityWidth - 36f, compact ? 59f : 41f),
                AcademyText("training.owner"), 13f, Muted);
            float buttonWidth = Mathf.Min(229f, activityWidth - 36f);
            ButtonAt(training,
                new Rect(Mathf.Max(18f, activityWidth - buttonWidth - 18f),
                    activityHeight - 54f, buttonWidth, 43f),
                AcademyText("training.open"), true, OpenAcademyBarracks);

            RectTransform studies = CardAt(parent,
                new Rect(twoColumns ? left + activityWidth + gap : left,
                    twoColumns ? cursor : cursor + activityHeight + 19f,
                    activityWidth, activityHeight),
                AcademyText("card.research"), "research");
            academyResearchLabel = Label(studies,
                new Rect(18f, 76f, activityWidth - 36f,
                    compact ? 78f : 58f),
                AcademyText("research.awaiting"), 15f, Paper, true);
            Label(studies,
                new Rect(18f, compact ? 164f : 138f,
                    activityWidth - 36f, compact ? 59f : 41f),
                AcademyText("research.owner"), 13f, Muted);
            ButtonAt(studies,
                new Rect(Mathf.Max(18f, activityWidth - buttonWidth - 18f),
                    activityHeight - 54f, buttonWidth, 43f),
                AcademyText("research.open"), true, OpenAcademyResearch);
            cursor += (twoColumns ? activityHeight : activityHeight * 2f + 19f) + 19f;

            // Reserve enough space for the four durable diploma entries, each
            // with its real server timestamp, also on portrait phone screens.
            float journalHeight = compact ? 334f : 260f;
            RectTransform journal = CardAt(parent,
                new Rect(left, cursor, width, journalHeight),
                AcademyText("card.records"), "research");
            academyRecordsLabel = Label(journal,
                new Rect(18f, 74f, inner, journalHeight - 84f),
                AcademyText("records.no_academy_history"), 14f, Muted);
            cursor += journalHeight + 19f;

            RectTransform content = parent as RectTransform;
            if (content != null)
                content.sizeDelta = new Vector2(0f,
                    Mathf.Max(cursor + 24f, viewportHeight + 1f));
            RefreshAcademyLabels();
        }

        private void StartAcademyCourse(string course)
        {
            AcademyTrainingPanelController controller =
                MobileAccountSessionRuntimeBootstrap.AcademyControllerForHiveMap;
            if (controller == null || !controller.IsConfigured ||
                controller.IsBusy)
                return;
            // No technical retries are initiated by player buttons.
            // Background refresh safely reconciles with the server.
            if (controller.State != AcademyTrainingScreenState.Ready)
                return;
            RemoteAcademySnapshot model = controller.Model;
            if (model == null) return;
            if (model.ActiveOperation != null &&
                string.Equals(model.ActiveOperation.CourseId,course,StringComparison.Ordinal))
            {
                if (controller.ReadyToComplete) controller.Complete();
                return;
            }
            RemoteAcademyCourse offer = model.Courses?.Find(x=>x.CourseId==course);
            if (offer != null && offer.CanStart) controller.Start(course);
        }

        private static void SetAcademyCourseButton(Button button,
            string label,bool enabled)
        {
            if (button == null) return;
            if (button.interactable != enabled) button.interactable = enabled;
            if (button.targetGraphic is RawImage graphic)
            {
                Color color = enabled ? new Color(.91f,.62f,.23f)
                    : new Color(.19f,.14f,.085f);
                if (graphic.color != color) graphic.color = color;
            }
            TextMeshProUGUI caption = button.GetComponentInChildren<TextMeshProUGUI>();
            if (caption != null)
            {
                UpdateAcademyText(caption,label);
                Color textColor = enabled ? Ink : Muted;
                if (caption.color != textColor) caption.color = textColor;
            }
        }

        private void RefreshAcademyTrainingLabels()
        {
            AcademyTrainingPanelController ctrl =
                MobileAccountSessionRuntimeBootstrap.AcademyControllerForHiveMap;
            RemoteAcademySnapshot model = ctrl?.Model;
            bool ready = ctrl != null && ctrl.IsConfigured &&
                ctrl.State == AcademyTrainingScreenState.Ready && !ctrl.IsBusy;
            bool offline = ctrl != null &&
                ctrl.State == AcademyTrainingScreenState.OfflineReadOnly;
            // Quietly retain the last verified Academy snapshot during reads.
            // Network state is deliberately NOT shown on course cards.
            string statusText = AcademyText("course.awaiting");

            foreach (string course in new[]
                { "tactics","logistics","exploration","mentorship" })
            {
                academyCourseStatusLabels.TryGetValue(course,
                    out TextMeshProUGUI status);
                academyCourseButtons.TryGetValue(course,out Button button);
                if (status == null || button == null) continue;
                if (model == null)
                {
                    UpdateAcademyText(status,statusText);
                    SetAcademyCourseButton(button,AcademyText("course.start"),false);
                    continue;
                }
                RemoteAcademyCourse offer = model.Courses?.Find(x=>x.CourseId==course);
                if (offer == null)
                {
                    UpdateAcademyText(status,AcademyText("course.awaiting"));
                    SetAcademyCourseButton(button,AcademyText("course.start"),false);
                    continue;
                }
                bool completed = model.Completed != null &&
                    model.Completed.Exists(x=>x.CourseId==course);
                bool current = model.ActiveOperation != null &&
                    model.ActiveOperation.CourseId == course;
                List<string> costs = new List<string>();
                if (offer.Costs != null)
                {
                    foreach (KeyValuePair<string,long> cost in offer.Costs)
                        costs.Add(BeeLocalization.Text("resource."+cost.Key,cost.Key) +
                            " "+cost.Value.ToString("N0",CultureInfo.CurrentCulture));
                }
                string summary = string.Join(" | ",costs);
                string requirements = string.Format(CultureInfo.CurrentCulture,
                    AcademyText("course.requirements"),offer.RequiredAcademyLevel,
                    GeneticsDuration(offer.Duration));
                if (completed)
                {
                    UpdateAcademyText(status,AcademyText("course.completed")+
                        "  |  "+AcademyText("course.credit"));
                    SetAcademyCourseButton(button,AcademyText("course.completed_button"),false);
                }
                else if (current)
                {
                    bool timeElapsed = ctrl.TrainingElapsed;
                    bool canFinish = ready && ctrl.ReadyToComplete;
                    // A finished timer stays visibly finished while a silent
                    // read confirms the server authorizes certificate pickup.
                    UpdateAcademyText(status,timeElapsed
                        ? AcademyText("course.ready")
                        : string.Format(CultureInfo.CurrentCulture,
                            AcademyText("course.running"),GeneticsDuration(ctrl.Remaining)));
                    SetAcademyCourseButton(button,
                        AcademyText(timeElapsed ? "course.complete" : "course.in_progress"),
                        canFinish);
                }
                else
                {
                    string text = requirements + "  |  " + summary;
                    if (!string.Equals(offer.State,"available",StringComparison.Ordinal))
                    {
                        text += "  |  " + AcademyText("course.locked");
                        if (model.AcademyBuildingLevel < offer.RequiredAcademyLevel)
                            text += "  |  " + AcademyText("course.level_lock");
                        if (offer.Prerequisites != null)
                        {
                            foreach (string prerequisite in offer.Prerequisites)
                            {
                                if (!model.Completed.Exists(x => x.CourseId == prerequisite))
                                    text += "  |  " +
                                        string.Format(CultureInfo.CurrentCulture,
                                            AcademyText("course.missing_certificate"),
                                            AcademyText("course." + prerequisite + ".title"));
                            }
                        }
                    }
                    else if (!offer.CanStart)
                    {
                        bool shortOfResources = false;
                        if (offer.Costs != null)
                        {
                            foreach (KeyValuePair<string,long> cost in offer.Costs)
                            {
                                long balance = 0;
                                if (model.Balances != null &&
                                    model.Balances.TryGetValue(cost.Key,
                                        out RemoteAcademyBalance amount) &&
                                    amount != null)
                                    balance = amount.Amount;
                                if (balance < cost.Value)
                                {
                                    shortOfResources = true;
                                    text += "  |  " +
                                        string.Format(CultureInfo.CurrentCulture,
                                            AcademyText("course.missing_resource"),
                                            (cost.Value - balance).ToString("N0",
                                                CultureInfo.CurrentCulture),
                                            BeeLocalization.Text("resource." + cost.Key,
                                                cost.Key));
                                }
                            }
                        }
                        if (!shortOfResources)
                            text += "  |  " +
                                AcademyText("course.insufficient_or_busy");
                    }
                    UpdateAcademyText(status,text);
                    SetAcademyCourseButton(button,AcademyText("course.start"),
                        ready && offer.CanStart && model.ActiveOperation == null);
                }
            }
            if (academyRecordsLabel != null)
            {
                string history = model == null ? AcademyText("records.no_academy_history")
                    : model.Completed.Count == 0
                        ? AcademyText("records.no_courses")
                        : string.Format(CultureInfo.CurrentCulture,
                            AcademyText("records.summary"),
                            model.Completed.Count,model.EarnedCredits);
                if (model?.Completed != null && model.Completed.Count > 0)
                {
                    foreach (RemoteAcademyCompletion item in model.Completed)
                        history += "\n- " +
                            AcademyText("course."+item.CourseId+".title") +
                            "  (" + item.CompletedAtUtc.ToLocalTime().ToString(
                                "g",CultureInfo.CurrentCulture) + ")";
                }
                UpdateAcademyText(academyRecordsLabel,history);
            }
        }

        private void RefreshAcademyLabels()
        {
            if (!academyMode || !windowOpen || suppressed ||
                academyLevelLabel == null || academySyncLabel == null)
                return;
            RefreshAcademyTrainingLabels();

            HiveBuildingUpgradeScreenModel model =
                HiveViewProductUiPresenter.OfficialAcademyUpgradeModelForExternalHost();
            int? level =
                HiveViewProductUiPresenter.OfficialAcademyBuildingLevelForExternalHost();
            UpdateAcademyText(academyLevelLabel,
                level.HasValue
                    ? string.Format(CultureInfo.CurrentCulture,
                        AcademyText("level.value"), level.Value)
                    : AcademyText("level.awaiting"));

            string sync = model == null ||
                model.State == HiveBuildingUpgradeScreenState.NotConfigured
                    ? "sync.loading"
                    : model.State == HiveBuildingUpgradeScreenState.Error
                        ? "sync.loading"
                        : model.State == HiveBuildingUpgradeScreenState.Loading ||
                          model.State == HiveBuildingUpgradeScreenState.Starting ||
                          model.State == HiveBuildingUpgradeScreenState.Completing
                            ? "sync.loading"
                            : model.State == HiveBuildingUpgradeScreenState.OfflineReadOnly
                                ? "sync.offline" : "sync.ready";
            // Polling temporarily moves the shared model into Loading. Do not
            // flash the status or erase verified data every 30 seconds.
            bool retained = level.HasValue && model != null &&
                model.State == HiveBuildingUpgradeScreenState.Loading &&
                (academySyncLabel.text == AcademyText("sync.ready") ||
                 academySyncLabel.text == AcademyText("sync.offline") ||
                 academySyncLabel.text == AcademyText("sync.error"));
            if (!retained)
                UpdateAcademyText(academySyncLabel, AcademyText(sync));

            HiveBuildingUpgradeOperationModel operation =
                HiveViewProductUiPresenter.OfficialAcademyUpgradeOperationForExternalHost();
            if (operation != null)
            {
                string current = operation.IsAwaitingCompletion
                    ? string.Format(CultureInfo.CurrentCulture,
                        AcademyText("upgrade.complete"), operation.ToLevel)
                    : string.Format(CultureInfo.CurrentCulture,
                        AcademyText("upgrade.running"), operation.ToLevel,
                        GeneticsDuration(
                            HiveViewProductUiPresenter
                                .OfficialAcademyUpgradeRemainingForExternalHost()
                            ?? TimeSpan.Zero));
                UpdateAcademyText(academyUpgradeLabel, current);
                UpdateAcademyText(academyUpgradeCostLabel,
                    AcademyText("upgrade.construction_link"));
            }
            else
            {
                HiveBuildingUpgradeOfferModel offer =
                    HiveViewProductUiPresenter.OfficialAcademyUpgradeOfferForExternalHost();
                if (offer == null)
                {
                    UpdateAcademyText(academyUpgradeLabel,
                        AcademyText(!level.HasValue ? "upgrade.awaiting" : "upgrade.max"));
                    UpdateAcademyText(academyUpgradeCostLabel,
                        AcademyText("upgrade.construction_link"));
                }
                else
                {
                    UpdateAcademyText(academyUpgradeLabel,
                        string.Format(CultureInfo.CurrentCulture,
                            AcademyText("upgrade.next"),
                            offer.FromLevel, offer.ToLevel));
                    List<string> costs = new List<string>();
                    foreach (KeyValuePair<string, long> cost in offer.Costs)
                        costs.Add(BeeLocalization.Text(
                            "resource." + cost.Key, cost.Key) + " " +
                            cost.Value.ToString("N0", CultureInfo.CurrentCulture));
                    string summary = costs.Count > 0
                        ? string.Join("  |  ", costs)
                        : AcademyText("upgrade.no_cost");
                    UpdateAcademyText(academyUpgradeCostLabel,
                        string.Format(CultureInfo.CurrentCulture,
                            AcademyText("upgrade.cost_duration"),
                            summary, GeneticsDuration(offer.Duration)));
                }
            }

            // The Palace's next-level requirements are provided by the SAME
            // building snapshot. Do not invent Academy level thresholds or buffs.
            HiveRoyalPalaceProgressionModel palace =
                HiveViewProductUiPresenter.OfficialAcademyRoyalPalaceForExternalHost();
            if (palace == null)
            {
                UpdateAcademyText(academyPalaceLabel, AcademyText("palace.awaiting"));
                UpdateAcademyText(academyPalaceNoteLabel,
                    AcademyText("palace.note"));
            }
            else if (palace.IsMaxConfiguredLevel || !palace.NextLevel.HasValue)
            {
                UpdateAcademyText(academyPalaceLabel, AcademyText("palace.max"));
                UpdateAcademyText(academyPalaceNoteLabel,
                    AcademyText("palace.note"));
            }
            else
            {
                HiveRoyalPalaceRequirementModel requirement = null;
                foreach (HiveRoyalPalaceRequirementModel candidate
                    in palace.NextLevelRequirements)
                {
                    if (string.Equals(candidate.BuildingKey, "academy_canopy",
                        StringComparison.Ordinal))
                    {
                        requirement = candidate;
                        break;
                    }
                }
                if (requirement == null)
                {
                    UpdateAcademyText(academyPalaceLabel,
                        string.Format(CultureInfo.CurrentCulture,
                            AcademyText("palace.no_requirement"), palace.NextLevel.Value));
                    UpdateAcademyText(academyPalaceNoteLabel,
                        AcademyText("palace.note"));
                }
                else
                {
                    UpdateAcademyText(academyPalaceLabel,
                        string.Format(CultureInfo.CurrentCulture,
                            AcademyText("palace.requirement"), palace.NextLevel.Value,
                            requirement.MinimumLevel, requirement.CurrentLevel));
                    UpdateAcademyText(academyPalaceNoteLabel,
                        AcademyText(requirement.IsSatisfied
                            ? "palace.satisfied" : "palace.unsatisfied"));
                }
            }

            // A recruitment operation is a BARRACKS operation, not an Academy
            // class or diploma. Its server state is only projected here.
            IHiveDoctrineRecruitmentPanelController barracks =
                MobileAccountSessionRuntimeBootstrap.DoctrineRecruitmentControllerForHiveMap;
            HiveDoctrineRecruitmentScreenModel recruitment =
                barracks != null && barracks.IsConfigured ? barracks.Model : null;
            bool barracksReadable = recruitment != null &&
                (recruitment.State == HiveDoctrineRecruitmentScreenState.Ready ||
                 recruitment.State == HiveDoctrineRecruitmentScreenState.OfflineReadOnly ||
                 recruitment.State == HiveDoctrineRecruitmentScreenState.Mutating ||
                 recruitment.State == HiveDoctrineRecruitmentScreenState.PendingConfirmation ||
                 recruitment.Revision > 0L);
            if (!barracksReadable)
                UpdateAcademyText(academyBarracksLabel,
                    AcademyText("training.awaiting"));
            else if (recruitment.ActiveOperation != null)
            {
                HiveDoctrineRecruitmentOperationModel job =
                    recruitment.ActiveOperation;
                string family = BeeLocalization.Text(
                    "defense.premium.family." + job.Family, job.Family);
                UpdateAcademyText(academyBarracksLabel,
                    job.AwaitingCompletion
                        ? string.Format(CultureInfo.CurrentCulture,
                            AcademyText("training.ready"), family, job.BatchSize)
                        : string.Format(CultureInfo.CurrentCulture,
                            AcademyText("training.active"), family, job.BatchSize,
                            GeneticsDuration(recruitment.Remaining(barracks.Elapsed))));
            }
            else
                UpdateAcademyText(academyBarracksLabel,
                    AcademyText("training.idle"));

            // The Research Center is a separate, live source of technology.
            // Its queue is not mislabeled an Academy course.
            IHiveResearchPanelController research =
                MobileAccountSessionRuntimeBootstrap.ResearchControllerForHiveMap;
            HiveResearchScreenModel researchModel =
                research != null && research.IsConfigured ? research.Model : null;
            bool researchReadable = researchModel != null &&
                (researchModel.State == HiveResearchScreenState.Ready ||
                 researchModel.State == HiveResearchScreenState.OfflineReadOnly ||
                 researchModel.State == HiveResearchScreenState.Starting ||
                 researchModel.State == HiveResearchScreenState.Completing ||
                 researchModel.Revision > 0L);
            if (!researchReadable)
                UpdateAcademyText(academyResearchLabel,
                    AcademyText("research.awaiting"));
            else if (researchModel.ActiveOperation != null)
            {
                UpdateAcademyText(academyResearchLabel,
                    researchModel.ActiveOperation.IsAwaitingCompletion
                        ? AcademyText("research.ready")
                        : string.Format(CultureInfo.CurrentCulture,
                            AcademyText("research.active"),
                            GeneticsDuration(
                                researchModel.Remaining(research.Elapsed))));
            }
            else
            {
                UpdateAcademyText(academyResearchLabel,
                    string.Format(CultureInfo.CurrentCulture,
                        AcademyText("research.completed"), researchModel.Completed.Count));
            }
        }
    }
}
