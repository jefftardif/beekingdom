# M074-CX — Prototype One Click Add Water sur World Map

Date : 2026-09-09
Scène modifiée : `Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity`
Chutes (`WorldMapWaterfallFxBootstrap`) : non touchées, comme demandé.

## Inspection de l'asset

`Assets/Houidisoft technology/One Click Add Water -Stylized Water Shader` :
un outil de menu (`Tools/Add Water Surface`) qui pose un `Plane` avec un
matériau utilisant `Shader Graphs/water shader ocean` (Shader Graph, 32
propriétés exposées : smoothness, metallic, darkness, reflect/refraction
power, normal strength x2 textures, foam texture/scale/speed/intensity/
amount, wave length/height/peak sharpness/speed, water movement speed). Une
scène de démo est fournie. Un second asset (`ESSW Easy Setup Stylized Water
2.0`) était déjà présent dans le projet et partage la même famille de
techniques (planar reflection, foam sub-graphs).

## Prototype réalisé

Une seule zone : le bassin/rivière calme juste au-dessus de la chute (tile
R04C07, world rect `7300,5800,760,340` — plafonné pour ne pas chevaucher
`WorldMapWaterfallFxBootstrap.WorldRect`). Réutilise le pattern caméra
dédiée + RenderTexture + composition `OnGUI` déjà validé pour les chutes,
mais dans un composant totalement séparé (`WorldMapOneClickWaterPrototype.cs`)
et un layer distinct (`WaterSurfaceFX`, index 11).

## Résultat : négatif, deux limitations trouvées

**1. Aucun contrôle de direction de courant.** Le shader n'expose qu'une
vitesse scalaire (`_water_movement_speed`) pour faire défiler la normal map,
aucun vecteur de direction. L'axe de défilement est câblé en dur dans le
Shader Graph. Impossible de faire suivre un courant à la courbe réelle de la
rivière peinte sans éditer le graphe — non tenté, conformément à la consigne
de ne pas construire un système maison sans validation CEO.

**2. Plus grave : rendu plat et uniforme, quel que soit le réglage.** Capturé
via la caméra offscreen isolée (obligatoire vu l'architecture `OnGUI` de la
World Map), la surface d'eau sort en blanc/gris **parfaitement uniforme sur
toute sa surface** — aucune vague, aucune écume, aucun reflet, vérifié en
lisant des dizaines de pixels de la RenderTexture aux quatre coins et au
centre. Écarté un par un : ce n'est pas une texture de foam manquante (assigné
une vraie `foam3.png`, aucun changement), pas le mot-clé `_ENABLE_WAVES`
désactivé (activé explicitement, aucun changement), pas Metallic/Smoothness
(les changer fait bien bouger la couleur plate de gris à blanc, donc le
matériau est bien évalué — juste jamais avec la moindre variation spatiale).
Un vrai échantillonnage de texture ne peut pas ressortir parfaitement
identique sur ~35 points répartis sur toute la surface : l'explication la
plus probable est un nœud de type Divide/gradient du graphe qui dépend de
Scene Depth et dégénère, faute de Depth/Opaque Texture (désactivées au niveau
du projet) et faute de toute autre géométrie 3D dans ce layer isolé pour
produire une profondeur réelle même si on les activait.

## Décision

Pas de généralisation, pas de système maison construit là-dessus — décision
CEO à prendre : adapter le Shader Graph en profondeur (hors scope d'un
prototype) ou abandonner cet asset pour l'eau horizontale de la World Map.

## Effet de bord utile

Le package `ESSW Easy Setup Stylized Water 2.0` (déjà présent) contenait une
erreur de compilation bloquant tout le projet (`Object.GetInstanceID()`
obsolète dans `PlanarReflections.cs`) — corrigée au minimum pour permettre le
Play Mode.

## Vérifications faites

Compilation propre, aucun matériau rose, aucune erreur console, testé en
Play Mode avec la vraie caméra de jeu, système de chutes intact et non
modifié.

## READY FOR CEO WATER SURFACE RETEST
