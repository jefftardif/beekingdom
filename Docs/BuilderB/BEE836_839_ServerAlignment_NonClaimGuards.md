# Builder-B - BEE-836 a BEE-839 Server Alignment et Non-Claim Guards

Statut : support non-runtime  
Date : 2026-07-12  
Portee : Ruche jouable uniquement  
Contexte : ARCH-185 valide QA-067 et SERVER-039  
Integration : materiel de garde-fous pour Builder-A, Builder-C, Demo-A et QA-A  

Ce document prepare les garde-fous d'integration BEE-836 a BEE-839. Il ne modifie pas le runtime principal, ne touche pas a la carte monde, ne cree aucun endpoint, ne publie rien, ne genere pas d'APK et ne declare aucun gameplay officiel.

## Sources lues

- `C:/projets/beekingdomgame-master/Docs/Architecture/ARCH-185_QA067_Server039_GateAdvance_Dispatch.md`
- `C:/projets/beekingdom/QA/QA_DEMO_067_BEE821_827_VALIDATION.md`
- `C:/projets/beekingdom/prompts_codex/BEE-836_Playable_Hive_Server_Authoritative_Roadmap_Alignment_Framework.md`
- `C:/projets/beekingdom/prompts_codex/BEE-837_Hive_Loop_Idempotency_Anti_Double_Spend_Server_Prep_Framework.md`
- `C:/projets/beekingdom/prompts_codex/BEE-838_Playable_Hive_Demo_QA_Evidence_Bundle_Framework.md`
- `C:/projets/beekingdom/prompts_codex/BEE-839_Playable_Hive_Product_Advance_Non_Claim_Guard_Framework.md`
- `C:/projets/beekingdom/prompt_server/rapports/SERVER-039 - Hive Loop Local SQL Opt In Migration Dry Run Plan Report.md`

## Synthese de gate

ARCH-185 valide l'avance du gate avec reserves :

- QA-067 est `PASS_WITH_RESERVES`.
- BEE-821 a BEE-827 peuvent avancer.
- SERVER-039 est accepte comme plan SQL local opt-in / dry-run uniquement.
- Les reserves restantes portent sur preuve tactile physique, evidence pan/pinch encore candidate, portrait compact.
- La carte monde reste hors scope.
- La distinction local demo / server readiness / official gameplay doit rester explicite.

SERVER-039 ne cree pas de serveur officiel :

- dry-run SQL local seulement ;
- rollback final ;
- non reference dans `DatabaseCatalog` ;
- aucune migration production ;
- aucune ecriture SQL live ;
- aucun endpoint ;
- aucun publish ;
- aucun changement Unity.

## 1. Support d'integration BEE-836 a BEE-839

| BEE | Domaine | Role du garde-fou | Sortie attendue | Refus QA |
| --- | --- | --- | --- | --- |
| BEE-836 | Alignement roadmap serveur authoritative | Relier la preview locale a une feuille de route serveur future sans activation live | Matrice client preview -> futur serveur, non-claims visibles | Claim serveur actif, sauvegarde officielle, economie officielle, carte monde |
| BEE-837 | Idempotence et anti double spend | Faire correspondre rapid tap local et future `Idempotency-Key` serveur | Invariants cout une fois, queue une fois, replay bloque | Double cout, double queue, key/payload ambigus, mutation live |
| BEE-838 | Bundle evidence Demo/QA | Standardiser les preuves DEMO-068 | Manifest + captures + logs + reserves explicites | Evidence incomplete, statut preview absent, confusion live |
| BEE-839 | Non-claim guard produit | Interdire les claims officiels tant que serveur non actif | Assertions non-live dans Demo/QA | `live`, `progression officielle`, `save active`, `armee persistante` |

### Responsabilites par equipe

| Equipe | Responsabilite |
| --- | --- |
| Builder-A | Preserver les gardes rapid tap, feedback, non-mute buttons et limites preview dans les prochaines modifications ruche |
| Builder-B | Fournir support non-runtime, invariants, manifeste cible et assertions QA |
| Builder-C | Convertir les invariants en regression/automation quand autorise |
| Demo-A | Produire DEMO-068 avec preuves ciblees, manifest deterministe et non-claims |
| QA-A | Reprendre les assertions QA-068 et refuser tout claim officiel |
| Server-A | Continuer preparation authoritative non-live, sans endpoint ni migration production |
| UI-A | Garder action, cout, duree, progression, file, disabled reason et statut preview lisibles |

## 2. Invariants a verifier dans DEMO-068

### Idempotency locale

La preview locale doit demontrer un comportement idempotent, meme sans serveur officiel.

Invariants :

