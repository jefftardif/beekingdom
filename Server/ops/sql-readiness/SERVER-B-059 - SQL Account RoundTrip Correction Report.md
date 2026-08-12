# SERVER-B-059 - SQL Account RoundTrip Correction Report

Date locale : 2026-07-13 21:59:09 -04:00  
Date UTC : 2026-07-14 01:59:09 UTC  
Portee : poste local uniquement, SQL Server LocalDB jetable  
Statut : correction et validation terminees, aucune activation staging/live

## 1. Objectif

Corriger la relecture SQL de `AccountProgression` sans deserialiser directement le JSON vers les contrats domaine exposes en `IReadOnlySet<string>` et `IReadOnlyCollection<string>`.

La correction devait conserver l'immutabilite publique du domaine, couvrir les cas de compatibilite et d'erreur, puis remettre au vert la matrice SQL locale de six scenarios et la suite .NET Release complete.

## 2. Cause racine

`SqlAccountRepository` serialisait puis deserialisait directement `AccountProgression` avec `System.Text.Json`.

La lecture echouait sur les proprietes de type `IReadOnlySet<string>` : le serializer ne disposait pas d'un type concret fiable a construire pour ce contrat domaine. Le format SQL et le modele domaine etaient ainsi couples au comportement implicite du serializer.

## 3. Correction appliquee

### Mapping SQL explicite

`SqlAccountRepository` utilise maintenant un DTO prive `AccountProgressionSqlDto` dont les collections sont concretes :

- `List<string>` pour les ensembles et historiques ;
- `Dictionary<string, double>` pour les statistiques ;
- constructeur sans parametre pour la lecture JSON ;
- constructeur explicite pour l'ecriture SQL.

La deserialisation suit desormais ce chemin :

1. JSON SQL vers DTO prive ;
2. validation explicite des champs et valeurs nulles ;
3. conversion des ensembles vers `HashSet<string>` avec `StringComparer.Ordinal` ;
4. copie des historiques vers des listes preservees dans leur ordre ;
5. construction explicite de `AccountProgression`.

La serialisation suit le chemin inverse. Les ensembles sont dedoublonnes et tries avec `StringComparer.Ordinal`; les statistiques sont ordonnees par cle avant ecriture. Le JSON persiste est donc deterministe pour ces structures non ordonnees.

### Contrats domaine preserves

Aucune propriete de `AccountProgression` n'a ete affaiblie ou remplacee. Les contrats `IReadOnlySet<string>` et `IReadOnlyCollection<string>` restent inchanges. Le DTO est prive au repository SQL et ne fuit pas dans le domaine.

### Traitement des donnees anciennes ou invalides

- JSON ancien avec noms de proprietes PascalCase : accepte par les options JSON existantes ;
- propriete historique absente : valeur vide par defaut ;
- doublons dans les champs d'ensemble : normalises par comparaison ordinale ;
- doublons dans les historiques : conserves, car ces champs sont ordonnes et ne sont pas des ensembles ;
- type JSON incorrect : `InvalidDataException` explicite avec `JsonException` interne ;
- champ collection explicitement `null` : `InvalidDataException` ciblant le champ ;
- element `null` dans une collection texte : `InvalidDataException` ciblant le champ.

## 4. Durcissement de concurrence decouvert pendant la validation

Le premier rejeu complet apres la correction du round-trip a revele une course preexistante lors de la creation initiale simultanee d'une base absente. Plusieurs executeurs pouvaient observer l'absence de la base puis lancer `CREATE DATABASE` en parallele.

`SqlServerMigrationRunner` acquiert maintenant un verrou de session SQL `sp_getapplock` sur `BeeKingdom.Database.Create:<databaseName>` depuis la connexion `master` avant le test et la creation de la base. Le verrou de migration existant reutilise le meme helper de verrou de session.

Le scenario de concurrence a ensuite passe trois executions ciblees consecutives, puis la matrice canonique complete.

## 5. Fichiers modifies

1. `Server/src/BeeKingdom.Accounts/Repositories/SqlAccountRepository.cs`
2. `Server/src/BeeKingdom.Persistence/Migrations/SqlServerMigrationRunner.cs`
3. `Server/tests/BeeKingdom.Tests/SqlServerOptInIntegrationTests.cs`

Aucun fichier de migration, fichier `appsettings`, script de deploiement, fichier Unity ou composant distant n'a ete modifie.

## 6. Tests AccountProgression

