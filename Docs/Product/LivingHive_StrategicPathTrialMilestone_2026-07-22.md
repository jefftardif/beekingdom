# LivingHive — jalon d’essais tactiques des voies stratégiques

Date : 22 juillet 2026

## Résultat produit

L’aperçu des cinq voies stratégiques permet maintenant d’essayer leur logique
avant tout choix officiel. Depuis le profil de la reine, `Voie stratégique` puis
`Essayer ce style` ouvre une mise en situation courte avec deux réactions
réversibles. La réponse explique soit l’affinité avec la voie, soit le compromis
qu’elle expose. Il ne s’agit pas d’un faux combat : aucun résultat tactique,
stock, bonus, classe, statistique, progression ou objectif n’est modifié.

Les cinq situations sont propres au monde des abeilles :

- Garde royale : tenir le passage du couvain plutôt que poursuivre une
  éclaireuse ennemie;
- Dard d’assaut : concentrer l’essaim sur la meneuse plutôt que diluer la
  pression;
- Nourricière : stabiliser une escorte épuisée avant de la relancer;
- Éclaireuse : lire le vent et baliser un repli avant de choisir la route;
- Alchimiste : préparer une réaction de propolis avant l’impact.

En portrait 390x844, les cinq voies, la situation, les deux réponses et le
résultat tiennent dans la surface sans défilement. En paysage 1600x900, la liste
reste à gauche et l’essai à droite. Le lancement et le retour font au moins
44 px; les réactions font 50 px en portrait et 56 px en paysage. Fermer le
panneau efface la réaction observée.

## Frontière appareil / serveur

- Appareil : langue, voie inspectée, scénario rendu et unique réponse courante
  en mémoire volatile. Il n’existe ni `PlayerPrefs`, ni cache, ni outbox, ni
  écriture hors ligne pour cet essai.
- Serveur : appartenance joueur/ruche, éligibilité niveau 10, catalogue
  `phase4-v1`, sélection officielle verrouillée, révision, idempotence et UTC.
- Les routes authentifiées préparées sont
  `GET /game/v1/hives/{hiveId}/strategic-path` et
  `POST /game/v1/hives/{hiveId}/strategic-path` avec
  `{pathId, expectedRevision, idempotencyKey}`.
- `StrategicPath:Enabled=false` reste la valeur par défaut et en Production.
  Le drapeau est vérifié avant authentification, lecture ou mutation; la route
  retourne alors `503 game.unavailable` sans toucher l’état.
- Le client mobile n’est pas encore raccordé à ces routes. Il ne prétend donc
  connaître ni sélection officielle ni bonus.
- Les tests HTTP `WebApplicationFactory`, SQL externe, TLS/IIS, transport mobile
  authentifié et staging Android restent des portes avant toute activation.
  `DeploymentAuthorized=false` demeure inchangé; aucun candidat, transfert ou
  déploiement n’a été effectué.

Le contrat serveur courant est décrit dans
`Docs/ProductionIntegration/LivingHive_Phase4_StrategicPath_ServerCore_2026-07-22.md`.
L’audit de frontière antérieur est historique et est supplanté par ce contrat.

## Validation

- Compilation de secours jeu + éditeur : 0 erreur et 217 avertissements
  historiques dans `Artifacts/LivingHiveStrategicTrial_DotnetBuild.log`.
- Suite F8 LivingHive finale : 94 contrôles, sortie 0, marqueur
  `LivingHive manual collection checks passed` et aucune erreur de compilation
  dans `Artifacts/LivingHiveStrategicTrial_F8_Final.log`.
- Capture Unity : sortie 0 et marqueur
  `LivingHive strategic path proofs captured` dans
  `Artifacts/LivingHiveStrategicTrial_Capture.log`.
- Catalogues : 868/868 entrées uniques, strictement alignées entre `fr-CA` et
  `en-US`.
- Serveur : 36/36 tests HiveOperations et build Release sans erreur;
  avertissement SqlClient préexistant. Aucune exécution sous runtime .NET 8
  natif n’est revendiquée.
- Processus finaux : Unity 0, dotnet 0, testhost 0.

Preuves inspectées à résolution native :

