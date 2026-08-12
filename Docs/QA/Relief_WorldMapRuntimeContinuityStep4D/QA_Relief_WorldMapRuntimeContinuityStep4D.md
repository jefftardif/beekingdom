# QA de relève - World Map Runtime Continuity Step4D

Date: 2026-07-14  
Statut révisé: **BLOCKED**  
Rôle: QA de relève indépendant  
Méthode: mise à jour documentaire uniquement; Unity non lancé; produit non modifié.

## Décision de relève

Le constat direct du propriétaire invalide le verdict produit positif précédemment établi à partir des sept captures déterministes:

> En interaction réelle, le background terrain reste statique pendant que les ruches, ressources et vols se déplacent ou zooment.

Le terrain et les entités ne partagent donc pas la même transformation perceptuelle du monde. Une carte dont les entités répondent au pan/zoom tandis que le terrain reste fixé à l'écran ne satisfait pas la continuité runtime Step4D, même si des captures fixes prises dans des états déterministes paraissent montrer des cadrages différents.

```text
QA_RELIEF_WORLD_MAP_RUNTIME_CONTINUITY_STEP4D = BLOCKED
TERRAIN_AND_ENTITIES_SHARED_WORLD_TRANSFORM = FAIL
READY_FOR_WORLD_MAP_WAVE3_UNITY_INTEGRATION = NO
```

Le produit reste bloqué sur cette porte jusqu'à la correction Builder-A Step5A et sa contre-validation interactive.

## Défaut bloquant

| Surface | Comportement observé en interaction réelle | Transformation perceptuelle |
| --- | --- | --- |
| Background terrain | Reste statique pendant le pan/zoom | Fixée à l'écran ou découplée du monde interactif |
| Ruches | Se déplacent ou zooment | Réagit au pan/zoom monde |
| Ressources | Se déplacent ou zooment | Réagit au pan/zoom monde |
| Vols et arcs | Se déplacent ou zooment | Réagit au pan/zoom monde |

Conséquence: les positions relatives entre terrain, ruches, ressources et vols changent artificiellement pendant l'interaction. La perception d'un monde spatial unique est rompue. Le défaut porte sur la transformation commune elle-même; il n'est pas nécessaire d'observer une couture de tuile pour bloquer.

## Pourquoi la preuve fixe ne clôt pas Step4D

Les sept PNG et leurs manifestes restent intègres au niveau de l'archive, mais ils ne démontrent pas que toutes les couches suivent la même transformation pendant une interaction continue.

| Élément archivé | Fait encore valable | Ce qu'il ne prouve plus |
| --- | --- | --- |
| 14 SHA-256 | Les fichiers audités correspondent à `SHA256SUMS.txt` | Le couplage terrain/entités pendant le pan ou le zoom |
| 7 paires PNG/JSON | Les paires, résolutions et états déclarés sont cohérents | Une transformation monde partagée entre deux frames interactives |
| Zooms 0.85 / 1.10 / 1.35 | Les états déterministes ont été enregistrés | Que le terrain réagit au geste de zoom en même temps que les entités |
| Pan C32_32 -> C35_32 -> C36_32 | Les manifestes et libellés portent cette séquence | Que le background suit réellement le pan continu |
| Variations de pixels entre captures | Les images fixes ne sont pas identiques | L'origine de la variation ni la synchronisation des couches pendant l'interaction |

La précédente comparaison de pixels à coordonnées écran était insuffisante pour conclure à un terrain mobile: elle comparait des sorties finales distinctes, pas la trajectoire temporelle du terrain et des entités sous un même geste. Le constat interactif direct est plus probant pour ce critère et annule la conclusion positive.

```text
EXACT_SEVEN_STATE_PROOF = FAIL
STATIC_CAPTURE_SET_INTEGRITY = MATCH
STATIC_CAPTURE_SET_SUFFICIENT_FOR_SHARED_TRANSFORM = NO
```

## Limites, bordures et camouflage

L'absence apparente de grille, Repeat, bande, trou ou camouflage dans les images fixes ne compense pas le défaut de transformation. Ces contrôles visuels statiques restent secondaires tant que la couche terrain ne suit pas le même pan/zoom que les objets du monde.

```text
VISIBLE_TILE_OR_CHUNK_BOUNDARY = INCONCLUSIVE_IN_REAL_INTERACTION
TERRAIN_STATIC_WHILE_ENTITIES_MOVE = YES
PRODUCT_RUNTIME_CONTINUITY = FAIL
```

## Overlays et vols

