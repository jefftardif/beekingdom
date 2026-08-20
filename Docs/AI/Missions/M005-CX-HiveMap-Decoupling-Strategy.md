# M005-CX HIVEMAP DECOUPLING STRATEGY

## Executive Conclusion

Oui, `LivingHive.unity` peut être retirée avant `HiveViewProductUiPresenter.cs`.

La scène legacy et le monolithe ne sont pas le même risque. `LivingHive.unity` est un point d’entrée, une composition de scène et un ancien runtime. `HiveViewProductUiPresenter.cs` est aujourd’hui aussi un conteneur de logique, d’état, de contrôleurs serveur, de modèles de présentation et de fenêtres IMGUI. HiveMap peut donc devenir l’expérience unique tout en gardant temporairement certains ponts `*ForExternalHost`, à condition de les traiter comme des adaptateurs provisoires et non comme une architecture cible.

Le bon chemin est un strangler incrémental par feature : isoler d’abord les flux dangereux où le monolithe possède encore de l’état joueur ou bloque l’input, puis remplacer progressivement les vues IMGUI par des vues HiveMap/uGUI connectées aux contrôleurs serveur existants.

## Current Dependency Model

Chaîne actuelle observée :

- HiveMap démarre via des bootstraps auto-attachés aux scènes `Environment2D5D*`.
- Plusieurs bootstraps HiveMap appellent directement `HiveViewProductUiPresenter` :
  - splash / entrée ruche ;
  - production manuelle ;
  - HUD ressources ;
  - file de construction ;
  - construction / prérequis / speed-up ;
  - caserne / entraînement ;
  - alliance ;
  - settings ;
  - input gating IMGUI.
- `LivingHiveMenuCanvas` est déjà une vue uGUI HiveMap séparée, dans son assembly propre, et passe par des bridges comme `LivingHiveSettingsBridge` parce que cet assembly ne peut pas dépendre directement du monolithe en `Assembly-CSharp`.
- `LivingHiveResearchWindow` est déjà découplée de la scène `LivingHive.unity`, mais elle reste une reproduction locale/proof de logique issue du monolithe.
- `MobileAccountSessionRuntimeBootstrap` instancie de nombreux clients/contrôleurs serveur puis les injecte dans `HiveViewProductUiPresenter` via `Configure*ForRuntime`.

La dépendance forte n’est donc plus principalement la scène LivingHive. Elle est dans le rôle excessif du monolithe comme façade statique globale.

## Scene Retirement vs Code Retirement

`LivingHive.unity = retrait de scène` signifie :

- ne plus charger la scène legacy en parcours joueur ;
- ne plus dépendre de ses objets, caméras, hotspots ni de `LivingHiveDemoBootstrap` ;
- utiliser HiveMap comme expérience canonique.

`HiveViewProductUiPresenter.cs = retrait de code` signifie :

- plus aucun runtime player-facing ne référence `HiveViewProductUiPresenter` ;
- les `*ForExternalHost` ont été remplacés ou supprimés ;
- les contrôleurs serveur, modèles et vues ont des propriétaires séparés ;
- le projet compile et tourne sans le fichier monolithique.

Ces jalons sont indépendants. Le premier peut arriver bien avant le second.

## Safe Temporary Dependencies

Peuvent rester après dépréciation de `LivingHive.unity`, si explicitement marqués temporaires :

- `SettingsOverlay*ForExternalHost`, via `LivingHiveSettingsBridge`.
- `DrawQueueSidebarForExternalHost`, tant que la sidebar est non bloquante et correctement masquée.
- `GetResourceTotalsForExternalHost`, comme source de snapshot HUD provisoire.
- `AllianceOverlay*ForExternalHost`, si alliance n’est pas la prochaine surface produit active.
- certains overlays IMGUI, tant qu’ils sont derrière un input gate fiable.
- les méthodes `Configure*ControllerForRuntime`, uniquement comme point d’injection transitoire des contrôleurs serveur.

La règle : acceptable si cela ne requiert aucun objet de `LivingHive.unity`, n’écrit pas par-dessus une implémentation HiveMap plus récente et reste encapsulé derrière un bootstrap ou un bridge identifiable.

## Dangerous Dependencies

À traiter tôt :

- Production manuelle : `TickManualProductionForExternalHost`, `CollectManualProductionForExternalHost`, feedback visuel et mutation de ressources sont trop mélangés.
- Caserne : entraînement, claim tap, badge, animation, highlight et overlay passent encore massivement par le monolithe.
- Construction : préselection bâtiment, prérequis, speed-up, tick et rendu overlay sont encore concentrés dans le monolithe.
- Splash / tutorial gate : `Draw()` et `SkipGuidedTutorialForExternalHost` gardent une odeur de runtime LivingHive.
- `HiveMapOverlayInputGateBootstrap` : utile, mais fragile parce qu’il dépend d’une liste manuelle d’overlays IMGUI ouverts.
- Tout calcul de rect écran ou hotspot hérité qui simule LivingHive au lieu de parler en coordonnées HiveMap.

## Recommended Strangler Architecture

Architecture cible incrémentale :

