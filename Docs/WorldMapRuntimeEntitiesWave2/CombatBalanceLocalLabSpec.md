# WorldMap Runtime Entities Wave2 - Combat Balance Local Lab Spec

Date locale: 2026-07-15

Statut: specification de prototype local, non officielle, documentation seulement.

Regle cible: `local_combat_balance_wave2_preview_v1`

## Verdict

`READY_FOR_LOCAL_COMBAT_BALANCE_PROTOTYPE=YES`

Ce verdict signifie que la matrice, les calculs deterministes, les cas limites, les parametres et les tests sont assez precis pour commencer un prototype local opt-in. Il ne signifie ni implementation terminee, ni validation Unity, ni balance production.

Valeurs obligatoires pour toute execution de cette regle:

- `server=false`
- `official_gain=false`
- `local_only=true`
- `reward_grants=[]`
- `xp_delta=0`
- `progression_delta=0`

`READY_FOR_PRODUCTION_COMBAT=NO`

## Portee

Cette specification couvre uniquement une simulation locale et deterministe de combat entre une composition de ruche test et une cible bestiaire ou ruche test.

Inclus:

- niveaux de ruche 1 a 50;
- classes `Neutral`, `RoyalGuard`, `Striker`, `Nurturer`, `Scout`, `Alchemist`;
- soldats, gardiennes, eclaireuses et ouvrieres;
- T1 a T4 en solo local;
- T5 a T7 en raid local;
- score de composition, PV, degats, pertes et cooldown preview;
- preview sans mutation et application a l'etat local seulement;
- presets et tests deterministes.

Exclus:

- serveur, appel distant, matchmaking ou synchronisation reseau;
- recompense, loot, XP, progression, classement ou economie officielle;
- persistence officielle et horloge serveur;
- probabilites, coups critiques, RNG ou seed de production;
- modification de terrain, evenement BearDen ou contenu externe;
- definition de statistiques de production.

## Sources et constats locaux

### Runtime de laboratoire

`WorldMapLocalLabRuntime.cs` fournit deja les bornes et champs suivants:

- deux ruches test stables: `PLAYER_TEST_HIVE` et `ENEMY_TEST_HIVE`;
- niveau borne a 1..50;
- six classes locales;
- compteurs editables `soldiers`, `guards`, `scouts`, `workers`;
- `health` borne a 0..`maxHealth`, avec `maxHealth >= 1`;
- combat actuel deterministe;
- animation combat actuelle de `1.75 + 0.55 + 1.75 = 4.05 s`;
- reapplication de `localOnly=true`, `authorityServer=false`, `officialGain=false` apres chargement et apres preset;
- sauvegarde locale du laboratoire seulement.

La formule historique du duel de ruches est volontairement simple:

- attaque: `soldiers * 3 + scouts + level * 2`;
- defense: `enemy.guards * 2 + enemy.level`;
- degats cible bornes a 12..180;
- retour borne a 0..80;
- aucune perte d'unites et aucun cooldown.

Cette formule reste sous `legacy_hive_duel_v1`. Wave2 ne doit pas changer silencieusement les preuves historiques.

### Bestiaire et contrat

Les rapports et recus locaux etablissent deja:

- couverture T1..T7;
- selection de cible;
- solo local pour les tiers bas;
- raid local pour les tiers hauts;
- resultat local deterministe;
- T7 recu avec `required=336`, `available=456`, `result=win`;
- absence de serveur et de gain officiel;
- contrat futur: T1..T4 peuvent etre solo, T5..T7 exigent raid ou cooperation;
- en production future seulement, resultat, pertes, recompenses et cooldowns appartiendront au serveur.

Recus de reference lus:

