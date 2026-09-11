# M080-OC — Rebuild WorldMap Resource Collection Loop

**Date:** 2026-09-11  
**Agent:** OC (Muse Spark) via MCP Unity  
**Commits:** `eed9b3e`, `8e3c5bd`, `fd12a74`, `fe9c8ea` (local, no push)  
**Parent:** M079E `4f75948` — missing bootstrap auto-initialization  
**Scene:** `Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity` (World Map, NOT HiveMap)

---

## ARCHITECTURE RETENUE

### Client-Side Visual State Machine

Le client gère la représentation visuelle du vol de collecte. Le serveur reste autoritaire sur le timing et les récompenses.

```
IDLE → COMPOSITION → OUTBOUND → COLLECTING → RETURNING → COMPLETED → IDLE
```

| État client | Signification | Serveur |
|---|---|---|
| **IDLE** | Aucun vol, joueur peut sélectionner une ressource | `Active == null` |
| **COMPOSITION** | Joueur choisit les troupes (panneau UI) | `Active == null` |
| **OUTBOUND** | Abeille vole vers la ressource (animation visuelle, 3s) | `Active != null` |
| **COLLECTING** | Abeille sur la ressource (10s pour Alpha) | `Active != null` |
| **RETURNING** | Abeille revient à la ruche (animation visuelle, 3s) | `Claim` appelé → `Active == null` |
| **COMPLETED** | Tout disparaît → retour à IDLE | `Active == null` |

### Machine d'états réelle

```csharp
private enum CollectionVisualState
{
    Idle = 0,
    Composition = 1,
    Outbound = 2,
    Collecting = 3,
    Returning = 4,
    Completed = 5
}
```

### Transitions autorisées

```
IDLE → COMPOSITION (joueur clique sur ressource)
COMPOSITION → OUTBOUND (joueur confirme l'envoi)
OUTBOUND → COLLECTING (timer 3s écoulé)
COLLECTING → RETURNING (timer 10s écoulé + Claim serveur)
RETURNING → COMPLETED → IDLE (timer 3s écoulé)
```

### Transitions interdites

```
COLLECTING → OUTBOUND
RETURNING → OUTBOUND
COMPLETED → OUTBOUND
```

Sans nouvelle action utilisateur.

---

## COMPOSANTS COMBAT RÉUTILISÉS

| Compat combat | Utilisation collecte |
|---|---|
| `DrawMarchFormation` | Formation d'abeilles sur la carte |
| `DrawStyledMarchPath` | Ligne de marche (courbe de Bézier) |
| `ComputeMarchVisualSample` | Échantillon visuel des troupes |
| `MarchVisibleOnScreen` | Culling zoom (inclut le marker) |
| `WorldSizeToScreen` | Proportions zoom-aware |
| `CombatFamilyOrder` | Ordre des familles (guardians, wingrunners, darters) |
| `CollectionMarchPalette` | Palette jaune (déjà existante) |
| `DrawCollectionTargetPulse` | Pulse concentrique jaune sur la ressource |

---

## CHANGEMENTS CLIENT

### Fichiers modifiés

1. **`Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs`**
   - Ajouté `CollectionVisualState` enum (6 états)
   - Ajouté champs: `collectionVisualState`, `collectionVisualTimer`, `collectionVisualTargetNodeId`, `collectionVisualSample`, `collectionVisualChampionId`, `showCompositionPanel`, `compositionTargetNodeId`
   - Ajouté constantes: `CollectionOutboundDuration = 3f`, `CollectionReturningDuration = 3f`, `CollectionDuration = 10f`
   - Remplacé `DrawWorldResourceCollectionMarch` par state machine visuelle
   - Ajouté `DrawOutboundMarch` — animation aller
   - Ajouté `DrawCollectingFormation` — formation sur ressource + pulse
   - Ajouté `DrawReturningMarch` — animation retour
   - Ajouté `DrawCompositionPanel` — panneau choix troupes
   - Ajouté `DrawCollectionDraftRow` — ligne de sélection famille
   - Ajouté `ConfirmCollectionLaunch` — lancement après confirmation
   - Ajouté `ComputeMarchVisualSample(int, int, int)` — overload pour brouillon
   - Remplacé `TryCollectOrLaunch` — ouvre panneau composition au lieu de lancer directement
   - Remplacé `CanCollectOrLaunch` — intègre nouveaux états visuels
   - Remplacé `CollectionActionLabel` — labels adaptatifs selon état

2. **`Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`**
   - Ajouté `AdjustOfficialWorldResourceCollectionDraftForWorldMap(family, delta)` — pont vers controller
   - Supprimé `ApplyDefaultWorldResourceCollectionEscort()` du `LaunchOfficialWorldResourceCollectionForWorldMap` — le joueur choisit désormais les troupes

---

## CHANGEMENTS SERVEUR

**Aucun.** Le serveur `WorldResourceCollectionService` est resté autoritaire :

- `Launch` crée le vol avec `StartedAtUtc` et `EndsAtUtc`
- `Claim` vérifie `now >= EndsAtUtc`, crédite les ressources, met le cooldown
- `Recall` annule sans récompense

Le client interprète la timing serveur :
- `OUTBOUND` : les 3 premières secondes après le lancement
- `COLLECTING` : de 3s jusqu'à `EndsAtUtc`
- `RETURNING` : après `Claim`, 3 secondes de retour visuel

