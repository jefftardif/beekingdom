# Dotation fondatrice du chapitre 1 — contrat serveur

## Portée

Tranche serveur locale/staging uniquement. Le profil v12 présent dans Unity
reste une prévisualisation/cache local et ne constitue pas une autorité de
récompense. Aucun fichier `Assets/`, scène, carte, image ou module chat n'est
modifié.

## Commande authentifiée

`POST /game/v1/hives/{hiveId}/chapter-1/foundation`

La route est derrière `FoundingFoundation:Enabled`, fermé (`false`) dans les
configurations locales et Production. Tant que ce drapeau reste fermé, toute
requête retourne `503 game.unavailable` avant authentification ou mutation.
Son ouverture est réservée à un environnement de test/staging où une
installation serveur éligible est déjà attestée; aucun booléen client ne peut
la créer.

L'infrastructure actuelle ne possède pas encore de transition de chapitre
serveur prouvant l'installation complète; aucun endpoint n'accepte donc de
mettre `InstallationComplete=true` à partir d'un booléen client. Le jalon
suivant doit relier ce champ à une certification serveur de l'installation
avant toute ouverture du drapeau.

En-tête obligatoire : `Authorization: Bearer <session valide>`.

Corps camelCase :

```json
{
  "expectedRevision": 12,
  "choice": "honey_reserve",
  "idempotencyKey": "chapter1-foundation-<opaque-key>"
}
```

Les choix autorisés sont strictement :

- `honey_reserve` : +250 miel, +0 pollen ;
- `mixed_foundation` : +170 miel, +80 pollen.

Le `PlayerId` est exclusivement pris dans le Bearer validé; il n'est jamais
accepté dans le corps.

## Validation et transaction

Sous le verrou atomique du dépôt de ruche, le serveur vérifie :

1. choix et clé d'idempotence présents ;
2. révision attendue exacte ;
3. installation serveur marquée complète ;
4. absence de dotation déjà persistée ;
5. capacité suffisante pour créditer la totalité de la dotation.

La transaction persiste ensemble le choix, les deux montants, un `Proof` opaque
non secret, la date UTC, les soldes et `Revision + 1`. Le reçu anti-rejeu porte
le hash du joueur, de la ruche et du choix, ainsi que le résultat. Une reprise
avec la même clé et le même choix retourne le même état/proof sans second
crédit. La même clé avec un autre choix retourne `409` /
`game.idempotency_conflict`. Une autre clé après réussite retourne
`409` / `game.foundation_conflict`.

Réponses 200 :

```json
{
  "choice": "mixed_foundation",
  "honeyAwarded": 170,
  "pollenAwarded": 80,
  "proof": "<opaque-proof>",
  "revision": 13,
  "honeyBalance": 170,
  "pollenBalance": 80
}
```

La route retourne 401 sans session valide, 400 pour choix/requête invalide et
409 pour révision, éligibilité, capacité ou rejeu contradictoire. Elle ne
modifie pas l'état si la validation échoue.

## Preuves locales

- `BeeKingdom.HiveOperations.Tests` : **14/14 réussis** sous hôte de
  compatibilité .NET 10 avec cible compilée net8.0 ;
- `BeeKingdom.Tests` : **251 réussis, 0 échec, 7 ignorés** (tests SQL externes),
  dont le contrat HTTP fondation ;
- sous-ensemble `GameFoundationEndpointTests` : **2/2 réussis** ;
  sous cible net10.0 ;
- compilation Release du serveur : **0 erreur** ; un avertissement préexistant
  de versions `Microsoft.Data.SqlClient` reste visible ;
- le dépôt de ruche sélectionne `SqlHiveStateRepository` lorsque
  `Persistence.Provider=SqlServer`, et le dépôt JSON local uniquement en mode
  `InMemory`; aucun fallback SQL silencieux n'est ajouté ;
- l'ajout de `InstallationComplete` et `FoundationDotation` fait passer
  `HiveStateMigrator.CurrentModelVersion` de 3 à **4**. `DurableJson` applique
  désormais `ToCurrent` en lecture et avant mutation atomique, comme SQL. Un
  test charge un état v3 sans les nouveaux champs et vérifie sa matérialisation
  v4 ;
- SQL Server/LocalDB et runtime .NET 8 natif absents de la VM : la preuve SQL
  reconstructible reste à exécuter en staging.

Le candidat `BeeKingdom.Server.20260721T211506Z` a été révoqué avant
reconstruction. Le successeur courant est
`BeeKingdom.Server.20260721T212438Z` (55 fichiers, manifeste SHA-256 validé,
smoke Healthy sur `127.0.0.1:5141`), avec
`DeploymentAuthorized=false`, `FoundingFoundation:Enabled=false`,
`ChatEnabled=false`, `RealtimeEnabled=false` et `PreparationOnly`.

## Fichiers créés ou modifiés

- `Server/src/BeeKingdom.HiveOperations/HiveOperationModels.cs`
- `Server/src/BeeKingdom.HiveOperations/HiveOperationService.cs`
- `Server/src/BeeKingdom.HiveOperations/HiveStateMigrator.cs`
- `Server/src/BeeKingdom.HiveOperations/DurableJsonHiveStateRepository.cs`
- `Server/src/BeeKingdom.HiveOperations/FoundationDotationOptions.cs`
- `Server/src/BeeKingdom.Server/BeeKingdom.Server.csproj`
- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/src/BeeKingdom.Server/appsettings.json`
- `Server/src/BeeKingdom.Server/appsettings.Production.json`
- `Server/tests/BeeKingdom.HiveOperations.Tests/HiveOperationServiceTests.cs`
- `Server/tests/BeeKingdom.Tests/BeeKingdom.Tests.csproj`
- `Server/tests/BeeKingdom.Tests/GameFoundationEndpointTests.cs`
- `Server/artifacts/candidates/BeeKingdom.Server.20260721T211506Z/` (révoqué)
- `Server/artifacts/candidates/BeeKingdom.Server.20260721T212438Z/` (courant local, 55 fichiers)
- `Server/artifacts/candidates/CANDIDATE-STATUS.json`
- `Docs/ProductionIntegration/Chapter1_FoundingFoundation_ServerContract_2026-07-21.md`

## Porte de promotion

Mise à jour du 2026-07-21 : le candidat local vérifié le plus récent est
`BeeKingdom.Server.20260721T215236Z` (55 fichiers, smoke local Healthy). Il
remplace le courant précédent dans `CANDIDATE-STATUS.json`; il reste
`DeploymentAuthorized=false`, avec fondation, chat et temps réel désactivés.

Cette tranche est prête pour validation locale/staging, mais ne change pas le
mode `PreparationOnly`, ne remplace pas la configuration SQL de production et
ne déclenche aucun déploiement, activation ou synchronisation.

Le candidat 212438Z a été produit avant le déplacement de la suite HTTP et la
dernière correction de tests; conformément au gel Unity, aucun nouveau candidat
n'est produit pendant cette fenêtre. Il reste conservé avec
`DeploymentAuthorized=false` et devra être reconstruit après le signal de fin.