- `Docs/BuilderA/WorldMapTestHivesCombatCollectionLab/PlayerProof/WorldMapLocalLabProofReceipt.md`;
- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/RuntimeIntegrationProof/RuntimeEntitiesProofReceipt.md`;
- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/InteractionPolishProof/InteractionPolishProofReceipt.md`;
- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/AutomatedRegressionProof/AutomatedRegressionProofReceipt.md`;
- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/RuntimeScenarioDataLayerProof/RuntimeScenarioDataLayerProofReceipt.md`.

## Invariants non editables

| Invariant | Valeur | Comportement exige |
|---|---:|---|
| `server` | `false` | Force a `false` au chargement, au preview, a l'application et dans la telemetrie. |
| `official_gain` | `false` | Force a `false`; aucune branche de calcul ne peut le changer. |
| `local_only` | `true` | Toute mutation reste dans l'etat du laboratoire. |
| `reward_grants` | `[]` | Aucun objet, ressource ou monnaie. |
| `xp_delta` | `0` | Aucun gain de niveau ou progression. |
| `combat_mode` | T1..T4 `solo_local`; T5..T7 `raid_local` | Un mode incompatible bloque l'action. |
| RNG | absent | Entrees identiques, sorties identiques. |
| arrondi | entier specifie ci-dessous | Aucun arrondi dependant de la plateforme. |

Si une sauvegarde locale contient des valeurs d'autorite non conformes, le chargement doit retablir `server=false` et `official_gain=false`, produire l'avertissement `unsafe_flag_normalized`, puis continuer localement.

## Versionnement et coexistence

Deux regles doivent coexister pendant le prototype:

| Regle | Usage | Mutation |
|---|---|---|
| `legacy_hive_duel_v1` | Preuves existantes `RunCombatForProof` et duel actuel | Etat local historique seulement |
| `local_combat_balance_wave2_preview_v1` | Nouveau panneau de balance T1..T7, opt-in | Preview sans mutation ou application locale explicite |

Un recu historique ne doit jamais etre regenere en utilisant Wave2 sans changer son `rule_version`.

## Vocabulaire

- **Tier**: difficulte de la cible, pas palier visuel de ruche.
- **Aile**: contribution virtuelle d'une ruche ou d'un membre de raid local. Aucune connexion joueur n'est impliquee.
- **Composition agregee**: somme des unites de toutes les ailes.
- **PV attaquant**: sante locale de la ruche meneuse; les PV ne remplacent pas les comptes d'unites.
- **PV cible**: sante locale de la cible bestiaire ou ruche test.
- **Ready**: les portes de niveau, mode, classe, composition, score, PV et cooldown passent.
- **Hold**: la sortie n'est pas lancee; aucune perte, aucun degat et aucun cooldown.

## Modele d'entree preview

Entree minimale:

- `preview_id` local unique;
- `rule_version=local_combat_balance_wave2_preview_v1`;
- `encounter_id` stable;
- `target_family=bestiary|test_hive`;
- `tier` 1..7;
- `combat_mode=solo_local|raid_local`;
- `wings[]` avec `level`, `class`, `soldiers`, `guards`, `scouts`, `workers`;
- `attacker_health`, `attacker_max_health`;
- `target_health`, `target_max_health`;
- `cooldown_remaining_local_seconds`;
- `server=false`;
- `official_gain=false`.

Sortie minimale:

- `eligible`;
- `block_reasons[]` et `warnings[]`;
- `required_score`, `available_score`, `readiness_bp`;
- `projected_target_damage`, `projected_attacker_hp_loss`;
- `projected_soldier_losses`, `projected_guard_losses`, `projected_scout_losses`;
- `projected_target_health_after`, `projected_attacker_health_after`;
- `projected_cooldown_seconds`;
- `gate=ready|hold`;
- `outcome=blocked|engaged|target_defeated|attacker_defeated|mutual_defeat`;
- `reward_grants=[]`, `xp_delta=0`, `progression_delta=0`;
- `server=false`, `official_gain=false`, `local_only=true`.

## Normalisation

Avant tout calcul:

