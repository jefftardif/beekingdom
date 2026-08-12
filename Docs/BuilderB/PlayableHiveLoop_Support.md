# Builder-B - Playable Hive Loop Support

Statut : préparation hors runtime  
Date : 2026-07-12  
Priorité : Ruche uniquement  
Intégration : réservée à Builder-A  

Ce document prépare une aide d'intégration pour Builder-A sur la boucle jouable de la ruche. Il ne modifie pas la scène principale, ne branche aucun runtime, ne change aucun équilibrage officiel et ne déclare pas la fonctionnalité comme intégrée au jeu.

## Objectif

Préparer une structure claire pour :

- associer les 14 zones officielles de ruche à des bâtiments ;
- prévoir les coûts, durées, ressources et états d'amélioration ;
- soutenir le panneau détail bâtiment ;
- préparer les suggestions d'amélioration ;
- cadrer l'entraînement de troupes : Soldats, Gardiennes, Éclaireuses ;
- éviter les boutons sans feedback.

## Sources préparatoires

- Zones cliquables : `Docs/BuilderB/hive_click_zones_v001.sample.json`
- Viewer halo/selection : `Docs/BuilderB/hive_click_zone_overlay_viewer.html`
- Cadrage ruche : `Docs/BuilderB/HiveViewport_ClickZones_BuilderA_Support.md`
- Bâtiments existants observés : `queens_chamber`, `honey_storage`, `flower_garden`, `wax_workshop`, `barracks`

Les valeurs preview ci-dessous sont volontairement non officielles. Elles servent à tester la lisibilité de l'interface, la cohérence des états et les besoins du panneau détail.

## Format de données recommandé

```json
{
  "schema": "bee-kingdom.playable-hive-loop-support.v1",
  "status": "builder-b-prep-only",
  "runtimeBindingAllowed": false,
  "balanceAuthority": "preview-only-not-official",
  "resources": ["honey", "pollen", "wax", "royal_jelly"],
  "buildingStates": ["idle", "selected", "upgrading", "completed", "blocked"],
  "trainingQueueStates": ["empty", "queued", "training", "ready", "blocked", "full"],
  "costFormulaPreview": {
    "costMultiplierPerLevel": 1.5,
    "durationMultiplierPerLevel": 1.2,
    "costForLevel": "round(baseCost * pow(1.5, targetLevel - 1))",
    "durationForLevel": "round(baseDurationSeconds * pow(1.2, targetLevel - 1))"
  },
  "buildings": [],
  "training": []
}
```

## Matrice bâtiments de ruche

| Ordre | Zone officielle | `zoneId` | Bâtiment proposé | Source | Ressource/effet produit | Coût base preview | Durée base | Etat initial |
| ---: | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Nurserie | `nursery_cluster` | `nursery` | proposé | Accélération couvain/recrutement futur | miel 180, pollen 80 | 18 s | `idle` |
| 2 | Reserve miel | `honey_storage` | `honey_storage` | existant | Capacité miel +5000, +2500/niv | miel 75, cire 5 | 12 s | `idle` |
| 3 | Caserne | `guard_post` | `barracks` | existant | Débloque/entraîne troupes preview | miel 300, cire 20 | 25 s | `idle` |
| 4 | Defense | `defense_growth` | `defense_growth` | proposé | Bonus défense/fortification | miel 360, cire 35 | 35 s | `blocked` |
| 5 | Genetique | `genetics_garden` | `flower_garden` ou `genetics_garden` | existant/proposé | Pollen +3, +1/niv si Flower Garden | miel 150 | 15 s | `idle` ou `blocked` selon choix |
| 6 | Recherche | `research_node` | `research_node` | proposé | Déblocages passifs futurs | miel 450, pollen 120, cire 80 | 45 s | `blocked` |
| 7 | Entrepot | `warehouse_cells` | `warehouse` | proposé | Capacité multi-ressources | miel 220, cire 30 | 24 s | `idle` |
| 8 | Transformation | `wax_workshop` | `wax_workshop` | existant | Cire +1, +1/niv | miel 250, pollen 60 | 20 s | `idle` |
| 9 | Infirmerie | `infirmary_grove` | `infirmary` | proposé | Récupération troupes future | miel 280, pollen 110 | 30 s | `blocked` |
| 10 | Academie | `academy_canopy` | `academy` | proposé | Bonus entraînement futur | miel 400, pollen 160, cire 40 | 40 s | `blocked` |
| 11 | Banque | `hive_bank` | `hive_bank` | serveur futur | Stockage/échanges futurs | miel 500, cire 100 | 60 s | `blocked` |
| 12 | Administration | `administration_core` | `queens_chamber` | existant | Progression/déblocages ruche | miel 100 | 10 s | `idle` |
| 13 | Archives | `archives_honeyfall` | `archives` | proposé | Bonus passifs/lore futurs | miel 320, cire 70 | 38 s | `blocked` |
| 14 | Centre alliance | `alliance_future_hall` | `alliance_hall` | serveur futur | Alliance/social futur | miel 600, pollen 220, cire 140 | 75 s | `blocked` |

## Etats bâtiment

