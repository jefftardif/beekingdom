# LivingHive — prévision de production mobile

## Statut

La tranche est implémentée et ratifiée dans Unity 6000.5.3f1. Le joueur sait
maintenant quand revenir avant qu’un bâtiment de production ne sature, sans
collecte automatique, sans crédit au HUD et sans modification des fondations
visuelles ou du chantier Communication.

## Résultat joueur

Le panneau `Sac & stocks` montre désormais, pour le miel, la cire et le pollen :

- le solde disponible dans la ruche;
- la quantité encore dans le bâtiment sur sa capacité locale;
- le taux produit par heure;
- le temps restant avant que ce bâtiment soit plein;
- un résumé `Premier stock plein dans ...` pour planifier le prochain retour.

Un bâtiment déjà plein annonce honnêtement qu’une collecte manuelle est
disponible. Un taux ou une capacité invalide affiche `Prévision indisponible`.
Le bouton `Voir`, de 44 px, ouvre le bâtiment correspondant sans collecter,
modifier le pending, créditer une ressource ou compléter une tâche.

Le calcul `ManualProductionForecast` est pur et déterministe :

`secondes = (capacité - pending) / taux_par_heure × 3600`

Il borne les nombres non finis et négatifs, plafonne le pending à la capacité,
distingue `Producing`, `Full` et `Unavailable`, puis choisit le premier stock qui
sera plein parmi les trois producteurs.

## Frontière appareil / serveur

| Responsabilité | Appareil mobile | Serveur |
|---|---|---|
| Afficher et rafraîchir le décompte | Oui | Non |
| Heure de référence UTC | Lecture seulement | Autoritaire |
| Taux, capacité, pending par bâtiment | Lecture seulement | Autoritaire |
| Calcul visuel « temps avant plein » | Dérivé du snapshot | Peut être vérifié |
| Collecte, soldes et progression | Aucune mutation depuis ce panneau | Autoritaire |
| Navigation `Voir` | Locale, sans collecte | Aucune mutation |

L’Intégrateur a confirmé que `HiveOfflineProductionSnapshotFactory` fournit déjà
`ServerUtc`, `ProductionAsOfUtc`, durée reconnue bornée, version de catalogue,
taux, capacité, pending plafonné et `BuildingKey`. Aucun nouveau contrat serveur
n’est requis pour cette tranche.

La porte reste honnête : `HiveOfflineProduction:Enabled=false`, aucune route HTTP
n’est exposée et le modèle durable ne persiste pas encore le marqueur/pending par
bâtiment. Le snapshot serveur actuel est donc une projection bornée, pas encore
une comptabilisation durable de production. Rapport :
`Docs/ProductionIntegration/LivingHive_ProductionForecast_DeviceServerBoundary_2026-07-22.md`.

## Validation

- compilation Unity jeu/éditeur : sortie propre, `0 error CS` et
  `0 Compilation failed` dans
  `Artifacts/LivingHiveProductionForecastCompile.log`;
- suite F8 finale : 70 contrôles réussis, aucun échec ni erreur de compilation
  dans `Artifacts/LivingHiveProductionForecastF8.log`;
- cinq nouveaux contrôles : calcul exact, bornage/états, premier stock plein,
  frontières d’autorité, navigation sans collecte et disposition/localisation;
- compilation C# de secours incluant jeu, tests et harnais : `0 erreur`, 217
  avertissements historiques dans
  `Artifacts/LivingHiveProductionForecastFallbackBuild.log`;
- catalogues `fr-CA` et `en-US` : `780/780` clés uniques, alignées et sans
  doublon, dont 11 nouvelles clés `ledger.forecast.*`;
- harnais visuel :
  `BeeKingdom.Playground.Editor.SandboxLivingHiveProductionForecastCapture.CaptureAndExit`,
  sortie propre et dimensions strictes dans
  `Artifacts/LivingHiveProductionForecastCapture.log`;
- fin de validation : `Unity=0`, `dotnet=0`, `testhost=0`, `bee_backend=0`.

## Preuves visuelles

Les deux images ont été inspectées à leur résolution native. Les trois lignes,
le résumé de prochain retour, les boutons `Voir`, la fermeture et les panneaux
de capacité/engagements sont lisibles sans collision. En paysage, la barre
Communication reste intacte et honnêtement `NotConfigured`.

- `LivingHive_ProductionForecast_FR_390x844.png`, `390x844`, décomptes
  39/45/46 minutes, SHA-256
  `f3aed17d3ce72d6d4eeff2f9ad9d311662c4d8845dfd6e1d0a25a5c2a2a57491`;
- `LivingHive_ProductionForecast_EN_1600x900.png`, `1600x900`, décomptes
  46/25/32 minutes, SHA-256
  `77e0b2b10c92fe3d31852b4e82dd2af3ac3cf84f202e68c677ba4dac4b3c80ee`.

Manifeste :
`Docs/Product/Evidence/LivingHiveProductionForecast/LivingHiveProductionForecast_CaptureManifest.md`.

## Fondations protégées

- scène canonique 50x50 : 7 776 octets, SHA-256
  `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3`;
- scène `LivingHive.unity` : 9 160 octets, SHA-256
  `ECCFE9AA81AE883317E4E951C8552DCEF1A156179F35480567466AB95A9708E7`;
- image de base LivingHive : 7 489 785 octets, SHA-256
  `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`.

Aucun terrain, image de carte, image de ruche, scène ou fichier Communication
n’a été modifié.

## Fichiers exacts

- `Assets/BeeKingdom/Playground/ManualProductionForecast.cs` et `.meta`;
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`;
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveProductionForecastTests.cs`
  et `.meta`;
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveProductionForecastCapture.cs`
  et `.meta`;
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveManualCollectionTests.cs`;
- catalogues `strings.fr-CA.json` et `strings.en-US.json`;
- ce rapport, le manifeste et les deux PNG;
- rapport de frontière serveur de l’Intégrateur;
- mémoire officielle LivingHive, plan d’exécution et continuation VM.

## Test manuel conseillé

1. Ouvrir `Assets/Scenes/LivingHive.unity`, passer en Play et entrer dans la
   démo locale.
2. Fermer l’introduction puis toucher `Sac`.
3. Vérifier que les trois producteurs montrent pending/capacité, taux horaire et
   un temps avant plein, avec un résumé du premier stock à saturation.
4. Noter le solde et le pending du miel, toucher `Voir`, puis confirmer que la
   réserve de miel s’ouvre sans modifier ces deux valeurs.
5. Collecter ensuite manuellement l’icône du bâtiment et vérifier que le HUD ne
   change qu’à ce moment.

## Synchronisation VM

La commande officielle `tools/vm-sync/Synchroniser-BeeKingdom.cmd` a été tentée
après la ratification, puis une dernière fois sur l’état final à
`2026-07-22T09:28:54Z`. Elle a échoué avant toute copie
avec `Accès refusé` sur `\\DESKTOP-D3D29K7\BeeKingdomHost`. Aucun contournement,
remappage ou accès direct à `Z:` n’a été tenté.

Le dernier rapport valide reste daté du `2026-07-22T02:57:51Z`, avec 0 conflit
bloqué et 4 suppressions historiques en attente. Cette tranche est donc validée
uniquement dans la copie locale `C:\projets\beekingdomgame-master` jusqu’à la
prochaine synchronisation officielle réussie.
