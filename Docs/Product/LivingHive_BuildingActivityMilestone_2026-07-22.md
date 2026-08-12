# LivingHive — activité contextuelle des bâtiments

## Statut

La tranche est implémentée et ratifiée dans Unity 6000.5.3f1. Les panneaux de
bâtiment montrent maintenant une identité vivante et un état utile sans modifier
l’économie, la progression, le terrain, l’image de ruche ou le chantier
Communication.

## Résultat joueur

Chaque bâtiment important possède une activité cohérente avec sa fonction :

- réserve de miel : nectar en stockage;
- atelier de cire : façonnage de la cire;
- entrepôt : tri du pollen;
- nurserie : soins du couvain;
- poste de garde : patrouille des gardiennes;
- autres chambres : entretien générique de la chambre.

Le ruban contextuel apparaît dans la fiche compacte du téléphone et dans le
panneau paysage. Il reprend l’état réellement connu : remplissage ou saturation
d’un producteur, nutrition de la nurserie, sécurité, amélioration ou formation en
cours. Une production pleine rappelle que la collecte demeure manuelle.

Deux ou trois signaux légers animent le ruban selon le bâtiment. Le mouvement
réduit les fige à des positions déterministes; le mode économie conserve un seul
signal. Ces préférences ne changent jamais les abeilles affectées, les quantités,
les coûts, les minuteries ou les résultats.

## Frontière appareil / serveur

| Responsabilité | Appareil mobile | Serveur |
|---|---|---|
| Dessiner le ruban, les icônes et le mouvement | Oui | Non |
| Mouvement réduit et mode économie | Préférences locales | Aucune autorité |
| Stocks, pending, capacité et taux | Lecture/présentation | Autoritaire |
| Opération active, destination, statut et UTC | Dernier instantané reconnu | Autoritaire |
| Déduire une activité lorsque le type est précis | Oui, sans mutation | Fait source |
| Sous-état ambigu ou absent | Statut générique ou absent | Ne jamais inventer |
| Coût, gain, progression ou collecte | Aucune mutation | Autoritaire |

L’Intégrateur confirme qu’aucun nouveau contrat n’est requis. Les projections
existantes `HiveOperationResumeSummaryFactory` et
`HiveOfflineProductionSnapshotFactory` contiennent les faits nécessaires lorsque
leur type est assez précis. Les modèles ne distinguent pas encore toujours soin
et formation, ou formation et patrouille; le client doit alors rester générique.
Les drapeaux et routes restent fermés. Rapport :
`Docs/ProductionIntegration/LivingHive_BuildingContextActivity_DeviceServerBoundary_2026-07-22.md`.

## Validation

- compilation Unity jeu/éditeur : `0 error CS`, `0 Compilation failed` dans
  `Artifacts/LivingHiveBuildingActivity_Compile.log`;
- suite F8 finale : 75 contrôles réussis, aucun échec ni erreur de compilation
  dans `Artifacts/LivingHiveBuildingActivity_F8.log`;
- cinq nouveaux contrôles : catalogue/fallback, budgets mobiles, états réels et
  autorité, confort visuel sans mutation, localisation/disposition;
- compilation C# de secours incluant jeu, tests et harnais : `0 erreur`, 217
  avertissements historiques dans
  `Artifacts/LivingHiveBuildingActivity_FallbackBuild.log`;
- catalogues `fr-CA` et `en-US` : `793/793` clés uniques, alignées et sans
  doublon, dont 13 nouvelles clés `building.activity.*`;
- harnais visuel :
  `BeeKingdom.Playground.Editor.SandboxLivingHiveBuildingActivityCapture.CaptureAndExit`,
  sortie propre et dimensions strictes dans
  `Artifacts/LivingHiveBuildingActivity_Capture.log`;
- fin de validation : `Unity=0`, `dotnet=0`, `testhost=0`, `bee_backend=0`.

## Preuves visuelles

Les deux images ont été inspectées à leur résolution native. Le ruban est lisible,
ne masque ni la fermeture ni l’action principale et reste dans le panneau aux
formats téléphone et paysage.

- `LivingHive_BuildingActivity_FR_390x844.png`, `390x844`, nurserie à 64 %,
  mouvement réduit statique, SHA-256
  `9b4a38f31dc3d7819aeb951192bf9de0c143a3ad0e4c8dc33402709856d5668c`;
- `LivingHive_BuildingActivity_EN_1600x900.png`, `1600x900`, atelier à 48 %,
  mouvement ambiant, SHA-256
  `8773ebec2c31a26e39a5b832ce816f84c38e27876047d84b8de612f50c4b7c53`.

Manifeste :
`Docs/Product/Evidence/LivingHiveBuildingActivity/LivingHiveBuildingActivity_CaptureManifest.md`.

## Fondations protégées

- scène canonique 50x50 : SHA-256
  `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3`;
- scène `LivingHive.unity` : SHA-256
  `ECCFE9AA81AE883317E4E951C8552DCEF1A156179F35480567466AB95A9708E7`;
- image de base LivingHive : SHA-256
  `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`.

Aucun terrain, image de carte, image de ruche, scène ou fichier Communication
n’a été modifié.

## Fichiers exacts

- `Assets/BeeKingdom/Playground/HiveBuildingActivityPresentation.cs` et `.meta`;
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`;
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveBuildingActivityTests.cs`
  et `.meta`;
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveBuildingActivityCapture.cs`
  et `.meta`;
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveManualCollectionTests.cs`;
- catalogues `strings.fr-CA.json` et `strings.en-US.json`;
- rapport de frontière serveur de l’Intégrateur;
- ce rapport, le manifeste et les deux PNG;
- mémoire officielle LivingHive, plan d’exécution et continuation VM.

## Test manuel conseillé

1. Ouvrir `Assets/Scenes/LivingHive.unity`, passer en Play et entrer dans la
   démo locale.
2. Fermer l’introduction, puis toucher la nurserie, la réserve de miel, l’atelier
   de cire et le poste de garde.
3. Vérifier que chaque fiche affiche son activité et un état cohérent sans
   masquer la fermeture ou l’action principale.
4. Ouvrir `Plus -> Confort mobile`, activer mouvement réduit puis mode économie,
   revenir à un bâtiment et confirmer que le mouvement se fige puis se simplifie.
5. Vérifier qu’aucun solde, pending, niveau ou minuterie ne change par le simple
   affichage du ruban.

## Synchronisation VM

La commande officielle `tools/vm-sync/Synchroniser-BeeKingdom.cmd` a été tentée
après la ratification à `2026-07-22T09:45:43Z`. Elle a échoué avant toute copie
avec `Accès refusé` sur `\\DESKTOP-D3D29K7\BeeKingdomHost`. Aucun contournement,
remappage ou accès direct à `Z:` n’a été tenté.

Le dernier rapport valide reste daté du `2026-07-22T02:57:51Z`, avec 0 conflit
bloqué et 4 suppressions historiques en attente. Cette tranche est donc validée
uniquement dans la copie locale `C:\projets\beekingdomgame-master` jusqu’à la
prochaine synchronisation lancée depuis la session utilisateur.
