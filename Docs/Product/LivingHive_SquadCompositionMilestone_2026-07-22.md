# LivingHive — composition multi-famille et réservation serveur

Date : 22 juillet 2026  
Responsable : Architecte  
État : code client et serveur livré; compilation et harnais autonome verts; F8 et preuves visuelles différés par la licence Unity.

## Résultat joueur

`Armée -> Préparer` n’est plus limité au choix d’une seule famille. Le joueur peut composer un brouillon de 12 unités avec Gardiennes, Voltigeuses et Lanceuses, retirer ou ajouter chaque famille, puis demander une suggestion explicable face à la menace observée.

La suggestion n’invente aucune puissance : elle réserve environ la moitié de la capacité à la famille qui répond à la menace, puis répartit le reste entre les deux autres familles dans les limites du roster disponible. Le rapport affiche les unités qui répondent, celles qui sont exposées et celles qui sont neutres. Il ne promet jamais une victoire, ne calcule ni dégât ni perte et ne lance aucun combat.

Le bouton de réservation officielle reste désactivé. Le brouillon est volatil et la fermeture du panneau l’efface. Aucun effectif n’est réservé, consommé ou muté localement.

## Frontière mobile / serveur

Contrat commun : `phase4-combat-squad-reservation-v1`, capacité initiale `12`, clés exactes `guardians`, `wingrunners`, `darters`.

Appareil :

- rendu et contrôles tactiles;
- brouillon volatil de composition;
- suggestion et lecture doctrinale explicables;
- aucune réservation locale, aucun résultat de combat et aucune autorité sur le roster.

Serveur :

- roster disponible et révision autoritaire;
- validation des trois quantités et de la capacité;
- commit atomique qui réserve sans consommer;
- release atomique et idempotente;
- isolation stricte par joueur et ruche;
- preuve et reçus d’idempotence persistés.

Routes préparées, authentifiées et fermées par défaut et en Production avec `CombatSquadReservation:Enabled=false` :

- `GET /game/v1/hives/{hiveId}/combat/squad-reservation`;
- `POST /game/v1/hives/{hiveId}/combat/squad-reservation/commit`;
- `POST /game/v1/hives/{hiveId}/combat/squad-reservation/release`.

Le drapeau fermé répond `503 game.unavailable` avant authentification ou lecture. Aucun client mobile n’appelle encore ces routes et aucun déploiement n’est autorisé.

Rapport serveur détaillé : `Docs/ProductionIntegration/LivingHive_Phase4_CombatSquadReservation_Server_2026-07-22.md`.

## Validation acquise

- compilation C# exacte générée par Unity, assemblage jeu : 0 erreur; 11 avertissements historiques;
- compilation C# exacte générée par Unity, assemblage Éditeur : 0 erreur; avertissements d’API obsolètes historiques;
- audit binaire du présentateur restauré : 947 -> 950 champs (exactement les trois compteurs de brouillon), 1316 -> 1333 méthodes et 168 -> 171 propriétés; les trois champs attendus sont présents;
- harnais autonome du modèle et des preuves : `64/64`, contrat, recommandation `3/6/3`, plafond 12, lecture `6/3/3`, lignes d’honnêteté et cibles tactiles portrait/paysage;
- journal : `Artifacts/LivingHiveSquadComposition_Standalone64.log`;
- tests serveur après audit : sous-suite réservation/recrutement `7/7`, suite HiveOperations `47/47`, build Release 0 erreur;
- catalogues `fr-CA` et `en-US` : `949/949` entrées, 0 doublon, 0 asymétrie, 9 clés de composition et 2 clés de commit par langue.

## Incident source restauré

La première compilation de cette tranche a révélé une corruption antérieure au début de `HiveViewProductUiPresenter.cs` : une fin de méthode Splash avait remplacé les champs d’état et les types internes. Le préfixe sain a été récupéré depuis `Library/ScriptAssemblies/Assembly-CSharp.dll`, compilée à 08:15 avant la corruption, avec les références Unity résolues. Le raccord a été fait sur l’unique frontière `DrawSplashLanguageButton`; les propriétés de preuve déjà présentes dans le suffixe source ont été conservées, puis les trois champs de composition postérieurs à la DLL ont été réinsérés. Les assemblages jeu et Éditeur compilent ensuite sans erreur.

Les outils et sources de décompilation temporaires ont été supprimés après validation. Aucun terrain, image, scène ou module Communication n’a été impliqué dans cette restauration.

## Ratification Unity acquise

Deux tentatives post-restauration n’avaient exécuté aucun test : le canal
`LicenseClient-tardi` avait disparu puis perdu sa connexion pendant le
rechargement de domaine. Ces refus historiques restent conservés dans :

- `Artifacts/LivingHiveSquadComposition_RecoveryF8.log`;
- `Artifacts/LivingHiveSquadComposition_RecoveryF8_2.log`.

Après stabilisation de Unity Hub, la suite contemporaine a été rejouée avec :

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.3f1\Editor\Unity.exe' -batchmode -projectPath 'C:\projets\beekingdomgame-master' -executeMethod BeeKingdom.Playground.Editor.SandboxLivingHiveManualCollectionTests.RunAllForBatch -logFile 'C:\projets\beekingdomgame-master\Artifacts\LivingHiveSquadComposition_FinalF8.log'
```

Puis :

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.3f1\Editor\Unity.exe' -batchmode -projectPath 'C:\projets\beekingdomgame-master' -executeMethod BeeKingdom.Playground.Editor.SandboxLivingHiveStrategicPathCapture.CaptureAndExit -logFile 'C:\projets\beekingdomgame-master\Artifacts\LivingHiveSquadComposition_Capture.log'
```

Le journal final de cette exécution est
`Artifacts/LivingHivePerimeterSortie_FinalF8.log` : marqueur de succès, zéro
`error CS` et fermeture propre. Le harnais stratégique passe lui aussi avec son
marqueur de succès dans `Artifacts/LivingHiveSquadComposition_Capture.log`.

Les deux nouvelles preuves, sans recadrage ni redimensionnement, sont :

- `LivingHive_SquadComposition_Mixed_FR_390x844.png`;
- `LivingHive_SquadComposition_Mixed_EN_1600x900.png`.

Le manifeste stratégique contient 12/12 images aux dimensions exactes et leurs
SHA-256. Les deux compositions ont été inspectées à résolution native : aucun
chevauchement, aucune coupure et toutes les commandes restent visibles. La dette
de ratification Unity de cette tranche est fermée.

## Fondations protégées

- scène canonique 50x50 : `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3`;
- scène LivingHive : `ECCFE9AA81AE883317E4E951C8552DCEF1A156179F35480567466AB95A9708E7`;
- image de base LivingHive : `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`.

Ces trois empreintes correspondent aux rapports antérieurs. Le terrain 50x50, ses images et l’image de base de la ruche sont inchangés. Communication est resté entièrement gelé.

## Fichiers produit client

- `Assets/BeeKingdom/Playground/HiveSquadCompositionPresentation.cs` et `.meta`;
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`;
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveFormationReadinessTests.cs`;
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveStrategicPathCapture.cs`;
- `Assets/_Project/Data/Localization/Resources/Localization/strings.fr-CA.json`;
- `Assets/_Project/Data/Localization/Resources/Localization/strings.en-US.json`.

`DeploymentAuthorized=false`. Aucun candidat, transfert, activation ou déploiement n’a été effectué.
