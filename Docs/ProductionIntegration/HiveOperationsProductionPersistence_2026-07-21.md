# Persistance de production des operations de ruche - tranche 1

## Resultat

La tranche isolee `BeeKingdom.HiveOperations` fournit une frontiere serveur autoritaire pour une amelioration de batiment avec debit, echeance UTC, resultat en attente de collecte manuelle et reclamation idempotente. Aucun fichier LivingHive, terrain, tutoriel, animation ou chat n'est modifie.

## Autorite

- Le client transmet uniquement l'identite, la cible, la revision attendue et une cle d'idempotence.
- Le serveur choisit cout, duree et rendement depuis son catalogue.
- `IServerClock` est la seule horloge utilisee.
- Le debit et l'insertion de l'operation sont executes sous le meme verrou de depot.
- Une echeance passee transforme l'operation en `AwaitingCollection`; aucun gain n'est alors credite.
- La collecte applique la capacite, credite une seule fois, monte le niveau et produit un recu idempotent.
- Une revision optimiste rejette les commandes concurrentes ou obsoletes.
- `ModelVersion` prepare les migrations de sauvegarde.

## Contrat d'integration LivingHive

LivingHive devra appeler un adaptateur de transport, jamais le depot directement:

1. `ReadAsync(playerId, hiveId)` pour obtenir ressources, capacites, niveaux, revision et operations.
2. `StartAsync(StartBuildingOperationCommand)` avec la revision lue et une cle d'idempotence stable pour toutes les nouvelles tentatives du meme clic.
3. Afficher `Running` avec `CompletesAtUtc` selon l'heure serveur, puis `AwaitingCollection` sans modifier le solde local.
4. `CollectAsync(CollectBuildingOperationCommand)` avec l'identifiant d'operation, la revision courante et une nouvelle cle stable de collecte.
5. Remplacer le cache Unity par l'etat retourne; ne jamais rejouer localement cout, gain ou niveau.

Codes principaux: `started`, `collected`, `collected_capacity_limited`, `revision_conflict`, `building_busy`, `insufficient_resources`, `not_ready`, `already_collected`, `storage_full`, `idempotency_conflict`.

## Persistance

`DurableJsonHiveStateRepository` est l'adaptateur durable local de reference pour les tests de redemarrage et la demo degradee. Il ecrit par remplacement atomique et ne doit pas etre presente comme base de production multi-instance.

La migration `070_hive_operations.sql` prepare les tables SQL Server versionnees pour etat, recus et file. L'adaptateur SQL transactionnel reste le prochain gate avant activation d'un endpoint officiel; il devra utiliser une transaction SQL et un verrou par `(PlayerId, HiveId)` ou une mise a jour conditionnelle de revision.

## Exploitation et securite

- Aucun secret ou chaine de connexion n'est stocke.
- Les identifiants de correlation devront reutiliser la cle d'idempotence au transport, sans journaliser sa valeur brute.
- Les metriques requises: commandes acceptees/rejetees, conflits de revision, doublons, operations dues, latence de transaction et taux de stockage plein.
- La migration doit suivre le runbook de sauvegarde/restauration existant et ne doit pas etre appliquee automatiquement en production.

## Couverture automatisee

Les tests utilisent une horloge controlee et couvrent reprise apres fermeture, fin hors ligne, collecte/retry sans double gain, commandes concurrentes, capacite presque pleine, ressources insuffisantes et independance vis-a-vis de l'horloge client. Les prochains tests a ajouter avec l'adaptateur SQL couvrent redemarrage SQL en transaction, migration d'un ancien `ModelVersion`, progression tutoriel et recompenses, puis formation/production.

## Fichiers de la tranche

Crees:

- `Server/src/BeeKingdom.HiveOperations/BeeKingdom.HiveOperations.csproj`
- `Server/src/BeeKingdom.HiveOperations/HiveOperationModels.cs`
- `Server/src/BeeKingdom.HiveOperations/HiveOperationService.cs`
- `Server/src/BeeKingdom.HiveOperations/DurableJsonHiveStateRepository.cs`
- `Server/tests/BeeKingdom.HiveOperations.Tests/BeeKingdom.HiveOperations.Tests.csproj`
- `Server/tests/BeeKingdom.HiveOperations.Tests/HiveOperationServiceTests.cs`
- `Server/src/BeeKingdom.Database/Scripts/070_hive_operations.sql`
- `Docs/ProductionIntegration/HiveOperationsProductionPersistence_2026-07-21.md`

