# 21 — Audit supplémentaire vérifié du code canonique (PCJEFF, main, HEAD afc9c31e)

> Inventaire documentaire du dépôt local le 20 octobre 2026. Aucune compilation, exécution ou validation du serveur distant ne découle de cet inventaire. Les fichiers peuvent évoluer : l'agent doit comparer HEAD et les contrats avant toute modification.

## Découverte essentielle : il existe déjà un moteur floral CLIENT

Sources inspectées :
- Assets/BeeKingdom/World/FlowerManager.cs
- Assets/BeeKingdom/World/FlowerPatch.cs
- Assets/BeeKingdom/World/FlowerSpecies.cs
- Assets/BeeKingdom/World/PollinationRules.cs
- Assets/BeeKingdom/World/EcologicalBalance.cs
- Assets/BeeKingdom/World/RegionalEcologyPressureFramework.cs
- Assets/BeeKingdom/World/RegionalResourceDistributionFramework.cs
- Assets/BeeKingdom/World/NaturalResourceNode.cs

### Nature réelle des composants

FlowerManager implémente ISimulationSystem, maintient des registres d'espèces et de patches en mémoire et avance la simulation à l'aide de SimulationExecutionContext.DeltaSeconds. Il expose RegisterSpecies, CreatePatch, Harvest, SeedFromRegion, SetEnvironment et Execute ; émet FlowerDepleted/FlowerBloomed quand une dépendance IEventBus est disponible.

FlowerPatch maintient PatchId, RegionId, HexCoordinates, FlowerSpecies, FlowerGrowthStage, AgeSeconds, Nectar et Pollen. Advance calcule le stade et régénère pendant Blooming ; Harvest soustrait nectar/pollen de la mémoire de cet objet. **Ce n'est PAS automatiquement une ressource officielle du serveur MMO**. Les types utilisent double en mémoire.

FlowerSpecies contient SpeciesId, DisplayName, capacités nectar/pollen, BloomCycle et PollinationRules. PollinationRules propose régénération de base + modificateurs pluie/tempête/printemps/hiver. EcologicalBalance a PollinationFactor/ClimateFactor/BiomeFactor ; CombinedFactor multiplie les trois. RegionalEcologyPressureFramework calcule une pression environnementale à partir de l'épuisement, météo, eau, pollinisation et biome ; ce calcul existant n'est pas une attribution persistante de récompenses multi-joueurs.

**Conséquences :**
1. Examiner les tests existants avant création de la nouvelle couche : Assets/BeeKingdom/Tests/Editor/FlowerEcosystemTests.cs et RegionalEcologyPressureFramework107Tests.cs sont présents.
2. **Réutiliser les noms/concepts/catalogues testés quand pertinents**, pas recréer des fleurs incompatibles.
3. Garder la simulation Unity CLIENT en projection/démonstration. Concevoir une version déterministe et autoritaire serveur des règles nécessaires, avec tests de parité ; ne jamais laisser un double côté Unity octroyer des ressources officielles.
4. Éviter d'avoir deux vérités économiques contradictoires (FlowerPatch local vs FloralSiteState serveur). L'agent doit établir une règle de désactivation/lecture-seule/adaptation pour le FlowerManager lorsqu'un site MMO officiel est affiché.
5. Les durées de simulation qui avancent au DeltaSeconds ne suffisent pas pour la persistance d'une expédition après fermeture du jeu.

## Architecture des ruches côté serveur

Server/src/BeeKingdom.HiveOperations/HiveOperationModels.cs définit PlayerHiveState, une suite de sous-états nullable à la fin du record, comprenant WorldResourceCollection, NurseryCohorts, Academy et d'autres modules. Ajouter un état civil ne doit pas provoquer de migration destructive ; aligner HiveStateMigrator.cs.

PlayerHiveState.Resources contient des ResourceBalance (Amount, Capacity) par clé. Les ressources officielles observées sont honey, pollen et wax. Une mission civique ne doit jamais appliquer un gain hors transaction.

NurseryCohortService.cs expose une cohorte avec réserve RecruitsInReserve, en regard de NurseryGrowthRules.cs. Dans StartAsync inspecté, une cohorte est créée sous prérequis de nutrition, stabilité, niveau Nurserie et réserve libre ; **le fragment de lancement vérifié ne réalise pas un débit de honey/pollen/wax**. Ne pas imaginer que la consommation existante est implémentée ; valider le contrat complet. Les principes de reçus et horloge peuvent être réutilisés.

AcademyTrainingService.cs a quatre matières existantes tactics, logistics, exploration, mentorship, leurs certificats et crédits n'appliquent **actuellement aucun bonus de gameplay**. Son contrat est distinct de l'entraînement militaire et de Recherche ; conserver cette séparation.

BuildingUpgradeContracts.cs confirme les **14 clés officielles** :
- nursery_cluster
- honey_storage
- guard_post
- defense_growth
- genetics_garden
- research_node
- warehouse_cells
- wax_workshop
- infirmary_grove
- academy_canopy
- hive_bank
- administration_core
- alliance_future_hall
- archives_honeyfall

Les noms affichés en UI ne permettent pas d'inventer d'autres clés : tout raccordement doit pointer vers ces IDs ou vers une nouvelle installation mondiale explicitement approuvée.

## Collecte et marches : risque d'architecture