---

## HACKS M079 SUPPRIMÉS

| Hack M079 | Raison suppression |
|---|---|
| `WorldResourceCollectionReturnTrip` struct | Remplacé par état `Returning` dans la state machine |
| `collectionReturnTrip` static | Plus nécessaire — le retour est géré par `DrawReturningMarch` |
| `lastKnownCollectionFlightId/NodeId/Sample/ChampionId` statics | Plus nécessaire — l'état est dans `collectionVisualState` |
| `lastSeenCollectionActiveUnscaledTime` static | Garde 4s supprimée — remplacée par la state machine |
| Garde 4 secondes (`Time.unscaledTime - lastSeenCollectionActiveUnscaledTime > 4f`) | Plus nécessaire — le retour est déclenché explicitement par `Claim` |
| `ApplyDefaultWorldResourceCollectionEscort` | Le joueur choisit les troupes via le panneau composition |
| `collectionReturnTrip` purge dans `DrawWorldResourceCollectionMarch` | Plus nécessaire — un seul état actif à la fois |

---

## COMPOSANTS M079 CONSERVÉS

| Composant | Statut |
|---|---|
| `CollectionMarchPalette` | ✅ Conservé — palette jaune |
| `DrawMarchFormation` | ✅ Conservé — formation d'abeilles |
| `DrawCollectionTargetPulse` | ✅ Conservé — pulse concentrique jaune |
| `WorldSizeToScreen` | ✅ Conservé — zoom-aware scaling |
| `MarchVisibleOnScreen` | ✅ Conservé — culling avec marker |
| `DrawStyledMarchPath` | ✅ Conservé — courbe de Bézier |
| `ComputeMarchVisualSample` | ✅ Conservé — échantillon visuel |

---

## CONSTANTES

```csharp
private const float CollectionOutboundDuration = 3f;  // Animation aller (hive → ressource)
private const float CollectionReturningDuration = 3f;  // Animation retour (resource → hive)
private const float CollectionDuration = 10f;          // Alpha: temps sur la ressource avant claim
```

**Note:** `CollectionDuration = 10f` est la valeur Alpha. Elle sera remplacée plus tard par une durée dépendant de la ressource, des troupes, et des technologies.

---

## ROOT CAUSE DU BLOCAGE INITIAL

Le `MobileAccountSessionRuntimeBootstrap.IsEnvironmentScene()` ne matchait que les scènes `Environment2D5D*`. La scène `WorldMapWave6Wave5Method12288Preview` n'était PAS reconnue → `TryConfigureGameplayForActiveSession()` n'était jamais appelée → `worldResourceCollectionController` restait `null` → toute la collecte morte.

De même, `WorldMapMmoFullscreenFoundationBootstrap.IsEnvironmentScene()` avait le même problème pour son `AutoStart`.

### FIX
- `MobileAccountSessionRuntimeBootstrap.cs` : `IsEnvironmentScene` matche maintenant `WorldMap*`
- `WorldMapMmoFullscreenFoundationBootstrap.cs` : `IsEnvironmentScene` matche maintenant `WorldMap*`
- `HiveViewProductUiPresenter.cs` : suppression de `ApplyDefaultWorldResourceCollectionEscort` (dead code M079C remplacé par le panneau de composition M080)

---

## RÉSULTAT COMPILATION

```
Unity Editor: 6000.5.3f1
Compilation: 0 erreur
Warnings: 20 (toutes pré-existantes, dépréciations Unity 6)
Console errors: 0
```

---

## SCÉNARIO DE TEST (pour CEO)

1. **Ouvrir scene** `WorldMapWave6Wave5Method12288Preview` (World Map)
2. **Entrer en Play Mode**
3. **Cliquer sur une ressource** (pollen core) → panneau composition s'ouvre
4. **Choisir les troupes** (Gardiennes, Voltigeuses, Lanceuses) avec boutons +/-
5. **Cliquer "Envoyer les abeilles"** → UNE marche jaune part
6. **Vérifier** : formation correspond aux troupes choisies
7. **Vérifier** : Championne visible si applicable
8. **Arrivée ressource** : ligne disparaît, formation reste sur ressource
9. **Pendant 10 sec** : aucune autre marche ne part
10. **Après 10 sec** : retour jaune automatique
11. **Même formation + Championne reviennent**
12. **Arrivée ruche** : tout disparaît
13. **Attendre 15 sec** : aucune marche ne repart seule
14. **Tester une DEUXIÈME collecte** pour confirmer retour à IDLE

---

## COMMITS

```
commit eed9b3e — M080-OC Rebuild WorldMap Resource Collection Loop
 Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs | +289 -104
 Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs              | +6 -2

commit 8e3c5bd — M080 HOTFIX: sync visual state with server on scene reload
 Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs | +46

commit fd12a74 — M080 HOTFIX: IsEnvironmentScene matches WorldMap* scenes
 Assets/BeeKingdom/Playground/MobileAccountSessionRuntimeBootstrap.cs    | +3

commit fe9c8ea — M080 CLEANUP: fix bootstrap IsEnvironmentScene, remove dead M079C hack
 Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs | +4 -1
 Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs              | -19 (dead code)
```

---

**READY FOR CEO M080 RESOURCE COLLECTION RETEST**
