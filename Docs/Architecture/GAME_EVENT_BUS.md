# BeeKingdom Game Event Bus

## Philosophie

Le Game Event Bus est le canal de communication découplé des systèmes gameplay. Un producteur publie un événement fortement typé sans connaître ses consommateurs. Chaque consommateur s'abonne indépendamment et peut être retiré sans modifier le producteur.

Le bus n'est pas un système métier et ne contient aucune règle Construction, Recherche, Reward ou UI.

## Architecture

Namespace : `BeeKingdom.Gameplay.Events`.

- `IGameEvent` : marqueur des événements fortement typés.
- `GameEventBus` : publication et abonnement par type générique.
- `GameEventSubscription` : désabonnement idempotent et sûr via `IDisposable`.
- `GameEventContext` : séquence, date UTC et source de publication.
- `OfficialGameEvents` : événements de référence du framework.

La publication utilise un canal générique déjà créé à l'abonnement. Aucune réflexion n'est exécutée pendant `Publish`; le dictionnaire de canaux est résolu par type et les handlers sont parcourus dans l'ordre d'enregistrement.

## Cycle de vie

```text
Producteur
  -> GameEventBus.Publish<TEvent>(eventData)
  -> GameEventContext(sequence, timestamp, source)
  -> handlers typés dans l'ordre d'abonnement
  -> désabonnement ou abonnement ponctuel
```

Une subscription peut être conservée dans un composant et disposée dans sa phase de destruction. `SubscribeOnce` retire le handler avant son invocation suivante. Les mutations pendant une publication sont compactées après le dispatch.

## Événements de référence

- `BuildingStarted`
- `BuildingCompleted`
- `ResearchStarted`
- `ResearchCompleted`
- `SpeedUpUsed`
- `RewardGranted`

La présentation Construction publie déjà `BuildingCompleted` après une complétion serveur acceptée. La migration des autres producteurs et consommateurs sera progressive.

## Exemple

```csharp
GameEventSubscription subscription = GameEventBus.Shared.Subscribe<BuildingCompleted>(
    (eventData, context) =>
    {
        // Réagir sans référencer le système Construction.
    });

GameEventBus.Shared.Publish(
    new BuildingCompleted("honey_storage", operationId),
    "construction");

subscription.Dispose();
```

## Bonnes pratiques

- Utiliser un `readonly struct` pour les événements simples.
- Garder les payloads compacts et stables.
- Publier une seule fois après la mutation autoritaire acceptée.
- Disposer les subscriptions avec le cycle de vie du consommateur.
- Garder les handlers courts et déléguer les opérations lourdes.
- Tester l'ordre et les effets de désabonnement.

## Erreurs à éviter

- Ne pas appeler directement un autre système depuis un producteur.
- Ne pas ajouter un switch central par type d'événement.
- Ne pas publier à chaque frame pour représenter un état continu.
- Ne pas conserver une subscription au-delà de la durée de vie de son propriétaire.
- Ne pas mettre de dépendance BeeQA, UI ou Reward dans le bus Core Gameplay.

## Extensions futures

Le même contrat accueillera les événements Quêtes, Battle Pass, Succès, Tutoriel, Analytics, Notifications, Audio, VFX, Alliance, PvP, Monde, Boutique, Calendrier et événements saisonniers. Des priorités, filtres, diagnostics et files différées pourront être ajoutés au bus sans modifier les modules existants.
