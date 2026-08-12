using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Playground
{
    public sealed class ServerFirstMmoConnectionIntake : ProductionRuntimeBridgeContractBase
    {
        public ServerFirstMmoConnectionIntake() : base(
            "BEE-721 ouvre le lot server-first avec ARCH-154, SERVER-023 et la cible production 104.129.128.136 non routee.",
            "Connexion serveur requise pour le jeu officiel; consultation demo non officielle disponible.",
            "SERVER-023 est un fait local non gameplay; la production 104.129.128.136 reste non routee pour Bee Kingdom.",
            "Preuve distincte ARCH-154, SERVER-023, cible non routee, interdiction hors serveur et BEE-741 bloquee.",
            "Aucune voie de jeu officielle hors serveur.",
            "BEE-741 bloquee jusqu'a validation architecte BEE-721 a BEE-740.") { }
    }

    public sealed class ProductionDeploymentRouteVerification : ProductionRuntimeBridgeContractBase
    {
        public ProductionDeploymentRouteVerification() : base(
            "BEE-722 verifie /health, /runtime/handshake, distinction StillThereForYou/Bee Kingdom, 404 non route et accord avant publish.",
            "Service Bee Kingdom en preparation; route production non confirmee.",
            "Unity ne publie rien; toute verification route production reste non destructive et appartient a Bee Server/Ops.",
            "Manifest route avec health, handshake, 404 et accord publish requis.",
            "Pas de claim production routee ou serveur live.",
            "Handoff Bee Server pour publish.") { }
    }

    public sealed class RuntimeHandshakeProductionAvailabilityGate : ProductionRuntimeBridgeContractBase
    {
        public RuntimeHandshakeProductionAvailabilityGate() : base(
            "BEE-723 prepare le gate POST /runtime/handshake public non secret, sans ressource ni colonie mutable.",
            "Handshake futur: disponibilite, maintenance ou incompatibilite seulement.",
            "Le handshake ne transporte aucune ressource, abeille, population, construction, sauvegarde ou commande.",
            "Preuve mock conforme SERVER-023 et 404 production documente.",
            "Aucune mutation gameplay dans le handshake.",
            "BEE-739 integration mock cote Unity.") { }
    }

    public sealed class OfficialAccountSessionEntryBoundary : ProductionRuntimeBridgeContractBase
    {
        public OfficialAccountSessionEntryBoundary() : base(
            "BEE-724 separe compte officiel futur, session officielle future, aucune sauvegarde compte, profil actif ou progression liee.",
            "Compte non actif; session officielle future.",
            "Compte/session officiels appartiennent a Bee Server; Unity ne cree ni ne conserve d'identite officielle.",
            "Preuve UI compte futur sans sauvegarde ni profil serveur actif.",
            "Aucun compte live, profil officiel, sauvegarde compte ou progression liee au compte.",
            "Bee Server devra fournir auth/session officielle.") { }
    }

    public sealed class ServerRequiredEntryUiContract : ProductionRuntimeBridgeContractBase
    {
        public ServerRequiredEntryUiContract() : base(
            "BEE-725 impose CTA connexion requis, consultation secondaire, etats maintenance/indisponible et aucun bouton de jeu officiel sans serveur.",
            "CTA principal: Connexion serveur. CTA secondaire: Consulter demo.",
            "Les etats serveur affiches sont des etats de boundary, pas une connexion live.",
            "Captures desktop et portrait avec serveur requis, indisponible et maintenance future.",
            "Aucun bouton de jeu officiel sans serveur.",
            "UI peut embellir sans changer l'autorite.") { }
    }

    public sealed class OfflineConsultationOnlyGuard : ProductionRuntimeBridgeContractBase
    {
        public OfflineConsultationOnlyGuard() : base(
            "BEE-726 verrouille le hors ligne en read-only: aucune action officielle, progression, sauvegarde ou economie.",
            "Hors ligne = consultation seulement.",
            "Offline ne valide aucune commande et ne persiste aucun etat officiel.",
            "Test Unity prouvant OfficialGameplayRequiresServer et GameplayMutationAllowed false.",
            "Aucune progression hors ligne, sauvegarde hors ligne ou economie hors ligne.",
            "Toute mutation future exige serveur.") { }
    }

    public sealed class ProductionRouteSecretlessDeploymentChecklist : ProductionRuntimeBridgeContractBase
    {
        public ProductionRouteSecretlessDeploymentChecklist() : base(
            "BEE-727 declare hash admin key, hash migration key, connection string hors depot, package publish separe et aucun secret capture.",
            "Aucun detail ops visible dans la vue joueur.",
            "Unity ne lit aucun secret et ne stocke aucune connection string.",
            "Audit absence secret dans captures et manifestes.",
            "Aucun secret, cle, hash complet ou connection string player-facing.",
            "Bee Server/Ops gere les secrets hors depot.") { }
    }

    public sealed class OpsEndpointExposureAndAdminGuard : ProductionRuntimeBridgeContractBase
    {
        public OpsEndpointExposureAndAdminGuard() : base(
            "BEE-728 garde ops: admin key hash, migration key distincte, ops non player-facing, apply protegee, rollback lecture seule.",
            "Les joueurs voient seulement service en preparation.",
            "Les endpoints ops/admin ne sont jamais appeles ni exposes par la vue joueur Unity.",
            "Preuve player-facing sans ops, admin, migration ni rollback.",
            "Aucun endpoint ops player-facing.",
            "Bee Server garde la protection admin.") { }
    }

    public sealed class AccountIdentityClaimBoundary : ProductionRuntimeBridgeContractBase
    {
        public AccountIdentityClaimBoundary() : base(
            "BEE-729 limite l'identite: account id technique futur, display name non officiel, aucun role alliance, historique ou badge progression.",
            "Identite preview non officielle.",
            "Unity peut afficher une etiquette demo, jamais un compte officiel.",
            "Preuve microcopy display name non officiel et role alliance absent.",
            "Aucun role alliance, historique officiel ou badge progression.",
            "Identity officielle via Bee Server uniquement.") { }
    }

    public sealed class SessionTokenLifecycleReadiness : ProductionRuntimeBridgeContractBase
    {
        public SessionTokenLifecycleReadiness() : base(
            "BEE-730 prepare issue token, refresh, expiration, revocation futures et interdit tout token en log.",
            "Session future; aucun token visible.",
            "Unity ne genere, persiste, loggue ou rafraichit aucun token live.",
            "Audit texte et logs sans token.",
            "Aucun token en log ou capture.",
            "Bee Server fournira cycle token.") { }
    }

    public sealed class PlayerColonyReadModelServerEntry : ProductionRuntimeBridgeContractBase
    {
        public PlayerColonyReadModelServerEntry() : base(
            "BEE-731 prepare colony id futur et interdit ressource, population ou construction officielle cote Unity.",
            "Donnees non officielles; disponibilite publique seulement.",
            "Read model colonie officiel appartient au serveur; Unity consomme seulement un futur contrat read-only.",
            "Preuve absence claim ressources/population/construction officielles.",
            "Aucune ressource officielle, population officielle ou construction officielle.",
            "Read model serveur futur.") { }
    }

    public sealed class UnityServerConnectionFailureUx : ProductionRuntimeBridgeContractBase
    {
        public UnityServerConnectionFailureUx() : base(
            "BEE-732 couvre timeout, production 404, maintenance, version incompatible et retenter connexion prioritaire.",
            "Messages courts: delai depasse, serveur non route, maintenance, version incompatible.",
            "Les echecs serveur ne debloquent pas le jeu officiel hors ligne.",
            "Capture portrait serveur indisponible et liste messages failure.",
            "Aucun fallback jouable; consultation seulement.",
            "Retry officiel quand endpoint disponible.") { }
    }

    public sealed class DemoReportLocalPlayLanguagePurge : ProductionRuntimeBridgeContractBase
    {
        public DemoReportLocalPlayLanguagePurge() : base(
            "BEE-733 purge l'ancien lexique offline jouable; impose consultation demo et serveur requis.",
            "Lexique player-facing: consultation demo, connexion serveur requise, jeu officiel serveur.",
            "Le langage Demo ne doit jamais presenter le hors ligne comme jeu officiel.",
            "Test lexical et manifest sans termes interdits.",
            "Ancien lexique offline jouable interdit.",
            "Tous rapports futurs doivent utiliser le lexique server-first.") { }
    }

    public sealed class QaServerFirstAcceptanceProtocol : ProductionRuntimeBridgeContractBase
    {
        public QaServerFirstAcceptanceProtocol() : base(
            "BEE-734 fixe QA: serveur requis, route production, handshake, langage Demo et absence social live.",
            "QA voit un shell officiel serveur et une consultation demo separee.",
            "QA valide seulement les preuves non destructives; Server valide la production.",
            "Checklist QA server-first dans le manifest BEE-740.",
            "Aucun social live, alliance live, chat, PvP, ranking ou matchmaking.",
            "QA finalise avec reserves explicites.") { }
    }

    public sealed class UiConnectedEntryStateLanguage : ProductionRuntimeBridgeContractBase
    {
        public UiConnectedEntryStateLanguage() : base(
            "BEE-735 impose connexion requise, serveur requis pour jouer, service en preparation, consultation seulement, donnees non officielles.",
            "Connexion serveur requise; service en preparation; consultation seulement.",
            "Ces textes ne prouvent aucune session live.",
            "Captures desktop et portrait avec microcopy visible.",
            "Donnees non officielles tant que serveur non connecte.",
            "UI garde ce lexique.") { }
    }

    public sealed class ProductionDeploymentRollbackApproval : ProductionRuntimeBridgeContractBase
    {
        public ProductionDeploymentRollbackApproval() : base(
            "BEE-736 exige accord explicite, backup evidence, fenetre maintenance, rollback plan et post deploy check.",
            "Aucun etat joueur ne pretend un deploiement approuve.",
            "Unity ne lance aucun deploy, backup, rollback ou post-deploy check.",
            "Manifest indique handoff Server/Ops.",
            "Aucun publish ou rollback implicite.",
            "Accord proprietaire requis avant production.") { }
    }

    public sealed class SqlBackupRestoreProofReadiness : ProductionRuntimeBridgeContractBase
    {
        public SqlBackupRestoreProofReadiness() : base(
            "BEE-737 exige backup evidence, restore non production, checksum, temps restore et aucune execution implicite.",
            "SQL invisible joueur; service en preparation.",
            "Unity ne touche pas SQL et ne simule pas une restauration officielle.",
            "Rapport Builder marque handoff serveur.",
            "Aucun backup/restore SQL execute par Unity.",
            "Bee Server/Ops doit prouver backup restore.") { }
    }

    public sealed class ServerFirstMobileEntryDeviceEvidence : ProductionRuntimeBridgeContractBase
    {
        public ServerFirstMobileEntryDeviceEvidence() : base(
            "BEE-738 demande portrait connexion requise, portrait serveur indisponible, safe area clavier, cibles tactiles et reserve device.",
            "Portrait mobile: Connexion serveur / Consulter demo lisibles sans texte coupe.",
            "Device physique non invente; Unity produit capture editor seulement.",
            "Captures 390x844 et manifest reserve device explicite.",
            "Pas de preuve appareil physique inventee.",
            "QA/device pass futur.") { }
    }

    public sealed class BuilderServerConnectionIntegrationBundle : ProductionRuntimeBridgeContractBase
    {
        public BuilderServerConnectionIntegrationBundle() : base(
            "BEE-739 bundle Builder: contrat handshake public, mock conforme, tests Unity connexion, non-regression BEE-700 et aucune mutation gameplay.",
            "Mock handshake non gameplay; la ruche reste consultable.",
            "Aucun appel live obligatoire; pas de gameplay mutate par le mock.",
            "Tests Unity server-first et captures DEMO-058.",
            "Aucune mutation gameplay.",
            "Bee Server prend le relais pour endpoint live.") { }
    }

    public sealed class ServerFirstMmoConnectionProductionRouteAndAccountSessionGate : ProductionRuntimeBridgeContractBase
    {
        public ServerFirstMmoConnectionProductionRouteAndAccountSessionGate(RuntimeBridgeGateVerdict verdict) : base(
            "BEE-740 ferme BEE-721 a BEE-740, bloque BEE-741, verifie route production, compte/session non live et lexique server-first.",
            "Jeu officiel via serveur; consultation demo non officielle hors ligne.",
            "Unity ne devient pas source authoritative; production route, compte et session restent Bee Server.",
            "Captures desktop, portrait, proof manifest, tests Unity et rapports Builder.",
            "Aucun compte live, session live, sauvegarde, economie, social live ou synchronisation temps reel.",
            "BEE-741 bloquee jusqu'a validation architecte.")
        {
            Verdict = verdict;
            Bee741Blocked = true;
            OfficialGameplayRequiresServer = true;
            OfflineIsConsultationOnly = true;
        }

        public RuntimeBridgeGateVerdict Verdict { get; }
        public bool Bee741Blocked { get; }
        public bool OfficialGameplayRequiresServer { get; }
        public bool OfflineIsConsultationOnly { get; }
    }

    public sealed class ServerFirstConnectionEvidenceManifest
    {
        public ServerFirstConnectionEvidenceManifest(IReadOnlyList<string> captures, IReadOnlyList<string> forbiddenLanguage, IReadOnlyList<string> requiredLanguage, bool productionRouteNonRouted, bool server023HandshakeLocalFact)
        {
            Captures = captures ?? Array.Empty<string>();
            ForbiddenLanguage = forbiddenLanguage ?? Array.Empty<string>();
            RequiredLanguage = requiredLanguage ?? Array.Empty<string>();
            ProductionRouteNonRouted = productionRouteNonRouted;
            Server023HandshakeLocalFact = server023HandshakeLocalFact;
        }

        public IReadOnlyList<string> Captures { get; }
        public IReadOnlyList<string> ForbiddenLanguage { get; }
        public IReadOnlyList<string> RequiredLanguage { get; }
        public bool ProductionRouteNonRouted { get; }
        public bool Server023HandshakeLocalFact { get; }
    }

    public sealed class ServerFirstConnectionFailureCatalog
    {
        private static readonly string[] Messages =
        {
            "Delai de connexion depasse. Connexion serveur requise pour le jeu officiel.",
            "Production Bee Kingdom non routee. Consultation demo seulement.",
            "Service en maintenance. Consulter demo sans progression.",
            "Version client incompatible. Consultation demo seulement avant mise a jour et connexion serveur.",
            "Retenter connexion serveur reste l'action prioritaire pour jouer officiellement."
        };

        public IReadOnlyList<string> PlayerMessages => Messages;
        public bool KeepsOfflineConsultationOnly => Messages.All(message => message.IndexOf("jeu officiel", StringComparison.OrdinalIgnoreCase) >= 0 || message.IndexOf("Consult", StringComparison.OrdinalIgnoreCase) >= 0 || message.IndexOf("connexion serveur", StringComparison.OrdinalIgnoreCase) >= 0);
    }
}
