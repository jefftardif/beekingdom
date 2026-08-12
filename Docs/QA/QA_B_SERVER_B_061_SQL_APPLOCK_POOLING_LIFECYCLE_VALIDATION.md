# QA-B - SERVER-B-061 SQL AppLock Pooling Lifecycle Validation

Date locale : 2026-07-13/14 (America/Toronto)  
Role : QA-B, validation locale independante pour QA-A/Architecte  
Portee : SQL Server LocalDB jetable, aucun acces distant ou staging  
Gate officiel : reserve a QA-A

## Verdict QA-B

**PASS**

SERVER-B-061 corrige le blocker confirme de SERVER-B-059. Les verrous de creation de base et de migration sont liberes explicitement sur la meme connexion et avec la meme ressource dans un `finally`. Les chemins succes, attente concurrente, acquisitions repetees, exception, annulation, acquisition ambigue et release ambigue sont conformes sous `Pooling=true`.

La matrice SQL passe 23/23, les six scenarios SQL opt-in passent 6/6, la suite Release passe 142/0/6 et le scenario backup/restore passe. Aucun residu SQL ou fichier n'est present. L'anomalie locale `sqllocaldb start` annoncee par Server-B n'est pas reproductible pendant cette validation et ne justifie aucune reserve.

Ce PASS est une conclusion QA-B locale. Il ne retrovalide pas SERVER-B-059, ne ferme pas le gate officiel de QA-A et n'active aucune persistance staging/live.

## Sources controlees

1. `C:\projets\beekingdom\prompt_server\rapports\SERVER-B-061 - SQL AppLock Pooling Lifecycle Correction Report.md`
2. `C:\projets\beekingdom\QA\QA_SERVER_B_059_SQL_ACCOUNT_ROUNDTRIP_VALIDATION.md`
3. `C:\projets\beekingdom\QA\QA_B_SERVER_B_059_SQL_APPLOCK_POOLING_INVESTIGATION.md`
4. `C:\projets\beekingdomgame-master\Server\src\BeeKingdom.Persistence\Migrations\SqlServerMigrationRunner.cs`
5. `C:\projets\beekingdomgame-master\Server\tests\BeeKingdom.Tests\SqlServerOptInIntegrationTests.cs`
6. `C:\projets\beekingdomgame-master\Server\src\BeeKingdom.Server\appsettings.Production.json`

Le rapport SERVER-B-061 reconnait explicitement le blocker 059 et presente son travail comme une nouvelle correction locale. Aucune retrovalidation trompeuse de 059 n'est relevee.

## Matrice de decision

| # | Critere | Resultat QA-B |
|---:|---|---:|
| 1 | Release explicite dans un `finally`, meme connexion/ressource, owner Session | PASS |
| 2 | Creation DB et migrations : succes, exception, annulation, repetitions | PASS |
| 3 | Eviction du pool et priorite correcte des erreurs | PASS |
| 4 | `Pooling=true`, SPID distincts et acquisition apres release | PASS |
| 5 | Aucun blocage apres fermeture/reutilisation poolee | PASS |
| 6 | 23/23, 6/6 SQL, 142/0/6, backup/restore | PASS |
| 7 | Anomalie `sqllocaldb start` analysee | PASS, aucune reserve |
| 8 | Production InMemory, aucun secret | PASS |
| 9 | Aucun staging, live ou acces distant | PASS |

## Verification statique du runner

### Trois sections critiques

Le helper unique `ExecuteWithSessionLockAsync` encadre :

- la lecture des migrations en attente, lignes 40-54 ;
- l'application des migrations, lignes 69-93 ;
- la creation conditionnelle de la base, lignes 102-127.

Les ressources restent :

- `BeeKingdom.Database.Create:<databaseName>` pour la creation ;
- `BeeKingdom.Database.Migrations` pour les deux chemins de migration.

### Acquisition et release

Dans `SqlServerMigrationRunner.cs` :

- l'acquisition est executee ligne 143 ;
- l'operation est executee lignes 151-160 ;
- le `finally` commence ligne 161 ;
- la release est executee ligne 165 ;
- `sp_getapplock` utilise `LockOwner = Session`, lignes 184-203 ;
- `sp_releaseapplock` reutilise le meme parametre `resource` et `LockOwner = Session`, lignes 206-223 ;
- la release utilise `CancellationToken.None`, ligne 222.

Le helper recoit une connexion deja ouverte et la transmet sans substitution a l'acquisition, a l'operation et a la release. Chaque acquisition reussie atteint donc une seule tentative de release dans le `finally`.

### Erreurs et eviction

En cas d'erreur ou annulation pendant l'acquisition :

- `DiscardPooledConnectionAsync` est appele avant le `throw` original, lignes 145-149.

En cas d'erreur de release :

