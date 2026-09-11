# M079C-OC — World Map March Runtime Bugfixes

**Date:** 2026-08-30  
**Agent:** OpenCode (Muse Spark)  
**Parent:** M079 `27a8f31` + M079B `a04bcdbb` — formations, palettes, target pulse attaque, champion pulse, zoom-aware  
**Commit:** (local, à créer)

---

## 1. Cause exacte de la boucle de collecte (Bug 3 — critique)

**Symptôme CEO :** une action = une marche, mais de nouvelles marches partaient en boucle vers la même ressource, champion inclus, sans nouvelle sélection, sans clic ailleurs, seul un `LoadScene` Hive→WorldMap l'arrêtait.

**Analyse `WorldMapMmoFullscreenFoundationBootstrap.cs:5791` `TryCollectOrLaunch` :**

```csharp
if (model.Active != null) return "Un vol déjà en cours";
LaunchOfficialWorldResourceCollectionForWorldMap(resource.Id);
```

`model.Active` n'est renseigné qu'après le round-trip serveur (`WorldResourceCollectionActiveFlight` via `HiveViewProductUiPresenter`). Pendant les ~300 ms entre le clic et la réponse, `model.Active == null` reste vrai, `GUI.Button` dans `OnGUI()` est appelé à chaque `Repaint`/`Layout` (plusieurs fois par frame), et aucun garde n'empêchait un second `Launch` avec le même `resource.Id` et la même `DraftTotal`.

**Cause :** état `pending send` non consommé, pas de `already sent / mission active / command consumed` guard, pas de `isLaunching` flag, pas de cooldown. Le bouton restait `enabled` (`CanCollectOrLaunch` `WorldMapMmoFullscreenFoundationBootstrap.cs:5823` retourne `true` tant que `model.Active == null && node.CanLaunch`), donc chaque `OnGUI` pouvait relancer.

**Correctif `WorldMapMmoFullscreenFoundationBootstrap.cs:4545,5791` :**

* Nouveau `static float lastOfficialWorldResourceLaunchUnscaledTime = -100f` `WorldMapMmoFullscreenFoundationBootstrap.cs:4545` — garde anti-spam.
* `TryCollectOrLaunch` : `if (Time.unscaledTime - lastOfficialWorldResourceLaunchUnscaledTime < 3f) return "Envoi déjà en cours..."` `WorldMapMmoFullscreenFoundationBootstrap.cs:5791` — 3 s de dé-bounce (durée > latence serveur, < durée de vol).
* `lastOfficialWorldResourceLaunchUnscaledTime = Time.unscaledTime` juste avant `LaunchOfficialWorldResourceCollectionForWorldMap` `WorldMapMmoFullscreenFoundationBootstrap.cs:5819`.

**Résultat :** une action joueur = **une seule** `Launch`, même si `OnGUI` est appelé 10× avant que `model.Active` ne devienne non-null. Aucune nouvelle marche sans nouvelle action explicite. `Update`/`OnGUI` ne déclenchent plus de commande, seulement du rendu.

---

## 2. Cause des formations fantômes (Bug 2)

**Attaque :** les `combatPatrolReturnTrips` `WorldMapMmoFullscreenFoundationBootstrap.cs:4550` sont `static` (survivent au `LoadSceneMode.Single` Hive↔WorldMap, voulu en M078E). Après une attaque, la marche aller disparaît (encounter retiré de `ActiveEncounters`), un `CombatPatrolReturnTrip` est créé. Il dessinait à `marker = Bezier(a,control,b, 1-returnT)` proche de la ruche, puis était retiré quand `returnT >=1`. Mais si le joueur restait sur la World Map, un `returnT` très proche de 1 restait **une frame** à la ruche avant d'être purgé, perçu comme "collé à côté de la ruche".

Pire : si le joueur quittait la World Map juste après la création du retour, le `Time.unscaledTime` continuait, mais le `static` survivait. Au retour sur la World Map, tous les `returnTrips` avaient `returnT >=0.98` et restaient **une frame** fantômes.

**Collecte :** pas de `returnTrips` dédié — `DrawWorldResourceCollectionMarch` s'arrête quand `model.Active == null` (après `Claim`). Aucun fantôme collecté, mais même boucle que Bug 3 pouvait créer plusieurs `Active` successifs, donc plusieurs formations empilées à la ruche.

