# Phase 5 — validation finale de la sortie au périmètre

## Contrat public

Les trois mutations `launch`, `claim` et `recall` renvoient `HivePerimeterMutationResponse { receipt, snapshot }`. Le reçu camelCase contient : `playerId`, `hiveId`, `idempotencyKey`, `action` (`launch|claim|recall`), `sortieId`, `signalKey`, `signalInstanceId`, `reservationId`, `cycleStartedAtUtc`, `cycleEndsAtUtc`, `revisionBefore`, `revisionAfter`, `acceptedAtUtc`, `creditedByResource`, `resultingBalances`, `code`. Aucun `payloadHash` ni secret interne n'est exposé.

Les reçus sont bornés à 128 par ruche, avec éviction déterministe (horodatage puis clé) en conservant le reçu courant. Le rejeu d'une même charge reconstruit le reçu initial après reconstruction, claim/recall et rollover; le snapshot peut refléter l'état courant. `long.MaxValue` est refusé avant tout incrément.

## Preuves exécutées

- Build Release serveur : 0 erreur; 1 avertissement préexistant de conflit `Microsoft.Data.SqlClient`.
- `BeeKingdom.HiveOperations.Tests`, filtre `HivePerimeterSortieTests`, Release/net8 avec `DOTNET_ROLL_FORWARD=Major` : 6 réussis, 0 échec.
- Relance HTTP `HivePerimeterSortieEndpointTests`, Release/net10 (`EnableNet10TestTarget=true`) : 6 découverts, 3 réussis, 3 échecs. Les trois échecs proviennent des fixtures historiques qui désérialisent encore directement `HivePerimeterSnapshot`; elles doivent lire `HivePerimeterMutationResponse.Snapshot` depuis la nouvelle enveloppe. Ce résultat ne constitue pas une ratification HTTP verte.

## Portes conservées

`HivePerimeterSortie:Enabled=false` par défaut et en Production, `DeploymentAuthorized=false`; aucune activation, synchronisation, candidat ou déploiement. SQL externe, staging/TLS/IIS et alignement des trois fixtures HTTP restent ouverts. Aucun fichier Assets/Unity/chat n'a été modifié.
