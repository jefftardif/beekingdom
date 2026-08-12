# Builder-B - Support BEE-821/840 Automated Checks et Input Boundaries

Statut : préparation non-runtime  
Date : 2026-07-12  
Priorité : Ruche jouable  
Contexte : ARCH-179 validé, gate DEMO-066 avancé avec réserves  
Portée : support de planification et de tests automatisables uniquement  

Ce document prépare la prochaine vague ruche jouable BEE-821 à BEE-840. Il ne modifie pas la scène, les scripts runtime, les assets, l'APK, le serveur, la sauvegarde ou l'économie officielle.

## Références lues

- `C:/projets/beekingdomgame-master/Docs/Architecture/ARCH-179_QA066_GateAdvance_Dispatch.md`
- `C:/projets/beekingdom/QA/QA_DEMO_066_RESERVE_CLOSURE_VALIDATION.md`
- `C:/projets/beekingdomgame-master/Docs/BuilderB/PlayableHiveLoop_NextRuntimeSupport.md`

Synthèse utile :

- DEMO-066 est accepté en `PASS WITH RESERVES`.
- Les réserves bloquantes précédentes sont suffisamment fermées pour avancer.
- Les réserves restantes concernent surtout preuve tactile réelle, polish portrait et automatisation.
- Les prochains checks doivent rester centrés sur la ruche jouable locale preview.
- Aucun test ne doit créer de claim serveur/live/save/économie/armée officielle.

## 1. Inventaire des checks automatisables

| Check | But | Entrée simulée | Observation attendue | Donnée manifeste recommandée |
| --- | --- | --- | --- | --- |
| Rapid tap `Améliorer` | Empêcher double commit | Deux taps rapides sur le bouton | Une seule amélioration démarre | `upgrade_commit_count_after_double_input: 1` |
| Rapid tap entraînement | Empêcher double ajout de lot | Deux taps rapides sur `Entraîner` ou `Ajouter à la file` | Un seul lot ajouté | `training_commit_count_after_double_input: 1` |
| Coût upgrade appliqué une fois | Valider économie locale déterministe | Action upgrade acceptée | Ressources diminuées une seule fois | `upgrade_cost_applied_once: true` |
| Coût entraînement appliqué une fois | Eviter double dépense | Action training acceptée | Ressources diminuées une seule fois | `training_cost_applied_once: true` |
| Niveau incrémenté une fois | Eviter double niveau | Fin upgrade | Niveau +1, pas +2 | `upgrade_level_increment: 1` |
| File entraînement cohérente | Garder ordre et capacité | Ajout lot, progression, fin | Queue stable, capacité respectée | `training_queue_consistent: true` |
| Compteur troupes incrémenté une fois | Valider fin entraînement | Lot prêt puis récupéré | Troupe + quantité attendue | `troop_increment_once: true` |
| HUD fixe pendant pan | Vérifier séparation ruche/HUD | Pan ruche | HUD ne bouge pas | `hud_fixed_during_pan: true` |
| HUD fixe pendant pinch | Vérifier séparation zoom/HUD | Pinch ruche | HUD/panneaux ne zooment pas | `hud_fixed_during_pinch: true` |
| Panneau fixe pendant zoom | Eviter panel attaché à ruche | Pinch avec panneau ouvert | Panneau reste fixe | `panel_fixed_during_pinch: true` |
| UI bloque pan/zoom | Eviter gestes ruche sous boutons | Drag/pinch sur bouton UI | Aucun pan/zoom ruche | `fixed_ui_blocks_hive_gesture: true` |
| Pan ne sélectionne pas | Eviter sélection accidentelle | Drag sur zone | Aucune sélection/action | `pan_does_not_select: true` |
| Pinch ne sélectionne pas | Eviter tap fantôme | Pinch sur zone | Aucune sélection/action | `pinch_does_not_select: true` |
| Bouton disabled explique | Aucun bouton muet | Tap disabled | Raison visible | `disabled_reason_visible: true` |
| Limites non-live | Eviter claim officiel | Captures/manifeste | Mention preview, pas live/officiel | `server_official_progression: false` |

## 2. Rapid tap `Améliorer`