| Etat | Définition | Feedback attendu | Action autorisée |
| --- | --- | --- | --- |
| `idle` | Bâtiment disponible sans sélection | Aucun halo ou contour très discret | Tap zone -> `selected` |
| `selected` | Bâtiment actif dans le panneau détail | Halo aligné au masque/polygone + panneau ouvert | Améliorer, entraîner, voir détails selon bâtiment |
| `upgrading` | Amélioration en cours | Barre de progression, temps restant, halo lent | Annuler seulement si Builder-A l'autorise |
| `completed` | Amélioration prête ou feedback de fin | Pulse court, son/animation légère, panneau actualisé | Collecter/valider ou auto-retour |
| `blocked` | Action impossible | Raison claire, bouton désactivé, pas de clic muet | Montrer condition manquante |

Règle de sécurité : un pan, un pinch zoom ou une ouverture de panneau ne doit jamais déclencher une amélioration, un entraînement ou une collecte.

## Panneau détail

Le panneau détail doit être alimenté par un objet unique dérivé de `zoneId + buildingId + state`.

```json
{
  "zoneId": "guard_post",
  "buildingId": "barracks",
  "displayName": "Caserne",
  "level": 2,
  "state": "selected",
  "role": "training",
  "resourceOutput": null,
  "upgradePreview": {
    "targetLevel": 3,
    "cost": {"honey": 450, "wax": 30},
    "durationSeconds": 30,
    "blockedReasons": []
  },
  "trainingPreview": {
    "queueState": "training",
    "availableTroops": ["soldier", "guardian", "scout"],
    "queueSlots": 2
  },
  "ctaState": {
    "primary": {"label": "Améliorer", "enabled": true},
    "secondary": {"label": "Entraîner", "enabled": true}
  }
}
```

Champs recommandés :

- `displayName` : nom validé plus tard par UI/Architecte.
- `level` : niveau courant officiel.
- `state` : état visuel et interactif.
- `resourceOutput` : production/capacité quand applicable.
- `upgradePreview` : coût, durée, niveau cible, raisons de blocage.
- `trainingPreview` : seulement pour Caserne/Académie ou bâtiments validés.
- `ctaState` : libellé, activation, raison si désactivé.

## Entraînement troupes

L'entraînement est préparé comme une boucle séparée de l'amélioration bâtiment. La Caserne est le point d'entrée naturel. L'Académie peut devenir un multiplicateur futur, mais ne doit pas être branchée sans validation.

| Troupe | `troopId` | Zone proposée | Rôle preview | Coût unitaire preview | Durée unitaire preview | Condition preview |
| --- | --- | --- | --- | --- | --- | --- |
| Soldats | `soldier` | `guard_post` | Défense standard | miel 40, pollen 15 | 20 s | Caserne niv. 1 |
| Gardiennes | `guardian` | `guard_post` | Défense lourde | miel 75, pollen 25, cire 8 | 35 s | Caserne niv. 2 |
| Éclaireuses | `scout` | `guard_post` puis `academy_canopy` futur | Exploration/alerte | miel 55, pollen 35 | 28 s | Caserne niv. 2, Académie future optionnelle |

Format preview :

```json
{
  "training": [
    {
      "troopId": "soldier",
      "displayName": "Soldats",
      "buildingId": "barracks",
      "zoneId": "guard_post",
      "baseCost": {"honey": 40, "pollen": 15},
      "baseDurationSeconds": 20,
      "batchSizeMin": 1,
      "batchSizeMaxPreview": 10,
      "requirements": [{"buildingId": "barracks", "level": 1}]
    },
    {
      "troopId": "guardian",
      "displayName": "Gardiennes",
      "buildingId": "barracks",
      "zoneId": "guard_post",
      "baseCost": {"honey": 75, "pollen": 25, "wax": 8},
      "baseDurationSeconds": 35,
      "batchSizeMin": 1,
      "batchSizeMaxPreview": 6,
      "requirements": [{"buildingId": "barracks", "level": 2}]
    },
    {
      "troopId": "scout",
      "displayName": "Éclaireuses",
      "buildingId": "barracks",
      "zoneId": "guard_post",
      "baseCost": {"honey": 55, "pollen": 35},
      "baseDurationSeconds": 28,
      "batchSizeMin": 1,
      "batchSizeMaxPreview": 8,
      "requirements": [{"buildingId": "barracks", "level": 2}]
    }
  ]
}
```

## Etats de file d'entraînement

| Etat file | Définition | Feedback attendu | Boutons |
| --- | --- | --- | --- |
| `empty` | Aucune troupe en attente | Zone Caserne neutre, panneau propose les troupes | `Entraîner` actif si ressources suffisantes |
| `queued` | Des lots attendent leur tour | Liste compacte des lots, ordre visible | Ajouter si place disponible |
| `training` | Un lot est en cours | Barre de progression + temps restant | Pas de double clic silencieux |
| `ready` | Lot terminé | Badge clair sur Caserne, bouton collecter/valider | Action de récupération visible |
| `blocked` | Ressources, niveau ou prérequis manquants | Raison précise sous le bouton | Bouton désactivé avec raison |
| `full` | File au maximum | Message court, pas d'ajout possible | Bouton ajouter désactivé |