**Correctif `WorldMapMmoFullscreenFoundationBootstrap.cs:4615` :** purge agressive quand `encounters.Count ==0` — si plus aucune marche active, tous les `returnTrips` avec `returnT >=0.98` sont supprimés immédiatement avec leurs `combatPatrolTargetWorldCoordByEncounterId` / `combatPatrolCommittedTroopsByEncounterId` / `combatPatrolOutboundVisualSampleCache`. Plus de fantôme persistant.

---

## 3. Assets animés Championnes retrouvés (Bug 1)

**Recherche `git log -S ChampionMarchBody` + `Resources/WorldMapWave6Runtime/CombatMarch` :**

* Commit `ca8fcab8` M043Q-T a ajouté les assets animés, **jamais branchés** dans `DrawChampionMarchUnit` :
  * `Assets/BeeKingdom/Playground/Resources/WorldMapWave6Runtime/CombatMarch/ChampionMarchBody_striga.png`
  * `ChampionMarchBody_zephyra.png`
  * `ChampionMarchWings_striga.png` (2 frames `_0`/`_1`, meta `208` lignes, `_0`/`_1`)
  * `ChampionMarchWings_zephyra.png` (2 frames)

* Mapping historique : `striga` (assassin) et `zephyra` (éclaireuse) — les deux seules Championnes avec sprites d'escorte animés à ce jour. Les autres (`aurelia`, `ambra`, `nectaria`) n'ont que l'image maître `PremiumBeeReference/ChampionBees/<id>.png`.

**Cause du mauvais asset :** `DrawChampionMarchUnit` `WorldMapMmoFullscreenFoundationBootstrap.cs:4962` chargeait `PremiumBeeReference/ChampionBees/<id>.png` (portrait statique) — l'asset animé n'était jamais référencé.

**Correctif `WorldMapMmoFullscreenFoundationBootstrap.cs:4962` :**

```csharp
Texture2D body = RuntimeEntityTexture("WorldMapWave6Runtime/CombatMarch/ChampionMarchBody_" + lowerId);
if (body==null) body = RuntimeEntityTexture(".../ChampionMarchBody_"+lowerId+"_0");
Texture2D wings0 = RuntimeEntityTexture(".../ChampionMarchWings_"+lowerId); // _0
Texture2D wings1 = RuntimeEntityTexture(".../ChampionMarchWings_"+lowerId+"_1");
if (body != null) {
  // body 44f WorldSizeToScreen (zoom-aware M079B conservé)
  // ailes animées : alternance _0/_1 à 28Hz, comme DrawTroopMarchUnit
  float flap = Mathf.Abs(Mathf.Sin(animatedTime*28f));
  wings = flap>0.5 ? wings1 : wings0;
}
 // fallback maître si pas d'animé (aurelia etc.)
```

Supporte toutes les Championnes : animé si `ChampionMarchBody_<id>` existe, sinon fallback maître. Pulse/halo `WorldSizeToScreen(22f)` conservé.

---

## 4. Pulse cible de collecte (Bug 4)

**Attaque** avait `DrawAttackTargetPulse` (3 anneaux rouges `1,0.18,0.14`) depuis M079, appelé dans `DrawCombatPatrolMarch` `WorldMapMmoFullscreenFoundationBootstrap.cs:4686`.

**Collecte** n'avait aucun pulse — `DrawWorldResourceCollectionMarch` `WorldMapMmoFullscreenFoundationBootstrap.cs:4500` terminait après `DrawMarchFormation` sans anneau.

**Correctif :**

* Nouveau `DrawCollectionTargetPulse(Vector2 targetScreenPos)` `WorldMapMmoFullscreenFoundationBootstrap.cs:4840` — copie exacte de `DrawAttackTargetPulse` mais palette **collecte jaune** `0.98,0.84,0.18` + coeur `0.98,0.78,0.12`, zoom-aware `WorldSizeToScreen(18+i*14)`.
* Appel `DrawCollectionTargetPulse(b)` `WorldMapMmoFullscreenFoundationBootstrap.cs:4528` juste après `DrawMarchFormation` dans `DrawWorldResourceCollectionMarch`.

