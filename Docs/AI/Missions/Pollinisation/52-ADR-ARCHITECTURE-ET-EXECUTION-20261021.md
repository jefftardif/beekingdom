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

