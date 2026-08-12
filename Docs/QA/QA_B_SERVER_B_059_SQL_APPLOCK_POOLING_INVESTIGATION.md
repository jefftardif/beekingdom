# QA-B - Contre-preuve ciblee SERVER-B-059

## SQL Session AppLock et connection pooling

- Date de l'enquete: 2026-07-13/14 (America/Toronto)
- Role: QA-B, prevalidation independante pour QA-A/Architecte
- Portee: poste local uniquement, SQL Server LocalDB jetable
- Interdits respectes: aucun serveur distant, aucun reseau distant, aucune modification du code producteur
- Gate officiel: non ferme par QA-B

## Verdict executif

Le risque signale par QA-A est **reproduit** avec `Microsoft.Data.SqlClient 5.2.2` et le pooling par defaut.

Apres acquisition d'un `sp_getapplock` avec `LockOwner=Session`, la fermeture logique de la connexion rend la connexion physique au pool sans liberer immediatement le verrou. Avant toute reutilisation du pool proprietaire, une seconde pool independante ne peut pas obtenir le verrou: resultat `-1` apres 1 207 ms. La reutilisation ulterieure de la meme connexion physique (meme SPID) reinitialise la session et le verrou devient `NoLock`, ce qui explique pourquoi une verification effectuee apres cette reutilisation masque le defaut.

La conception SERVER-B-059 sans `sp_releaseapplock` explicite est donc bloquante pour une vague SQL concurrente ou multi-instance. Le risque principal est un echec de disponibilite/demarrage ou de migration par timeout, pas une preuve de corruption de donnees.

## Sources inspectees

1. `C:\projets\beekingdom\prompt_server\rapports\SERVER-B-059 - SQL Account RoundTrip Correction Report.md`
2. `C:\projets\beekingdomgame-master\Server\src\BeeKingdom.Persistence\Migrations\SqlServerMigrationRunner.cs`
3. `C:\projets\beekingdomgame-master\Server\tests\BeeKingdom.Tests\SqlServerOptInIntegrationTests.cs`
4. `C:\projets\beekingdomgame-master\Server\src\BeeKingdom.Persistence\BeeKingdom.Persistence.csproj`

Le rapport SERVER-B-059 decrit l'acquisition des verrous de creation de base et de migration en mode `Session`. Le projet fixe le pilote a `Microsoft.Data.SqlClient 5.2.2`.

## Derive d'etat observee

La source visible a evolue pendant l'enquete, sans intervention de QA-B:

- `SqlServerMigrationRunner.cs`, horodatage UTC `2026-07-14T02:58:27.8891775Z`, contient maintenant `ExecuteWithSessionLockAsync`, une liberation explicite en `finally`, puis l'elimination de la connexion du pool en cas d'echec d'acquisition ou de release (lignes 135-263);
- les tests visibles, horodatage UTC `2026-07-14T03:04:23.0768556Z`, imposent maintenant `Pooling=true` et utilisent l'identifiant `BeeKingdom.SERVER-B-061.Tests` (lignes 64-79), avec des controles de release, exception et annulation (lignes 337-368).

Cet etat plus recent constitue un correctif candidat distinct. Il ne retrovalide pas le livrable SERVER-B-059 et devra rester rattache a sa propre vague de correction.

## Methode de reproduction

Une sonde QA isolee reference le projet `BeeKingdom.Persistence` et utilise exactement l'assembly charge par le projet:

- assembly: `Microsoft.Data.SqlClient`;
- version package/informationnelle: `5.2.2`;
- cible: `(localdb)\MSSQLLocalDB` uniquement;
- authentification integree, aucun secret;
- `Pooling` omis de la chaine principale, valeur effective verifiee a `true`;
- deux `Application Name` differents afin de forcer deux pools independantes.

Ordre critique du scenario principal:

1. Ouvrir la connexion proprietaire et acquerir un verrou exclusif `Session`.
2. Fermer/disposer la connexion proprietaire, donc la rendre au pool.
3. Sans reprendre la pool proprietaire, tenter l'acquisition depuis une seconde pool avec un timeout de 1 200 ms.
4. Reprendre ensuite la connexion proprietaire et verifier le SPID et `APPLOCK_MODE`.
5. Reessayer depuis la seconde pool apres nettoyage.

## Resultats experimentaux

| Scenario | Resultat | Conclusion |
|---|---:|---|
| Pooling omis dans la chaine | effectif `true` | Comportement par defaut confirme |
| Acquisition initiale | `0`, mode `Exclusive` | Verrou acquis |
| Seconde pool avant reutilisation proprietaire | `-1` apres 1 207 ms | Blocage reproduit |
| Reutilisation pool proprietaire | SPID `77` puis `77` | Meme session physique reprise |
| Mode apres reutilisation proprietaire | `NoLock` | Reset tardif lors de la reprise |
| Seconde pool apres nettoyage | `0`, attente 0 ms | Verrou redevenu disponible |
| Controle `Pooling=false` | acquisition `0`, attente 0 ms | Aucun verrou residuel apres fermeture physique |
| Controle release explicite | release `0`, acquisition concurrente `0` | Correctif minimal efficace |

