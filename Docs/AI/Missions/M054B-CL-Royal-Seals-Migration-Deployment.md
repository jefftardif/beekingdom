# M054B-CL — Royal Seals Migration Deployment

## Contexte

M054 (portefeuille joueur) et M054A (correction de l'attribution exacte)
étaient acceptés mais non déployés, et la migration des soldes legacy
n'était ni câblée à un endpoint ni exécutée. Cette mission déploie le code,
câble un mécanisme de migration protégé miroir de la convention
`/ops/migrations/*`, puis exécute la migration en production sous
supervision explicite du CEO (prévisualisation en lecture seule d'abord,
confirmation, puis application réelle).

## 1. Isolation du worktree partagé

Deux commits distincts, chacun vérifié `git status`/`git diff` avant
`git add`, aucun fichier non lié touché :

- `2f19922b` — le travail M054/M054A déjà préparé (portefeuille joueur +
  correction d'attribution exacte), aucun hunk mélangé détecté sur les 13
  fichiers concernés.
- `f1674387` — l'ajout de cette mission (endpoints ops + mode `dryRun` +
  1 nouveau test), 4 fichiers, chacun avec un diff propre (`Program.cs`
  n'avait qu'un seul hunk, l'ajout des deux nouvelles routes).

`git status --short` après chaque commit confirme que tout le travail non
lié (BuildingInteractionController, bootstraps HiveMap, LivingHiveMenuCanvas,
police Cinzel, EditorBuildSettings, rapports de mission antérieurs non
commités) reste exactement intact.

## 2. Mécanisme de migration câblé

Deux nouveaux endpoints, miroir exact de `/ops/migrations/pending` +
`/ops/migrations/apply` :

- `GET /ops/royal-seals-migration/preview` — Ops:AdminKey uniquement,
  purement en lecture (`RoyalSealsMigrationService.MigrateAsync(dryRun: true)`),
  n'écrit jamais dans `PlayerHiveState`.
- `POST /ops/royal-seals-migration/apply` — Ops:AdminKey **et**
  Ops:MigrationApplyKey requis (les deux clés distinctes, comme pour les
  migrations SQL), seul appel qui écrit réellement.

Nouveau test dédié (`Migration_DryRun_ReportsAccurateCountsWithoutWritingAnything`)
prouve que l'aperçu rapporte les comptes exacts sans jamais écrire, qu'un
apply réel après l'aperçu produit le même résultat, et qu'un aperçu après
l'apply rapporte correctement « déjà migré ».

## 3. Vérifications pré-déploiement

- `AllianceResearchServiceTests` : **77/77 verts** (76 hérités de M054A + 1
  nouveau test dry-run).
- `BeeKingdom.HiveOperations.Tests` : **181/181**, aucune régression.
- Suite complète : 1 seul échec constant, pré-existant et documenté depuis
  M051/M052 (`CatalogSqlMatchesCheckedInScriptFiles`, sans rapport avec
  Alliance Research).
- Unity : Play Mode confirmé inactif, `AllianceResearchClientTests` **8/8**,
  compilation propre — aucun changement Unity nécessaire (contrat DTO
  inchangé).

## 4. Déploiement

- `git push origin main` (deux fois, un par commit) : réussi.
- `git push origin main:deploy` (deux fois) : réussi, déclenchant les
  workflows GitHub Actions `Deploy BeeKingdomApi` (runs `33934025840` puis
  `33970565878`), tous deux **succès**. Zéro mention de « migration » dans
  les deux journaux complets — aucune migration SQL demandée ni exécutée,
  conforme à l'attente (le portefeuille n'exige aucun changement de schéma).
- `GET /health` : `200 Healthy` après chaque déploiement.

## 5. Exécution de la migration — sous supervision CEO

**Étape 1 — Aperçu en lecture seule** (`GET /ops/royal-seals-migration/preview`,
Admin Key fournie par le CEO) :

```json
{"allianceRowsScanned":1,"legacyBalancesFound":2,"playersCredited":2,
 "alreadyMigratedSkipped":0,"playersWithNoOwnedHive":0,
 "totalRoyalSealsMigrated":650}
```

Résultat présenté au CEO avant toute écriture : 1 ligne Alliance scannée
(Alliance Test [BKT]), 2 soldes legacy trouvés, 650 Sceaux Royaux au total —
correspond exactement aux valeurs certifiées par le CEO en M053B (Stara ≥
50, Jeff ≥ 600 → 50 + 600 = 650). Aucune surprise, aucun joueur inattendu,
aucun joueur sans ruche possédée.

**Étape 2 — Autorisation explicite du CEO** : « Yes, proceed with the apply ».

**Étape 3 — Application réelle** (`POST /ops/royal-seals-migration/apply`,
Admin Key + Migration Key) :

```json
{"allianceRowsScanned":1,"legacyBalancesFound":2,"playersCredited":2,
 "alreadyMigratedSkipped":0,"playersWithNoOwnedHive":0,
 "totalRoyalSealsMigrated":650}
```

Résultat identique à l'aperçu — 2 joueurs crédités, 650 Sceaux Royaux migrés
au total, exactement comme annoncé.

**Étape 4 — Vérification d'idempotence en direct** (`GET .../preview` rejoué
immédiatement après) :