1. Borner `tier` a 1..7 pour l'editeur, mais refuser une requete d'application dont le tier brut est hors plage.
2. Borner chaque niveau a 1..50.
3. Borner chaque compte d'unites a 0..999999.
4. Forcer `max_health` a 1..999999.
5. Borner `health` a 0..`max_health`.
6. Utiliser des intermediaires signes 64 bits pour tout produit.
7. Forcer les trois invariants `local_only=true`, `server=false`, `official_gain=false`.
8. Retourner les champs normalises dans `warnings`, sans modifier les donnees officielles puisqu'il n'en existe aucune ici.

## Score de composition

Pour `n` ailes:

```text
L = floor(sum(wing.level) / n)
S = sum(wing.soldiers)
G = sum(wing.guards)
E = sum(wing.scouts)
O = sum(wing.workers)

available_score A = S + G + E + floor(O / 2) + 2 * L
```

Regles:

- les ouvrieres representent la logistique et valent un demi-point;
- le niveau effectif est la moyenne basse, pas la somme, afin qu'ajouter une aile vide ne cree pas de puissance;
- les classes ne changent pas `A`; elles modifient degats, PV recus, pertes et cooldown;
- les minima de chaque famille sont des portes dures: compenser zero gardienne avec des ouvrieres ne suffit pas;
- une aile de raid doit avoir au moins un soldat et un poids d'unites strictement positif;
- une aile manquante, vide ou hors bande de niveau produit `hold`.

## Matrice locale non officielle T1..T7

Tous les nombres de cette table sont editables pour le laboratoire, non officiels et versionnes avec la regle preview.

| Tier | Niveaux | Mode | Ailes | Soldats min | Gardiennes min | Eclaireuses min | Ouvrieres min | Score requis | PV cible | PV max attaquant conseille | Degats base bp | Contre PV bp | Pertes base bp | CD base s |
|---|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| T1 | 1..4 | solo_local | 1 | 16 | 4 | 2 | 8 | 28 | 120 | 180 | 3600 | 600 | 150 | 5 |
| T2 | 5..9 | solo_local | 1 | 24 | 8 | 4 | 20 | 56 | 220 | 260 | 3400 | 800 | 200 | 8 |
| T3 | 10..14 | solo_local | 1 | 36 | 12 | 6 | 20 | 84 | 360 | 450 | 3200 | 1000 | 250 | 12 |
| T4 | 15..19 | solo_local | 1 | 48 | 16 | 8 | 20 | 112 | 560 | 650 | 3000 | 1200 | 350 | 18 |
| T5 | 20..29 | raid_local | 2 | 96 | 28 | 12 | 48 | 200 | 900 | 800 | 2800 | 1500 | 500 | 30 |
| T6 | 30..34 | raid_local | 3 | 112 | 40 | 20 | 64 | 264 | 1300 | 1000 | 2600 | 1800 | 700 | 45 |
| T7 | 35..50 | raid_local | 4 | 140 | 48 | 28 | 100 | 336 | 1800 | 1200 | 2400 | 2200 | 900 | 60 |

Chaque ligne au niveau minimal produit exactement le score requis. Exemple T7: `140 + 48 + 28 + floor(100/2) + 2*35 = 336`.

Le T6 passe volontairement du seuil historique local 228 au seuil preview 264. Ce delta lisse la progression raid et rend les minima croissants. Il est limite a `local_combat_balance_wave2_preview_v1`.

Les `PV max attaquant conseille` produisent un avertissement `hp_below_recommended`, pas un blocage. Seul `attacker_health=0` bloque.

## Classes et roles

Une valeur de 10000 bp vaut 100 %. Une valeur inferieure dans les colonnes PV recus, pertes ou cooldown est favorable.

