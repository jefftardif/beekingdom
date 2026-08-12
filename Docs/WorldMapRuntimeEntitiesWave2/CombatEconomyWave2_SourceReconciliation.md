# Combat/Economy Wave2 - Source Reconciliation

Date locale: 2026-07-15
Statut: rapport de reconciliation documentaire
Verdict: PASS_WITH_REPORT_ONLY_RESERVES

## Objet

Ce rapport reconcilie les livrables Combat/Economy Wave2 avec les deux sources locales:

- `CombatBalanceLocalLabSpec.md`
- `ResourceSpawnEconomySpec.md`

Il accompagne le manifeste:

- `Docs/WorldMapRuntimeEntitiesWave2/CombatEconomyWave2_DeterministicManifest.md`

Aucun Unity, serveur, PNG, APK, test runtime ou build n'est produit.

## Resultat synthetique

| Gate | Resultat | Decision |
|---|---|---|
| Sources presentes | PASS | Les deux specs Wave2 sont dans `Docs/WorldMapRuntimeEntitiesWave2`. |
| Sources consommees | PASS | Les invariants, matrices, IDs, tests et non-claims sont repris. |
| Manifeste deterministe | PASS | Scenario canonique avec seed, digest, ressources, collecte et raid. |
| Combat reconcile | PASS | T7 336/456, LCB-015, T1..T7 et invariants preserves. |
| Economy reconcile | PASS | Seed preview, IDs, caps, cooldown, collecte et hashes preserves. |
| Rapports seulement | PASS | Aucun runtime ou artefact produit. |
| Production officielle | FAIL_EXPECTED | Hors scope et interdite par les deux specs. |

Verdict final: `PASS_WITH_REPORT_ONLY_RESERVES`

## Reconciliation Combat

| Source Combat | Exigence | Manifestation dans le livrable | Resultat |
|---|---|---|---|
| Regle cible | `local_combat_balance_wave2_preview_v1` | Utilisee dans `RAID-001` et `RAID-NEG-001`. | PASS |
| Invariants | `server=false`, `official_gain=false`, `local_only=true` | Repete dans invariants, raid, telemetrie et refus. | PASS |
| Coexistence legacy | `legacy_hive_duel_v1` ne doit pas etre remplace silencieusement | Le manifeste cite Wave2 uniquement et conserve LCB-015 comme oracle; aucun recu legacy regenere. | PASS |
| Ruche test stable | `PLAYER_TEST_HIVE` | Ruche test canonique du manifeste. | PASS |
| Ruche ennemie stable | `ENEMY_TEST_HIVE` | Ruche ennemie locale T7 du manifeste. | PASS |
| Niveaux | 1..50 | Ruche test et ailes T7 au niveau 35. | PASS |
| Unites | soldiers, guards, scouts, workers | 140/86/70/180, avec wings virtuelles. | PASS |
| T1..T4 solo | `solo_local` | Repris dans delivery; le manifeste cible T7 et refuse T7 solo. | PASS |
| T5..T7 raid | `raid_local` | `RAID-001` T7 accepte; `RAID-NEG-001` T7 solo refuse. | PASS |
| Ancre T7 | `required=336`, `available=456`, `readiness_bp=13571` | Valeurs exactes dans le manifeste. | PASS |
| Cooldown local | Pas de temps serveur | `projected_cooldown_seconds=59`, local only. | PASS |
| Preview vs apply | Preview peut etre sans mutation; application locale explicite seulement | Le manifeste documente les sorties attendues, sans mutation officielle. | PASS |
| LCB-015 | T3 RoyalGuard: damage 108, HP loss 37, pertes 2/0/0, cooldown 12 | Oracle conserve dans le manifeste. | PASS |
| LCB-032 | Telemetrie finit par `official_gain=false server=false` | Les deux lignes telemetry du manifeste respectent l'ordre final. | PASS |

## Reconciliation Resource/Economy

