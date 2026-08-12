# Builder-B - Support ruche playable loop

Statut : préparation hors runtime  
Date : 2026-07-12  
Portée : ruche uniquement  
Intégration : réservée à Builder-A  

Ce document prépare le support technique du playable loop de ruche sans brancher de logique dans le jeu officiel. Les valeurs de coûts, durées, états et suggestions ci-dessous sont des valeurs de cadrage UX et de test de données. Elles ne doivent pas être considérées comme équilibrage final.

## Objectif

Préparer pour Builder-A une base exploitable pour :

- associer les 14 zones officielles de ruche à des bâtiments ou emplacements de bâtiments ;
- prévisualiser coûts et durées d'amélioration ;
- normaliser les états visuels d'un bâtiment ;
- alimenter un futur panneau détail ;
- proposer des suggestions d'amélioration sans imposer la logique officielle de gameplay.

## Sources préparatoires

- Zones de ruche : `Docs/BuilderB/hive_click_zones_v001.sample.json`
- Viewer zones/halo : `Docs/BuilderB/hive_click_zone_overlay_viewer.html`
- Support cadrage/zones : `Docs/BuilderB/HiveViewport_ClickZones_BuilderA_Support.md`
- Bâtiments existants observés côté projet :
  - `queens_chamber`
  - `honey_storage`
  - `flower_garden`
  - `wax_workshop`
  - `barracks`

## Principes de données

Le support playable loop devrait rester séparé des zones cliquables. Une zone décrit où l'utilisateur touche. Un bâtiment décrit ce qui est affiché et quelles actions sont proposées.

Schéma recommandé :

```json
{
  "schema": "bee-kingdom.hive-playable-loop-preview.v1",
  "status": "builder-b-prep-only",
  "runtimeBindingAllowed": false,
  "balanceAuthority": "preview-only-not-official",
  "costFormula": {
    "costMultiplierPerLevel": 1.5,
    "durationMultiplierPerLevel": 1.2,
    "costForLevel": "round(baseCost * pow(1.5, targetLevel - 1))",
    "durationForLevel": "round(baseDurationSeconds * pow(1.2, targetLevel - 1))"
  },
  "states": [
    "idle",
    "selected",
    "upgrading",
    "completed",
    "blocked"
  ],
  "buildings": []
}
```

## Etats bâtiment

| Etat | Rôle UX | Halo/frontière | Panneau détail | Interaction recommandée |
| --- | --- | --- | --- | --- |
| `idle` | Etat neutre, aucune action en cours | Aucun halo ou contour très discret | Fermé sauf si zone ciblée | Tap ouvre `selected` si la zone est active |
| `selected` | Zone choisie par le joueur | Halo visible aligné à la zone, intensité moyenne | Ouvert avec niveau, rôle, coût, durée | Bouton amélioration si non bloqué |
| `upgrading` | Amélioration en cours | Halo animé lent ou contour progressif | Progression, temps restant, action secondaire limitée | Pas de nouvelle amélioration concurrente sur le même bâtiment |
| `completed` | Amélioration prête à valider ou feedback bref | Pulse court, puis retour vers `idle` | Résumé gain niveau + bénéfice | Tap ou auto-retour selon choix Builder-A |
| `blocked` | Action indisponible | Halo rouge/orange très sobre ou icône verrou | Raisons bloquantes lisibles | Pas d'appel serveur, pas de mutation locale officielle |

Règle importante : le passage `selected -> upgrading -> completed` doit rester piloté par la logique officielle de Builder-A. Builder-B prépare seulement les états attendus et les données nécessaires au panneau.

## Matrice des 14 zones

Les coûts et durées sont des previews. Quand un bâtiment existe déjà dans les ScriptableObjects, la ligne reprend son identifiant et ses valeurs observées comme point de départ. Les autres lignes sont des propositions de support pour panneau et suggestion.

