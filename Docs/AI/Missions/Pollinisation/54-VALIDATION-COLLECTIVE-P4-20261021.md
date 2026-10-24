# 54 — P4.1 : Pollinisation collective, preuves et décisions provisoires

Date : 21 octobre 2026. Statut : code en branche de développement isolée, sans déploiement.
Les valeurs chiffrées sont des hypothèses QA configurables NON approuvées par le Product Owner.

## Décisions approuvées respectées

- D-001 : aucune refonte de carte, paysage, tuile ou biome Unity.
- D-002/D-003 : vrai pilier économique, sites publics pour solitaires et projets pour alliances.
- D-004/D-005 : participation de membres distincts obligatoire, niveau d'alliance séparé des recherches.
- D-006 : civils distincts des soldats.
- D-007/D-008 : escortes et événement ours restent au backlog, sans refonte du gameplay.

## Implémentation réelle

- FloralAllianceProjectModels.cs : état du projet, progression et options serveur sous flag OFF.
- SqlFloralAllianceProjectService.cs : création par leader/officier, lecture par membres réels,
  appartenance et statut lus dans les tables SQL canoniques Alliances/AllianceMemberships,
  contrôle de version, reçus idempotents, slots actifs et confidentialité.
- SqlFloralAllianceContributionApplier.cs : contribution au projet dans la transaction SQL
  EXISTANTE de règlement ruche + site + historique, sans double mutation ni double récompense.
- Scripts/098_floral_alliance_projects.sql + DatabaseCatalog.cs : 4 nouvelles tables additives
  FloralAllianceProjects, FloralAllianceProjectMembers, FloralAllianceProjectDeliveries,
  FloralAllianceProgression, avec contraintes/index de membres et expéditions uniques.
- CivilExpeditionMission.AllianceIdAtStart : alliance active capturée dans SQL au départ.
  Changer d'alliance en chemin ne permet pas de créditer un projet de la nouvelle alliance.
- API protégées : GET /alliance/v1/alliances/{allianceId}/floral-projects ;
  GET /alliance/v1/alliances/{allianceId}/floral-projects/{projectId} ;
  POST /alliance/v1/alliances/{allianceId}/floral-projects.
- Options FloralAllianceProjects:Enabled désactivées par défaut; mutation collective SQL uniquement.

## Invariants collectifs

1. Un projet nécessite simultanément Points >= GoalPoints et
   QualifiedMembers >= RequiredDistinctMembers pour réussir.
2. Un joueur devient qualifié seulement si ses contributions officielles atteignent
   MinimumPointsPerMember; un joueur puissant ne remplace pas plusieurs membres.
3. Le journal est unique par MissionId et les membres uniques par ProjectId + PlayerId.
4. Les ressources créditées en ruche ne dépassent jamais les prélèvements de sites.
5. La progression d'alliance reçoit une fois l'expérience d'objectif à la complétion ;
   aucune nouvelle monnaie, taxe ou niveau issu de la recherche d'alliance.
6. Les sites restent publics : une mission sans alliance reçoit les ressources personnelles
   mais aucun point de projet.

## Règles provisoires à approuver avant activation

- Paramètres QA par défaut : objectif 100 points, 2 participants distincts qualifiés,
  minimum 5 points par participant, niveau 2 à 100 XP, niveau 3 à 300 XP.
- Un seul projet actif pour alliance de niveau 1, deux à partir du niveau 2 en QA.
- L'inscription au projet en cours est automatique pour une mission lancée après création
  du projet, lorsque le joueur est toujours membre de l'alliance du départ à l'arrivée.
- Un changement d'alliance avant le retour coupe la contribution collective,
  tout en préservant la récolte personnelle.
- Une mission terminant après désactivation du flag collectif ne donne pas de
  contribution collective rétroactive. Prévoir politique de drainage avant prod.
- Expérience calculée sur l'objectif du projet, pas sur le dépassement.
- Ces choix ne remplacent aucune question de 17-DECISIONS-OUVERTES.md.

## Validation réelle sur PCJEFF

Machine et base QA : SQL Server 2025 LocalDB BKPollinationQA, base BeeKingdomPollinationQA.
Schémas 070/090/096/097/098 appliqués uniquement dans cette base dédiée.
Commande reproductible : dotnet run --project Server/ops/pollination-alliance-qa/PollinationAllianceQa.csproj.

Assertions vérifiées dans la sonde :
- Projet créé par le Leader, idempotence du double appel.
- Interdiction de création par Member et lecture par un joueur étranger.
- Premier joueur contribue 25 points sur objectif 20 : projet reste Actif,
  car un seul membre est qualifié.
- Deuxième joueur apporte les 5 unités restantes : projet terminé, deux joueurs
  qualifiés, 30 points collectifs, un seul passage au niveau 2 selon les règles QA.
- Collision SQL forcée sur le journal unique du projet : aucun débit/crédit/point
  partiel après rollback ; même mission récupérée sans duplication après correction.
- Réouverture des services : état du projet et niveau toujours persistants.
- Changement d'alliance après lancement : le joueur reçoit son pollen personnel,
  mais le projet de la nouvelle alliance ne reçoit aucun point.
- Nettoyage des lignes de QA après test ; données des joueurs réels préservées.

Ces tests prouvent le cœur SQL, PAS l'interface Unity et PAS un déploiement MMO.

## Limitations et prochaines dépendances

- Interface Unity FR/EN, carte et site floral réellement localisé non raccordés.
- Trajet à deux sites, relais, contrôle de territoire, coalition, génétique,
  bonus de recherche civil, escortes et tanière de l'ours encore au backlog.
- Régénération écologique avancée, charge multi-instance, sauvegarde/restauration
  SRVESDT et arbitrage économie/XP non validés.
- Ne jamais fusionner sur main sans audit des modifications hors pollinisation et
  QA Unity. Aucun déploiement SRVESDT de ce lot, flags OFF.## Bilan final des tests, branche P4.1 (21 octobre 2026)

- Compilation de l'API .NET : **0 erreur**, avertissements préexistants dans des modules non liés.
- Suite complète BeeKingdom.HiveOperations.Tests : **313 réussis, 0 échec, 0 ignoré**.
- Suite complète BeeKingdom.Tests avec SQL LocalDB intégré activé par BEE_SQL_INTEGRATION_CONNECTION_STRING : **716 réussis, 0 échec, 0 ignoré**, journal TRX PollinationP4FullApi.
- Test de cohérence fichier SQL / catalogue de migrations : **PASS** ; contraintes de la migration 098 : **PASS**.
- Test réel Server/ops/pollination-alliance-qa : **PASS**, y compris anti-solo, rôles, idempotence, rollback provoqué, niveau 2 et changement d'alliance.
- Un premier passage minimal de la suite API avait donné un échec parmi 715 tests. L'exécution détaillée avec SQL activé a ensuite donné 716/716 : surveiller d'éventuelles intermittences et ne pas présenter ce premier échec comme inexistant.
- Commit source P4 : 1ce2e711, publié sur la branche GitHub feature/pollinisation-alpha-20261021. Pas de fusion dans main.
- Aucun test Unity visuel, charge MMO multi-instance, restauration SQL SRVESDT ni déploiement officiel effectué.
