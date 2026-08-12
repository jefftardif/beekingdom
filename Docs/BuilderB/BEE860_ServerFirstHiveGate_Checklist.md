# Builder-B - BEE-860 Server-First Hive Gate Checklist

Statut : support non-runtime  
Date : 2026-07-12  
Portee : gate Ruche jouable server-first avant toute carte monde  
Contexte : ARCH-194 valide Planner BEE-841 a BEE-860  
Integration : support pour DEMO-070 / QA-070, sans modification runtime  

Ce document prepare le gate BEE-860. Il ne modifie pas le runtime principal, la scene, les assets, le serveur, la carte monde ou l'APK.

## Sources lues

- `C:/projets/beekingdomgame-master/Docs/Architecture/ARCH-194_BEE841_860_ValidationAndDispatch.md`
- `C:/projets/beekingdom/prompts_codex/BEE-860_Playable_Hive_Product_Server_First_Gate_Before_World_Map_Framework.md`
- `C:/projets/beekingdom/prompts_codex/BEE-842_Hive_Resource_Tick_Feedback_Persistability_Prep_Framework.md`
- `C:/projets/beekingdom/prompts_codex/BEE-843_Hive_Resource_Growth_Error_And_Cap_States_Framework.md`
- `C:/projets/beekingdom/prompts_codex/BEE-844_Building_Upgrade_Cost_Timer_Completion_Framework.md`
- `C:/projets/beekingdom/prompts_codex/BEE-845_Building_Upgrade_Failure_Cancel_Local_States_Framework.md`
- `C:/projets/beekingdom/prompts_codex/BEE-846_Building_Upgrade_Anti_Double_Action_Server_Prep_Framework.md`
- `C:/projets/beekingdom/prompts_codex/BEE-847_Troop_Training_Cost_Timer_Queue_Completion_Framework.md`
- `C:/projets/beekingdom/prompts_codex/BEE-848_Troop_Training_Anti_Double_Queue_Guard_Framework.md`
- `C:/projets/beekingdom/prompts_codex/BEE-849_Local_Army_Minimal_Product_Section_Framework.md`
- `C:/projets/beekingdom/prompts_codex/BEE-850_Army_Counts_Feedback_Non_Persistent_Guard_Framework.md`
- `C:/projets/beekingdom/QA/QA_DEMO_069_BEE832_840_VALIDATION.md`

## Synthese du gate

ARCH-194 valide la planification BEE-841 a BEE-860 avec une priorite stricte : Ruche jouable produit server-first, pas carte monde.

Le gate BEE-860 doit empecher BEE-861 et toute carte monde tant que la ruche n'est pas une boucle concrete :

- ressources qui tickent avec feedback lisible ;
- etats de ressource : gain, cap, manque, erreur ;
- amelioration batiment : cout, timer, progression, completion ;
- etats upgrade : succes, echec local, blocage, annulation si autorisee ;
- anti double action upgrade relie a une preparation serveur future, sans serveur live ;
- entrainement troupes : cout, timer, queue, completion ;
- anti double queue training ;
- armee locale minimale visible ;
- compteurs armee avec feedback, sans persistence officielle ;
- non-claims stricts : local preview, pas serveur officiel, pas save officielle, pas economie officielle, pas armee persistante officielle, pas carte monde.

## 1. Checklist PASS / PASS_WITH_RESERVES / BLOCKED pour BEE-842 a BEE-850