Modifies:

- `Server/BeeKingdom.Server.slnx`
- `Server/src/BeeKingdom.Database/DatabaseCatalog.cs`

## Validation locale

- compilation Release de `BeeKingdom.HiveOperations`: 0 erreur, 0 avertissement;
- tests Release: 4/4 reussis avec horloge controlee;
- aucune synchronisation finale demandee ou requise pour la livraison locale;
- aucun fichier sous `Assets/` modifie.

## Points d'integration et gates

- LivingHive doit recevoir un adaptateur HTTP/transport vers les trois operations decrites plus haut; aucune integration directe n'a ete faite pendant le travail parallele de l'Architecte.
- Le depot JSON est une preuve durable locale et un cache degrade, pas l'autorite multi-instance finale.
- Avant activation officielle: implementer le depot SQL transactionnel, exposer des endpoints authentifies scopes au joueur, appliquer la migration via le runbook, puis ajouter les tests SQL de redemarrage et de migration.
- Les recompenses et checkpoints de tutoriel restent hors de cette tranche et ne sont ni relus ni accordes silencieusement.

## Extension SQL transactionnelle

La seconde passe locale ajoute `SqlHiveStateRepository`, sans activation ni deploiement live. Chaque commande ouvre une transaction `Serializable`, prend un `sp_getapplock` exclusif scope par `(PlayerId, HiveId)`, relit la ligne avec `UPDLOCK,HOLDLOCK`, applique la commande puis persiste le JSON versionne avant commit. Une panne avant commit ne peut donc laisser un debit sans operation ni un gain sans recu.

Le fournisseur recoit sa chaine de connexion par injection. Aucun hote, utilisateur ou secret de production n'est code dans le module. Les commandes SQL sont parametrees et leur delai est borne. La migration `070_hive_operations.sql` reste non appliquee; le serveur de production n'a recu aucune ecriture.

## Modele v2

`HiveStateMigrator` migre un etat v1 vers v2 en initialisant les collections nouvelles sans rejouer de cout ou de gain. Le modele v2 ajoute:

- checkpoint de tutoriel avec chapitre, etape sure de reprise et derniere etape observee;
- recompenses persistantes en attente ou reclamees;
- reclamation atomique et idempotente avec limite de capacite;
- compteurs diagnostics acceptations, rejets, replays idempotents et conflits de revision.

## Contrats supplementaires pour Architecte

- `SaveTutorialProgressAsync`: memoriser l'etape observee mais fournir explicitement une `SafeResumeStepKey`; LivingHive reprend uniquement cette frontiere sure.
- `ClaimRewardAsync`: reclamer une recompense serveur existante; le client ne transmet ni montant ni ressource.
- `ReadAsync`: retourne maintenant `ModelVersion`, `Tutorial` et `Rewards` en plus des ressources, niveaux et files.

Pour chaque nouvelle tentative reseau, LivingHive doit reutiliser la meme cle d'idempotence et remplacer son cache par l'etat retourne. Un `revision_conflict` impose une nouvelle lecture avant toute autre commande.

## Rollback et exploitation

Le rollback `070_hive_operations.rollback.sql` supprime dans l'ordre file, recus puis etat. Il est destructif et ne doit etre execute qu'apres sauvegarde, fenetre de maintenance et validation explicite. En exploitation normale, une evolution du modele utilise une nouvelle migration; le rollback n'est pas un mecanisme de downgrade de sauvegardes joueur.

Les identifiants d'idempotence ne doivent pas etre journalises en clair. Le transport devra propager un identifiant de correlation distinct et exposer une sante SQL sans details de connexion. Les compteurs du module sont prets a etre raccordes au systeme de metriques du serveur.

## Validation finale locale

- compilation Release `BeeKingdom.HiveOperations`: 0 erreur, 0 avertissement;
- tests Release: 8/8 reussis sans attente reelle;
- scenarios ajoutes: retry apres delai reseau, conflit de payload idempotent, migration v1 vers v2, reprise sure du tutoriel et recompense deja reclamee;
- migration et rollback inspectes mais non appliques;
- aucun endpoint, deploiement, secret, fichier Unity ou chat modifie;
- aucune synchronisation lancee.

