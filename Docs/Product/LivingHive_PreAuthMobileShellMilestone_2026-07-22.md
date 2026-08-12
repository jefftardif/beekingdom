# LivingHive — coquille mobile de pré-authentification

Date de ratification : 22 juillet 2026  
Statut : réalisé et ratifié sous Unity 6000.5.3f1; authentification officielle volontairement fermée

## Résultat produit

L’entrée de `LivingHive` distingue maintenant clairement le jeu local de
prévisualisation du futur compte officiel. L’onglet `Connexion` montre l’état
réel de préparation du service au lieu d’afficher des champs courriel/mot de
passe sans transport de production. Dans l’état Production courant, le joueur
voit que les sessions et les jetons sont fermés côté serveur, puis peut
continuer dans une démo locale explicitement non officielle.

L’onglet `Créer` crée seulement un profil de démonstration sur l’appareil. Il
demande un nom d’affichage de trois caractères ou plus, sans courriel, mot de
passe, compte serveur, jeton ni promesse de synchronisation. Une saisie trop
courte reste sur place et explique comment corriger l’erreur.

Le menu `Développement` reste limité à l’accueil dans l’éditeur ou un build de
développement. Il ne chevauche plus les panneaux Connexion ou Créer et ne fait
pas partie de la surface joueur de production.

## Double porte de sécurité mobile

`MobileAccountSessionGate` refuse toute collecte de justificatifs tant que les
deux conditions suivantes ne sont pas vraies en même temps :

1. la readiness serveur autorise comptes, sessions et émission de jetons;
2. le client mobile possède un transport sécurisé réellement configuré.

Un serveur prêt ne contourne donc jamais un client incomplet, et un client
configuré ne contourne jamais une porte serveur fermée. Logout ou changement de
joueur remet les deux portes à zéro. Cette classe ne transporte et ne persiste
aucun mot de passe, access token ou refresh token.

Les états `NotConfigured`, `Checking`, `PreparationOnly`, `Unavailable` et
`Ready` sont représentables et localisés. Le runtime actuel reste dans
`PreparationOnly`; aucun client HTTP de readiness ni formulaire officiel n’est
activé par ce jalon.

## Frontière appareil / serveur

- Appareil aujourd’hui : langue et préférence locale, nom d’affichage de la
  démo, rendu, tutoriel et caches de prévisualisation non officiels.
- Serveur aujourd’hui : identité, compte, empreinte de justificatif, session,
  émission/rotation/révocation de jetons et autorité de profil officiel.
- Interdit sur l’appareil par cette coquille : mot de passe persistant, access
  token persistant, refresh token en clair, soldes ou progression officiels.
- Hors ligne : la démo locale reste utilisable; aucune création de compte,
  connexion ou mutation officielle n’est simulée.

Le raccordement futur exigera une URL HTTPS validée, un client de readiness
borné et annulable, un access token en mémoire, un refresh token protégé par
Android Keystore ou iOS Keychain, la rotation serveur, la révocation au logout
et la purge complète lors d’un changement de joueur. Le chat restera
`NotConfigured` jusqu’à l’existence de cette session officielle et de ce
stockage protégé.

## Durcissement du serveur par l’Intégrateur

L’audit de l’Intégrateur a trouvé que les anciennes routes `/auth/login`,
`/auth/refresh` et `/accounts` pouvaient encore atteindre leurs services alors
que la readiness Production annonçait les portes fermées. Elles retournent
maintenant `503 auth.unavailable` avant tout appel métier lorsque leur drapeau
Production est faux.

Le smoke Release en environnement Production confirme les trois refus. Le build
Release passe sans erreur. Le test NUnit ajouté compile, mais le testhost local
ne découvre toujours aucun test NUnit; il n’est donc pas compté comme exécuté.
Les drapeaux Production restent faux, aucun jeton, candidat, transfert, staging
ou déploiement n’a été activé. Rapport :
`Docs/ProductionIntegration/Authentication_ProductionBoundaryAudit_2026-07-22.md`.

## Validation Unity et preuves mobiles

- F8 et suite LivingHive : `Artifacts/LivingHivePreAuthShellF8.log`, commande
  batch sortie 0, marqueur `LivingHive manual collection checks passed.`, zéro
  échec, zéro `error CS` et zéro `Compilation failed`;
- captures : `Artifacts/LivingHivePreAuthShellCapture.log`, commande batch
  sortie 0, marqueur `LivingHive language selector proof captured`, zéro
  `error CS`;
- catalogues : 649 clés uniques dans `fr-CA` et 649 dans `en-US`, 0 doublon,
  0 asymétrie, dont 69 clés `splash.*`;
- preuves : huit PNG exacts dans
  `Docs/Product/Evidence/LivingHiveLanguageSelector`, quatre en 390x844 et quatre
  en 1600x900;
- manifeste :
  `Docs/Product/Evidence/LivingHiveLanguageSelector/LivingHiveLanguageSelectorManifest.md`;
- inspection : accueil FR/EN, état serveur fermé et profil démo restent lisibles
  sans champ secret, coupure, collision ou menu de développement superposé;
- fin de validation : 0 processus Unity, dotnet, testhost ou bee_backend.

Empreintes des quatre nouvelles preuves de frontière :

- `LivingHive_AccountPreparation_fr-CA_390x844.png` :
  `50AB19254BA1F38970E9F5691E4C5C2B5A45E2D9CAD585E04F433A00A9AFB414`;
- `LivingHive_AccountPreparation_en-US_1600x900.png` :
  `F96FC3CB9CEF10AE47F108DAD7EC2BE99CDBBE8562403630C2C44D1B859A938A`;
- `LivingHive_DemoProfile_en-US_390x844.png` :
  `FF2EDB73B7981BDE05AFC7D33D05D4AF5AE00613DD4C8A3E25A585AAFCD0B206`;
- `LivingHive_DemoProfile_fr-CA_1600x900.png` :
  `57D88237A5AEC160749D6A53A7159C5410783CC8FD2B4F1A4604229FD8C05C1E`.

## Fichiers exacts

Architecte :

- `Assets/BeeKingdom/Networking/AccountSessionReadinessGate.cs`
- `Assets/BeeKingdom/Networking/AccountSessionReadinessGate.cs.meta`
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveManualCollectionTests.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxSplashLanguageCapture.cs`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.fr-CA.json`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.en-US.json`
- `Docs/Product/Evidence/LivingHiveLanguageSelector/*`
- ce rapport et les trois documents de continuité produit.

Intégrateur :

- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/tests/BeeKingdom.Tests/AuthenticationProductionBoundaryTests.cs`
- `Docs/ProductionIntegration/Authentication_ProductionBoundaryAudit_2026-07-22.md`

Aucun module, test, document ou ancrage de chat n’a été modifié.

## Fondations protégées

- scène canonique 50x50 : 7 776 octets, SHA-256
  `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3`;
- image de base LivingHive : 7 489 785 octets, SHA-256
  `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`;
- aucune scène, image de ruche, tuile ou image terrain régénérée, recadrée,
  remplacée ou modifiée.

## Synchronisation

La synchronisation officielle normale a été retentée après la clôture
documentaire. Elle a échoué avant toute copie avec `Test-Path : Accès refusé`
sur `\\DESKTOP-D3D29K7\BeeKingdomHost`. Le rapport
`.codex/vm-sync-last-report.txt` reste donc daté du 22 juillet à 02:57:51 UTC,
avec 0 conflit et 4 suppressions en attente. Toutes les modifications de ce
jalon restent sur la copie locale `C:`; aucun droit n’a été élargi et aucun accès
direct à `Z:` n’a été utilisé.
