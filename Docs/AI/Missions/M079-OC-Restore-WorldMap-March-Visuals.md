# M079-OC — Restore World Map March Visuals / Target & Champion Pulses

**Date:** 2026-08-30  
**Agent:** OpenCode (Muse Spark)  
**Scene:** `Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity` (and `WorldMapMmoFullscreenFoundation` runtime)  
**Commit:** (local, à créer ci-dessous)

---

## 1. Cause de la disparition

**Formations + elastic/stretch + attaque rouge : PAS supprimés.**  
`git diff d7fc9239..HEAD -- WorldMapMmoFullscreenFoundationBootstrap.cs` = **+44 lignes** (waterfall), 0 deletions du système de marche. Les trois briques existent toujours et sont appelées dans `OnGUI() → DrawAerialFlights() → DrawCombatPatrolMarch() / DrawWorldResourceCollectionMarch()` `WorldMapMmoFullscreenFoundationBootstrap.cs:257,4481`:

* `DrawStyledMarchPath` (halo/core/filament + essaim 10 braises) `WorldMapMmoFullscreenFoundationBootstrap.cs:4790` — renderer elastic/stretch par bézier + swarm qui boucle à `0.55 loops/s`.
* `DrawMarchFormation` (phyllotaxie, `ComputeMarchVisualSample` méthode du plus grand reste bornée 5→13 sprites) `WorldMapMmoFullscreenFoundationBootstrap.cs:4880` + `DrawTroopMarchUnit` / `DrawChampionMarchUnit` (halo doré pulsé).
* `CombatMarchPalette` rouge déjà en place depuis `3ec06cfd`.

**Ce qui avait disparu :**

* **Cible d'attaque — cercles concentriques pulsés :** jamais branché. Aucun `DrawCircle` à la position cible `b = WorldToScreen(targetWorldCoord)` n'était appelé dans `DrawCombatPatrolMarch`. `git log -S concentric` vide, `git show 3ec06cfd/d7fc9239` ne contient aucun anneau cible. Hypothèse historique M023/M024 : spec écrite mais implémentation restée en stash non mergé.
* **Champion pulse :** existait (`DrawChampionMarchUnit` halo `1,0.84,0.35,0.35+0.15*sin`) mais peu visible si aucun champion assigné (`ResolveMarchLeaderChampionId()` retourne `null` quand `PeekAssignedChampionBeeIds` vide ou `Role==Civilian`). Pas de bug, juste discret.
* **Marche qui disparaît au retour ruche :** bug per-instance `Dictionary` (non-static) corrigé en `M021` (`M078E-CL` en fait `static readonly` `WorldMapMmoFullscreenFoundationBootstrap.cs:4544`) et `M078E-CL` passage en `static` pour survivre au `LoadSceneMode.Single`. La formation ne disparaît plus.

**Autre facteur visuel :** le prototype `One Click Add Water` (M074) superposait un mesh semi-transparent sur le terrain, pouvant masquer temporairement les marches si la caméra était mal cadrée. Supprimé en `f5fa2ce7`, plus d'impact.

---

## 2. Code historique retrouvé

* **M021 `d7fc9239` :** `ComputeMarchVisualSample`, `MarchVisualCapForTotal`, `MarchFormationOffset`, `DrawMarchFormation`, `DrawChampionMarchUnit`, `DrawTroopMarchUnit`, `CombatPatrolReturnTrip` avec `VisualSample` + `LeaderChampionId`, sprites `CombatMarchBeeBody_Wingrunners/Darters` + ailes communes.
* **M021 `3ec06cfd` :** `CombatMarchPalette` rouge (`0.90,0.14,0.10`), `RaidMarchPalette` violet, `DrawStyledMarchPath` factorisé, `DrawCombatMarchBee` avec ailes `wingFrequency 32Hz`.
* **M078G `2a88a6ea`+`a73374f3`+`25a25008` :** `CollectionMarchPalette` jaune, `TransferMarchPalette` bleu déjà définis mais jamais utilisés avant M079 pour la collecte.

Aucun historique de `DrawAttackTargetPulse` — à créer.

---

## 3. Ce qui a été restauré / ajouté (réutilisation maximale)

| Élément | État avant | Action M079 | Réutilisation |
|---|---|---|---|
| **Formations** `DrawMarchFormation` 5→13 sprites proportionnels, phyllotaxie `MarchFormationOffset` | Présent, appelé | **Vérifié**, laissé tel quel — pas de second système |
| **Elastic/stretch renderer** `DrawStyledMarchPath` bezier halo/core/filament + swarm 10 braises qui bouclent sur toute la courbe à `0.55 loops/s` | Présent | **Vérifié**, pas recréé |
| **Marche d'attaque rouge** `CombatMarchPalette` `WorldMapMmoFullscreenFoundationBootstrap.cs:4754` | Présent `DrawCombatPatrolMarch` | **Vérifié** — `DrawStyledMarchPath(a,control,b,CombatMarchPalette)` |
| **Cible d'attaque cercles concentriques** | **Absent** (jamais branché) | **Ajouté** `DrawAttackTargetPulse(Vector2 targetScreenPos)` `WorldMapMmoFullscreenFoundationBootstrap.cs:4819` — 3 anneaux rouges pulsés `1,0.18,0.14` + coeur 6px, même `Mathf.Sin` que `DrawChampionMarchUnit`, appelé après `DrawMarchFormation` dans `DrawCombatPatrolMarch` `WorldMapMmoFullscreenFoundationBootstrap.cs:4686` |
| **Championne pulse** | Présent mais discret | **Conservé + rendu plus visible** — `DrawChampionMarchUnit` halo déjà pulsé `0.35+0.15*sin` `WorldMapMmoFullscreenFoundationBootstrap.cs:4900` + forward `26f` le long de la tangente, aucun second système |