| BEE | Domaine | PASS attendu | PASS_WITH_RESERVES acceptable | BLOCKED si |
| --- | --- | --- | --- | --- |
| BEE-842 | Resource tick feedback / persistability prep | Les ressources augmentent en local preview, le HUD montre le delta et la source de gain, le statut non officiel reste visible | Persistability future seulement documentee, sans save officielle ; reserve UX mineure si feedback compact mais lisible | Ticks invisibles, valeur qui change sans feedback, claim save/serveur, texte coupe |
| BEE-843 | Resource growth, error, cap states | Gain, cap, manque et erreur sont distincts et lisibles ; ressource cappee ne deborde pas | Certains etats rares restent dans manifest si la demo explique la limite | Ressources negatives, cap ignore, erreur technique brute, manque non explique |
| BEE-844 | Building upgrade cost / timer / completion | Avant action : cout/duree/niveau ; pendant : timer/progression ; apres : niveau/resultat ; cout applique une fois | Completion acceleree pour demo si declaree preview | Upgrade seulement statique, cout absent, timer absent, completion invisible, double niveau |
| BEE-845 | Upgrade failure / cancel / local states | Blocage, echec local et annulation autorisee ont feedback clair ; annulation si absente est expliquee | Annulation non implementee si bouton absent et non necessaire au scenario | Bouton annuler muet, echec sans raison, cout perdu sans feedback, etat incoherent |
| BEE-846 | Upgrade anti double action / server prep | Rapid tap upgrade garde un seul commit, un seul cout, un seul timer ; lien idempotence serveur futur non-live | Idempotency key future seulement en manifest, pas runtime serveur | Double spend, double timer, claim idempotence serveur live, endpoint actif |
| BEE-847 | Troop training cost / timer / queue / completion | Avant : troupe/cout/duree ; pendant : queue/progression ; apres : troupe ajoutee localement | Queue compacte mais lisible en portrait | Training statique, cout/duree absents, queue introuvable, completion non visible |
| BEE-848 | Training anti double queue guard | Rapid tap training ajoute un seul lot, cout une fois, queue coherente, feedback second tap | Test automation sans preuve tactile physique si reserve explicite | Double queue, double cout, file incoherente, bouton muet |
| BEE-849 | Local army minimal section | Soldats, Gardiennes, Eclaireuses visibles comme armee locale non officielle | Section minimale mais lisible, reserve de polish possible | Armee absente, troupe sans libelle, claim armee officielle ou persistante |
| BEE-850 | Army counts feedback non persistent guard | Changement de compteurs visible apres training, feedback local, non persistence explicite | Compteurs non persistants seulement dans manifest si UI indique preview | Compteur change sans feedback, armee presentee comme persistante, save officielle suggeree |

## 2. Non-claims obligatoires DEMO-070 / QA-070

DEMO-070 et QA-070 doivent confirmer explicitement :

- local preview uniquement ;
- aucune progression serveur officielle ;
- aucun serveur officiel actif ;
- aucune sauvegarde officielle ;
- aucune economie officielle ;
- aucune armee persistante officielle ;
- aucun endpoint live ;
- aucune migration SQL production ;
- aucune ecriture SQL live ;
- aucun publish production ;
- aucune carte monde ;
- aucun world map runtime ;
- BEE-861 reste bloquee jusqu'a validation Architecte du lot BEE-841 a BEE-860.

Microcopies autorisees :

- `Simulation locale de demonstration`
- `Apercu de developpement`
- `Donnees non officielles`
- `Serveur requis`
- `Service en preparation`
- `Armee locale non officielle`
- `Non persistant`

Microcopies ou claims a refuser comme etat actif :

- `Progression officielle`
- `Serveur officiel actif`
- `Live`
- `Sauvegarde active`
- `Economie officielle`
- `Armee persistante`
- `Carte monde active`
- `Synchronise` sans serveur valide

## 3. Lignes de manifeste DEMO-070 a exiger

DEMO-070 doit prouver avant / pendant / apres action, plus erreur ou blocage quand applicable.

