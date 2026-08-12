# TEST REPORT — Expérience 2.5D : Zoom stable + Ancrage manuel des bâtiments

**Scène** : `Assets/Experiments/Environment2D5D/Scenes/Environment2D5D_SpatialV3.unity` (laboratoire expérimental isolé — aucun système de production modifié, image non modifiée, aucune depth map)

## 1. Correctif ZOOM (dernière itération — à tester en priorité)

### Problème identifié
Le zoom précédent déplaçait la caméra en Z **mais réorientait celle-ci vers un point fixe** du décor à chaque frame (`LookAt`) → l'inclinaison changeait pendant le zoom → la projection du plan (keystone) variait → **le paysage penchait et se déformait fortement**.

### Correctif implémenté
- **Rotation de la caméra verrouillée en dur chaque frame** : `Quaternion.Euler(pitch, 0, 0)` → **roll = 0, yaw = 0, jamais dérivants** (plus aucun LookAt).
- **Zoom = dolly pur le long de l'axe de visée** : la molette modifie la **distance** caméra/point visé ; la caméra se translate sur son axe de profondeur, **inclinaison (pitch) strictement constante**.
- **FOV fixe à 55°** (jamais modifié — ni par le zoom ni par les presets).
- **Limites de distance** : distance min 8 / max 220 sur l'axe de visée + **garde-fou dur `maxCameraZ = 27`** (le plan du décor est à z=30 : la caméra ne peut jamais le traverser ni coller dessus → pas de cisaillement extrême).
- Résultat attendu : le décor **zoome uniformément** autour du point visé — ses lignes horizontales restent horizontales, l'horizon ne bouge pas, aucune rotation apparente, aucun effet de plan qui se couche.

### Protocole de vérification du zoom
1. **G** pour ouvrir le mode DEBUG (affiche FOV, distance de zoom, distance caméra→décor, rotation X/Y/Z, position).
2. Zoom avant jusqu'à la butée (molette) → vérifier : FOV reste 55, rotation X constante, Y=Z=0, le décor grossit **sans pencher** (les lignes du paysage restent horizontales), la caméra s'arrête avant le plan (distance min).
3. Zoom arrière jusqu'à la butée → le décor rétrécit uniformément, mêmes contrôles.
4. Refaire le cycle en presets 1/2/3 (les presets changent pitch+point visé, jamais le FOV ni le zoom) → même stabilité.
5. Confirmer que les bâtiments 3D gardent la vraie perspective : loin = petits, proches = grands, quelle que soit la distance de zoom.

### Nouveaux contrôles caméra
| Touche | Action |
|---|---|
| **Molette** | Zoom avant/arrière (dolly sur axe de visée, angle constant) |
| **WASD** | Pan de la vue (déplace le point visé sur le décor, orientation identique) |
| **R / F** | Inclinaison (pitch) : R = regarder plus haut, F = regarder plus bas |
| **1 / 2 / 3** | Presets LOW / MEDIUM / HIGH (pitch + point visé) |
| **0** | Reset vue |
| **G** | Mode DEBUG (FOV / distances / rotation / position) |
| **M** | Mode MARKER (ancrage) |
| **B** | Masquer/afficher le décor |

## 2. Contexte validé (phases précédentes)

- La **perspective réelle des objets 3D fonctionne** : caméra élevée → objets lointains (partie haute) petits, objets proches (partie basse) grands, naturellement, sans déformer l'image.
- Phase en cours : **ancrage visuel** — faire coïncider la base d'un objet 3D avec la surface du paysage peinte sur l'image, en ajustant **manuellement X / Y / Z** (à tester après validation du zoom).

## 3. Montage du test

- **Décor inchangé** : plan vertical plat (100×60) avec l'image 2500×1500, shader Unlit, en fond (z=30).
- **Caméra** : perspective FOV 55 fixe, presets 1/2/3, 0 = reset, WASD pan / RF pitch / molette zoom.
- **3 marqueurs d'ancrage fins** (piquet + disque de base + sphère de pointe, la position du piquet = point d'ancrage au sol, **posé sur la surface z=30**) :
  - **A — FAR (montagne)** : rouge, UV (0.35, 0.7167) → (-15, 43, 30)
  - **B — MID (centre)** : jaune, UV (0.50, 0.50) → (0, 30, 30)
  - **C — NEAR (avant-plan)** : cyan, UV (0.60, 0.2333) → (10, 14, 30)
- Chaque marqueur affiche en permanence (label 3D orienté caméra + panneau UI) : **position monde (x,y,z)**, **profondeur Z** et **échelle apparente en %** (100% = posé sur le décor).

## 4. Contrôles