Règles de file recommandées :

- La file d'entraînement ne doit pas bloquer l'amélioration de la ruche entière, sauf décision Builder-A.
- L'amélioration de la Caserne pendant une file active doit être explicitement définie : autorisée avec file conservée, ou bloquée avec raison claire.
- Le panneau doit toujours montrer le prochain événement : entraînement terminé, prochain lot, ou blocage.

## Suggestions zones/bâtiments

Les suggestions doivent guider sans automatiser.

| Situation | Suggestion | Zone | Priorité preview | Feedback |
| --- | --- | --- | ---: | --- |
| Miel proche du plafond | Améliorer Reserve miel | `honey_storage` | 90 | Halo doux + carte suggestion |
| Manque de cire pour upgrades | Améliorer Transformation | `wax_workshop` | 82 | Montrer la ressource manquante |
| Progression principale disponible | Améliorer Administration | `administration_core` | 95 | Suggestion prioritaire mais non bloquante |
| Pas assez de troupes | Entraîner Soldats | `guard_post` | 78 | Badge Caserne, panneau entraînement |
| Défense insuffisante future | Gardiennes ou Defense | `guard_post` / `defense_growth` | 70 | Bloquer si système combat non validé |
| Exploration future disponible | Éclaireuses | `guard_post` | 55 | Marquer preview/future si non intégré |
| Pollen insuffisant | Améliorer Flower Garden/Nurserie | `genetics_garden` ou `nursery_cluster` | 75 | Selon mapping retenu par Builder-A |
| Fonction future | Recherche, Banque, Alliance | zones concernées | 30 | Affichage découverte, pas CTA fort |

Format :

```json
{
  "id": "suggest_train_soldiers_low_defense",
  "priority": 78,
  "zoneId": "guard_post",
  "buildingId": "barracks",
  "actionType": "train",
  "targetId": "soldier",
  "reason": "troop_count_low",
  "blockedReasons": []
}
```

## Feedback attendu par action

| Action | Feedback immédiat | Feedback persistant | Cas bloqué |
| --- | --- | --- | --- |
| Sélection zone | Halo + panneau détail | Zone reste sélectionnée | Si zone future : panneau bloqué lisible |
| Améliorer bâtiment | Déduction preview/validation officielle, transition `upgrading` | Barre + temps restant | Montrer ressources/prérequis manquants |
| Fin amélioration | Pulse court + niveau actualisé | Retour `idle` ou `completed` court | Aucun bouton silencieux |
| Entraîner troupe | Ajout visible dans la file | Barre sur lot actif | Montrer manque de ressource/place/niveau |
| Fin entraînement | Badge Caserne + lot prêt | Compteur troupe actualisé après validation | Si récupération impossible, raison claire |
| Suggestion tap | Sélectionne zone/bâtiment concerné | Panneau détail contextualisé | Si suggestion obsolète, la retirer |
| Tap hors zone | Ferme ou conserve panneau selon UX | Aucun changement gameplay | Aucun effet caché |
| Pan/zoom | Déplacement/zoom seulement | HUD fixe, halos alignés | Aucune sélection/action |

## Boutons muets interdits

Un bouton est interdit s'il reçoit un tap sans produire de feedback visible, sans raison de blocage, ou sans changement d'état perceptible.

Liste à surveiller :

- Bouton `Améliorer` actif alors que les ressources sont insuffisantes.
- Bouton `Améliorer` désactivé sans raison affichée.
- Bouton `Entraîner` qui ne crée pas de lot visible dans la file.
- Bouton `Collecter` ou `Terminer` qui ne change ni compteur, ni état, ni badge.
- Bouton de zone future qui ressemble à une action disponible.
- Bouton de suggestion qui ne sélectionne pas la zone concernée.
- Bouton appuyé pendant pan/pinch qui déclenche une action.
- Bouton cliquable sous un panneau ou derrière le HUD.
- Bouton avec cooldown/progression mais sans temps restant.
- Bouton serveur futur sans état `blocked` explicite.

Règle UX : chaque tap doit produire au moins un feedback parmi `state change`, `visual pulse`, `progress update`, `disabled reason`, `selection change`, ou `error reason`.

## Notes Builder-A

- Conserver la séparation couche ruche / HUD / panneaux.
- Utiliser la même géométrie pour hit-test et halo.
- Ne pas mélanger amélioration bâtiment et entraînement dans un seul état implicite.
- Remplacer les coûts/durées preview par l'équilibrage officiel.
- Valider les noms affichés avec UI/Architecte avant intégration.
- Garder les zones serveur/futures en `blocked` tant que les systèmes correspondants ne sont pas officiels.
- Tester centre, bord, hors-zone, pan, pinch, panneau ouvert et panneau fermé.

## Limites

- Ce document est uniquement du matériel de préparation Builder-B.
- Aucun branchement runtime n'a été effectué.
- La scène principale n'est pas modifiée.
- La carte monde n'est pas concernée.
- La boucle ruche n'est pas déclarée fonctionnelle dans le jeu.