| Ordre | Zone officielle | `zoneId` | Bâtiment proposé | Source | Rôle preview | Coût base preview | Durée base | Etat initial recommandé |
| ---: | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Nurserie | `nursery_cluster` | `nursery` | proposé | Déblocage/accélération couvain | miel 180, pollen 80 | 18 s | `idle` |
| 2 | Reserve miel | `honey_storage` | `honey_storage` | existant | Capacité miel | miel 75, cire 5 | 12 s | `idle` |
| 3 | Caserne | `guard_post` | `barracks` | existant | Défense de base | miel 300, cire 20 | 25 s | `idle` |
| 4 | Defense | `defense_growth` | `defense_growth` | proposé | Bonus défense/fortification | miel 360, cire 35 | 35 s | `blocked` |
| 5 | Genetique | `genetics_garden` | `flower_garden` ou `genetics_garden` | existant/proposé | Pollen puis évolutions futures | miel 150 | 15 s | `idle` si Flower Garden retenu, sinon `blocked` |
| 6 | Recherche | `research_node` | `research_node` | proposé | Recherche passive/future | miel 450, cire 80, pollen 120 | 45 s | `blocked` |
| 7 | Entrepot | `warehouse_cells` | `warehouse` | proposé | Capacité multi-ressources | miel 220, cire 30 | 24 s | `idle` |
| 8 | Transformation | `wax_workshop` | `wax_workshop` | existant | Production cire | miel 250, pollen 60 | 20 s | `idle` |
| 9 | Infirmerie | `infirmary_grove` | `infirmary` | proposé | Soin/récupération abeilles | miel 280, pollen 110 | 30 s | `blocked` |
| 10 | Academie | `academy_canopy` | `academy` | proposé | Formation/bonus abeilles | miel 400, pollen 160, cire 40 | 40 s | `blocked` |
| 11 | Banque | `hive_bank` | `hive_bank` | proposé serveur futur | Stockage/échanges futurs | miel 500, cire 100 | 60 s | `blocked` |
| 12 | Administration | `administration_core` | `queens_chamber` | existant | Coeur de progression | miel 100 | 10 s | `idle` |
| 13 | Archives | `archives_honeyfall` | `archives` | proposé | Historique/lore/bonus passifs | miel 320, cire 70 | 38 s | `blocked` |
| 14 | Centre alliance | `alliance_future_hall` | `alliance_hall` | proposé serveur futur | Alliance/social futur | miel 600, cire 140, pollen 220 | 75 s | `blocked` |

## Exemple JSON draft

```json
{
  "schema": "bee-kingdom.hive-playable-loop-preview.v1",
  "status": "builder-b-prep-only",
  "runtimeBindingAllowed": false,
  "balanceAuthority": "preview-only-not-official",
  "resources": ["honey", "pollen", "wax", "royal_jelly"],
  "stateModel": {
    "allowed": ["idle", "selected", "upgrading", "completed", "blocked"],
    "defaultState": "idle"
  },
  "costFormula": {
    "costMultiplierPerLevel": 1.5,
    "durationMultiplierPerLevel": 1.2
  },
  "buildings": [
    {
      "zoneId": "administration_core",
      "buildingId": "queens_chamber",
      "displayName": "Administration",
      "source": "existing-building-so",
      "role": "core_progression",
      "maxLevelPreview": 10,
      "baseUpgradePreview": {
        "targetLevel": 2,
        "cost": {"honey": 100},
        "durationSeconds": 10
      },
      "statePreview": "idle",
      "detailPanel": {
        "primaryMetric": "Niveau de ruche",
        "secondaryMetric": "Déblocages à venir",
        "cta": "Améliorer"
      }
    },
    {
      "zoneId": "honey_storage",
      "buildingId": "honey_storage",
      "displayName": "Reserve miel",
      "source": "existing-building-so",
      "role": "storage",
      "maxLevelPreview": 10,
      "baseUpgradePreview": {
        "targetLevel": 2,
        "cost": {"honey": 75, "wax": 5},
        "durationSeconds": 12
      },
      "capacityPreview": {
        "resource": "honey",
        "baseBonus": 5000,
        "increasePerLevel": 2500
      },
      "statePreview": "idle",
      "detailPanel": {
        "primaryMetric": "Capacité miel",
        "secondaryMetric": "+2500 par niveau preview",
        "cta": "Améliorer"
      }
    },
    {
      "zoneId": "wax_workshop",
      "buildingId": "wax_workshop",
      "displayName": "Transformation",
      "source": "existing-building-so",
      "role": "production",
      "maxLevelPreview": 10,
      "baseUpgradePreview": {
        "targetLevel": 2,
        "cost": {"honey": 250, "pollen": 60},
        "durationSeconds": 20
      },
      "productionPreview": {
        "resource": "wax",
        "baseRate": 1,
        "increasePerLevel": 1
      },
      "statePreview": "idle",
      "detailPanel": {
        "primaryMetric": "Production cire",
        "secondaryMetric": "+1 par niveau preview",
        "cta": "Améliorer"
      }
    },
    {
      "zoneId": "guard_post",
      "buildingId": "barracks",
      "displayName": "Caserne",
      "source": "existing-building-so",
      "role": "defense",
      "maxLevelPreview": 5,
      "baseUpgradePreview": {
        "targetLevel": 2,
        "cost": {"honey": 300, "wax": 20},
        "durationSeconds": 25
      },
      "statePreview": "idle",
      "detailPanel": {
        "primaryMetric": "Défense",
        "secondaryMetric": "Entraînement futur",
        "cta": "Améliorer"
      }
    },
    {
      "zoneId": "research_node",
      "buildingId": "research_node",
      "displayName": "Recherche",
      "source": "builder-b-proposed",
      "role": "future_unlock",
      "statePreview": "blocked",
      "blockedReasons": [
        "feature_not_integrated",
        "requires_architect_approval"
      ],
      "baseUpgradePreview": {
        "targetLevel": 1,
        "cost": {"honey": 450, "wax": 80, "pollen": 120},
        "durationSeconds": 45
      },
      "detailPanel": {
        "primaryMetric": "Recherche future",
        "secondaryMetric": "Préparation uniquement",
        "cta": "Indisponible"
      }
    }
  ]
}
```