Objectif : garantir qu'un double tap très rapide ne lance pas deux améliorations, ne déduit pas deux fois le coût et ne produit pas deux incréments de niveau.

Préconditions recommandées :

- Un bâtiment sélectionné.
- Bouton `Améliorer` disponible.
- Ressources suffisantes pour une seule amélioration.
- Aucun upgrade en cours sur ce bâtiment.

Séquence test :

1. Capturer ressources initiales, niveau initial, état initial.
2. Envoyer deux taps sur `Améliorer` dans une fenêtre courte, par exemple 0 à 120 ms.
3. Attendre une frame d'interface stable.
4. Vérifier que l'état devient `upgrading` ou équivalent.
5. Vérifier que le coût n'a été appliqué qu'une fois.
6. Vérifier que le second tap reçoit un état bloqué ou ignoré, mais pas silencieux.
7. Laisser finir ou forcer la fin test si le mode test l'autorise.
8. Vérifier que le niveau augmente d'un seul niveau.

Résultats attendus :

```json
{
  "upgrade_double_tap_guard": true,
  "upgrade_commit_count_after_double_input": 1,
  "upgrade_repeat_blocked_count": 1,
  "upgrade_cost_applied_once": true,
  "upgrade_level_increment": 1,
  "upgrade_second_tap_feedback": "building_already_upgrading"
}
```

Points de refus :

- Deux coûts déduits.
- Deux timers créés.
- Niveau +2.
- Bouton actif sans feedback sur le second tap.
- Message technique brut.

## 3. Rapid tap entraînement

Objectif : garantir qu'un double tap ne crée pas deux lots d'entraînement et ne double pas la dépense locale.

Préconditions recommandées :

- Caserne sélectionnée.
- Action entraînement disponible.
- Une troupe sélectionnée, par exemple `soldier`.
- File vide ou avec au moins un slot libre.
- Ressources suffisantes pour un seul lot, pas forcément deux.

Séquence test :

1. Capturer ressources initiales, taille de file initiale et compteur troupe initial.
2. Envoyer deux taps rapides sur `Ajouter à la file` ou `Entraîner`.
3. Vérifier qu'un seul lot est ajouté.
4. Vérifier que le coût n'est appliqué qu'une fois.
5. Vérifier que le second tap produit un feedback : `File entraînement occupée`, `Capacité atteinte`, ou raison équivalente.
6. Terminer le lot si le test le permet.
7. Vérifier que le compteur troupe augmente une seule fois de la quantité attendue.

Résultats attendus :

```json
{
  "training_double_tap_guard": true,
  "training_commit_count_after_double_input": 1,
  "training_repeat_blocked_count": 1,
  "training_cost_applied_once": true,
  "training_queue_delta": 1,
  "troop_increment_once": true,
  "training_second_tap_feedback": "training_queue_busy_or_full"
}
```

Points de refus :

- Deux lots ajoutés pour un double tap.
- Coût appliqué deux fois.
- Compteur troupe augmenté deux fois.
- File visuellement vide alors qu'un lot est actif.
- Bouton qui ne répond pas et n'explique pas le blocage.

## 4. Coût appliqué une fois

Le contrôle du coût doit être indépendant du contrôle visuel. L'UI peut afficher un message correct tout en ayant appliqué deux mutations locales ; le check doit donc comparer les valeurs.

Formule attendue :

```text
resource_after = resource_before - accepted_action_cost
```

Même en cas de double input :

```text
resource_after != resource_before - (accepted_action_cost * 2)
```

Manifest recommandé :

```json
{
  "cost_checks": {
    "upgrade": {
      "resource_before": {"honey": 1000, "wax": 100},
      "accepted_cost": {"honey": 100, "wax": 10},
      "resource_after": {"honey": 900, "wax": 90},
      "cost_applied_once": true
    },
    "training": {
      "resource_before": {"honey": 900, "pollen": 300},
      "accepted_cost": {"honey": 40, "pollen": 15},
      "resource_after": {"honey": 860, "pollen": 285},
      "cost_applied_once": true
    }
  }
}
```

Checks additionnels :

- Les ressources ne deviennent pas négatives.
- Les ressources manquantes bloquent l'action avant mutation.
- Le coût affiché correspond au coût appliqué.
- Le coût reste visible avant l'action.