- la connexion est evincee, lignes 167-170 ;
- si aucune erreur operationnelle n'existe, l'erreur de release est propagee, lignes 170-173 ;
- si une erreur operationnelle existe deja, elle reste primaire et l'erreur de release est journalisee, lignes 175-179.

L'eviction tente `SqlConnection.ClearPool(connection)`, puis `CloseAsync`; `ClearAllPools` sert de repli si l'eviction ciblee echoue, lignes 225-263.

## Couverture producteur

Le setup SQL impose maintenant `Pooling=true` pour les connexions runtime, migration et master derivee. Le backup restaure utilise egalement `Pooling=true`.

Le scenario `SqlServerSerializesMigrationsAndRejectsConcurrentDuplicateAccount` couvre :

- quatre providers concurrents sur une base initialement absente ;
- attente puis acquisition/release pour le verrou creation ;
- attente puis acquisition/release pour le verrou migration ;
- trois acquisitions consecutives sans compteur residuel ;
- exception SQL dans la section creation ;
- annulation pendant l'acquisition du verrou creation ;
- exception SQL 207 apres acquisition du verrou migration ;
- annulation apres acquisition du verrou migration.

Les connexions owner et verifier sont ouvertes simultanement. Elles ne peuvent donc pas etre reutilisees par le runner pendant la preuve. Le runner doit ouvrir une session physique additionnelle.

## Sonde QA-B independante

Une sonde locale reference directement `BeeKingdom.Persistence` et charge le pilote exact du projet :

- assembly : `Microsoft.Data.SqlClient` ;
- version informationnelle : `5.2.2` ;
- cible : `(localdb)\MSSQLLocalDB` uniquement ;
- Integrated Security ;
- `Pooling=true` explicite ;
- aucun secret ou endpoint distant.

### Succes et reutilisation

| Mesure | Valeur |
|---|---:|
| SPID owner | 77 |
| SPID verifier | 78 |
| SPID distincts | oui |
| Acquisition owner | 0 |
| Verifier pendant detention | -1 |
| Release owner | 0 |
| Verifier apres release | 0, attente 0 ms |
| Connexion physique owner reutilisee | oui |
| Mode apres fermeture/reutilisation | `NoLock` |
| Verifier apres reutilisation | 0 |

La seconde session acquiert donc immediatement apres la release. La fermeture et la reutilisation de la connexion poolee ne recreent aucun blocage.

### Acquisition ambigue

Trois SPID simultanes et distincts ont ete observes. Le runner attendait un verrou externe lorsque son token a ete annule :

- erreur d'annulation observee ;
- chemin d'eviction journalise ;
- connexion victime fermee ;
- identifiant de connexion physique different a la reprise ;
- verifier apres l'echec : acquisition 0, attente 0 ms.

Le verrou et une connexion d'etat ambigu ne reviennent pas normalement au pool.

### Release ambigue, erreur primaire

La sonde ferme volontairement la connexion dans la section critique, avant le `finally` :

- la release echoue par `InvalidOperationException` ;
- l'erreur de release est propagee ;
- le chemin d'eviction est journalise ;
- l'identifiant physique de remplacement est different ;
- une seconde session acquiert avant toute reutilisation du pool owner, resultat 0.

### Release ambigue avec erreur operationnelle existante

La meme panne de release est combinee a une exception sentinelle de l'operation :

- l'exception sentinelle reste l'erreur observee ;
- l'erreur de release ne la masque pas ;
- la connexion est evincee ;
- la seconde session acquiert avant reutilisation du pool owner, resultat 0.

Cette sonde complete la couverture producteur, qui ne force pas directement une panne de `sp_releaseapplock`.

## Executions QA-B

### Matrice SQL Release

Execution directe avec la chaine LocalDB integree et le filtre canonique :

- total : 23 ;
- passes : 23 ;
- echecs : 0 ;
- ignores : 0.

Les six scenarios SQL opt-in sont presents une fois chacun et passent :

1. `SqlServerBackupCanBeVerifiedAndRestoredToDisposableDatabase`
2. `SqlServerCreatesDisposableDatabaseAndAppliesMigrationsIdempotently`
3. `SqlServerRepositoryRoundTripsSyntheticAccountProgression`
4. `SqlServerSerializesMigrationsAndRejectsConcurrentDuplicateAccount`
5. `SqlServerStoresSyntheticCredentialSessionAndWorldScopedColonies`
6. `WorldSchemaReadinessDraftExecutesAndRollsBackLocally`

Le test backup/restore execute `BACKUP ... WITH CHECKSUM`, `RESTORE VERIFYONLY`, restaure une base jetable et confirme le marqueur compte ainsi que toutes les migrations : PASS.

### Suite Release complete

Avec `BEE_SQL_INTEGRATION_CONNECTION_STRING` absent :

- total : 148 ;
- executes/passes : 142 ;
- echecs : 0 ;
- non executes : les six scenarios SQL opt-in, exactement.