```yaml
demo_id: DEMO-070
scope: playable_hive_bEE842_850_bEE860_gate
hive_playable_priority: true
server_first_non_live_scope: true
world_map_scope: false
world_map_runtime_allowed: false
bee_861_or_later_scope: false

bee_842_resource_ticks:
  resource_tick_visible: true
  resource_delta_feedback_visible: true
  resource_source_or_rate_visible: true
  future_persistability_prep_only: true
  official_save_active: false

bee_843_resource_states:
  resource_gain_state_visible: true
  resource_cap_state_visible: true
  resource_insufficient_state_visible: true
  resource_error_state_safe_message: true
  resource_values_non_negative: true

bee_844_upgrade_flow:
  upgrade_before_cost_visible: true
  upgrade_before_duration_visible: true
  upgrade_before_level_visible: true
  upgrade_during_timer_visible: true
  upgrade_during_progress_visible: true
  upgrade_after_completion_visible: true
  upgrade_after_level_increment_once: true
  upgrade_cost_applied_once: true

bee_845_upgrade_failure_cancel:
  upgrade_blocked_reason_visible: true
  upgrade_failure_local_state_visible_if_triggered: true
  upgrade_cancel_state_defined: true
  upgrade_cancel_button_not_mute_if_visible: true
  upgrade_no_hidden_cost_loss_on_failure: true

bee_846_upgrade_anti_double_action:
  upgrade_commit_count_after_double_input: 1
  upgrade_repeat_blocked_count_at_least: 1
  upgrade_double_action_guard: true
  server_idempotency_future_contract_only: true
  server_idempotency_live_active: false

bee_847_training_flow:
  training_before_troop_type_visible: true
  training_before_cost_visible: true
  training_before_duration_visible: true
  training_during_queue_visible: true
  training_during_progress_visible: true
  training_after_completion_visible: true
  training_troop_increment_once: true
  training_cost_applied_once: true

bee_848_training_anti_double_queue:
  training_commit_count_after_double_input: 1
  training_repeat_blocked_count_at_least: 1
  training_queue_consistent: true
  training_queue_capacity_respected: true
  training_no_hidden_duplicate_items: true

bee_849_local_army_section:
  local_army_section_visible: true
  soldiers_visible: true
  guardians_visible: true
  scouts_visible: true
  local_army_non_official_label_visible: true

bee_850_army_counts_non_persistent:
  army_count_feedback_visible: true
  army_count_delta_after_training_visible: true
  army_counts_non_persistent_guard_visible: true
  official_army_persistence_active: false

bee_860_gate:
  playable_hive_server_first_gate_reviewed: true
  action_evidence_before_present: true
  action_evidence_during_present: true
  action_evidence_after_present: true
  action_evidence_blocked_or_error_present: true
  bee_861_remains_blocked_pending_architect: true
  no_world_map_before_hive_gate: true

non_claims:
  simulation_locale_de_demonstration: true
  preview_status_visible: true
  progression_serveur_officielle: false
  serveur_officiel_actif: false
  sauvegarde_active: false
  economie_officielle_active: false
  armee_persistante_officielle: false
  endpoint_live_created: false
  sql_live_write_performed: false
  production_migration_applied: false
  publish_production: false
  world_map_modified: false
  world_map_runtime_active: false
```

Lignes bloquantes si elles apparaissent en `true` :

```yaml
world_map_scope: true
world_map_runtime_active: true
bee_861_or_later_scope: true
progression_serveur_officielle: true
serveur_officiel_actif: true
sauvegarde_active: true
economie_officielle_active: true
armee_persistante_officielle: true
endpoint_live_created: true
sql_live_write_performed: true
production_migration_applied: true
publish_production: true
official_save_active: true
official_army_persistence_active: true
```

## 4. Assertions QA-070 pour decider si BEE-861 reste bloquee

QA-070 ne doit pas debloquer BEE-861 automatiquement. Elle peut seulement recommander a l'Architecte de conserver ou revoir le blocage.

### Assertions PASS obligatoires

