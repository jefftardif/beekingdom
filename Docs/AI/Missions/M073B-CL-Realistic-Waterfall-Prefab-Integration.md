# M073B-CL — Intégration Realistic Waterfall Prefab dans la World Map

Date : 2026-09-09
Scène modifiée : `Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity`

## Résumé

Intégration d'une première chute d'eau 3D animée (asset acheté `Assets/Tazo_fx`,
Realistic Waterfall Prefab v1.6) dans la World Map, positionnée précisément sur
la chute déjà peinte dans le fond de carte (tuiles R02C20/R02C21). `Assets/Tazo_fx`
n'a pas été modifié — tout ce qui devait être adapté a été copié dans
`Assets/BeeKingdom`.

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
  structure de matériaux, donc le correctif de shader ci-dessous s'applique
  identiquement si on bascule dessus).

Trois instances de `sold3_waterfall_high` sont placées côte à côte
(-1.7 / 0 / +1.7 unités locales) pour couvrir la largeur de la chute peinte,
qui est nettement plus large qu'un seul flux de l'asset.

## Problème trouvé et corrigé : matériaux roses (Legacy vs URP)

Tous les matériaux du pack (`fulid_01`, `fulid_alpha_01`, `fog_1`, `fog_2`,
`splash_1`, `splash_2`, `Caustics_1`, `foam_1`) utilisent
`Legacy Shaders/Particles/Alpha Blended` ou `Legacy Shaders/Particles/Additive`
— des shaders du **Built-in Render Pipeline**, incompatibles avec **URP**
(le pipeline de ce projet). Sans correctif, la chute se serait affichée
**entièrement rose/magenta** en jeu.

Correctif : deux nouveaux shaders URP unlit, fidèles au shader legacy d'origine
(même combinaison `texture * tint * couleur-sommet * 2`, même blend mode) :
- `Assets/BeeKingdom/Playground/Shaders/TazoWaterfallAlphaBlendedURP.shader`
- `Assets/BeeKingdom/Playground/Shaders/TazoWaterfallAdditiveURP.shader`

Et 8 matériaux copiés (mêmes textures/teintes que l'original, juste le shader
change) dans `Assets/BeeKingdom/Playground/Resources/WaterfallFX/Materials/`
(`*_urp.mat`), assignés à la place des matériaux Tazo_fx d'origine sur
l'instance en scène. `Assets/Tazo_fx` reste intact.

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

## Localisation de la chute peinte

Repérée par script (pas de Play Mode disponible dans cette session — voir
limitation ci-dessous) : scan des 2500 tuiles de
`WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_wave5method_12288_preview`
par lecture GPU (blit + ReadPixels, ne touche jamais aux réglages d'import des
textures terrain), classement par "fraction de surface écume" (luminosité
élevée + dominance bleue). Résultat net et sans ambiguïté : tuiles **R02C20**
(58 % de la surface) et **R02C21** (51 %), très loin devant le reste de la
carte (troisième position à 48 %, sur une tuile non adjacente). Rivière calme
confirmée juste au-dessus (R01C20/21), bassin calme juste en dessous
(R03C20/21).

World rect résultant (système de coordonnées de
`WorldMapWave6StreamingTileProvider`, TileSize=512, OriginChunk=(7,7)) :
`(13824, 4608, 1024, 512)`, stocké comme constante
`WorldMapWaterfallFxBootstrap.WorldRect`.

## Assets BeeKingdom créés

- `Assets/BeeKingdom/Playground/Shaders/TazoWaterfallAlphaBlendedURP.shader`
- `Assets/BeeKingdom/Playground/Shaders/TazoWaterfallAdditiveURP.shader`
- `Assets/BeeKingdom/Playground/Resources/WaterfallFX/Materials/*_urp.mat` (8
  fichiers)
- `Assets/BeeKingdom/Playground/WorldMapWaterfallFxBootstrap.cs`
- Modification de `Assets/BeeKingdom/Playground/
  WorldMapMmoFullscreenFoundationBootstrap.cs` (ajout, pas de réécriture du
  chemin de dessin du terrain existant) : champ `waterfallFx`, méthode
  `DrawWaterfallFxOverlay()`, un appel dans `OnGUI()`.
- Scène : GameObject racine `WaterfallFX` (3 instances de
  `sold3_waterfall_high`, remappées sur les matériaux URP + composant
  `WorldMapWaterfallFxBootstrap` qui crée sa propre caméra de rendu).

## Vérifications faites avant de terminer

- Aucune erreur de compilation introduite (shaders + script C#, vérifié via
  la Console Unity après chaque étape).
- Aucun matériau rose : cause identifiée (shader Legacy vs URP) et corrigée à
  la racine plutôt que contournée.
- Aucune régression du chemin de dessin du terrain existant : le rendu du
  terrain (`DrawWave6WorldTerrain`) n'a pas été modifié cette fois, seul un
  nouvel appel de dessin (avec repli silencieux si absent) a été ajouté juste
  après.
- Scène sauvegardée proprement (`IsDirty: false` après sauvegarde).

## Limitation importante — non vérifié visuellement

**Cette session n'a pas eu accès au Play Mode ni aux captures d'écran du Game
View** (outils MCP correspondants indisponibles ce soir). Tout ce qui précède
a été vérifié par la donnée (références de composants, shaders, compilation)
mais **le rendu final n'a pas pu être vu**. Deux choses en particulier
mériteront très probablement un ajustement visuel de ta part en Play Mode :
- l'échelle/l'espacement des 3 instances de chute et la distance de la caméra
  de rendu (valeurs choisies par calcul à partir de la scène de démo du
  vendeur, jamais vérifiées à l'écran) ;
- l'alignement fin du rectangle de composition avec le bord exact de la
  falaise peinte (la position a été déduite par analyse de pixels, précise à
  l'échelle de la tuile, mais pas ajustée au pixel près).

C'est un premier passage d'intégration technique complet et fonctionnel, pas
un résultat déjà peaufiné visuellement.

## Prochain test utilisateur

Ouvrir `Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity`, Play Mode,
naviguer vers la chute existante (secteur nord de la carte, tuiles
R02C20-C21). Confirmer que la chute animée apparaît par-dessus le fond peint,
suit bien le pan/zoom, et juger si l'échelle/le cadrage doivent être corrigés.
