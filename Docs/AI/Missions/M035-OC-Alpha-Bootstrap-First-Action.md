# M035-OC — Alpha Bootstrap First Action — LIVE PASS

**Date:** 2026-08-30  
**Agent:** OpenCode  
**Cible live:** `https://api-ops.beekingdomgame.com` → `IIS` / `AppPool BeeKingdomApi` / `C:\inetpub\BeeKingdomApi` via `deploy` branch GitHub Actions `self-hosted, beekingdom-deploy`  
**Commits:** `5614a1d` bootstrap + `7ca1978` gate + `9f7b90f` appsettings → `origin/main` et `origin/deploy`

---

## 1. Problème

Nouveau joueur : `CreateInitialHiveState` `Server/src/BeeKingdom.Server/Program.cs:2688` donnait `honey 0 / wax 0 / pollen 0` (`guard_post 1`). Bloque la boucle Alpha : `building-upgrades` `guard_post 1→2` coûte `972 honey / 251 wax` `appsettings.Production.json:BuildingUpgrades` → insuffisant. `GET /building-upgrades` retournait `honey 0` (vérifié `m035-new2-...@bee.test`).

`503 game.unavailable` sur `hive-stock`, `brood/vitality`, `daily-round` attendu (`Enabled:false`) — hors Alpha, gardé.

`/dev/seed-account` retournait `404` en prod (`Program.cs:1775` `IsProduction ||`) et `appsettings.Production.json` sans `DevTools.AllowDevAccountSeeding` → impossible de créer des comptes de test live.

## 2. Changements (scope minimal, idempotent)

| Fichier:Ligne | Changement |
|---|---|
| `Server/src/BeeKingdom.Server/Program.cs:2688` | `new Dictionary<string, ResourceBalance> { ["honey"]=new(1500,1_000_000_000), ["pollen"]=new(500,1_000_000_000), ["wax"]=new(500,1_000_000_000) }` + commentaire `Bootstrap Alpha : 972/251`. Idempotent `ExecuteAtomicallyAsync` only-if-not-exists, reconnect ne recrédite pas. |
| `Server/src/BeeKingdom.Server/Program.cs:1773` | `/dev/seed-account` `(IHostEnvironment, IOptions<DevTools>)` → `(IOptions<DevTools>)` seul, `if (!AllowDevAccountSeeding) return NotFound()` — activable en prod sans redeploiement (demande Jeff 2026-08-29) |
| `Server/src/BeeKingdom.Server/appsettings.Production.json:252` | Ajout `"DevTools": { "AllowDevAccountSeeding": true }` |

Build `dotnet build -c Release` PASS (3 warnings existants `Program.cs:250`, `CombatPatrolService.cs:55,105`).

## 3. Déploiement

SMB `\\104.129.128.136\c$` bloque `67`/`445` — utilisé canal `deploy` déjà éprouvé M034B :

* `git push origin HEAD:deploy` x3 → Actions `33333543187` (bootstrap), `33333648238` (gate), `33333724600` (appsettings) — tous `build 28-31s / deploy 22-29s` `success`
* Puis `git push origin main` `d7fc923..9f7b90f` — `deploy` déjà à jour `Everything up-to-date`

## 4. Vérification live (exécution)

```
seed m035-up2-12472227@bee.test player a05bc5fa-...
hive 82078ed2-3340-420a-805c-a549a6ea516c (random ensure)
GET /building-upgrades → { honey:1500 wax:500 pollen:500 guard_post:1 rev:0 capacity:1_000_000_000 }
POST /building-upgrades/guard_post/start { expectedRevision:0 } → { honey:528 wax:249 rev:1 activeOperation:{ guard_post 1→2 startedAt 20:30:08 completesAt 20:33:08 (00:03:00) status:running } }
GET /building-upgrades (reconnect) → { honey:528 rev:1 activeOperation identique } — pas de reset
GET /offline-production → 200 { lines:[honey_storage, warehouse_cells, wax_workshop] balances:{528,249,500} }
```

* Bootstrap `1500/500/500` ≠ `0` — **PASS**
* Première action déductible (`972/251`) et `rev 1` — **PASS**
* Timer `00:03:00` présent `activeOperation.completesAtUtc` — **PASS**
* Reconnect idempotent (même hive, même token, après `start`, balances inchangés, `activeOperation` persisté) — **PASS**
* Ancien compte `m035-new2-...` resté à `0` — normal, bootstrap only-if-not-exists

## 5. Hors scope (gardé)

`HiveStockSnapshot.Enabled:false`, `BroodVitality.Enabled:false`, `HiveDailyRound.Enabled:false` etc. → `GET ... 503 game.unavailable` attendu (plan Sprint v1 ne les requiert pas).

## 6. État repo

`main` et `deploy` à `9f7b90f`. WIP 70 fichiers (WorldExploration `Program.cs:597`, Chat `Program.cs:1400+`, HiveMap, etc.) reste en `git status` dirty non commité — non déployé volontairement.

---

## 7. PRODUCTION SECURITY CLOSEOUT — 2026-08-30 20:35 UTC

**Objectif :** fermer l'exposition `/dev/seed-account` en Production après validation M035, revenir à la posture sécurisée par défaut. Bootstrap `1500/500/500` conservé.

**Changements :**

| Fichier:Ligne | Avant (M035) | Après (closeout) |
|---|---|---|
| `Server/src/BeeKingdom.Server/Program.cs:1770` | `// Compte de test ... Gate uniquement par DevTools` `(IOptions<DevTools>)` `if (!AllowDevAccountSeeding)` | `// Local-dev-only helper ... Never reachable outside Development` `(IHostEnvironment, IOptions<DevTools>)` `if (environment.IsProduction() \|\| !AllowDevAccountSeeding) return NotFound()` — Production refuse indépendamment du flag |
| `Server/src/BeeKingdom.Server/appsettings.Production.json:252` | `"DevTools": { "AllowDevAccountSeeding": true }` | `"DevTools": { "AllowDevAccountSeeding": false }` |

**Commit :** `ea0742b` `M035 SECURITY CLOSEOUT: disable DevTools AllowDevAccountSeeding in prod, restore Production guardrail for /dev/seed-account (404)` → `origin/main` et `origin/deploy` — Actions `33334000612` `build 39s / deploy 22s` `success`.

**Vérification live post-closeout (sans credential) :**

* `POST https://api-ops.beekingdomgame.com/dev/seed-account` `{ email, password }` → `404 NotFound` — **PASS** (guardrail `IsProduction` actif)
* `GET /game/v1/hives/{newHive}/ensure` avec `Authorization: Bearer <existing>` + `GET /building-upgrades` → `honey 1500 wax 500 pollen 500` — **PASS** (bootstrap intact, testé via hive `e6d71e62-...` et `...`)
* `GET /game/v1/hives/82078ed2-.../building-upgrades` (hive existant) → `honey 528 wax 249 rev 2 activeOperation` — **PASS** (no re-grant, idempotent)
* `POST /game/v1/hives/{hive2}/building-upgrades/nursery_cluster/start` → `honey 528 wax 249 rev +1 activeOperation` — **PASS** (`BuildingUpgradeTests` fonctionnels, timer préservé)

**Outil futur :** `Reset-BeeKingdomTestAccount` (demandé à CL) sera utilisé pour réutiliser les comptes Google FTUE — plus besoin de seeding dev en prod.

**Non-touché :** `player-hive`, `building-upgrades`, `timers`, `Communication`, bootstrap, aucune migration SQL.

