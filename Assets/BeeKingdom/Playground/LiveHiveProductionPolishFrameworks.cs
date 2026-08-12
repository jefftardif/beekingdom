using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Playground
{
    public enum LiveHiveProductionPolishVerdict { Pass, PassWithReserves, ReworkRequired }

    public interface ILiveHiveProductionPolishContract
    {
        string FeedbackScope { get; }
        string MotionIntegrationRule { get; }
        string PlayerReadabilityRule { get; }
        string PerformanceEvidence { get; }
        string DemoQaRequirement { get; }
        string NonLiveBoundary { get; }
    }

    public abstract class LiveHiveProductionPolishContractBase : ILiveHiveProductionPolishContract
    {
        protected LiveHiveProductionPolishContractBase(string feedbackScope, string motionIntegrationRule, string playerReadabilityRule, string performanceEvidence, string demoQaRequirement, string nonLiveBoundary)
        {
            FeedbackScope = Require(feedbackScope, nameof(feedbackScope));
            MotionIntegrationRule = Require(motionIntegrationRule, nameof(motionIntegrationRule));
            PlayerReadabilityRule = Require(playerReadabilityRule, nameof(playerReadabilityRule));
            PerformanceEvidence = Require(performanceEvidence, nameof(performanceEvidence));
            DemoQaRequirement = Require(demoQaRequirement, nameof(demoQaRequirement));
            NonLiveBoundary = Require(nonLiveBoundary, nameof(nonLiveBoundary));
        }

        public string FeedbackScope { get; }
        public string MotionIntegrationRule { get; }
        public string PlayerReadabilityRule { get; }
        public string PerformanceEvidence { get; }
        public string DemoQaRequirement { get; }
        public string NonLiveBoundary { get; }

        protected static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A live hive polish field is required.", name);
            return value;
        }
    }

    public sealed class LiveHiveProductionPolishIntake : LiveHiveProductionPolishContractBase
    {
        public LiveHiveProductionPolishIntake() : base(
            "BEE-681 reprend BEE-680, ouvre le gate BEE-700, conserve la ruche habitee preview et reporte reserves motion/mobile/device.",
            "Motion locale uniquement, integree au presenter SandboxPlayground sans scene de remplacement.",
            "Hotspots, panneau detail, HUD, navigation, abeilles visibles et mobile restent coherents.",
            "Preuves motion/performance requises avant fermeture du gate.",
            "Demo/QA doivent prouver player-facing sans overlay debug.",
            "LOCAL PREVIEW seulement: aucun serveur live, economie, progression, population officielle ou synchronisation.") { }
    }

    public sealed class PlayerFeedbackPulseLanguage : LiveHiveProductionPolishContractBase
    {
        public PlayerFeedbackPulseLanguage() : base(
            "Pulses selection/tap/preview/disabled/retour ruche courts, contrastes et non-reward.",
            "Pulse 0.35-0.65s avec easing doux, pas de boucle agressive ni glow de recompense.",
            "Le feedback explique selection locale, preview locale, serveur futur et indisponible sans ambiguite.",
            "Budget sans allocation lourde; reduced motion garde un feedback statique court.",
            "QA verifie comprehension et absence de gain implicite.",
            "Aucun feedback ne cree gain, cout, stock, ordre, reward ou etat serveur.") { }
    }

    public sealed class MotionIntegrationTimingCurveSet : LiveHiveProductionPolishContractBase
    {
        public MotionIntegrationTimingCurveSet() : base(
            "Courbes idle, vol court, panneau open, badge pulse et delai selection.",
            "Idle 2.5-4s, vol 0.8-1.6s, panneau 0.28s, pulse 0.45s, stagger agents par phase deterministe.",
            "Pas de jitter, pas de boucle agressive; motion lisible avant decoration.",
            "FPS friendly, desktop 14 agents, portrait 7 agents, reduced motion idle-only.",
            "Manifest timings et strip motion doivent exister.",
            "Motion visuelle locale non synchronisee.") { }
    }

    public sealed class HiveActivityLayerComposition : LiveHiveProductionPolishContractBase
    {
        public HiveActivityLayerComposition() : base(
            "Composition activity: art reference, hotspots, abeilles, pulses, panneau et HUD.",
            "Abeilles et pulses restent sous HUD/panneau/tokens et au-dessus de l'art.",
            "La couche rend la ruche vivante sans masquer les zones cliquables.",
            "Densite reglee selon viewport et occlusion guard.",
            "Preuve desktop/mobile sans debug visible.",
            "Activite locale decorative seulement.") { }
    }

    public sealed class ZoneSpecificBeeActivityPolish : LiveHiveProductionPolishContractBase
    {
        public ZoneSpecificBeeActivityPolish() : base(
            "Activite par zone: reserve miel, nurserie, defense, recherche, transformation, alliance.",
            "Chaque famille/motion est attachee a une zone et fade pres des etats critiques.",
            "Les zones doivent rester reconnaissables sans lire le panneau.",
            "Budget mobile reduit; agents hors zone critique caches.",
            "QA verifie que le panneau change selon zone selectionnee.",
            "Aucune activite n'indique une production officielle.") { }
    }

    public sealed class PlayerHoverTapFeedbackSeparation : LiveHiveProductionPolishContractBase
    {
        public PlayerHoverTapFeedbackSeparation() : base(
            "Hover doux desktop, tap pulse mobile, disabled/server/future distincts.",
            "Hover ne selectionne pas; tap selectionne et lance pulse local.",
            "Le joueur distingue previsualisation, selection locale et action indisponible.",
            "Hover ignore mobile; tap reste court et sans spam.",
            "Preuve avec hotspot hover/tap et panneau mis a jour.",
            "Input local seulement, aucune commande serveur.") { }
    }

    public sealed class AnimatedDetailPanelResponse : LiveHiveProductionPolishContractBase
    {
        public AnimatedDetailPanelResponse() : base(
            "Panneau detail anime a la selection, CTA pulse local et non-claim stable.",
            "Animation 0.28s en slide/fade; contenu change sans jitter.",
            "Header, icone, valeur, barre et disclosure restent lisibles.",
            "Compact mobile moins dense; reduced motion sans slide.",
            "Demo montre changement panneau entre deux zones.",
            "Panneau preview, aucune action live.") { }
    }

    public sealed class BeeMotionReducedMotionAccessibility : LiveHiveProductionPolishContractBase
    {
        public BeeMotionReducedMotionAccessibility() : base(
            "Fallback reduced motion pour abeilles, pulses, panneau et badges.",
            "Reduced motion remplace trajectoires par idle/static halo.",
            "Lisibilite prioritaire pour tokens et non-claims.",
            "Moins de mouvements et moins d'agents en portrait.",
            "QA verifie option exploitable et non intrusive.",
            "Accessibilite locale, sans preference serveur.") { }
    }

    public sealed class MobileMotionDensityThrottle : LiveHiveProductionPolishContractBase
    {
        public MobileMotionDensityThrottle() : base(
            "Throttle mobile: densite, vitesse, offscreen et occlusion.",
            "Portrait limite a 7 agents et cache les trails trop proches du panneau.",
            "Ruche navigable, pas surchargee.",
            "Target 30 FPS preview, mesures batch exposees.",
            "Capture portrait et preuve performance requises.",
            "Pas de telemetry serveur.") { }
    }

    public sealed class ProductionPolishAssetReplacementChecklist : LiveHiveProductionPolishContractBase
    {
        public ProductionPolishAssetReplacementChecklist() : base(
            "Checklist assets: pas de placeholder, pas de lettres/pastilles/debug.",
            "Sprites premium existants, atlas/provenance et fallback statique declares.",
            "Icones non rognees et personnages lisibles.",
            "Atlas mobile futur reserve, pas bloquant pour preview.",
            "Manifest assets et refus placeholders en docs.",
            "Assets preview ne certifient pas production live.") { }
    }

    public sealed class MotionDebugPlayerViewSeparation : LiveHiveProductionPolishContractBase
    {
        public MotionDebugPlayerViewSeparation() : base(
            "Vue joueur propre; diagnostics uniquement manifestes/captures QA separees.",
            "Aucun overlay debug, FPS texte ou scorecard QA dans player-facing.",
            "Les preuves techniques ne polluent pas la Game View normale.",
            "Performance mesuree hors UI joueur.",
            "QA peut relire logs/manifests separes.",
            "Aucun debug ne simule une source de verite serveur.") { }
    }

    public sealed class SoundlessVisualFeedbackFallback : LiveHiveProductionPolishContractBase
    {
        public SoundlessVisualFeedbackFallback() : base(
            "Feedback visuel suffisant sans son ni haptique.",
            "Pulse, halo, badge et CTA donnent la comprehension sans audio.",
            "Non-claims visibles et discrets.",
            "Pas de dependance audio mobile.",
            "QA comprehension sans son.",
            "Aucun son/haptique officiel ou reward.") { }
    }

    public sealed class MotionInteractionRegressionLock : LiveHiveProductionPolishContractBase
    {
        public MotionInteractionRegressionLock() : base(
            "Verrou regression hotspots, panneau, HUD, nav, mobile, non-claims.",
            "Motion et feedback ne cassent pas selection/pan/click-through.",
            "Toutes les zones restent selectionnables.",
            "Tests editor et captures batch protegent regressions.",
            "QA verifie BEE-660/BEE-680 non casses.",
            "Aucune action live ajoutee par regression.") { }
    }

    public sealed class LiveHivePerformanceEvidencePack : LiveHiveProductionPolishContractBase
    {
        public LiveHivePerformanceEvidencePack(int samples, float averageFrameMs, int allocations)
            : base(
                "Pack performance desktop/mobile preview.",
                "Mesure simulee runtime des agents, pulses et panneau anime.",
                "Lisibilite non sacrifiee au profit de la densite.",
                "Samples=" + samples + ", avgFrameMs=" + averageFrameMs.ToString("0.00") + ", allocations=" + allocations,
                "Manifest performance et log Unity requis.",
                "Mesures locales sans telemetry serveur.")
        {
            Samples = Math.Max(0, samples);
            AverageFrameMs = Math.Max(0f, averageFrameMs);
            Allocations = Math.Max(0, allocations);
        }

        public int Samples { get; }
        public float AverageFrameMs { get; }
        public int Allocations { get; }
        public bool MeetsPreviewBudget => Samples >= 60 && AverageFrameMs <= 33.4f;
    }

    public sealed class PlayerComprehensionFeedbackQaProtocol : LiveHiveProductionPolishContractBase
    {
        public PlayerComprehensionFeedbackQaProtocol() : base(
            "QA comprehension selection/preview/server/future/disabled.",
            "Feedback doit etre immediat et non ambigu.",
            "Un joueur doit comprendre sans panneau QA.",
            "Mobile et reduced motion inclus.",
            "Checklist QA livree avec refus reward/debug.",
            "Non-claims obligatoires.") { }
    }

    public sealed class UiProductionMotionScorecard : LiveHiveProductionPolishContractBase
    {
        public UiProductionMotionScorecard(IReadOnlyDictionary<string, int> scores) : base(
            "Scorecard UI motion/polish/feedback.",
            "Tous les axes critiques doivent atteindre 4/5 pour gate preview.",
            "Player-facing prime sur diagnostics.",
            "Performance reserve au moins 4/5 avec preuve.",
            "Scores exposables a UI/QA hors vue joueur.",
            "Scorecard n'est pas un claim production/live.")
        {
            Scores = scores ?? new Dictionary<string, int>();
        }

        public IReadOnlyDictionary<string, int> Scores { get; }
        public int MinimumScore => Scores.Count == 0 ? 0 : Scores.Values.Min();
        public bool MeetsGate => MinimumScore >= 4;
    }

    public sealed class DemoLiveHiveMotionIntegrationShotList : LiveHiveProductionPolishContractBase
    {
        public DemoLiveHiveMotionIntegrationShotList(IReadOnlyList<string> shots) : base(
            "Shot list Demo player-facing pour BEE-700.",
            "Desktop, mobile, detail animated, hover/tap, motion strip, performance.",
            "Aucun overlay QA dans les captures joueur.",
            "Portrait et performance requis.",
            "Demo handoff clair.",
            "Captures preview/local seulement.")
        {
            Shots = shots ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> Shots { get; }
    }

    public sealed class ServerNonLiveAuditProductionPolish : LiveHiveProductionPolishContractBase
    {
        public ServerNonLiveAuditProductionPolish(bool serverRuntimeCreated) : base(
            "Audit non-live du polish production.",
            "Feedback/motion ne publient aucun evenement serveur.",
            "Copie interdit reward, economie, compte, chat, classement, sync.",
            "Pas de telemetry/performance serveur.",
            "Server relit seulement les non-claims.",
            "Aucun endpoint, schema, persistence ou commande authoritative.")
        {
            ServerRuntimeCreated = serverRuntimeCreated;
        }

        public bool ServerRuntimeCreated { get; }
        public bool PassesBoundary => !ServerRuntimeCreated;
    }

    public sealed class BuilderMotionIntegrationImplementationBundle : LiveHiveProductionPolishContractBase
    {
        public BuilderMotionIntegrationImplementationBundle(IReadOnlyList<string> artifacts) : base(
            "Bundle Builder BEE-681-700.",
            "Presenter hooks, courbes, pulses, panneau anime, preuves motion/performance.",
            "Hotspots, panneau, HUD, navigation et mobile proteges.",
            "Compile log, validation log, capture log et performance manifest.",
            "Demo/QA handoff sans overlay player-facing.",
            "Aucun serveur, economie live, progression officielle ou sync.")
        {
            Artifacts = artifacts ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> Artifacts { get; }
    }

    public sealed class LiveHiveProductionPolishMotionGate : LiveHiveProductionPolishContractBase
    {
        public LiveHiveProductionPolishMotionGate(
            UiProductionMotionScorecard scorecard,
            LiveHivePerformanceEvidencePack performance,
            ServerNonLiveAuditProductionPolish serverAudit,
            IReadOnlyList<string> evidencePaths)
            : base(
                "Gate BEE-700: feedback, pulses, hover/tap, panneau anime, abeilles vivantes et preuves.",
                "Motion curves, activity layer, mobile throttle, reduced motion and regression locks ready.",
                "Player-facing clean view with no debug overlay and no placeholder.",
                performance == null ? "Performance evidence missing" : performance.PerformanceEvidence,
                "Demo shot list, QA protocol, performance pack and motion proof required.",
                "BEE-701 blocked; no live server system.")
        {
            Scorecard = scorecard ?? throw new ArgumentNullException(nameof(scorecard));
            Performance = performance ?? throw new ArgumentNullException(nameof(performance));
            ServerAudit = serverAudit ?? throw new ArgumentNullException(nameof(serverAudit));
            EvidencePaths = evidencePaths ?? Array.Empty<string>();
            Verdict = Evaluate();
        }

        public UiProductionMotionScorecard Scorecard { get; }
        public LiveHivePerformanceEvidencePack Performance { get; }
        public ServerNonLiveAuditProductionPolish ServerAudit { get; }
        public IReadOnlyList<string> EvidencePaths { get; }
        public LiveHiveProductionPolishVerdict Verdict { get; }
        public bool Bee701Blocked => true;

        private LiveHiveProductionPolishVerdict Evaluate()
        {
            if (!Scorecard.MeetsGate || !Performance.MeetsPreviewBudget || !ServerAudit.PassesBoundary || EvidencePaths.Count < 4)
            {
                return LiveHiveProductionPolishVerdict.ReworkRequired;
            }

            return LiveHiveProductionPolishVerdict.PassWithReserves;
        }
    }
}
