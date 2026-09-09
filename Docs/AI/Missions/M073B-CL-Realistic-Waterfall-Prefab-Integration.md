# M073B-CL — Intégration Realistic Waterfall Prefab dans la World Map

Date : 2026-09-09
Scène modifiée : `Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity`

## Résumé

Intégration d'une chute d'eau 3D animée (asset acheté `Assets/Tazo_fx`,
Realistic Waterfall Prefab v1.6) dans la World Map, positionnée précisément sur
la chute déjà peinte dans le fond de carte. `Assets/Tazo_fx` n'a pas été
modifié — tout ce qui devait être adapté a été copié dans `Assets/BeeKingdom`.

Cette mission a eu une phase 1 (intégration technique sans accès visuel) puis
une phase 2, ce soir, de débogage **en direct** avec accès `computer-use`
(contrôle d'écran réel de l'Éditeur Unity) : la position réelle de la chute
peinte a été corrigée (l'estimation par scan initial visait la mauvaise
chute), puis trois bugs de rendu distincts ont été identifiés et corrigés,
faisant passer le résultat d'un rectangle gris plat, presque invisible,
à une chute animée avec un vrai motif d'eau bleu-vert visible et un
recouvrement correct de la largeur de la falaise.

## Prefab / technique retenue

**`sold3_waterfall_high.prefab`** (variante "Standard Mesh", qualité haute) —
maillage animé (UV scrolling) pour le flux principal + particules pour
l'écume/le mist/les caustiques. Choisi plutôt que :
- `alpha_waterfall` (style particules pur) : moins net pour une lecture RTS à
  distance ;
- `sold_waterfall_refraction` : nécessite une passe de grab/refraction typique
  du Built-in Render Pipeline, à éviter en URP (voir contrainte #8 de la
  mission) ;
- `sold1_waterfall_low` / `sold2_waterfall_mid` : gardées en réserve comme
  option d'allègement mobile si `high` s'avère trop coûteux à l'usage (même
  structure de matériaux, donc les correctifs de shader ci-dessous
  s'appliquent identiquement si on bascule dessus).

Trois instances de `sold3_waterfall_high` sont placées côte à côte
(-1.7 / 0 / +1.7 unités locales) pour couvrir la largeur de la chute peinte,
qui est nettement plus large qu'un seul flux de l'asset.

## Localisation de la chute peinte — corrigée en session live

Le premier scan (fait sans Play Mode disponible) avait identifié les tuiles
R02C20/R02C21 comme la chute avec le score "écume" le plus élevé sur
l'ensemble de la carte 50×50 — une vraie chute, mais **pas** celle du secteur
où le CEO joue réellement. En comparant en direct la position `worldCenter`
réelle du CEO (relevée par `Debug.Log`) pendant qu'il se trouvait debout sur
la chute, un second scan restreint au secteur observé a trouvé les tuiles
**R05C07/R05C08** — un match quasi exact. `WorldMapWaterfallFxBootstrap.WorldRect`
est maintenant `(7168, 6144, 1024, 512)` (au lieu de `(13824, 4608, 1024, 512)`
dans la version précédente de ce rapport).

## Trois bugs de rendu trouvés et corrigés en débogage live

Avec l'accès `computer-use`, la chute est apparue en Play Mode comme un
**quadrilatère gris plat, presque sans détail de texture**, bien positionné
mais visuellement inconvaincant. Trois causes distinctes, empilées, ont été
diagnostiquées par inspection directe (Hierarchy/Inspector, lecture de pixels
de la RenderTexture via script) puis corrigées :

**1. Matériaux roses (Legacy vs URP)** — Tous les matériaux du pack
(`fulid_01`, `fulid_alpha_01`, `fog_1`, `fog_2`, `splash_1`, `splash_2`,
`Caustics_1`, `foam_1`) utilisent `Legacy Shaders/Particles/Alpha Blended` ou
`.../Additive`, des shaders Built-in Render Pipeline incompatibles avec URP.
Corrigé par 4 nouveaux shaders URP unlit (2 variantes "mesh", sans lecture de
couleur de sommet, + 2 variantes "particule") dans
`Assets/BeeKingdom/Playground/Shaders/`, et 8 matériaux copiés dans
`Assets/BeeKingdom/Playground/Resources/WaterfallFX/Materials/` (`*_urp.mat`).

