# Builder-B - BEE-876 / BEE-879 / BEE-880 QA, No-World-Map et Gate Support

Statut : support non-runtime  
Date : 2026-07-12  
Portee : Ruche jouable uniquement, aucun runtime carte monde  
Contexte : ARCH-198 valide Planner BEE-861 a BEE-880  
Integration : support QA/Demo/Architect, sans modification runtime Builder-A  

Ce document prepare les artefacts de support Builder-B pour BEE-876, BEE-879 et BEE-880. Il ne modifie pas le runtime principal, la scene, les assets, le serveur, la carte monde ou l'APK.

## Sources lues

- `C:/projets/beekingdomgame-master/Docs/Architecture/ARCH-198_Planner861_880_ValidationAndParallelDispatch.md`
- `C:/projets/beekingdom/QA/QA_DEMO_070_BEE842_860_VALIDATION.md`
- `C:/projets/beekingdom/prompt_demo/rapports/DEMO-070_BEE842_860/DEMO-070_Report.md`
- `C:/projets/beekingdom/prompts_codex/BEE-876_Player_QA_Produce_Spend_Upgrade_Train_Matrix_Framework.md`
- `C:/projets/beekingdom/prompts_codex/BEE-879_No_World_Map_Scope_Guard_Framework.md`
- `C:/projets/beekingdom/prompts_codex/BEE-880_Playable_Hive_Server_Bridge_Gate_Framework.md`

## Synthese de cadrage

ARCH-198 valide la vague BEE-861 a BEE-880 avec une priorite stricte : ruche jouable produit, bridge serveur dev-only/pre-officiel, snapshot futur, UX des etats action, preuves joueur et gate no-world-map.

Pour Builder-B, le travail parallele porte uniquement sur :

- BEE-876 : matrice QA joueur produire / depenser / ameliorer / entrainer / armee observable ;
- BEE-879 : garde stricte contre carte monde, exploration, alliance, guerre et map MMO ;
- BEE-880 : gate ruche server bridge avant suite, sans serveur officiel live.

QA-070 accepte la boucle locale avec reserves. Les reserves importantes restent :

- absence de serveur officiel live ;
- absence de sauvegarde officielle ;
- absence d'economie officielle ;
- absence d'armee persistante officielle ;
- absence de runtime carte monde ;
- BEE-861/BEE-881 non debloquees automatiquement.

## 1. Matrice joueur QA produce / spend / upgrade / train / army

QA doit pouvoir tester comme un joueur, pas seulement comme lecteur de manifest.

| Axe joueur | Etat initial attendu | Action QA | Preuve attendue | PASS | BLOCKED si |
| --- | --- | --- | --- | --- | --- |
| Produire | Ressources visibles dans le HUD | Attendre un tick ou declencher scenario de tick local | Delta visible, ex. `+X/s`, valeur modifiee | Ressource change avec feedback lisible | Valeur change sans feedback, HUD masque, claim economie officielle |
| Depenser | Cout visible avant action | Lancer upgrade ou training avec ressources suffisantes | Ressources apres = avant - cout accepte | Cout applique une seule fois | Double depense, cout cache, ressource negative |
| Ameliorer | Batiment selectionne, cout/duree/niveau visibles | Tap `Ameliorer` | Timer/progression, completion, niveau/resultat | Avant/pendant/apres visibles | Bouton muet, timer absent, progression absente, double niveau |
| Upgrade bloque | Ressources insuffisantes ou action deja en cours | Tap action bloquee | Raison proche de l'action | Raison lisible, pas de mutation cachee | Disabled sans raison, cout retire malgre blocage |
| Entrainer | Caserne/training visible, troupe selectionnee | Ajouter un lot training | Cout/duree/file/progression visibles | Lot unique, queue coherente | Double queue, cout double, file introuvable |
| Training complete | Lot pret ou termine | Recuperer/constater resultat | Troupe locale + quantite attendue | Compteur armee local change avec feedback | Compteur ne change pas, change deux fois, claim armee persistante |
| Armee observable | Soldats/Gardiennes/Eclaireuses visibles | Observer apres training | Section armee locale non officielle | Troupes nommees et compteurs lisibles | Armee absente, libelles coupes, persistance officielle suggeree |
| Action refusee | Etat serveur requis/refuse/pending | Tap action future ou server-required | Message calme : serveur requis / action refusee / en attente | Refus explique, sans crash ni texte debug | Erreur brute, bouton muet, claim serveur officiel |
| Snapshot futur | Mention dev-only si presente | Consulter statut snapshot/bridge | Snapshot futur/dev-only, pas save officielle | Frontiere dev-only claire | Sauvegarde active/officielle affichee |

