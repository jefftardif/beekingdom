# SERVER-B-061 - SQL AppLock Pooling Lifecycle Correction Report

Date locale : 2026-07-13 23:19:16 -04:00  
Date UTC : 2026-07-14 03:19:16 UTC  
Portee : poste local uniquement, SQL Server LocalDB jetable  
Statut : correction et validation terminees, aucune activation staging/live

## 1. Blocker QA confirme

QA-A a correctement identifie que `sp_getapplock` etait utilise avec `LockOwner = Session` sans appel correspondant a `sp_releaseapplock`.

Avec `Pooling=true`, `SqlConnection.Close` ou `Dispose` rend la connexion physique au pool. La session SQL reste alors vivante et conserve son verrou. Une acquisition ulterieure depuis un autre SPID peut expirer. Les tests SERVER-B-059 imposaient `Pooling=false` et masquaient ce defaut.

## 2. Correction du runner

Les trois sections critiques du runner utilisent maintenant le meme helper `ExecuteWithSessionLockAsync` :

1. `BeeKingdom.Database.Create:<databaseName>` autour du test d'existence et de `CREATE DATABASE` ;
2. `BeeKingdom.Database.Migrations` autour de la lecture des migrations en attente ;
3. `BeeKingdom.Database.Migrations` autour de l'application des migrations.

Pour chaque appel :

- `sp_getapplock` est execute sur la connexion qui portera la section critique ;
- chaque acquisition reussie est suivie d'un seul `sp_releaseapplock` ;
- la liberation utilise la meme ressource, la meme connexion et `LockOwner = Session` ;
- la liberation est dans un `finally` ;
- la liberation utilise `CancellationToken.None`, afin qu'une annulation appelante ne saute pas le nettoyage ;
- les codes negatifs de `sp_getapplock` et `sp_releaseapplock` sont rejetes explicitement.

### Etat d'acquisition ambigu

Une annulation ou exception pendant `sp_getapplock` peut survenir apres que SQL Server a accorde le verrou mais avant que le client ait recu le resultat. Dans ce cas le runner ne suppose pas que le verrou est absent :

- `SqlConnection.ClearPool(connection)` marque la connexion physique pour destruction ;
- `CloseAsync` ferme ensuite la connexion ;
- `ClearAllPools` sert de repli si l'eviction ciblee echoue ;
- l'exception initiale est propagee.

La session physique ambiguë ne peut donc pas retourner normalement au pool.

### Echec de liberation

Si `sp_releaseapplock` echoue :

- la connexion est evincee puis fermee ;
- l'erreur de liberation est propagee lorsqu'elle est l'erreur principale ;
- si une erreur metier/migration est deja en cours, elle reste l'erreur principale et l'erreur de liberation est journalisee apres eviction physique.

## 3. Pooling representatif

Le harness SQL utilise maintenant explicitement `Pooling=true` pour :

- la connexion runtime ;
- la connexion migration ;
- la connexion `master` derivee ;
- la base restauree du scenario backup/restore.

Les deux occurrences de `Pooling=false` identifiees par QA ont ete supprimees. `TearDown` dispose les providers, appelle `SqlConnection.ClearAllPools`, puis supprime les bases jetables.

## 4. Preuves LocalDB ajoutees

Les nouvelles preuves restent dans le scenario top-level existant `SqlServerSerializesMigrationsAndRejectsConcurrentDuplicateAccount`. Le nombre de scenarios SQL reste donc exactement six et la matrice globale reste exactement 23 tests.

### Sessions physiques et attente

Pour chaque ressource, le test ouvre un proprietaire et un verificateur avant de lancer le runner :

- leurs `@@SPID` sont distincts ;
- le proprietaire acquiert le verrou ;
- le runner demarre sur une troisieme session physique et reste en attente ;
- le proprietaire libere ;
- le runner termine apres avoir acquis puis libere ;
- le verificateur, reste ouvert et donc non reutilisable par le runner, acquiert ensuite la meme ressource.

Cette preuve est executee separement pour :

- `BeeKingdom.Database.Create:<databaseName>` ;
- `BeeKingdom.Database.Migrations`.

### Creation initiale concurrente

Quatre providers independants lancent les migrations en parallele contre une base initialement absente. La creation, les migrations et l'unicite de `SchemaVersion` restent coherentes.

### Acquisitions repetees

Le runner execute trois lectures consecutives des migrations en attente avec pooling actif. Des connexions verifiantes, ouvertes avant ces appels, acquierent ensuite les deux ressources. Aucun compteur de verrou residuel n'est present.

### Exception de creation

Un nom de base local de 129 caracteres provoque une erreur SQL dans la section critique de creation, apres acquisition du verrou. Une session verifiante acquiert ensuite la ressource de creation et la base invalide n'existe pas.

### Annulation pendant l'acquisition de creation

Une session externe conserve le verrou de creation pendant que le runner attend. Le token est annule, SqlClient remonte localement l'annulation sous forme de `SqlException`, la connexion ambiguë est evincee, puis une session distincte peut acquerir la ressource.

### Exception de migration

Le test remplace temporairement `dbo.SchemaVersion` par une table de forme invalide. La lecture echoue avec l'erreur SQL 207 apres acquisition du verrou migration. Le verrou est disponible depuis une autre session avant que le schema original soit restaure.