- `Gameplay/Application State` : état durable, ressources, files, recherches, entraînement, construction.
- `Server/Client Controllers` : clients et panel controllers existants, instanciés hors du monolithe.
- `Presentation Models` : modèles purs consommables par IMGUI legacy ou uGUI HiveMap.
- `Feature Controllers` : petits orchestrateurs par feature, par exemple production, construction, barrack, settings.
- `Legacy IMGUI Presentation` : conservée temporairement comme vue branchée sur les mêmes modèles.
- `HiveMap uGUI Presentation` : nouvelle cible player-facing.

Le pivot technique recommandé est un host HiveMap par feature, plus un registre central d’overlays/input, plutôt qu’une extraction globale du monolithe.

## Extraction Priorities

NOW

- Créer mentalement puis techniquement une frontière `HiveMapFeatureHost` / overlay registry.
- Remplacer les dépendances d’input gating par un registre d’overlays ouvert/fermé.
- Stabiliser les interfaces de snapshot : ressources, construction active, entraînement actif.
- Préparer l’accès direct aux contrôleurs serveur déjà créés par `MobileAccountSessionRuntimeBootstrap`.

DURING MIGRATION

- Production manuelle vers contrôleur de feature + feedback HiveMap.
- Caserne vers controller réutilisable + badge/overlay HiveMap.
- Construction/prérequis/speed-up vers modèle de présentation + view HiveMap.
- HUD ressources vers provider dédié au lieu de `GetResourceTotalsForExternalHost`.
- Settings et chat vers services/overlays natifs si l’UX HiveMap en dépend.

LATER

- Alliance overlay.
- Queue sidebar.
- Combat/world-map overlays encore appelés depuis le monolithe.
- Nettoyage des helpers visuels IMGUI.
- Suppression des méthodes `ForProof` historiques qui ne bloquent aucun parcours joueur.

DO NOT EXTRACT WITHOUT PRODUCT NEED

- Styles, couleurs, petits helpers visuels IMGUI isolés.
- Fenêtres IMGUI qui ne sont pas dans le chemin critique joueur.
- Logique de proof/harness liée à l’ancienne image de référence.
- Géométrie hotspot LivingHive.
- Toute abstraction “propre” qui ne réduit ni risque joueur, ni duplication active, ni dépendance bloquante.

## LivingHive Scene Retirement Milestone

Déclarer `LivingHive.unity = DEPRECATED` quand :

- le parcours joueur charge uniquement HiveMap / `Environment2D5D_HiveMap_Test` ou son successeur production ;
- aucune navigation production ne charge `LivingHive.unity` ;
- `LivingHiveDemoBootstrap` n’est plus requis pour splash, entrée ruche, ressources, recherche, construction, caserne, settings, alliance/chat minimum ;
- HiveMap possède les entrées bâtiment nécessaires et ne dépend pas d’objets de scène LivingHive ;
- les overlays hérités encore utilisés sont appelés seulement comme code adapters ;
- l’input uGUI + bâtiments est validé sans click-through ;
- la scène LivingHive reste uniquement référence QA/editor, exclue du chemin joueur.

## Monolith Retirement Milestone

Déclarer `HiveViewProductUiPresenter.cs = RETIRED` quand :

- aucun runtime HiveMap, WorldMap ou menu ne référence `HiveViewProductUiPresenter`;
- tous les `*ForExternalHost` sont remplacés, supprimés ou confinés à des tests legacy exclus du player ;
- les contrôleurs serveur sont configurés hors monolithe ;
- les vues IMGUI player-facing ont une alternative HiveMap/uGUI ou sont abandonnées ;
- le registre overlay/input ne dépend plus du monolithe ;
- le projet compile, démarre HiveMap et passe les parcours critiques sans ce fichier.

## Risks

- Confondre retrait de scène et retrait de code, puis lancer une réécriture trop large.
- Extraire une vue IMGUI au lieu d’extraire le modèle ou le contrôleur utile.
- Perdre des comportements serveur déjà fonctionnels en recodant depuis LivingHive.
- Laisser LivingHive écraser une implémentation HiveMap plus récente.
- Accumuler des bridges `ForExternalHost` sans propriétaire ni date de sortie.
- Casser l’input mobile par cohabitation IMGUI/uGUI.

## Recommended Implementation Sequence

1. Figer officiellement HiveMap comme autorité pour toute feature déjà portée.
2. Inventorier les `ForExternalHost` encore appelés par HiveMap et leur donner un statut : temporary-safe, replace-early, delete-later.
3. Introduire un registre overlay/input central pour retirer la liste manuelle d’overlays.
4. Faire sortir les snapshots simples : ressources, entraînement, construction.
5. Rebrancher HiveMap directement sur les contrôleurs serveur existants quand ils existent.
6. Remplacer production, caserne, construction dans cet ordre.
7. Déclarer `LivingHive.unity` deprecated dès que le parcours joueur HiveMap est complet.
8. Garder le monolithe comme legacy adapter, puis le vider feature par feature.
9. Supprimer le monolithe seulement quand il n’est plus dans la chaîne runtime.

## Confidence

MEDIUM

La séparation scène/code est claire et solide. La confiance reste medium plutôt que high parce que certains fichiers très volumineux ont été inspectés par recherche ciblée, pas relus intégralement ligne par ligne, et M004 couvre encore la provenance fine.
