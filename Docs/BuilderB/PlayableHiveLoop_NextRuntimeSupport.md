# Builder-B - Support prochaine passe runtime Ruche

Statut : préparation non conflictuelle  
Date : 2026-07-12  
Priorité : Ruche jouable  
Contexte : ARCH-174 validé, Builder-A en attente Demo/QA  
Portée : documentation de support uniquement  

Ce document prépare la prochaine passe de Builder-A sur la ruche jouable après retour Demo/QA. Il ne modifie ni scène, ni scripts runtime, ni assets, ni APK, ni serveur.

## Références lues

- `C:/projets/beekingdomgame-master/Docs/Architecture/ARCH-174_PlayableHiveLoop_ValidationAndDispatch.md`
- `C:/projets/beekingdomgame-master/Docs/BuilderB/PlayableHiveLoop_Support.md`
- `C:/projets/beekingdom/prompt_ui/rapports/UI-060_PLAYABLE_HIVE_LOOP_UX_SPEC.md`

Synthèse de cadrage :

- ARCH-174 valide une boucle locale preview avec réserves.
- Builder-A doit attendre Demo/QA avant nouvelle modification runtime.
- La prochaine passe doit renforcer la compréhension joueur : ressources, sélection, amélioration, progression, entraînement, feedback.
- Aucun état local ne doit prétendre être une progression MMO officielle.
- Aucun bouton visible ne doit être muet.

## 1. Inventaire des boutons visibles et comportement attendu

Inventaire préparatoire des boutons que Builder-A devra vérifier ou stabiliser après QA.

| Zone UI | Bouton visible | Etat attendu | Comportement attendu | Feedback obligatoire | Cas bloqué |
| --- | --- | --- | --- | --- | --- |
| Panneau bâtiment | `Améliorer` | disponible | Lance une amélioration locale preview du bâtiment sélectionné | Transition vers `upgrading`, coût visible, timer visible, message court | Afficher ressource manquante, bâtiment déjà en amélioration, niveau max ou serveur requis |
| Panneau bâtiment | `Amélioration en cours` | occupé | Ne relance pas une amélioration | Barre de progression + temps restant | Bouton désactivé avec raison |
| Panneau bâtiment | `Terminer` ou `Collecter` | terminé | Valide la fin locale preview si Builder-A conserve cette étape | Niveau actualisé, pulse bref, message succès | Si impossible, raison visible |
| Panneau Caserne | `Entraîner` | disponible | Ouvre ou utilise la section entraînement | Section troupe visible, coût/durée/quantité lisibles | Si serveur requis ou ressources insuffisantes, raison affichée |
| Ligne troupe | `+` quantité | disponible | Augmente la quantité à entraîner | Total coût/durée mis à jour | Si capacité atteinte, désactiver avec `Capacité atteinte` |
| Ligne troupe | `-` quantité | disponible si quantité > min | Réduit la quantité | Total coût/durée mis à jour | Désactivé au minimum, pas de clic muet |
| Panneau entraînement | `Ajouter à la file` | disponible | Ajoute un lot visible dans la file | Message `Formation ajoutée à la file`, lot listé | File pleine, ressource manquante, prérequis absent |
| File entraînement | `Collecter` ou `Récupérer` | lot prêt | Ajoute les troupes au compteur preview | Compteur mis à jour, badge retiré | Si état non prêt, bouton absent ou désactivé avec raison |
| Panneau bâtiment | `Fermer` | toujours | Ferme le panneau sans changer gameplay | Panneau fermé, sélection conservée ou retirée selon règle Builder-A | Aucun effet caché |
| Suggestion | Carte suggestion | disponible | Sélectionne la zone/bâtiment concerné | Halo + panneau synchronisé | Si suggestion obsolète, elle disparaît ou explique |
| Zone future | Bouton action future | bloqué | N'exécute rien | `Serveur requis` ou `En préparation` | Ne jamais sembler disponible |
| HUD ressources | Ressource ou badge | consultation | Montre la ressource, pas nécessairement cliquable | Pulse ou `+X` quand valeur change | Si cliquable, ouvrir panneau utile ou expliquer |

Règle simple : chaque bouton visible doit produire au moins un résultat perceptible parmi changement d'état, message, pulse, ouverture de panneau, progression, raison de blocage.

## 2. Séparation pan/zoom ruche et UI fixe

La ruche zoomable doit être une couche séparée de l'interface fixe.

Structure recommandée :

```text
RootCanvas
  FixedHUDLayer
    TopResources
    PreviewStatus
    Navigation
  HiveViewportLayer
    GestureInputSurface
    HivePanZoomContent
      HiveBackground
      HiveZoneHitTargets
      HiveSelectionHalos
      HiveProgressBadges
  FixedPanelLayer
    BuildingDetailPanel
    TrainingQueuePanel
    FeedbackToasts
```