## Fichiers exacts apres extension

Nouveaux dans cette passe:

- `Server/src/BeeKingdom.HiveOperations/SqlHiveStateRepository.cs`
- `Server/src/BeeKingdom.HiveOperations/HiveStateMigrator.cs`
- `Server/src/BeeKingdom.HiveOperations/HiveOperationDiagnostics.cs`
- `Server/src/BeeKingdom.Database/Scripts/070_hive_operations.rollback.sql`

Modifies dans cette passe:

- `Server/src/BeeKingdom.HiveOperations/BeeKingdom.HiveOperations.csproj`
- `Server/src/BeeKingdom.HiveOperations/HiveOperationModels.cs`
- `Server/src/BeeKingdom.HiveOperations/HiveOperationService.cs`
- `Server/tests/BeeKingdom.HiveOperations.Tests/HiveOperationServiceTests.cs`
- `Server/src/BeeKingdom.Database/DatabaseCatalog.cs`
- `Server/src/BeeKingdom.Database/DatabaseRollbackCatalog.cs`
- `Docs/ProductionIntegration/HiveOperationsProductionPersistence_2026-07-21.md`

## Risques et gates restants

- Les projets historiques references par `BeeKingdom.Server.slnx` n'ont pas leurs `.csproj` sources dans cette copie locale; seule la nouvelle tranche autonome peut etre compilee ici. Leur restauration est necessaire avant validation de solution complete.
- Le depot SQL doit recevoir un test d'integration sur une base ephemere ou staging apres application controlee de la migration 070.
- Les endpoints authentifies ne sont pas ajoutes tant que le raccordement a `AuthenticationSessionValidator`, le scope PlayerId et la selection de fournisseur ne peuvent pas etre compiles et testes ensemble.
- Formation et production reutiliseront le meme aggregate et le meme protocole atomique dans la prochaine tranche.
- Aucun changement central n'est demande a `Communication`; le module chat reste intact.

## Extension files de formation et production

Le modele courant passe en version 3 et distingue maintenant trois types de file: `BuildingUpgrade`, `Training` et `Production`. Les anciens enregistrements sans type restent interpretes comme ameliorations de batiment, ce qui maintient la compatibilite de lecture.

`StartQueuedOperationAsync` recoit uniquement une `OperationKey`, la revision attendue et la cle d'idempotence. Le serveur retrouve dans son catalogue la cible, le cout, la duree et le resultat. Une seule operation non collectee de chaque type peut occuper sa file. Le debit et l'insertion restent dans la meme transaction du depot.

La formation et la production suivent la meme politique que les batiments:

- echeance calculee par l'horloge UTC serveur;
- reprise apres redemarrage depuis les horodatages persistants;
- passage a `AwaitingCollection` sans credit automatique;
- collecte manuelle idempotente;
- capacite appliquee au resultat;
- aucune modification de niveau de batiment pour une formation ou une production.

Deux tests supplementaires valident une formation d'ouvrieres survivant au redemarrage et une production de cire limitee par la capacite. La suite finale contient 10/10 tests reussis.

Fichiers modifies dans cette extension:

- `Server/src/BeeKingdom.HiveOperations/HiveOperationModels.cs`
- `Server/src/BeeKingdom.HiveOperations/HiveOperationService.cs`
- `Server/src/BeeKingdom.HiveOperations/HiveStateMigrator.cs`
- `Server/tests/BeeKingdom.HiveOperations.Tests/HiveOperationServiceTests.cs`
- `Docs/ProductionIntegration/HiveOperationsProductionPersistence_2026-07-21.md`

Contrat Architecte ajoute: LivingHive peut utiliser `StartQueuedOperationAsync` pour les catalogues de formation et production, afficher `Kind`, `BuildingKey` comme cible actuelle, `CompletesAtUtc` et `Status`, puis appeler le meme `CollectAsync` avec l'identifiant d'operation. Le futur contrat de transport renommera la cible en `TargetKey` sans exposer les montants de catalogue au client.

Aucun changement central n'est demande a `Communication`. Aucun endpoint, deploiement, migration live ou synchronisation n'a ete effectue.
