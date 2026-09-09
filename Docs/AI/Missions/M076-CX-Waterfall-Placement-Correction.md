# M076 — Placement des chutes

## Cause
Les rectangles secondaires utilisaient des coordonnées sans l'origine runtime `(3584,3584)` et étiraient le même maillage à deux bras sur toutes les chutes. Le catalogue JSON n'était pas consommé ; les affirmations de placement du jalon M075 sont remplacées par cette validation.

## Fichiers et réalisation
`WorldMapWaterfallFxBootstrap.cs`, `WorldMapMmoFullscreenFoundationBootstrap.cs`, `Resources/WaterfallFX/WaterfallCatalog.json` (+ meta) et `WaterfallMapSurface.shader`. Le catalogue contient maintenant 12 emplacements mesurés, avec contours propres, rectangle, échelle, rotation, flux, opacité, animation, feathering et résolution ; le composant commun assure captures isolées, matériaux, projection OnGUI et désactivation des caméras hors écran.

La référence conserve ses 169 sommets, 264 triangles, textures, opacité 0,92 et RT 640 × 429. Les anciens prefabs de démonstration sont désactivés uniquement en Play Mode ; aucune image de terrain ni scène n'a été modifiée dans cette correction.

## Transfert
Le catalogue est un asset Resources indépendant de la scène. Le renderer le charge automatiquement et le host appelle `DrawOverlay(worldCenter, zoom, viewport)` après le terrain ; aucun placement individuel n'est à refaire pour réutiliser cette carte en production. `worldOffset` déplace l'ensemble si l'origine du host change.

## Vérifications
Compilation Unity et shader sans erreur, démarrage Play Mode et inspection des 12 emplacements, dont les 7 zones entourées par l'utilisateur. Comparaison GPU de la référence avec le shader et le maillage approuvés, à temps figé et projection identique : **0 pixel différent sur 274 560** ; animation enregistrée sur 60 images. Déplacement/zoom 0,85 et 1,25 vérifiés sur la chute ouest ; chargement automatique des 12 entrées vérifié en désactivant temporairement le composant de la scène.

Preuves : [placements Play Mode](M076-CX-Waterfalls/placements-playmode.jpg) et [vidéo de la chute ouest](M076-CX-Waterfalls/west-playmode.mp4). Aucun test général ni changement de gameplay.

## Points non résolus
None.

## Acceptation
- [x] Positions et contours corrigés sur les chutes visibles recensées.
- [x] Référence visuelle conservée.
- [x] Configuration réutilisable effectivement chargée.
- [x] Compilation et validation visuelle Play Mode.
- [x] Commit local, sans push ; changements concurrents exclus.