## 5. Queue entraînement cohérente

Objectif : valider que la file d'entraînement reste lisible, bornée et déterministe.

Etats minimaux à vérifier :

| Etat | Condition | Attendu |
| --- | --- | --- |
| `empty` | Aucun lot | Message `Aucune formation en cours` ou équivalent |
| `queued` | Lot en attente | Lot visible avec troupe, quantité, ordre |
| `training` | Lot actif | Progression + temps restant |
| `ready` | Lot terminé | Badge ou action récupération visible |
| `blocked` | Ressource/prérequis manquant | Raison visible |
| `full` | Capacité atteinte | Ajout désactivé avec raison |

Invariants :

```json
{
  "training_queue_capacity_respected": true,
  "training_queue_order_preserved": true,
  "training_active_item_count_max": 1,
  "training_ready_item_collectable": true,
  "training_empty_state_visible": true,
  "training_no_hidden_duplicate_items": true
}
```

Cas limites à couvrir :

- Ajouter un lot quand la file est vide.
- Ajouter un lot quand un lot est déjà actif.
- Tenter d'ajouter quand la file est pleine.
- Tenter d'ajouter sans ressources.
- Fermer puis rouvrir le panneau Caserne : la file reste cohérente.
- Changer de bâtiment puis revenir : la file reste visible.

## 6. HUD et panneaux fixes

Objectif : automatiser ou semi-automatiser la preuve que la ruche pan/zoom ne déplace pas le HUD, les boutons fixes et les panneaux.

Méthode de mesure recommandée :

1. Relever les rectangles écran du HUD ressources, du panneau détail et des boutons fixes avant geste.
2. Simuler un pan de la ruche.
3. Relever les mêmes rectangles.
4. Simuler un pinch zoom.
5. Relever les mêmes rectangles.
6. Comparer les deltas.

Tolérances proposées :

| Elément | Tolérance position | Tolérance taille | Attendu |
| --- | ---: | ---: | --- |
| HUD ressources | 0 à 1 px | 0 à 1 px | Fixe |
| Panneau détail | 0 à 1 px | 0 à 1 px | Fixe |
| Navigation fixe | 0 à 1 px | 0 à 1 px | Fixe |
| Feedback toast | 0 à 2 px | 0 à 1 px | Fixe ou ancré écran |
| Halo sélection | Suit la ruche | Suit la ruche | Non fixe, aligné zone |

Manifest recommandé :

```json
{
  "fixed_layer_checks": {
    "hud_fixed_during_pan": true,
    "hud_fixed_during_pinch": true,
    "panel_fixed_during_pan": true,
    "panel_fixed_during_pinch": true,
    "navigation_fixed_during_pan": true,
    "selection_halo_tracks_hive_content": true
  }
}
```

Point clé : le halo et les badges de zone doivent bouger avec la ruche, mais les boutons UI doivent rester fixes.

## 7. Boutons UI qui bloquent pan/zoom

Objectif : prouver que les gestes commencés sur l'UI fixe ne traversent pas vers la surface de ruche.

Zones à tester :

- Bouton `Améliorer`.
- Bouton `Entraîner` / `Ajouter à la file`.
- Bouton `Fermer`.
- Boutons `+` et `-` quantité troupe.
- Cartes de suggestion.
- HUD ressources si cliquable.
- Zone scrollable du panneau portrait.

Séquences :

| Séquence | Entrée | Attendu |
| --- | --- | --- |
| Drag sur bouton UI | Un doigt glissé depuis le bouton | Aucun pan ruche |
| Pinch sur panneau | Deux doigts sur panneau fixe | Aucun zoom ruche |
| Tap bouton puis drag court | Tap/drag sous seuil bouton | Action bouton ou feedback, pas pan |
| Scroll panneau portrait | Swipe vertical dans panneau | Scroll panneau, pas pan ruche |
| Tap disabled | Tap sur bouton bloqué | Raison visible, pas pan |

Manifest recommandé :

```json
{
  "input_boundary_checks": {
    "fixed_ui_blocks_hive_gesture": true,
    "button_drag_does_not_pan_hive": true,
    "panel_pinch_does_not_zoom_hive": true,
    "portrait_panel_scroll_does_not_pan_hive": true,
    "disabled_button_tap_shows_reason": true
  }
}
```

