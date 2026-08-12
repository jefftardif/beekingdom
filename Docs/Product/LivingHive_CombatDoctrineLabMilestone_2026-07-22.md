# LivingHive — laboratoire de doctrine de combat

Date : 22 juillet 2026

## Résultat produit

Le panneau `Voie stratégique` possède maintenant une entrée tactile `Contres`.
Elle ouvre un laboratoire bilingue où le joueur choisit d’abord sa famille de
formation, puis la menace observée. Le résultat distingue avantage doctrinal,
doctrine exposée ou rapport neutre et rappelle toujours qu’un contre ne garantit
jamais la victoire. Aucun dégât, coefficient, puissance, rang, perte, butin ou
résultat de combat n’est calculé.

Le cycle Bee Kingdom est fermé et sans famille dominante :

- Gardiennes (`guardians`) > Lanceuses (`darters`);
- Lanceuses (`darters`) > Voltigeuses (`wingrunners`);
- Voltigeuses (`wingrunners`) > Gardiennes (`guardians`);
- une famille contre elle-même donne un rapport neutre.

Les cinq voies stratégiques restent des identités de compte. Les trois familles
de doctrine sont des rôles de formation au combat. Le laboratoire ne choisit ni
l’une ni l’autre et ne transforme pas une voie en caste militaire. Il enseigne
seulement la lecture du triangle avant la future préparation d’escouade.

En portrait 390x844, l’introduction, les deux rangées de trois familles, le
résultat, le cycle et la frontière serveur tiennent sans défilement. En paysage
1600x900, les mêmes éléments utilisent toute la largeur. Le bouton `Contres`,
les six choix et le retour `Voies/Paths` font au moins 44 px; les cartes de
famille font 64 px en portrait et 76 px en paysage.

## Frontière appareil / serveur

- Appareil : langue, famille alliée courante et menace observée en mémoire
  volatile. Fermer la surface remet Gardiennes par défaut et efface la menace.
  Aucun `PlayerPrefs`, cache, outbox ou état hors ligne n’est créé.
- Serveur : catalogue/version autoritaires, identifiants de famille et cycle de
  contres. Les futurs coefficients, formations, rangs, compétences et résultats
  de bataille devront également rester serveur.
- Le catalogue partagé porte la version `phase4-combat-v1`.
- La lecture authentifiée préparée est
  `GET /game/v1/combat/doctrine`.
- `CombatDoctrine:Enabled=false` reste la valeur par défaut et en Production.
  Le drapeau fermé retourne `503 game.unavailable` avant authentification et
  avant lecture; lorsqu’il sera activé, l’absence de session retournera
  `401 game.session_required`.
- Le client mobile n’est pas encore raccordé à cette route. Il affiche donc
  explicitement `CATALOGUE LOCAL · serveur requis avant combat officiel`.
- Les tests HTTP `WebApplicationFactory`, SQL, TLS/IIS, transport mobile et
  staging Android restent des portes avant exposition. Aucun coefficient,
  dégât, récompense, sélection, candidat, transfert ou déploiement n’a été créé.
  `DeploymentAuthorized=false` demeure inchangé.

Le contrat serveur est documenté dans
`Docs/ProductionIntegration/LivingHive_Phase4_CombatDoctrine_Server_2026-07-22.md`.

## Validation

- Compilation jeu + éditeur : 0 erreur et 217 avertissements historiques dans
  `Artifacts/LivingHiveCombatDoctrine_InitialBuild.log`.
- Suite F8 LivingHive finale : 98 contrôles, sortie 0, marqueur
  `LivingHive manual collection checks passed` et zéro erreur de compilation
  dans `Artifacts/LivingHiveCombatDoctrine_F8_Final.log`.
- Capture Unity finale : sortie 0, marqueur
  `LivingHive strategic path proofs captured` et six images aux dimensions
  exactes dans `Artifacts/LivingHiveCombatDoctrine_Capture_Final.log`.
- Catalogues : 902/902 entrées uniques et strictement alignées entre `fr-CA` et
  `en-US`, dont 34 clés `combat_doctrine.*` par langue.