WorldResourceCollectionService.cs expose WorldResourceActiveFlight avec CommittedTroops (militaires), NodeId, StartedAtUtc/EndsAtUtc. Le catalogue officiel accepte honey/pollen/wax. Le service est par ruche, avec son propre état WorldResourceCollectionState ; ne pas fusionner civils et soldats au même compteur de recrutement ni recréer les ressources existantes.

WorldPresenceService.cs est explicitement une **présence ambiante échantillonnée** : il lit un maximum d'environ 60 ruches récentes et renvoie au plus 20 sightings. Il ne constitue pas un registre complet de tous les joueurs ni un ledger de pollinisation.

Assets/BeeKingdom/Playground/WorldMapMarchRegistry.cs définit actuellement WorldMapMarchType.Combat et Collection seulement ; ses marches incluent CommittedTroops et une limite statique de 3 marches actives. **C'est une projection/coordination cliente**, PAS une garantie serveur. Ajouter une catégorie civile et discuter des slots civils séparés ; ne jamais déduire l'état civil officiel du registre local.

Assets/BeeKingdom/Networking/WorldResourceCollectionClient.cs fournit lancement/claim/recall de collecte authentifiée ; références utiles pour l'interface et le pattern d'idempotence. Le nouveau client civil devra être séparé, avec session/identité et états d'erreur clairs.

## Alliances : manque réel de niveau explicite

Server/src/BeeKingdom.Alliance/Models/AllianceModels.cs définit AllianceEntity avec identité, statut, membres, nombre maximal, revision ; **aucune propriété de niveau/XP d'alliance n'a été observée dans ce record**. AllianceRole est Member, Officer, Leader. Un joueur ne peut appartenir qu'à une alliance active d'après les commentaires du modèle.

Server/src/BeeKingdom.Alliance/Research/AllianceResearchModels.cs montre AllianceResearchState, contributions, recherches et bonus ProductionBp, CapacityBp, CombatPowerBp. **Recherche d'alliance != niveau d'alliance** ; la spécification doit introduire/proposer une vraie progression d'alliance persistante, ou en déduire une à partir d'un système officiel vérifié, sans confondre les deux.

Server/src/BeeKingdom.Alliance/Models/AllianceDiplomacy.cs possède déjà des concepts de relations Neutral (absence de ligne), NonAggressionPact, Ally, Hostile, War, avec statuts Proposed, Active, Ended, Rejected, Cancelled. La classe AllianceWar n'est pas un moteur de batailles : elle référence la relation et l'état de guerre. **Un Pacte de Floraison ne doit pas réinventer la diplomatie** ; il est soit un accord spécialisé lié à une relation existante, soit une extension compatible versionnée. CanonicalPair et relations stockées doivent être respectés.

Server/src/BeeKingdom.HiveOperations/AllianceGameplayBonus.cs définit un port IAllianceGameplayBonusResolver pour éviter le cycle de dépendances entre HiveOperations et Alliance. Le nouveau service de progression florale doit respecter l'orientation des dépendances.

## Routes API observées, pas celles de la future fonctionnalité

Server/src/BeeKingdom.Server/Program.cs expose notamment :
- GET /game/v1/hives/{hiveId}/academy
- POST /game/v1/hives/{hiveId}/academy/{courseId}/start
- POST /game/v1/hives/{hiveId}/academy/{operationId}/complete
- GET /game/v1/hives/{hiveId}/world-resources
- POST /game/v1/hives/{hiveId}/world-resources/{nodeId}/launch
- POST /game/v1/hives/{hiveId}/world-resources/{flightId:guid}/claim
- POST /game/v1/hives/{hiveId}/world-resources/{flightId:guid}/recall
- GET /game/v1/hives/{hiveId}/world-presence
- POST /alliance/v1/alliances
- GET /alliance/v1/membership/mine
- GET /alliance/v1/alliances/{allianceId:guid}

**Important :** les routes Alliance actuelles sont sous /alliance/v1 et non toutes /game/v1. Les routes de futur projet floral doivent être choisies après vérification des conventions de services, pas imposées par l'ancien brouillon de 13.

## Tanière de l'ours

WorldMapBearDenLandmark.cs référence l'asset visuel, l'ancre, l'exclusion de spawn ; BearVisible retourne false et ActiveEvent retourne false dans le code inspecté. L'objet visuel ne prouve pas l'existence d'un boss mondial fonctionnel. Intention approuvée : raid coopératif et compétitif entre alliances.

## Ce qui reste À AUDITER

- Véritable emplacement monde et clé d'identité des positions du jeu actuellement branché.
- Repositories SQL/JSON, isolation des transactions, schémas et stratégie de stockage partagé.
- Progression de la Génétique et des championnes, conflits de réservation de championne.
- Calcul exact des soldes et production passive, limites de stockage et de vitesse.
- Auth/membership actuelle à l'instant des mutations et droits de progression d'alliance.
- Statut production des flags AcademyTraining et NurseryCohorts sur SRVESDT.
- Tests Unity visuels, tests C# en service complet, conventions de migration de données.
- État Git sale avant modifications, worktrees/branches concurrents et pipeline de déploiement.

## Conclusion d'intégration

Un agent codeur doit créer **les faits persistants civils/multijoueurs manquants** sans remplacer les écosystèmes Unity existants, sans prétendre que les données locales sont officielles et sans dupliquer recrutement militaire, diplomatie ou récompenses.

