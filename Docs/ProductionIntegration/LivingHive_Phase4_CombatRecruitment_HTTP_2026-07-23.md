# Phase 4 — recrutement doctrinal et readiness

Le serveur expose, derrière deux drapeaux fermés (`CombatRecruitment:Enabled=false`
et `CombatFormationReadiness:Enabled=false`), les lectures et commandes suivantes :

- `GET /game/v1/hives/{hiveId}/combat/recruitment`
- `POST /game/v1/hives/{hiveId}/combat/recruitment/start`
- `POST /game/v1/hives/{hiveId}/combat/recruitment/{operationId}/claim`
- `GET /game/v1/hives/{hiveId}/combat/formation-readiness`

Les routes exigent un bearer et une appartenance joueur/ruche. Les corps start/claim
contiennent uniquement `family` (start), `expectedRevision` et `idempotencyKey`;
les clés sont bornées et ASCII sûres. Les coûts, lots, durées, révisions et comptes
sont exclusivement autoritaires. Les replays utilisent la même clé/charge et les
conflits sont renvoyés en `409`; aucune lecture ne mute l’état.

La projection publique est dédiée : snapshot avec `contractVersion`, `catalogVersion`,
`playerId`, `hiveId`, `revision`, `serverTimeUtc`, offres, soldes miel/pollen,
comptes doctrinaux, rôles legacy et opération publique limitée à id/famille/lot,
horodatages et statut. START/CLAIM renvoient `{ receipt, snapshot }`; aucune clé
interne ni `payloadHash` n’est sérialisée.

Les reçus publics portent désormais les révisions du `DoctrineRoster` (et non la
révision globale), la famille et le lot persistés dans les champs de reçu internes;
les rejeux CLAIM reproduisent ces valeurs même lorsque plusieurs familles existent.
La projection capture une seule horloge UTC par snapshot. CLAIM refuse tout
dépassement de compte au-delà de 1_000_000_000 et retient au plus 128 reçus,
avec éviction déterministe par `CreatedAtUtc` puis clé lexicographique, en
conservant toujours le reçu courant.

Fichiers de cette passe :

- `Server/src/BeeKingdom.HiveOperations/CombatRecruitmentService.cs`
- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/tests/BeeKingdom.HiveOperations.Tests/CombatRecruitmentTests.cs`
- `Server/tests/BeeKingdom.Tests/CombatRecruitmentEndpointTests.cs`

Preuves finales exécutées sur l’état serveur courant :

- `CombatRecruitmentEndpointTests` : 3/3;
- `CombatRecruitmentTests` cœur : 4/4;
- suite serveur `net10.0` : 341 réussis, 0 échec, 8 SQL ignorés;
- build Release : 0 erreur, 1 avertissement préexistant
  `Microsoft.Data.SqlClient`.

Les processus `dotnet` et `testhost` étaient absents après la passe. Aucun flag
n’est activé ici.

Le roster absent reste `not_recorded` pour readiness, sans faux zéros ni conversion
des rôles legacy. Aucun coefficient, dégât, victoire ou déploiement n’est exposé.
SQL, TLS/IIS, staging Android, activation et candidat restent des portes ouvertes.
