# LivingHive — Ronde quotidienne officielle mobile

Date : 23 juillet 2026  
État : tranche hors Unity ratifiée; validation Unity, appareil et staging ouvertes.

## Résultat produit

`Quêtes -> Ronde` possède maintenant un mode officiel injecté par la session
mobile. Il lit :

- `GET /game/v1/hives/{hiveId}/daily-round`;
- `POST /game/v1/hives/{hiveId}/daily-round/claim`;
- le contrat `living-hive-daily-round-v1`.

Le panneau affiche uniquement les faits acceptés par le serveur pour le jour UTC
courant :

- `collection_received`;
- `operation_launched`;
- `snapshot_read`.

La collecte vient de la production hors ligne officielle. Le lancement vient
d'une amélioration de bâtiment ou d'une recherche officielles. La lecture vient
du snapshot `Sac & stocks`. Une source fraîche marque le fait dans la même
transaction que son action; un rejeu ne marque rien et ne modifie aucune
révision.

Lorsque les trois faits sont vrais, le serveur autorise exactement une
réclamation de 120 miel et 60 pollen. Le téléphone ne fabrique ni fait, ni
récompense, ni révision.

## Frontière appareil, protection et serveur

### Appareil

- rendu responsive, navigation et textes `fr-CA` / `en-US`;
- états transitoires et dernier snapshot validé;
- préparation d'une commande de claim avant le transport;
- aucune preuve quotidienne, aucun crédit et aucune horloge autoritaire;
- aucun envoi automatique après un redémarrage ou une ambiguïté réseau.

### Cache de lecture protégé

- dernier GET validé, cloisonné par joueur, ruche, contrat et route;
- restauration en lecture seule pour le même joueur;
- aucune fusion avec la démonstration locale;
- corruption ou identité étrangère refusée.

### Boîte de mutation protégée

- format versionné, rétention maximale de 8 commandes et de 2 jours;
- plafond de 64 Kio;
- cloisonnement joueur + ruche + contrat + route;
- AES-GCM avec clé Android Keystore dans l'adaptateur de production;
- aucun jeton ou secret de session enregistré;
- même jour attendu, même révision et même clé d'idempotence au retry explicite;
- reprise visible mais jamais soumise automatiquement;
- conservation lors d'une interruption ordinaire;
- purge explicite au logout ou au changement de joueur.

### Serveur

- identité joueur/ruche, jour et prochain reset UTC;
- trois faits, révision, récompenses et disponibilité du claim;
- marquage atomique sur collecte, amélioration, recherche et lecture des stocks;
- reçu typé durable avec jour, révisions avant/après, heure d'acceptation,
  crédits exacts et code;
- rejeu exact après reconstruction et après une mutation ultérieure;
- rétention bornée à 128 reçus dédiés;
- migration et normalisation des anciens états JSON;
- activation fermée par défaut et en Production.

## États de l'interface

Le contrôleur expose `NotConfigured`, `Loading`, `Ready`, `OfflineReadOnly`,
`PreparingClaim`, `Claiming`, `ClaimPendingConfirmation` et `Error`.

Sans snapshot validé, aucune tâche, récompense ou progression n'est inventée.
Hors ligne, le panneau reste explicitement en lecture seule. Après une réponse
ambiguë, le bouton devient une vérification/reprise explicite avec la même
commande protégée. Une boîte corrompue impose une nouvelle lecture serveur avant
toute soumission.

Les trois boutons `Aller` ouvrent les parcours existants. Les cinq cibles
interactives du panneau mesurent au moins 44 px en 390x844 et 1600x900. Le badge
de Quêtes utilise `ClaimAvailable` seulement lorsque le contrôleur officiel est
configuré.

## Fermetures défensives

- contrat, identités, UTC, jour, reset, révision et faits strictement validés;
- exactement trois faits connus et aucun fait supplémentaire;
- récompenses obligatoirement égales à 120 miel et 60 pollen;
- révision maximale refusée avant addition;
- une seule rotation de session après un rejet 401;
- claim refusé si le snapshot, le jour ou la révision ont changé;
- reçu étranger, incomplet, altéré ou non UTC refusé;
- un reçu rejoué reste valide lorsque le snapshot courant possède une révision
  plus récente;
- état serveur corrompu fermé par le migrateur;
- replay de collecte consulté avant tout accrual, donc aucune mutation cachée;
- aucune seconde récompense après rejeu ou reconstruction.

## Localisation

Vingt-cinq clés `daily_round.official.*` ont été ajoutées. Les catalogues
contiennent 1 199 entrées chacun :

- 0 doublon;
- 0 valeur vide;
- 0 divergence de clés;
- 0 divergence de paramètres.

## Fichiers mobiles

- `Assets/BeeKingdom/Networking/HiveDailyRoundClient.cs`
- `Assets/BeeKingdom/Networking/ProtectedGameMutationOutbox.cs`
- `Assets/BeeKingdom/Networking/AndroidKeystoreGameMutationOutboxStore.cs`
- `Assets/BeeKingdom/Playground/HiveDailyRoundPresentation.cs`
- `Assets/BeeKingdom/Playground/MobileAccountSessionRuntimeBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`
- `Assets/BeeKingdom/Tests/Editor/HiveDailyRoundClientTests.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveOfficialDailyRoundTests.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveOfficialDailyRoundCapture.cs`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.fr-CA.json`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.en-US.json`

