# BeeKingdom - Integration officielle Nurserie (14 octobre 2026)

## Statut

- Depot local : C:\projets\beekingdomgame-master
- Branche officielle : main
- Avant integration : 5004c7d9
- Apres integration : b6656062
- Commits reportes depuis bkstudio/nursery-growth-20261014 :
  - 8f6aafd2 - regles de croissance (origine e78dc6df)
  - b02c7cb8 - persistance et routes authentifiees (origine 66e87665)
  - b6656062 - rapport QA historique (origine 0ea99ec0)
- Integration effectuee par fast-forward, sans fusion du reste de la branche BK Studio.
- Conflit sur HiveOperationModels.cs resolu en conservant la structure de main :
  seul NurseryCohortState? est ajoute, sans importer RoyalReserveState du chantier Banque.
- Aucun deploiement IIS, aucun push GitHub lors de cette operation.

## Perimetre fonctionnel

Les routes GET /game/v1/hives/{hiveId}/brood/cohorts et
POST /game/v1/hives/{hiveId}/brood/cohorts/start sont ajoutees et authentifiees.
Le flag NurseryCohorts.Enabled est explicitement false dans les deux appsettings;
par defaut dans le code, l'option vaut egalement false.
La croissance produit des recrues dans une reserve distincte, pas dans la Caserne.
La lecture et le demarrage emploient les donnees de la ruche du joueur authentifie.
Une maturation terminee est reglee atomiquement et ne doit pas etre creditee deux fois.

## Preuves locales sur le portage dans main

- BeeKingdom.Server.csproj : compilation OK, 0 erreur, 0 avertissement.
- Tests metier cibles Nurserie : 50/50 reussis.
- Tests HTTP cibles Nurserie : 10/10 reussis.
- Suite HiveOperations complete : 280 reussis, 2 echoues, 0 ignores (282).
  Les deux echecs WorldResourceCollectionServiceTests ont ete reproduits sur
  le commit de depart 5004c7d9, dans un worktree distinct :
  Claim_applies_daily_focus_bonus_only_to_the_featured_node ;
  Claim_applies_world_event_yield_bonus_only_to_the_matching_resource.
- Suite HTTP complete : 659 reussis, 3 echoues, 9 ignores (671).
  Les trois echecs correspondent aux anomalies connues :
  CatalogSqlMatchesCheckedInScriptFiles ;
  DurableJsonRoundTripPreservesPendingRevisionReceiptAndExactReplay ;
  MigratorV9WithoutOfflineBlockBecomesV10WithNullBlock.
  Ces memes noms figurent dans le rapport de base du travail specialise.
- Trace de la certification :
  C:\BeeKingdom\DEV\BK-Studio\reports\nursery-official-20261014

## Conditions non encore certifiees

- Compilation et tests EditMode Unity : BLOQUES par l'echec au lancement
  du processus Unity Package Manager (IPC Upm) en mode batch.
  Le code reseau Unity a ete extrait de la sauvegarde pour examen puis remis
  en attente; il n'a pas ete integre dans main.
- Interface premium Couvées : pas de raccordement Unity dans le portage officiel.
- Integration SQL de staging et suppression concurrente de ruche : a certifier
  avant toute activation du flag en production.
- Compatibilite Caserne/reserve : a concevoir et tester sans creation de troupes fictives.
- Tests fonctionnels Unity en Play : a effectuer apres reparation de l'environnement.

## Sauvegardes et regles

- Stash : BKSTUDIO-SAFETY-20261014-before-git-reconciliation
- Branches backup/bkstudio-20261014-working-tree,
  backup/bkstudio-20261014-index et backup/bkstudio-20261014-untracked conservees.
- Le client Unity NurseryCohortClient.cs et ses tests Unity sont recuperables
  dans backup/bkstudio-20261014-untracked.
- Les worktrees des agents desactives ne sont ni supprimes ni fusionnes ici.
- Ne pas activer NurseryCohorts.Enabled ni deplacer la reserve vers l'armee
  avant certification de bout en bout.

## Etape suivante

Reparer Unity Package Manager, valider les tests du client reseau, integrer
seulement les composants UI necessaires depuis les sauvegardes, verifier le
jeu en Play, faire les essais SQL en staging et effectuer ensuite un
deploiement serveur sauvegarde avec health check et retour arriere.
