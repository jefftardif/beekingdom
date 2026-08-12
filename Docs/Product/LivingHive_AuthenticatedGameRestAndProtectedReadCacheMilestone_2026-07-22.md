# LivingHive — REST de jeu authentifié et cache de lecture protégé

Date : 22 juillet 2026  
Responsable : Architecte  
État : implémenté et validé localement; activation Production fermée

## Résultat

Le shell de compte mobile peut maintenant porter les opérations officielles de
sortie au périmètre jusqu'aux routes `/game/v1/**`. Le client renouvelle l'accès
avant expiration et autorise exactement une seconde tentative après un `401`,
avec le même objet de commande et la même clé d'idempotence. Un second `401`
purge la session. Une panne réseau ne répète jamais une mutation.

Les derniers `GET` déjà validés peuvent être consultés hors ligne depuis un
cache Android chiffré. Cette consultation retire toutes les actions serveur et
n'accorde ni récompense, ni progression, ni modification économique.

## Propriété appareil / serveur

| Élément | Appareil mobile | Serveur |
|---|---|---|
| Jeton d'accès | mémoire seulement | émission, expiration et révocation |
| Jeton de renouvellement | AndroidKeyStore, AES-GCM | rotation à usage unique |
| Identité joueur/session | liaison protégée nécessaire au partitionnement | autorité et validation finales |
| Derniers `GET` de jeu | cache chiffré, borné, lecture seule | source officielle des instantanés |
| Ruche, révisions, cycle, heure | rendu du dernier instantané validé | autorité |
| Réservation, sortie, rappel, réclamation | commandes en ligne seulement | validation et mutation atomique |
| Ressources et récompenses | jamais créditées par le cache | autorité et reçu |
| Idempotence | même clé conservée lors de l'unique répétition 401 | reçu et conflit |

## Session et transport

- `MobileAccountSessionClient` implémente une source de session asynchrone et
  déduplique les renouvellements concurrents;
- l'identité protégée est chargée avant la lecture de disponibilité afin de
  permettre une consultation hors ligne sûre après redémarrage;
- `UnityAuthenticatedGameRestTransport` exige HTTPS, sauf loopback HTTP activé
  explicitement pour le développement;
- méthodes acceptées : `GET` et `POST` sous `/game/v1/` seulement;
- requête bornée à 512 Kio, réponse à 1 Mio, profondeur JSON à 32;
- aucun gestionnaire de certificat personnalisé, aucun log de jeton ou de corps;
- les `GET` officiels exigent `Cache-Control: private, no-store`;
- `PlayerId` serveur sous forme `{ "value": "guid" }`, GUID simples,
  dictionnaires, UTC et durées sont décodés par le codec commun;
- le transport ne relance rien lui-même. Le client de domaine possède l'unique
  budget de répétition après `401`.

## Cache de lecture protégé

Contrat `v1` :

- stockage AndroidKeyStore `AES/GCM/NoPadding`, alias distinct du jeton de
  renouvellement;
- aucune persistance en clair dans `PlayerPrefs` ou `SharedPreferences`;
- partition : joueur + ruche + version de contrat + route exacte;
- 12 entrées maximum, 512 Kio par lecture, document de 1 Mio, rétention de
  7 jours;
- empreinte SHA-256 interne de chaque corps, puis authentification AES-GCM du
  document complet;
- corruption, version inconnue ou corps incohérent : suppression du cache,
  sans état inventé;
- sauvegarde seulement après validation complète du DTO et seulement sur GET;
- repli seulement sur indisponibilité réseau ou disponibilité de session
  impossible. Aucun repli sur `401`, conflit serveur ou réponse invalide;
- POST, réservation, lancement, rappel et réclamation ne lisent jamais le
  cache et ne sont jamais répétés après une panne réseau.

## Runtime et interface

La configuration reste volontairement opt-in. Il faut simultanément :

1. `officialAccountsEnabled=true`;
2. `officialGameplayEnabled=true`;
3. une URL de base valide;
4. un `officialHiveId` GUID;
5. soit une session authentifiée avec autorité de jeu serveur, soit, uniquement
   pour consultation, une identité connue dans le magasin protégé Android.

Aucun asset `Assets/Resources/BeeKingdom/MobileAccountSessionRuntime.asset`
n'est livré. Sans cet asset, le produit reste `NotConfigured`.

Le panneau de sortie affiche `CONSULTATION HORS LIGNE` / `OFFLINE VIEW ONLY`,
la date du cache dans les preuves, et remplace les actions par `Attendre le
serveur` / `Wait for server`. L'aperçu hors ligne ne montre pas un ancien reçu
de récompense comme nouveau débrief.

## Frontière serveur

L'Intégrateur a renforcé les lectures `/game/v1/**` :

- `401 game.session_required` sans identité ni contenu sensible pour un bearer
  absent ou refusé;
- `Cache-Control: private, no-store` et `Pragma: no-cache` sur les GET;
- flags Production inchangés et fermés;
- tests `GameReadModelSecurityTests` : 2/2;
- build serveur : 0 erreur, avertissement `Microsoft.Data.SqlClient`
  préexistant;