Les nouveaux scripts possèdent leurs fichiers `.meta`. Les inclusions ajoutées
temporairement aux quatre `.csproj` générés par Unity ont été retirées après la
compilation de preuve.

## Fichiers serveur

- `Server/src/BeeKingdom.HiveOperations/HiveDailyRoundContracts.cs`
- `Server/src/BeeKingdom.HiveOperations/HiveDailyRoundFacts.cs`
- `Server/src/BeeKingdom.HiveOperations/HiveDailyRoundOptions.cs`
- `Server/src/BeeKingdom.HiveOperations/HiveOperationModels.cs`
- `Server/src/BeeKingdom.HiveOperations/HiveOperationService.cs`
- `Server/src/BeeKingdom.HiveOperations/BuildingUpgradeContracts.cs`
- `Server/src/BeeKingdom.HiveOperations/HiveOfflineProductionService.cs`
- `Server/src/BeeKingdom.HiveOperations/HiveStateMigrator.cs`
- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/src/BeeKingdom.Server/appsettings.json`
- `Server/src/BeeKingdom.Server/appsettings.Production.json`
- `Server/tests/BeeKingdom.HiveOperations.Tests/BuildingUpgradeDailyRoundTests.cs`
- `Server/tests/BeeKingdom.HiveOperations.Tests/HiveDailyRoundTests.cs`
- `Server/tests/BeeKingdom.HiveOperations.Tests/HiveDailyRoundPersistenceTests.cs`
- `Server/tests/BeeKingdom.HiveOperations.Tests/HiveOfflineProductionDailyRoundTests.cs`
- `Server/tests/BeeKingdom.HiveOperations.Tests/LivingHiveResearchTests.cs`
- `Server/tests/BeeKingdom.Tests/BuildingUpgradeEndpointTests.cs`
- `Server/tests/BeeKingdom.Tests/HiveDailyRoundEndpointTests.cs`
- `Server/tests/BeeKingdom.Tests/HiveOfflineProductionEndpointTests.cs`
- `Server/tests/BeeKingdom.Tests/HiveStockEndpointTests.cs`
- `Server/tests/BeeKingdom.Tests/LivingHiveResearchEndpointTests.cs`

## Validation

### Mobile hors Unity

- boîte protégée : 8/8 réussis;
- client réseau : 10/10 réussis;
- présentation et reprise : 9/9 réussis;
- `BeeKingdom.Networking` : 0 erreur;
- `BeeKingdom.Tests` : 0 erreur;
- `Assembly-CSharp` : 0 erreur;
- `Assembly-CSharp-Editor` : 0 erreur;
- avertissements Unity observés : historiques et hors tranche;
- tests Editor Ronde : 6 scénarios ajoutés et compilés, exécution Unity non
  revendiquée.

### Serveur

- `BeeKingdom.HiveOperations.Tests` : 60 réussis, 0 échec;
- HTTP Ronde : 4/4;
- HTTP Stocks avec preuve quotidienne : 4/4;
- HTTP Amélioration avec preuve quotidienne : 3/3;
- HTTP Recherche avec preuve quotidienne : 5/5;
- HTTP Production hors ligne avec preuve quotidienne : 9/9;
- suite serveur complète net10.0 Release : 336 réussis, 0 échec et 8 SQL
  ignorés;
- build `BeeKingdom.Server` Release : 0 erreur et 1 avertissement de dépendance
  préexistant.

Le premier replay de collecte quotidienne a honnêtement révélé que
`CollectAsync` effectuait l'accrual avant la consultation du reçu : un rejeu
pouvait avancer silencieusement `pending` et la révision. Le reçu est maintenant
consulté avant toute mutation et une collecte fraîche produit une seule
incrémentation globale. Toutes les preuves ci-dessus ont été rejouées après cette
correction.

## Portes restant ouvertes

- F8 Unity global et exécution des tests Editor dans l'instance de l'utilisateur;
- captures honnêtes `NotConfigured` FR 390x844 et EN 1600x900, puis inspection
  native;
- parcours manuel dans `Assets/Scenes/LivingHive.unity`;
- Android Keystore sur appareil physique, mise en arrière-plan, reprise réseau
  ambiguë et changement de joueur;
- vraie session mobile contre TLS staging;
- SQL Server jetable et persistance multi-instance;
- activation coordonnée des drapeaux Stocks, Production, Amélioration, Recherche
  et Ronde;
- candidat, transfert, déploiement et smoke staging.

Aucun candidat, transfert, activation ou déploiement n'a été réalisé.

## Fondations protégées

La scène `Assets/Scenes/LivingHive.unity`, la scène terrain canonique, la carte
50x50, ses images, l'image de base de la ruche et le chantier Communication ne
sont pas modifiés.

Empreintes finales :

- scène terrain canonique :
  `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3`;
- scène `LivingHive.unity` :
  `ECCFE9AA81AE883317E4E951C8552DCEF1A156179F35480567466AB95A9708E7`;
- image de base `background_hive.png` :
  `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`.

## Synchronisation

La synchronisation de fin a été tentée le 23 juillet à `06:09:26Z`. Elle a
échoué avant toute copie avec `Accès refusé` sur
`\\DESKTOP-D3D29K7\BeeKingdomHost`. Le dernier rapport valide demeure daté du
`2026-07-22T02:57:51Z` : 0 conflit bloqué, 0 copie VM vers l'hôte et 4
suppressions historiques en attente. Aucun remappage, accès direct à `Z:` ou
relâchement du bac à sable n'a été tenté. Les changements restent ratifiés sur
la copie locale `C:`.
