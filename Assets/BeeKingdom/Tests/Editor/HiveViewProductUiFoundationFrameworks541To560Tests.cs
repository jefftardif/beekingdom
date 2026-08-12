using System;
using System.Collections.Generic;
using BeeKingdom.Colony;
using NUnit.Framework;

namespace BeeKingdom.Tests
{
    public sealed class HiveViewProductUiFoundationFrameworks541To560Tests
    {
        [Test]
        public void FoundationGridCellsZonesHudNavigationAndRooms_AreReadyForPreview()
        {
            var intake = new HiveViewProductUiFoundationIntake(Requirements(), new[] { new HiveProductReserve("art", "final art later") });
            Assert.That(intake.Verdict, Is.EqualTo(HiveProductFoundationVerdict.ReadyWithProductReserves));

            var grid = new HexagonalHiveSpatialGrid(2, Cells(), new HiveGridFraming(0.16f, 0.18f, true));
            Assert.That(grid.IsRenderable, Is.True);

            var language = new HiveCellVisualLanguage(new[]
            {
                State(HiveCellStateKind.EmptyPreview), State(HiveCellStateKind.OccupiedPreview), State(HiveCellStateKind.Locked),
                State(HiveCellStateKind.UpgradeCandidate), State(HiveCellStateKind.ServerRequired)
            }, new[]
            {
                Legend(HiveCellStateKind.EmptyPreview), Legend(HiveCellStateKind.OccupiedPreview), Legend(HiveCellStateKind.Locked),
                Legend(HiveCellStateKind.UpgradeCandidate), Legend(HiveCellStateKind.ServerRequired)
            });
            Assert.That(language.Verdict, Is.EqualTo(HiveCellLanguageVerdict.Ready));

            Assert.That(new HiveFunctionalZoneLayout(new[]
            {
                Zone("Storage"), Zone("Nursery"), Zone("Production"), Zone("Defense"), Zone("Research"), Zone("SocialGate")
            }).Verdict, Is.EqualTo(HiveZoneLayoutVerdict.Ready));

            Assert.That(new IconizedResourceHudForHiveView(new[]
            {
                Chip("Honey"), Chip("Wax"), Chip("Pollen"), Chip("Bees"), Chip("Capacity")
            }, "LOCAL PREVIEW").Verdict, Is.EqualTo(ResourceHudVerdict.Ready));

            Assert.That(new HiveViewProductNavigationRails(new[]
            {
                Nav("Hive"), Nav("Actions"), Nav("World"), Nav("Alliance"), Nav("Army"), Nav("Research")
            }, Array.Empty<HiveNavRailEntry>()).Verdict, Is.EqualTo(HiveNavigationRailVerdict.Ready));

            Assert.That(new HiveBuildingSlotRoomPreview(new[]
            {
                Room("StorageRoom"), Room("NurseryRoom"), Room("WorkshopRoom"), Room("DefensePost"), Room("ResearchCell")
            }, new[] { new HiveRoomPlacementRule("local", "preview only") }).Verdict, Is.EqualTo(HiveRoomPreviewVerdict.Ready));
        }