| Classe | Role local | Degats solo bp | Degats raid bp | PV recus bp | Pertes bp | Cooldown bp |
|---|---|---:|---:|---:|---:|---:|
| Neutral | aucun bonus | 10000 | 10000 | 10000 | 10000 | 10000 |
| RoyalGuard | protection | 9400 | 9600 | 8200 | 8500 | 10000 |
| Striker | assaut | 11500 | 11200 | 10800 | 11000 | 10500 |
| Nurturer | soutien | 9800 | 10000 | 9200 | 7800 | 8500 |
| Scout | reconnaissance | 10400 | 10600 | 9000 | 9000 | 9000 |
| Alchemist | assaut technique | 10800 | 11600 | 10000 | 10000 | 11500 |

Regles de niveau et classes:

- niveaux 1..9: la classe effective est toujours `Neutral`; une autre selection est conservee dans l'editeur mais retourne `class_inactive_below_level_10`;
- T3 et T4: une seule classe, toute classe autorisee;
- T5: deux classes distinctes, une de `{RoyalGuard,Nurturer}` et une de `{Striker,Scout,Alchemist}`;
- T6: trois classes distinctes avec `RoyalGuard`, une de `{Striker,Alchemist}` et une de `{Nurturer,Scout}`;
- T7: quatre classes distinctes: `RoyalGuard`, `Nurturer`, `Scout` et une de `{Striker,Alchemist}`;
- `Neutral` ne remplit aucun role de raid.

Pour un raid, chaque modificateur de classe est une moyenne ponderee tronquee:

```text
wing_weight = soldiers + guards + scouts + floor(workers / 2)
party_modifier_bp = floor(sum(wing_weight * wing_modifier_bp) / sum(wing_weight))
```

Pour un solo, le modificateur est celui de la classe effective.

## Adaptateur raid pour la ruche test existante

Le modele actuel expose une seule ruche joueuse. Le prototype peut creer des ailes virtuelles locales sans inventer de joueurs:

| Tier | Classes generees par defaut |
|---|---|
| T5 | `RoyalGuard`, `Striker` |
| T6 | `RoyalGuard`, `Scout`, `Alchemist` |
| T7 | `RoyalGuard`, `Nurturer`, `Scout`, `Alchemist` |

Les unites agregees sont reparties par division entiere; les restes vont dans l'ordre des ailes. Le niveau de chaque aile reprend le niveau de la ruche test. L'utilisateur peut ensuite editer chaque aile.

Ancre de regression T7 existante:

```text
level=35, soldiers=140, guards=86, scouts=70, workers=180
A = 140 + 86 + 70 + floor(180/2) + 2*35
A = 456
R = 336
readiness_bp = floor(456*10000/336) = 13571
```

Le prototype doit donc conserver `required=336` et `available=456` pour ce preset, avec `server=false` et `official_gain=false`.

## Portes de lancement

L'ordre des portes est fixe:

1. regle et tier valides;
2. `server=false`, `official_gain=false`, `local_only=true` forces;
3. attaquant et cible presents;
4. `attacker_health > 0` et `target_health > 0`;
5. aucune action combat deja en vol;
6. cooldown local termine;
7. mode conforme au tier;
8. nombre d'ailes et niveaux conformes;
9. roles de classes conformes;
10. minima S/G/E/O atteints;
11. `available_score >= required_score`.

Toute porte echouee retourne `gate=hold`, `outcome=blocked`, un code stable, zero degat, zero perte et zero cooldown.

Codes minimum:

- `invalid_rule_version`;
- `invalid_tier`;
- `missing_attacker`;
- `missing_target`;
- `attacker_defeated`;
- `target_already_defeated`;
- `action_in_progress`;
- `cooldown_active`;
- `mode_not_allowed`;
- `raid_wing_count`;
- `wing_level_out_of_band`;
- `class_composition`;
- `unit_minimum`;
- `score_below_required`.

## Calcul deterministe du preview

Tous les produits utilisent des entiers 64 bits. `floor` est la division entiere positive. `ceil_div(x,d) = floor((x+d-1)/d)`.

### 1. Readiness et pression

