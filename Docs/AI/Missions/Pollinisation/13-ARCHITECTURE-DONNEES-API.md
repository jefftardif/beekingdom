# 13 — Architecture d'implantation : données, serveur, API, transactions

**Statut : SPECIFICATION CIBLE, aucune API ci-dessous n'est déclarée implémentée.**

## Autorité et frontières

BeeKingdom est un MMO multi-joueurs : la pollinisation écologique, la présence réelle des civils, les niveaux d'alliance et les récompenses doivent être **authoritative server-side**. Unity n'affiche que des projections et envoie des intentions authentifiées.

Composants conceptuels :
- **CivilPopulation** (propriété de la ruche) : cohortes, effectifs disponibles, en mission, fatigue/blessures, spécialisations.
- **CivilExpedition** (propriété de la ruche et identité de mission globale) : essaim, championne, éventuelle escorte, itinéraire, progression, cargaison et reçu.
- **FloralSite/Ecology** (état mondial partagé) : capacité, famille, fertilité, biodiversité, réserves, cycle, version.
- **PollinationProject** (état collectif) : seuils, membres qualifiés, alliances autorisées, ledger, étapes.
- **AllianceTerritory/FloralOutpost** (état d'alliance) : contrôle, droits et effets logistiques.
- **BloomPact** (relation alliances) : partenaires, droits, durée, clauses et révocation.
- **Event** (plus tard) : floraisons, parasites, ours et classements.
- **RewardLedger** : écriture une fois des crédits, gains collectifs, pénalités et historique de décisions.

Le schéma exact doit épouser l'infrastructure SQL/JSON actuelle ; **ne pas supposer qu'une transaction par ruche couvre un champ partagé**.

## Entités et champs minimaux recommandés

### CivilPopulationState, rattaché au PlayerHiveState ou à un agrégat civil

- playerId, hiveId, revision, schemaVersion.
- stockBySpecialty: available, training, recovering, injured.
- broodCohorts: cohortId, startedAtUtc, readyAtUtc, size, state.
- activeExpeditions: IDs ; reservations matérialisées avec effectifs.
- capacityLimits: population, sorties civiles, taille par essaim.
- statistics: experience, qualifications, dernière régénération réglée serveur.
- idempotencyReceipts (bornés selon politique de rétention).

### FloralSiteState, monde partagé

- worldId, regionId, siteId, catalogVersion, flowerFamily.
- tier, lifecyclePhase, phaseUntilUtc, fertilityBp, biodiversityBp, reserveUnits, maxReserveUnits.
- lastSettledAtUtc, revision, ownership/access **référence uniquement** (pas duplicata de source d'autorité).
- activeContributionWindow, eventModifiers, safeZoneRule.

### CivilExpedition

- expeditionId, playerId, hiveId, ownerAllianceIdAtLaunch (nullable), sourceId, destinationId (nullable), routeVersion.
- missionMode, workerCountBySpecialty, championId nullable, escortReservationId nullable.
- state, timestamps UTC (started, transit, work, return, complete), cargoBefore/After, ecologyEffect.
- costReceiptId, payoutReceiptId, idempotencyKey, revision.
- failureCode/returnPolicy, projectContributionId (nullable).

### PollinationProject/Contributions

- projectId, initiatorAllianceId, regionId, siteIds, pactIds, open/close times, goals and current counts, level/prerequisites.
- membersQualified **uniques** avec seuil de contribution ; routesRequired / routesCompleted.
- ledger immuable d'événements de contribution, récompenses et révocations ; settledAt, revision.
- contribution individuelle identifiée par expeditionId unique ; relation alliance et pacte figée lors de l'action.

## Séparation avec systèmes existants

- WorldResourceCollectionService.cs utilise **CommittedTroops** et peut n'admettre qu'un vol actif. **Ne pas dériver le pool civil de ce contrat** ; créer un nouveau type de mission civile.
- WorldPresenceService.cs est un échantillon ambiant, **pas** un répertoire de toutes les missions ; créer index/queries adaptés pour les vues collectives.
- NurseryCohortService.cs est un prototype à compatibilité délicate ; extension additive uniquement après audit, sans recrues gratuites.
- AcademyTrainingService.cs et ses certificats ont un contrat distinct ; ne pas changer la signification des diplômes existants.
- Les outils de régionalisation de Assets/BeeKingdom/World sont utiles à l'affichage/simulation, mais **pas une preuve d'autorité serveur**.
- BeeKingdom.Alliance dépend de HiveOperations : utiliser un **port/interface** ou couche application pour les bonus et permissions ; **aucune dépendance cyclique**.
- Le système d'escorte militaire conserve sa propre réservation et sa propre cohérence avec la Caserne et le combat.

## Exemple de contrat API PROPOSÉ (aucun endpoint actuellement certifié)

Toutes les routes sous /game/v1, HTTPS/auth requises, identité joueur dérivée du jeton :
- GET /game/v1/hives/{hiveId}/civil-population
- POST /game/v1/hives/{hiveId}/civil-cohorts/start
- POST /game/v1/hives/{hiveId}/civil-cohorts/{cohortId}/claim
- GET /game/v1/world/floral-sites?regionId=...
- GET /game/v1/world/floral-sites/{siteId}
- POST /game/v1/hives/{hiveId}/civil-expeditions/prepare
- POST /game/v1/hives/{hiveId}/civil-expeditions/start
- GET /game/v1/hives/{hiveId}/civil-expeditions
- POST /game/v1/hives/{hiveId}/civil-expeditions/{expeditionId}/claim
- GET /game/v1/alliances/{allianceId}/floral-projects
- POST /game/v1/alliances/{allianceId}/floral-projects
- GET /game/v1/alliances/{allianceId}/floral-contributions
- POST /game/v1/alliances/{allianceId}/bloom-pacts (Bêta, après sécurité)
- GET /game/v1/world/bloom-events (phase ultérieure)

### Exemple d'intention de lancement (non final)

~~~json
{
  "expectedHiveRevision": 128,
  "expectedSiteRevision": 74,
  "idempotencyKey": "client-unique-key",
  "sourceSiteId": "trifolium-a",
  "destinationSiteId": "trifolium-b",
  "workers": [{"specialty": "general_forager", "count": 100}],
  "missionMode": "balanced",
  "championId": null,
  "escortId": null,
  "projectId": null
}
~~~

Le serveur **recalcule** tous les prérequis et effets ; ni l'horloge client, ni les récompenses, ni la compatibilité déclarée par Unity n'ont autorité. expectedSiteRevision peut provoquer 409/relecture sur conflit.

## Gestion multi-agrégats et atomicité

Le lancement et l'achèvement peuvent toucher **ruche + site + projet + alliance + récompense**. Choisir une stratégie vérifiable :
1. Si même base transactionnelle : transaction unique avec mises à jour conditionnelles revision/CAS, ordre fixe des verrous et ledger d'idempotence.
2. Si repos distincts : ordonnanceur/saga durable avec réservations, messages idempotents, compensation et **outbox** pour garantir la livraison des effets.
3. Ne jamais supposer qu'un verrou local sur PlayerHiveState protège un FloralSite partagé par tous.

Séquence d'un lancement : authentification -> vérification/validation du trajet -> réservations atomiques civils+coûts+ressources/slots -> création mission -> reçu -> projection.  
Séquence résolution : serveur relit état/time -> prélève/affecte écologie + contribution une fois -> décide récompense -> libère civils -> événement audit -> reçu.

Le traitement doit être réentrant : une mission à reprendre après crash peut être recalculée à partir d'états datés, sans double facturation/production.

## Codes d'erreur et comportements

- 401/403 : non authentifié / interdit par rôle ou droit territorial.
- 400 : demande mal formée.
- 404 : entité inaccessible/inexistante.
- 409 : revision obsolète, manque de civils, site épuisé ou conflit de réservation avec code distinct.
- 422 : route incompatible/prérequis gameplay manquant si convention API validée.
- 503 : fonctionnalité désactivée ou indisponible.
- Retry du client uniquement si idempotence garantie ; GET après reconnexion.
- Métadonnées : contractVersion, serverTimeUtc, revision et request/receipt ID.

## Anti-triche, sécurité et exploitation

- Ownership serveur : le joueur n'envoie jamais un playerId d'autorité non vérifié.
- Validation d'identité ruche/joueur/alliance, rôle, pacte et zone au moment de l'action.
- Quotas par joueur/site/projet ; empêcher lancement du même essaim deux fois.
- Concurrence de fin d'expédition et de capture de relais testée, pas seulement test unitaire isolé.
- Logs audit : migration, mission, débits/crédits, variation de fertilité, changement de pacte/contrôle, échec/retour.
- Métriques : missions par minute, refus 409, retards de règlement, écologie/région, création nette de ressources, distribution de contributions, taux d'inactivité.

## Migration / compatibilité

- Schémas additifs avec états civils vides par défaut sur anciennes ruches ; **aucun cadeau implicite** de civils.
- Ne pas modifier les API militaires/ressources existantes sous la même version.
- Feature flags distincts : CivilPopulation, FloralSites, CivilExpeditions, AllianceFloralProjects, FloralTerritories, BloomPacts, FloralEvents (noms à aligner aux conventions serveur).
- Désactiver la **création** de nouvelles missions sans effacer les missions déjà actives ; prévoir mode drainage.
- Aucun déploiement direct ni push lors de la rédaction de ce document.

## Unity / source de vérité

Unity affiche état serveur + éventuellement cache hors ligne **en lecture seule clairement identifié**. Animation de mission et icônes sont une projection, pas la simulation officielle. Changement d'utilisateur/ruche annule les anciens callbacks et cache d'identité ; ne jamais afficher l'essaim du joueur précédent.

## Décisions techniques ouvertes

Placement physique des agrégats dans SQL/JSON, tick lazy ou jobs, politique de rétention des ledgers, service temps réel, index de géolocalisation des sites, contrôle de nombreuses expéditions simultanées, stratégie de forte charge et version de contrats FR/EN.

