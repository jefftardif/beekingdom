using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Playground
{
    public enum RuntimeBridgeGateVerdict { Pass, PassWithReserves, Blocked }
    public enum RuntimeBridgePlayerMode { LocalPreview, OfflineFallback, ServerPreparation, MaintenanceFuture, ExpiredFuture }

    public interface IProductionRuntimeBridgeContract
    {
        string Scope { get; }
        string PlayerVisibleState { get; }
        string ServerBoundary { get; }
        string EvidenceRequirement { get; }
        string NonClaimRule { get; }
        string NextGate { get; }
    }

    public abstract class ProductionRuntimeBridgeContractBase : IProductionRuntimeBridgeContract
    {
        protected ProductionRuntimeBridgeContractBase(string scope, string playerVisibleState, string serverBoundary, string evidenceRequirement, string nonClaimRule, string nextGate)
        {
            Scope = Require(scope, nameof(scope));
            PlayerVisibleState = Require(playerVisibleState, nameof(playerVisibleState));
            ServerBoundary = Require(serverBoundary, nameof(serverBoundary));
            EvidenceRequirement = Require(evidenceRequirement, nameof(evidenceRequirement));
            NonClaimRule = Require(nonClaimRule, nameof(nonClaimRule));
            NextGate = Require(nextGate, nameof(nextGate));
        }

        public string Scope { get; }
        public string PlayerVisibleState { get; }
        public string ServerBoundary { get; }
        public string EvidenceRequirement { get; }
        public string NonClaimRule { get; }
        public string NextGate { get; }

        protected static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A runtime bridge field is required.", name);
            return value;
        }
    }

    public sealed class ProductionRuntimeBridgeIntake : ProductionRuntimeBridgeContractBase
    {
        public ProductionRuntimeBridgeIntake() : base(
            "BEE-701 ouvre le pont runtime depuis ARCH-150, conserve DEMO-056 comme baseline et garde BEE-721 bloquee.",
            "La ruche reste visible en mode local preview.",
            "SERVER-020 est lie comme reference sans duplication ni commande runtime.",
            "Preuve de transition documentaire/runtime sans MMO live.",
            "Aucun compte, sauvegarde, economie, alliance, chat, PvP, ranking, matchmaking ou sync live.",
            "BEE-721 reste bloquee jusqu'au gate BEE-720.") { }
    }

    public sealed class ServerDeploymentReadinessLedger : ProductionRuntimeBridgeContractBase
    {
        public ServerDeploymentReadinessLedger() : base(
            "BEE-702 ledger readiness: ports 80/443, RDP non presume, separation StillThereForYou/Bee Kingdom Server, backup requis.",
            "Le joueur voit seulement Serveur Bee Kingdom en preparation.",
            "Readiness serveur appartient a Bee Server; Unity ne publie rien et ne lit aucun secret.",
            "Ledger et handoff SERVER-021/022 sans endpoint Unity nouveau.",
            "Pas de claim serveur connecte ou production ready.",
            "SERVER-021/022/023 selon preuves serveur.") { }
    }

    public sealed class OpsReadinessPlayerSafeDisclosure : ProductionRuntimeBridgeContractBase
    {
        public OpsReadinessPlayerSafeDisclosure() : base(
            "BEE-703 traduit readiness ops en langage player-safe.",
            "Message public neutre, sans SQL, admin key, migration key ou stack trace.",
            "Details ops reserves a Server/Admin, jamais au HUD joueur.",
            "Audit textes player-facing et absence de fuite.",
            "Aucun secret, stack trace, provider SQL ou cle ops visible.",
            "QA secret audit avant toute extension.") { }
    }

    public sealed class UnityClientNonGameplayHandshakeBoundary : ProductionRuntimeBridgeContractBase
    {
        public UnityClientNonGameplayHandshakeBoundary() : base(
            "BEE-704 prepare un handshake non gameplay: version client, environnement, compatibilite et maintenance.",
            "Etat discret; la consultation demo reste disponible hors ligne, mais le jeu officiel passe par la connexion serveur.",
            "Aucune ressource, abeille, colonie mutable, action ou progression ne transite.",
            "Scenario offline fallback et incompatibilite future simulables.",
            "Pas de synchronisation temps reel ni commande serveur.",
            "SERVER-023 futur pour protocole officiel.") { }
    }

    public sealed class PlayerAccountEntryReadiness : ProductionRuntimeBridgeContractBase
    {
        public PlayerAccountEntryReadiness() : base(
            "BEE-705 separe invite local, compte futur et profil minimal.",
            "Compte non actif; la consultation demo reste possible, mais le compte/serveur deviennent la voie officielle de jeu.",
            "Compte officiel et sauvegarde compte interdits cote Unity.",
            "Shell montre consultation demo sans login et connexion serveur comme voie officielle future.",
            "Aucun profil serveur actif, aucune sauvegarde compte, aucun paywall.",
            "Handoff account/session vers Bee Server.") { }
    }

    public sealed class PlayerSessionReadinessAndExpiry : ProductionRuntimeBridgeContractBase
    {
        public PlayerSessionReadinessAndExpiry() : base(
            "BEE-706 modele LocalOnly, ServerUnavailable, ExpiredFuture, Maintenance et FutureAuthenticated.",
            "Session future; expiration ne supprime rien et ne bloque pas la ruche locale.",
            "Pas de token live, rotation ou persistence par Unity.",
            "Preuve maintenance/expired simulee sans production.",
            "Aucune session authentifiee live.",
            "SERVER-021 session readiness.") { }
    }

    public sealed class ColonyReadModelApiReadinessBoundary : ProductionRuntimeBridgeContractBase
    {
        public ColonyReadModelApiReadinessBoundary() : base(
            "BEE-707 frontiere read model colonie future read-only.",
            "Donnees non officielles; chiffres de ruche restent preview locale.",
            "Aucune commande colonie, stock officiel ou tick serveur.",
            "QA verifie absence de stock/population/progression officielle.",
            "Unity ne devient pas source de verite colonie.",
            "SERVER-022 read model futur.") { }
    }

    public sealed class RuntimeBridgeOfflineFallback : ProductionRuntimeBridgeContractBase
    {
        public RuntimeBridgeOfflineFallback() : base(
            "BEE-708 fallback offline: serveur indisponible, consultation demo non officielle, pas de spinner permanent.",
            "Hors ligne = consultation seulement; aucune progression, sauvegarde, economie ou action officielle.",
            "Aucun retry agressif ni appel production obligatoire.",
            "Capture serveur indisponible avec ruche consultable.",
            "Aucune perte de donnees pretendue car aucune donnee officielle hors ligne n'existe.",
            "Offline fallback reste actif avant BEE-721.") { }
    }

    public sealed class ProductionRuntimeNonRegressionVisualBaseline : ProductionRuntimeBridgeContractBase
    {
        public ProductionRuntimeNonRegressionVisualBaseline() : base(
            "BEE-709 baseline BEE-700: desktop, portrait, reduced motion, no debug, selection et panneau detail.",
            "Ruche premium reference-backed conservee.",
            "Serveur non requis pour comparer la baseline visuelle.",
            "Captures BEE-700 et bridge regenerables.",
            "Pas d'overlay debug player-facing.",
            "Non-regression obligatoire avant BEE-721.") { }
    }

    public sealed class MobileDeviceEvidencePreparation : ProductionRuntimeBridgeContractBase
    {
        public MobileDeviceEvidencePreparation() : base(
            "BEE-710 prepare preuve mobile: editor, telephone portrait, tablette future, paysage optionnel, seuil tactile.",
            "Portrait 390x844 reste lisible avec microcopy au-dessus du rail.",
            "Aucun claim appareil physique tant que non teste.",
            "Checklist device et capture editor distinctes.",
            "Pas de preuve device inventee.",
            "QA garde reserve appareil reel.") { }
    }

    public sealed class LongMotionEvidenceRuntimeCapture : ProductionRuntimeBridgeContractBase
    {
        public LongMotionEvidenceRuntimeCapture() : base(
            "BEE-711 demande sequence motion longue 10s+, variation hotspot, comparaison reduced motion et multi-frame.",
            "Motion locale observee sans claim production.",
            "Server non concerne; motion reste player-facing locale.",
            "Video/GIF/strip long a produire par Demo.",
            "Pas de synchronisation temps reel.",
            "Reserve motion longue reportee au gate.") { }
    }

    public sealed class HudPortraitDensityRuntimePolish : ProductionRuntimeBridgeContractBase
    {
        public HudPortraitDensityRuntimePolish() : base(
            "BEE-712 budget HUD portrait: badge serveur compact, panneau prioritaire, microcopy au-dessus rail.",
            "Badge serveur futur court et non bloquant.",
            "Statuts serveur consommes comme microcopies futures seulement.",
            "Capture portrait avec fallback et panneau ouvert.",
            "Aucune nouvelle jauge officielle.",
            "UI/QA doivent rescorrer densite mobile.") { }
    }

    public sealed class ReferenceBackedAssetProductionPortReadiness : ProductionRuntimeBridgeContractBase
    {
        public ReferenceBackedAssetProductionPortReadiness() : base(
            "BEE-713 inventaire provenance: art principal, hotspots runtime, icones, tokens, panneaux et anti-bypass.",
            "Image reference-backed reste art layer, pas UI aplatie seule.",
            "Hotspots, selection, tokens et panneau restent runtime.",
            "Manifest provenance et absence placeholder.",
            "Assets preview non certifies production.",
            "Port production UI futur sans casser Sandbox.") { }
    }

    public sealed class MmoPlayerEntryShell : ProductionRuntimeBridgeContractBase
    {
        public MmoPlayerEntryShell() : base(
            "BEE-714 shell entree MMO preview: connexion serveur officielle future, consultation demo non officielle, compte futur, maintenance.",
            "Connexion serveur presentee comme voie officielle; consultation demo disponible sans progression.",
            "Aucun social live, chat, alliance, PvP, ranking ou matchmaking.",
            "Capture shell sans bloquer SandboxPlayground.",
            "Le shell ne promet pas MMO actif.",
            "BEE-721 attend validation architecte.") { }
    }

    public sealed class MmoEntryNonClaimLanguage : ProductionRuntimeBridgeContractBase
    {
        public MmoEntryNonClaimLanguage() : base(
            "BEE-715 lexique non-claim pour entree MMO.",
            "Consultation demo non officielle; Connexion serveur requise pour jeu officiel; Compte non actif; Session future; Donnees non officielles.",
            "Server relit les microcopies mais Unity n'authentifie rien.",
            "Scenario online unavailable avec textes courts.",
            "Mots connecte, sauvegarde, profil officiel et synchronise interdits si non prouves.",
            "QA language gate.") { }
    }

    public sealed class ServerSecretsProductionAccessGovernance : ProductionRuntimeBridgeContractBase
    {
        public ServerSecretsProductionAccessGovernance() : base(
            "BEE-716 gouvernance secrets: mot de passe, AdminKey, MigrationApplyKey, connection strings, backup, rotation.",
            "Aucun detail ops visible joueur.",
            "Secrets hors depot, rapports, scripts et UI.",
            "Audit absence de secrets.",
            "Aucun secret expose.",
            "Handoff Bee Server.") { }
    }

    public sealed class SqlProductionDryRunReadiness : ProductionRuntimeBridgeContractBase
    {
        public SqlProductionDryRunReadiness() : base(
            "BEE-717 dry run SQL: runtime/migration distincts, backup, maintenance window, rollback.",
            "Etat public reste Serveur en preparation.",
            "SQL appartient a Bee Server; Unity n'execute rien.",
            "Documenter sans confondre dry run et production.",
            "Aucun SQL, rollback public ou migration destructive.",
            "SERVER-022 naturel.") { }
    }

    public sealed class RuntimeBridgeDemoEvidencePlan : ProductionRuntimeBridgeContractBase
    {
        public RuntimeBridgeDemoEvidencePlan() : base(
            "BEE-718 plan Demo: desktop, portrait, offline fallback, shell MMO, reduced motion, no-debug.",
            "Preuves bridge player-facing et capture-friendly.",
            "Server peut etre mock/safe seulement, sans secret.",
            "Manifest non-claims bridge.",
            "Pas de planche debug comme preuve joueur.",
            "Demo bridge evidence pack.") { }
    }

    public sealed class RuntimeBridgeQaAcceptanceProtocol : ProductionRuntimeBridgeContractBase
    {
        public RuntimeBridgeQaAcceptanceProtocol() : base(
            "BEE-719 protocole QA: non-regression BEE-700, no secrets, offline fallback, no MMO live claim, server target readiness.",
            "Scorecard lisible mobile, motion, HUD et shell MMO.",
            "QA verifie rapports Server sans deploiement destructif.",
            "Checklist officielle BEE-701 a BEE-720.",
            "Tout claim live ou secret bloque le gate.",
            "BEE-720 gate.") { }
    }

    public sealed class ProductionRuntimeBridgeServerDeploymentReadinessAndMmoPlayerEntryGate : ProductionRuntimeBridgeContractBase
    {
        public ProductionRuntimeBridgeServerDeploymentReadinessAndMmoPlayerEntryGate(RuntimeBridgeGateVerdict verdict) : base(
            "BEE-720 ferme BEE-701 a BEE-720, bloque BEE-721 et route vers SERVER-021/022/023.",
            "Pont runtime en preparation; la connexion serveur est la voie officielle future et le hors ligne reste consultation demo.",
            "Aucun endpoint cree par Unity; Bee Server garde autorite.",
            "Gate avec preuves desktop, portrait, offline fallback, shell MMO, reduced motion et manifest.",
            "Bee Kingdom n'est pas declare MMO live.",
            "BEE-721 bloquee jusqu'a validation architecte.")
        {
            Verdict = verdict;
            Bee721Blocked = true;
        }

        public RuntimeBridgeGateVerdict Verdict { get; }
        public bool Bee721Blocked { get; }
    }

    public sealed class RuntimeBridgePlayerFacingState
    {
        public RuntimeBridgePlayerFacingState(RuntimeBridgePlayerMode mode, string statusTitle, string primaryAction, string disclosure, bool offlineConsultationAvailable, bool gameplayMutationAllowed, bool officialGameplayRequiresServer)
        {
            Mode = mode;
            StatusTitle = statusTitle ?? string.Empty;
            PrimaryAction = primaryAction ?? string.Empty;
            Disclosure = disclosure ?? string.Empty;
            OfflineConsultationAvailable = offlineConsultationAvailable;
            GameplayMutationAllowed = gameplayMutationAllowed;
            OfficialGameplayRequiresServer = officialGameplayRequiresServer;
        }

        public RuntimeBridgePlayerMode Mode { get; }
        public string StatusTitle { get; }
        public string PrimaryAction { get; }
        public string Disclosure { get; }
        public bool OfflineConsultationAvailable { get; }
        public bool GameplayMutationAllowed { get; }
        public bool OfficialGameplayRequiresServer { get; }
    }

    public sealed class RuntimeBridgeEvidenceManifest
    {
        public RuntimeBridgeEvidenceManifest(IReadOnlyList<string> captures, IReadOnlyList<string> nonClaims, bool bee700BaselinePreserved, bool bee721Blocked)
        {
            Captures = captures ?? Array.Empty<string>();
            NonClaims = nonClaims ?? Array.Empty<string>();
            Bee700BaselinePreserved = bee700BaselinePreserved;
            Bee721Blocked = bee721Blocked;
        }

        public IReadOnlyList<string> Captures { get; }
        public IReadOnlyList<string> NonClaims { get; }
        public bool Bee700BaselinePreserved { get; }
        public bool Bee721Blocked { get; }
    }

    public sealed class RuntimeBridgeContractCatalog
    {
        public RuntimeBridgeContractCatalog(IReadOnlyList<IProductionRuntimeBridgeContract> contracts)
        {
            Contracts = contracts ?? Array.Empty<IProductionRuntimeBridgeContract>();
        }

        public IReadOnlyList<IProductionRuntimeBridgeContract> Contracts { get; }
        public bool HasCompleteLot => Contracts.Count >= 20 && Contracts.All(contract => !string.IsNullOrWhiteSpace(contract.Scope));
        public bool HasNoLiveMmoClaim => Contracts.All(contract => contract.NonClaimRule.IndexOf("Aucun", StringComparison.OrdinalIgnoreCase) >= 0 || contract.NonClaimRule.IndexOf("Pas de", StringComparison.OrdinalIgnoreCase) >= 0);
    }
}