```text
readiness_bp = floor(A * 10000 / R)
pressure_bp = clamp(
  10000 + floor((readiness_bp - 10000) * 6000 / 10000),
  8500,
  14500)
```

La resolution ne s'execute que si `readiness_bp >= 10000` et si toutes les autres portes passent.

### 2. Degats a la cible

Avec `base_damage_bp` de la matrice et `class_damage_bp` du mode:

```text
damage_step_1_bp = floor(base_damage_bp * pressure_bp / 10000)
target_damage_bp = clamp(
  floor(damage_step_1_bp * class_damage_bp / 10000),
  100,
  5000)
projected_target_damage = min(
  target_health,
  max(1, floor(target_max_health * target_damage_bp / 10000)))
```

Le cap de 50 % empeche un one-shot de simple surcomposition. La cible riposte dans la meme resolution, meme si ses PV projetes atteignent zero.

### 3. Perte de PV attaquant

```text
overmatch_bp = max(0, readiness_bp - 10000)
counter_relief_bp = floor(overmatch_bp * 3500 / 10000)
raw_counter_bp = max(100, base_counter_bp - counter_relief_bp)
attacker_hp_loss_bp = clamp(
  floor(raw_counter_bp * received_hp_modifier_bp / 10000),
  100,
  5000)
projected_attacker_hp_loss = min(
  attacker_health,
  max(1, ceil_div(attacker_max_health * attacker_hp_loss_bp, 10000)))
```

Les PV sont reduits a zero au minimum; aucune valeur negative n'est serialisee.

### 4. Pertes d'unites

Les ouvrieres sont de la logistique et ne subissent pas de pertes directes dans cette version.

```text
loss_relief_bp = floor(overmatch_bp * 1500 / 10000)
raw_loss_bp = max(100, base_loss_bp - loss_relief_bp)
loss_rate_bp = clamp(
  floor(raw_loss_bp * casualty_modifier_bp / 10000),
  100,
  3000)
combatants = S + G + E
total_losses = min(
  combatants,
  ceil_div(combatants * loss_rate_bp, 10000))
```

Repartition par exposition:

```text
soldier_exposure = S * 60
guard_exposure = G * 25
scout_exposure = E * 15
```

Les pertes sont reparties proportionnellement aux expositions par methode des plus grands restes. Ordre de departage: soldats, gardiennes, eclaireuses. Une famille est bornee a son effectif; tout reste est redistribue selon le meme ordre parmi les familles non epuisees.

### 5. Etat apres resolution

```text
target_health_after = max(0, target_health - projected_target_damage)
attacker_health_after = max(0, attacker_health - projected_attacker_hp_loss)
```

Ordre d'issue:

1. deux valeurs a zero: `mutual_defeat`;
2. cible a zero: `target_defeated`;
3. attaquant a zero: `attacker_defeated`;
4. sinon: `engaged`.

`gate=ready` indique seulement que la sortie pouvait partir. L'adaptateur de preuve historique peut continuer a traduire `ready` en ancien `result=win`, mais la telemetrie Wave2 doit conserver `gate` et `outcome` separes.

## Cooldown preview

Le cooldown commence apres l'application locale d'une resolution eligible. Il ne repose jamais sur un temps serveur.

```text
cooldown_step_1 = ceil_div(base_cooldown_seconds * class_cooldown_bp, 10000)
outcome_cooldown_bp = 15000 si attacker_defeated ou mutual_defeat, sinon 10000
projected_cooldown_seconds = ceil_div(cooldown_step_1 * outcome_cooldown_bp, 10000)
```

Regles:

- `hold` ou preview sec sans application: aucun cooldown cree;
- `engaged` ou `target_defeated`: cooldown normal;
- `attacker_defeated` ou `mutual_defeat`: cooldown x1.5;
- horloge monotone locale injectable dans les tests;
- retour de l'horloge systeme: `remaining=max(0, previous_remaining-elapsed_monotonic)`;
- reset du laboratoire: cooldown local efface;
- les 4.05 s d'animation actuelle sont distinctes du cooldown et ne le remplacent pas.