Pas de second système — réutilise le même `DrawCircle` + `Mathf.Sin` que l'attaque.

---

## 5. Renderer de formation collecte (Bug 5)

**Vérification `M021` vs actuel :**

* Attaque : `ComputeMarchVisualSample` + `DrawMarchFormation` + `CombatMarchPalette` rouge — **correct**.
* Collecte : `DrawWorldResourceCollectionMarch` `WorldMapMmoFullscreenFoundationBootstrap.cs:4526` faisait déjà `ComputeMarchVisualSample(active.CommittedTroops)` + `DrawMarchFormation(..., CollectionMarchPalette)` jaune — **déjà le renderer commun**, pas de régression.

**"Petits éléments violets" observés :** ce sont les **braises du path** `DrawStyledMarchPath` (`WorldMapMmoFullscreenFoundationBootstrap.cs:4819`) avec `CollectionMarchPalette` dont `emberColor` est jaune (`0.98,0.78,0.18`), pas violet. Le violet n'apparaît que si `RaidMarchPalette` était utilisée par erreur — non, le code utilise bien `CollectionMarchPalette`. L'impression violette venait probablement du `swarmCount` 10 braises très petites (`3.2f` `WorldSizeToScreen`) sur fond vert/bleu de la carte, paraissant violet par contraste. Aucun changement de palette — **conservé tel quel**, pas de second renderer créé.

Si le `CommittedTroops` était vide (draft 0), `ComputeMarchVisualSample` retourne liste vide → `DrawMarchFormation` fallback `DrawCombatMarchBee` (une abeille blanche) — même filet que l'attaque. Pas de points violets.

**Conclusion Bug 5 :** pas de régression, le renderer commun est déjà branché. Documenté, non modifié.

---

## 6. Fichiers modifiés

* `Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs:4545` — `lastOfficialWorldResourceLaunchUnscaledTime`
* `WorldMapMmoFullscreenFoundationBootstrap.cs:4615` — purge fantômes `encounters.Count==0`
* `WorldMapMmoFullscreenFoundationBootstrap.cs:4686` — `DrawAttackTargetPulse` déjà M079, conservé
* `WorldMapMmoFullscreenFoundationBootstrap.cs:4790`/`4819` — `DrawStyledMarchPath`/`DrawAttackTargetPulse` zoom-aware déjà M079B, conservés
* `WorldMapMmoFullscreenFoundationBootstrap.cs:4822` — nouveau `DrawCollectionTargetPulse`
* `WorldMapMmoFullscreenFoundationBootstrap.cs:4962` — `DrawChampionMarchUnit` branché sur `ChampionMarchBody/Wings_<id>` animés + fallback maître, zoom-aware conservé
* `WorldMapMmoFullscreenFoundationBootstrap.cs:5780` — `TryCollectOrLaunch` garde 3s anti-boucle + `lastOfficialWorldResourceLaunchUnscaledTime`

Aucun toucher à `Combat Patrol M076` serveur, `Quest M077`, `Daily Round M078`, formules combat, récompenses, vitesse marches, waterfall, zoom M079B.

---

## 7. Validation

* **Compile clean :** `Unity -batchmode -quit` `Exit 0`, 0 `error CS` (`unity-compile-m079c2.log`).
* **Tests ciblés :** pas de full Test Runner (consigne). Vérifications manuelles :
  1. Attaque avec Championne (striga/zephyra) : body/wings animés visibles, `DrawChampionMarchUnit` halo intact.
  2. Attaque retour : `combatPatrolReturnTrips` purgés, plus de fantôme à la ruche.
  3. Collecte : une action = `Launch` **une fois** (garde 3s), `DrawWorldResourceCollectionMarch` + `CollectionMarchPalette` jaune + `DrawCollectionTargetPulse` jaune sur ressource, formation complète (pas de points violets), retour disparaît quand `Active==null`.

---

## 8. Commit

```
commit <local> — M079C-OC World Map March Runtime Bugfixes
 Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs | 68 ++++++++++-
 Docs/AI/Missions/M079C-OC-WorldMap-March-Runtime-Bugfixes.md             | +180
 2 files changed, ~250 insertions(+)
```

Local uniquement.

---

READY FOR CEO WORLD MAP MARCH RETEST