- aucun candidat, transfert, activation ni déploiement.

Rapport serveur :
`Docs/ProductionIntegration/GameReadModel_AuthCacheBoundary_2026-07-22.md`.

## Validation

- harnais autonome .NET : 43/43;
- scénarios couverts : deux joueurs, expiration, renouvellements concurrents,
  un puis deux `401`, conservation de l'idempotence, panne réseau POST sans
  répétition, GET hors ligne, partitionnement, corruption, codec serveur;
- Unity 6000.5.3f1, F8 global :
  `Artifacts/LivingHiveAuthenticatedGameBridge_ClosureF8.log`, marqueur de succès,
  0 `error CS`, 0 `Compilation failed`, 0 `AssertionException`;
- catalogues : 1056/1056 entrées uniques en fr-CA et en-US, aucun doublon;
- capture : `Artifacts/LivingHiveAuthenticatedGameBridge_Capture.log`, sortie 0,
  huit PNG natifs et manifeste;
- preuves hors ligne inspectées à résolution native :
  - `LivingHive_PerimeterSortie_OfflineReadOnlyQA_FR_390x844.png`, SHA-256
    `710685fcd0632db8f5ac05a4807e8f6cc30c4a801911af7dcb1ef4753b73f41b`;
  - `LivingHive_PerimeterSortie_OfflineReadOnlyQA_EN_1600x900.png`, SHA-256
    `bb7c2a2ce5b0b17710a1686424aa5f6a15130e58e3ebd569832ec5cc77532d31`.

Le manifeste complet est
`Docs/Product/Evidence/LivingHivePerimeterSortie/LivingHivePerimeterSortie_CaptureManifest.md`.
Les captures QA utilisent des DTO synthétiques de mise en page, portent
`APERÇU QA` / `QA PREVIEW`, n'appellent aucun serveur et ne persistent rien.

## Fondations protégées

- scène canonique 50x50 :
  `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3`;
- scène LivingHive :
  `ECCFE9AA81AE883317E4E951C8552DCEF1A156179F35480567466AB95A9708E7`;
- image de base LivingHive :
  `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`.

Aucun fichier terrain, aucune image protégée et aucun fichier Communication
n'a été modifié.

## Portes encore ouvertes

- créer hors dépôt l'asset de configuration par environnement et fournir le
  véritable HiveId après décision d'exploitation;
- activer comptes, sessions et autorité de jeu uniquement en staging contrôlé;
- prouver sur appareil Android physique AndroidKeyStore, rotation, reprise après
  arrêt forcé, corruption, TLS réel et absence de fuite;
- produire un build Android IL2CPP/AOT et valider le codec `System.Text.Json`;
- exécuter les intégrations SQL natives et les scénarios staging bout en bout;
- décider seulement ensuite d'un candidat, d'une activation et d'un déploiement.

## Fichiers principaux

Client :

- `Assets/BeeKingdom/Networking/AuthenticatedGameRestContracts.cs`;
- `Assets/BeeKingdom/Networking/UnityAuthenticatedGameRestTransport.cs`;
- `Assets/BeeKingdom/Networking/ProtectedGameReadCache.cs`;
- `Assets/BeeKingdom/Networking/AndroidKeystoreGameReadCacheStore.cs`;
- `Assets/BeeKingdom/Networking/MobileAccountSessionClient.cs`;
- `Assets/BeeKingdom/Networking/HivePerimeterSortieClient.cs`;
- `Assets/BeeKingdom/Networking/MobileAccountSessionRuntimeConfiguration.cs`;
- `Assets/BeeKingdom/Networking/link.xml`;
- `Assets/BeeKingdom/Playground/MobileAccountSessionRuntimeBootstrap.cs`;
- `Assets/BeeKingdom/Playground/HivePerimeterSortiePresentation.cs`;
- blocs non-Communication de
  `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`;
- tests et harnais LivingHive associés;
- catalogues fr-CA et en-US.

Serveur :

- `Server/src/BeeKingdom.Server/Program.cs`;
- `Server/tests/BeeKingdom.Tests/GameReadModelSecurityTests.cs`;
- rapport de frontière serveur cité plus haut.

## Synchronisation VM

La synchronisation d'entrée a été tentée par l'outil officiel et a échoué avant
toute copie : accès refusé à `\\DESKTOP-D3D29K7\BeeKingdomHost`. Aucun remappage,
accès direct à `Z:` ou relâchement du bac à sable n'a été tenté. Le rapport
`.codex/vm-sync-last-report.txt` reste daté de `2026-07-22T02:57:51Z`, avec
0 conflit et 4 suppressions historiques en attente. Une nouvelle tentative sera
faite à la clôture.

La tentative finale du `2026-07-22T20:36:25Z` a produit le même refus avant
toute copie. Le rapport officiel est donc resté daté du
`2026-07-22T02:57:51Z`. Le jalon demeure intégralement sur `C:` en attente de la
synchronisation par l'utilisateur ou du rétablissement normal du partage.