Refus :

- Pan déclenché en drag sur bouton.
- Zoom déclenché en pinch sur panneau.
- Sélection de zone derrière un panneau.
- Bouton disabled sans feedback.

## 8. Limites non-live

La vague BEE-821/840 doit préparer des automatisations sans transformer la preview locale en système officiel.

Assertions non-live à conserver :

```json
{
  "non_live_limits": {
    "server_official_progression": false,
    "save_active": false,
    "official_economy": false,
    "official_army_persistence": false,
    "world_map_scope": false,
    "local_preview_label_visible": true,
    "no_live_claim_visible": true
  }
}
```

Microcopies autorisées :

- `Simulation locale de démonstration`
- `Aperçu de développement`
- `Données non officielles`
- `Serveur requis`
- `Service en préparation`

Microcopies à refuser :

- `Progression officielle`
- `Live`
- `Synchronisé` sans serveur réel
- `Sauvegarde active` si aucune sauvegarde officielle
- `Armée persistante` si aucune persistance officielle
- `Économie officielle` si économie locale preview

Checks visuels recommandés :

- Le statut preview/non officiel est visible mais discret.
- Aucune capture ne suggère une progression serveur officielle.
- Les messages de succès ne promettent pas une sauvegarde.
- Les compteurs troupes restent présentés comme preview si non persistés.

## Proposition de découpage BEE-821 à BEE-840

Ce découpage est indicatif pour Planner/Builder-A. Builder-B ne crée aucun ticket officiel.

| BEE | Sujet proposé | Type |
| --- | --- | --- |
| BEE-821 | Harness automatisé rapid tap upgrade | Test |
| BEE-822 | Assertion coût upgrade appliqué une fois | Test |
| BEE-823 | Assertion niveau upgrade +1 seulement | Test |
| BEE-824 | Harness rapid tap entraînement | Test |
| BEE-825 | Assertion coût entraînement appliqué une fois | Test |
| BEE-826 | Assertion queue entraînement capacité/ordre | Test |
| BEE-827 | Assertion compteur troupe + quantité attendue | Test |
| BEE-828 | Check bouton disabled avec raison lisible | Test/UI |
| BEE-829 | Check HUD fixe pendant pan | Test/Input |
| BEE-830 | Check HUD fixe pendant pinch | Test/Input |
| BEE-831 | Check panneau fixe pendant pan/zoom | Test/Input |
| BEE-832 | Check boutons UI bloquent pan | Test/Input |
| BEE-833 | Check panneaux UI bloquent pinch | Test/Input |
| BEE-834 | Check scroll panneau portrait sans pan ruche | Test/Input |
| BEE-835 | Check pan ne sélectionne pas | Test/Input |
| BEE-836 | Check pinch ne sélectionne pas | Test/Input |
| BEE-837 | Check halo reste aligné après pan/zoom | Test/Visual |
| BEE-838 | Check limites non-live et microcopies interdites | Test/UX |
| BEE-839 | Manifeste déterministe unifié DEMO/QA | Tooling |
| BEE-840 | Pack de preuves tablette/paysage + portrait | Demo/QA |

## Checklist Builder-A après feu vert

- Transformer les checks manifest existants en tests répétables quand possible.
- Vérifier rapid tap upgrade et training sur le même build.
- Comparer ressources avant/après plutôt que seulement l'état visuel.
- Tester avec ressources suffisantes, insuffisantes et limite exacte.
- Tester file vide, active, pleine et prête.
- Mesurer HUD/panneaux en coordonnées écran avant/après pan/zoom.
- Confirmer que les inputs UI ne traversent jamais vers la ruche.
- Garder les libellés non-live et éviter tout claim serveur/officiel.
- Conserver la carte monde hors scope.

## Limites Builder-B

- Aucun changement runtime.
- Aucune modification de scène.
- Aucun asset modifié.
- Aucun APK généré.
- Aucun serveur branché.
- Aucun test automatisé implémenté ici.
- Document préparatoire uniquement, prêt pour intégration future par Builder-A.
