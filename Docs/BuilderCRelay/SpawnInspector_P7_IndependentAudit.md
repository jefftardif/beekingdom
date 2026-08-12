# WorldMap Spawn Inspector P7 - Audit technique independant Builder-C

Date locale: 2026-07-15

## Mandat et limites

Cet audit couvre uniquement les invariants documentaires du Spawn Inspector P7.
Il ne relit ni code, ni scene, ni log Unity, ni PNG, ni APK et ne depend
d'aucun ancien thread.

Sources examinees, et seulement celles-ci:

- Docs/BuilderCRelay/WorldMapSpawnDistribution_TechnicalContract.md
- Docs/WorldMapRuntimeEntitiesWave1/SpawnInspectorIntegration_Report.md
- Docs/BuilderA/WorldMapRuntimeEntitiesWave1/SpawnInspectorProof/SpawnInspectorProofReceipt.md
- Docs/BuilderCRelay/WorldMap50x50_RuntimePerformanceContract.md

Le rapport d'integration et le recu constituent des declarations de preuve. Sans
artefact brut autorise dans ce mandat, l'audit peut controler leur coherence avec
les contrats, mais pas reproduire leur execution.

## Echelle d'audit

| Statut | Sens |
| --- | --- |
| CONFORME_SUR_PREUVE | La valeur observee et la regle contractuelle sont explicites et coherentes. |
| PARTIEL | Une preuve positive existe, mais ne couvre pas tout l'invariant. |
| NON_PROUVE | Le contrat exige le comportement, mais les deux preuves P7 ne donnent pas la mesure necessaire. |
| ECART_A_RESOUDRE | Les documents emploient des modeles incompatibles ou insuffisamment definis. |

## Verdict executif

Le coeur proof-first est credible: deux hashes differents sont fournis, la
repetition meme seed/version est declaree deterministe, la fenetre observee reste
a 25 chunks, les populations observees restent sous les caps et le client est
declare local sans gain officiel.

Le PASS independant complet n'est toutefois pas justifie. Les points bloquants
sont la representation du seed reel, la canonicalisation du hash, les exclusions
BearDen/eau/falaise sans hit positif, l'absence des rejets distance/budget, et
l'absence de mesures P7 propres au cache, aux allocations et au temps CPU.

- BUILDER_C_P7_AUDIT=CONDITIONAL_PASS
- READY_FOR_P8_TELEMETRY=YES

CONDITIONAL_PASS autorise l'instrumentation P8 et la poursuite d'une preview
locale. Il ne valide ni production, ni autorite serveur, ni economie, ni combat,
ni raid, ni respawn officiel.

## Valeurs de preuve retenues

| Mesure | Valeur declaree |
| --- | --- |
| Hash Seed A | 01b78336 |
| Hash Seed B | fef6f1b4 |
| Chunks actifs | 25 |
| Ruches actives | 2 |
| Ressources actives | 11 |
| Menaces actives | 7 |
| Hits BearDen/eau/falaise/evenement | 0/0/0/25 |
| Overlay diagnostic par defaut | OFF |
| Serveur | false |
| Gain officiel | false |
| Regression P1-P6 | PASS declare |

Les valeurs exactes des Seeds A et B ne figurent pas dans les preuves. Les deux
hashes ne peuvent donc pas servir de golden values reproductibles en l'etat.

## Audit des invariants

