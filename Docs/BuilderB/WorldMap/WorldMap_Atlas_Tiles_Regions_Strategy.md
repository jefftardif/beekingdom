# Builder-B - Stratégie World Map Atlas / Tuiles / Régions

## Statut

Préparation uniquement. Ce document ne modifie pas la scène principale, ne modifie pas le runtime, ne branche aucune donnée officielle et ne remplace pas la carte en jeu.

Objectif : préparer une trajectoire technique pour passer progressivement de la carte `reference-backed` actuelle vers une carte MMO scalable, découpée en atlas, tuiles et régions chargeables.

Références Builder-B déjà préparées :

- `Docs/BuilderB/WorldMap/world_map_zones_v001.sample.json`
- `Docs/BuilderB/WorldMap/world_map_overlay_viewer.html`
- `Docs/BuilderB/WorldMap/MapPanZoom_HUDSeparation_Spec.md`
- `Docs/BuilderB/WorldMap/WorldMapMMO_Strategy.md`

## Diagnostic De Départ

La référence actuelle `C:/projets/beekingdom/carte.png` est une image complète `1536x1024` qui contient à la fois :

- le fond de carte ;
- les marqueurs ;
- les territoires visuels ;
- la légende ;
- la minimap ;
- les barres HUD ;
- les menus.

Cette approche est utile pour valider la direction artistique, mais elle ne passera pas bien à l’échelle MMO :

- impossible de charger seulement une zone visible ;
- coût mémoire fixe même si le joueur regarde un coin de la carte ;
- difficulté à avoir des niveaux de zoom propres ;
- HUD et carte mélangés dans le même bitmap ;
- hotspots et halos risquent de dériver si les coordonnées sont liées à l’image aplatie ;
- future synchronisation serveur compliquée si territoires, hives et routes ne sont pas des entités séparées.

## Approche Recommandée

Passer en trois étapes, sans big bang.

### Étape 1 - Référence Découpée

Objectif : conserver le rendu actuel, mais séparer les responsabilités.

Sorties attendues :

- `map_background_reference.png` : fond de carte sans HUD.
- `map_overlay_reference.png` : effets artistiques non interactifs facultatifs.
- `world_map_zones_v001.sample.json` : entités interactives séparées.
- HUD, légende, minimap et panneaux en UI fixe.

À ce stade, la carte peut encore être une seule image, mais elle est rendue sous un `MapCameraLayer` séparé du HUD.

### Étape 2 - Atlas Par Niveau De Zoom

Objectif : préparer plusieurs résolutions de carte.

Proposition :

```text
WorldMapAtlas/
  v001/
    zoom_0/
      atlas_world_z0.png
      atlas_world_z0.json
    zoom_1/
      atlas_world_z1.png
      atlas_world_z1.json
    zoom_2/
      atlas_world_z2.png
      atlas_world_z2.json
```

Usage :

- `zoom_0` : vue globale, faible détail, territoires lisibles.
- `zoom_1` : vue régionale, hives et routes principales.
- `zoom_2` : vue locale, champs de ressources, points d’intérêt, nids hostiles.

Règle : les entités restent en coordonnées monde normalisées, pas en coordonnées atlas. L’atlas ne sert qu’au rendu visuel.

### Étape 3 - Tuiles Et Régions Streamables

Objectif : charger seulement les tuiles et entités visibles.

Structure proposée :

```text
WorldMapTiles/
  v001/
    z0/
      r00_c00.png
    z1/
      r00_c00.png
      r00_c01.png
      r01_c00.png
      r01_c01.png
    z2/
      r00_c00.png
      r00_c01.png
      r00_c02.png
      ...
```

Chaque tuile a :

- un niveau de zoom `z`;
- un index ligne/colonne ;
- un rectangle monde normalisé ;
- une priorité de chargement ;
- une version/cache key.

## Découpage Chunks / Régions

Coordonnées monde recommandées :

```text
worldX: 0.0 à 1.0
worldY: 0.0 à 1.0
origine: haut-gauche pour compatibilité avec les données Builder-B actuelles
```

Découpage initial recommandé :

- niveau région : grille `4 x 3` ou `4 x 4` selon cadrage final ;
- niveau chunk : chaque région contient `4 x 4` chunks ;
- niveau tuile visuelle : chaque chunk peut correspondre à une ou plusieurs tuiles selon zoom.

Exemple :

```json
{
  "regionId": "region_north_west",
  "bounds": { "x": 0.0, "y": 0.0, "width": 0.25, "height": 0.3333 },
  "chunkGrid": { "columns": 4, "rows": 4 },
  "theme": "mountain_forest",
  "defaultThreat": "neutral"
}
```

Règle de nommage :