- `Docs/Product/Evidence/LivingHiveStrategicPath/LivingHive_StrategicPath_Nurturer_FR_390x844.png`
  — 390x844, SHA-256
  `f84dc187885d5c9444f8a7b621b94184305d9e067123d4ebd3b024fae6f39f0b`;
- `Docs/Product/Evidence/LivingHiveStrategicPath/LivingHive_StrategicTrial_Nurturer_FR_390x844.png`
  — 390x844, SHA-256
  `4afefe798e99642db305809ce5031273b8923f508e4ddf7128fc1a33363152c8`;
- `Docs/Product/Evidence/LivingHiveStrategicPath/LivingHive_StrategicPath_Scout_EN_1600x900.png`
  — 1600x900, SHA-256
  `749ad358f25eb618e9d1cf49babfeba581085ae447057c5d007e7c5748ca96df`;
- `Docs/Product/Evidence/LivingHiveStrategicPath/LivingHive_StrategicTrial_Scout_EN_1600x900.png`
  — 1600x900, SHA-256
  `daef2b9e68acf282101150bbf44d8780c0e354207b494a5eb927353e78df9961`.

Le manifeste est
`Docs/Product/Evidence/LivingHiveStrategicPath/LivingHiveStrategicPath_CaptureManifest.md`.
Ces quatre images ont été régénérées lors du jalon de préparation d’escouade;
le manifeste courant contient aussi les deux preuves du laboratoire et les deux
preuves de préparation, soit huit images, et remplace les empreintes antérieures.

## Fondations protégées

- Carte canonique 50x50 :
  `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3`.
- Scène `LivingHive` :
  `ECCFE9AA81AE883317E4E951C8552DCEF1A156179F35480567466AB95A9708E7`.
- Image de base `background_hive.png` :
  `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`.

Les trois empreintes sont inchangées. Aucun fichier Communication n’a été
modifié; le chantier Communication est resté entièrement gelé.

## Fichiers client modifiés

- `Assets/BeeKingdom/Playground/HiveStrategicPathPresentation.cs`;
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`;
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveStrategicPathTests.cs`;
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveStrategicPathCapture.cs`;
- `Assets/_Project/Data/Localization/Resources/Localization/strings.fr-CA.json`;
- `Assets/_Project/Data/Localization/Resources/Localization/strings.en-US.json`.

## Fichiers serveur modifiés par l’Intégrateur

- `Server/src/BeeKingdom.HiveOperations/HiveOperationModels.cs`;
- `Server/src/BeeKingdom.HiveOperations/StrategicPathService.cs`;
- `Server/src/BeeKingdom.HiveOperations/StrategicPathOptions.cs`;
- `Server/src/BeeKingdom.Server/Program.cs`;
- `Server/src/BeeKingdom.Server/appsettings.json`;
- `Server/src/BeeKingdom.Server/appsettings.Production.json`;
- `Server/tests/BeeKingdom.HiveOperations.Tests/StrategicPathTests.cs`;
- `Docs/ProductionIntegration/LivingHive_Phase4_StrategicPath_ServerCore_2026-07-22.md`.

## Vérification manuelle recommandée

Ouvrir `Assets/Scenes/LivingHive.unity`, entrer en Play/Game et fermer
l’introduction. Toucher le portrait de la reine, puis `Voie stratégique`.
Parcourir les cinq cartes, ouvrir `Essayer ce style` et choisir successivement
les deux réactions d’une même situation. Le libellé doit alterner entre
`Style révélé` et `Compromis exposé`. Revenir au profil, fermer le panneau puis
le rouvrir : aucune réaction ne doit rester choisie et `Sélection officielle`
doit encore indiquer `serveur requis`. Les stocks, niveaux, files, statistiques
et objectifs doivent rester strictement inchangés.

## Synchronisation VM

La synchronisation officielle de fin tentée le 22 juillet 2026 à
`2026-07-22T11:18:08Z` a échoué avant toute copie : `Test-Path` reçoit
`Accès refusé` sur `\\DESKTOP-D3D29K7\BeeKingdomHost`. Le rapport
`.codex/vm-sync-last-report.txt` demeure daté de `2026-07-22T02:57:51Z`, avec
0 conflit et 4 suppressions historiques en attente. Aucun accès direct à `Z:`,
remappage ou contournement du bac à sable n’a été tenté; le jalon reste donc sur
la copie locale `C:` jusqu’à la synchronisation utilisateur.