## Preview puis application locale

Deux commandes sont separees:

1. `Preview`: calcule la sortie, n'ecrit aucun PV, aucune unite et aucun cooldown.
2. `Apply local`: recalcule avec le meme snapshot, verifie son empreinte, puis applique une seule fois.

L'empreinte contient au minimum `rule_version`, `encounter_id`, tier, mode, ailes, PV et compteur local d'action. Si une valeur change entre preview et application, retourner `stale_preview` sans mutation.

Un `preview_id` deja applique retourne `duplicate_apply` sans seconde perte. L'application locale ne modifie jamais stocks, capacites, ressources, XP ou progression.

## Telemetrie locale

Format minimum recommande:

```text
combat_preview rule=local_combat_balance_wave2_preview_v1 tier=T7 mode=raid_local required=336 available=456 gate=ready outcome=engaged target_hp_delta=-N attacker_hp_delta=-N soldier_loss=N guard_loss=N scout_loss=N cooldown_preview_s=N official_gain=false server=false
```

Pour un blocage:

```text
combat_preview rule=local_combat_balance_wave2_preview_v1 tier=T5 mode=solo_local required=200 available=N gate=hold outcome=blocked reason=mode_not_allowed official_gain=false server=false
```

Chaque ligne de combat, y compris erreur et reset, doit finir avec `official_gain=false server=false`.

## Parametres editables

### Par tier

- `level_min`, `level_max` dans 1..50;
- `required_score` dans 1..2000000;
- minima `soldiers`, `guards`, `scouts`, `workers` dans 0..999999;
- `target_max_health` dans 1..999999;
- `recommended_attacker_max_health` dans 1..999999;
- `base_damage_bp` dans 100..5000;
- `base_counter_bp` dans 100..5000;
- `base_loss_bp` dans 100..3000;
- `base_cooldown_seconds` dans 0..600;
- classes de preset et repartition des ailes.

### Par classe

- role local;
- `solo_damage_bp`, `raid_damage_bp` dans 5000..15000;
- `received_hp_modifier_bp` dans 5000..15000;
- `casualty_modifier_bp` dans 5000..15000;
- `cooldown_modifier_bp` dans 5000..15000.

### Globaux

- poids soldat/gardienne/eclaireuse/ouvriere: `1/1/1/0.5` par defaut;
- coefficient niveau: `2`;
- coefficient pression: `6000 bp`;
- soulagement contre: `3500 bp`;
- soulagement pertes: `1500 bp`;
- caps degats, contre et pertes;
- poids d'exposition `60/25/15`;
- multiplicateur cooldown de defaite `15000 bp`;
- durees visuelles combat `1.75/0.55/1.75 s`.

### Verrouilles

Ne sont jamais editables dans le panneau:

- `server=false`;
- `official_gain=false`;
- `local_only=true`;
- absence de recompenses et progression;
- arrondis et ordre des portes pour une `rule_version` donnee;
- T1..T4 solo et T5..T7 raid pour cette version.

Toute modification d'un parametre balance exige un nouvel identifiant de table, par exemple `wave2_local_balance_table_r2`, conserve dans la telemetrie.

## Presets locaux recommandes

### Seuil par tier

Creer sept presets `scenario_combat_threshold_t1` a `scenario_combat_threshold_t7` avec les minima de la matrice et le niveau minimal. Leur score doit etre exactement egal au score requis.

### T7 recu

`scenario_raid_t7_receipt_anchor`:

- niveau 35;
- soldats 140;
- gardiennes 86;
- eclaireuses 70;
- ouvrieres 180;
- PV attaquant 1200/1200;
- PV cible 1800/1800 pour le bestiaire preview;
- classes virtuelles `RoyalGuard,Nurturer,Scout,Alchemist`;
- score 456, requis 336;
- `server=false`, `official_gain=false`.