```text
region_{row}_{col}
chunk_{regionId}_{row}_{col}
tile_z{zoom}_r{row}_c{col}
```

Exemple :

```text
region_01_02
chunk_region_01_02_03_00
tile_z2_r07_c08
```

## Règles De Chargement Progressif

Chargement par priorité :

1. Fond basse résolution global déjà en mémoire.
2. Tuiles visibles dans le viewport.
3. Tuiles voisines dans une marge de préchargement.
4. Données interactives visibles : hives, territoires, routes, ressources.
5. Détails secondaires : labels longs, animations, effets, décor enrichi.

Marge de préchargement :

```text
preloadMargin = 1 tuile autour du viewport à zoom faible
preloadMargin = 2 tuiles autour du viewport à zoom fort
```

Éviction :

- garder les tuiles visibles ;
- garder les tuiles du dernier viewport pendant quelques secondes ;
- évincer les tuiles éloignées et les détails secondaires d’abord ;
- ne pas évincer les données nécessaires à la sélection courante.

État recommandé par tuile :

```text
Unloaded
Queued
Loading
ReadyLowRes
ReadyHighRes
FailedFallback
Evicting
```

Transitions :

```text
viewport change -> compute visible tile set
missing visible tile -> queue high priority
neighbor tile -> queue medium priority
far tile -> keep if budget allows, otherwise evict
load failure -> show parent zoom tile or low-res fallback
```

## Format De Données Proposé

Le JSON Builder-B actuel peut évoluer vers un paquet par région.

### Index Monde

```json
{
  "schema": "bee-kingdom.world-map-index.v1",
  "worldId": "mmo-world-v001",
  "coordinateSystem": {
    "space": "world-normalized",
    "origin": "top-left",
    "xRange": [0, 1],
    "yRange": [0, 1]
  },
  "tilePyramid": {
    "tileSizePx": 512,
    "zoomLevels": [0, 1, 2, 3],
    "maxResidentTilesMobile": 24
  },
  "regions": [
    {
      "id": "region_01_02",
      "bounds": { "x": 0.25, "y": 0.3333, "width": 0.25, "height": 0.3333 },
      "dataPath": "regions/region_01_02.json"
    }
  ]
}
```

### Paquet Région

```json
{
  "schema": "bee-kingdom.world-map-region.v1",
  "regionId": "region_01_02",
  "bounds": { "x": 0.25, "y": 0.3333, "width": 0.25, "height": 0.3333 },
  "hives": [],
  "territories": [],
  "routes": [],
  "resources": [],
  "wonders": [],
  "hostileNests": [],
  "neutralZones": [],
  "pointsOfInterest": []
}
```

### Hive

```json
{
  "id": "hive_goldenheart",
  "serverId": null,
  "nameKey": "world.hive.goldenheart",
  "ownership": "player-preview",
  "position": { "x": 0.448, "y": 0.390 },
  "regionId": "region_01_01",
  "territoryId": "territory_goldenheart",
  "marker": {
    "icon": "hive_player",
    "minZoomVisible": 0,
    "labelMinZoom": 1,
    "tapRadiusDp": 44
  }
}
```

### Territoire

```json
{
  "id": "territory_goldenheart",
  "ownerHiveId": "hive_goldenheart",
  "allianceId": null,
  "state": "player-preview",
  "polygon": [
    { "x": 0.320, "y": 0.300 },
    { "x": 0.430, "y": 0.250 }
  ],
  "visual": {
    "fillStyle": "player",
    "borderStyle": "soft-gold",
    "minZoomVisible": 0
  }
}
```

### Route / Flight Path

```json
{
  "id": "route_goldenheart_northern",
  "routeType": "flight-path-preview",
  "fromHiveId": "hive_goldenheart",
  "toHiveId": "hive_northern",
  "points": [
    { "x": 0.448, "y": 0.390 },
    { "x": 0.389, "y": 0.089 }
  ],
  "minZoomVisible": 1,
  "serverAuthorityRequired": true
}
```

### Ressource

```json
{
  "id": "resource_north_pollen_field",
  "resourceType": "pollen",
  "position": { "x": 0.540, "y": 0.170 },
  "regionId": "region_00_02",
  "minZoomVisible": 1,
  "tapRadiusDp": 44,
  "officialYield": null
}
```

### Merveille / Nid Hostile / Point D’intérêt

```json
{
  "id": "wonder_frost_spire",
  "kind": "wonder",
  "position": { "x": 0.704, "y": 0.168 },
  "regionId": "region_00_02",
  "minZoomVisible": 0,
  "detailDataPath": "poi/wonder_frost_spire.json"
}
```

## Contraintes Pan / Zoom / Minimap

Règles principales :