| Source Resource | Exigence | Manifestation dans le livrable | Resultat |
|---|---|---|---|
| Autorite | `source_kind=seed_preview`, `official=false`, `official_gain=false` | Invariants globaux et receipts de collecte. | PASS |
| Seed obligatoire | `spawn_seed_value` non vide, max 128 octets UTF-8 | Seed canonique publie avec SHA-256. | PASS |
| Version seed | `resource_spawn_v2` | Utilisee dans contexte et IDs. | PASS |
| IDs Wave2 | Format `preview:{world_id}:{world_grid_version}:resource:{chunk}:r{slot}:{spawn_seed_version}:{seed_digest16}:{distribution_table_version}` | Sept IDs ressources conformes. | PASS |
| Ressources couvertes | nectar, pollen, water, wax, honey, royal_jelly, propolis | Les sept familles apparaissent une fois. | PASS |
| R1/R2/R3 | Tiers locaux preview | Manifest: R1, R2 et R3 presents. | PASS |
| Capacity/remaining | `remaining` local, pas inventaire | Tables ressources et receipts. | PASS |
| Collecte | Decrement simule, pas gain officiel | `COL-001` de 84 a 72, inventory `{}`. | PASS |
| Collecte cooldown | Rejet sur cooldown/depleted/suppressed | `COL-NEG-001` rejete avec due tick 1400. | PASS |
| Respawn | Horloge demo, ID/capacity/position stables | `RES-007` conserve ID/capacity, cooldown local. | PASS |
| Hash separation | Collecte/cooldown ne changent pas `base_distribution_hash` | Le manifeste note base unchanged, runtime changed. | PASS |
| Caps | Chunk/fenetre/rares | Cap check explicite, 7 ressources, 1 R3. | PASS |
| Contention | Lease court et decrement atomique | `COL-001` lock jusqu'au tick 120. | PASS |
| Anti-farm | Retarde/refuse sans changer distribution de base | `farm_heat_cell=3`, pas de suppression, base unchanged. | PASS |
| RSE-NEG-016 | `official=true` ou gain officiel = hard fail | `REPLAY-AUTH-001` invalide le scenario. | PASS |

## Ecarts et resolutions

| Sujet | Observation | Resolution |
|---|---|---|
| Ruche ennemie vs bestiaire | La source Combat permet `target_family=bestiary|test_hive`; la demande utilisateur nomme une ruche ennemie. | Le manifeste choisit `ENEMY_TEST_HIVE` avec `target_family=test_hive`, tout en gardant la matrice T7. |
| Raid T7 et cible PV | Le preset historique mentionne une ruche ennemie a 900 PV, mais la source precise que le bestiaire T7 preview utilise 1800 PV sauf choix explicite. | Le manifeste choisit 1800 PV pour rester aligne avec la matrice Wave2 T7. |
| Resource generation vs manifest fixe | La source definit l'algorithme, pas une liste canonique de noeuds. | Le manifeste fige une liste de sept noeuds comme scenario de reprise; l'implementation future devra prouver que son generateur reproduit ou documente cette table. |
| Hashes complets | La source exige `base_distribution_hash` et `runtime_availability_hash`, mais aucun prototype ne les calcule ici. | Le rapport publie un `scenario_manifest_digest` documentaire et reserve les hashes runtime au prototype local futur. |
| JSON vs rapport | Le precedent checkpoint demandait `Wave2_LocalScenarioManifest.json` ou equivalent Markdown. | Un equivalent Markdown est publie pour respecter "rapports seulement". |

## Non-claims preserves

Le manifeste et la reconciliation n'autorisent pas:

- serveur live;
- endpoint officiel;
- matchmaking;
- joueur adverse reel;
- raid officiel;
- guerre/alliance officielle;
- economie officielle;
- inventaire officiel;
- recompense;
- XP;
- progression;
- persistence officielle;
- APK/device proof;
- PNG ou capture visuelle.

## Handoff documentaire

| Livrable | Chemin | Verdict |
|---|---|---|
| Manifeste deterministe | `C:\projets\beekingdomgame-master\Docs\WorldMapRuntimeEntitiesWave2\CombatEconomyWave2_DeterministicManifest.md` | PASS_REPORT_ONLY |
| Reconciliation sources | `C:\projets\beekingdomgame-master\Docs\WorldMapRuntimeEntitiesWave2\CombatEconomyWave2_SourceReconciliation.md` | PASS_WITH_REPORT_ONLY_RESERVES |

## Verdict

`PASS_WITH_REPORT_ONLY_RESERVES`

La reconciliation des sources est complete pour un handoff documentaire. Les reserves restantes sont normales pour cette demande: pas d'implementation, pas de tests runtime, pas de validation Unity, pas de serveur/live, pas de PNG et pas d'APK.