Le scenario SQL `SqlServerRepositoryRoundTripsSyntheticAccountProgression` couvre dans une base jetable :

- ensemble vide ;
- une valeur ;
- plusieurs valeurs ;
- doublons d'ensembles normalises ;
- ordre d'ecriture deterministe apres sauvegarde ;
- historique ordonne avec doublons preserves ;
- JSON ancien PascalCase ;
- propriete ancienne manquante mappee vers une collection vide ;
- valeur de type JSON incorrect ;
- champ collection explicitement `null`.

Resultat cible : **PASS, 1/1**.

## 7. Matrice SQL locale canonique

Commande :

```powershell
.\Server\ops\sql-readiness\Invoke-LocalSqlReadiness.ps1 -NoRestore
```

Resultat : **23 tests passes, 0 echec**, dont **6/6 scenarios SQL opt-in passes** :

1. `SqlServerBackupCanBeVerifiedAndRestoredToDisposableDatabase`
2. `SqlServerCreatesDisposableDatabaseAndAppliesMigrationsIdempotently`
3. `SqlServerRepositoryRoundTripsSyntheticAccountProgression`
4. `SqlServerSerializesMigrationsAndRejectsConcurrentDuplicateAccount`
5. `SqlServerStoresSyntheticCredentialSessionAndWorldScopedColonies`
6. `WorldSchemaReadinessDraftExecutesAndRollsBackLocally`

Preuves confirmees par cette matrice :

- creation initiale et migrations : PASS ;
- second passage idempotent, aucune migration en attente : PASS ;
- comptes, progression, identifiants et donnees synthetiques : PASS ;
- sessions, colonie et scope monde : PASS ;
- concurrence de creation/migration et unicite compte : PASS ;
- schema monde preparatoire execute puis annule : PASS ;
- backup, verification et restauration jetable : PASS.

## 8. Suite .NET Release complete

Commande :

```powershell
Remove-Item Env:BEE_SQL_INTEGRATION_CONNECTION_STRING -ErrorAction SilentlyContinue
dotnet test .\Server\BeeKingdom.Server.slnx --configuration Release --no-restore --logger "console;verbosity=minimal"
```

Resultat : **142 passes, 0 echec, 6 ignores, 148 total**.

Les six tests ignores sont exactement les six scenarios SQL opt-in listes ci-dessus. Leur omission dans la suite standard est attendue lorsque `BEE_SQL_INTEGRATION_CONNECTION_STRING` est absent. Ils ont tous ete executes et passes dans la matrice LocalDB dediee.

## 9. Nettoyage et controles de securite

Apres la derniere matrice :

- bases `BeeKingdom_Local_SERVERB057_%` restantes dans LocalDB : **0** ;
- fichiers LocalDB correspondants dans `%TEMP%` ou sous `Server` : **0** ;
- provider de `Server/src/BeeKingdom.Server/appsettings.Production.json` : **InMemory** ;
- chaine runtime SQL dans ce fichier : absente ;
- chaine migration SQL dans ce fichier : absente ;
- adresse du serveur distant dans les fichiers modifies : absente ;
- secret ou identifiant reel ajoute : aucun.

Le scan lexical des trois fichiers modifies ne remonte que les noms de configuration de test `Authentication:AccessTokenLifetime` et `Authentication:RefreshTokenLifetime`. Ce sont des durees synthetiques, pas des jetons ni des secrets.

## 10. Limites et interpretation

- Les validations SQL ont ete effectuees sur SQL Server LocalDB sous une seule identite Windows locale.
- Elles ne constituent pas une preuve de charge, de latence reseau, de droits de service, de bascule ou de restauration sur l'infrastructure staging.
- La normalisation des ensembles est ordinale et sensible a la casse. Elle supprime les doublons exacts uniquement.
- Les historiques conservent volontairement ordre et doublons.
- Une propriete absente reste compatible et devient vide ; une propriete presente avec `null` ou un type invalide est rejetee explicitement.
- Le verdict autorise seulement une future decision de vague staging persistante privee. SQL n'est active ni en staging ni en production par ce travail.

## 11. Conclusion

Le blocage immediat de SERVER-B-057 est ferme localement. Le round-trip SQL de `AccountProgression` ne depend plus de la construction implicite des interfaces domaine, les six scenarios SQL passent, la suite Release ne regresse pas, le backup/restore et la concurrence restent valides, et aucun residu LocalDB ne subsiste.

READY_FOR_SQL_PERSISTENCE_STAGING_WAVE2 = YES
