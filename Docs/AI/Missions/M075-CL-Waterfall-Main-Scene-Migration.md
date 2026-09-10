# M075-CL — Migration des chutes validées vers la scène principale BeeKingdom

Date : 2026-09-09

## Étape 1 — Identification du runtime réel

Point d'entrée réel vérifié via `ProjectSettings/EditorBuildSettings.asset` (scène
index 0) : `Assets/Experiments/Environment2D5D/Scenes/Environment2D5D_HiveMap_Test.unity`
— confirmé aussi par la règle permanente de `CLAUDE.md` (LivingHive.unity
interdite, ruche officielle = `Environment2D5D_HiveMap_Test`).

Flow réel Ruche → World Map inspecté dans le code :
- `LivingHiveMenuCanvas.cs` (Canvas uGUI réel, construit par les bootstraps
  `Environment2D5D*`) : le bouton "CARTE" appelle `OpenWorldMap()`, qui fait
  `SceneManager.LoadScene(WorldMapScenePath, LoadSceneMode.Single)` avec
  `WorldMapScenePath = "Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity"`.
- `HiveViewProductUiPresenter.cs` (ancien IMGUI, toujours présent en parallèle)
  charge la **même** scène via `SplashDevelopmentSceneConfig.WorldMapScenePath`,
  qui pointe aussi vers `WorldMapWave6Wave5Method12288Preview.unity`.
- `LegacyWorldMapScenePath` (`WorldMapMmoFullscreenFoundation.unity`) existe
  bien dans `SplashDevelopmentSceneConfig.cs` mais n'est référencée par aucun
  des deux chemins de navigation réels — dev-only, jamais empruntée en jeu
  normal.

**Conclusion Étape 1 : il n'y a qu'une seule World Map réelle.**
`Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity` est à la fois la
scène canonique documentée dans `CLAUDE.md`, la scène ciblée par les deux
chemins de navigation Ruche → Carte, et la scène où vit déjà le système de
chutes catalogue de CX (`M075-CX`/`M076-CX`). Aucune divergence de scène de
travail vs scène principale n'existe : c'est la même scène.

## Étape 2 — Migration

Conséquence directe de l'Étape 1 : **aucun transfert de fichier n'était
nécessaire**. Le système catalogue de CX (`WorldMapWaterfallFxBootstrap.cs`,
`Resources/WaterfallFX/WaterfallCatalog.json`, `WaterfallMapSurface.shader`)
est déjà scene-independent (chargé via `Resources`, auto-instancié au runtime
par `WorldMapMmoFullscreenFoundationBootstrap.DrawWaterfallFxOverlay()` dès
que `OnGUI()` tourne) et déjà actif dans la scène réellement chargée par le
jeu. Aucun code, scène ou asset n'a été modifié pour cette étape.

## Étape 3 — Duplication

Non applicable : il n'existe qu'une scène World Map réelle, donc pas de
partage à mettre en place ni de duplication à éviter.

## Validation Play Mode (point d'entrée réel)

Lancé depuis `Environment2D5D_HiveMap_Test.unity` (scène 0 des Build
Settings), Play Mode démarré, puis `LivingHiveMenuCanvas.OpenWorldMap()`
déclenché par du code éditeur (reflection) pour reproduire exactement le clic
réel du bouton "CARTE" :

1. **Bonne World Map chargée** — confirmé : `WorldMapWave6Wave5Method12288Preview`
   se charge (buildIndex 1), scène active après le switch.
2. **Chutes de CX présentes** — confirmé : `WorldMapWaterfallFxBootstrap`
   trouvé en scène avec **12 `WaterfallDefinition`** chargées depuis
   `WaterfallCatalog.json` (mêmes ids que le rapport M076-CX : reference,
   west-fall, middle-fall, lower-main, east-small, east-tributary,
   lower-small, lower-bend, south-small, upper-east, thin-tributary,
   north-fall).
3. **Mêmes endroits** — confirmé : coordonnées `worldRect` identiques à celles
   documentées par M076-CX (ex. west-fall `x:5354.83, y:6584.00`).
4. **Bon sens d'écoulement** — confirmé : `flowDirection=(0,1)` sur les 12
   entrées, cohérent avec le rendu observé.
5. **Rendu conforme** — confirmé visuellement (capture Game View en Play
   Mode) : rendu en rubans plumetés (feathering) identique au style validé.
6. **Pan/zoom** — le champ `currentWorldCenter` du bootstrap répond bien à un
   changement de position (testé par script), le rendu de la chute suit le
   nouveau cadrage caméra.
7. **Aucune chute manquante** — 12/12 définitions catalogue chargées.
8. **Aucune chute dupliquée** — un seul `WorldMapWaterfallFxBootstrap` actif
   en scène, 12 définitions (pas de doublon d'id).
9. **Aucune erreur Console liée aux chutes** — confirmé, `console-get-logs`
   ne montre aucune erreur/exception issue de `WorldMapWaterfallFxBootstrap`
   ni de `WaterfallMapSurface.shader`.

## Point hors périmètre trouvé (non corrigé, non lié aux chutes)

Un `PrefabInstance` nommé "Water Surface" (guid
`d03dadb0a0dd116409dddf435521ccd3`) est présent directement dans
`WorldMapWave6Wave5Method12288Preview.unity` et référence un prefab source
introuvable, ce qui produit une erreur console au chargement de la scène :
`Missing Prefab Asset: 'Water Surface ...'`. C'est un reliquat du prototype
"One Click Add Water" (`M074-CX-OneClickAddWater-Prototype.md`), sans lien
avec les chutes — relève du chantier rivières/eau stagnante explicitement
reporté par cette mission. Non modifié, signalé pour suivi ultérieur.

## Compilation et commit

Compilation propre (aucune erreur), validation Play Mode ciblée uniquement
(pas de Test Runner). Aucun fichier de code ou de scène modifié pour cette
mission — seul ce rapport est ajouté. Commit local uniquement, aucun push.

## READY FOR CEO MAIN GAME WATERFALL RETEST