Le point determinant est le test **avant** reutilisation de la connexion proprietaire. Tester uniquement apres `Open` sur la meme pool provoque la reinitialisation qui cache la fuite temporaire.

## Impacts SERVER-B-059

### Creation de base

Une instance qui termine `EnsureDatabaseAsync` sans release explicite peut conserver `BeeKingdom.Database.Create:<database>` sur une connexion physique inactive du pool. Une autre instance ou un autre processus dispose de sa propre pool et peut expirer avant d'entrer dans la section de creation.

### Migrations concurrentes

Le meme mecanisme s'applique a `BeeKingdom.Database.Migrations`. Le verrou peut survivre a la fin logique d'une migration jusqu'a la reprise, l'expiration ou la destruction de la connexion physique. Une seconde instance peut alors echouer par timeout/erreur `51057` alors qu'aucune migration n'est encore active.

### Effet masquant des tests

`Pooling=false` ferme la connexion physique et libere le verrou, donc ce mode ne couvre pas le risque de production par defaut. Une verification qui reprend d'abord la pool proprietaire masque egalement le risque en provoquant le reset de session.

### Severite QA-B

- P1 bloquant pour activation SQL concurrente, multi-processus ou multi-instance.
- Pas de P0 de perte de donnees demontre dans cette enquete.
- Aucun claim staging/live n'est autorise par cette preuve.

## Correctif minimal attendu de Server-B

1. Encadrer chaque acquisition par un `try/finally` et appeler `sys.sp_releaseapplock` avec le meme `Resource`, `LockOwner=Session` et la meme connexion avant son retour au pool.
2. Executer la release avec un token non annule afin qu'une annulation de l'operation ne saute pas le nettoyage.
3. Preserver l'exception metier originale; si acquisition ou release echoue, invalider/vider la connexion concernee avant sa reutilisation.
4. Ajouter un test `Pooling=true` avec deux pools independantes qui tente l'acquisition concurrente **avant** toute reprise de la pool proprietaire.
5. Couvrir succes, exception, annulation, creation de base et verrou de migration.

L'etat source courant semble mettre en oeuvre ces points. QA-B n'a applique aucune correction et ne prononce pas ici le verdict officiel de cette vague plus recente.

## Controle de l'etat courant

Le test cible actuel `SqlServerSerializesMigrationsAndRejectsConcurrentDuplicateAccount` a ete execute localement:

- total: 1;
- passe: 1;
- echec: 0;
- ignore: 0.

La sonde du runner courant observe egalement:

- verrous creation et migration apres retour: `NoLock`;
- seconde pool: succes avant nettoyage global;
- trois essais de creation concurrente: 2/2 succes chacun, aucune erreur `51057`;
- retry apres nettoyage: succes.

Ces resultats soutiennent le correctif candidat courant, sans annuler la contre-preuve de SERVER-B-059.

## Nettoyage et absence de residu

La sonde a enregistre quatre bases jetables et huit chemins de fichiers:

- bases QA restantes: `0`;
- fichiers MDF/LDF restants: `0`;
- sessions QA restantes apres purge des pools: `0`;
- erreurs de nettoyage: `0`.

Un controle SQL independant apres la sonde et le test cible confirme egalement:

- bases `BeeKingdom_QA_SERVERB059_%`: `0`;
- bases `BeeKingdom_Local_SERVERB057_%`: `0`;
- sessions des deux outils: `0`;
- fichiers correspondants dans le workspace et `%TEMP%`: `0`.

L'instance `MSSQLLocalDB`, initialement arretee, a ete remise a l'etat **Arrete**. Aucun appel distant n'a ete effectue.

## Preuves QA

- JSON de reproduction: `C:\projets\beekingdomgame-master\Docs\QA\QA_B_SERVER_B_059_SQL_APPLOCK_POOLING_EVIDENCE.json`
  - SHA-256: `7D026A1B32DBF7080980852599758312CD660D1BAC1B85E4E69910EF60E32541`
- Resultat TRX du runner courant: `C:\projets\beekingdomgame-master\Docs\QA\QA_B_SERVER_B_059_CURRENT_RUNNER_TARGETED_TEST.trx`
  - SHA-256: `F7D3BF7375A0D08834D0627E807809B7F076BCB01B9C06FA6A1A1F51C2C553DB`
- Sonde reproductible: `C:\projets\beekingdomgame-master\Docs\QA\Probes\ServerB059SqlAppLockPooling\`
- SHA-256 du runner courant inspecte: `7E38297B967FBCADBF41023FB5E64FC02618A18947AFC3B963D67DCB0BCE3949`
- SHA-256 des tests courants inspectes: `27609DE2DE64DEB03C0499F44EC91B8F79A5CAC17B928E30CAF1F3DB53CC3891`

## Non-claims

- Cette enquete ne prouve aucun service staging ou live.
- Elle ne couvre pas un SQL Server distant, la latence reseau, les identites de service ou la charge.
- Elle ne remplace pas la validation finale de QA-A/Architecte.
- QA-B confirme uniquement la reproduction locale du risque de pooling de SERVER-B-059 et documente le correctif candidat observe ensuite.

QA_B_SERVER_B_059_SQL_APPLOCK_POOLING = CONFIRMED_BLOCKER