Le resultat attendu `142/0/6` est confirme.

## Anomalie LocalDB

Le rapport producteur indique deux echecs historiques de `sqllocaldb start MSSQLLocalDB`, puis une matrice directe equivalente reussie. QA-B a rejoue un cycle propre :

1. `sqllocaldb stop MSSQLLocalDB` : code 0 ;
2. `sqllocaldb start MSSQLLocalDB` : code 0 ;
3. connexion directe et `SERVERPROPERTY('IsLocalDB')` : code 0, valeur 1 ;
4. arret final : code 0.

L'anomalie n'est pas reproductible. La matrice directe utilise le meme filtre, la meme configuration Release et la meme chaine LocalDB que la commande documentee. Elle est fonctionnellement suffisante pour le gate local; aucune reserve QA-B n'est requise sur ce point.

## Production, secrets et non-claims

Le controle structure de `appsettings.Production.json` confirme :

- `Persistence.Provider = InMemory` ;
- aucune section `ConnectionStrings` ;
- aucune chaine runtime ou migration SQL ;
- cles admin et migration vides ;
- `AccountSessionReadiness.OfficialPersistenceClaimAllowed = false`.

Le scan haute confiance sur rapports, sources, configuration, preuves JSON et TRX ne trouve :

- aucune cle privee ;
- aucun mot de passe de chaine SQL ;
- aucun `User ID` renseigne ;
- aucun bearer token ;
- aucune cle ou jeton non vide.

Les occurrences textuelles restantes sont un mot de passe synthetique de test, les proprietes de validation `UserID`/`Password` et les noms de duree de tokens. Elles ne sont pas des secrets.

Aucune commande distante, connexion staging, activation SQL, migration distante ou modification du rapport producteur n'a ete effectuee par QA-B.

## Nettoyage

Apres les matrices et la sonde :

- bases `BeeKingdom_Local_SERVERB057_%` : 0 ;
- fichiers MDF/LDF/BAK correspondants dans `%TEMP%` : 0 ;
- fichiers correspondants sous `Server` : 0 ;
- sessions `BeeKingdom.SERVER-B-061.Tests` : 0 ;
- sessions `BeeKingdom.QA.SERVERB061` : 0 ;
- processus `dotnet/testhost` Bee Kingdom actifs : 0 ;
- instance `MSSQLLocalDB` finale : arretee.

La sonde de panne de release ne cree aucune base ni aucun fichier.

## Preuves et empreintes

- Rapport producteur SERVER-B-061 : `D3264ED1DA1E7E0D7F67AD235966E69824EC14CCDA86B2323282680C887E2F20`
- Runner inspecte : `7E38297B967FBCADBF41023FB5E64FC02618A18947AFC3B963D67DCB0BCE3949`
- Tests SQL inspectes : `27609DE2DE64DEB03C0499F44EC91B8F79A5CAC17B928E30CAF1F3DB53CC3891`
- Matrice SQL TRX : `989A10912BCE00EAFA05EEC90CB80E8ACC3D564419CCB1D4DAE89CD557B733EA`
- Suite Release TRX : `488026153ECBC84FB6FEC0388C652410A9B7232F05260931FD947627D8D41EC1`
- Sonde lifecycle JSON : `309AF71CF4F1D69B5AFCE29DBD8DAA73029B20C581884ADFDDAA38CC632AD700`
- Controle environnement LocalDB JSON : `2CC4CAE1F638E10B7083B2F64F4711546710CAFC92AF0CFB1496759103EAAD7C`

Fichiers de preuve :

- `C:\projets\beekingdomgame-master\Docs\QA\Evidence\SERVER-B-061\QA_B_SERVER_B_061_SQL_MATRIX.trx`
- `C:\projets\beekingdomgame-master\Docs\QA\Evidence\SERVER-B-061\QA_B_SERVER_B_061_RELEASE_FULL.trx`
- `C:\projets\beekingdomgame-master\Docs\QA\Evidence\SERVER-B-061\QA_B_SERVER_B_061_APPLOCK_LIFECYCLE_PROBE.json`
- `C:\projets\beekingdomgame-master\Docs\QA\Evidence\SERVER-B-061\QA_B_SERVER_B_061_LOCALDB_ENVIRONMENT_CHECK.json`
- `C:\projets\beekingdomgame-master\Docs\QA\Probes\ServerB061SqlAppLockLifecycle\`

## Limites

- Validation LocalDB sous une identite Windows locale uniquement.
- Pas de preuve de latence, charge, haute disponibilite ou droits staging.
- Aucun claim de persistance officielle, monde live, comptes live ou sessions live.
- Le verdict final officiel et toute decision de staging restent a QA-A/Architecte.

QA_SERVER_B_061_SQL_APPLOCK_POOLING_LIFECYCLE = PASS
READY_FOR_QA_A_SQL_PERSISTENCE_READINESS = YES