        [Test]
        public void SelectionDetailStateMobileAssetsAndProgression_AreReadableWithoutOfficialClaims()
        {
            var focus = new HiveCellSelectionFocus("cell-0-0", "tap", new[]
            {
                new HiveFocusFeedback("outline", "outline", true), new HiveFocusFeedback("glow", "glow", true),
                new HiveFocusFeedback("label", "label", true), new HiveFocusFeedback("drawer", "drawer", true)
            });
            Assert.That(focus.IsVisible, Is.True);

            var detail = new HiveCellDetailPlayerPanel(
                new HiveCellPanelHeader("Reserve miel", "cell-0-0", "jar"),
                new[] { new HiveCellPanelFact("HeaderRoom", "room"), new HiveCellPanelFact("WhyItMatters", "value"), new HiveCellPanelFact("CurrentPreviewState", "preview") },
                new[] { new HiveCellPanelConstraint("NeededResources", "local"), new HiveCellPanelConstraint("LockedReason", "server future") },
                new HiveCellPanelAuthorityNote("preview only", true));
            Assert.That(detail.IsReadable, Is.True);

            Assert.That(new HiveViewVisualStateLanguage(new[]
            {
                Rule("Normal"), Rule("Locked"), Rule("Preview"), Rule("ServerRequired"), Rule("Selected"), Rule("SoftAlert")
            }, Array.Empty<HiveVisualStateMisuse>()).Verdict, Is.EqualTo(HiveVisualStateVerdict.Ready));

            Assert.That(new MobilePortraitHiveViewFraming(new HiveMobileSafeArea(12, 12, 8, 8), new[]
            {
                Layout("CentralGrid"), Layout("TopHud"), Layout("BottomRail"), Layout("RetractableDetail"),
                Layout("CompactLabels"), Layout("TouchSelection"), Layout("NoCutText")
            }).Verdict, Is.EqualTo(HiveMobileFramingVerdict.Ready));

            Assert.That(new ProfessionalPreviewAssetSetForHiveView(new[]
            {
                Family("CellShapes"), Family("RoomPictograms"), Family("ResourceGlyphs"), Family("StatusBadges"),
                Family("PanelFrames"), Family("NavigationMarks"), Family("SelectionEffects"), Family("BackgroundTextures")
            }, Array.Empty<HiveAssetReplacementRule>()).Verdict, Is.EqualTo(HivePreviewAssetVerdict.Ready));

            Assert.That(new HiveProgressionVisualLayers(new[]
            {
                new HiveProgressionLayer("EarlyHive", "small"), new HiveProgressionLayer("GrowingHive", "more rooms"), new HiveProgressionLayer("FortifiedHive", "defense")
            }, "no official progression").Verdict, Is.EqualTo(HiveProgressionLayerVerdict.Ready));
        }

        [Test]
        public void DemoUiQaServerBuilderRegressionAndGate_CloseLotWithProductReserves()
        {
            Assert.That(new DemoReferenceComparisonContract("ARCH-085 Hive Reference", new[]
            {
                Compare("CentralHive"), Compare("Hexagons"), Compare("Cells"), Compare("Zones"), Compare("HudIcons"),
                Compare("Navigation"), Compare("Selection"), Compare("DetailPanel"), Compare("Mobile"), Compare("Assets")
            }).Verdict, Is.EqualTo(DemoReferenceComparisonVerdict.ReadyForDemoReview));

            Assert.That(new UiVisualGapReviewChecklist(new[]
            {
                Score("SilhouetteRuche", 4), Score("HexRhythm", 4), Score("CellReadability", 4), Score("ZoneHierarchy", 4),
                Score("IconCraft", 3), Score("PanelComposition", 4), Score("MobileDensity", 4), Score("BeeKingdomMood", 4),
                Score("ReferenceSimilarity", 3), Score("NextArtPassPriority", 4)
            }, new[]
            {
                new UiVisualPriorityFix("icons", "sharpen icon craft"), new UiVisualPriorityFix("depth", "increase depth"), new UiVisualPriorityFix("reference", "closer reference")
            }).Verdict, Is.EqualTo(UiVisualGapVerdict.AcceptableWithMajorReserves));

            Assert.That(new QaPlayerReadabilityValidation(new[]
            {
                Prompt("IdentifyHive"), Prompt("NameTwoZones"), Prompt("FindResource"), Prompt("FindSelectedCell"),
                Prompt("ExplainLock"), Prompt("CloseDrawer"), Prompt("SayPreview")
            }, Array.Empty<QaMisreadPattern>()).Verdict, Is.EqualTo(QaPlayerReadabilityVerdict.ReadyForObservation));

            Assert.That(new ServerNonAuthoritativeHiveViewDataGuard(new[]
            {
                Data("CellId"), Data("RoomType"), Data("ZoneLabel"), Data("ResourceValue"),
                Data("ProgressionLayer"), Data("SelectedCell"), Data("LockedState"), Data("ServerRequiredState")
            }, Array.Empty<HiveServerClaimViolation>()).Verdict, Is.EqualTo(HiveDataAuthorityVerdict.SafeForPreview));

            var proof = new BuilderHiveViewRuntimeProofPackage("SandboxPlayground", "Hive View Product Root", new[]
            {
                "CellShapes", "RoomPictograms", "ResourceGlyphs", "StatusBadges", "PanelFrames", "NavigationMarks", "SelectionEffects", "BackgroundTextures"
            }, new[] { "compile.log", "tests.xml", "scene-validation.log" });
            Assert.That(proof.IsComplete, Is.True);

            Assert.That(new HiveViewProductRegressionRule(Array.Empty<HiveProductRegressionCondition>(), new[]
            {
                Evidence("HexGrid"), Evidence("Cells"), Evidence("Zones"), Evidence("HudIcons"), Evidence("Selection"), Evidence("DetailPanel"), Evidence("Assets"), Evidence("DemoCompared")
            }).Verdict, Is.EqualTo(HiveProductRegressionVerdict.Protected));

            Assert.That(new HiveViewProductUiFoundationGate(Ledger(), new[] { new HiveViewProductReserve("FinalArt", "final art pass remains") }, Bee561BlockerStatus.ReadyForArchitectReview).Verdict, Is.EqualTo(HiveViewProductGateVerdict.ReadyWithProductReserves));
        }