| ID | Invariant contractuel | Evidence P7 | Statut | Conclusion independante |
| --- | --- | --- | --- | --- |
| P7-DET-01 | Meme contexte versionne produit memes candidats, IDs, acceptes, rejets et hash. | Meme seed/version est declare PASS; le rapport ajoute hashes, IDs, positions et tiers/richesses identiques. | PARTIEL | Le contexte complet, les listes de candidats et les rejets ne sont pas recus. |
| P7-DET-02 | Une variation de seed change le hash et la distribution. | 01b78336 differe de fef6f1b4 et SEED_VARIATION=PASS. | CONFORME_SUR_PREUVE | Un couple est positif, mais les valeurs de seed manquent. |
| P7-DET-03 | Une variation de table, exclusion ou grille change le hash. | Aucun test de mutation de version n'est rapporte. | NON_PROUVE | Les trois axes doivent etre testes separement en P8. |
| P7-DET-04 | L'ordre fichiers, frames, chunks et chargements n'influence pas la sortie. | Aucune permutation d'ordre ni reentree de fenetre n'est rapportee. | NON_PROUVE | Un test d'ordre inverse et un retour apres eviction sont requis. |
| P7-DET-05 | Le generateur n'utilise ni temps, ni frame count, ni random global, ni GetHashCode. | Interdiction presente dans le contrat seulement. | NON_PROUVE | La telemetrie doit prouver l'egalite apres nouvelle session et ordres differents. |
| P7-ID-01 | L'ID preview suit le format contractuel et reste stable. | Le rapport annonce preview:{world}:{grid}:family:chunk:slot:version:seed. Le contrat termine par spawn_seed_version sans seed reel. | ECART_A_RESOUDRE | Le schema d'ID doit etre choisi, versionne et couvert par un digest; aucun exemple d'ID runtime n'est recu. |
| P7-SEED-01 | Toutes les entrees deterministes sont explicites. | Le contrat enumere spawn_seed_version mais aucun spawn_seed_value; l'integration expose un seed editable et Seed A/B. | ECART_A_RESOUDRE | Impossible de reconstruire exactement un run tant que la valeur et son encodage canonique ne sont pas recus. |
| P7-HASH-01 | Le hash est stable bit-a-bit au niveau logique. | Deux hashes 32-bit sont recus, sans algorithme runtime ni payload canonique. | PARTIEL | FNV-1a ou xxHash32 equivalent n'est pas un choix canonique; ordre, flottants et champs inclus restent inconnus. |
| P7-CAP-01 | Fenetre active <= 25 chunks. | 25 chunks observes. | CONFORME_SUR_PREUVE | La borne haute passe sur le run recu. |
| P7-CAP-02 | Coin valide a 9 chunks; centre, NW, SE et plus dense respectent les caps. | Aucun cas coin ou densest P7 n'est detaille. Le contrat performance donne un baseline anterieur, non une mesure Spawn Inspector P7. | NON_PROUVE | P8 doit identifier chaque scenario et son set de chunks. |
| P7-CAP-03 | Caps fenetre: ruches 25, ressources 75, bestiaire 25, evenements 8. | 2/11/7 sous les caps; aucun compte d'evenements actifs. | PARTIEL | Le run nominal passe pour trois familles seulement. |
| P7-CAP-04 | Caps chunk: hive 1, resource 3, bestiary 1, event 1. | Aucun maximum par chunk ni liste d'anchors proof n'est recu. | NON_PROUVE | Le total fenetre ne prouve pas les caps locaux. |
| P7-EXC-01 | BearDen bloque hive, resource et bestiary. | BearDen hits=0 alors que EXCLUSION_ZONES=PASS. | NON_PROUVE | Zero hit ne constitue pas une preuve positive de blocage. |
| P7-EXC-02 | Eau bloque les entites terrestres selon table. | Water hits=0. | NON_PROUVE | Une fixture avec candidat terrestre dans l'eau est requise. |
| P7-EXC-03 | Falaise bloque ruches et ressources interactives. | Cliff hits=0. | NON_PROUVE | Une fixture positive par famille bloquee est requise. |
| P7-EXC-04 | Evenement bloque ou reserve selon sa version. | Event hits=25. | PARTIEL | Le chemin est exerce, mais ni volume_id, ni candidat, ni disposition ne sont recus. |
| P7-EXC-05 | Exclusion precede distance puis budget et tout rejet est inspectable. | Compteurs globaux uniquement. | NON_PROUVE | Aucun rejet structure candidate_id/phase/detail n'est recu. |
| P7-DIST-01 | Les six distances minimales et l'ordre de priorite sont deterministes. | Aucun check, minimum observe ou rejet distance n'est rapporte. | NON_PROUVE | Un compteur de violation et des fixtures de frontiere sont requis. |
| P7-AUTH-01 | Toute sortie locale porte official=false. | Badge local, server=false et official_gain=false. | PARTIEL | Le champ Official de chaque inspection et entite n'est pas compte. |
| P7-AUTH-02 | Aucun gain, combat, raid, respawn ou persistence officiel n'est calcule. | official_gain=false et absence serveur declares. | PARTIEL | Combat, raid, respawn et persistence ne disposent pas de compteurs recus. |
| P7-INS-01 | L'inspection expose acceptes, rejetes, hash, caps et compteurs. | UI, detail selection, hash et populations sont declares PASS. | PARTIEL | Les rejets complets, leur ordre et les compteurs de cap ne sont pas fournis. |
| P7-MIG-01 | world_coord_normalized est disponible et toute reprojection ecrit un audit hash. | Le detail UI expose une coordonnee normalisee; aucune reprojection n'est testee. | PARTIEL | La presence par entite et migration_audit_hash restent a prouver. |
| P7-PERF-01 | Steady-state apres warmup vise 0 B/frame; switch <= 32 KB. | Aucune allocation Spawn Inspector P7 n'est recue. | NON_PROUVE | Le baseline 50x50 a 0 B ne couvre pas automatiquement ce panneau/generateur. |
| P7-PERF-02 | Le stress 50x50 alloue <= 2,000,000 B et ne remplit pas chunkCache. | Le contrat performance rapporte 0 B et cache 25/25 sur sa preuve propre. | PARTIEL | Baseline favorable, mais correlation avec seed/hash/exclusions P7 absente. |
| P7-PERF-03 | Cache terrain <= 96 et aucun terrain 50x50 n'est cree. | Le contrat donne <=96; l'integration exclut terrain 50x50 et PNG. | PARTIEL | Aucun cache before/after n'est present dans le recu P7. |
| P7-PERF-04 | Spike CPU simulation pan/zoom <= 4 ms. | Aucune mesure de temps P7. | NON_PROUVE | P8 doit isoler la simulation de l'UI et du rendu. |
| P7-UI-01 | Overlay diagnostic OFF par defaut. | Valeur OFF dans rapport et recu. | CONFORME_SUR_PREUVE | Invariant explicitement recu. |
| P7-REG-01 | P1-P6 non regresses. | PASS declare dans rapport et recu. | PARTIEL | Aucun detail de sous-gate n'est autorise dans ce mandat. |

