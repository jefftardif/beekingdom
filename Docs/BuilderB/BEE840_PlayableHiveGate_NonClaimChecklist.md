# Builder-B - BEE-840 Playable Hive Gate et Non-Claim Checklist

Statut : support non-runtime  
Date : 2026-07-12  
Portee : fermeture de vague Ruche jouable avant future carte monde  
Contexte : ARCH-190 valide QA-068 et lance BEE-832/BEE-833  
Integration : support pour Demo-A, QA-A, Builder-A, Builder-C et Architecte  

Ce document prepare le gate BEE-840. Il ne modifie pas le runtime principal, la scene, les assets, le serveur, la carte monde ou l'APK.

## Sources lues

- `C:/projets/beekingdomgame-master/Docs/Architecture/ARCH-190_QA068_GateAdvance_BEE832_833_Dispatch.md`
- `C:/projets/beekingdom/QA/QA_DEMO_068_BEE828_835_VALIDATION.md`
- `C:/projets/beekingdom/prompts_codex/BEE-840_Playable_Hive_Product_Advance_Gate_Before_World_Map_Framework.md`
- `C:/projets/beekingdom/prompts_codex/BEE-832_Right_Panel_Density_Product_Polish_Framework.md`
- `C:/projets/beekingdom/prompts_codex/BEE-833_Disabled_Reason_Readability_Placement_Framework.md`

## Synthese de gate

ARCH-190 autorise la suite avec reserves :

- QA-068 est `PASS_WITH_RESERVES`.
- BEE-828 a BEE-831 sont acceptes pour avancer.
- BEE-832 et BEE-833 doivent maintenant traiter la densite du panneau droit et le placement lisible des raisons de blocage.
- BEE-840 doit fermer proprement la vague Ruche jouable avant toute future carte monde.
- BEE-841 demeure bloque tant que BEE-840 n'est pas valide par l'Architecte.

## 1. Checklist de fermeture BEE-840

| Domaine | PASS attendu | PASS_WITH_RESERVES acceptable | BLOCKED si |
| --- | --- | --- | --- |
| Priorite Ruche | DEMO-069 reste centree sur la Ruche jouable | Le rapport mentionne la carte monde uniquement comme hors scope | Carte monde relancee, modifiee ou presentee comme prochaine preuve active |
| BEE-832 panneau droit | Panneau moins dense, blocs courts, action principale lisible, cout/duree/progression/file visibles | Lisible mais encore compact en portrait, reserve de confort explicite | Panneau surcharge, action principale noyee, selection ou cout masques |
| BEE-833 raisons de blocage | Raisons visibles dans le flux normal de lecture | Raison lisible mais encore perfectible en portrait | Raison trop basse, crampee, invisible ou dependante d'un crop separe |
| Boutons non muets | Chaque bouton visible agit, ouvre, explique ou indique preview | Bouton secondaire perfectible mais non critique et feedback present | Bouton critique muet : ameliorer, entrainer, fermer, disabled, futur |
| Cout/duree upgrade | Cout et duree visibles avant action | Format compact mais comprehensible | Action possible sans cout ou duree visible |
| Progression upgrade | Etat running visible dans zone ou panneau, timer/progression lisible | Progression acceptable mais reserve d'animation/polish | Running confondu avec idle/locked ou invisible |
| Training | Type troupe, cout, duree, file et resultat preview visibles | File compacte mais lisible | File introuvable, cout/duree absents, double training non garde |
| Idempotence locale | Rapid taps gardes : cout une fois, queue une fois, niveau/troupe une fois | Evidence par manifest/tests mais pas preuve tactile physique | Double cout, double queue, double niveau, double troupe |
| HUD/panneaux fixes | HUD et panneaux restent fixes pendant pan/zoom | Evidence telemetry/capture candidate, reserve tactile explicite | HUD/panneau bouge avec ruche ou preuve absente |
| UI bloque gestes ruche | Boutons/panneaux bloquent pan/zoom ruche | Evidence telemetry/capture candidate, reserve tactile explicite | Drag/pinch UI declenche pan/zoom ruche |
| Portrait telephone | Boucle coeur lisible : selection, action, cout, raison, file | Compact mais utilisable, reserve final comfort explicite | Portrait illisible ou action principale hors champ |
| Tablette paysage | Ruche dominante, panneau lisible, HUD fixe | Minor polish reserve possible | Ruche reduite a decor ou panneau domine l'ecran |
| Non-claims | Local preview clair, pas serveur/save/economie/armee/carte monde | Mention preview discrete mais visible | Claim live/officiel, save active, economie officielle, armee persistante |
| Evidence bundle | Manifest, captures, logs, contact sheet, reserves | Une preuve tactile physique encore reservee si explicitement nommee | Manifest absent, reserves masquees, support confondu avec runtime |

