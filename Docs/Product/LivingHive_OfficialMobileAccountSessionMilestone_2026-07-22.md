# LivingHive — session officielle mobile

Date : 2026-07-22  
Statut : fondation client/serveur ratifiée, fermée par défaut, non déployée

## Résultat

LivingHive possède maintenant une vraie chaîne de session officielle injectable :
readiness publique, formulaire conditionnel, login, jeton d'accès en mémoire,
refresh rotatif dans un coffre protégé, restauration, expiration, logout bearer
et fermeture de l'ancien joueur avant le suivant.

La chaîne reste volontairement inactive dans le produit courant : aucune ressource
`MobileAccountSessionRuntime.asset` n'est livrée et les claims/drapeaux serveur
Production restent fermés. L'état réel affiché au joueur demeure donc
`Connexion mobile non configurée`, sans courriel, mot de passe, jeton ou compte
fictif. Les écrans `PRÊT · APERÇU QA` et `SESSION ACTIVE · QA PREVIEW` sont des
preuves de disposition en mémoire, sans réseau ni persistance.

## Frontière appareil / serveur

| Élément | Appareil mobile | Serveur |
|---|---|---|
| Readiness | lit la route publique et ferme le formulaire si un prérequis manque | publie les autorisations, claims live, blockers et l'heure UTC, sans secret |
| Mot de passe | tampon UI temporaire, masqué et effacé immédiatement après soumission; jamais persisté | vérifie le justificatif et applique verrouillage/limites |
| Jeton d'accès | mémoire vive seulement; jamais PlayerPrefs, fichier ou manifeste | émet, expire, valide et révoque |
| Jeton de renouvellement | chiffré AES-GCM par une clé non exportable Android Keystore; jamais en clair dans PlayerPrefs | rotation à usage unique liée au joueur et à la session |
| Identité | ne choisit ni PlayerId, AccountId ni SessionId | dérive et atteste les trois identités |
| Installation | GUID aléatoire propre à l'installation, non matériel et non autoritaire | simple métadonnée de session, jamais preuve d'identité |
| Adresse réseau | aucune valeur déclarée par le mobile n'est digne de confiance | dérive `RemoteIpAddress` de la connexion, ou `unknown`; aucun header forwarded implicite |
| Gameplay | rend l'état et bloque les commandes si l'autorité manque | publie `GameplayAuthorityGranted` et demeure propriétaire des mutations |
| Logout | purge toujours accès + refresh local, même si la confirmation distante échoue ou est annulée | dérive la session du bearer et la révoque; ignore tout SessionId déclaré |

## Contrat client

- `MobileAccountSessionGate` exige simultanément le transport/coffre client et les
  claims serveur `LiveAccounts`, `LiveSessions`, `SessionCreationAllowed` et
  `TokenIssuanceAllowed`.
- `MobileAccountSessionClient` implémente `IGameAccountSessionSource`; les clients
  de jeu existants peuvent donc recevoir le bearer sans le persister.
- Le login n'est publié aux autres systèmes qu'après écriture puis relecture
  exacte du refresh dans le coffre protégé.
- Si cette écriture ou celle d'un refresh rotatif échoue, le client tente de
  révoquer immédiatement la session nouvellement émise et ne publie aucun accès.
- Un refresh dont le PlayerId ou le SessionId ne correspond pas au coffre est
  refusé, purgé et jamais présenté comme session active.
- Une expiration interdit immédiatement `TryGetSession`; aucune action offline
  n'est transformée en mutation serveur.
- Un second login ferme d'abord la session A, purge son coffre, puis seulement
  publie la session B.

## Transport et stockage

- Routes exactes :
  - `GET /runtime/account-session-readiness`;
  - `POST /auth/login`;
  - `POST /auth/refresh`;
  - `POST /auth/logout` avec bearer seulement.
- HTTPS est obligatoire. Un HTTP loopback doit être explicitement autorisé pour
  le développement; aucun certificat personnalisé ou contournement TLS n'existe.
- Réponses bornées à 1 MiB, timeout borné, aucune relance automatique de login ou
  mutation.
- Le corps readiness est rejeté s'il contient un champ access token, refresh
  token ou mot de passe.
- Android : AES/GCM/NoPadding, clé `AndroidKeyStore`, IV et ciphertext seulement
  dans SharedPreferences. Editor et toute plateforme sans coffre restent fermés.
- iOS Keychain n'est pas encore implémenté; aucune promesse iOS n'est faite.

## Interface LivingHive

- L'onglet Connexion garde son état réel fermé en l'absence de configuration.
- Le formulaire courriel/mot de passe n'apparaît que si les deux portes sont
  ouvertes. Les trois cibles utiles mesurent au moins 44 px en 390x844 et
  1600x900.
- Le bouton démo redondant a été retiré du formulaire prêt pour éviter une
  collision paysage; l'accès démo reste disponible sous Accueil.
- Les messages de carte, d'en-tête et de bas de panneau partagent le même état.
- Une session compte active sans autorité de jeu indique explicitement
  `Compte connecté · jeu encore local`.