**2. Cadrage caméra : la chute ne remplissait que ~5 % de la RenderTexture** —
La caméra de rendu dédiée utilisait une position locale fixe
`(0, 0.05, -distance)`, qui supposait que le pivot du maillage était à son
centre vertical. En réalité le pivot de `sold3_waterfall_high` est à la
**base** de la chute (le maillage s'étend de Y≈0 à Y≈2.84) : la caméra visait
donc juste la base, laissant ~95 % du maillage hors du champ. Confirmé par
lecture directe des pixels de la RenderTexture (script `ReadPixels` +
comptage de pixels non-transparents) : 4,9 % de couverture, dans une petite
zone décentrée. Corrigé en calculant le cadrage à partir des `Bounds`
combinés des **`MeshRenderer`** (pas les `ParticleSystemRenderer`, qui
donnent des bounds dégénérés avant leur première simulation) sous
`waterfallRoot`, dans `WorldMapWaterfallFxBootstrap.EnsureRenderCamera()` —
la caméra se centre maintenant automatiquement sur la géométrie réelle,
quel que soit le nombre d'instances placées dans `WaterfallFX`.

**3. Le vrai coupable du "gris plat" : shader additif sans masque alpha** —
Une fois le cadrage corrigé, la chute restait presque uniformément grise/
blanche, sans motif d'eau visible, alors que la texture `t1.png` (matériau
`fulid_01`, sheet d'eau principal) est bien une texture colorée bleu-vert
marbrée (vérifié en l'ouvrant directement). Diagnostic par isolement successif
des sous-maillages : `waterfall_meash` seul montrait déjà la bonne couleur ;
c'est la superposition de `waterfall_meash_3` (matériau `caustics_1`, texture
`highlight_1.png`) qui noyait tout en blanc. Cause : `highlight_1.png` est
une image quasiment blanc pur en RGB, avec la forme réelle du motif de
caustiques encodée dans le **canal alpha** (convention standard pour les
sprites de glow additifs) — mais le shader additif écrit ne multipliait pas
`tex.rgb` par `tex.a`, donc il ajoutait un blanc plein sur **toute** la
surface du maillage au lieu de suivre la forme du motif. Corrigé dans
`TazoWaterfallMeshAdditiveURP.shader` et `TazoWaterfallAdditiveURP.shader`
(`rgb = tex.rgb * tex.a * _TintColor.rgb * _TintColor.a * 2.0`).

Un quatrième point mineur avait été corrigé plus tôt dans la session :
compositing alpha "au carré" en superposant plusieurs couches semi-
transparentes sur la RenderTexture (déjà à alpha=0) avec un seul facteur de
blend — corrigé par un blend en deux parties
(`Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha`) dans les shaders
alpha-blended.

## Problème d'architecture trouvé et résolu : le terrain masque toute la scène 3D

La World Map ne contient **aucune géométrie 3D** — le fond peint est dessiné
chaque frame via `OnGUI` (`WorldMapMmoFullscreenFoundationBootstrap.
DrawWave6WorldTerrain`, un blit plein écran des tuiles peintes). Or `OnGUI`
se dessine systématiquement **par-dessus** tout ce que la Main Camera a rendu
en 3D dans la même frame (comportement fixe du moteur Unity, IMGUI en
overlay). Un prefab 3D placé normalement dans la scène aurait donc été
**invisible en Play Mode**, caché sous le fond de carte à chaque frame — un
problème d'architecture qui n'était pas anticipé dans l'énoncé de la mission.

Solution mise en place :
1. La chute et sa caméra dédiée vivent sur un layer isolé (`WaterfallFX`,
   index 10, ajouté au projet).
2. Une caméra dédiée (`WorldMapWaterfallFxBootstrap`, nouveau composant sur le
   GameObject `WaterfallFX` de la scène) ne voit que ce layer et rend dans une
   `RenderTexture` hors écran (créée au runtime dans `Awake()`, pas un asset
   fichier — se recrée proprement à chaque chargement de scène).
3. `WorldMapMmoFullscreenFoundationBootstrap.DrawWaterfallFxOverlay()` (appelé
   juste après le dessin du terrain dans `OnGUI`) compose cette texture au bon
   endroit écran via `GUI.DrawTexture`, en réutilisant exactement la même
   conversion monde→écran (`WorldRectToScreenRect`) que les tuiles de terrain
   — la chute reste donc verrouillée sur la carte pendant le pan/zoom, comme
   n'importe quel autre élément dessiné sur la carte.

## Assets BeeKingdom créés

- `Assets/BeeKingdom/Playground/Shaders/TazoWaterfallAlphaBlendedURP.shader`
- `Assets/BeeKingdom/Playground/Shaders/TazoWaterfallAdditiveURP.shader`
- `Assets/BeeKingdom/Playground/Shaders/TazoWaterfallMeshAlphaBlendedURP.shader`
- `Assets/BeeKingdom/Playground/Shaders/TazoWaterfallMeshAdditiveURP.shader`
- `Assets/BeeKingdom/Playground/Resources/WaterfallFX/Materials/*_urp.mat` (8
  fichiers)
- `Assets/BeeKingdom/Playground/WorldMapWaterfallFxBootstrap.cs`
- Modification de `Assets/BeeKingdom/Playground/
  WorldMapMmoFullscreenFoundationBootstrap.cs` (ajout, pas de réécriture du
  chemin de dessin du terrain existant) : champ `waterfallFx`, méthode
  `DrawWaterfallFxOverlay()`, un appel dans `OnGUI()`.
- Scène : GameObject racine `WaterfallFX` (3 instances de
  `sold3_waterfall_high` côte à côte, remappées sur les matériaux URP +
  composant `WorldMapWaterfallFxBootstrap` qui crée sa propre caméra de
  rendu, auto-cadrée sur les bounds réels des maillages).

## Vérifications faites avant de terminer

- Aucune erreur de compilation (shaders + script C#, vérifié en Console après
  chaque étape, y compris après les corrections de cadrage/shader de ce soir).
- Aucun matériau rose.
- Chute confirmée visible en Play Mode depuis la vraie caméra de jeu (pas
  seulement la Scene View), avec pan/zoom testé via téléportation de
  `currentWorldCenter`/`targetWorldCenter` — la chute suit correctement la
  carte.
- Motif d'eau réellement visible (bleu-vert marbré, distinct du fond peint),
  animation de flux confirmée (UV scrolling actif).
- Aucune régression du chemin de dessin du terrain existant.
- Scène sauvegardée proprement en Edit Mode (3 instances, positions
  `-1.7/0/+1.7`, toutes actives).

## Limitations restantes

- **Jonctions rivière → chute → bassin non retravaillées** (hors scope de
  cette mission, comme demandé) : les 3 segments juxtaposés montrent des
  coutures verticales visibles entre eux, et la transition avec l'écume
  peinte à la base n'est pas encore parfaitement fondue.
- **Cadrage encore approximatif** : la largeur des 3 instances couvre une
  bonne partie de la chute peinte mais n'a pas été ajustée au pixel près
  contre les bords exacts de la falaise ; un réglage fin de
  `cameraDistance`/espacement des instances reste possible si le CEO le juge
  nécessaire après un nouveau test.
- **Performance mobile non mesurée** : la caméra de rendu additionnelle (3
  instances de maillages + 4 systèmes de particules) n'a pas été profilée sur
  cible mobile ; à surveiller si d'autres effets 3D du même type s'ajoutent.
- Les variantes `sold1_waterfall_low` / `sold2_waterfall_mid` restent une
  option de repli si la variante `high` s'avère trop coûteuse en pratique.

## Prochain test utilisateur

Ouvrir `Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity`, Play Mode,
naviguer vers la chute (secteur autour de `worldCenter` ≈ (7680, 6400)).
Confirmer que le rendu convient à l'échelle RTS et juger si un ajustement fin
du cadrage ou de l'espacement des 3 instances est souhaité.