- Les ressources tickent avec feedback visible et non officiel.
- Les etats gain / cap / manque / erreur sont lisibles.
- Une action upgrade montre avant, pendant et apres : cout, duree, timer, progression, completion.
- Les etats upgrade bloque / echec / annulation si visible ne sont pas muets.
- L'upgrade anti double action garde un seul cout, un seul timer, un seul increment.
- Une action training montre avant, pendant et apres : troupe, cout, duree, queue, progression, completion.
- Le training anti double queue garde un seul lot, un seul cout et un seul increment troupe.
- Une section armee locale montre Soldats, Gardiennes et Eclaireuses.
- Les compteurs armee changent avec feedback et restent marques non persistants.
- Le manifest DEMO-070 contient les non-claims obligatoires.
- Les preuves distinguent runtime local, server prep dev-only et absence de serveur officiel.
- Aucune carte monde ou world map runtime n'est modifie.

### PASS_WITH_RESERVES acceptable

- Preuve tactile physique encore reservee, si telemetry/capture est nommee comme telle.
- Portrait encore compact, si action/cout/timer/progression/queue/armee restent lisibles.
- Server-first bridge seulement documentaire/dev-only, si aucune activation live n'est suggeree.
- Certaines erreurs rares non capturees en image, si le manifest les liste et aucun bouton critique n'est muet.

### BLOCKED

- Carte monde modifiee ou relancee.
- BEE-861 ou plus inclus sans validation Architecte.
- Claim serveur officiel, sauvegarde officielle, economie officielle ou armee persistante officielle.
- Ressources qui changent sans feedback.
- Upgrade sans cout, timer, progression ou completion.
- Training sans cout, timer, queue ou completion.
- Double cout, double timer, double queue, double increment niveau ou troupe.
- Armee locale absente ou presentee comme persistante.
- Bouton critique muet.
- Texte coupe sur une information critique.
- Manifest DEMO-070 absent ou incomplet sur les non-claims.

## 5. Decision gate BEE-860

| Resultat QA-070 | Decision recommandee | BEE-861 |
| --- | --- | --- |
| Tous les domaines critiques PASS, non-claims PASS | BEE-860 eligible pour decision Architecte | Peut etre reconsidere uniquement par Architecte |
| Runtime PASS avec reserves tactiles/polish/non-live explicites | BEE-860 PASS_WITH_RESERVES possible | Reste bloquee jusqu'a decision Architecte |
| Non-claim FAIL, carte monde modifiee ou action loop incomplete | BEE-860 BLOCKED | Reste bloquee |

## 6. Garde-fous par role

### Builder-A

- Implementer seulement la tranche Ruche locale BEE-842 a BEE-850.
- Ne pas prendre la carte monde.
- Ne pas activer serveur officiel, save officielle, economie officielle ou armee persistante.
- Preserver les guards anti double action.
- Garder les messages preview/non officiels.

### Builder-B

- Fournir checklist, lignes de manifeste et assertions QA.
- Ne pas modifier runtime, scene, assets, serveur, carte monde ou APK.

### Builder-C

- Preparer matrices/tests avant/pendant/apres et erreur/blocage.
- Distinguer preuve capture/automation et preuve tactile physique.

### Demo-A

- Montrer au moins une action upgrade complete et une action training complete.
- Montrer un blocage ou une erreur lisible.
- Montrer l'armee locale et les non-claims.
- Inclure le manifest DEMO-070 avec les lignes exigees.

### QA-A

- Refuser toute preuve statique quand une action est demandee.
- Refuser toute confusion support documentaire / runtime / serveur officiel.
- Confirmer que BEE-861 reste bloquee sauf decision Architecte ulterieure.

## Verdict Builder-B

Le support BEE-860 est pret pour guider DEMO-070 et QA-070. La carte monde et BEE-861 doivent rester bloquees tant que la ruche jouable server-first n'a pas prouve une boucle concrete avec ressources, upgrade, training, armee locale et non-claims stricts.

READY_FOR_DEMO_070_GATE_SUPPORT = YES