Règles de routage :

- Un doigt sur la surface ruche : pan uniquement après franchissement du seuil de déplacement.
- Deux doigts sur la surface ruche : pinch zoom uniquement.
- Tap court sans déplacement : sélection de zone si hit-test valide.
- Tap sur HUD, panneau, bouton ou navigation : action UI fixe, jamais pan/zoom ruche.
- Pendant pan ou pinch : bloquer sélection, amélioration, entraînement et collecte.
- Après zoom/pan : halos, badges et progressions restent enfants de `HivePanZoomContent` pour conserver l'alignement.
- HUD, boutons, panneaux et messages restent hors de `HivePanZoomContent`.

Seuils proposés à valider par Builder-A :

| Paramètre | Valeur preview | But |
| --- | ---: | --- |
| Seuil anti-sélection après mouvement | 8 à 12 px écran | Eviter un tap accidentel pendant pan |
| Délai max tap | 180 à 240 ms | Différencier tap et pression hésitante |
| Distance pinch minimale | 8 px entre frames | Eviter zoom tremblant |
| Lissage zoom | 0.12 à 0.18 s | Zoom doux sans retard excessif |
| Lissage pan | 0.08 à 0.14 s | Pan stable, pas flottant |

## 3. Structure d'état locale proposée

Cette structure est pensée pour une boucle locale preview. Elle ne remplace pas les systèmes officiels ni les contrats serveur.

```json
{
  "schema": "bee-kingdom.local-hive-loop-state.preview.v1",
  "status": "preview-only-not-server-authoritative",
  "resources": {
    "honey": {
      "amount": 1240,
      "capacity": 5000,
      "ratePerSecondPreview": 1.5,
      "lastDelta": 12,
      "lastChangedAt": 0.0
    },
    "pollen": {
      "amount": 360,
      "capacity": 2000,
      "ratePerSecondPreview": 0.8,
      "lastDelta": 4,
      "lastChangedAt": 0.0
    },
    "wax": {
      "amount": 120,
      "capacity": 1000,
      "ratePerSecondPreview": 0.25,
      "lastDelta": 1,
      "lastChangedAt": 0.0
    }
  },
  "selection": {
    "zoneId": "honey_storage",
    "buildingId": "honey_storage",
    "selectedAt": 0.0
  },
  "buildings": {
    "honey_storage": {
      "zoneId": "honey_storage",
      "level": 2,
      "state": "selected",
      "upgrade": {
        "status": "available",
        "targetLevel": 3,
        "cost": {"honey": 113, "wax": 8},
        "durationSeconds": 15,
        "startedAt": null,
        "endsAt": null,
        "progress01": 0.0
      },
      "blockedReason": null
    },
    "barracks": {
      "zoneId": "guard_post",
      "level": 1,
      "state": "idle",
      "training": {
        "queueState": "empty",
        "queueCapacity": 2,
        "items": []
      },
      "blockedReason": null
    }
  },
  "training": {
    "troopCounts": {
      "soldier": 4,
      "guardian": 0,
      "scout": 0
    },
    "queuesByBuilding": {
      "barracks": []
    }
  },
  "feedback": {
    "message": null,
    "type": "none",
    "source": null,
    "expiresAt": null
  },
  "blockedReason": null,
  "previewNotice": {
    "label": "Aperçu de développement",
    "serverOfficial": false
  }
}
```

Champs à garder séparés :

- `resources` : valeurs, capacité, production preview, dernier delta pour feedback HUD.
- `selection` : uniquement la zone/bâtiment sélectionné, pas l'état d'amélioration.
- `buildings.{id}.upgrade` : coût, durée, progression, statut.
- `buildings.{id}.training` : file locale du bâtiment concerné.
- `feedback` : message court affiché au joueur.
- `blockedReason` : raison globale temporaire si une action ne peut pas être effectuée.
- `previewNotice` : rappel discret que la progression est non officielle.

Raisons de blocage normalisées :

```json
[
  "insufficient_honey",
  "insufficient_pollen",
  "insufficient_wax",
  "building_already_upgrading",
  "training_queue_full",
  "troop_requirement_missing",
  "building_locked",
  "max_level_reached",
  "server_required",
  "feature_in_preparation",
  "gesture_in_progress"
]
```

Messages lisibles associés :

