# Builder-B - Support Ruche Pour Builder-A

## Statut

Préparation uniquement. Ce document ne modifie pas la scène principale, ne modifie pas le runtime, ne lance aucun build et ne branche aucune fonctionnalité officielle.

Priorité actuelle : ruche uniquement. La carte monde n’est pas traitée dans ce document.

Références Builder-B liées :

- `Docs/BuilderB/hive_click_zones_v001.sample.json`
- `Docs/BuilderB/hive_click_zone_overlay_viewer.html`
- `Docs/BuilderB/HiveClickableZones_Strategy.md`
- `Docs/BuilderB/HiveZones_Refinement_Pass03.md`
- `Docs/BuilderB/ARCH166_HiveGestureAndViewport_Handoff.md`

## Objectif

Préparer une aide claire pour Builder-A sur :

- cadrage tablette paysage de la ruche ;
- cadrage téléphone portrait ;
- zones cliquables précises ;
- halo de sélection ;
- matrice de validation centre / bord / hors-zone ;
- réduction de l’encombrement UI ;
- séparation couche ruche / HUD / panneaux.

## Cadrage Tablette Paysage

But : la ruche doit occuper le maximum d’espace utile sans que le HUD ou les panneaux masquent les zones principales.

Recommandation :

```text
Ecran
  FixedHudLayer
    HUD haut compact
    rail/navigation fixe
    panneau détail repliable
  HiveViewportClip
    HiveCameraLayer
      asset ruche
      zones
      halos
      présence abeilles
```

Règles tablette paysage :

- La ruche doit être le premier signal visuel.
- Le viewport ruche devrait viser au moins `76%` de la hauteur utile après safe area.
- Préférer un cadrage `aspect-fill + clip` plutôt qu’une ruche réduite avec marges vides.
- Le centre Administration / Reine doit être visible au cadrage initial.
- Les zones latérales doivent être accessibles par pan léger, pas cachées derrière un panneau fixe.
- Les panneaux latéraux doivent être compacts, repliables ou semi-contextuels.

Valeurs de départ :

```text
topHudHeight: 8-10% hauteur écran
bottomRailHeight: 8-10% hauteur écran
sidePanelWidthOpen: max 24% largeur écran
sidePanelWidthCollapsed: 0-8% largeur écran
hiveViewportTargetHeight: >= 76% hauteur utile
defaultZoomLandscape: minZoom * 1.05
maxZoomLandscape: minZoom * 2.4 à 2.8
```

## Cadrage Portrait

But : garder les menus et panneaux essentiels visibles sans écraser la ruche.

Recommandation :

- Ne pas forcer toute la ruche à rentrer dans l’écran portrait.
- Utiliser une fenêtre ruche pannable/croppable entre le HUD haut et le rail/panneau bas.
- Garder le panneau détail en bottom-sheet plutôt qu’en grand panneau latéral.
- Après sélection, recentrer doucement la zone sélectionnée si le panneau bas la masque.

Règles portrait :

```text
topHud: fixe
hiveViewport: pannable, zoomable, clipped
bottomRail: fixe
detailPanel: bottom-sheet, hauteur limitée
```

Valeurs de départ :

```text
topHudHeight: 9-13% hauteur écran
bottomRailHeight: 9-13% hauteur écran
detailPanelClosedHeight: 0-12% hauteur écran
detailPanelOpenHeight: max 34% hauteur écran
defaultZoomPortrait: minZoom * 1.15 à 1.25
maxZoomPortrait: minZoom * 2.2 à 2.6
```

Règle de recentrage :

```text
si selectedZoneBounds intersecte detailPanelScreenRect:
  targetPan = pan nécessaire pour placer selectedZoneCenter dans la zone visible
  appliquer SmoothDamp
```

## Zones Cliquables Précises

Source actuelle :

`Docs/BuilderB/hive_click_zones_v001.sample.json`

Le JSON contient les 14 zones officielles :

1. Nurserie
2. Reserve miel
3. Caserne
4. Defense
5. Genetique
6. Recherche
7. Entrepot
8. Transformation
9. Infirmerie
10. Academie
11. Banque
12. Administration
13. Archives
14. Centre alliance

Règles recommandées :

- Coordonnées source : `asset-normalized`, origine haut-gauche.
- Données de référence : points normalisés `0..1` sur `2048x3072`.
- Source de vérité visuelle finale : masque alpha par zone.
- Fallback : polygone fin.
- Les cercles et rectangles ne doivent pas être utilisés comme forme finale.
- L’extension tactile ne doit pas déplacer la frontière visuelle.

Contrat de projection :

```text
zone normalized -> hive local -> HiveCameraLayer transform -> viewport -> screen
screen pointer -> viewport -> inverse HiveCameraLayer transform -> hive local -> normalized
```

Builder-A doit éviter :

- hit-test en coordonnées écran non reprojetées ;
- halo en coordonnées différentes de la zone ;
- art zoomé mais zones non zoomées ;
- zones zoomées mais HUD/panneaux zoomés aussi.

## Halo De Sélection

Objectif : le halo doit confirmer précisément la zone sélectionnée sans masquer les détails premium.

Recommandation :

- Halo généré depuis la même géométrie que le hit-test.
- Trait screen-space ou équivalent pour garder une épaisseur lisible à tous les zooms.
- Remplissage très léger ou absent.
- Couleur contrastée mais non agressive.
- Animation douce, désactivable/réduite si reduced motion.

Valeurs de départ :

```text
haloStrokeWidthScreen: 2 à 4 px
haloFillOpacity: 0.08 à 0.16
haloPulseAmplitude: max 10% opacity
haloPulseDuration: 1.2 à 1.8 s
reducedMotion: pas de pulse, trait stable
```

