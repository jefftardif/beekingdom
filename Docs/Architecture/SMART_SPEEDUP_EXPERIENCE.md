# Smart SpeedUp Experience

## Philosophie UX

Le joueur ouvre les accélérations depuis l'action qu'il veut terminer. Le contexte porte la catégorie, l'identifiant de la cible et le temps restant; la fenêtre ne connaît pas la construction, la recherche ou l'entraînement.

## Flux

```text
Timer / file d'attente
  -> SpeedUpDialogContext
  -> SpeedUpDialog
  -> filtrage spécialisé + universel
  -> prévisualisation du plan
  -> Utiliser automatiquement
  -> consommation atomique
  -> temps restant borné à zéro
  -> complétion locale et feedback
```

`SpeedUpDialog` est le point d'entrée unique. La file d'attente et les panneaux de détail Construction/Entraînement ouvrent ce même contexte; `OpenSpeedUpDialogForProof` existe pour les harnesses. Les intégrations gameplay peuvent appeler la même API avec leur contexte.

## Filtrage

Un contexte `Research`, par exemple, utilise les stacks `Research` et `Universal`. Les stacks Construction, Training, Healing et Manufacturing ne sont jamais ajoutées à la liste présentée. Le contexte global du menu conserve les six catégories pour la gestion de l'inventaire.

## AutoUse

`SmartSpeedUpCalculator` réutilise `SpeedUpInventory` et remplace le calcul glouton historique. Il convertit les durées en minutes, utilise une sélection bornée par groupes binaires, choisit la plus petite durée totale couvrant la cible et expose `WasteSeconds` et `RemainingAfterSeconds`.

La consommation est validée pour toutes les entrées avant suppression. Une accélération supérieure au temps restant produit toujours `RemainingAfterSeconds = 0` et ne produit jamais de temps négatif.

Lorsqu'un timer local atteint zéro, les méthodes de complétion existantes sont appelées et publient `RewardGranted` sur le Game Event Bus. Le Reward Pipeline peut ainsi consommer le signal sans dépendre de la fenêtre SpeedUp.

## Motion et feedback

La fenêtre réutilise `UIAnimationLibrary` et `UIFeedbackSystem` existants. La complétion locale passe par les méthodes de fin de timer existantes; aucune animation SpeedUp parallèle n'est créée.

## Futures intégrations

Les mêmes contextes pourront être ouverts depuis Construction, Recherche, Entraînement, Fabrication, Guérison, Quêtes, événements, gemmes et boutique. Le calcul et la consommation restent communs.
