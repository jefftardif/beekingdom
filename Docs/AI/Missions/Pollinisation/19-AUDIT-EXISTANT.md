# 19 — Audit ciblé du dépôt existant au 2026-10-20

**Nature :** inventaire partiel de fichiers effectivement consultés sur PCJEFF, branche locale **main**. Ce document n'est ni une certification du jeu ni la preuve du statut du déploiement sur SRVESDT.

## Éléments vérifiés

1. **Carte canonique / Unity** — Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs existe ; Assets/BeeKingdom/WorldMap contient WorldManager, WorldObject, WorldChunk, WorldSelection, streaming et caméra. **Implication :** intégrer les nouveaux points d'intérêt dans l'affichage existant, pas remplacer les tuiles.
2. **Tanière de l'ours** — Assets/BeeKingdom/Playground/WorldMapBearDenLandmark.cs existe ; ce code définit une texture et une ancre. Les propriétés consultées sont BearVisible=false et ActiveEvent=false. **Implication :** la présence graphique n'est PAS un événement mondial jouable : serveur, combat collectif et classement restent à spécifier.
3. **Collecte officielle de ressources** — Server/src/BeeKingdom.HiveOperations/WorldResourceCollectionService.cs contient WorldResourceCollectionState et WorldResourceActiveFlight avec CommittedTroops. Le catalogue connu accepte honey, pollen, wax ; tiers poor/medium/rich. **Implication :** NE PAS recycler les troupes militaires comme butineuses : nouveau pool civil durable, coexistence explicite avec la collecte actuelle.
4. **Présence ambiante** — Server/src/BeeKingdom.HiveOperations/WorldPresenceService.cs échantillonne des vols actifs, avec au plus 20 sightings sur 60 ruches récemment actives, sans registre global complet. **Implication :** ce service ne peut PAS servir de registre autoritaire de missions collectives ou de présence de toutes les alliances.
5. **Nurserie** — Server/src/BeeKingdom.HiveOperations/NurseryCohortService.cs décrit un prototype NurseryCohorts activable par feature flag et désactivé par défaut ; cohorte avec réserve durable. **Implication :** auditer le sens des recrues existantes et leur liaison aux troupes avant de créer des cohortes civiles indépendantes.
6. **Académie** — Docs/AI/Missions/BKSTUDIO-20261019-ACADEMY-TRAINING-V1.md décrit quatre formations et une API authentifiée ; l'activation en production n'était pas autorisée par QA à cette date. **Implication :** ne pas prétendre que toutes les formations sont actives ; ajouter l'apprentissage civil comme extension versionnée, pas écraser les diplômes existants.
7. **Alliance** — Server/src/BeeKingdom.HiveOperations/AllianceGameplayBonus.cs expose IAllianceGameplayBonusResolver ; commentaire indiquant que BeeKingdom.Alliance dépend de HiveOperations et qu'un lien inverse créerait une dépendance circulaire. **Implication :** passer par interfaces/ports ou services orchestrateurs, ne pas ajouter de référence inverse.
8. **Ressources environnementales** — Assets/BeeKingdom/World/NaturalResourceNode.cs et RegionalResourceDistributionFramework.cs contiennent les abstractions de ressources et une catégorie Floral. **Implication :** éléments réutilisables comme concepts, MAIS l'autorité économique réelle doit être validée côté serveur, sans confondre simulation client et état officiel.
9. **Architecture serveur** — répertoires BeeKingdom.HiveOperations, BeeKingdom.Alliance, BeeKingdom.Server, BeeKingdom.Persistence, etc. présents. HiveStateMigrator et repositories SQL/JSON présents. **Implication :** audit de la persistance et migration additive requis.

## Précautions spécifiques

- Le dépôt main présente des **modifications préexistantes** ; ne pas faire de reset, checkout destructif, git add global ni fusion sauvage.
- Les fichiers liés à l'infirmerie, à l'Alliance, à la Banque, aux fenêtres et aux tests semblent modifiés : hors périmètre de la mission documentaire.
- L'état réel des fonctionnalités et du déploiement peut différer entre PCJEFF, GitHub et SRVESDT.
- Ne pas décider dans la spécification que les ressources existantes, les cohortes et la gestion d'expédition sont « prêtes » pour la pollinisation sans tests E2E.
- Un seul état atomique par ruche ne suffit pas à garantir une opération qui modifie **plusieurs ruches et un champ partagé**. Concevoir la stratégie de transaction multi-agrégats avant développement.

## Audit nécessaire AVANT la première PR d'implantation

- Cartographie exacte des IDs de bâtiment officiels (Nurserie, Académie, Génétique, Recherche, Hall, Entrepôt, Caserne, etc.).
- Contrats complets de l'API de collecte, de déploiement des troupes, de combat et des bonus alliance.
- Stock de base honey/pollen/wax, cadences et mécanismes anti-double crédit.
- Contrats d'identité joueur/ruche/alliance, adhésions, exclusions et rôles.
- Gestion du temps hors ligne, cache Unity, mises à jour push/refresh et migration PlayerHiveState.
- Statut de l'Académie et des cohortes sur le serveur effectivement utilisé.
- Tests existants à conserver pour carte/ressources/Nurserie/Académie/Alliance/combat.


## COMPLEMENT AUDIT AGENT CODEUR — découvertes complémentaires 2026-10-20

Un audit supplémentaire des sources a confirmé la présence de **FlowerManager.cs, FlowerPatch.cs, FlowerSpecies.cs, PollinationRules.cs, EcologicalBalance.cs, RegionalEcologyPressureFramework.cs** sous Assets/BeeKingdom/World. Ce code avance des fleurs localement au DeltaSeconds : ne pas le traiter comme état multi-joueurs officiel. Voir 21 et 41.

Le record AllianceEntity examiné dans Server/src/BeeKingdom.Alliance/Models/AllianceModels.cs ne contient **pas de champ Level/Experience explicite** ; les recherches d'alliance sont des objets distincts. Une progression d'alliance réelle est donc à spécifier. Voir 29.

AllianceDiplomacy.cs fournit déjà les types Neutral, NonAggressionPact, Ally, Hostile et War : introduire un Pacte de Floraison de façon compatible au lieu de créer une deuxième diplomatie.

WorldMapMarchRegistry.cs est une projection locale de marches Combat/Collection et limite ses marches actives à trois ; cela ne doit pas imposer une limite de missions civiles ni être utilisé comme autorité serveur.

NurseryCohortService.StartAsync inspecté initialise une cohorte sous conditions de nutrition/stabilité/capacité, **sans débit visible de honey/pollen/wax dans ce fragment**. Les coûts des cohortes civiles restent une règle future à approuver. Voir 21, 23, 28.