Le `maxHealth=900` de la ruche ennemie du preset historique reste une valeur de duel de ruches. Il ne doit pas remplacer les 1800 PV du bestiaire T7 preview sans choix explicite de `target_family`.

## Cas limites obligatoires

- Valeur negative: normaliser a zero, avertir, puis recalculer.
- Niveau 0 ou 51: normaliser pour l'editeur; refuser une application brute non normalisee.
- `health > max_health`: borner a `max_health`.
- `max_health <= 0`: normaliser a 1.
- Attaquant a 0 PV: bloquer sans pertes supplementaires.
- Cible a 0 PV: bloquer sans cooldown.
- Score suffisant mais soldats sous minimum: `unit_minimum`.
- Beaucoup d'ouvrieres mais aucune unite de combat: `unit_minimum`.
- T1..T4 en raid ou T5..T7 en solo: `mode_not_allowed`.
- Aile manquante, vide, classe dupliquee ou role absent: `raid_wing_count` ou `class_composition`.
- Classe speciale sous niveau 10: utiliser `Neutral` et avertir.
- Cooldown actif: preview informatif permis, application bloquee.
- Action 4.05 s deja en vol: application bloquee.
- Cible ou composition modifiee apres preview: `stale_preview`.
- Double clic ou retry du meme `preview_id`: une seule application.
- Pertes superieures a une famille: cap puis redistribution.
- Degat projetant les deux camps a zero: `mutual_defeat`.
- Comptes proches de 999999: calcul 64 bits, aucune valeur negative par overflow.
- Sauvegarde locale corrompue: reset local par defaut, invariants forces.
- Sauvegarde avec drapeaux vrais: les forcer a faux avant tout calcul.
- Reset en cooldown: restaurer uniquement le preset local.
- Changement de table pendant un preview: `stale_preview`.
- Aucun chemin ne modifie stock, capacite, ressource, XP, loot ou progression.

## Matrice de tests

| ID | Test | Attendu |
|---|---|---|
| LCB-001 | Charger un etat adversarial avec des valeurs d'autorite non conformes | Retablissement de `server=false` et `official_gain=false`; avertissement; aucune sortie distante. |
| LCB-002 | Preview et application sur chaque tier | Chaque sortie contient `server=false`, `official_gain=false`, `local_only=true`. |
| LCB-003 | Presets seuil T1..T7 | Scores exacts `28,56,84,112,200,264,336`; `gate=ready`. |
| LCB-004 | Retirer un soldat de chaque preset seuil | `gate=hold`, raison `score_below_required` ou `unit_minimum`, aucune mutation. |
| LCB-005 | Modes T1..T4 | `solo_local` accepte; `raid_local` bloque. |
| LCB-006 | Modes T5..T7 | `raid_local` accepte; `solo_local` bloque. |
| LCB-007 | Ancre T7 existante | `required=336`, `available=456`, `readiness_bp=13571`. |
| LCB-008 | T6 score 263 puis 264 | 263 `hold`; 264 `ready`. |
| LCB-009 | Score au-dessus du seuil mais une famille sous minimum | `unit_minimum`; zero degat/perte/cooldown. |
| LCB-010 | Classe `Striker` au niveau 9 | Classe effective `Neutral`; avertissement stable. |
| LCB-011 | Chaque classe au niveau 10 | Modificateurs egaux a la table; aucun effet sur `available_score`. |
| LCB-012 | T5 classes dupliquees | `class_composition`. |
| LCB-013 | T6 sans `RoyalGuard` | `class_composition`. |
| LCB-014 | T7 avec `RoyalGuard,Nurturer,Scout,Alchemist` | Porte classes PASS. |
| LCB-015 | T3 seuil, `RoyalGuard`, PV 450/450 contre 360/360 | Degat cible 108, perte PV attaquant 37, pertes totales 2, reparties `2/0/0`, cooldown 12 s, outcome `engaged`. |
| LCB-016 | T4 seuil, `Nurturer` | Cooldown preview `ceil(18*8500/10000)=16 s`. |
| LCB-017 | Preview sec | Aucun champ d'etat local ne change. |
| LCB-018 | Appliquer deux fois le meme `preview_id` | Premiere application unique; seconde `duplicate_apply`. |
| LCB-019 | Modifier un PV apres preview | `stale_preview`; aucune mutation. |
| LCB-020 | Attaquant a 0 PV | `attacker_defeated`; zero perte supplementaire. |
| LCB-021 | Cible a 0 PV | `target_already_defeated`; zero cooldown. |
| LCB-022 | Cooldown restant 1 s | Application bloquee; horloge injectee +1 s rend l'action disponible. |
| LCB-023 | Action combat en vol | `action_in_progress`; aucun double resultat. |
| LCB-024 | Pertes avec une famille a zero | Repartition sans division par zero et sans valeur negative. |
| LCB-025 | PV des deux camps projetes a zero | `mutual_defeat`; cooldown de defaite x1.5. |
| LCB-026 | Comptes 999999 et modificateurs max | Aucun overflow; sortie bornee et deterministe. |
| LCB-027 | Dix previews identiques | Sorties byte-for-byte identiques hors `preview_id` et horodatage local. |
| LCB-028 | Application eligible | Stocks, capacites, XP, loot et progression inchanges. |
| LCB-029 | Reset apres combat/cooldown | Preset local restaure, cooldown efface, drapeaux toujours faux. |
| LCB-030 | `legacy_hive_duel_v1` | Recu historique combat reste PASS et n'utilise pas la table Wave2. |
| LCB-031 | Animation combat | Duree 4.05 s conservee et distincte du cooldown preview. |
| LCB-032 | Toute ligne de telemetrie combat/reset/erreur | Termine par `official_gain=false server=false`. |

