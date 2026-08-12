# LivingHive — soin officiel du couvain mobile

Date : 2026-07-23

## Résultat produit

La Nurserie possède maintenant un cycle de soin officiel complet, sans confondre
l'aperçu local historique avec l'autorité de production :

- `Nourrir` coûte 300 miel, dure 12 secondes et ajoute 22 nutrition ;
- `Stabiliser` coûte 45 cire, dure 13 secondes et ajoute 7 stabilité ;
- une seule opération peut être active ;
- le débit a lieu une seule fois au démarrage ;
- aucun gain n'est accordé avant la complétion ;
- nutrition et stabilité restent bornées entre 0 et 100.

Dans l'interface LivingHive, le détail de la Nurserie affiche les deux actions,
l'opération active, le temps restant dérivé de l'UTC serveur, l'action de
finalisation, le mode hors ligne consultatif et la reprise explicite d'une
commande dont la réponse réseau demeure ambiguë. Les cibles utiles restent
d'au moins 44 pixels en 390x844 et 1600x900.

L'état `Vitality=null` reste honnête : l'application montre que le couvain n'est
pas initialisé et n'invente ni jauge, ni ressource, ni action disponible.

## Frontière appareil et serveur

### Sur l'appareil

- rendu, localisation et accessibilité tactile ;
- dernier snapshot validé dans le cache de lecture protégé ;
- minuteur visuel projeté avec une horloge monotone à partir de l'UTC serveur ;
- commande préparée dans l'outbox Android Keystore avant tout transport ;
- reprise explicite avec exactement la même route, révision et clé idempotente ;
- conservation d'une commande ambiguë pendant une interruption normale ;
- purge de toutes les commandes du joueur au logout ou changement explicite.

L'appareil ne débite jamais le miel ou la cire et ne crédite jamais la vitalité.
Il ne soumet jamais automatiquement une commande préparée au redémarrage ou au
retour du réseau.

### Sur le serveur

Le contrat unique est `living-hive-brood-vitality-v1` :

- `GET /game/v1/hives/{hiveId}/brood/vitality` ;
- `POST /game/v1/hives/{hiveId}/brood/vitality/care/start?type=...` ;
- `POST /game/v1/hives/{hiveId}/brood/vitality/care/{operationId}/complete`.

Le serveur possède l'identité, l'appartenance à la ruche, les ressources, les
valeurs de vitalité, l'UTC, l'opération, les révisions et les reçus. Les réponses
de mutation contiennent `{ receipt, snapshot }`; le reçu expose explicitement
`RevisionBefore` et `RevisionAfter`.

`BroodCareReceipts` est typé, persistant, hashé et borné à 128 entrées. Un rejeu
identique renvoie le reçu original sans second débit, gain ou changement de
révision. La même clé avec une charge différente produit
`game.idempotency_conflict`. Un démarrage frais marque le fait
`OperationLaunched` de la Ronde uniquement lorsque `HiveDailyRound` est activé,
jamais au rejeu.

## États et récupération mobile

Le contrôleur distingue :

- non configuré ;
- chargement ;
- prêt ;
- lecture protégée hors ligne ;
- préparation et envoi ;
- confirmation réseau incertaine ;
- erreur récupérable.

Après une réponse ambiguë, l'entrée protégée demeure visible et seul le bouton
de reprise renvoie la commande exacte. Une lecture serveur réconcilie et retire
les entrées devenues obsolètes. Un échec de stockage protégé ferme les actions
avant le transport.

## Défenses vérifiées

- snapshot étranger joueur/ruche refusé ;
- contrat, UTC, bornes, révisions et durées 12/13 secondes validés ;
- type inconnu et révision maximale refusés avant session ou transport ;
- reçu altéré ou détaché refusé ;
- réponse fraîche incohérente avec son opération refusée ;
- outbox cloisonnée par joueur, ruche, contrat et route ;
- charge de soin liée au hash sans détourner le champ de jour de la Ronde ;
- document corrompu supprimé de la zone protégée ;
- aucun jeton d'accès stocké ;
- aucun débit local, crédit local ou soumission automatique.

## Localisation

Trente-neuf clés `brood_care.*` ont été ajoutées en français canadien et en
anglais américain. Les deux catalogues comptent chacun 1 238 entrées :

- 0 doublon ;
- 0 valeur vide ;
- 0 clé divergente ;
- 39 clés de soin du couvain dans chaque langue.

## Fichiers mobiles

