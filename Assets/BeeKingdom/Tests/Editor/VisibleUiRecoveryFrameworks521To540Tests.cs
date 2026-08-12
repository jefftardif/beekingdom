using System;
using BeeKingdom.Colony;
using NUnit.Framework;

namespace BeeKingdom.Tests
{
    public sealed class VisibleUiRecoveryFrameworks521To540Tests
    {
        [Test]
        public void VisibleHomeHudNavigationActionAndReadout_AreReadyForPlayerLaunch()
        {
            Assert.That(DefaultBootstrap().VisibleState, Is.EqualTo(VisibleHomeUiState.VisibleOnLaunch));
            Assert.That(DefaultHud().IsMobileHomeReady, Is.True);
            Assert.That(DefaultAction().IsReadablePreview, Is.True);
            Assert.That(DefaultReadout().IsComplete, Is.True);

            var nav = new PrimaryNavigationEntryPoints(new[]
            {
                new PlayerNavEntryPoint("Hive", "Ruche", "hive", string.Empty),
                new PlayerNavEntryPoint("World", "Monde", "world", "Carte live non connectee"),
                new PlayerNavEntryPoint("Alliance", "Alliance", "alliance", "Social live non connecte"),
                new PlayerNavEntryPoint("Messages", "Messages", "messages", "Messagerie preview"),
                new PlayerNavEntryPoint("Army", "Armee", "army", "Combat verrouille"),
                new PlayerNavEntryPoint("Research", "Recherche", "research", "Bonus non actifs")
            });
            Assert.That(nav.Verdict, Is.EqualTo(PlayerNavigationVisibilityVerdict.Visible));
        }

        [Test]
        public void MobileAssetsSceneDemoQaAndServerGuards_BlockWrongClaims()
        {
            var framing = new HiveViewBackgroundSafeFraming("HiveBackground", new HiveSafeFramePortrait(0.05f, 0.05f, 0.95f, 0.95f), new[]
            {
                new HiveVisualAnchor("HiveCore", "coeur ruche", true),
                new HiveVisualAnchor("ResourceZone", "ressources", false),
                new HiveVisualAnchor("ActionZone", "actions", false),
                new HiveVisualAnchor("LowerNavLimit", "navigation", true),
                new HiveVisualAnchor("NoTextZone", "zone protegee", true)
            });
            Assert.That(framing.IsSafeForPortrait, Is.True);

            var portrait = new MobilePortraitInteractionConstraints(new[]
            {
                new MobilePortraitRule("TapTarget", "targets lisibles", true),
                new MobilePortraitRule("NoTextOverflow", "texte sans depassement", true),
                new MobilePortraitRule("BottomNav", "navigation basse", true),
                new MobilePortraitRule("SafeArea", "safe area", true),
                new MobilePortraitRule("ScrollablePanel", "panneaux scrollables", true),
                new MobilePortraitRule("Contrast", "contraste", true),
                new MobilePortraitRule("PortraitOrientation", "portrait", true),
                new MobilePortraitRule("HomePriority", "accueil prioritaire", true)
            }, Array.Empty<MobileLayoutFailure>());
            Assert.That(portrait.Verdict, Is.EqualTo(MobilePortraitVerdict.Ready));

            var assets = new UiAssetIntegrationGuardrail(new[]
            {
                new UiAssetRequirement("HiveBackground", "fond ruche", true),
                new UiAssetRequirement("ResourceIcon", "ressources", true),
                new UiAssetRequirement("NavIcon", "navigation", true),
                new UiAssetRequirement("LockIcon", "verrou", true),
                new UiAssetRequirement("HomePanel", "panneau accueil", true),
                new UiAssetRequirement("BeeKingdomAccent", "accent marque", true)
            }, new[]
            {
                new AllowedPlaceholderAsset("HiveBackground", "preview locale"),
                new AllowedPlaceholderAsset("ResourceIcon", "preview locale"),
                new AllowedPlaceholderAsset("NavIcon", "preview locale"),
                new AllowedPlaceholderAsset("LockIcon", "preview locale"),
                new AllowedPlaceholderAsset("HomePanel", "preview locale"),
                new AllowedPlaceholderAsset("BeeKingdomAccent", "preview locale")
            });
            Assert.That(assets.Verdict, Is.EqualTo(UiAssetGuardrailVerdict.ReadyForPreview));

            Assert.That(new SceneBindingBootstrapGuard("SandboxPlayground", "SandboxPlaygroundBootstrap", "VisiblePlayerHomeUiPresenter").Verdict, Is.EqualTo(SceneBindingVerdict.Bound));
            Assert.That(new DemoImmediateVisualException("ARCH-081 critical recovery", "SandboxPlayground", "Play Mode home UI visible").Verdict, Is.EqualTo(DemoImmediateVerdict.ReadyForProofCapture));
            Assert.That(new QaUnityLaunchSmokeTest("SandboxPlayground", new[]
            {
                new QaLaunchCheck("SceneLoaded", "scene loaded", true),
                new QaLaunchCheck("PlayModeEntered", "play mode", true),
                new QaLaunchCheck("UiRootVisible", "home visible", true),
                new QaLaunchCheck("HomeReadable", "home readable", true),
                new QaLaunchCheck("NavVisible", "nav visible", true),
                new QaLaunchCheck("NoBlankBlue", "no blank screen", true),
                new QaLaunchCheck("NoBlockingError", "no blocking error", true)
            }).Verdict, Is.EqualTo(QaLaunchSmokeVerdict.Passed));

            var server = new ServerNonClaimGuard(new[]
            {
                new DisplayedDataAuthorityRow("Resources", "local preview", false),
                new DisplayedDataAuthorityRow("Progression", "local preview", false),
                new DisplayedDataAuthorityRow("HiveName", "local preview", false),
                new DisplayedDataAuthorityRow("Alliance", "local preview", false),
                new DisplayedDataAuthorityRow("Messages", "local preview", false),
                new DisplayedDataAuthorityRow("Army", "local preview", false),
                new DisplayedDataAuthorityRow("Research", "local preview", false),
                new DisplayedDataAuthorityRow("ActionPreview", "local preview", false)
            }, Array.Empty<ServerClaimViolation>());
            Assert.That(server.Verdict, Is.EqualTo(ServerNonClaimVerdict.SafeForPreview));
        }