```json
{"allianceRowsScanned":1,"legacyBalancesFound":2,"playersCredited":0,
 "alreadyMigratedSkipped":2,"playersWithNoOwnedHive":0,
 "totalRoyalSealsMigrated":0}
```

Confirme l'idempotence réelle en production : un second appel voit
maintenant les 2 joueurs comme « déjà migrés », ne crédite rien de plus.

**Étape 5 — Vérification finale** : `GET /health` → `200 Healthy` ;
`GET /alliance/v1/alliances/search?nameOrTag=BKT` et `/by-slug/alliance-test`
→ Alliance Test toujours 2 membres, `InviteOnly`, Stara toujours Chef,
`activeWarCount: 0` — aucun état Alliance affecté par la migration.

## 6. Ce qui N'A PAS été fait

Aucune sélection de cible de financement, aucun don, aucun lancement de
recherche, aucun SpeedUp, aucun changement de rôle/adhésion, aucune
réinitialisation d'Alliance — la migration ne touche que
`PlayerHiveState.RoyalSeals` et `PlayerHiveState.Receipts` (clé
`royal-seals-migration:*`), rien d'autre.

## Checklist finale

A. M054/M054A hunks isolated safely? **YES**
B. Unrelated working-tree changes preserved? **YES**
C. Ops migration endpoints wired (Admin Key + Migration Key)? **YES**
D. Dry-run preview matches real apply outcome? **YES** (650/650, 2/2 joueurs)
E. Focused server tests green? **YES** — 77/77 (was 76/76)
F. HiveOperations.Tests green? **YES** — 181/181
G. Unity tests green? **YES** — 8/8
H. Unity compile green? **YES**
I. SQL migration required? **NO**
J. Commits created? **YES** — `2f19922b`, `f1674387`
K. Push main successful? **YES** (both)
L. Push deploy successful? **YES** (both)
M. Deployment workflows successful? **YES** — runs `33934025840`, `33970565878`
N. /health Healthy (post-deploy, post-migration)? **YES**
O. Migration preview shown to CEO before apply? **YES**
P. CEO explicit authorization obtained before apply? **YES** — "Yes, proceed with the apply"
Q. Migration applied successfully? **YES** — 2 players, 650 Royal Seals total
R. Migration idempotency verified live (re-run after apply)? **YES** — 0 credited, 2 already-migrated
S. Alliance Test preserved (membership, Chef, status)? **YES**
T. Any unauthorized production mutation (funding/donate/launch/speedup/role/membership)? **NO**
U. Jeff/Stara balances migrated as expected? **YES** — matches M053B-certified values exactly (50 + 600 = 650)

M054B DEPLOYED — ROYAL SEALS MIGRATION APPLIED — 2 PLAYERS CREDITED, 650 TOTAL — NO UNAUTHORIZED PRODUCTION MUTATION — IDEMPOTENCY VERIFIED LIVE.
