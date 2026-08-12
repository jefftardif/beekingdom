# WORLD_MAP_ARCHITECTURE.md

Version : 1.0
Statut : Livre (Sprint-028 — Fondation de la Carte du Monde, EPIC-07 The Living World)
Auteur : Studio Director

---

## Vue d'ensemble

La carte du monde est un espace de jeu **infini et découpé en chunks** (`WorldChunk`),
chargé et déchargé dynamiquement autour du focus du joueur (`WorldStreamer`). Tous les
systèmes vivent sous `Assets/BeeKingdom/WorldMap/` (assembly `BeeKingdom.WorldMap`),
avec **zéro couplage** vers les systèmes existants : l'intégration UI, caméra, gameplay
et rendu se fera par des adaptateurs dédiés dans les sprints suivants.

Rappel de conception (voir `Docs/World/WORLD_DESIGN_BIBLE.md`) : la carte EST le jeu —
elle n'est pas un écran intermédiaire. Le monde doit faire vivre la ruche.

## Systèmes livrés (15)

| Système | Fichier | Rôle |
|---|---|---|
| `WorldCoordinateSystem` | Core/WorldCoordinates.cs | Maths du monde infini : positions 64 bits, chunks/tuiles 32 bits, bornes encodables vérifiées |
| `WorldGrid` | Core/WorldGrid.cs | Répertoire des chunks vivants + registre des objets du monde |
| `WorldChunk` | Core/WorldChunk.cs | Conteneur de tuiles (clé `TileCoordinate`) et d'objets avec états |
| `WorldChunkLoader` | Streaming/WorldChunkLoader.cs | Chargement/déchargement borné (gates de concurrence), déduplication, annulation |
| `WorldStreamer` | Streaming/WorldStreamer.cs | Boucle tick : calcul de la fenêtre de chargement, ordre (centre → bord), unload le plus loin d'abord |
| `WorldObjectPool` | Objects/WorldObjectPool.cs | Pool de vues rendues par clé (prefab/type), capacité max, warmup |
| `WorldSelection` | Selection/WorldSelection.cs | Sélection générique par `WorldObjectId`, mono/multi |
| `WorldLodManager` | Lod/WorldLodManager.cs | Niveaux de détail par distance (réglable) |
| `WorldSaveStore` | Save/WorldSaveStore.cs | Persistance chunk/objet (mémoire pour l'instant) |
| `WorldCameraController` | Camera/WorldCameraController.cs | Déplacement/zoom de la caméra orthographique |
| `WorldInputProcessor` | Input/WorldInputProcessor.cs | Regroupement gestes pointeur → clic/appui/pan/zoom |
| `WorldDebugOverlay` | Runtime/WorldDebugOverlay.cs | Overlay de diagnostic (touches, état streaming) |
| `WorldManager` | Runtime/WorldManager.cs | Compose le tout (MonoBehaviour de la scène démo) |
| `IWorldChunkContentSource` | Streaming/IWorldChunkContentSource.cs | Contrat de la source de contenu (réseau/terrain/scripted) |
| `NeutralTerrainContentSource` | Runtime/NeutralTerrainContentSource.cs | Source de démonstration neutre |

## Contrat de streaming (important)

- `LoadChunkAsync` / `UnloadChunkAsync` sont **synchrones vis-à-vis de la source** :
  `IWorldChunkContentSource.LoadAsync` est appelée **avant le retour de l'appel** (le
  corps s'exécute inline jusqu'à sa première suspension réelle). Les tests s'appuient
  sur ce contrat (vérification des demandes juste après `Tick`, complétion scriptée
  juste après `Tick`).
- Les tâches en vol sont enregistrées dans `inFlightLoads` / `inFlightUnloads`
  **avant** toute exécution asynchrone possible du corps (Add et Remove dans le même
  flux synchrone, `RunTrackedLoadAsync`/`RunTrackedUnloadAsync`). Cela élimine :
  - la race de complétion synchrone (une entrée complétée qui coincerait `DrainAsync`),
  - le non-déterminisme introduit par un `Task.Yield()` préalable.