- le pan/zoom transforme seulement le `MapCameraLayer` ;
- le HUD, les menus, la légende, les panneaux et la minimap restent fixes ;
- les coordonnées de hit-test sont calculées par transformation inverse écran -> monde ;
- les halos utilisent la même source de données que les hotspots ;
- les labels changent de densité selon le zoom ;
- les markers gardent une taille tactile minimale en dp même si leur position suit la carte.

Limites de zoom recommandées :

```text
zoomMin = vue monde complète, fond basse résolution
zoomDefault = cadre autour de la ruche joueur + territoires proches
zoomMaxMobile = 3.0 à 3.5 tant que les tuiles restent 512/1024 px
zoomMaxTablet = 4.0 possible si tuiles haute résolution disponibles
```

Minimap :

- ne charge pas ses propres données officielles ;
- utilise l’index monde et la position caméra principale ;
- affiche un rectangle de viewport projeté ;
- ne devient jamais la source de vérité des coordonnées ;
- peut utiliser des tuiles très basse résolution ou une image séparée simplifiée.

Formule de viewport minimap :

```text
minimapRect.x = cameraWorldMinX
minimapRect.y = cameraWorldMinY
minimapRect.width = cameraWorldMaxX - cameraWorldMinX
minimapRect.height = cameraWorldMaxY - cameraWorldMinY
```

## Règles De Densité Par Zoom

```text
zoom 0:
  territoires majeurs
  hives principales
  merveilles majeures
  pas de petits champs de ressources

zoom 1:
  routes principales
  champs de ressources importants
  nids hostiles majeurs
  labels courts

zoom 2:
  points d’intérêt
  ressources secondaires
  routes secondaires
  contours plus précis

zoom 3+:
  détails locaux
  labels complets
  micro-interactions futures
```

La densité doit être pilotée par données :

```json
{
  "minZoomVisible": 1,
  "labelMinZoom": 2,
  "detailMinZoom": 3
}
```

## Risques De Performance Mobile

Risques principaux :

- trop de grosses textures résidentes ;
- trop de tuiles chargées simultanément après un zoom rapide ;
- labels générant trop de Canvas rebuilds ;
- polygones de territoires trop détaillés ;
- halos et contours avec effets coûteux ;
- chargement synchrone qui provoque des saccades ;
- minimap rendue comme une deuxième carte complète ;
- surdraw élevé avec couches semi-transparentes ;
- mémoire GPU instable sur appareils Android bas/milieu de gamme.

Mesures recommandées :

- limiter les tuiles résidentes par budget mobile ;
- charger en asynchrone ;
- utiliser une tuile parent basse résolution pendant le chargement ;
- regrouper les overlays statiques par région ;
- simplifier les polygones selon le zoom ;
- cacher les labels non essentiels à zoom faible ;
- garder les halos en traits simples screen-space ;
- éviter les ombres/flous dynamiques sur des centaines de markers ;
- profiler sur téléphone portrait et tablette paysage.

Budgets initiaux proposés :

```text
tileSize mobile: 512 px
tileSize tablette haute: 1024 px si mémoire suffisante
maxResidentTilesMobile: 16 à 24
maxResidentTilesTablet: 24 à 40
maxVisibleLabelsMobile: 20 à 35
maxVisibleLabelsTablet: 40 à 70
maxTerritoryVerticesVisibleMobile: 1500 à 2500
```

## Plan De Transition Pour Builder-A

1. Garder la carte actuelle comme référence visuelle.
2. Exporter une version sans HUD.
3. Définir l’index monde `world-map-index.v1`.
4. Convertir le JSON Builder-B actuel en paquet global.
5. Découper en régions sans changer les ids.
6. Générer une première pyramide de tuiles `z0/z1/z2`.
7. Créer un loader Editor/debug-only.
8. Valider pan/zoom/minimap avec tuiles locales.
9. Ajouter les données serveur seulement après contrat serveur.
10. Remplacer progressivement les données preview par des données autorisées.

## Checklist Builder-A

- Confirmer le repère monde normalisé.
- Confirmer le découpage régions/chunks.
- Séparer carte, HUD, légende et minimap.
- Garder les ids stables entre preview et futur serveur.
- Définir les budgets de tuiles résidentes.
- Définir les niveaux de zoom et densités.
- Prévoir fallback basse résolution.
- Prévoir un overlay debug pour tuiles visibles/chargées.
- Tester pan rapide, zoom rapide et changement orientation.
- Vérifier que la sélection reste alignée après chargement/éviction de tuiles.
- Ne pas afficher de données officielles sans serveur.

## Non-Claims

Cette stratégie ne rend pas la carte MMO scalable fonctionnelle dans le jeu. Elle ne modifie pas la scène, ne modifie pas le runtime et ne valide aucun lot QA. Elle prépare uniquement le chantier technique pour Builder-A.