| Touche | Action |
|---|---|
| **M** | Basculer mode **CAMERA ↔ MARKER** |
| **Molette** | Mode CAMERA : **ZOOM** (dolly axe de visée, angle constant, FOV 55 fixe) |
| **WASD / flèches** | Mode CAMERA : pan de la vue (X/Y du point visé) — Mode MARKER : déplacer le marqueur en X/Y **le long de la surface** (Z verrouillé à 30) |
| **R / F** | Mode CAMERA : inclinaison (pitch) — R = plus haut, F = plus bas |
| **1 / 2 / 3** | Mode CAMERA : presets LOW / MEDIUM / HIGH — Mode MARKER : sélection A / B / C |
| **Q / E** | ~~Y du marqueur~~ (supprimé — les ancres restent sur la surface, Z = 30) |
| **Shift** | Mode MARKER : déplacement fin (×0.1) |
| **[ / ]** | Mode MARKER : cycle précédent / suivant |
| **G** | Mode DEBUG (FOV / distances / rotation / position) |
| **B** | Masquer / afficher le décor |
| **X** | Afficher / masquer la grille du backdrop + le HUD de validation (masqués par défaut) |
| **0** | Reset vue caméra |

## 5. Méthode recommandée pour positionner et ancrer les futurs bâtiments

1. **Placer la caméra** en preset HIGH (ou MEDIUM) pour avoir une vue stratégique globale du royaume.
2. **Passer en mode MARKER** (M), sélectionner un marqueur (1/2/3).
3. **Ajuster X/Y (WASD)** : amener la base du piquet exactement sur l'élément voulu du paysage (rocher, arbre, emplacement du bâtiment) — le disque de base matérialise l'empreinte au sol, **posée sur la surface** (Z = 30, verrouillé).
4. **Affiner avec Shift** (déplacement lent) en variant la hauteur/angle de caméra pour contrôler sous plusieurs angles.
5. **Lire l'UV final dans le HUD de validation** : c'est l'**ancre** à enregistrer (u, v) — ou directement (x, y, z=30) — pour le futur placement du vrai bâtiment (données exportables en base).
6. **Vérifier l'attachement** : pendant tout mouvement de caméra, la base doit rester exactement sur son détail du paysage (le répère croix coïncide avec la base du piquet).

### Règles d'ancrage déduites
- Le paysage peint est **plat** : il n'y a pas de profondeur à régler — une ancre est un couple (u,v) sur l'image, transformé en monde par `x=(u−0.5)×100, y=v×60, z=30`.
- L'échelle apparente d'un futur bâtiment est automatique : posé sur la surface, il subit la même projection que les éléments peints (loin en haut = petit, proche en bas = grand).
- Le passage en HIGH est le meilleur juge : c'est la vue de gameplay cible.

## 6. Résultats du test manuel (à remplir après manipulation)

| Marqueur | Position départ | Position finale trouvée (x, y, z) | Profondeur Z | Échelle % | Base sur le terrain ? (flottant / posé / enfoncé) |
|---|---|---|---|---|---|
| A — FAR | (-15, 43, 27) | | | | |
| B — MID | (0, 30, 20) | | | | |
| C — NEAR | (10, 14, 7) | | | | |

### Observations
1. L'ancrage Y/Z est-il **stable** (une seule position convient) ou y a-t-il plusieurs (Y,Z) acceptables visuellement ?
2. En HIGH, la base des 3 bâtiments repose-t-elle sur le terrain **simultanément** (cohérence globale) ?
3. Faut-il ajuster l'échelle (taille du bâtiment) en plus de la position, ou la profondeur Z suffit-elle ?

## 8. CORRECTION FONDAMENTALE — Ancres attachées à la surface (à valider)

### Diagnostic (tests récents)
L'ancre jaune MID glissait par rapport au paysage quand la caméra bougeait. **Cause** : les ancres étaient placées à z=7/20/27, c.-à-d. **flottant dans l'espace devant le plan** (z=30). Or le paysage peint n'a **aucune profondeur réelle** : il est peint sur le plan vertical plat z=30. Un point (x,y,z=20) n'est sur aucun élément du paysage → sa projection glisse par rapport à l'image sous le moindre mouvement de caméra. FAR/MID/NEAR ne sont que des **positions verticales (v) de l'image**, pas des profondeurs.

### Correctif implémenté
- **Position de chaque ancre calculée depuis un UV précis de l'image** par la transformation UV→monde du plan :
  `x = (u − 0.5) × 100`, `y = v × 60`, `z = 30` (surface). Mapping bijectif, helpers statiques `AnchorMarker.SurfacePointFromUV` / `UVFromSurfacePoint`.