        private static IReadOnlyList<HiveProductVisualRequirement> Requirements()
        {
            string[] ids = { "MainHive", "HexStructure", "Cells", "FunctionalZones", "IconHud", "Navigation", "Selection", "DetailPanel", "VisualStates", "DemoComparison" };
            var list = new List<HiveProductVisualRequirement>();
            foreach (string id in ids) list.Add(new HiveProductVisualRequirement(id, id, true));
            return list;
        }

        private static IReadOnlyList<HiveHexCell> Cells()
        {
            var cells = new List<HiveHexCell>();
            for (int q = -2; q <= 2; q++)
            {
                int r1 = Math.Max(-2, -q - 2);
                int r2 = Math.Min(2, -q + 2);
                for (int r = r1; r <= r2; r++) cells.Add(new HiveHexCell("cell-" + q + "-" + r, q, r, "Storage", HiveCellStateKind.EmptyPreview));
            }
            return cells;
        }

        private static IReadOnlyList<HiveViewDecisionLedgerRow> Ledger()
        {
            string[] ids = { "VisualFoundation", "HexGrid", "CellLanguage", "FunctionalZones", "IconHud", "Navigation", "RoomSlots", "Selection", "DetailDrawer", "StateLanguage", "MobileFrame", "AssetLibrary", "ProgressionLayers", "DemoComparison", "UiScorecard", "QaObservation", "ServerGuard", "BuilderProof", "RegressionRule" };
            var rows = new List<HiveViewDecisionLedgerRow>();
            foreach (string id in ids) rows.Add(new HiveViewDecisionLedgerRow(id, true, "preview evidence"));
            return rows;
        }

        private static HiveCellVisualState State(HiveCellStateKind state) => new HiveCellVisualState(state, state.ToString(), "icon-" + state, true);
        private static HiveCellLegendItem Legend(HiveCellStateKind state) => new HiveCellLegendItem(state, state.ToString());
        private static HiveFunctionalZone Zone(string id) => new HiveFunctionalZone(id, id, "icon-" + id, "preview");
        private static HiveResourceHudChip Chip(string id) => new HiveResourceHudChip(id, id, "icon-" + id, "10", "preview");
        private static HiveNavRailEntry Nav(string id) => new HiveNavRailEntry(id, id, "icon-" + id, id == "Hive" ? string.Empty : "preview");
        private static HiveRoomPreviewSlot Room(string id) => new HiveRoomPreviewSlot(id, "cell", id, "icon-" + id, "preview");
        private static HiveVisualStateRule Rule(string id) => new HiveVisualStateRule(id, "all", "icon-" + id, true);
        private static HiveMobileLayoutRule Layout(string id) => new HiveMobileLayoutRule(id, true);
        private static HiveAssetFamily Family(string id) => new HiveAssetFamily(id, id, true);
        private static DemoVisualComparisonRow Compare(string id) => new DemoVisualComparisonRow(id, true, string.Empty);
        private static UiVisualScoreRow Score(string id, int score) => new UiVisualScoreRow(id, score, "ok");
        private static QaObservationPrompt Prompt(string id) => new QaObservationPrompt(id, id, true);
        private static HiveDisplayedDataRow Data(string id) => new HiveDisplayedDataRow(id, "local", "preview", false);
        private static HiveProductRegressionEvidence Evidence(string id) => new HiveProductRegressionEvidence(id, true);
    }
}