### Oracle detaille LCB-015

Entree T3 seuil: `L=10`, `S=36`, `G=12`, `E=6`, `O=20`, classe `RoyalGuard`.

```text
A = 36 + 12 + 6 + 10 + 20 = 84
R = 84
readiness_bp = 10000
pressure_bp = 10000
target_damage_bp = floor(3200 * 9400 / 10000) = 3008
projected_target_damage = floor(360 * 3008 / 10000) = 108
attacker_hp_loss_bp = floor(1000 * 8200 / 10000) = 820
projected_attacker_hp_loss = ceil(450 * 820 / 10000) = 37
loss_rate_bp = floor(250 * 8500 / 10000) = 212
total_losses = ceil(54 * 212 / 10000) = 2
```

Expositions: soldats `2160`, gardiennes `300`, eclaireuses `90`. La methode des plus grands restes attribue les deux pertes aux soldats.

## Criteres d'acceptation du prototype

Le prototype local sera considere conforme lorsque:

- tous les tests LCB-001 a LCB-032 passent;
- les preuves historiques restent sur `legacy_hive_duel_v1` et restent vertes;
- le preset T7 conserve 336/456;
- aucune requete reseau ou persistence officielle n'existe;
- aucun gain, loot, XP, ressource ou progression n'est produit;
- toutes les sorties et telemetries portent `server=false` et `official_gain=false`;
- les parametres actifs et leur version sont visibles dans le snapshot local;
- les calculs sont identiques sur executions repetees.

## Decision finale

Le socle local dispose deja des champs editables, du combat deterministe, des tiers T1..T7, des modes solo/raid et des garde-fous non officiels. La presente specification ajoute une matrice exploitable, un score lie aux unites reelles, des effets de classes, des PV, des pertes, des cooldowns preview, des cas limites et un oracle de test sans remettre en cause les recus existants.

`READY_FOR_LOCAL_COMBAT_BALANCE_PROTOTYPE=YES`

`server=false`

`official_gain=false`
