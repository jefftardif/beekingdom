using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Playground
{
    public enum BeePresenceMotionKind { Idle, ShortFlight, WalkCrawl, LocalCollection, ZoneEntryExit }
    public enum BeePresenceFamily { Worker, Guard, Drone, Queen, Scout, Nurse }
    public enum BeePresenceGateVerdict { Pass, PassWithReserves, ReworkRequired }

    public interface IBeePresenceContract
    {
        string CharacterOrAssetScope { get; }
        string AnimationRule { get; }
        string ZoneReadabilityRule { get; }
        string MobilePerformanceRule { get; }
        string EvidenceRequirement { get; }
        string NonLiveBoundary { get; }
    }

    public sealed class LiveHivePresenceIntakeLedger
    {
        public LiveHivePresenceIntakeLedger(IReadOnlyList<string> inheritedReserves, string boundary)
        {
            InheritedReserves = inheritedReserves ?? Array.Empty<string>();
            Boundary = BeePresenceRequirements.Require(boundary, nameof(boundary));
            PreviewOnly = true;
        }

        public IReadOnlyList<string> InheritedReserves { get; }
        public string Boundary { get; }
        public bool PreviewOnly { get; }
    }

    public sealed class VisibleBeeCharacterFamilyCatalog
    {
        public VisibleBeeCharacterFamilyCatalog(IReadOnlyList<BeeCharacterFamilyDefinition> families)
        {
            Families = families ?? Array.Empty<BeeCharacterFamilyDefinition>();
        }

        public IReadOnlyList<BeeCharacterFamilyDefinition> Families { get; }
        public int VisibleFamilyCount => Families.Count(family => family.VisibleInPlayerView);
    }

    public sealed class BeeCharacterFamilyDefinition
    {
        public BeeCharacterFamilyDefinition(BeePresenceFamily family, string iconId, string role, bool visibleInPlayerView)
        {
            Family = family;
            IconId = BeePresenceRequirements.Require(iconId, nameof(iconId));
            Role = BeePresenceRequirements.Require(role, nameof(role));
            VisibleInPlayerView = visibleInPlayerView;
        }

        public BeePresenceFamily Family { get; }
        public string IconId { get; }
        public string Role { get; }
        public bool VisibleInPlayerView { get; }
    }

    public sealed class BeeCharacterVisualStyleAssetBrief : IBeePresenceContract
    {
        public BeeCharacterVisualStyleAssetBrief(string perspective, string palette, string mobileReadability, string forbiddenPlaceholderRule)
        {
            Perspective = BeePresenceRequirements.Require(perspective, nameof(perspective));
            Palette = BeePresenceRequirements.Require(palette, nameof(palette));
            MobileReadability = BeePresenceRequirements.Require(mobileReadability, nameof(mobileReadability));
            ForbiddenPlaceholderRule = BeePresenceRequirements.Require(forbiddenPlaceholderRule, nameof(forbiddenPlaceholderRule));
            CharacterOrAssetScope = "BEE-663 visible bee characters, LOD0/LOD1/fallback static, wings, shadows and premium icon silhouettes";
            AnimationRule = "Style brief must be approved before motion implementation; no primitive, cube, sphere, capsule, dot or debug billboard";
            ZoneReadabilityRule = "Characters must never compete with zone landmarks, hotspot halos, state tokens, HUD, detail panel or non-claim copy";
            MobilePerformanceRule = "Readable silhouettes at portrait density, semi-transparent wings, honey/wax palette and soft shadows";
            EvidenceRequirement = "Player-facing desktop/mobile captures plus asset manifest and family proof";
            NonLiveBoundary = "Visual preview only: no official bee population, economy, progression, command or synchronization";
        }

        public string Perspective { get; }
        public string Palette { get; }
        public string MobileReadability { get; }
        public string ForbiddenPlaceholderRule { get; }
        public string CharacterOrAssetScope { get; }
        public string AnimationRule { get; }
        public string ZoneReadabilityRule { get; }
        public string MobilePerformanceRule { get; }
        public string EvidenceRequirement { get; }
        public string NonLiveBoundary { get; }
    }

    public sealed class BeeIdleAnimationLoopSet : IBeePresenceContract
    {
        public BeeIdleAnimationLoopSet(float minimumSeconds, float maximumSeconds, bool desynchronized)
        {
            MinimumSeconds = Math.Max(0.1f, minimumSeconds);
            MaximumSeconds = Math.Max(MinimumSeconds, maximumSeconds);
            Desynchronized = desynchronized;
            CharacterOrAssetScope = "BEE-664 worker, guard, nurse, scout, drone and queen idle presence";
            AnimationRule = "2.5s to 4.0s desynchronized idle with wing shimmer, breathing offset and no jitter";
            ZoneReadabilityRule = "Idle motion stays behind state tokens and fades near the selected hotspot";
            MobilePerformanceRule = "No unseeded random noise; same deterministic phase offsets in desktop and mobile";
            EvidenceRequirement = "Runtime proof exposes idle motion and family agents";
            NonLiveBoundary = "Idle presence is ambience only and never indicates official task execution";
        }

        public float MinimumSeconds { get; }
        public float MaximumSeconds { get; }
        public bool Desynchronized { get; }
        public string CharacterOrAssetScope { get; }
        public string AnimationRule { get; }
        public string ZoneReadabilityRule { get; }
        public string MobilePerformanceRule { get; }
        public string EvidenceRequirement { get; }
        public string NonLiveBoundary { get; }
    }

    public sealed class BeeShortFlightAnimationPlan : IBeePresenceContract
    {
        public BeeShortFlightAnimationPlan(float minimumSeconds, float maximumSeconds, int maximumConcurrentFlights)
        {
            MinimumSeconds = Math.Max(0.1f, minimumSeconds);
            MaximumSeconds = Math.Max(MinimumSeconds, maximumSeconds);
            MaximumConcurrentFlights = Math.Max(0, maximumConcurrentFlights);
            CharacterOrAssetScope = "BEE-665 short local flights for reserve, transformation, nursery, alliance and defense ambience";
            AnimationRule = "Arc flight with capped speed, soft acceleration, visible wings and no final pathfinding claim";
            ZoneReadabilityRule = "Flight paths avoid HUD/detail panel and keep hotspot tokens readable";
            MobilePerformanceRule = "Concurrent flights capped and portrait agents reduced by density budget";
            EvidenceRequirement = "Zoomable player-facing proof includes short flight traces without debug overlay";
            NonLiveBoundary = "Flights are local visual loops, not travel orders, workers, scouts, rewards or server sync";
        }

        public float MinimumSeconds { get; }
        public float MaximumSeconds { get; }
        public int MaximumConcurrentFlights { get; }
        public string CharacterOrAssetScope { get; }
        public string AnimationRule { get; }
        public string ZoneReadabilityRule { get; }
        public string MobilePerformanceRule { get; }
        public string EvidenceRequirement { get; }
        public string NonLiveBoundary { get; }
    }

    public sealed class BeeWalkCrawlAnimationSet : IBeePresenceContract
    {
        public BeeWalkCrawlAnimationSet(float crawlPixelsPerSecond, string surfaceRule)
        {
            CrawlPixelsPerSecond = Math.Max(0f, crawlPixelsPerSecond);
            SurfaceRule = BeePresenceRequirements.Require(surfaceRule, nameof(surfaceRule));
            CharacterOrAssetScope = "BEE-666 crawl/walk on wax cells, honey bridges and local zone surfaces";
            AnimationRule = "Slow readable crawl, no jitter, no zigzag and no off-screen wandering";
            ZoneReadabilityRule = "Crawl stays inside art surfaces and below hotspot/panel priority";
            MobilePerformanceRule = "Crawl agents count toward the portrait density budget";
            EvidenceRequirement = "Motion kinds proof contains WalkCrawl and captures keep zones readable";
            NonLiveBoundary = "Walk/crawl is decorative presence, not assignment, pathfinding, population or production";
        }

        public float CrawlPixelsPerSecond { get; }
        public string SurfaceRule { get; }
        public string CharacterOrAssetScope { get; }
        public string AnimationRule { get; }
        public string ZoneReadabilityRule { get; }
        public string MobilePerformanceRule { get; }
        public string EvidenceRequirement { get; }
        public string NonLiveBoundary { get; }
    }

    public sealed class LocalCollectionGestureAnimation : IBeePresenceContract
    {
        public LocalCollectionGestureAnimation(string gesture, string nonClaimCopy, bool writesEconomy)
        {
            Gesture = BeePresenceRequirements.Require(gesture, nameof(gesture));
            NonClaimCopy = BeePresenceRequirements.Require(nonClaimCopy, nameof(nonClaimCopy));
            WritesEconomy = writesEconomy;
            CharacterOrAssetScope = "BEE-667 local collection gesture around honey and wax reserve visuals";
            AnimationRule = "Small return gesture with honey glow, no inventory mutation and no completion state";
            ZoneReadabilityRule = "Gesture fades under selected zone halo and never masks local/server disclosures";
            MobilePerformanceRule = "Gesture visible only within density budget and hidden if it would enter HUD or panel";
            EvidenceRequirement = "Validation asserts WritesEconomy is false and captures preserve non-claim copy";
            NonLiveBoundary = "Apercu local only: no gain, cost, stock, timer, reward, account, order or server command";
        }

        public string Gesture { get; }
        public string NonClaimCopy { get; }
        public bool WritesEconomy { get; }
        public string CharacterOrAssetScope { get; }
        public string AnimationRule { get; }
        public string ZoneReadabilityRule { get; }
        public string MobilePerformanceRule { get; }
        public string EvidenceRequirement { get; }
        public string NonLiveBoundary { get; }
    }

    public sealed class ZoneEntryExitBeeTrafficMap
    {
        public ZoneEntryExitBeeTrafficMap(IReadOnlyList<string> zoneIds, int desktopBeeBudget, int mobileBeeBudget)
        {
            ZoneIds = zoneIds ?? Array.Empty<string>();
            DesktopBeeBudget = Math.Max(0, desktopBeeBudget);
            MobileBeeBudget = Math.Max(0, mobileBeeBudget);
        }

        public IReadOnlyList<string> ZoneIds { get; }
        public int DesktopBeeBudget { get; }
        public int MobileBeeBudget { get; }
    }

    public sealed class BeeDensityMobilePerformanceBudget
    {
        public BeeDensityMobilePerformanceBudget(int desktopVisibleBees, int portraitVisibleBees, int targetFps)
        {
            DesktopVisibleBees = Math.Max(0, desktopVisibleBees);
            PortraitVisibleBees = Math.Max(0, portraitVisibleBees);
            TargetFps = Math.Max(1, targetFps);
        }

        public int DesktopVisibleBees { get; }
        public int PortraitVisibleBees { get; }
        public int TargetFps { get; }
    }

    public sealed class BeeOcclusionHotspotReadabilityGuard
    {
        public BeeOcclusionHotspotReadabilityGuard(int continuousHotspotOcclusionMs, bool blocksHudPanelOcclusion, bool clickThroughToHotspots)
        {
            ContinuousHotspotOcclusionMs = Math.Max(0, continuousHotspotOcclusionMs);
            BlocksHudPanelOcclusion = blocksHudPanelOcclusion;
            ClickThroughToHotspots = clickThroughToHotspots;
        }

        public int ContinuousHotspotOcclusionMs { get; }
        public bool BlocksHudPanelOcclusion { get; }
        public bool ClickThroughToHotspots { get; }
    }

    public sealed class BeeAnimationStateTokenCompatibility
    {
        public BeeAnimationStateTokenCompatibility(IReadOnlyList<string> supportedStates, bool fadesNearSelectedHotspot)
        {
            SupportedStates = supportedStates ?? Array.Empty<string>();
            FadesNearSelectedHotspot = fadesNearSelectedHotspot;
        }

        public IReadOnlyList<string> SupportedStates { get; }
        public bool FadesNearSelectedHotspot { get; }
    }

    public sealed class BeeAudioHapticPreviewBoundary
    {
        public BeeAudioHapticPreviewBoundary(bool audioPreviewOnly, bool hapticsPreviewOnly, string nonClaimRule)
        {
            AudioPreviewOnly = audioPreviewOnly;
            HapticsPreviewOnly = hapticsPreviewOnly;
            NonClaimRule = BeePresenceRequirements.Require(nonClaimRule, nameof(nonClaimRule));
        }

        public bool AudioPreviewOnly { get; }
        public bool HapticsPreviewOnly { get; }
        public string NonClaimRule { get; }
    }

    public sealed class BeeAssetImportNamingVersionPipeline : IBeePresenceContract
    {
        public BeeAssetImportNamingVersionPipeline(IReadOnlyList<string> approvedAssetPrefixes, string versionRule, string provenanceRule)
        {
            ApprovedAssetPrefixes = approvedAssetPrefixes ?? Array.Empty<string>();
            VersionRule = BeePresenceRequirements.Require(versionRule, nameof(versionRule));
            ProvenanceRule = BeePresenceRequirements.Require(provenanceRule, nameof(provenanceRule));
            CharacterOrAssetScope = "BEE-673 Bee_[Family]_[AssetType]_[LOD]_[Variant]_v### plus Bee_Mobile_Atlas_v###";
            AnimationRule = "Every motion clip or sprite must be bound to a family, role, LOD, status and source";
            ZoneReadabilityRule = "Assets with noisy silhouettes or hotspot occlusion risk are rejected before runtime use";
            MobilePerformanceRule = "LOD1/fallback static and mobile atlas are required for portrait density control";
            EvidenceRequirement = "Manifest path and approved prefixes are exposed in Builder bundle";
            NonLiveBoundary = "Asset provenance documents visual preview status and does not certify production/live use";
        }

        public IReadOnlyList<string> ApprovedAssetPrefixes { get; }
        public string VersionRule { get; }
        public string ProvenanceRule { get; }
        public string CharacterOrAssetScope { get; }
        public string AnimationRule { get; }
        public string ZoneReadabilityRule { get; }
        public string MobilePerformanceRule { get; }
        public string EvidenceRequirement { get; }
        public string NonLiveBoundary { get; }
    }

    public sealed class BeeAnimationAuthoringHandoff : IBeePresenceContract
    {
        public BeeAnimationAuthoringHandoff(IReadOnlyList<BeePresenceMotionKind> motions, string reducedMotionRule)
        {
            Motions = motions ?? Array.Empty<BeePresenceMotionKind>();
            ReducedMotionRule = BeePresenceRequirements.Require(reducedMotionRule, nameof(reducedMotionRule));
            CharacterOrAssetScope = "BEE-674 handoff covers families, poses, protected zones, shot list and QA refusal criteria";
            AnimationRule = "Idle 2.5-4s, short flight 0.8-1.6s, crawl 1.5-3s, entry/exit 1-2.5s; reduced motion uses idle-only";
            ZoneReadabilityRule = "Protected Defense and Centre alliance zones keep non-claims and state tokens above motion";
            MobilePerformanceRule = "Handoff includes portrait density, fallback static and no pulse spam";
            EvidenceRequirement = "Builder captures desktop/mobile/zoom proof and exposes motion list";
            NonLiveBoundary = "Handoff does not create authoritative animation state, action queue or server protocol";
        }

        public IReadOnlyList<BeePresenceMotionKind> Motions { get; }
        public string ReducedMotionRule { get; }
        public string CharacterOrAssetScope { get; }
        public string AnimationRule { get; }
        public string ZoneReadabilityRule { get; }
        public string MobilePerformanceRule { get; }
        public string EvidenceRequirement { get; }
        public string NonLiveBoundary { get; }
    }

    public sealed class StaticVersusInhabitedHiveDemoContract
    {
        public StaticVersusInhabitedHiveDemoContract(IReadOnlyList<string> requiredShots)
        {
            RequiredShots = requiredShots ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> RequiredShots { get; }
    }

    public sealed class QaLiveHivePresenceValidationProtocol
    {
        public QaLiveHivePresenceValidationProtocol(IReadOnlyList<string> checkpoints)
        {
            Checkpoints = checkpoints ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> Checkpoints { get; }
    }

    public sealed class ServerNonLiveAuditInhabitedHive
    {
        public ServerNonLiveAuditInhabitedHive(IReadOnlyList<string> forbiddenClaims, bool backendRuntimeCreated)
        {
            ForbiddenClaims = forbiddenClaims ?? Array.Empty<string>();
            BackendRuntimeCreated = backendRuntimeCreated;
        }

        public IReadOnlyList<string> ForbiddenClaims { get; }
        public bool BackendRuntimeCreated { get; }
        public bool PassesUnityBoundary => !BackendRuntimeCreated;
    }

    public sealed class BuilderLiveHivePresenceImplementationBundle : IBeePresenceContract
    {
        public BuilderLiveHivePresenceImplementationBundle(IReadOnlyList<string> artifacts, bool sandboxPlaygroundIntegrated)
        {
            Artifacts = artifacts ?? Array.Empty<string>();
            SandboxPlaygroundIntegrated = sandboxPlaygroundIntegrated;
            CharacterOrAssetScope = "BEE-678 SandboxPlayground presenter, live presence frameworks, tests, captures, manifests and docs";
            AnimationRule = "Scoped runtime presenter layer, no scene replacement and no global animation manager";
            ZoneReadabilityRule = "Hotspot regression tests and occlusion guard remain part of the bundle";
            MobilePerformanceRule = "Max instances enforced by BeeDensityMobilePerformanceBudget and portrait capture proof";
            EvidenceRequirement = "Compile log, validation log, capture manifest and asset/handoff manifests are bundle outputs";
            NonLiveBoundary = "Builder bundle contains no backend, persistence, economy, progression or server authority";
        }

        public IReadOnlyList<string> Artifacts { get; }
        public bool SandboxPlaygroundIntegrated { get; }
        public string CharacterOrAssetScope { get; }
        public string AnimationRule { get; }
        public string ZoneReadabilityRule { get; }
        public string MobilePerformanceRule { get; }
        public string EvidenceRequirement { get; }
        public string NonLiveBoundary { get; }
    }

    public sealed class UiBeeCharacterMotionScorecard
    {
        public UiBeeCharacterMotionScorecard(IReadOnlyDictionary<string, int> scores)
        {
            Scores = scores ?? new Dictionary<string, int>();
        }

        public IReadOnlyDictionary<string, int> Scores { get; }
        public int MinimumScore => Scores.Count == 0 ? 0 : Scores.Values.Min();
        public bool MeetsUiThreshold => MinimumScore >= 4;
    }

    public sealed class LiveHivePresenceBeeCharactersAnimationGate : IBeePresenceContract
    {
        public LiveHivePresenceBeeCharactersAnimationGate(
            VisibleBeeCharacterFamilyCatalog catalog,
            BeeDensityMobilePerformanceBudget densityBudget,
            BeeOcclusionHotspotReadabilityGuard occlusionGuard,
            ServerNonLiveAuditInhabitedHive nonLiveAudit,
            UiBeeCharacterMotionScorecard scorecard,
            IReadOnlyList<string> reserves)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            DensityBudget = densityBudget ?? throw new ArgumentNullException(nameof(densityBudget));
            OcclusionGuard = occlusionGuard ?? throw new ArgumentNullException(nameof(occlusionGuard));
            NonLiveAudit = nonLiveAudit ?? throw new ArgumentNullException(nameof(nonLiveAudit));
            Scorecard = scorecard ?? throw new ArgumentNullException(nameof(scorecard));
            Reserves = reserves ?? Array.Empty<string>();
            CharacterOrAssetScope = "BEE-680 final gate: families, asset brief, idle, flight, walk, collection, traffic, performance, occlusion and evidence";
            AnimationRule = "All motion contracts must be present, demonstrable, non-aggressive and reduced-motion compatible";
            ZoneReadabilityRule = "Hotspots, state tokens, HUD, panel and non-claims keep visual/tap priority";
            MobilePerformanceRule = "Portrait density budget and fallback constraints are mandatory";
            EvidenceRequirement = "Demo pack, validation log, compile log, manifest assets and BEE-681 blocked status";
            NonLiveBoundary = "No official population, economy, progression, account, chat, ranking, persistence, sync or authoritative server";
            Verdict = Evaluate();
        }

        public VisibleBeeCharacterFamilyCatalog Catalog { get; }
        public BeeDensityMobilePerformanceBudget DensityBudget { get; }
        public BeeOcclusionHotspotReadabilityGuard OcclusionGuard { get; }
        public ServerNonLiveAuditInhabitedHive NonLiveAudit { get; }
        public UiBeeCharacterMotionScorecard Scorecard { get; }
        public IReadOnlyList<string> Reserves { get; }
        public BeePresenceGateVerdict Verdict { get; }
        public bool Bee681Blocked => true;
        public string CharacterOrAssetScope { get; }
        public string AnimationRule { get; }
        public string ZoneReadabilityRule { get; }
        public string MobilePerformanceRule { get; }
        public string EvidenceRequirement { get; }
        public string NonLiveBoundary { get; }

        private BeePresenceGateVerdict Evaluate()
        {
            if (Catalog.VisibleFamilyCount < 4 || DensityBudget.PortraitVisibleBees <= 0 || !OcclusionGuard.ClickThroughToHotspots || !NonLiveAudit.PassesUnityBoundary)
            {
                return BeePresenceGateVerdict.ReworkRequired;
            }

            return Reserves.Count == 0 && Scorecard.MeetsUiThreshold ? BeePresenceGateVerdict.Pass : BeePresenceGateVerdict.PassWithReserves;
        }
    }

    internal static class BeePresenceRequirements
    {
        public static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A live hive presence field is required.", name);
            return value;
        }
    }
}
