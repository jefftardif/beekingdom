# M053B-CL — Deploy Alliance Research Bonus Freshness Fix

## Contexte

M053 avait découvert et corrigé, côté code uniquement, un compromis
documenté depuis M052 : un minuteur de recherche objectivement expiré
pouvait laisser le bonus de gameplay périmé jusqu'à ce qu'une lecture/
mutation Alliance Research le résolve en `Completed`. Cette mission déploie
**exclusivement** ce correctif ponctuel — rien d'autre du périmètre M053
(Sceaux Royaux, catalogue, SpeedUp) n'a été touché, conformément à
l'instruction explicite de laisser Sceaux Royaux à M054.

## 1. Isolation du worktree partagé

`git status`/`git diff` avant tout `git add` ont confirmé que les deux
fichiers pertinents (`AllianceResearchBonusResolver.cs`,
`AllianceResearchServiceTests.cs`) n'avaient été touchés par **aucun**
autre travail concurrent — chaque fichier a produit un diff propre (3 hunks
et 2 hunks respectivement, tous M053), sans mélange à isoler par patch de
hunk. Le travail non lié en cours (`BuildingInteractionController.cs`,
bootstraps HiveMap, `LivingHiveMenuCanvas.cs`, police Cinzel,
`EditorBuildSettings.asset`, rapports de mission antérieurs non commités
M047/M048/M049/M049B/M049C/M051B/M052B) est resté strictement intact,
confirmé par un `git status --short` identique avant et après le commit.

Aucune commande destructive utilisée (`git add .`/`-A`, `git reset --hard`,
`git checkout .`, `git restore .`, `git clean`, `git stash` d'un fichier non
lié).

## 2. Vérifications pré-commit

- `AllianceResearchServiceTests` : **58/58 verts**.
- Les deux tests de fraîcheur explicitement requis par la mission, exécutés
  isolément : `Bonus_CountsElapsedButNotYetPersistedResearch_WithoutRequiringAReadFirst`
  et `Bonus_DoesNotCountResearchThatHasNotYetElapsed` — **2/2 verts**.
  Invariant confirmé : `CompletesAtUtc <= now` ⇒ bonus actif immédiatement
  (sans lecture préalable) ; `CompletesAtUtc > now` ⇒ bonus jamais activé
  prématurément.
- `BeeKingdom.HiveOperations.Tests` : **181/181 verts**, aucune régression
  (consommateur indirect du resolver via `AllianceGameplayBonusResolverAdapter`).
- Unity : Play Mode confirmé inactif (`Application.isPlaying=False` via
  sonde `script-execute` en lecture seule) avant `assets-refresh` et
  exécution des tests. `AllianceResearchClientTests` : **8/8 verts**,
  compilation Unity propre, 0 erreur.
- Le resolver reste strictement en lecture seule pour ce comportement de
  fraîcheur — confirmé par relecture du code : aucune écriture, aucun
  polling, aucun worker/scheduler en arrière-plan introduit.

## 3. Commit

Un seul commit focalisé, exactement les 3 fichiers attendus :

```
Server/src/BeeKingdom.Alliance/Research/AllianceResearchBonusResolver.cs
Server/tests/BeeKingdom.Tests/AllianceResearchServiceTests.cs
Docs/AI/Missions/M053-CL-Alliance-Research-Major-SpeedUp-Certification.md
```

Hash : `ec94b9af` — message `fix(alliance): resolve elapsed research
bonuses immediately`.

`git status --short` après le commit confirme que tout le travail non lié
reste exactement dans son état d'avant.

## 4. Push / Déploiement

- `git push origin main` : réussi (`2e673d78..ec94b9af main -> main`).
- `git push origin main:deploy` : réussi (`2e673d78..ec94b9af main -> deploy`),
  déclenchant le workflow GitHub Actions `Deploy BeeKingdomApi`
  (run `33934025840`).
- Workflow : **succès** (`gh run watch --exit-status`). Étapes confirmées :
  site hors ligne, fichiers publiés copiés, site remis en ligne, test de
  fumée réussi. **Zéro mention de « migration »** dans le journal complet
  du run — aucune migration demandée ni exécutée, conforme à l'attente.
  Seuls des avertissements de compilation pré-existants et sans rapport
  (nullable reference warnings dans `Program.cs`, `PlayerDirectoryService.cs`,
  `CombatPatrolService.cs`) apparaissent dans les annotations, aucune erreur.

## 5. Vérification post-déploiement

```
GET https://api-ops.beekingdomgame.com/health -> 200
{"service":"BeeKingdom.Server","status":"Healthy", ...}
```

Vérifications en lecture seule uniquement :
- `GET /alliance/v1/alliances/search?nameOrTag=BKT` : Alliance Test [BKT]
  toujours présente, `memberCount: 2`, `joinMode: InviteOnly`.
- `GET /alliance/v1/alliances/by-slug/alliance-test` : `leader.displayName:
  "Stara"` — Stara reste Chef. `status: Active`, `createdAtUtc` inchangé,
  aucune diplomatie/guerre active.

Aucun appel authentifié, aucune mutation (sélection de cible, don,
lancement, SpeedUp, rôle, adhésion, réinitialisation) n'a été effectué à
aucun moment de cette mission.

## Checklist finale

A. M053 freshness hunks safely isolated? **YES**
B. Unrelated working-tree work preserved? **YES**
C. Freshness tests pass? **YES** (2/2, explicitly isolated)
D. AllianceResearchServiceTests 58/58? **YES**
E. HiveOperations.Tests 181/181? **YES**
F. Unity AllianceResearchClientTests 8/8? **YES**
G. Unity compile green? **YES**
H. SQL migration required? **NO**
I. Commit created? **YES** — `ec94b9af`
J. Push main successful? **YES**
K. Push deploy successful? **YES**
L. Deployment workflow successful? **YES** — run `33934025840`
M. /health 200 Healthy? **YES**
N. Alliance Test preserved? **YES** — 2 members, InviteOnly, Stara still Chef
O. Production mutation performed? **NO**
P. Sceaux Royaux architecture touched? **NO**
Q. READY FOR M054? **YES**

M053B DEPLOYED — ALLIANCE RESEARCH BONUS FRESHNESS LIVE — NO PRODUCTION STATE MUTATED — READY FOR M054.