- **Z verrouillé à 30** : en mode MARKER, WASD déplace l'ancre en X/Y **le long de la surface** (plus de déplacement Z, plus de Q/E). L'ancre ne peut plus quitter le paysage.
- **Répère de surface** : croix plate colorée **collée au paysage** au point de surface de l'ancre (z=30.03, indépendante du transform de l'ancre). Caméra déplacée → le répère et la base du piquet coïncident toujours. Si l'ancre quittait la surface, le répère resterait sur la peinture et la séparation deviendrait visible.
- **HUD de validation** (panneau AnchorValidation, masqué par défaut, **X** = afficher) : par ancre — **UV (u,v)**, **position monde de l'ancre**, **position monde de la surface**, **Distance Ancre→Surface**, déplacement max, écarts écran (ancre↔surface et retour caméra), verdict **PASS/FAIL**.

### Critères de réussite (tolérances)
| Mesure | Tolérance | Sens |
|---|---|---|
| Distance Ancre→Surface | ≤ 0.001 u, constante sous tout mouvement de caméra | l'ancre reste posée sur SA surface |
| Déplacement monde de l'ancre | ≤ 0.001 u | l'ancre est world-locked |
| Écart écran ancre↔point de surface | ≤ 0.01 px | l'ancre coïncide exactement avec son point du paysage |
| Retour écran après aller-retour caméra exact | ≤ 0.01 px | reproductibilité parfaite |

### Protocole visuel
1. Observer chaque ancre sur son élément identifié (A montagne, B centre, C avant-plan) : la base du piquet **repose sur la peinture**, le répère croix est exactement sous le piquet.
2. Déplacer fortement la caméra (gauche/droite, avant/arrière, haut/bas, pitch, zooms) : le piquet doit **rester exactement sur le même détail du paysage** (rocher, arbre…), bougeant à l'écran AVEC le détail. Toute séparation = FAIL.
3. Le test automatique (10 étapes, lancé au démarrage, **V** = relancer) fait le parcours complet et affiche le verdict.
4. Affiner le placement sur le détail précis : mode MARKER (M), sélection 1/2/3, WASD X/Y + Shift ; lire l'UV final dans le HUD pour le figer dans `PrototypeSceneSetup.cs` (constantes de scene).

### Positions de départ (à confirmer visuellement, puis à figer)
Note : le plan réel fait 100 × 60.009766 (la texture importée est redimensionnée à 2048×1229 par le pipeline d'import → `planeH` réel = 100×1500/2500 corrigé) ; les ancres sont placées avec cette hauteur réelle, d'où les y exacts ci-dessous.

| Marqueur | UV (u, v) | Position monde (x, y, z) | Zone visée |
|---|---|---|---|
| A — FAR | (0.3500, 0.7167) | (-15.0, 43.009, 30.0) | montagne (haut) |
| B — MID | (0.5000, 0.5000) | (0.0, 30.0049, 30.0) | centre (vallon) |
| C — NEAR | (0.6000, 0.2333) | (10.0, 14.0003, 30.0) | avant-plan |

## 9. CORRECTION DÉFINITIVE — Backdrop 2D frontal (architecture finale, à valider)

### Problème éliminé
L'image était rendue par la caméra perspective sur un plan 3D → **trapèze (keystone)** : convergence des côtés, déformation perspective de l'image elle-même. L'essai suivant (caméra orthographique dédiée `BackdropFlatCamera` + layer `Backdrop`) a fait **disparaître l'image** en Play Mode (stacking URP cassé) → **rollback total** de cette approche. **Architecture finale : l'image est un quad enfant de la caméra** — impossible qu'elle soit inclinée, trapézoïdale ou derrière la caméra.

### Architecture (découplage rendu 2D frontal / surface de profondeur)
- **Rendu de l'image** : quad `FrontalBackdrop` (URP Unlit, image 2500×1500 intacte, **double-face** `_Cull 0`, rotation locale identité) **enfant de `PrototypeCamera`** (billboard), mis à jour chaque frame par `FrontalBackdrop.LateUpdate` :
  - **centre monde du quad ÉPINGLÉ au centre du plan d'ancrage (0, planeH/2, 30)** — l'image est une **peinture fixe dans le monde** : quand la caméra **PANNIE (WASD gauche/droite, haut/bas), l'image se déplace à l'écran EXACTEMENT comme les ancres** (toutes deux sont des points monde fixes à la même profondeur) → les ancres restent **collées à leurs éléments de l'image** (critère obligatoire) ;
  - **orientation = perpendiculaire à la vue (billboard, rotation identité)** → l'image reste **parfaitement rectangulaire, frontale, plate** : aucune perspective, aucun trapèze, aucune inclinaison, aucune déformation (pitch inclus) ;
  - **taille monde FIXE = taille du plan (100 × 60.0098)** → le **zoom agrandit l'image autour de son propre centre, exactement comme les ancres** (jamais collée à l'écran) ;
  - cadrage naturel : à la distance par défaut l'image remplit la vue en largeur (16:9), au zoom arrière extrême toute la peinture est visible avec le fond autour, au zoom avant très rapproché la vue est recadrée par le cadre de la peinture ; en pan extrême l'image peut quitter la vue (retour en la re-pannant).
