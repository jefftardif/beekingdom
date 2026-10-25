# 52 — ADR / journal d'exécution de la pollinisation (21 octobre 2026)

**Statut : décision technique provisoire, réversible, non approuvée comme équilibrage produit.**
**Machine :** PCJEFF. **Code de départ :** afc9c31e. **Branche isolée :** feature/pollinisation-alpha-20261021.
**Copie isolée :** C:\BeeKingdom\DEV\BK-Studio\staging\pollinisation-alpha-20261021.
**Production :** aucun déploiement de ce chantier; flags OFF.
**Sécurité Git :** main reste sale (modifications Unity, Academy, Infirmerie, banque, localisation, API) et a été laissé intact. Aucun reset ni fusion.

## Réutilisation vérifiée et lacunes

| Source | Décision | Pourquoi |
|---|---|---|
| PlayerHiveState + HiveStateMigrator | ADAPTER | Etat civil nullable additif, réserve militaire inchangée |
| DurableJsonHiveStateRepository, SqlHiveStateRepository | REUTILISER P1 | Transactions mono-ruche sérialisées et sauvegardes existantes |
| NurseryCohortService / NurseryGrowthRules | REUTILISER LES CONVENTIONS, PAS LES EFFECTIFS | Cohortes existantes RecruitsInReserve ne sont pas des civils destinés au champ |
| WorldResourceCollectionService | NE PAS REUTILISER POUR POLLINISATION CIVILE | CommittedTroops mobilise les unités militaires |
| FlowerManager / FlowerPatch / FlowerSpecies / PollinationRules | REUTILISER CATALOGUE ET PRESENTATION, PAS LES SOLDES | Simulation Unity locale, pas monde partagé persistant |
| RegionalEcologyPressureFramework | INSPIRATION / CONVERSION DETERMINISTE | Pas de mécanisme de règlement MMO |
| WorldMapMarchRegistry / WorldPresenceService | PROJECTION, PAS AUTORITE | marches client et présence ambiante échantillonnée |
| AllianceEntity / AllianceResearch / AllianceDiplomacy | REUTILISER PAR INTERFACE | Niveau général d'alliance encore absent ; recherches != niveau |
| Récompenses, honey/pollen/wax, horloge serveur | REUTILISER | Interdiction de créer ressources ou horloges alternatives |

## ADR-001 : essaim civil distinct

- Les civils sont conservés en nombres entiers (available/assigned/recovering), séparés des soldats.
- Première naissance explicitement payée en honey/pollen depuis la ruche; aucun civil gratuit sur migration.
- Une cohorte civile active par ruche, maturation automatique lors d'une lecture authentifiée, sans jeu ouvert.
- Révision de ruche et reçus idempotents : une même requête ne génère ni charge ni génération supplémentaire.
- Aucune perte permanente ni entretien passif imposé; spécialité general_forager V1.
- **Paramètres purement QA, modifiables sans migration :** 20 civils, 50 miel, 10 pollen, 60 minutes, 200 places/niveau Nurserie. **Aucune valeur n'est approuvée pour le lancement.**
- Par sécurité, CivilPopulation.Enabled est faux par défaut.

## ADR-002 : propriété des champs et transaction multijoueur — CHOIX CONDITIONNEL

- L'autorité de chaque site floral est UN registre partagé FloralSiteState (worldId+regionId+siteId), indépendant de toutes les ruches.
- Un retrait de pollen et le crédit/livraison à une ruche ne sont légitimes qu'en **UNE transaction cohérente inter-agrégats**.
- **SQL Server (production visée) :** utiliser même DB et transaction sérialisable, verrouiller ruche puis site dans un ordre stable, versionner les deux, écrire le reçu et l'historique avec le solde et l'écologie.
- **JSON local :** transactions mono-ruche existantes NON suffisantes pour modifier un champ global. Tant qu'une saga/outbox/reconciler durable et testée n'existe pas, interdire les mutations multisources en JSON (lecture seule catalogue possible).
- Ne pas copier les réserves de site dans PlayerHiveState. Ne pas utiliser FlowerPatch Harvest côté Unity comme débit officiel.
- Interdire toute mission/claim sur le site si la migration SQL et la transaction croisée ne sont pas opérationnelles.
- Politique de reprise : l'identifiant d'expédition et les reçus de règlement sont persistants, vérifiés avant toute attribution; un GET après crash doit permettre de régler exactement une fois.
- **Avant de lever les flags :** test concurrence deux ruches partageant une faible réserve, test reprise après crash transaction, test idempotence/redémarrage, contrôle des autorisations, vérification backups SQL et rollback.