## 2. Checklist anti-regression

Cette checklist doit accompagner Demo-A et QA-A pour DEMO-071.

### Boutons non muets

- `Ameliorer` : agit ou explique cout/manque/en cours/serveur requis.
- `Entrainer` : agit ou explique queue occupee/capacite/ressource/precondition.
- `Fermer` / `Ouvrir` panneau : feedback visible.
- Bouton futur/service : affiche `Serveur requis` ou `Service en preparation`.
- Bouton disabled : raison lisible proche du bouton.
- Boutons quantite training si visibles : `+` / `-` changent quantite ou expliquent limite.

Refus QA :

- bouton critique sans reaction ;
- bouton disabled sans raison ;
- bouton future qui ressemble a une action live ;
- bouton qui ferme/ouvre sans etat visible.

### Pas de double depense

- rapid tap upgrade : `upgrade_commit_count_after_double_input: 1`.
- rapid tap training : `training_commit_count_after_double_input: 1`.
- cout upgrade applique une fois.
- cout training applique une fois.
- ressources jamais negatives apres action ou blocage.
- cout affiche = cout applique.

Refus QA :

- deux couts pour une intention ;
- deux timers pour un upgrade ;
- ressources negatives ;
- difference manifeste entre cout affiche et cout applique.

### Pas de double queue

- un double tap training cree un seul lot.
- queue capacity respectee.
- ordre de file conserve.
- un lot pret incremente les troupes une seule fois.
- fermeture/reouverture panneau ne duplique pas la queue.

Refus QA :

- deux lots identiques apres rapid tap ;
- queue visuellement vide mais commit effectue ;
- compteur troupe +2 fois ;
- queue pleine sans raison lisible.

### Raisons disabled lisibles

- `Miel insuffisant`, `Cire insuffisante`, `Pollen insuffisant` si ressource manque.
- `Batiment deja en amelioration` si upgrade occupe.
- `File entrainement occupee` ou `Capacite atteinte` si training bloque.
- `Serveur requis` si action future/pre-officielle.
- `En preparation` si service non disponible.

Refus QA :

- raison trop basse, coupee ou hors champ ;
- raison lisible seulement dans un crop separe ;
- rouge/couleur comme seul signal ;
- erreur technique brute.

## 3. No-world-map guard

BEE-879 exige une preuve explicite que le lot ne relance pas la carte monde, l'exploration, les alliances, la guerre ou une map MMO.

### Assertions obligatoires

```yaml
no_world_map_guard:
  playable_hive_only: true
  world_map_scope_allowed: false
  world_map_runtime_active: false
  world_map_modified: false
  exploration_world_active: false
  alliance_system_active: false
  war_system_active: false
  mmo_map_claim_visible: false
  bee_881_or_later_scope: false
```

### Preuves attendues

- Rapport Demo centré sur la ruche.
- Manifest indiquant explicitement `world_map_runtime_active:false`.
- Absence de capture carte monde.
- Absence d'action exploration/alliance/guerre.
- Rapport QA confirmant que la preuve reste ruche.
- Aucun fichier/runtime de carte monde revendique comme modifie par le lot.

### Refus immediat

| Signal | Decision |
| --- | --- |
| Capture carte monde comme preuve principale | BLOCKED |
| `world_map_runtime_active:true` | BLOCKED |
| Exploration/alliance/guerre activees | BLOCKED |
| Map MMO presentee comme prochaine runtime integree | BLOCKED |
| BEE-881+ incluse sans Architecte | BLOCKED |

## 4. Gate BEE-880 : criteres pour passer a Demo puis QA

BEE-880 est un gate, pas une feature runtime. Il decide si le bundle Builder-A peut aller vers Demo-A puis QA-A.

### Pre-Demo : Builder-A peut passer a Demo si

- Server-A a livre ou confirme les limites dev-only/pre-officielles utiles au lot.
- Le runtime Builder-A reste dans la ruche jouable.
- Les actions produire/depenser/ameliorer/entrainer/armee sont observables.
- Les etats accepte/refuse/en attente/serveur requis sont lisibles si presents.
- Aucun bouton critique n'est muet.
- Les protections anti-double depense et anti-double queue restent presentes.
- Les non-claims sont visibles : local preview, pas serveur officiel, pas save, pas economie, pas armee persistante.
- Le manifest source contient les lignes no-world-map guard.

### Demo-A doit fournir