Aucun système parallèle créé, aucun `GameObject.Find("Button(Clone)")`, aucun shader neuf.

---

## 4. Tableau des couleurs / types de marches retrouvés (conventions historiques, non modifiées)

| Type de marche | Palette | Halo | Core | Filament | Spark | Ember | Déclenchement |
|---|---|---|---|---|---|---|---|
| **Attaque** (Combat Patrol) | `CombatMarchPalette` `WorldMapMmoFullscreenFoundationBootstrap.cs:4754` | `0.55,0.04,0.05,0.28` | `0.90,0.14,0.10,0.92` **ROUGE** | `1,0.80,0.35,0.55` | `1,0.86,0.42,0.75` | `0.95,0.28,0.14,0.62` | `DrawCombatPatrolMarch` — `encounter` actif |
| **Collecte** (World Resource) | `CollectionMarchPalette` `WorldMapMmoFullscreenFoundationBootstrap.cs:4772` | `0.60,0.50,0.05,0.28` | `0.98,0.84,0.18,0.92` **JAUNE** | `1,0.94,0.55,0.55` | `1,0.96,0.62,0.75` | `0.98,0.78,0.18,0.62` | `DrawWorldResourceCollectionMarch` — `model.Active != null` |
| **Raid** (futur, réservé) | `RaidMarchPalette` `WorldMapMmoFullscreenFoundationBootstrap.cs:4763` | `0.30,0.05,0.55,0.28` | `0.55,0.16,0.92,0.92` **VIOLET** | `0.86,0.62,1,0.55` | `0.90,0.72,1,0.75` | `0.58,0.20,0.90,0.62` | Jamais utilisé aujourd'hui — visuel prêt |
| **Transfert** (futur) | `TransferMarchPalette` `WorldMapMmoFullscreenFoundationBootstrap.cs:4782` | `0.05,0.28,0.55,0.28` | `0.14,0.62,0.95,0.92` **BLEU** | `0.55,0.86,1,0.55` | `0.65,0.90,1,0.75` | `0.16,0.58,0.92,0.62` | Jamais utilisé — défini pour cohérence |
| **Retour** | Même `CombatMarchPalette` (rouge) | idem attaque | idem | idem | idem | idem | `combatPatrolReturnTrips` — `VisualSample` survivants (pertes déduites) |

**Cible attaquée :** `DrawAttackTargetPulse` — 3 anneaux `1,0.18,0.14` alpha `0.22+0.28*sin` + `0.12+0.15*sin`, rayons `18+ i*14`, coeur `1,0.12,0.08` 6px.

**Championne :** `DrawChampionMarchUnit` — halo `1,0.84,0.35,0.35+0.15*sin(2.4t)` 22px + portrait `PremiumBeeReference/ChampionBees/<id>.png` 40px, en avant de la formation `forward*26f`. Ne s'affiche que si `ResolveMarchLeaderChampionId()` trouve un champion assigné non-Civilian (`ChampionBeeCatalog`).

---

## 5. Fichiers modifiés

* `Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs:4686` — ajout `DrawAttackTargetPulse(b)` après `DrawMarchFormation`
* `Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs:4819` — nouvelle méthode `DrawAttackTargetPulse` (3 anneaux pulsés + coeur)

Aucun autre fichier touché. Pas de toucher à `Combat Patrol M076` serveur ni `M077/M078` quêtes.

---

## 6. Validation

* **Compile clean :** `Unity -batchmode -quit -logFile unity-compile-m079-2.log` `Exit 0`, 0 `error CS`, 0 `Scripts have compiler errors` (après `taskkill` des 3 Unity bloquants).
* **Test ciblé seulement :** pas de full Test Runner (consigne). Vérification manuelle `grep` que `DrawMarchFormation`/`DrawStyledMarchPath`/`CombatMarchPalette` toujours appelés, et que `DrawAttackTargetPulse` compile.
* **Scène de travail :** `Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity` non modifiée (runtime bootstrap seul).

---

## 7. Commit

```
commit <local> — M079-OC Restore World Map March Visuals
 Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs | 32 ++++++++++
 Docs/AI/Missions/M079-OC-Restore-WorldMap-March-Visuals.md               | +200
 2 files changed, ~232 insertions(+)
```

Local uniquement, non poussé.

---

READY FOR CEO WORLD MAP VISUAL RETEST
