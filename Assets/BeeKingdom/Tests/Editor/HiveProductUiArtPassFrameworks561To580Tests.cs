using BeeKingdom.Colony;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class HiveProductUiArtPassFrameworks561To580Tests
    {
        [Test]
        public void IntakeHudMaterialsTouchRailDetailAndMicrocopyAreReady()
        {
            HiveProductUiArtPassIntake intake = new HiveProductUiArtPassIntake(new[]
            {
                new ArtPassReserveRow("Bee581", "Architecte", false)
            }, new[]
            {
                Stream("ResourceHud"), Stream("TouchNavigation"), Stream("DetailPanel"), Stream("VisualStates"), Stream("ZoneIcons"),
                Stream("AssetSet"), Stream("HiveAmbience"), Stream("MobilePortrait"), Stream("NonAuthoritativeDisclosure")
            });

            ResourceHudIconRefinement hud = new ResourceHudIconRefinement(new[]
            {
                Icon("HoneyDrop"), Icon("WaxBlock"), Icon("PollenGrain"), Icon("BeePopulation"), Icon("StorageCapacity")
            }, ResourceHudDensityMode.Comfortable, "Preview locale non officielle.");

            MaterialResourceChipHierarchy materials = new MaterialResourceChipHierarchy(
                new MaterialChipSpec("Honey", "Survie", 1, "amber", true),
                new MaterialChipSpec("Wax", "Construction preview", 2, "gold", true),
                new MaterialChipSpec("Pollen", "Terrain", 3, "olive", true),
                new[] { new MaterialChipRule("Passive", "Lecture passive.") });

            TouchNavigationTargetMap touch = new TouchNavigationTargetMap(new[]
            {
                Target("BackHome"), Target("ResourceHudInfo"), Target("ZoneFilter"), Target("CellSelect"), Target("DetailClose"), Target("DetailPrimaryPreview")
            }, new SafeAreaPolicy(true, true, true));

            BottomThumbRail rail = new BottomThumbRail(
                Rail("HomeAnchor"), Rail("ZoneLens"), Rail("ResourceLens"), Rail("DetailToggle"), Rail("PreviewBadge"), new ThumbRailCollapseRule("KeepsHome", true));

            HiveDetailPanelComposition detail = new HiveDetailPanelComposition(
                new DetailPanelHeader("Cellule", "Role"),
                new DetailPanelStateBlock("Selection", "Selected"),
                new DetailPanelPreviewBlock("Preview locale", "Comprendre sans action."),
                new DetailPanelActionRow("Fermer", "Inspecter", false));

            DetailPanelMicrocopyCatalog copy = new DetailPanelMicrocopyCatalog(new[]
            {
                Entry("PreviewRole", "Preview locale."), Entry("LocalEstimate", "Estimation locale."), Entry("ServerRequired", "Serveur requis plus tard."),
                Entry("LockedFuture", "Reserve future."), Entry("NoOfficialAction", "Aucune action officielle.")
            }, new[] { new ForbiddenClaimPattern("collecter", "Interdit"), new ForbiddenClaimPattern("acheter", "Interdit") });

            Assert.That(intake.Verdict, Is.EqualTo(ArtPassReadinessVerdict.ReadyWithReserves));
            Assert.That(hud.IsComplete, Is.True);
            Assert.That(materials.IsReadable, Is.True);
            Assert.That(touch.Verdict, Is.EqualTo(TouchNavigationVerdict.Ready));
            Assert.That(rail.IsReady, Is.True);
            Assert.That(detail.IsPolished, Is.True);
            Assert.That(copy.Verdict, Is.EqualTo(MicrocopyVerdict.Ready));
        }

        [Test]
        public void VisualTokensAmbienceAssetsPortraitTactileFutureAndAccessibilityAreSafe()
        {
            HiveVisualStateTokenSet tokens = new HiveVisualStateTokenSet(
                Token("Selected"), Token("Locked"), Token("ServerRequired"), Token("Preview"),
                new[] { new StateTokenAccessibilityRule("NoColorOnly", true) });

            HiveZoneIconSet zones = new HiveZoneIconSet(Zone("Nursery"), Zone("Storage"), Zone("Defense"), Zone("Research"), new[] { new ZoneIconUsageRule("Label", "Libelle requis.") });
            HiveWaxAmbienceLayer ambience = new HiveWaxAmbienceLayer(new AmbiencePalette("wax", "honey", "neutral", "olive"), new TextureTreatment("wax", true), new DepthTreatment("depth", true), new ReadabilityGuard(true, true, true));
            HexCellDepthTreatment depth = new HexCellDepthTreatment(new BorderTreatment("normal", true), new BorderTreatment("selected", true), new ShadowTreatment("shadow", true), new HighlightTreatment("highlight", true));
            HiveArtPassAssetManifest assets = new HiveArtPassAssetManifest(new[]
            {
                Asset("ResourceIcons"), Asset("ZoneIcons"), Asset("StateBadges"), Asset("WaxTextures"), Asset("RailIcons"), Asset("PanelChrome")
            }, new AssetNamingPolicy("bk_hive_", true), new AssetPreviewStatus("Preview", false));
            MobilePortraitHiveLayoutPolicy portrait = new MobilePortraitHiveLayoutPolicy(new SafeAreaInsets(0, 0, 0, 0), new PortraitStackOrder("HUD", "Hive", "Detail", "Rail"), new[] { new PortraitOverlapRule("NoOverlap", true) });
            TactileFeedbackLanguage tactile = new TactileFeedbackLanguage(Press("Pressed"), Press("Released"), Press("RefusedPreview"), Press("FocusShift"));
            FutureRoomArtTreatment future = new FutureRoomArtTreatment(Cell("Empty"), Cell("Locked"), Cell("FutureRoom"), new[] { new FutureRoomClaimRule("NoCostTimerReward", true) });
            HiveArtPassAccessibilityRules accessibility = new HiveArtPassAccessibilityRules(new[] { new ContrastRule("Hud", 4.5f, true) }, new[] { new SymbolPairingRule("Icons", true, true) }, new TextFitPolicy(true, true));

            Assert.That(tokens.IsAccessible, Is.True);
            Assert.That(zones.IsComplete, Is.True);
            Assert.That(ambience.IsReady, Is.True);
            Assert.That(depth.IsReady, Is.True);
            Assert.That(assets.IsValid, Is.True);
            Assert.That(portrait.Verdict, Is.EqualTo(PortraitReadabilityVerdict.Ready));
            Assert.That(tactile.IsSafe, Is.True);
            Assert.That(future.IsSafe, Is.True);
            Assert.That(accessibility.Verdict, Is.EqualTo(ArtPassAccessibilityVerdict.Ready));
        }

        [Test]
        public void DemoQaServerGuardAndGateKeepBee581Blocked()
        {
            ArtPassDemoCaptureRequirements demo = new ArtPassDemoCaptureRequirements(new[]
            {
                Shot("DesktopOverview"), Shot("MobilePortrait"), Shot("DetailPanelOpen"), Shot("ResourceHudCloseRead"), Shot("StateTokenSample")
            }, new[] { new CaptureAnnotationRule("Preview", true) });

            ArtPassQaReadabilityProtocol qa = new ArtPassQaReadabilityProtocol(new[]
            {
                Prompt("FindHud"), Prompt("IdentifyTwoZoneIcons"), Prompt("ReadSelectionState"), Prompt("ClosePanel"), Prompt("SayWhatIsPreview"), Prompt("RecognizeHivePortrait")
            }, new[]
            {
                Answer("FindHud"), Answer("IdentifyTwoZoneIcons"), Answer("ReadSelectionState"), Answer("ClosePanel"), Answer("SayWhatIsPreview"), Answer("RecognizeHivePortrait")
            }, new[] { new ReadabilityBlocker("ServerClaim", false) });

            ArtPassNonAuthoritativeGuard guard = new ArtPassNonAuthoritativeGuard(new[]
            {
                Claim("Resources"), Claim("Capacity"), Claim("FutureRoom"), Claim("ServerRequired"), Claim("LockedState"), Claim("PreviewAction"), Claim("TactileFeedback")
            }, new[] { new ForbiddenServerImplication("OfficialAction", false) });

            HiveProductUiArtPassGate gate = new HiveProductUiArtPassGate(new[]
            {
                Gate("Intake"), Gate("ResourceHud"), Gate("MaterialHierarchy"), Gate("TouchTargets"), Gate("ThumbRail"), Gate("DetailPanel"), Gate("Microcopy"),
                Gate("StateTokens"), Gate("ZoneIcons"), Gate("Ambience"), Gate("CellDepth"), Gate("AssetManifest"), Gate("MobilePortrait"), Gate("TactileFeedback"),
                Gate("FutureRooms"), Gate("Accessibility"), Gate("DemoCapture"), Gate("QaReadability"), Gate("ServerGuard")
            }, new[] { new ArtPassRemainingReserve("Bee581", "Validation architecte requise.") }, Bee581BlockerStatus.BlockedUntilArtPassValidation);

            Assert.That(demo.Verdict, Is.EqualTo(DemoCaptureVerdict.ReadyForFutureDemo));
            Assert.That(qa.IsReady, Is.True);
            Assert.That(guard.Verdict, Is.EqualTo(NonAuthoritativeVerdict.SafeForPreview));
            Assert.That(gate.Verdict, Is.EqualTo(ArtPassGateVerdict.ReadyWithReserves));
            Assert.That(gate.Bee581Status, Is.EqualTo(Bee581BlockerStatus.BlockedUntilArtPassValidation));
        }

        private static ArtPassWorkstream Stream(string id) => new ArtPassWorkstream(id, id);
        private static ResourceIconSpec Icon(string id) => new ResourceIconSpec(id, id, 32, id, true);
        private static TouchTargetSpec Target(string id) => new TouchTargetSpec(id, 48, 8);
        private static ThumbRailItem Rail(string id) => new ThumbRailItem(id, id, id, true);
        private static DetailCopyEntry Entry(string id, string text) => new DetailCopyEntry(id, text);
        private static VisualStateToken Token(string id) => new VisualStateToken(id, "shape", id, "border");
        private static ZoneIconSpec Zone(string id) => new ZoneIconSpec(id, id, id, true);
        private static AssetManifestEntry Asset(string category) => new AssetManifestEntry("bk_hive_" + category, category, "Preview", true);
        private static PressStateToken Press(string id) => new PressStateToken(id, "feedback", false);
        private static CellArtTreatment Cell(string id) => new CellArtTreatment(id, "treatment", false);
        private static CaptureShotRequirement Shot(string id) => new CaptureShotRequirement(id, true);
        private static PlayerPrompt Prompt(string id) => new PlayerPrompt(id, true);
        private static ExpectedAnswer Answer(string id) => new ExpectedAnswer(id, "ok");
        private static DisplayedDataClaim Claim(string id) => new DisplayedDataClaim(id, "surface", true);
        private static ArtPassGateRow Gate(string id) => new ArtPassGateRow(id, true, "Unity preview evidence.");
    }
}