        [Test]
        public void FeedbackArmySocialSurfaceRegressionAndGate_CloseVisibleRecoveryWithReserves()
        {
            Assert.That(DefaultArmy().IsPreviewOnly, Is.True);
            Assert.That(DefaultSocial().IsPreviewOnly, Is.True);

            var language = new PlayerFeedbackLockedStateLanguage(new[]
            {
                new LockedStateMessage("ActionPreview", "Action en preview locale.", "no execution"),
                new LockedStateMessage("ServerNotConnected", "Serveur non connecte.", "server boundary"),
                new LockedStateMessage("SocialNotLive", "Social live non connecte.", "social boundary"),
                new LockedStateMessage("CombatLocked", "Combat verrouille.", "combat boundary"),
                new LockedStateMessage("LocalResources", "Ressources locales preview.", "economy boundary"),
                new LockedStateMessage("DemoRecovery", "Accueil joueur visible.", "demo recovery")
            }, new[] { new ForbiddenFeedbackPhrase("envoye au serveur", "server claim") });
            Assert.That(language.Verdict, Is.EqualTo(LockedStateLanguageVerdict.Ready));

            var acceptance = new UiTeamAcceptanceChecklist(new[]
            {
                new UiAcceptanceCheck("BeeKingdomIdentity", true),
                new UiAcceptanceCheck("HiveVisible", true),
                new UiAcceptanceCheck("HudReadable", true),
                new UiAcceptanceCheck("PrimaryNav", true),
                new UiAcceptanceCheck("ActionPreview", true),
                new UiAcceptanceCheck("LocksComprehensible", true),
                new UiAcceptanceCheck("MobilePortrait", true),
                new UiAcceptanceCheck("CleanAssets", true)
            }, new[] { new UiAcceptanceReserve("FinalArt", "assets finalises plus tard") });
            Assert.That(acceptance.Verdict, Is.EqualTo(UiAcceptanceVerdict.AcceptedWithReserves));

            var proof = new BuilderRuntimeProofPackage("SandboxPlayground", "SandboxPlaygroundBootstrap", new[] { "VisiblePlayerHomeUiPresenter", "Main Camera", "Sandbox Playground" }, new[] { "SandboxPlaygroundBootstrap.cs", "VisiblePlayerHomeUiPresenter.cs" }, "Logs/bee-521-540-compile.log");
            Assert.That(proof.IsComplete, Is.True);

            var separation = new ProductionDemoSurfaceSeparation(new[]
            {
                Surface("HomeLaunch", SurfaceClass.Demo, SurfaceAudience.Player),
                Surface("SandboxEvidence", SurfaceClass.Demo, SurfaceAudience.Reviewer),
                Surface("DiagnosticsPanel", SurfaceClass.DebugQa, SurfaceAudience.Qa),
                Surface("DebugHotkeys", SurfaceClass.DebugQa, SurfaceAudience.Reviewer)
            }, Array.Empty<SurfaceMisclassificationRisk>());
            Assert.That(separation.Verdict, Is.EqualTo(SurfaceSeparationVerdict.Separated));

            var regression = new VisibleProductRegressionRule(Array.Empty<VisibleRegressionCondition>(), new[]
            {
                new VisibleRegressionEvidence("NoBlankScreen", true),
                new VisibleRegressionEvidence("NoBlueScreen", true),
                new VisibleRegressionEvidence("NotDiagnosticOnly", true),
                new VisibleRegressionEvidence("HudVisible", true),
                new VisibleRegressionEvidence("NavVisible", true),
                new VisibleRegressionEvidence("MobileReadable", true),
                new VisibleRegressionEvidence("SceneBinding", true)
            });
            Assert.That(regression.Verdict, Is.EqualTo(VisibleRegressionVerdict.Protected));

            var gate = new VisibleUiRecoveryGate(ValidRows(), new[] { new VisibleUiRecoveryReserve("DemoQaProof", "Demo and QA must still attach visual proof before BEE-541.") }, Bee541BlockerStatus.ReadyForArchitectReview);
            Assert.That(gate.Verdict, Is.EqualTo(VisibleUiRecoveryVerdict.ReadyWithReserves));
        }

