# Phase 5 — sortie au périmètre de la ruche (serveur)

## Contrat livré

Le noyau serveur introduit `phase5-hive-perimeter-sortie-v1`, persistant dans `PlayerHiveState` (migration modèle v8→v9). Le cycle courant est calculé exclusivement avec l’horloge UTC serveur et dure 8 heures. Le catalogue borné contient `foraging_scout` (16 s, minimum 1, 40 miel/20 pollen, hazardDoctrine `wingrunners`) et `brood_watch` (20 s, minimum 2, 25 miel/35 pollen, hazardDoctrine `guardians`). Les deux signaux sont des observations non-combat : aucune carte, coordonnée, victoire, perte ou puissance n’est simulée.

Le service vérifie une réservation de squad active et la conserve pendant la sortie. La lecture et les mutations launch/claim/recall sont exposées sous `/game/v1/hives/{hiveId}/perimeter-sortie`, avec authentification et appartenance par bearer. Le drapeau `HivePerimeterSortie:Enabled` est false par défaut : 503 `game.unavailable` est renvoyé avant authentification/lecture/mutation.

Claim après `EndsAtUtc` crédite uniquement les soldes existants sans dépasser leur capacité, puis remet les trois familles de réservation à zéro. Recall libère sans récompense. Les reçus sont bornés à 4096, les clés à 256 caractères et les rejeux contradictoires renvoient `game.idempotency_conflict`.

## Fichiers modifiés

- `Server/src/BeeKingdom.HiveOperations/HiveOperationModels.cs`
- `Server/src/BeeKingdom.HiveOperations/HivePerimeterSortieService.cs`
- `Server/src/BeeKingdom.HiveOperations/HivePerimeterSortieOptions.cs`
- `Server/src/BeeKingdom.HiveOperations/HiveStateMigrator.cs`
- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/src/BeeKingdom.Server/appsettings.json`
- `Server/src/BeeKingdom.Server/appsettings.Production.json`
- `Server/tests/BeeKingdom.HiveOperations.Tests/HivePerimeterSortieTests.cs`
- `Server/tests/BeeKingdom.Tests/HivePerimeterSortieEndpointTests.cs`

## Preuves

- Build Release `BeeKingdom.Server.csproj`: 0 erreur, 1 avertissement existant de conflit Microsoft.Data.SqlClient.
- Suite `BeeKingdom.HiveOperations.Tests` (Release, runtime avec `DOTNET_ROLL_FORWARD=Major`): 47/47 réussis, 0 échec, 0 ignoré.
- Après exécution : aucun processus `dotnet` ou `testhost` du chantier détecté.
- Tests Phase 5 ciblés DurableJson : 5/5 réussis, incluant reconstruction, rejeu, rollover, claim/recall, garde de libération, capacité et migration corrompue. Suite HiveOperations complète : 52/52 réussis. Build Release serveur : 0 erreur, avertissement SQL préexistant.
- Un premier essai net8 a été refusé historiquement (0 découvert puis 0/3 avec incompatibilité PipeWriter); il est remplacé par la cible net10 prévue par le projet.
- Relance avec le lanceur net8.0 demandé : 3 tests découverts, 0 réussis, 3 échoués. Les échecs se produisent dans le pipeline WebApplicationFactory (`ResponseBodyPipeWriter`/`UnflushedBytes`) avant la preuve métier; ils ne constituent pas une ratification HTTP.
- Cible de test (`-p:EnableNet10TestTarget=true --framework net10.0`) : `HivePerimeterSortieEndpointTests` 5/5 réussis. Les tests métier utilisent désormais un dépôt DurableJson isolé, un état seedé authentifié et une horloge mutable; launch et rejeu sont vérifiés avec instance serveur. Suite précédente `BeeKingdom.Tests` : 265 réussis, 7 ignorés, 0 échec (272 total).
- Après correction de la désérialisation camelCase via `ReadFromJsonAsync`, les assertions HTTP launch/claim/rejeu/release/recall sont passées. Full `BeeKingdom.Tests` net10.0 : 265/272 réussis, 7 ignorés, 0 échec. La porte HTTP ciblée est fermée pour cette tranche.
- Les précédents essais net8 (0 découvert puis 0/3) sont historiques et refusés; la preuve de référence est la cible net10.0 prévue par le projet : 5/5 ciblés, avec assertions de code `game.perimeter_not_complete`, soldes exacts, réservation nulle et roster 4/6/4.
- Le snapshot expose désormais `Revision` (révision du cycle) : la séquence board rev0 → launch rev1 → claim rev2 permet un lancement ultérieur avec `expectedRevision=2` sans recalcul côté client.
- Le snapshot expose également `ServerTimeUtc`, alimenté par la même horloge serveur injectable que les mutations. Preuves : service ciblé 5/5 et HTTP ciblé 5/5 ; les réponses GET, launch et claim égalent `clock.UtcNow` et portent toutes un offset UTC nul.
- Preuve HTTP séquentielle ajoutée et passée : board rev0, launch `foraging_scout` rev1, claim rev2/Active=null, nouvelle réservation, launch `brood_watch` avec expectedRevision=2, snapshot rev3 et réservation concordante. Tests service et HTTP ciblés : 5/5 chacun.
- Les signaux complétés sont désormais persistés par cycle, l’instance de signal est liée à player+hive+cycleStart+SignalKey, et une réservation ne peut plus être libérée par la route squad pendant une sortie active.
- Le snapshot expose maintenant un read model par signal (`SignalInstanceId`, `Completed`, `CanLaunch`). Une sortie active peut dépasser la fin du cycle de huit heures; l’ancien cycle est conservé jusqu’au claim/recall et les reçus sont conservés au rollover.
- Feature flags `HivePerimeterSortie`, `Chat`, `Realtime` et `DeploymentAuthorized` restent fermés; aucun candidat, déploiement ou synchronisation n’a été effectué.

## Addendum reçu de claim — validation 2026-07-22

Le snapshot expose désormais un `claimReceipt` optionnel, écrit dans la même transaction que le crédit et la libération, puis conservé dans `ClaimReceipts` pour un rejeu exact après reconstruction DurableJson. Le reçu corrèle joueur, ruche, sortie, signal, instance, cycle, révision, `ServerTimeUtc`, crédits effectifs par ressource et soldes/capacités résultants. Une capacité partielle crédite le minimum autoritaire restant; une capacité pleine crédite zéro sans dépasser la capacité. Une clé contradictoire reste `game.idempotency_conflict`.

Preuves contemporaines : filtre service Phase 5 6/6; filtre HTTP net10 6/6; suite HiveOperations 53/53; suite BeeKingdom.Tests net10 266 réussis, 7 ignorés, 0 échec (273); build Release 0 erreur, avertissement Microsoft.Data.SqlClient préexistant. SQL externe reste ignoré (7 tests). Les flags restent fermés et `DeploymentAuthorized=false`.

## Frontière appareil/serveur

L’appareil peut conserver un cache de lecture, un brouillon et des notifications, mais ne fournit ni horloge, ni composition autoritaire, ni récompense. Le serveur reste propriétaire du cycle, de la réservation, des préconditions, des temps et des soldes. La carte mondiale demeure `PreparationOnly/ReadOnlyNonLiveFoundation`.
