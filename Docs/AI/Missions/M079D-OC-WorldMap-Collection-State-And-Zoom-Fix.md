# M079D-OC — World Map Collection State And Zoom Fix

**Date :** 2026-09-11
**Agent :** OC (Muse Spark, via MCP Unity)
**Parent :** M079 `27a8f31` + M079B `a04bcdbb` + M079C `e947e95`
**Scène :** `Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity` (runtime `WorldMapMmoFullscreenFoundationBootstrap.cs`)
**Règle suivie :** calquer la collecte sur la marche d'attaque (elle part et revient).

---

## 1. Cause racine de la boucle (collecte)

**Constat de code (prouvé, pas supposé) :**

1. **Aucun état "commande consommée / mission active" côté WorldMap.** Entre le clic "Envoyer" et la réponse serveur, `model.Active` reste `null` et `CanCollectOrLaunch` (`WorldMapMmoFullscreenFoundationBootstrap.cs:5860`) ne consultait ni `controller.IsBusy` ni `State == Mutating` : le bouton restait armé avec les drafts intacts. Le garde M079C (`lastOfficialWorldResourceLaunchUnscaledTime`, délai wall-clock 3 s) était aveugle : il bloquait aussi les actions légitimes après 3 s et ne prouvait rien sur l'état réel.
2. **Aucun retour visuel.** `DrawWorldResourceCollectionMarch` ne dessinait que l'aller (`travelT` saturé à 1, escouade parquée au nœud). Au Claim/Recall, `model.Active` passe à `null` → la formation s'évaporait sur place. L'attaque, elle, spawne un `CombatPatrolReturnTrip` (aller + retour + purge). D'où "elle ne revient jamais".
3. **La boucle perçue** = combinaison de (1) bouton réarmé en permanence + (2) vols qui ne reviennent jamais visuellement + `UpdateOfficialWorldResourceCollectionPolling` qui ne rafraîchit que si `Active != null` (si le Launch a réussi côté serveur mais que le client n'a jamais vu `Active`, chaque nouveau clic est une nouvelle tentative).

**Correctif (cause réelle, pas de temporisation) :**
- Suppression du garde 3 s M079C (`lastOfficialWorldResourceLaunchUnscaledTime` + champ + écriture).
- `CanCollectOrLaunch` officiel exige désormais `!IsBusy && State != Mutating` (`WorldMapMmoFullscreenFoundationBootstrap.cs:5943`).
- `TryCollectOrLaunch` refuse en amont si busy/Mutating avec statut "Envoi déjà en cours..." (`WorldMapMmoFullscreenFoundationBootstrap.cs:5910`).
- Nouveau `IsOfficialWorldResourceCollectionBusyForWorldMap()` dans `HiveViewProductUiPresenter.cs:38804` (remonte `controller.IsBusy`, même pattern que les autres ponts).
- Le suivi `lastKnownCollectionFlightId/NodeId/Sample/ChampionId` (`WorldMapMmoFullscreenFoundationBootstrap.cs:4520`) constitue la preuve "mission active" et alimente le retour.

Résultat : **une action joueur = une seule marche**. Le bouton se désarme pendant la mutation, se réarme sur `Active` (aller), puis le retour prend le relais visuellement jusqu'à purge. Aucune relance sans nouvelle action explicite.

## 2. Cause racine du problème de zoom

**Culling sur les extrémités seules.** Les 4 tracés de marche utilisaient `if (!IsOnScreen(a, 420f) && !IsOnScreen(b, 420f)) return;` (collecte `4510`, attaque aller `4685`, attaque retour `4731`, vol local `5155`). En zoom avant (`MaxZoom = 2.65`), la ruche (a) et la cible (b) sortent de l'écran alors que le marqueur (entre les deux) reste visible → **toute la marche est cullée** : abeille + ligne jaune + pulse. Les tailles, elles, étaient déjà correctes (M079B : `WorldSizeToScreen` linéaire, même facteur que le terrain).

**Correctif :** nouveau helper `MarchVisibleOnScreen(a, b, marker)` (`WorldMapMmoFullscreenFoundationBootstrap.cs:6458`) — `IsOnScreen(a,420) || IsOnScreen(b,420) || IsOnScreen(marker,220)`. Branché sur collecte aller/retour, attaque aller/retour (même cause latente). Tailles inchangées (proportionnelles au terrain sur toute la plage 0.30–2.65, pas de clamp qui briserait la proportionnalité ; `WorldStrokeToScreen` garde son plancher 0.75 px).

## 3. Retour collecte (miroir attaque)

Nouveau `WorldResourceCollectionReturnTrip` (`WorldMapMmoFullscreenFoundationBootstrap.cs:4503`) + slot statique unique (un seul vol officiel possible côté serveur) :
- suivi du vol actif à chaque frame (FlightId/NodeId/sample/champion + horodatage),
- à la disparition d'`Active` (claim/recall) : spawn du retour nœud → ruche **après 4 s de confirmation** (> polling 3 s, évite de confondre un Refresh en retard — ex. retour de scène, modèle `Loading` — avec une vraie fin de vol),
- animation `1f - returnT` avec `CollectionMarchPalette` (jaune conservé) + même formation + champion,
- purge à `returnT >= 1f`, suppression propre, aucun fantôme,
- si un aller réapparaît (course refresh), le retour est annulé.

Vitesse/durée réutilisées de l'attaque (`CombatPatrolReturnTripWorldUnitsPerSecond = 90`, min 1.5 s / max 10 s). Palettes M079/M079C intactes (attaque rouge, collecte jaune, raid violet, transfert bleu).

## 4. Fichiers modifiés

- `Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs` : return-trip collecte, garde busy/Mutating, suppression garde 3 s, `MarchVisibleOnScreen` ×4 tracés.
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs:38804` : accesseur `IsOfficialWorldResourceCollectionBusyForWorldMap()`.
- `Docs/AI/Missions/M079D-OC-WorldMap-Collection-State-And-Zoom-Fix.md` : présent rapport.

Non touché : serveur M076, quêtes M077/M078, LivingHive, waterfall, formules, récompenses, vitesses réelles.

## 5. Validation

- **Compile :** refresh + recompilation via instance Unity MCP (`6000.5.3f1`), `ready_for_tools:true`, **0 erreur** console ; seuls warnings pré-existants CS0618 (compat Wave5). Batchmode CLI impossible (instance projet déjà ouverte — même verrou que M079C).
- **Test ciblé (exécuté dans l'éditeur réel via MCP `execute_code`, reflection sur `MarchVisibleOnScreen`) :** `zoom-marker-visible=True` (a/b hors écran, marqueur visible → dessiné ; avant fix → cullé), `all-offscreen=False` (pas de dessin inutile), `endpoint-visible=True` (nominal conservé). Écran 1920×1080.
- **Pas de full Test Runner** (consigne). Retest visuel CEO requis (aller + retour + zoom).

## 6. Commit

Local, PAS de push (consigne).

---

READY FOR CEO RETEST