## ADR-003 : progression d'alliance

- Ne jamais attribuer un Level fictif à AllianceEntity ni assimiler son arbre de recherches au niveau.
- Concevoir AllianceProgression persistante et ledger de contributions qualifiées avec identités de joueurs uniques.
- Une seule ruche surpuissante n'augmente jamais le nombre de membres distincts qualifiés.
- Les règles d'XP, caps, seuils et récompenses doivent rester configurables/provisoires avant arbitrage produit.

## Contrats P1 implémentés

- GET /game/v1/hives/{hiveId}/civil-population
- POST /game/v1/hives/{hiveId}/civil-population/cohorts/start
- Jeton du joueur obligatoire; contrôle d'existence de ruche avant mutation; ExpectedRevision + IdempotencyKey à la création.
- Serveur seul auteur des délais/coûts/identités. Pas de modification des APIs de combat.
- Code : CivilPopulationService.cs, HiveOperationModels.cs, HiveStateMigrator.cs, Program.cs.
- Tests : CivilPopulationServiceTests.cs, CivilPopulationEndpointTests.cs.

## Contrat suivant (non implémenté à la date initiale de cet ADR)

- Catalogue stable pour au moins deux sites publics compatibles, ID immuables et mapping de coordonnées validé sur la scène du monde.
- FloralSiteState en base SQL globale, fertilité et biodiversité bornées, stock durable, révision.
- Mise en place d'une transaction inter-ruche + site pour chaque expédition, sans débiter deux fois ni créer du pollen sans extraction.
- Deux comptes authentifiés lisent le même site et accomplissent un objectif collectif sans possibilité de solo exploit.

## Regressions et vérification

La preuve est limitée aux tests effectivement exécutés : compilation de l'API et tests des cohortes civiles puis du module HiveOperations. Les tests Unity visuels, la migration SQL en serveur et le test multi-joueur de pollinisation complet ne sont PAS déclarés réussis.

## Décisions produit encore ouvertes

Q-01 à Q-08, Q-31, Q-33 et Q-36 à Q-40 restent ouverts pour lancement. ADR-001 à 003 sont des choix techniques QA réversibles, pas des validations d'équilibrage, perte civile, revenu ou droit territorial.

## Plan retour arrière

Garder flags désactivés, ne pas lancer de nouvelles missions sous OFF. Champs CivilPopulation nullable et états déjà enregistrés doivent demeurer lisibles. Une fois des missions durables existantes, un retour arrière doit drainer ou régler les missions avant changement de version et préserver les reçus. Aucun DROP ni réécriture automatique de soldes.



## Incrément P2.1 — sites floraux mondiaux, lecture persistante (2026-10-21)

**Implémenté en branche isolée, toujours OFF en production :**
- `FloralSiteService.cs` : catalogue versionné côté serveur, identités world/region/site/family, état persistant (réserve pollen, capacité, fertilité et biodiversité en basis points, revision, dernier règlement UTC).
- `FloralSiteRepositories.cs` : dépôt SQL partagé `dbo.FloralSiteStates` avec transaction Serializable et verrou de site ; dépôt JSON local de développement à initialisation atomique en lecture seule.
- `DatabaseCatalog.cs` et `Scripts/096_floral_site_states.sql` : migration SQL additive. Migration créée **mais NON appliquée à SRVESDT**.
- `Program.cs` : feature flag `FloralSites:Enabled=false` par défaut, authentification et deux routes GET :
  - `/game/v1/world/floral-sites`
  - `/game/v1/world/floral-sites/{siteId}`
- Tests `FloralSiteServiceTests.cs` (6 réussis) et `FloralSiteEndpointTests.cs` (4 réussis).

**Limitations explicites :** pas encore de consommation des réserves, cycles/régénération, expéditions ni API de mutations écologiques ; aucune localisation des deux sites QA sur une scène Unity approuvée. Les identifiants `world-qa` et `clover-qa-01` sont exclusivement des jeux de tests. Les deux dépôts lisent le même agrégat global, mais les transactions entre un champ et des ruches sont le lot suivant. Aucune preuve de migration ni de contention SQL contre une base de test réelle n'a encore été exécutée.

## P3.1 — Expéditions civiles / transaction SQL réelle (2026-10-21)