- Serveur : 38/38 tests HiveOperations et build Release sans erreur;
  avertissement SqlClient MSB3277 préexistant. Le runtime .NET 8 natif n’est pas
  présent dans la VM et aucune validation sous ce runtime n’est revendiquée.
- Processus finaux : Unity 0, dotnet 0, testhost 0.

Preuves principales inspectées à résolution native :

- `Docs/Product/Evidence/LivingHiveStrategicPath/LivingHive_CombatDoctrine_GuardiansVsDarters_FR_390x844.png`
  — 390x844, SHA-256
  `3690512669e6a5d51b75e2d30422d2ce394c9da9be2918dd548c733bddd0d839`;
- `Docs/Product/Evidence/LivingHiveStrategicPath/LivingHive_CombatDoctrine_WingrunnersVsGuardians_EN_1600x900.png`
  — 1600x900, SHA-256
  `e8e4700bd7058fa8cfd6166dc9d9d54a3202c60a7f934443cdc4e2e9e350236b`.

Les quatre vues antérieures d’aperçu et d’essai ont aussi été régénérées. Le
jalon suivant de préparation d’escouade a porté le manifeste courant à huit
preuves; leurs empreintes sont
`Docs/Product/Evidence/LivingHiveStrategicPath/LivingHiveStrategicPath_CaptureManifest.md`.

## Fondations protégées

- Carte canonique 50x50 :
  `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3`.
- Scène `LivingHive` :
  `ECCFE9AA81AE883317E4E951C8552DCEF1A156179F35480567466AB95A9708E7`.
- Image de base `background_hive.png` :
  `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`.

Les trois empreintes sont inchangées. Aucun fichier Communication n’a été
modifié et le chantier Communication est resté entièrement gelé.

## Fichiers client modifiés

- `Assets/BeeKingdom/Playground/HiveStrategicPathPresentation.cs`;
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`;
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveStrategicPathTests.cs`;
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveStrategicPathCapture.cs`;
- `Assets/_Project/Data/Localization/Resources/Localization/strings.fr-CA.json`;
- `Assets/_Project/Data/Localization/Resources/Localization/strings.en-US.json`.

## Fichiers serveur modifiés par l’Intégrateur

- `Server/src/BeeKingdom.HiveOperations/CombatDoctrine.cs`;
- `Server/src/BeeKingdom.HiveOperations/CombatDoctrineOptions.cs`;
- `Server/src/BeeKingdom.Server/Program.cs`;
- `Server/src/BeeKingdom.Server/appsettings.json`;
- `Server/src/BeeKingdom.Server/appsettings.Production.json`;
- `Server/tests/BeeKingdom.HiveOperations.Tests/CombatDoctrineTests.cs`;
- `Docs/ProductionIntegration/LivingHive_Phase4_CombatDoctrine_Server_2026-07-22.md`.

## Vérification manuelle recommandée

Ouvrir `Assets/Scenes/LivingHive.unity`, entrer en Play/Game et fermer
l’introduction. Toucher le portrait de la reine, `Voie stratégique`, puis
`Contres`. Choisir successivement les trois familles dans `Notre essaim`, puis
les trois familles dans `Menace observée`. Vérifier les trois états : avantage,
exposition et neutralité. Revenir par `Voies`, rouvrir `Contres`, puis fermer le
panneau et le rouvrir : la menace ne doit pas être conservée. Les stocks, files,
niveaux, statistiques, objectifs, voie stratégique et sélection officielle
doivent rester strictement inchangés.

## Synchronisation VM

La synchronisation officielle de fin tentée le 22 juillet 2026 à
`2026-07-22T11:37:22Z` a échoué avant toute copie : `Test-Path` reçoit
`Accès refusé` sur `\\DESKTOP-D3D29K7\BeeKingdomHost`. Le rapport
`.codex/vm-sync-last-report.txt` demeure daté de `2026-07-22T02:57:51Z`, avec
0 conflit et 4 suppressions historiques en attente. Aucun accès direct à `Z:`,
remappage ou contournement du bac à sable n’a été tenté; le jalon reste donc sur
la copie locale `C:` jusqu’à la synchronisation utilisateur.