        private static VisibleUiGateRow[] ValidRows()
        {
            return new[]
            {
                Row("BootstrapVisible"), Row("HUD"), Row("HiveBackground"), Row("Nav"), Row("ActionPreview"),
                Row("Resources"), Row("Army"), Row("Alliance"), Row("FeedbackLocks"), Row("MobilePortrait"),
                Row("Assets"), Row("SceneBinding"), Row("DemoProof", "Play Mode home visible"), Row("QaSmoke", "QA smoke contract passed"),
                Row("ServerNonClaim"), Row("UiAcceptance"), Row("BuilderProof"), Row("SurfaceSeparation"), Row("RegressionRule"), Row("Bee541Gate")
            };
        }

        private static VisibleUiGateRow Row(string id, string evidence = "local preview evidence")
        {
            return new VisibleUiGateRow(id, true, evidence);
        }

        private static SurfaceClassification Surface(string id, SurfaceClass surfaceClass, SurfaceAudience audience)
        {
            return new SurfaceClassification(id, surfaceClass, audience, new SurfaceVisualMarker(id + "Marker", true), new[] { "SandboxPlayground" }, Array.Empty<SurfaceForbiddenRoute>(), new SurfacePromiseGuard());
        }

        private static VisiblePlayerHomeUiBootstrap DefaultBootstrap()
        {
            return new VisiblePlayerHomeUiBootstrap("SandboxPlayground", "Sandbox Playground", new[]
            {
                new VisibleHomeUiElement("BeeKingdomTitle", "identity", "Bee Kingdom", true),
                new VisibleHomeUiElement("HiveNamePreview", "hive", "Ruche Prime", true),
                new VisibleHomeUiElement("MainResource", "resource", "Miel", true),
                new VisibleHomeUiElement("HiveEntry", "hive nav", "Ruche", true),
                new VisibleHomeUiElement("WorldEntry", "world nav", "Monde", true),
                new VisibleHomeUiElement("PreviewStatusMessage", "status", "Preview locale", true),
                new VisibleHomeUiElement("BackCloseAffordance", "back", "Retour accueil", true)
            });
        }

        private static MobileHomeHudShell DefaultHud()
        {
            return new MobileHomeHudShell(new HomeHudHeader("Bee Kingdom", "Ruche Prime", "Preview locale"), new[]
            {
                new HomeHudResourceChip("Honey", "Miel", "1 240", "honey"),
                new HomeHudResourceChip("Wax", "Cire", "420", "wax"),
                new HomeHudResourceChip("Pollen", "Pollen", "315", "pollen")
            }, new[]
            {
                new HomeHudNavEntry("Hive", "Ruche", "hive", string.Empty),
                new HomeHudNavEntry("World", "Monde", "world", "preview"),
                new HomeHudNavEntry("Alliance", "Alliance", "alliance", "preview"),
                new HomeHudNavEntry("Messages", "Messages", "messages", "preview")
            }, new HomeHudPreviewStatus("visible", true));
        }

        private static HiveActionPreviewPanel DefaultAction()
        {
            return new HiveActionPreviewPanel("Ameliorer la salle de stockage", "Plus de capacite.", new[] { new ActionPreviewRequirement("Wax", "Cire requise", false) }, "Preview seulement.");
        }

        private static ResourceAndProgressionReadout DefaultReadout()
        {
            return new ResourceAndProgressionReadout(new[]
            {
                new VisibleResourceValue("Honey", "1 240", "preview"),
                new VisibleResourceValue("Wax", "420", "preview"),
                new VisibleResourceValue("Pollen", "315", "preview"),
                new VisibleResourceValue("Population", "86", "preview")
            }, new VisibleProgressionValue("Progression", "Niveau 3", "preview"), "Valeurs de preview locale.");
        }

        private static ArmyDefenseAccessPreview DefaultArmy()
        {
            return new ArmyDefenseAccessPreview("Armee", "Calme", "Combat verrouille.", new[]
            {
                new ArmyPreviewSignal("Guards", "Gardes", false),
                new ArmyPreviewSignal("HiveDefense", "Defense", false),
                new ArmyPreviewSignal("FutureTraining", "Entrainement futur", false),
                new ArmyPreviewSignal("CombatLocked", "Combat verrouille", true)
            });
        }

        private static AllianceSocialPreviewNotebook DefaultSocial()
        {
            return new AllianceSocialPreviewNotebook("Carnet social", "Non connecte", new[]
            {
                new SocialNotebookRow("Invite", "Invitation future", "preview"),
                new SocialNotebookRow("Help", "Aide locale", "preview"),
                new SocialNotebookRow("SystemMessage", "Message systeme", "preview"),
                new SocialNotebookRow("Etiquette", "Moderation", "preview"),
                new SocialNotebookRow("Trust", "Confiance", "preview")
            }, "Respect et clarte.");
        }
    }
}