- une intention utilisateur acceptee produit un seul commit local ;
- un tap repete sur la meme action deja acceptee est bloque ou ignore avec feedback ;
- une action en cours ne peut pas etre re-ouverte comme nouvelle transaction ;
- le second tap ne doit pas appliquer de cout, creer de timer, ajouter de queue ou incrementer de compteur ;
- l'evidence peut mentionner une future `Idempotency-Key`, mais ne doit pas pretendre qu'une cle serveur est active.

Manifest cible :

```yaml
local_idempotency_preview: true
server_idempotency_active: false
idempotency_key_future_contract_only: true
duplicate_input_guarded_locally: true
duplicate_input_creates_second_commit: false
```

### Cout une fois

Invariants :

- rapid tap `Ameliorer` : cout applique une seule fois ;
- rapid tap `Entrainer` : cout applique une seule fois ;
- les ressources apres action correspondent a `avant - cout accepte` ;
- les ressources ne deviennent pas negatives ;
- le cout affiche correspond au cout applique dans le manifest.

Manifest cible :

```yaml
upgrade_cost_applied_once: true
training_cost_applied_once: true
upgrade_commit_count_after_double_input: 1
training_commit_count_after_double_input: 1
resource_values_non_negative_after_actions: true
displayed_cost_matches_applied_cost: true
```

### Queue une fois

Invariants :

- un double tap training n'ajoute qu'un lot ;
- la file respecte sa capacite ;
- un seul item actif par slot de training prevu ;
- ordre de file conserve ;
- recuperation de lot pret incremente les troupes une seule fois ;
- fermeture/reouverture du panneau ne duplique pas la file.

Manifest cible :

```yaml
training_queue_consistent: true
training_queue_delta_after_double_input: 1
training_queue_capacity_respected: true
training_queue_order_preserved: true
training_active_item_count_max: 1
troop_increment_once: true
panel_reopen_does_not_duplicate_queue: true
```

### Non-claim serveur/save/economie/armee

Invariants :

- progression serveur officielle : false ;
- sauvegarde active : false ;
- economie officielle active : false ;
- armee persistante officielle : false ;
- endpoint ruche live : false ;
- migration production : false ;
- ecriture SQL live : false ;
- carte monde modifiee : false ;
- statut preview visible dans preuve Demo.

Manifest cible :

```yaml
simulation_locale_de_demonstration: true
progression_serveur_officielle: false
sauvegarde_active: false
economie_officielle_active: false
armee_persistante_officielle: false
hive_live_endpoint_active: false
sql_live_write_performed: false
production_migration_applied: false
database_catalog_registered_hive_loop_tables: false
world_map_modified: false
preview_status_visible: true
```

## 3. Lignes de manifeste DEMO-068 a exiger

DEMO-068 devrait inclure un bloc dedie BEE-836 a BEE-839.

```yaml
demo_id: DEMO-068
scope: playable_hive_bEE836_839_support
hive_playable_priority: true
world_map_scope: false
bee_841_or_later_scope: false

bee_836_server_alignment:
  server_authoritative_roadmap_aligned: true
  server_authoritative_gameplay_active: false
  server_endpoint_added: false
  sql_dry_run_only: true
  database_catalog_registration_added: false
  production_publish_performed: false

bee_837_idempotency_anti_double_spend:
  local_idempotency_preview: true
  server_idempotency_active: false
  idempotency_key_future_contract_only: true
  upgrade_commit_count_after_double_input: 1
  upgrade_repeat_blocked_count: 1
  upgrade_cost_applied_once: true
  level_increment_once: true
  training_commit_count_after_double_input: 1
  training_repeat_blocked_count: 1
  training_cost_applied_once: true
  training_queue_consistent: true
  troop_increment_once: true

bee_838_evidence_bundle:
  manifest_present: true
  contact_sheet_present: true
  before_after_evidence_present: true
  rapid_tap_upgrade_evidence_present: true
  rapid_tap_training_evidence_present: true
  non_claim_evidence_present: true
  logs_referenced: true
  remaining_reserves_listed: true
  device_touch_proof_type: telemetry_or_physical_named
  physical_device_touch_proof_final: false

bee_839_non_claim_guard:
  simulation_locale_de_demonstration: true
  preview_status_visible: true
  progression_serveur_officielle: false
  sauvegarde_active: false
  economie_officielle_active: false
  armee_persistante_officielle: false
  live_claim_visible: false
  official_gameplay_claim_visible: false
  official_army_persistence_claim_visible: false
  publish_production: false
  sql_live_write_performed: false
  endpoint_live_created: false
```

Lignes strictement interdites si elles sont `true` :

```yaml
progression_serveur_officielle: true
sauvegarde_active: true
economie_officielle_active: true
armee_persistante_officielle: true
live_claim_visible: true
official_gameplay_claim_visible: true
endpoint_live_created: true
sql_live_write_performed: true
production_migration_applied: true
world_map_modified: true
```

## 4. Assertions QA-068 a reprendre

QA-068 devrait reprendre les assertions suivantes.