| `blockedReason` | Message joueur recommandé |
| --- | --- |
| `insufficient_honey` | `Miel insuffisant` |
| `insufficient_pollen` | `Pollen insuffisant` |
| `insufficient_wax` | `Cire insuffisante` |
| `building_already_upgrading` | `Bâtiment déjà en amélioration` |
| `training_queue_full` | `Capacité atteinte` |
| `troop_requirement_missing` | `Prérequis manquant` |
| `building_locked` | `Verrouillé` |
| `max_level_reached` | `Niveau maximum atteint` |
| `server_required` | `Serveur requis` |
| `feature_in_preparation` | `En préparation` |
| `gesture_in_progress` | Aucun message nécessaire, ignorer l'action |

## 4. Risques de conflit avec Builder-A

| Risque | Pourquoi c'est sensible | Prévention Builder-B |
| --- | --- | --- |
| Modifier le runtime pendant attente Demo/QA | ARCH-174 bloque les nouvelles modifications Builder-A jusqu'au retour officiel | Ne produire que ce document |
| Remplacer les valeurs d'équilibrage | Builder-A peut déjà avoir une boucle locale candidate | Marquer toutes les valeurs comme preview |
| Imposer une architecture de code | Builder-A connaît l'état réel du runtime | Fournir des structures proposées, pas des classes à intégrer telles quelles |
| Modifier microcopies validées | UI-060 encadre les libellés acceptés/interdits | Reprendre les microcopies autorisées, éviter nouveaux claims |
| Casser la séparation pan/zoom/HUD | ARCH-166/UI-060 insistent sur HUD fixe et ruche zoomable | Documenter les couches, ne pas toucher aux prefabs |
| Créer un doublon de viewer ou prototype actif | Pourrait brouiller Demo/QA | Aucun prototype runtime ajouté |
| Travailler sur carte monde | La priorité revient à la ruche | Aucune section carte monde |
| Toucher aux zones officielles | Builder-A peut avoir intégré des zones/halos | Référencer les zones, ne pas modifier les JSON existants |
| Générer un APK | Interdit par la tâche | Aucun build |
| Ajouter serveur/save/persistence | La boucle validée est locale preview | Mentionner explicitement non officiel, serveur non branché |

## 5. Checklist d'intégration rapide pour Builder-A après QA

Cette checklist est à utiliser uniquement après retour Demo/QA et feu vert Architecte.

### Avant modification runtime

- Lire le rapport Demo-A et les refus QA éventuels.
- Identifier les fichiers déjà touchés par Builder-A dans la passe ARCH-174.
- Confirmer si la prochaine passe corrige lisibilité, boutons muets, pan/zoom, ou structure d'état.
- Ne pas importer directement les valeurs preview Builder-B comme équilibrage final.

### Boutons et feedback

- Vérifier tous les boutons visibles avec la table d'inventaire.
- Pour chaque bouton disabled, afficher une raison lisible.
- Pour chaque bouton actif, produire une action visible ou un message.
- Supprimer tout libellé vague : `OK`, `Go`, `Action`, `Test`.
- Ne jamais afficher `LOCAL` ou `LOCAL PREVIEW` en Game View joueur.

### Ruche, pan/zoom et sélection

- Garder HUD et panneaux hors de la couche zoomée.
- Bloquer sélection/action pendant pan et pinch.
- Vérifier que le halo reste aligné après zoom/pan.
- Tester centre de zone, bord de zone, hors-zone et chevauchement.
- Tester tablette paysage et téléphone portrait.

### Ressources et amélioration

- Afficher coût avant lancement.
- Afficher durée avant lancement.
- Montrer les ressources manquantes avec texte + couleur, pas couleur seule.
- Montrer progression sur zone et dans panneau.
- Actualiser niveau et ressources après fin locale preview.
- Garder le statut preview/non officiel discret mais visible.

### Entraînement

- Caserne : action `Entraîner` visible seulement si elle a un effet ou une raison de blocage.
- Afficher type de troupe, quantité, coût, durée et capacité.
- Rendre la file visible depuis le panneau concerné.
- Afficher état vide : `Aucune formation en cours`.
- En portrait, garder la file compacte et scrollable.

### Validation finale de la passe

- Un nouveau joueur doit comprendre en moins de 10 secondes : ressource, bâtiment sélectionné, coût, durée, progression, file, statut preview.
- Aucun bouton visible ne reste muet.
- Aucun claim live/officiel n'apparaît sans serveur.
- Le panneau détail ne couvre pas la sélection active.
- Les modifications restent limitées à la ruche jouable.

## Limites Builder-B

- Document préparatoire uniquement.
- Aucune modification de scène.
- Aucune modification de script runtime.
- Aucun changement d'asset.
- Aucun APK généré.
- Aucun serveur branché.
- Aucun travail carte monde.