## Données panneau détail

Le panneau détail peut être alimenté par un objet de vue unique, dérivé de la zone sélectionnée et du bâtiment associé.

```json
{
  "zoneId": "wax_workshop",
  "buildingId": "wax_workshop",
  "displayName": "Transformation",
  "level": 3,
  "state": "selected",
  "role": "production",
  "summary": "Produit de la cire pour les bâtiments.",
  "productionPreview": {
    "resource": "wax",
    "currentRate": 3,
    "nextRate": 4
  },
  "capacityPreview": null,
  "upgradePreview": {
    "targetLevel": 4,
    "cost": {"honey": 563, "pollen": 135},
    "durationSeconds": 35,
    "requirements": [],
    "blockedReasons": []
  },
  "ctaState": {
    "label": "Améliorer",
    "enabled": true,
    "disabledReason": null
  },
  "selectionVisual": {
    "haloStyle": "zone_contour_soft",
    "anchor": "zone_polygon_centroid"
  }
}
```

Champs minimum recommandés :

- `zoneId` : liaison avec les zones cliquables Builder-B.
- `buildingId` : liaison avec les données de bâtiment.
- `displayName` : nom affiché validé plus tard par UI/Architecte.
- `level` : niveau courant fourni par la logique officielle.
- `state` : état de rendu.
- `upgradePreview` : coût, durée, exigences et raisons de blocage.
- `selectionVisual` : style de halo/contour et ancrage.

## Suggestions d'amélioration

Les suggestions doivent rester explicables et non intrusives. Elles ne doivent pas déclencher d'amélioration automatiquement.

Format recommandé :

```json
{
  "suggestions": [
    {
      "id": "suggest_storage_when_honey_full",
      "priority": 90,
      "zoneId": "honey_storage",
      "buildingId": "honey_storage",
      "reason": "honey_capacity_near_full",
      "titleKey": "suggestion.hive.honeyStorage",
      "actionPreview": "upgrade",
      "blockedReasons": []
    },
    {
      "id": "suggest_wax_when_upgrade_blocked_by_wax",
      "priority": 82,
      "zoneId": "wax_workshop",
      "buildingId": "wax_workshop",
      "reason": "wax_shortage_for_next_upgrade",
      "actionPreview": "upgrade",
      "blockedReasons": []
    },
    {
      "id": "suggest_core_when_unlocks_available",
      "priority": 95,
      "zoneId": "administration_core",
      "buildingId": "queens_chamber",
      "reason": "core_unlock_available",
      "actionPreview": "upgrade",
      "blockedReasons": []
    }
  ]
}
```

