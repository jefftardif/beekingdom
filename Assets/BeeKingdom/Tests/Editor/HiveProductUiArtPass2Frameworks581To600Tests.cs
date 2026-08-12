using BeeKingdom.Colony;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class HiveProductUiArtPass2Frameworks581To600Tests
    {
        [Test]
        public void IntakeAssetsPortraitShotsHudDetailGestureAtmosphereZonesAndAccessibilityAreReady()
        {
            HiveProductUiArtPass2Intake intake = new HiveProductUiArtPass2Intake(new[] { new ArtPass2Reserve("Assets", "BEE-582", true) }, new[]
            {
                Target("FinalAssets"), Target("MobileDevicePortrait"), Target("DedicatedCapture"), Target("AccessibilityDeviceAudit"),
                Target("UiScorecard"), Target("QaReadability"), Target("ServerNonClaims"), Target("BuilderBundle")
            }, Bee600GateStatus.PreparingEvidence);

            FinalHiveAssetReplacementPlan assets = new FinalHiveAssetReplacementPlan(new[]
            {
                Replacement("ResourceIcons"), Replacement("ZoneIcons"), Replacement("StateBadges"), Replacement("WaxTexture"), Replacement("CellBorders"), Replacement("DetailPanelChrome"), Replacement("BottomRailIcons")
            }, new[]
            {
                Fallback("ResourceIcons"), Fallback("ZoneIcons"), Fallback("StateBadges"), Fallback("WaxTexture"), Fallback("CellBorders"), Fallback("DetailPanelChrome"), Fallback("BottomRailIcons")
            });

            MobileDevicePortraitEvidence portrait = new MobileDevicePortraitEvidence(new PortraitEvidenceShot("MobilePortrait", "reserve", false), new[]
            {
                Portrait("HudVisible"), Portrait("HiveVisible"), Portrait("RailAccessible"), Portrait("PanelCollapsible"), Portrait("SelectionReadable"), Portrait("PreviewDisclosureVisible")
            });

            Bee600VisualShotList shots = new Bee600VisualShotList(new[]
            {
                Shot("OverviewDesktop"), Shot("MobilePortrait"), Shot("DetailOpen"), Shot("HudCloseRead"), Shot("StateTokenSample"), Shot("EmptyLockedFutureRoom"), Shot("AccessibilityReducedView")
            }, new[]
            {
                Failure("OverviewDesktop"), Failure("MobilePortrait"), Failure("DetailOpen"), Failure("HudCloseRead"), Failure("StateTokenSample"), Failure("EmptyLockedFutureRoom"), Failure("AccessibilityReducedView")
            });

            ResourceHudFinalReadability hud = new ResourceHudFinalReadability(new[] { Hud("RecognizableIcon"), Hud("ShortLabel"), Hud("UnofficialValue"), Hud("LocalBadge"), Hud("StableOrder"), Hud("Contrast") }, ResourceHudCaptureMode.OverviewAndPortrait);
            DetailPanelFinalHierarchy detail = new DetailPanelFinalHierarchy(new PanelSpacingRule("Header", 12, true), new PanelSpacingRule("State", 10, true), new PanelSpacingRule("Disclosure", 12, true), new PanelTextFitRule(true, true, true));
            GesturePathClarityMap gestures = new GesturePathClarityMap(new[] { Gesture("SelectCell"), Gesture("OpenDetail"), Gesture("CloseDetail"), Gesture("ReturnHome"), Gesture("SwitchLens"), Gesture("ReadPreview") }, new[] { new RailCoherenceRule("Rail", true) });
            HiveAtmosphereMotionLighting atmosphere = new HiveAtmosphereMotionLighting(new LightingTreatment("Wax", true), new MotionTreatment("Pulse", true, false), new PerformanceGuard("Budget", true, true));
            ZoneIconRecognitionSet zones = new ZoneIconRecognitionSet(new[] { Zone("Nursery"), Zone("Storage"), Zone("Defense"), Zone("Research"), Zone("Future") }, ZoneVariants());
            DeviceAccessibilityAudit accessibility = new DeviceAccessibilityAudit(new[] { Device("HudContrast"), Device("SelectionContrast"), Device("TargetSize"), Device("TextFit"), Device("NonColorOnlyStates"), Device("ReducedScreenshotReadability") }, new[] { new DeviceAccessibilityReserve("DevicePending", true) });

            Assert.That(intake.Bee600Gate, Is.EqualTo(Bee600GateStatus.PreparingEvidence));
            Assert.That(assets.Verdict, Is.EqualTo(AssetReplacementVerdict.Ready));
            Assert.That(portrait.Verdict, Is.EqualTo(PortraitEvidenceVerdict.DeviceReserveRequired));
            Assert.That(shots.Verdict, Is.EqualTo(VisualShotListVerdict.Ready));
            Assert.That(hud.Verdict, Is.EqualTo(ResourceHudReadabilityVerdict.Ready));
            Assert.That(detail.IsReady, Is.True);
            Assert.That(gestures.Verdict, Is.EqualTo(GestureClarityVerdict.Ready));
            Assert.That(atmosphere.Verdict, Is.EqualTo(AtmosphereVerdict.Ready));
            Assert.That(zones.Verdict, Is.EqualTo(ZoneIconRecognitionVerdict.Ready));
            Assert.That(accessibility.Verdict, Is.EqualTo(DeviceAccessibilityVerdict.ReadyWithDeviceReserve));
        }

        [Test]
        public void SessionDisclosureDemoUiQaServerBuilderRegressionAndLedgerAreReady()
        {
            FirstSessionHiveVisualPath path = new FirstSessionHiveVisualPath(new[]
            {
                Step("RecognizeHive", 1), Step("ReadHud", 2), Step("SeeSelectedCell", 3), Step("OpenDetail", 4), Step("ReadPreviewLimit", 5), Step("ReturnHome", 6)
            }, new[] { new ComprehensionCheckpoint("Read", true) });

            FinalNonClaimDisclosureMatrix disclosure = new FinalNonClaimDisclosureMatrix(new[]
            {
                Disclosure("ResourceValue"), Disclosure("Capacity"), Disclosure("FutureRoom"), Disclosure("LockedState"), Disclosure("ServerRequired"), Disclosure("PreviewGesture"), Disclosure("DetailAction")
            }, new[] { new ForbiddenOfficialClaim("OfficialAction", false) });

            Bee600DemoCapturePipeline demo = new Bee600DemoCapturePipeline(new[]
            {
                Pipe("PrepareScene", 1), Pipe("CaptureDesktop", 2), Pipe("CapturePortrait", 3), Pipe("CaptureDetail", 4), Pipe("CaptureReducedView", 5), Pipe("WriteManifest", 6), Pipe("FileReserves", 7)
            }, new CaptureManifest("SandboxPlayground", true, true));

            Bee600UiScorecard ui = new Bee600UiScorecard(new[]
            {
                Axis("HiveIdentity"), Axis("AssetFinish"), Axis("HudReadability"), Axis("PortraitFit"), Axis("DetailPanel"), Axis("GestureClarity"), Axis("StateLanguage"), Axis("Accessibility"), Axis("NonClaimClarity")
            }, new[] { new UiPriorityReserve("A", 1), new UiPriorityReserve("B", 2), new UiPriorityReserve("C", 3) });

            Bee600QaReadabilityProtocol qa = new Bee600QaReadabilityProtocol(new[]
            {
                Prompt("RecognizeHive"), Prompt("ReadHud"), Prompt("IdentifyTwoZones"), Prompt("FindSelection"), Prompt("CloseDetail"), Prompt("ExplainPreview"), Prompt("ConfirmMobilePortrait")
            }, new[]
            {
                Answer("RecognizeHive"), Answer("ReadHud"), Answer("IdentifyTwoZones"), Answer("FindSelection"), Answer("CloseDetail"), Answer("ExplainPreview"), Answer("ConfirmMobilePortrait")
            }, new[] { new QaBlockingFailure("None", false) });

            Bee600ServerAuthorityClaimAudit server = new Bee600ServerAuthorityClaimAudit(new[]
            {
                Claim("ResourceBalance"), Claim("ConstructionAction"), Claim("FutureRoomUnlock"), Claim("ProgressionLayer"), Claim("SavedSelection"), Claim("SyncStatus"), Claim("RewardClaim")
            }, new[] { new ForbiddenAuthoritySignal("Live", false) });

            BuilderBee600VisualEvidenceBundle builder = new BuilderBee600VisualEvidenceBundle(new[]
            {
                Artifact("CodeTouchpoints"), Artifact("AssetManifest"), Artifact("FocusedTests"), Artifact("CompileLog"), Artifact("SceneValidation"), Artifact("CaptureReadinessNote"), Artifact("KnownReserves")
            }, new[] { new BuilderReserve("ValidationPending", false) });

            HiveProductVisualRegressionLock regression = new HiveProductVisualRegressionLock(new[]
            {
                Blocker("BlankScene"), Blocker("DebugOnlySurface"), Blocker("MissingHud"), Blocker("MissingHiveCells"), Blocker("MissingDetailPanel"), Blocker("BrokenPortrait"), Blocker("OfficialClaimVisible")
            }, new[] { new VisualRegressionReserve("DevicePending", true) });

            Bee600CrossTeamVisualReviewLedger ledger = new Bee600CrossTeamVisualReviewLedger(new[]
            {
                Row("BuilderBundle"), Row("DemoShots"), Row("UiScorecard"), Row("QaReadability"), Row("ServerClaims"), Row("AccessibilityAudit"), Row("RegressionLocks")
            }, new[] { new CrossTeamReserve("ArchitectReviewPending", false) });

            Assert.That(path.Verdict, Is.EqualTo(FirstSessionVerdict.Ready));
            Assert.That(disclosure.Verdict, Is.EqualTo(NonClaimDisclosureVerdict.Ready));
            Assert.That(demo.Verdict, Is.EqualTo(DemoEvidenceVerdict.ReadyForDemo));
            Assert.That(ui.Verdict, Is.EqualTo(UiScorecardVerdict.Ready));
            Assert.That(qa.IsReady, Is.True);
            Assert.That(server.Verdict, Is.EqualTo(ServerClaimAuditVerdict.SafeForVisualEvidence));
            Assert.That(builder.Verdict, Is.EqualTo(BuilderEvidenceVerdict.ReadyForDemo));
            Assert.That(regression.Verdict, Is.EqualTo(RegressionLockVerdict.ClearWithReserves));
            Assert.That(ledger.Verdict, Is.EqualTo(CrossTeamVisualVerdict.ReadyForGate));
        }

        [Test]
        public void Bee600DecisionBoardKeepsBee601Blocked()
        {
            Bee600VisualMilestoneDecisionBoard board = new Bee600VisualMilestoneDecisionBoard(new[]
            {
                Input("CapturePack"), Input("MobilePortraitProof"), Input("HudRead"), Input("DetailProof"), Input("GestureProof"), Input("AssetManifest"),
                Input("AccessibilityResult"), Input("UiScore"), Input("QaVerdict"), Input("ServerClaimAudit"), Input("BuilderBundle"), Input("RegressionLock"), Input("CrossTeamLedger")
            }, new[]
            {
                Stop("BlankScene"), Stop("UnreadableHive"), Stop("MissingHud"), Stop("PortraitUndocumented"), Stop("DetailUnclear"), Stop("DisclosureAbsent"), Stop("OfficialClaimVisible"), Stop("DemoMissingWithoutArchitectExemption")
            }, new[] { new MilestoneCarryForwardReserve("ArchitectReviewPending", true) }, Bee601BlockerStatus.BlockedUntilBee600Validation);

            Assert.That(board.Decision, Is.EqualTo(VisualMilestoneDecision.PassWithReserves));
            Assert.That(board.Bee601Status, Is.EqualTo(Bee601BlockerStatus.BlockedUntilBee600Validation));
        }

        private static VisualEvidenceTarget Target(string id) => new VisualEvidenceTarget(id, "BEE", true);
        private static AssetReplacementRow Replacement(string category) => new AssetReplacementRow(category, "source", "target", "mobile");
        private static AssetFallbackRule Fallback(string category) => new AssetFallbackRule(category, true);
        private static PortraitReadabilityCheck Portrait(string id) => new PortraitReadabilityCheck(id, true);
        private static VisualShotSpec Shot(string id) => new VisualShotSpec(id, "content", true);
        private static VisualShotFailureRule Failure(string id) => new VisualShotFailureRule(id, "refuse");
        private static ResourceChipReadabilityRule Hud(string id) => new ResourceChipReadabilityRule(id, true);
        private static GesturePath Gesture(string id) => new GesturePath(id, "feedback", true);
        private static ZoneIconRecognitionRule Zone(string id) => new ZoneIconRecognitionRule(id, true);
        private static DeviceAccessibilityCheck Device(string id) => new DeviceAccessibilityCheck(id, true);
        private static VisualPathStep Step(string id, int order) => new VisualPathStep(id, order);
        private static DisclosureRow Disclosure(string id) => new DisclosureRow(id, "surface", true);
        private static CapturePipelineStep Pipe(string id, int order) => new CapturePipelineStep(id, order, true);
        private static UiScoreAxis Axis(string id) => new UiScoreAxis(id, 4, true);
        private static QaPlayerPrompt Prompt(string id) => new QaPlayerPrompt(id, true);
        private static QaExpectedAnswer Answer(string id) => new QaExpectedAnswer(id, "ok");
        private static AuthorityClaimCheck Claim(string id) => new AuthorityClaimCheck(id, true);
        private static BuilderEvidenceArtifact Artifact(string id) => new BuilderEvidenceArtifact(id, true);
        private static VisualRegressionBlocker Blocker(string id) => new VisualRegressionBlocker(id, false);
        private static CrossTeamReviewRow Row(string id) => new CrossTeamReviewRow(id, true, "owner");
        private static MilestoneDecisionInput Input(string id) => new MilestoneDecisionInput(id, true);
        private static MilestoneStopCondition Stop(string id) => new MilestoneStopCondition(id, false);

        private static ZoneIconVariant[] ZoneVariants()
        {
            string[] zones = { "Nursery", "Storage", "Defense", "Research", "Future" };
            string[] variants = { "Normal", "Selected", "Locked", "Inactive", "Compact" };
            ZoneIconVariant[] rows = new ZoneIconVariant[zones.Length * variants.Length];
            int index = 0;
            for (int i = 0; i < zones.Length; i++)
            {
                for (int j = 0; j < variants.Length; j++)
                {
                    rows[index++] = new ZoneIconVariant(zones[i], variants[j], true);
                }
            }

            return rows;
        }
    }
}