Règle d’alignement :

```text
selectedZonePolygon == haloPolygon == hitTestPolygonFallback
si mask alpha final existe:
  contourHalo = contour extrait du mask ou polygon validé comme contour QA
```

## Matrice Centre / Bord / Hors-Zone

À préparer pour chaque zone.

| Cas | Point de test | Résultat attendu |
| --- | --- | --- |
| Centre | centroïde de la zone | sélectionne la zone |
| Centre visuel | point placé sur le motif principal de la zone | sélectionne la zone |
| Bord intérieur | 2-3 px à l’intérieur de la frontière visuelle | sélectionne la zone |
| Bord extérieur | 2-3 px à l’extérieur de la frontière visuelle | ne sélectionne pas la zone, sauf extension tactile debug explicitement affichée |
| Frontière partagée | point proche de deux zones | sélection selon priorité déterministe |
| Hors-zone proche | point entre deux alvéoles ou sur décor | aucune sélection ou sélection décor interdite |
| HUD recouvrant | point dans un panneau fixe au-dessus d’une zone | HUD prioritaire, pas de sélection ruche |
| Après pan | même point logique après déplacement caméra | sélection identique |
| Après zoom | même point logique après zoom | sélection identique |
| Portrait panneau ouvert | zone partiellement proche du panneau bas | sélection reste alignée, panneau ne capture pas hors de sa zone |

Format de matrice recommandé :

```json
{
  "zoneId": "administration_core",
  "tests": [
    { "case": "center", "point": { "x": 0.501, "y": 0.466 }, "expected": "hit" },
    { "case": "inside-edge", "point": { "x": 0.345, "y": 0.431 }, "expected": "hit" },
    { "case": "outside-edge", "point": { "x": 0.330, "y": 0.424 }, "expected": "miss" },
    { "case": "shared-border", "point": { "x": 0.500, "y": 0.386 }, "expected": "priority-rule" }
  ]
}
```

Priorité en cas de recouvrement :

```text
1. HUD / panneaux fixes
2. zone sélectionnée actuelle si le point reste dans sa frontière
3. zone priorité la plus haute
4. zone de plus petite aire
5. ordre officiel stable
```

## Réduction De L’Encombrement UI

Problème probable : la ruche premium perd son impact si trop de panneaux, badges, rails et textes occupent l’écran.

Recommandations :

- Garder le HUD haut compact : ressources essentielles seulement.
- Déporter les détails longs dans un panneau contextuel.
- Utiliser un bottom-sheet portrait avec états fermé / compact / ouvert.
- Éviter les panneaux latéraux permanents en téléphone portrait.
- Limiter les badges permanents sur les zones.
- Afficher les labels seulement si zoom suffisant ou zone sélectionnée.
- Ne pas superposer texte, halo et abeilles sur le même hotspot.
- Prévoir un bouton ou geste clair pour replier le détail.

Règles de densité :

```text
zoom faible:
  halo sélection seulement
  labels minimaux
  badges critiques uniquement

zoom moyen:
  labels zones sélectionnée + voisines
  badges d’état courts

zoom fort:
  points de détail
  labels plus complets
  micro-animations locales
```

Règles d’occupation recommandées :

```text
tabletLandscape:
  HUD + rails + panneaux ouverts <= 30% surface écran

phonePortrait:
  HUD haut + rail bas <= 26% hauteur écran
  panneau détail ouvert <= 34% hauteur écran
  ruche viewport visible >= 53% hauteur écran
```

## Séparation Couche Ruche / HUD / Panneaux

Structure recommandée :

```text
HiveScreenRoot
  FixedHudLayer
    ResourceHud
    TopStatus
    NavigationRail
  FixedPanelLayer
    DetailPanel
    BottomSheet
    ModalOverlay
  HiveViewportClip
    HiveCameraLayer
      HiveArt
      ZoneMaskLayer
      ZonePolygonDebugLayer
      SelectionHaloLayer
      BeePresenceLayer
      DebugCoordinateProbe
```

Règles :

- `HiveCameraLayer` reçoit pan/zoom.
- `FixedHudLayer` et `FixedPanelLayer` ne reçoivent jamais pan/zoom.
- Les zones et halos vivent sous `HiveCameraLayer`.
- Les panneaux capturent les touches dans leurs rectangles.
- La ruche ignore les touches capturées par HUD/panneaux.
- Les coordonnées de zones restent normalisées et indépendantes de l’orientation.

## Checklist Builder-A

- Confirmer que la ruche peut être cadrée en tablette paysage sans marges inutiles.
- Confirmer que le portrait utilise crop/pan plutôt qu’une ruche écrasée.
- Garder HUD et panneaux fixes pendant pan/zoom.
- Faire vivre art, zones et halos dans le même repère transformé.
- Utiliser les 14 ids de zones déjà préparés par Builder-B.
- Remplacer les polygones draft par contours affinés ou masques alpha.
- Ajouter la matrice centre/bord/hors-zone pour chaque zone.
- Bloquer la sélection quand le geste devient pan.
- Bloquer la sélection pendant pinch zoom.
- Vérifier halo aligné à min/default/max zoom.
- Vérifier halo aligné après pan.
- Vérifier tablette paysage et téléphone portrait.
- Vérifier que les panneaux ne masquent pas durablement la zone sélectionnée.
- Garder toute intégration sous revue Builder-A / Architecte.

## Non-Claims

Ce support ne corrige pas la ruche dans le jeu. Il ne modifie pas le runtime et ne valide aucun lot QA. Il prépare uniquement du matériel exploitable par Builder-A.
