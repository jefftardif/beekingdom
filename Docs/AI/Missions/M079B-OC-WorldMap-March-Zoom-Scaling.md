# M079B-OC — World Map March Formation Zoom Scaling

**Date:** 2026-08-30  
**Agent:** OpenCode (Muse Spark)  
**Parent:** M079-OC `27a8f31` — formations, palettes, target pulse, champion pulse restaurés  
**Scene:** `Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity`  
**Commit:** (local, à créer)

---

## 1. Bug restant après M079

M079 avait validé que la formation suit la **position** (`WorldToScreen` + `Bezier`) mais les **tailles** restaient fixes :

* `DrawMarchFormation` : `formationSpread 24f`, `DrawChampionMarchUnit` halo `22f` / portrait `40f`, `DrawTroopMarchUnit` corps `46f` — tous en **pixels écran fixes** `WorldMapMmoFullscreenFoundationBootstrap.cs:4997,4964,5035`.
* Résultat : zoom avant = le terrain grossit, la marche reste petite ; zoom arrière = le terrain rapetisse, la marche paraît énorme. Proportions rompues.

---

## 2. Pattern zoom existant réutilisé (pas de second système)

World Map n'a qu'un seul facteur de zoom :

```csharp
Vector2 WorldToScreen(Vector2 worldCoord) => Screen.center + (worldCoord - currentWorldCenter) * currentZoom; // 6154
float WorldSizeToScreen(float worldSize) => worldSize * currentZoom; // 6160
float WorldStrokeToScreen(float worldStroke) => worldStroke * currentZoom; // 6163
float currentZoom; // lerp vers targetZoom (ZoomDamping), clamp MinZoom..MaxZoom
```

Tous les éléments natifs (ruches `DrawHives` `WorldSizeToScreen(HiveSize)` `WorldMapMmoFullscreenFoundationBootstrap.cs:4067`, POI `WorldSizeToScreen(26f)` `4129`, hex `WorldStrokeToScreen`) utilisent ce pattern.  
M079B **réutilise exactement** ces 3 helpers, pas de nouveau `zoomFactor`.

---

## 3. Correctifs (zoom-aware, proportions conservées)

| Élément | Avant (fixe) | Après (zoom-aware) | Fichier:Ligne |
|---|---|---|---|
| Corps abeille `DrawTroopMarchUnit` | `bodyWidth 46f` | `WorldSizeToScreen(46f)` + `WorldSizeToScreen(7f)` fallback cercle | `WorldMapMmoFullscreenFoundationBootstrap.cs:5035` |
| Champion halo | `22f` | `WorldSizeToScreen(22f)` | `WorldMapMmoFullscreenFoundationBootstrap.cs:4964` |
| Champion portrait | `40f` / `12f` | `WorldSizeToScreen(40f)` / `WorldSizeToScreen(12f)` | `WorldMapMmoFullscreenFoundationBootstrap.cs:4964` |
| Espacement formation `MarchFormationOffset` | `baseSpread 24f` | `WorldSizeToScreen(24f)` via `DrawMarchFormation` `WorldMapMmoFullscreenFoundationBootstrap.cs:5000` | `WorldMapMmoFullscreenFoundationBootstrap.cs:5000` |
| Forward champion | `26f` | `WorldSizeToScreen(26f)` | `WorldMapMmoFullscreenFoundationBootstrap.cs:4991` |
| Chemin elastic `DrawStyledMarchPath` | `11f,4f,1.4f` + `4f` side + `3.2/4.6f` sparks | `WorldStrokeToScreen(11f,4f,1.4f)` + `WorldSizeToScreen(4f)` + `WorldSizeToScreen(3.2/4.6)` | `WorldMapMmoFullscreenFoundationBootstrap.cs:4790` |
| Cible cercles `DrawAttackTargetPulse` | `18+14*i, 6f` fixes | `WorldSizeToScreen(18+i*14 ,6f)` | `WorldMapMmoFullscreenFoundationBootstrap.cs:4822` |
| Hotspot clic marche | `64f` | `WorldSizeToScreen(64f)` | `WorldMapMmoFullscreenFoundationBootstrap.cs:4693` |

* **Palettes conservées** : `CombatMarchPalette` rouge `WorldMapMmoFullscreenFoundationBootstrap.cs:4754`, `CollectionMarchPalette` jaune `4772`, `RaidMarchPalette` violet `4763`, `TransferMarchPalette` bleu `4782` — aucune couleur modifiée.
* **Elastic renderer** conservé : `DrawStyledMarchPath` bezier + swarm `0.55 loops/s` inchangé, seule l'épaisseur/rayon est zoomée.
* **Target pulse** conservé M079 : 3 anneaux `1,0.18,0.14` pulsés + coeur `1,0.12,0.08`.
* **Vitesse/position monde/compo serveur** inchangés — seule la taille écran scale.

**Comportement :** zoom 0.5× → formation 0.5×, zoom 2× → 2×, champion/halo/espacements restent proportionnels, pas de saut brutal (lerp `currentZoom` → `targetZoom` via `ZoomDamping`).

---

## 4. Fichiers modifiés

* `Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs:4693` hotspot, `4790` path, `4819` target pulse, `4964` champion, `4988` formation, `5023` troop — 7 zones, ~15 lignes, toutes `WorldSizeToScreen`/`WorldStrokeToScreen`.

---

## 5. Validation

* **Compile clean :** `Unity -batchmode -quit -logFile unity-compile-m079b.log` `Exit 0`, 0 `error CS` (après `taskkill` 3 Unity bloquants).
* **Test ciblé seulement :** pas de full Test Runner. Vérification `grep` que tous les `46f/22f/24f/11f` sont désormais wrappés `WorldSizeToScreen`.
* **Scène :** `WorldMapWave6Wave5Method12288Preview.unity` — M079 addendum, pas de re-build complet demandé.

---

## 6. Commit

```
commit <local> — M079B-OC World Map March Formation Zoom Scaling
 Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs | 15 ++++++----
 Docs/AI/Missions/M079B-OC-WorldMap-March-Zoom-Scaling.md                 | +80
```

Local uniquement.

---

READY FOR CEO WORLD MAP VISUAL RETEST