### Annulation apres acquisition de migration

Une transaction externe conserve un verrou `TABLOCKX, HOLDLOCK` sur `dbo.SchemaVersion`. Le runner acquiert d'abord l'applock migration puis attend sur la table. Un probe depuis un autre SPID confirme que l'applock est detenu avant l'annulation. L'annulation declenche le `finally`, puis le probe acquiert la ressource apres liberation.

## 5. Resultats de validation

### Scenario pooling cible

`SqlServerSerializesMigrationsAndRejectsConcurrentDuplicateAccount` : **PASS, 1/1**.

Le scenario cible a ete execute avec `Pooling=true` avant la matrice complete.

### Matrice SQL Release

Commande equivalente au filtre canonique du runbook :

```powershell
$env:BEE_SQL_INTEGRATION_CONNECTION_STRING = 'Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=15;'
dotnet test .\Server\BeeKingdom.Server.slnx --configuration Release --no-restore --filter "FullyQualifiedName~SqlServerOptInIntegrationTests|FullyQualifiedName~DatabaseMigrationTests|FullyQualifiedName~PersistenceProviderSelectionTests" --logger "console;verbosity=normal"
```

Resultat : **23 passes, 0 echec**, dont **6/6 scenarios SQL opt-in passes** :

1. `SqlServerBackupCanBeVerifiedAndRestoredToDisposableDatabase`
2. `SqlServerCreatesDisposableDatabaseAndAppliesMigrationsIdempotently`
3. `SqlServerRepositoryRoundTripsSyntheticAccountProgression`
4. `SqlServerSerializesMigrationsAndRejectsConcurrentDuplicateAccount`
5. `SqlServerStoresSyntheticCredentialSessionAndWorldScopedColonies`
6. `WorldSchemaReadinessDraftExecutesAndRollsBackLocally`

Creation, migrations idempotentes, round-trip compte, scope monde, concurrence, backup/restore et rollback du schema monde sont tous PASS.

### Suite .NET Release complete

Commande :

```powershell
Remove-Item Env:BEE_SQL_INTEGRATION_CONNECTION_STRING -ErrorAction SilentlyContinue
dotnet test .\Server\BeeKingdom.Server.slnx --configuration Release --no-restore --logger "console;verbosity=minimal"
```

Resultat : **142 passes, 0 echec, 6 ignores, 148 total**.

Les six ignores sont exactement les six scenarios SQL opt-in ci-dessus, tous executes et passes dans la matrice dediee.

## 6. Note d'environnement LocalDB

Deux tentatives via `Invoke-LocalSqlReadiness.ps1` ont rencontre une anomalie locale de l'API `sqllocaldb start MSSQLLocalDB` : l'instance automatique etait annoncee comme non creee alors qu'une connexion directe `(localdb)\MSSQLLocalDB` la demarrait et repondait correctement.

Ces tentatives ont ete arretees et ne sont pas comptees comme validation. Le script ops a ete remis dans son etat entrant. La matrice a ensuite ete executee directement avec le meme filtre, la meme configuration Release et la meme chaine LocalDB, avec 23/23 PASS.

Cette anomalie de demarrage de l'instance automatique est un point d'outillage local, pas un echec du cycle de vie des applocks. QA peut reproduire avec sa propre instance LocalDB deja fonctionnelle ou avec la commande directe documentee ci-dessus.

## 7. Nettoyage et securite

Apres la derniere matrice :

- bases `BeeKingdom_Local_SERVERB057_%` : **0** ;
- fichiers correspondants sous `%TEMP%` ou `Server` : **0** ;
- processus de test SQL Bee Kingdom actifs : **0** ;
- providers et connexions de test disposes ;
- pools SqlClient nettoyes par le `TearDown`.

Controle Production en lecture seule :

- `Persistence.Provider = InMemory` ;
- aucune chaine runtime SQL ;
- aucune chaine migration SQL ;
- aucune activation SQL staging/live.

Le scan des fichiers modifies ne trouve aucun mot de passe, identifiant SQL, cle API, secret client, jeton, cle privee ou adresse du serveur distant. Les seuls faux positifs sont les noms de parametres de duree `Authentication:AccessTokenLifetime` et `Authentication:RefreshTokenLifetime`.

## 8. Fichiers modifies

1. `Server/src/BeeKingdom.Persistence/Migrations/SqlServerMigrationRunner.cs`
2. `Server/tests/BeeKingdom.Tests/SqlServerOptInIntegrationTests.cs`
3. ce rapport local SERVER-B-061

`Server/ops/sql-readiness/Invoke-LocalSqlReadiness.ps1` a ete experimente puis restaure ; il ne fait pas partie du changement final.

Aucun fichier Production, script SERVER-056/060, fichier Unity, migration SQL, secret ou composant distant n'a ete modifie.

## 9. Limites

- Validation sous SQL Server LocalDB et identite Windows locale uniquement.
- Pas de test de latence reseau, droits staging, haute disponibilite ou charge.
- Le verdict autorise une revalidation QA du cycle de vie des applocks. Il n'active aucune persistance SQL staging/live.

READY_FOR_QA_SQL_APPLOCK_POOLING_LIFECYCLE = YES