- Les dictionnaires en vol sont protégés par un verrou court (`sync`) : les tâches
  reprennent sur le pool (continuations asynchrones) pendant que `DrainAsync` en
  énumère les valeurs. Le verrou ne contient jamais d'`await`.
- `DrainAsync` boucle tant qu'il reste des opérations en vol : il snapshot le
  contenu sous verrou, attend la complétion, re-vérifie. La borne de concurrence
  réelle (`StreamingSettings.MaxConcurrentLoads/Unloads`) s'applique au corps
  (`loadGate`/`unloadGate`), pas aux demandes.

## Conventions d'extension

- **Source de contenu** : implémenter `IWorldChunkContentSource` (terrain, réseau,
  sauvegarde). `LoadAsync` retourne un `WorldChunkContent` (objets + tuiles) ; le
  loader enregistre les objets du chunk dans la grille et pose les tuiles.
- **Focus** : implémenter `IWorldFocusProvider` (position du joueur/caméra) ; le
  streamer ne lit que ce contrat.
- **Vue rendue** : implémenter `IWorldObjectView` et l'enregistrer dans le pool par
  clé de prefab. Le pool ne connaît que cette abstraction.
- **Sélection** : écouter `WorldSelection.SelectionChanged` (adds/removes) —
  jamais de couplage avec un type concret.
- **Tests** : les fixtures vivent dans `Assets/BeeKingdom/Tests/Editor/WorldMap/`
  et utilisent `ScriptedContentSource` (complétion par signal déterministe).
  La configuration de test utilise des gates larges (256) : la borne de concurrence
  est un détail d'exécution, pas un comportement à figer dans les tests fonctionnels.

## Validation

- **81 tests unitaires verts** (10 fixtures, EditMode) :
  - WorldCoordinateSystemTests 24, WorldChunkTests 5, WorldGridTests 8,
    WorldStreamerTests 10, WorldObjectPoolTests 8, WorldSelectionTests 4,
    WorldLodTests 4, WorldCameraMathTests 8, WorldSaveTests 4,
    WorldInputProcessorTests 6.
- Bugs réels corrigés durant la validation :
  - `WorldChunkLoader` : race de complétion synchrone → tâche complétée coincée
    dans `inFlightLoads` → `DrainAsync` bouclait sans fin (détecté par les tests
    de streaming).
  - `WorldObjectPool.Rent` : `outstanding[key]++` levait `KeyNotFoundException`
    au premier rent.
  - `WorldObjectPool` : le compteur `Created` comptait aussi les réutilisations.
  - `WorldSelection.Raise` : `NullReferenceException` quand la sélection était
    vide en mode mono-sélection.
- Scène démo : `Assets/Scenes/WorldMapFoundation.unity` (générée par
  `BeeKingdom.Editor.WorldMapSceneBuilder`, menu `BeeKingdom/World Map/Build
  Foundation Scene`).

## Outillage de test

- CLI `-runTests` inutilisable sur ce projet (TestJobRunner se bloque sans fin
  sur certains filtres) → utiliser :
  `Unity.exe -batchmode -projectPath <projet> -executeMethod
  BeeKingdom.Editor.WorldMapTestRunner.Execute -worldmapTests <classe1> <classe2> ...`
  Le runner enchaîne les classes séquentiellement et quitte avec le code 0 (succès)
  ou 2 (échecs). Ne pas passer `-quit` (sortie immédiate sans exécuter).
- Attention : un timeout shell pendant un batch laisse des processus Unity
  orphelins qui verrouillent le projet (symptôme : « je n'arrive plus à
  compiler »). Toujours surveiller les runs et tuer les résidus
  (`Get-Process -Name Unity`).

## Prochaines étapes

1. Source de contenu réelle (terrain procédural / réseau) branchée sur
   `NeutralTerrainContentSource`.
2. Adaptateurs UI (panneau de sélection) et rendu (vues `IWorldObjectView`).
3. Persistance réelle du `WorldSaveStore` (sauvegarde de serveur).
4. LOD jouable + optimisations de rendu.