- Capture avant action.
- Capture pendant timer/progression ou attente.
- Capture apres completion.
- Capture erreur/blocage/refus ou serveur requis.
- Capture ou strip armee locale observable.
- Manifest avec couts, deltas, commit counts, queue counts et non-claims.
- Rapport separant clairement runtime, support, serveur dev-only et limites.

### QA-A peut accepter si

- QA peut reproduire ou verifier visuellement produire/depenser/ameliorer/entrainer/armee.
- Aucun double spend/double queue n'est constate.
- Les raisons disabled sont proches et lisibles.
- Les preuves restent ruche-only.
- Les non-claims serveur/save/economie/armee/carte monde sont tous vrais.
- Les supports Builder-B/Builder-C ne sont pas cites comme runtime officiel.

### BEE-880 doit rester BLOCKED si

- Server bridge est presente comme serveur officiel live.
- Une sauvegarde officielle est declaree.
- Une economie officielle est declaree.
- Une armee persistante officielle est declaree.
- Carte monde, exploration, alliance ou guerre entrent dans la preuve.
- QA ne peut pas voir produire/depenser/ameliorer/entrainer/armee.
- Un bouton critique reste muet.
- Rapid taps creent double cout ou double queue.

## 5. Lignes de manifeste DEMO-071 recommandees

```yaml
demo_id: DEMO-071
scope: playable_hive_bEE861_880_bridge_gate
builder_b_support:
  bee_876_player_qa_matrix_present: true
  bee_879_no_world_map_guard_present: true
  bee_880_gate_support_present: true

player_action_matrix:
  produce_observable: true
  spend_observable: true
  upgrade_observable: true
  train_observable: true
  local_army_observable: true
  accepted_state_visible: true
  rejected_or_blocked_state_visible: true
  pending_or_server_required_state_visible_if_applicable: true

anti_regression:
  no_mute_important_buttons: true
  disabled_reasons_readable: true
  disabled_reasons_near_action: true
  upgrade_commit_count_after_double_input: 1
  upgrade_cost_applied_once: true
  training_commit_count_after_double_input: 1
  training_cost_applied_once: true
  training_queue_consistent: true
  troop_increment_once: true

server_bridge_limits:
  server_bridge_dev_only: true
  official_server_live_claim_allowed: false
  official_server_live_active: false
  official_save_active: false
  official_economy_active: false
  official_persistent_army_active: false
  production_sql_migration_applied: false
  production_endpoint_created: false
  publish_production: false

no_world_map_guard:
  playable_hive_only: true
  world_map_scope_allowed: false
  world_map_runtime_active: false
  world_map_modified: false
  exploration_world_active: false
  alliance_system_active: false
  war_system_active: false
  mmo_map_claim_visible: false
  bee_881_or_later_scope: false
```

## 6. Assertions QA-071 proposees

QA-071 devrait reprendre les assertions suivantes.

### BEE-876

- PASS si QA peut observer produire, depenser, ameliorer, entrainer et armee locale.
- PASS si les etats accepte/refuse/en attente/serveur requis sont comprehensibles.
- FAIL si le joueur ne peut pas comprendre pourquoi une action est bloquee.
- FAIL si cout, timer, queue, progression ou armee sont masques alors qu'ils concernent le scenario.

### BEE-879

- PASS si le bundle reste ruche-only.
- PASS si le manifest exclut carte monde, exploration, alliance, guerre et map MMO.
- FAIL si une preuve carte monde entre dans le scope.
- FAIL si une capture ou un rapport revendique exploration/alliance/guerre.

### BEE-880

- PASS si Builder-A peut passer a Demo puis QA avec preuves joueur completes et non-claims.
- PASS_WITH_RESERVES si preuve tactile physique ou XML structure restent reserves, mais la boucle joueur est visible.
- FAIL si le bridge serveur est presente comme live/officiel.
- FAIL si save/economie/armee persistante officielles sont revendiquees.
- FAIL si support documentaire est confondu avec runtime implemente.

## 7. Limites Builder-B

- Aucun code runtime ajoute.
- Aucune scene modifiee.
- Aucun asset modifie.
- Aucun serveur modifie.
- Aucune carte monde modifiee.
- Aucun APK genere.
- Aucun blocage Server-A ou Builder-A : ce document sert de garde-fou parallele.

## Verdict Builder-B

Le support BEE-876 / BEE-879 / BEE-880 est pret pour DEMO-071 et QA-071, sous reserve que le lot reste centre sur la ruche jouable, que Server-A reste dev-only/pre-officiel, et qu'aucun scope carte monde/exploration/alliance/guerre ne soit ouvert.

READY_FOR_DEMO_071_GATE_SUPPORT = YES