**Implémenté et compilé en branche isolée** :
- `CivilExpeditionModels.cs` + `CivilExpeditionService.cs` : mission équilibrée du civil vers un site et retour, reservation des butineuses, quatre phases dérivées de l'horloge serveur, débit champ = crédit pollen réel à la ruche sous limites de stockage. Les soldats ne sont jamais détournés.
- `SqlCivilExpeditionUnitOfWork.cs` : une transaction SQL Server `SERIALIZABLE` avec verrous `hive:{player}:{hive}` puis `floral:{world}:{site}`, contrôle des révisions, mutation des deux agrégats et insert dans ledger unique au même COMMIT; aucun moteur JSON de mutation autorisé.
- `097_floral_civil_contributions.sql` (et DatabaseCatalog) : historique de contribution unique par `MissionId`, file `FloralCivilActiveMissions` indexée par date de retour et par propriétaire, deux mises à jour dans la même transaction.
- `CivilExpeditionDueProcessor.cs` et `CivilExpeditionSettlementWorker.cs` : traitement serveur toutes les ~30 s sous flags ON; reprend les missions à échéance sans requête Unity. SQL garde la mission à traiter après une panne, et les verrous/reçus empêchent de créditer deux fois.
- Routes `GET /game/v1/hives/{hiveId}/civil-expeditions` et `POST /game/v1/hives/{hiveId}/civil-expeditions/start` ajoutées, avec authentification officielle, contrôle ownership, clé d'idempotence et deux révisions. `CivilExpeditions:Enabled` reste **OFF**.
- Durée 15 min, rendement 1 pollen/civil, gains +5 bp fertilité et +2 bp biodiversité sont **des fixtures/valeurs QA configurables non approuvées**.

**Preuve SQL réelle sur PCJEFF uniquement** :
- SQL Server 2025 LocalDB instance `BKPollinationQA`, DB `BeeKingdomPollinationQA`, schémas 070/096/097 installés localement.
- Console de test reproductible `Server/ops/pollination-sql-qa` (`dotnet run`), sans secret stocké : deux ruches, même réserve de 15 pollen; missions concurrentes 12 + 12, cumul crédité 15, site final 0, deux joueurs distincts, ledger à deux lignes, aucune double récompense.
- Test d'échec réel : insérer volontairement une contribution unique conflictuelle pendant le règlement -> exception SQL, **rollback commun** des réserves, ressources et effectifs; effacer la ligne de test puis réessayer, sans doublon.
- Test d'absence de client : deuxième site, mission lancée puis réglée uniquement par `CivilExpeditionDueProcessor` après avancée horloge UTC; index vidé et civils/récompenses récupérés. Nettoyage des comptes/sites QA exécuté.
- Sur la première exécution, bug SQL `READPAST` avec isolation de connexion réutilisée détecté, corrigé en forçant READ COMMITTED pour le scan de la file; test SQL complet ensuite **PASS**.
- Distinction essentielle : test réel en **LocalDB**, pas encore d'intégration au SQL distant SRVESDT ni validation Unity.

**Limitations de ce lot, non déclarées livrées** :
- Une seule destination florale par mission V1. Routes source -> deuxième champ, formation et spécialisations, champions, génétique, alliances/projets, régénération écologique et escorte : à faire selon le cahier.
- La file d'attente SQL est vérifiée en LocalDB mais le service d'hébergement continu dans une API réellement démarrée avec flags ON n'a pas été testé sur serveur distant; activation interdite avant smoke tests complets et sauvegardes.
- Le registre des contributions est global par joueur/site, **ce n'est pas encore** un projet d'alliance avec minimum obligatoire de membres distincts.
- Les clés/réceptions de mission sont stockées dans la ruche avec limite de 10000 éléments, sans purge automatique (fail-safe); politique retention/scalabilité à concevoir avant Beta/charge.
- Le client Unity premium bilingue n'est pas branché; aucune simulation ne doit prétendre envoyer un essaim officiel.
- Protéger les modifications encore non commitées sur `main`; merge/rebase à préparer avec Academy, recherches et interfaces actuelles, jamais automatique.

**Prochaine marche** : implantation de deux sites officiels géolocalisés sans toucher aux tuiles du monde et API/client Unity FR/EN, puis projets multi-joueurs d'alliance avec anti-solo et progression persistante. Maintenir flags OFF jusqu'au test d'acceptation E2E et rollback validé.