## Ecarts prioritaires

### P7-A01 - Seed reel absent du contexte canonique

Severite: bloquant pour un PASS deterministe complet.

Le contexte contractuel contient spawn_seed_version, qui versionne l'algorithme,
mais pas la valeur editable Seed A/Seed B. Le pseudo-code StableHash ne prend pas
non plus cette valeur. A l'inverse, le rapport affirme qu'un seed editable change
la distribution et decrit un ID termine par seed.

Resolution P8:

- ajouter spawn_seed_value et spawn_seed_encoding a la telemetrie;
- separer clairement valeur de seed et version d'algorithme;
- fixer si le seed reel appartient ou non a l'ID preview;
- inclure la valeur canonique dans context_hash et inspection_audit_digest.

### P7-A02 - Digest non reconstructible

Severite: bloquant pour une comparaison independante.

Les hashes 01b78336 et fef6f1b4 ne precisent ni seed, ni algorithme effectif, ni
ordre des champs, ni serialisation des coordonnees, ni contenu des acceptes et
rejetes. Un digest canonique distinct du hash historique est requis.

### P7-A03 - Couverture d'exclusion insuffisante

Severite: bloquant pour le gate EXCLUSION_ZONES independant.

BearDen, eau et falaise ont chacun zero hit. Seul evenement a une valeur positive.
Le PASS global ne demontre donc pas les trois chemins principaux. P8 doit utiliser
des fixtures dediees et conserver le candidat, le volume, la phase et la raison.

### P7-A04 - Caps et distances non auditables

Severite: elevee.

Les totaux 2/11/7 sont conformes a la fenetre observee, mais ne prouvent ni les
caps par chunk, ni les evenements, ni les distances, ni l'ordre des priorites.
La conservation candidates = accepted + rejected doit etre mesuree.

### P7-A05 - Performance P7 non instrumentee

Severite: elevee avant extension 50x50.

Le contrat performance fournit un baseline utile, mais le recu Spawn Inspector ne
donne ni cache before/after, ni allocation, ni temps. La collecte P8 doit exclure
sa propre serialisation de la boucle mesuree.

### P7-A06 - Frontiere d'autorite seulement partielle

Severite: elevee avant toute integration serveur.

server=false et official_gain=false sont conformes, mais ils ne comptent pas les
champs official par snapshot/entite ni les tentatives de combat, raid, respawn ou
persistence. Tous ces compteurs doivent rester a zero.

## Conditions de promotion vers PASS

BUILDER_C_P7_AUDIT pourra devenir PASS seulement si un recu P8 lisible fournit:

1. Le contexte complet avec seed reel, encodage et toutes les versions.
2. Un digest canonique reproductible pour contexte, candidats, acceptes et rejets.
3. Zero mismatch sur repetition, nouvelle session, reentree et permutation d'ordre.
4. Une couverture positive BearDen, eau, falaise et evenement sans accepte interdit.
5. Les caps par chunk/fenetre, les distances et la conservation de tous les candidats.
6. Cache avant/apres, allocations et temps selon les seuils de la specification P8.
7. Zero sortie official=true, zero gain/action/respawn/persistence officiel et zero appel serveur.

## Gate final

BUILDER_C_P7_AUDIT=CONDITIONAL_PASS
READY_FOR_P8_TELEMETRY=YES

