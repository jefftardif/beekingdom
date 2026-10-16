# M095-CG — Construction / Upgrade alpha

## Cause racine

Le rejeu idempotent d'un SpeedUp accepté renvoyait `Succeeded=true` sans `Response` : la route HTTP retournait un corps vide, refusé par le client qui exige un reçu. Risque confirmé de reprise incohérente après perte de réponse ; test reproduit en échec avant correction.

## Fichiers changés

- `Server/src/BeeKingdom.HiveOperations/SpeedUpContracts.cs`
- `Server/tests/BeeKingdom.HiveOperations.Tests/SpeedUpInventoryServiceTests.cs`
- Ce rapport.

## Implémentation

Le rejeu restitue le reçu original (révision et date conservées) avec le snapshot courant, sans nouvelle consommation ni réduction. Seul ce défaut est corrigé ; conflits de clé et refus restent des refus, sans changement de schéma ni modification des périmètres exclus.

## Tests / compilation

28/28 tests serveur ciblés réussis : `SpeedUpInventoryServiceTests` + `BuildingUpgradeDailyRoundTests` (6), `BuildingUpgradeServiceTests` + `BuildingUpgradeEndpointTests` + `RoyalPalaceProgressionTests` (22), via `dotnet test` filtré en net8.0 ; compilation des projets associés réussie. Preuves locales : `Logs/M095/M095-speedup.trx` et `Logs/M095/M095-construction.trx` ; le nouveau test échouait sur le reçu nul avant correction, puis passe après completion et reconstruction du service.

## Points non résolus

Certification HiveMap complète non acquise : worktree non importé, 495 ressources LFS non matérialisées ; compilation Unity, tests EditMode et parcours visuel non exécutés. La reprise testée reconstruit le service sur le même dépôt mémoire : elle ne certifie ni redémarrage SQL ni reconnexion réseau réelle ; l'éditeur ouvert sur une autre copie n'a pas été utilisé.

## Checklist d'acceptation

- [x] Prérequis Palais Royal et ressources insuffisantes refusés côté serveur sans débit.
- [x] Coûts serveur, lancement unique, timer UTC et completion avec niveau mis à jour couverts par tests ciblés.
- [x] SpeedUp Construction, completion accélérée et rejeu avec reçu stable sans double consommation couverts.
- [x] Routage sélection / progression HiveMap inspecté dans le code ; aucun changement UI.
- [ ] Sélection visuelle, coûts/prérequis affichés, timer et completion à certifier dans HiveMap.
- [ ] Persistance et reconnexion réelles, puis SpeedUp depuis HiveMap à certifier.
- [x] Worktree uniquement ; aucun push, merge main, changement World Map marches, Chat Royal, FTUE ou Alliance.