### BEE-836 - Alignement serveur non-live

- PASS si la preuve relie la boucle ruche locale a une roadmap serveur future sans endpoint live.
- PASS si SERVER-039 reste presente comme dry-run local opt-in.
- PASS si aucune table ruche n'est declaree comme migration production.
- FAIL si la preuve indique une progression server-authoritative active.
- FAIL si Demo ou Builder-A presente la preview comme sauvegardee officiellement.

### BEE-837 - Idempotency / anti double spend

- PASS si rapid tap `Ameliorer` garde `upgrade_commit_count_after_double_input:1`.
- PASS si rapid tap training garde `training_commit_count_after_double_input:1`.
- PASS si cout upgrade et training sont chacun appliques une seule fois.
- PASS si queue training reste coherente et troop increment une seule fois.
- FAIL si deux taps produisent deux couts, deux queues, deux timers ou deux increments.
- FAIL si l'idempotence est presentee comme service serveur live alors qu'elle est locale preview.

### BEE-838 - Evidence bundle Demo/QA

- PASS si manifest, captures, logs et reserves sont presents.
- PASS si evidence cible rapid tap upgrade/training, non-claims et limites serveur.
- PASS_WITH_RESERVES si pan/pinch restent telemetry/capture candidate sans device physique, mais sont explicitement nommes.
- FAIL si le bundle omet les limites non-live.
- FAIL si le bundle relance la carte monde ou couvre BEE-841+ sans autorisation.

### BEE-839 - Non-claim guard

- PASS si `progression_serveur_officielle:false`, `sauvegarde_active:false`, `economie_officielle_active:false`, `armee_persistante_officielle:false`.
- PASS si microcopy preview/non officielle reste visible et sobre.
- PASS si aucun screenshot ou rapport ne contient de claim live/officiel.
- FAIL si `Live`, `Progression officielle`, `Sauvegarde active`, `Economie officielle`, `Armee persistante` apparaissent comme etat actif.
- FAIL si le rapport laisse croire qu'un endpoint, SQL live ou publish production existe.

## 5. Garde-fous d'integration pour Builder-A / Builder-C / Demo

### Builder-A

- Ne pas prendre la carte monde.
- Ne pas activer de runtime serveur officiel pour BEE-836 a BEE-839.
- Preserver les protections rapid tap deja validees par QA-067.
- Garder le statut local preview visible.
- Ne pas renommer les microcopies en claims officiels.
- Ne pas convertir SERVER-039 en migration active.

### Builder-C

- Garder les tests centres sur la ruche jouable.
- Mesurer idempotence locale via compteurs de commit, couts et files.
- Distinguer clairement telemetry/capture candidate et preuve physique device.
- Ne pas transformer les frameworks BEE-836 a BEE-839 en validation serveur live.

### Demo-A

- Produire un bundle evidence-only.
- Inclure manifest DEMO-068 avec les lignes exigees ci-dessus.
- Montrer les non-claims explicitement.
- Nommer les reserves restantes.
- Ne pas utiliser de formulation "officielle" pour la progression locale.

### QA-A

- Refuser tout claim live, save, economie, armee ou serveur officiel.
- Verifier que la preuve reste sur BEE-836 a BEE-839.
- Verifier que BEE-841+ reste bloque tant que l'Architecte ne l'autorise pas.
- Verifier que SERVER-039 reste dry-run non-live.

## 6. Matrice de refus rapide

| Signal detecte | Decision recommandee |
| --- | --- |
| `progression_serveur_officielle:true` | FAIL |
| `sauvegarde_active:true` | FAIL |
| `economie_officielle_active:true` | FAIL |
| `armee_persistante_officielle:true` | FAIL |
| `sql_live_write_performed:true` | FAIL |
| `endpoint_live_created:true` | FAIL |
| `world_map_modified:true` | FAIL |
| Double cout upgrade ou training | FAIL |
| Double queue training apres rapid tap | FAIL |
| Manifest absent | FAIL ou BLOCKED |
| Device proof absent mais reserve explicite | PASS_WITH_RESERVES possible |
| Statut preview absent de la preuve | FAIL |

## 7. Limites explicites

- Support documentaire seulement.
- Aucun runtime principal modifie.
- Aucune scene modifiee.
- Aucun asset modifie.
- Aucun APK genere.
- Aucun endpoint serveur cree.
- Aucune migration production.
- Aucune ecriture SQL live.
- Aucune carte monde.
- Aucune declaration de gameplay officiel.

## Verdict Builder-B

Le materiel BEE-836 a BEE-839 est pret pour servir de garde-fou DEMO-068/QA-068, sous reserve que Demo-A et QA-A conservent les limites non-live et que Builder-A ne prenne pas de runtime serveur officiel dans cette tranche.

READY_FOR_DEMO_068_SUPPORT = YES