### Decision de fermeture

| Resultat QA-069 | Decision BEE-840 | BEE-841 |
| --- | --- | --- |
| Tous domaines critiques PASS, reserves uniquement tactiles/polish | BEE-840 peut etre propose `PASS_WITH_RESERVES` ou `PASS` selon QA | Peut etre propose au deblocage Architecte ulterieur |
| BEE-832/BEE-833 PASS, non-claims PASS, reserves tactiles | BEE-840 peut fermer la vague avec reserves | Deblocage possible seulement apres decision Architecte |
| Non-claim FAIL ou carte monde modifiee | BEE-840 BLOCKED | BEE-841 reste bloque |
| Bouton critique muet ou cout/raison invisible | BEE-840 BLOCKED | BEE-841 reste bloque |
| Evidence Demo/manifest absente | BEE-840 BLOCKED ou QA inconclusive | BEE-841 reste bloque |

## 2. Non-claims obligatoires DEMO-069 / QA-069

DEMO-069 et QA-069 doivent confirmer explicitement :

- boucle locale preview seulement ;
- aucune progression serveur officielle ;
- aucun serveur officiel actif ;
- aucune sauvegarde officielle ;
- aucune economie officielle ;
- aucune armee persistante officielle ;
- aucun endpoint live cree ;
- aucune migration production ;
- aucune ecriture SQL live ;
- aucun publish production ;
- aucune carte monde modifiee ;
- aucun BEE-841+ inclus dans le scope.

Microcopies acceptables :

- `Simulation locale de demonstration`
- `Apercu de developpement`
- `Donnees non officielles`
- `Serveur requis`
- `Service en preparation`

Microcopies ou claims a refuser si presentes comme etat actif :

- `Progression officielle`
- `Live`
- `Synchronise` sans serveur valide
- `Sauvegarde active`
- `Economie officielle`
- `Armee persistante`
- `Carte monde live`
- `Serveur officiel actif`

## 3. Lignes de manifeste DEMO-069 a exiger

DEMO-069 devrait contenir un bloc dedie BEE-832/BEE-833 et BEE-840.

```yaml
demo_id: DEMO-069
scope: playable_hive_bEE832_833_bEE840_gate
hive_playable_priority: true
world_map_scope: false
bee_841_or_later_scope: false
runtime_validated_bees:
  - BEE-832
  - BEE-833
support_gate_bees:
  - BEE-840

bee_832_right_panel_density:
  right_panel_less_dense: true
  right_panel_grouped_sections: true
  primary_action_visible: true
  cost_duration_progress_queue_visible: true
  selected_building_identity_visible: true
  tablet_landscape_panel_readable: true
  phone_portrait_panel_usable: true
  phone_portrait_final_comfort_reserved: true

bee_833_disabled_reason_readability:
  disabled_reason_in_normal_reading_flow: true
  disabled_reason_readable_without_crop: true
  disabled_reason_near_related_action: true
  insufficient_resource_reason_visible: true
  future_service_reason_visible: true
  training_queue_busy_reason_visible: true
  disabled_button_has_feedback: true

bee_840_gate_before_world_map:
  playable_hive_wave_closure_candidate: true
  gate_before_world_map_reviewed: true
  bee_841_remains_blocked_pending_architect: true
  no_world_map_expansion: true
  support_documents_not_claimed_as_runtime: true
  remaining_reserves_listed: true
  qa_069_gate_decision_required: true

deterministic_guards:
  upgrade_commit_count_after_double_input: 1
  upgrade_cost_applied_once: true
  level_increment_once: true
  training_commit_count_after_double_input: 1
  training_cost_applied_once: true
  training_queue_consistent: true
  troop_increment_once: true

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
```