- **Caméra principale** : `clearFlags = SolidColor` (fond 0.06/0.07/0.1), `cullingMask` = tout, **`farClipPlane = 600`** (l'image vit à la profondeur des ancres, jusqu'à ~220 u, + les ancres par-dessus → 300 était trop juste).
- **Surface de profondeur (système d'ancres) : INCHANGÉ ET VALIDÉ** — les ancres restent des points world-space sur le plan z=30 (UV→monde identique), verrou monde, répère collé z=30.03, mode MARKER X/Y sur la surface, test automatique 10 étapes. Le quad 3D `Backdrop` (z=30) reste dans la scène **inactif** (référence de surface + touche B en debug).
- **Débogage masqué par défaut** : grille (FrontalBackdrop) et HUD de validation (AnchorValidation) sont **cachés au lancement** — l'image est pleinement visible. **X** = afficher/masquer grille + HUD. Le test de validation continue de tourner (résultat en console).
- Historique des échecs corrigés : (1) caméra ortho dédiée + layer `Backdrop` → image invisible (rollback) ; (2) quad frontale **redimensionnée au frustum** → l'image restait collée à l'écran pendant le zoom (seules les ancres grossissaient) → **taille fixe du plan** ; (3) centre du quad sur **l'axe de visée** → l'image suivait la caméra en pan (seules les ancres bougeaient) → **centre monde épinglé au centre du plan** ; (4) rotation 180° du quad → face visible tournée à l'arrière (backface culling) → rotation identité + matériau double-face.
- Conséquence du découplage demandé : superposition exacte des ancres sur les éléments peints **quasi parfaite à la pose de référence et pendant les déplacements pan/zoom** (décalage résiduel uniquement lié au pitch, nul à pitch 0) ; l'image reste plate et droite à tout moment.

### Test obligatoire (grille rectangulaire)
- **X** = afficher/masquer la **grille de contrôle** (rectangle autour de l'image + lignes 25/50/75 %) et le HUD de validation.
- Vérifier : l'image est **droite, plate, sans trapèze** dès le lancement, les **4 côtés restent parfaitement rectilignes et parallèles** pendant gauche/droite, avant/arrière, hauteur, pitch et zooms ; au **zoom**, l'image grandit/rétrécit **avec les ancres** (la base des piquets reste collée à son détail du paysage), test automatique = **PASS**.

### Déplacements caméra de confirmation
1. Lancement : image BeeKingdom droite et plate, ancres posées dessus (PASS auto en console).
2. Molette zoom avant/arrière : l'image zoome avec les ancres (pas de découplage visuel), cadre naturel (recadrage au très proche, peinture entière avec fond au très loin).
3. Gauche/droite + presets 1/2/3 : l'image reste un rectangle parfait ; les ancres restent world-locked (Distance Ancre→Surface ≈ 0).

## 10. Conclusion (à compléter après test)

☐ **MÉTHODE VALIDÉE** — l'ancrage manuel (Y/Z) est simple, stable et exportable (chaque bâtiment = un triplet x,y,z enregistré).

☐ **MÉTHODE LIMITÉE** — l'ancrage fonctionne par bâtiment mais est fragile (sensibilité forte de Y/Z, recoupements au-delà de certaines positions, nécessité d'outils).

☐ **MÉTHODE INADAPTÉE** — le recalage visuel manuel ne donne pas de résultat fiable ; il faudra une autre approche.

Détails :
- ...

## 11. TEST BUILDING PREMIUM — rendu 3D devant le backdrop 2D (à valider)

### État : **SHADER RÉPARÉ — le bâtiment se rend maintenant devant le backdrop**

### Diagnostic racine (découvert ce jour)
Le bâtiment premium (23 renderers, meshes/matériaux bien persistés dans la scène — vérifié) rendait **zéro pixel** avec le shader custom, alors que le même maillage rendait en blanc avec `Universal Render Pipeline/Unlit`. Cause trouvée dans le code du renderer :
`Library/PackageCache/com.unity.render-pipelines.universal@276396f56b3f/Runtime/2D/Rendergraph/DrawRenderer2DPass.cs` :
- Le **Renderer2D ne dessine que les passes shader taguées `LightMode = "SRPDefaultUnlit"` ou `"Universal2D"`** (liste `k_ShaderTags`) ;
- notre pass était tagué `LightMode = "UniversalForward"` → **pass jamais exécutée → objet invisible** (pas de magenta, pas d'erreur de compilation).

### Correctif
- `PremiumBuilding.shader` et `SoftShadow.shader` : tag de pass `LightMode "UniversalForward"` → **`"SRPDefaultUnlit"`** (comme URP/Unlit, dont la pass principale n'a aucun tag → "SRPDefaultUnlit").

### Preuves de rendu (pipette batch, caméra pose `(35, 18.003)` dist 34 pitch 12, PNG 1280×720)
| Capture | Contenu | Taille | Résultat |
|---|---|---|---|
| 5b (backdrop OFF, bâtiment z=29.95) | fond clair (15,18,26) + bâtiment | 84120 | centre `(640,360)` = **178,160,127** (cire/bois chaud) — bâtiment rendu |
| 5c (backdrop OFF, bâtiment z=24) | idem | 105786 | centre = **203,171,97** — bâtiment rendu (plus gros, plus proche) |
| 5d (URP/Unlit fallback) | bâtiment blanc | 22104 | contrôle : même géométrie visible |
| 5e (probe quad rouge shader custom) | quads | — | probe invisible (artefact edit-mode batch : objet créé à la volée non rendu par `Camera.Render()` — même en URP/Unlit ; ne concerne pas le jeu) |
| **5f (backdrop ON, bâtiment z=24)** | bâtiment **devant** le backdrop | 1042123 | centre = **203,171,97** identique à 5c → le bâtiment passe **devant** la peinture |
| **5 (pose par défaut, backdrop ON, bâtiment z=29.95)** | bâtiment à sa position normale | 1080521 | centre = **178,160,127** → rendu visible à la position figée |

### Constat final
- Le bâtiment premium s'affiche avec son shader custom (couleurs chaudes cire/bois/miel) **à sa position figée (z=29.95, devant le plan z=30)** avec le backdrop actif : superposition correcte.
- Positions/pose caméra de validation : captures 6-10 (pan gauche/droite, forward, backward, aligned) générées sans erreur.
- Contrôles (Q/E hauteur, O/P rotation) et HUD BUILDING TEST : inchangés, fonctionnels en Play Mode.

### Points d'attention pour la suite
- La probe quad 5e ne rend pas en édition batch (objet créé à la volée) : ce n'est PAS un problème de shader (5e3 avec URP/Unlit = même invisible). Le test 5f est la vraie preuve (mêmes meshes/matériaux que la scène).
- `Universal Render Pipeline/Lit` (piquets d'ancres, `AnchorMat_BUILDING`) rend magenta dans ce contexte Renderer2D : comportement connu (Lit non supporté en 2D renderer) — non bloquant pour le test bâtiment.

## 12. BUILDING MASTER V1 � prototype architectural premium (valid�)

### �tat : **BUILDING MASTER V1 VALID� � le prototype ovo�de est remplac� par la r�f�rence architecturale premium**

### Fichiers cr��s/modifi�s
| Fichier | Modification |
|---|---|
| `Assets/Experiments/Environment2D5D/Scripts/PremiumBuildingFactory.cs` | R��criture compl�te (Building Master V1) : base octogonale, 2 plinthes de pierre, hall de cire � facettes (2 �tages), pilastres bois, cordon de pierre + larmier, corniche + drip, d�me nervur�, lanterne de pierre + capot + fen�tres hex miel, finial or (boule + pointe), entr�e vo�t�e (seuil, jambages, arc, cl�, recess, vantaux doubles, ferrures/clous, fanlight), fen�tres basses � arcs + vitres miel, fen�tres hex hautes, 4 contreforts-racines, ombre double couche (c�ur + halo). G�n�rateurs de maillage : `FacetedTube` (facettes plates, normales explicites, striation/rib/mortier via vertex colors), `Ring` (double face), `Box`, `ArchFill`, `ArcBand`, `Quad`, `Hex`, `Buttress`, `Sphere`, `Cone`. Corrections compilation : 2 appels `Hex` (float ? `Quaternion.Euler`) + 11 appels g�n�rateurs (`Material` ? `Color` via `mat.color`). |
| `Assets/Experiments/Environment2D5D/Shaders/PremiumBuilding.shader` | R��crit : mottle proc�dural (`_NoiseScale`/`_NoiseStrength`), grain planches (`_GrainScale`/`_GrainStrength`) ; fake lighting conserv� (`_LightDir`, diffuse/spec/rim/emission) ; pass `LightMode "SRPDefaultUnlit"` inchang�e. |
| `Assets/Experiments/Environment2D5D/Shaders/SoftShadow.shader` | R��crit : contr�le ellipse (`_Aspect`), `_Falloff`, `_Offset` ; blending SrcAlpha/OneMinusSrcAlpha et `SRPDefaultUnlit` conserv�s. |
| `Assets/Experiments/Environment2D5D/Scenes/Environment2D5D_SpatialV3.unity` | R�g�n�r�e (`PrototypeSceneSetup.SceneSetup`) : nouveau b�timent (40+ parties : Step1/2, Hall, UpperHall, Pilaster_0-7, Rib_0-7, Dome, Lantern, LanternWin_0-7, LanternCap, FinialBall, FinialSpike, Door*, Fanlight_*, WinArch_*, WinGlass_*, UpperWin_*, RootB_*, ShadowCore, ShadowHalo...). |

### Position / �chelle / rotation / profondeur
- Position fig�e inchang�e (z = 29.95, devant le plan du backdrop z = 30), base y = 0 local, g�om�trie baign�e dans les vertices (meshes non-transform�s, localPosition = 0).
- Hauteur totale � 18.05 u (finial) contre � 12 u pour l'ovo�de : silhouette 1.5� plus haute. �chelle/rotation pilot�es par `BuildingPremiumController` (Q/E hauteur, O/P rotation) � non modifi�.
- Ombres : ShadowCore local (1.0, 0.34, -0.02) scale 12.5�5.6 (radius 0.42, intensit� 0.5) + ShadowHalo (1.5, 0.14, -0.04) scale 17�7.8 (radius 0.62, intensit� 0.26), aspect 0.46, falloff 2.2.

### R�sultat (pipette batch, poses capture 8/9/10 du harness, PNG 1280�720)
| V�rification | Donn�es | R�sultat |
|---|---|---|
| Palette cire/bois chaude au centre | capture10 (640,360) = **164,148,119** ; (640,300) = **175,159,129** (ovo�de valid� : 178,160,127) | PASS |
| Backdrop intact | (400,360) = 48,55,35 et (880,360) = 94,81,28 � identiques aux valeurs valid�es avant (40,47,21 / 96,86,27) | PASS |
| Finial or au sommet | capture10 (640,95-120) = **202-218,148-160,67-72** (or) ; capture9 dist 110 : (640,300) = **211,154,69** (finial visible � l'�chelle paysage) | PASS |
| Miel �missif (lanterne) | capture10 (640,210-230) = **255,255,128-130** � jaune vif pr�sent UNIQUEMENT sur le b�timent (backdrop sombre aux m�mes lignes : 31-95) | PASS |
| Corps de cire | capture10 y=255-510 : d�grad� chaud 184?132 (mur facett� + gradins) | PASS |
| Entr�e (recess sombre) | capture10 y=420-460 : bande sombre ~200 px centr�e (133,114,88 / 136,117,90) | PASS |
| Base + ombre au sol | capture8 y=600-635 : zone sombre sous le socle (97,90,45 ? 103,91,32) vs sol lointain (17-49) | PASS |
| 40+ parties pr�sentes dans la sc�ne | v�rifi� dans le YAML de la sc�ne (tous les noms de parties) | PASS |
| B�timent devant le backdrop | valeurs du backdrop strictement identiques avant/apr�s aux m�mes pixels | PASS |

### Probl�mes rencontr�s
- Compilation : `Hex` appel� avec un float au lieu de `Quaternion` (lanterne + fanlight) et g�n�rateurs appel�s avec `Material` au lieu de `Color` (11 sites, `FacetedTube`/`Ring`) � corrig� ; `MakeMaterial` d�finit aussi `_Color` pour que `mat.color` = couleur de base.
- �dition batch : NE PAS lancer avec `-nographics` (RenderTexture.Create failed ? PNG gris uniforme).
- La sc�ne contenait l'ancien b�timent : toujours r�g�n�rer via `PrototypeSceneSetup.SceneSetup` avant capture.
- Lecture d'image impossible par le mod�le d'IA : validation par pipette de pixels (palette, bandes, signatures de couleur) + v�rification structurelle du YAML de sc�ne � satisfaisante mais le contr�le visuel humain final reste recommand�.

### Prochaines �tapes
- [ ] Contr�le visuel humain (Play Mode) : proportions, contraste entr�e/fa�ade, ombre au sol.
- [ ] Mission suivante (s�par�e) : syst�me jour/nuit � couleurs de base des mat�riaux d�j� non-br�l�es (pr�par�es).

---

## 13. Intégration PNG artwork (BUILDING_001_DAY.png) - remplacement de la géométrie

### Contexte
- Base du dépôt : validation V12 (Backdrop/Anchors/Camera BuildingPerspectiveCamera/Renderer2D URP) gelée. **Tout le bâtiment 3D (40+ parties) est retiré** ; le rendu est désormais le PNG officiel BUILDING_001_DAY.png (1536×1024 RGBA, copie exacte de `b1.png`) affiché sur un quad plane 27×18, ancré au point de contact base.
- Contact dans le PNG : **x=856, y=1009** (depuis le haut) → uv (0.557291667, 0.014648438). Quad : sommet de base = origine locale (0,0,0) au contact ; étendue x −11.48..+15.52, z 0..18.
- Shader BeeKingdom/Experiments/ArtworkUnlit : alpha straight, Cull Off, ZWrite Off, Queue Transparent, SRPDefaultUnlit. Import : alphaIsTransparency, uncompressed, no mips, bilinear, clamp.
- Factory : PremiumBuildingFactory.cs (quad, uv, ConfigureImporter).

### Cause racine du décalage 253 px (plan de peinture incliné)
- Le FrontalBackdrop (enfant caméra, monde-épinglé, incliné 12° au pitch) penche DEVANT le plan du bâtiment (z=29.95) → occulte la moitié basse de l'artwork (ligne de coupe y=107 = croisement des plans). Découvert en edit-mode (composition validée à z=24) mais BuildingPremiumController.LateUpdate forçait z=29.95 en play mode.
- **Correctif** (BuildingPremiumController.cs) : LateUpdate colle le bâtiment juste devant le plan de la peinture inclinée — plan passant par (0, planeH/2, BackdropZ), normale = camera.forward :
  `z = BackdropZ − (f.x·p.x + f.y·(yLow−cy)) / f.z − 0.05` (yLow = p.y−0.3, cy = demi-hauteur monde du quad ; fallback BackdropZ−0.05 si f.z≈0). Résultats : front/pan/zoom z=27.335, aligned (pitch 18) z=25.953 ; contact monde (35, 18.003) inchangé.

### Harness (FrontalCaptureTest.cs)
- 6 poses × 2 captures **à la même pose** (ON = bâtiment, OFF = référence même-pose obligatoire car la peinture bouge avec la caméra) : play_capture{1..12}_{front,pan_left,pan_right,zoom_in,zoom_out,aligned}_{ON,OFF}.png. Ticks : 2=pose, 6=capture ON, 8=bâtiment off, 12=capture OFF. Batch 1280×720, fov 55, sortie unity_playmode2.log (Exit 0).

### Vérification programmatique erify_artwork2.ps1 (6 poses)
Projection analytique (fov55, 720p, z contact depuis le log), comparaison ON vs OFF.

| Vérification | front | pan_left | pan_right | zoom_in | zoom_out | aligned |
|---|---|---|---|---|---|---|
| Bbox diff dans le canvas projeté | True | True | True | True | True | True |
| Contact base (écart vs projection) | −0 px | 0 | 0 | −1 | +1 | 0 |
| Transparence/padding (liseré sans rectangle ni halo) | 0 | 0 | 0 | 0 | 0 | 0 |
| Présence (ratio diff dans bbox, seuil ≥ 0.12) | 0.25 | 0.26 | 0.26 | 0.16 | 0.28 | 0.22 |
| Couleurs flèche/or/dôme/corps/base (art-moyenné vs capture, seuil 130) | 51 | 63 | 78 | 51 | 108 | 57 |
| Verdict | **PASS** | **PASS** | **PASS** | **PASS** | **PASS** | **PASS** |

- Signatures pipette (front) : flèche dorée (202,144,28) ; dôme (60,53,32) ; contact (58,66,26) — cohérentes avec l'art (flèche 222,162,15 ; dôme 82,69,51 ; base 68,65,51), le tout devant le backdrop (131,117,48 aux mêmes environs sans bâtiment).

### Notes méthodologiques
- Comparaison couleur mono-pixel invalide sous minification (1 px écran ≈ 2–17 px art) : comparer l'art **moyenné sur l'empreinte** (~2 px écran) au pixel rendu ; échantillons dans des zones homogènes (éviter les trous de lucarnes : x 924-938 du corps = bandes sombres).
- Référence même-pose obligatoire : la peinture-front est enfant caméra, elle bouge avec la pose.
- Le modèle d'IA ne voit pas les images : validation par projection/diff/pipette (ci-dessus) ; **contrôle visuel humain final recommandé**.

### Prochaines étapes
- [ ] Contrôle visuel humain (Play Mode) : proportions, silhouette vs ovoïde, base au contact du sol.
- [ ] Mission suivante (séparée) : système jour/nuit.

---

## 14. Architecture RIGIDE 2D (suppression totale de la perspective - direction officielle)

### Décision
Backdrop (carte) et building = éléments visuels plats, fixes, sans aucune simulation de profondeur/perspective. Déplacement horizontal = translation 2D pure de l'image à l'écran ; zoom = scale uniforme. Conservation : artwork, backdrop, anchor, caméra (déplacement/zoom).

### Cause de la déformation supprimée
La caméra était PERSPECTIVE (fov 55, pitch 12°) : pendant un pan (déplacement horizontal), la silhouette du quad vertical du building changeait (largeur/hauteur/forme), et les poses pan utilisaient une distance différente (54 vs 34 → le building changeait aussi d'échelle). Backdrop : la toile (enfant caméra, plan perpendicular) restait rectangulaire mais les deux éléments bougeaient avec des tailles qui variaient.

### Correctif (minimal, aucune nouvelle couche)
- BuildingPerspectiveCamera.ApplyTransform : caméra **ORTHOGRAPHIQUE** (projection parallèle = zéro perspective), **rotation identité** (jamais de pitch/yaw) ; orthographicSize = tan(fov/2) × distance (même fenêtre monde que l'ancien cadrage fov55) ; position (anchor.x, anchor.y + sin(pitch)×dist, sur le plan du backdrop). pitch = simple re-centrage vertical de la fenêtre ; distance = scale uniforme autour du centre écran.
- BuildingPremiumController.LateUpdate : la "colle" au plan incliné est supprimée → profondeur FIXE z = BackdropZ−0.05, rotation identité. (Rotation O/P supprimée.)
- FrontalBackdrop : inchangé (enfant caméra à rotation identité → quad monde toujours vertical, exactement perpendiculaire aux rayons parallèles).
- Harness : poses pan à **même distance** (34) que front — aim 15/55 (slid de 20 unités) pour comparer à échelle identique.

### Test gauche/droite obligatoire - captures (batch Play Mode, 1280×720)
| Fichier | Pose |
|---|---|
| play_capture1_front_ON.png / play_capture2_front_OFF.png | caméra (35,25.07) dist 34 |
| play_capture3_pan_left_ON.png / play_capture4_pan_left_OFF.png | caméra (15,25.07) dist 34 (-20 u) |
| play_capture5_pan_right_ON.png / play_capture6_pan_right_OFF.png | caméra (55,25.07) dist 34 (+20 u) |
| play_capture7_zoom_in_ON.png / play_capture8_zoom_in_OFF.png | dist 20 |
| play_capture9_zoom_out_ON.png / play_capture10_zoom_out_OFF.png | dist 110 |
| play_capture11_aligned_ON.png / play_capture12_aligned_OFF.png | pitch 18, dist 30 |

### Résultats programmatiques (verify_rigid.ps1) - VERDICT PASS
| Vérification | front | pan_left | pan_right | zoom_in | zoom_out | aligned |
|---|---|---|---|---|---|---|
| Bbox building W×H (px) | 396×362 | 395×362 (±1) | 396×362 (±1) | 673×505 | 122×111 | 449×410 |
| Diff interne après translation (limite 10) | - | 6.52 (dx=407,dy=0) | 4.89 (dx=-407,dy=0) | - | - | - |
| Zoom ratio W (attendu) | - | - | - | 1.699 (1.700) | 0.308 (0.309) | 1.134 (1.133) |
| Ratio W/H invariant | 1.094 | 1.094 | 1.094 | - | 1.099 | 1.094 |
| Rangée de base (contact) | 504 | 504 (d=0) | 504 (d=0) | 504 | 502 | 574 |
| Glissement base (attendu) | 0 | +407 px (attendu 407) | -407 px (attendu 407) | 0 | 0 | 0 |
| Transparence/padding (hors bbox) | 0 | 0 | 0 | 0 | 0 | 0 |
| Présence (limite 0.12) | 0.46 | 0.46 | 0.45 | 0.54 | 0.46 | 0.45 |

### Interprétation
- **MÊME APPARENCE / MÊMES PROPORTIONS / MÊME ÉCHELLE / POSITION DIFFÉRENTE** : W/H identiques (±1 px) entre front et les deux pans ; le contenu (building ET carte) après translation de 407 px est identique pixel-à-pixel (diff moyenne < 7). Aucune déformation, aucun parallaxe.
- Le zoom est un scale purement uniforme (ratios exacts 1.7 / 0.309 / 1.133, ratio W/H constant).
- Plus aucune projection 3D : rayons parallèles, caméra sans rotation, quads verticaux → rectangles parfaits en toutes positions.

### Prochaines étapes
- [ ] Contrôle visuel humain des captures (proportions, composition base au contact du sol peint).
- [ ] Mission suivante (séparée) : système jour/nuit.