- `Assets/BeeKingdom/Networking/HiveBroodVitalityClient.cs`
- `Assets/BeeKingdom/Networking/HiveBroodVitalityClient.cs.meta`
- `Assets/BeeKingdom/Networking/ProtectedGameMutationOutbox.cs`
- `Assets/BeeKingdom/Tests/Editor/HiveBroodVitalityClientTests.cs`
- `Assets/BeeKingdom/Tests/Editor/HiveBroodVitalityClientTests.cs.meta`
- `Assets/BeeKingdom/Tests/Editor/ProtectedGameMutationOutboxTests.cs`
- `Assets/BeeKingdom/Tests/Editor/ProtectedGameMutationOutboxTests.cs.meta`
- `Assets/BeeKingdom/Playground/HiveBroodVitalityPresentation.cs`
- `Assets/BeeKingdom/Playground/HiveBroodVitalityPresentation.cs.meta`
- `Assets/BeeKingdom/Playground/MobileAccountSessionRuntimeBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveOfficialBroodCareTests.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveOfficialBroodCareTests.cs.meta`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveManualCollectionTests.cs`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.fr-CA.json`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.en-US.json`

Le bloc Communication du présentateur partagé n'a pas été modifié.

## Fichiers serveur

- `Server/src/BeeKingdom.HiveOperations/BroodVitalityCareService.cs`
- `Server/src/BeeKingdom.HiveOperations/HiveOperationModels.cs`
- `Server/src/BeeKingdom.HiveOperations/HiveStateMigrator.cs`
- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/tests/BeeKingdom.HiveOperations.Tests/BroodVitalityCareTests.cs`
- `Server/tests/BeeKingdom.HiveOperations.Tests/HiveOperationServiceTests.cs`
- `Server/tests/BeeKingdom.Tests/BroodVitalityEndpointTests.cs`
- `Docs/ProductionIntegration/LivingHive_BroodVitalityCare_Server_2026-07-23.md`

## Validation hors Unity

Mobile :

- harnais autonome final : 23 réussis, 0 échec ;
- client : 9 tests ;
- outbox : 5 tests ;
- présentation et intégration : 9 tests ;
- `BeeKingdom.Networking` : 0 erreur, 33 avertissements préexistants ;
- `BeeKingdom.Tests` : 0 erreur, 0 avertissement ;
- `Assembly-CSharp` : 0 erreur, 117 avertissements préexistants ;
- `Assembly-CSharp-Editor` : 0 erreur lors de la passe d'intégration, puis le
  dernier test ajouté a été recompilé dans le harnais autonome ;
- toutes les inclusions temporaires ajoutées aux projets Unity générés ont été
  retirées.

Serveur après revue croisée :

- tests ciblés BroodVitality cœur : 9/9 ;
- suite HiveOperations : 69/69 ;
- tests HTTP BroodVitality : 4/4 ;
- suite serveur net10 : 338 réussis, 0 échec, 8 SQL ignorés ;
- build Release : 0 erreur, 1 avertissement Microsoft.Data.SqlClient
  préexistant.

Journaux :

- `Artifacts/BroodCareTestsFinal.log`
- `Artifacts/BroodCareServerHiveOperationsFinal.log`
- `Artifacts/BroodCareServerNet10Final.log`
- `Artifacts/BroodCareServerBuildFinal.log`

## Portes restant ouvertes

La tranche n'est pas revendiquée comme ratifiée dans Unity cette nuit. Demain :

1. ouvrir `Assets/Scenes/LivingHive.unity` ;
2. laisser Unity terminer son import et vérifier l'absence d'erreur Console ;
3. exécuter F8 et les tests Editor du soin officiel ;
4. inspecter la Nurserie en portrait 390x844 et paysage 1600x900 ;
5. vérifier le parcours local historique sans le confondre avec l'état officiel ;
6. avec une session de preuve, contrôler les états non configuré, prêt, actif,
   hors ligne et reprise explicite.

Les portes de production restent : vraie configuration compte/HiveId, Keystore
sur appareil Android, interruption/réseau réel, SQL natif et multi-instance,
TLS staging, activation coordonnée de `BroodVitality` et `HiveDailyRound`,
candidat, transfert, déploiement et smoke test. Les deux drapeaux concernés
restent faux par défaut et en Production.

## Fondations protégées

Aucune scène, image, carte terrain 50x50 ou donnée de chat n'a été modifiée.
Empreintes confirmées :

- terrain canonique :
  `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3` ;
- `Assets/Scenes/LivingHive.unity` :
  `ECCFE9AA81AE883317E4E951C8552DCEF1A156179F35480567466AB95A9708E7` ;
- `background_hive.png` :
  `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`.

## Synchronisation

La synchronisation de départ puis celle de fin, retentée le
`2026-07-23T07:24:35Z`, ont échoué avant toute copie avec `Accès refusé` sur
`\\DESKTOP-D3D29K7\BeeKingdomHost`. Aucun accès direct à `Z:`, remappage ou
contournement du bac à sable n'a été tenté. Le dernier rapport valide demeure
celui du `2026-07-22T02:57:51Z` : 0 conflit, 0 copie VM vers l'hôte et
4 suppressions historiques en attente. Le jalon reste intégralement sur `C:`
avec la présente liste exacte.