Lignes bloquantes si elles apparaissent en `true` :

```yaml
world_map_scope: true
bee_841_or_later_scope: true
progression_serveur_officielle: true
serveur_officiel_actif: true
sauvegarde_active: true
economie_officielle_active: true
armee_persistante_officielle: true
endpoint_live_created: true
sql_live_write_performed: true
production_migration_applied: true
publish_production: true
support_documents_claimed_as_runtime: true
```

## 4. Assertions QA-069 pour decider si BEE-841 peut etre debloquee plus tard

QA-069 ne debloque pas automatiquement BEE-841. Elle peut seulement recommander a l'Architecte que BEE-841 devienne eligible plus tard.

### Assertions PASS obligatoires

- BEE-832 est implementee et validee : panneau droit moins dense, action principale lisible, boucle joueur conservee.
- BEE-833 est implementee et validee : raisons de blocage lisibles dans le flux normal, sans crop separe.
- Les boutons critiques restent non muets.
- Cout, duree, progression, niveau/resultat et file training restent visibles.
- Rapid tap upgrade/training reste garde : cout une fois, queue une fois, increment une fois.
- HUD, navigation et panneaux restent fixes pendant pan/zoom dans l'evidence disponible.
- Les gestes UI ne traversent pas vers la ruche dans l'evidence disponible.
- Le manifest DEMO-069 contient les non-claims obligatoires.
- La preuve separe clairement runtime implemente, support documentaire et serveur futur.
- La carte monde reste non modifiee.

### PASS_WITH_RESERVES acceptable

- Preuve tactile physique encore reservee, si la preuve telemetry/capture est honnete et explicite.
- Portrait utilisable mais encore compact, si la boucle coeur reste comprehensible.
- Polish visuel mineur restant sur le panneau, si action/cout/raison/file restent lisibles.
- Support BEE-840 documentaire, si non cite comme runtime.

### BLOCKED

- Carte monde modifiee, relancee ou utilisee comme preuve.
- BEE-841+ inclus sans validation Architecte.
- Claim live, serveur officiel, sauvegarde officielle, economie officielle ou armee persistante officielle.
- BEE-832 non implementee ou panneau droit encore trop dense pour lire l'action principale.
- BEE-833 non implementee ou raison disabled illisible.
- Bouton critique muet.
- Cout/duree/progression/file invisibles ou hors champ.
- Rapid tap cree double cout, double queue, double niveau ou double troupe.
- Manifest DEMO-069 absent ou incomplet sur les non-claims.

## 5. Garde-fous par role

### Builder-A

- Implementer uniquement les corrections Ruche autorisees BEE-832/BEE-833.
- Ne pas prendre le runtime carte monde.
- Ne pas transformer le support BEE-840 en fonctionnalite runtime.
- Preserver les guards BEE-821 a BEE-831 deja acceptes.
- Garder les microcopies preview/non officielles.

### Builder-B

- Fournir checklist, manifest cible et assertions QA.
- Ne pas modifier scene/runtime/assets/serveur/APK.
- Ne pas travailler carte monde.

### Builder-C

- Preparer matrices/tests pour BEE-832/BEE-833/BEE-840.
- Distinguer preuve automation/capture et preuve tactile physique.

### Demo-A

- Produire DEMO-069 avec captures, manifest, logs, reserves.
- Montrer explicitement BEE-832 et BEE-833.
- Inclure bloc BEE-840 gate/non-claims.

### QA-A

- Valider BEE-832/BEE-833 comme runtime.
- Traiter BEE-840 comme gate de fermeture, pas comme feature.
- Recommander ou refuser l'eligibilite future de BEE-841 selon les assertions.

## 6. Verdict Builder-B

Le support BEE-840 est pret pour guider DEMO-069 et QA-069. La fermeture de vague ne doit etre proposee que si BEE-832/BEE-833 passent et si les non-claims restent strictement vrais.

READY_FOR_DEMO_069_GATE_SUPPORT = YES