Règles de priorité proposées :

| Condition | Zone suggérée | Priorité preview | Note |
| --- | --- | ---: | --- |
| Miel proche de la limite | `honey_storage` | 90 | Evite la frustration de ressources perdues |
| Manque de cire pour plusieurs actions | `wax_workshop` | 82 | Oriente vers la production |
| Nouvelle progression globale disponible | `administration_core` | 95 | Priorité forte, car coeur de progression |
| Défense basse ou alerte hostile future | `guard_post` puis `defense_growth` | 70 | A garder inactif tant que la boucle combat n'est pas officielle |
| Pollen limitant le recrutement/amélioration | `genetics_garden` ou `nursery_cluster` | 75 | Selon décision Builder-A sur `flower_garden` |
| Fonction future/verrouillée | zone concernée | 30 | Afficher en découverte, pas comme action urgente |

## Halo et sélection

Le halo doit suivre la zone, pas un rectangle de panneau.

Recommandation :

- `idle` : pas de halo, ou contour très léger seulement en mode debug.
- `selected` : contour plein doux, couleur neutre chaude, alpha stable.
- `upgrading` : contour identique + pulse lent ou segment progressif.
- `completed` : pulse court de validation, puis retour neutre.
- `blocked` : contour discret avec feedback de blocage, sans alerte agressive.

Le halo doit utiliser la même source géométrique que le hit-test :

1. masque alpha final si disponible ;
2. polygone fin si le masque n'est pas encore produit ;
3. contour de debug seulement dans le viewer, jamais comme vérité gameplay.

## Matrice centre / bord / hors-zone

| Cas de tap | Résultat attendu | Notes |
| --- | --- | --- |
| Centre d'une zone active | Sélection du bâtiment | Ouvre le panneau détail |
| Bord intérieur d'une zone | Sélection si dans masque/polygone | Tolérance visuelle de 3 px cible |
| Frontière entre deux zones | Résolution par priorité puis plus petite surface | Evite les sélections instables |
| Hors zone mais proche | Aucun bâtiment, ou fermeture panneau selon UX | Ne pas agrandir au point de casser le pixel-perfect |
| Pendant pan | Aucune sélection | Seuil anti-sélection à conserver depuis ARCH-166 |
| Pendant pinch zoom | Aucune sélection | Le HUD reste fixe |

## Support JSON futur

Fichiers proposés, à créer uniquement quand Builder-A valide l'intégration :

```text
Docs/BuilderB/HivePlayableLoop/
  hive_building_matrix.preview.json
  hive_building_states.preview.json
  hive_upgrade_suggestions.preview.json
  README.md
```

Fichiers runtime possibles plus tard, côté Builder-A uniquement :

```text
Assets/_Project/Data/Hive/
  HiveBuildingMatrix.asset ou hive_building_matrix.json
  HiveUpgradeRules.asset ou hive_upgrade_rules.json
```

## Checklist Builder-A

- Valider les 14 associations `zoneId -> buildingId`.
- Décider si `flower_garden` reste lié à `genetics_garden`, à `nursery_cluster`, ou à une autre zone.
- Remplacer les coûts/durées preview par les valeurs d'équilibrage officielles.
- Brancher les états sur la logique officielle d'amélioration, pas sur les données Builder-B.
- Utiliser une seule source géométrique pour hit-test et halo.
- Vérifier qu'un pan ou pinch ne déclenche jamais une sélection.
- Garder HUD, menus et panneaux hors de la couche zoomée de ruche.
- Ajouter les raisons de blocage réelles : ressources, niveau requis, max level, feature future, serveur.
- Valider les microcopies avec UI/Architecte avant affichage final.

## Limites explicites

- Aucun élément de ce document n'est intégré au runtime.
- Les coûts et durées ne sont pas officiels.
- Les zones future/server restent bloquées tant que Builder-A et l'Architecte ne les activent pas.
- Le document prépare du matériel exploitable ; il ne déclare pas la boucle ruche fonctionnelle dans le jeu.