Les ruches, ressources, marqueurs et arcs de vol sont visuellement séparés du terrain. Ce point devient précisément le symptôme bloquant: ces couches réagissent à la transformation interactive tandis que le background n'y réagit pas. Le caractère aérien des vols ne corrige pas leur désancrage perceptuel par rapport au terrain.

| Contrôle | Verdict révisé |
| --- | --- |
| Overlays séparés du terrain | OBSERVÉ |
| Vols représentés comme aériens | OBSERVÉ |
| Ancrage des vols au même monde visuel que le terrain | FAIL |
| Ancrage des ruches et ressources au terrain pendant pan/zoom | FAIL |

## Correction attendue en Step5A

Builder-A doit corriger le runtime afin que le terrain, les ruches, les ressources, les sélections et les vols utilisent une transformation monde perceptuellement commune.

Critères de sortie minimaux pour la nouvelle preuve:

1. Pendant un pan continu, un repère terrain identifiable et les entités qui lui sont associées conservent leurs positions relatives.
2. Pendant un zoom continu, terrain, ruches, ressources et arcs changent d'échelle autour du même pivot perceptuel.
3. Les extrémités des arcs de vol restent ancrées à leurs sources et destinations sur le terrain pendant pan et zoom.
4. Une preuve temporelle montre le mouvement simultané des couches; une simple série d'images finales n'est pas suffisante.
5. Après correction interactive, les états 0.85, 1.10, 1.35, le portrait 720x1280 à 1.10 et le pan C32_32 -> C35_32 -> C36_32 sont recapturés et revérifiés.
6. Les contrôles Clamp, UV bornés et 25 chunks sont rejoués sans masquer le défaut de transformation partagée.

Cette relève n'implémente pas Step5A et ne modifie aucun fichier Unity.

## Dette d'outil

Le défaut connu de la file automatique reste une dette P2. Il n'est plus le motif principal de réserve: le blocage actuel provient du comportement produit directement observé. Une preuve manuelle exacte ne peut pas déroger à ce défaut runtime.

## Non-claims

- `master_5x5_integrated=false`; aucune intégration 5x5 revendiquée.
- `server_live=false`; aucune activité live revendiquée.
- Aucun test sur appareil physique revendiqué.
- Aucun lancement Unity dans cette relève.
- Aucune correction produit réalisée par QA.

## Chemins de preuve

- Rapport QA révisé: `C:\projets\beekingdomgame-master\Docs\QA\Relief_WorldMapRuntimeContinuityStep4D\QA_Relief_WorldMapRuntimeContinuityStep4D.md`
- Archive statique désormais insuffisante pour le critère interactif: `C:\projets\beekingdomgame-master\Docs\QA\Architect_WorldMapRuntimeContinuityStep4D_DirectProof`
- Rapport Architecte antérieur: `C:\projets\beekingdomgame-master\Docs\QA\Architect_WorldMapRuntimeContinuityStep4D_DirectProof\Architect_WorldMapRuntimeContinuityStep4D_DirectProof.md`
- Rapport Builder-A Step4D: `C:\projets\beekingdom\prompts_codex\rapports\BuilderA_WorldMapRuntimeContinuityStep4D_ProofControls_Report.md`
- Reçu canonique antérieur: `C:\projets\beekingdom\QA\ARCHITECT_WORLD_MAP_RUNTIME_CONTINUITY_STEP4D_DIRECT_PROOF.md`

## Matrice finale révisée

```text
QA_RELIEF_WORLD_MAP_RUNTIME_CONTINUITY_STEP4D = BLOCKED
TERRAIN_AND_ENTITIES_SHARED_WORLD_TRANSFORM = FAIL
TERRAIN_STATIC_WHILE_ENTITIES_MOVE_OR_ZOOM = YES
EXACT_SEVEN_STATE_PROOF = FAIL
STATIC_CAPTURE_SET_SUFFICIENT_FOR_PRODUCT_PASS = NO
VISIBLE_TILE_OR_CHUNK_BOUNDARY = INCONCLUSIVE_IN_REAL_INTERACTION
OVERLAYS_WORLD_ANCHORING = FAIL
FLIGHT_ARCS_WORLD_ANCHORING = FAIL
PRODUCT_RUNTIME_CONTINUITY = FAIL
AUTOMATIC_CAPTURE_QUEUE_DEBT = P2_NON_PRIMARY
BUILDER_A_STEP5A_FIX_REQUIRED = YES
MASTER_5X5_INTEGRATED = NO
SERVER_LIVE = NO
PHYSICAL_DEVICE_PROOF = NO
PRODUCT_BLOCK = YES
READY_FOR_WORLD_MAP_WAVE3_UNITY_INTEGRATION = NO
```