- Les erreurs visibles utilisent seulement les codes sûrs localisés; aucun corps
  brut, identifiant, mot de passe ou jeton n'est rendu.

## Preuves

- Harnais autonome client + sortie : `34/34`, 0 échec.
- Serveur avec descripteur partagé aligné sur `/auth/refresh` et IP dérivée de la
  connexion : tests session `7/7`, contrats partagés/HTTP `116/116`, suite
  `272` réussis, `7` SQL externes ignorés, build Release 0 erreur et 1 warning
  SqlClient préexistant.
- F8 final : `Artifacts/LivingHiveMobileAccountSession_ClosureF8.log`;
  marqueur de succès 1, `error CS` 0, `Compilation failed` 0,
  `AssertionException` 0.
- Captures finales : `Artifacts/MobileAccountSession_RatifiedCapture.log`;
  succès 1, échec 0, mauvaise dimension 0, erreur C# 0.
- Catalogues : `fr-CA` 1054/1054 et `en-US` 1054/1054, aucune clé dupliquée.
- Manifeste :
  `Docs/Product/Evidence/MobileAccountSession/MobileAccountSession_CaptureManifest.md`.

Preuves exactes :

- NotConfigured FR 390x844 —
  `f5b97beba1a2ab27f2a38dc27d9219cce9d8830d9f9be63d7b5d7315961f6730`;
- ReadyForm QA FR 390x844 —
  `07964b92dfcedf76011f7781227b9d02c294f12093580b0bc558bee1603b780e`;
- NotConfigured EN 1600x900 —
  `05ceb3021207f7c708517c61eb6dbc78408c4189c9a5c5aa9ed90a57e5b94256`;
- AuthenticatedPreview QA EN 1600x900 —
  `3b8821d407e2b262055e63480a6337e907488184e639a267bad47a9ec6abc294`.

Le premier F8 a refusé le bouton démo qui empiétait sur le message paysage. La
première inspection visuelle a ensuite refusé un message `non raccordé` resté
sous les états QA, puis un en-tête portrait sur deux lignes. Ces trois versions
ne sont pas ratifiées; les preuves finales ont été régénérées nativement, sans
recadrage ni redimensionnement.

## Fondations protégées

- scène canonique 50x50 :
  `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3`;
- scène LivingHive :
  `ECCFE9AA81AE883317E4E951C8552DCEF1A156179F35480567466AB95A9708E7`;
- image de base LivingHive :
  `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`.

## Fichiers client et preuves

- `Assets/BeeKingdom/Networking/MobileAccountSessionClient.cs`;
- `Assets/BeeKingdom/Networking/AndroidKeystoreRefreshTokenStore.cs`;
- `Assets/BeeKingdom/Networking/UnityMobileAccountSessionRestTransport.cs`;
- `Assets/BeeKingdom/Networking/MobileAccountSessionRuntimeConfiguration.cs`;
- `Assets/BeeKingdom/Playground/MobileAccountSessionRuntimeBootstrap.cs`;
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`;
- `Assets/BeeKingdom/Tests/Editor/MobileAccountSessionClientTests.cs`;
- `Assets/BeeKingdom/Playground/Editor/MobileAccountSessionUiTests.cs`;
- `Assets/BeeKingdom/Playground/Editor/SandboxMobileAccountSessionCapture.cs`;
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveManualCollectionTests.cs`;
- catalogues `strings.fr-CA.json` et `strings.en-US.json`;
- harnais autonome `Artifacts/HivePerimeterClientHarness`;
- preuves `Docs/Product/Evidence/MobileAccountSession`.

## Portes encore fermées

1. prouver Android Keystore sur appareil physique et cycle installer/redémarrer/logout;
2. créer une configuration staging non secrète et HTTPS, absente du build actuel;
3. configurer et prouver explicitement le proxy de confiance avant toute lecture
   de headers forwarded en staging/Production;
4. tests SQL natifs des comptes/sessions, sauvegarde et rollback;
5. test staging deux joueurs, rotation/rejeu, changement de compte et perte réseau;
6. cache de gameplay protégé, borné et partitionné avec lecture offline seulement;
7. autorité serveur pour progression, tutoriel, stocks, production, roster et
   autres systèmes encore locaux;
8. iOS Keychain si la cible iOS est retenue;
9. flags Production, candidat, transfert, activation et déploiement restent
   interdits jusqu'à ratification explicite.

Communication est resté entièrement gelé et aucun fichier chat n'a été modifié.

## Synchronisation VM

Les synchronisations d'entrée et de sortie ont échoué avant toute copie avec
`Accès refusé` sur `\\DESKTOP-D3D29K7\BeeKingdomHost`; la dernière tentative date
de `2026-07-22T19:56:36Z`. Le rapport
`.codex/vm-sync-last-report.txt` reste daté de `2026-07-22T02:57:51Z`, avec
0 conflit, 0 copie VM vers hôte et 4 suppressions historiques en attente. Aucun
remappage, assouplissement du bac à sable ou accès direct à `Z:` n'a été tenté.
Le jalon reste uniquement sur `C:` jusqu'à la synchronisation utilisateur.
