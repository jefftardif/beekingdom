# LivingHive — jalon d’aperçu des voies stratégiques

Date : 22 juillet 2026

## Résultat produit

Le profil joueur de `LivingHive` ouvre maintenant un panneau tactile bilingue
présentant cinq identités de colonie au niveau 10 : Garde royale, Dard d’assaut,
Nourricière, Éclaireuse et Alchimiste. Chaque carte explique son rôle, deux
forces et un compromis. Le panneau est adapté au portrait 390x844 et au paysage
1600x900; ses cibles font au moins 56 px et il se ferme par `X`, `Escape` ou un
toucher hors surface.

Cette tranche est volontairement un aperçu. Toucher une voie change uniquement
la carte inspectée en mémoire. L’interface affiche `APERÇU LOCAL · aucun bonus
appliqué` et `Sélection officielle indisponible · serveur requis`. Elle ne
modifie ni `PlayerSkillState`, ni classe, ni bonus, ni progression, ni économie.
L’option neutre n’est pas sélectionnable.

## Frontière appareil / serveur

- Appareil : rendu, langue et voie actuellement inspectée en mémoire seulement.
- Serveur : niveau d’éligibilité, catalogue/version, sélection verrouillée,
  révision, idempotence, isolation joueur/ruche et horodatage UTC.
- Le noyau serveur persistant `phase4-v1` est livré derrière
  `StrategicPath:Enabled=false`, par défaut et en Production.
- Les routes GET/POST authentifiées ont été ajoutées au jalon suivant, mais
  restent fermées par le même drapeau et ne sont pas raccordées au client. Le
  client n’invente donc ni snapshot, ni sélection officielle, ni bonus.
- Les prochaines portes sont : tests HTTP `WebApplicationFactory`, SQL externe,
  TLS/IIS, transport mobile authentifié, staging Android, puis activation
  coordonnée.

Le contrat serveur est détaillé dans
`Docs/ProductionIntegration/LivingHive_Phase4_StrategicPath_ServerCore_2026-07-22.md`.
La mise en situation qui permet d’essayer chaque voie est documentée dans
`Docs/Product/LivingHive_StrategicPathTrialMilestone_2026-07-22.md`.

## Validation

- Compilation Unity 6000.5.3f1 : sortie 0, zéro `error CS` et zéro
  `Compilation failed` dans `Artifacts/LivingHiveStrategicPath_Compile.log`.
- Suite F8 LivingHive : 90 contrôles, sortie 0, marqueur
  `LivingHive manual collection checks passed` dans
  `Artifacts/LivingHiveStrategicPath_F8.log`.
- Compilation de secours jeu + éditeur : 0 erreur et 217 avertissements
  historiques dans `Artifacts/LivingHiveStrategicPath_DotnetBuild.log`.
- Catalogues : 833/833 entrées uniques et clés strictement alignées.
- Noyau serveur : 36/36 tests HiveOperations, build Release sans erreur;
  avertissement SqlClient préexistant.
- Processus finaux : Unity 0, dotnet 0, testhost 0.

Preuves inspectées à résolution native :

- `Docs/Product/Evidence/LivingHiveStrategicPath/LivingHive_StrategicPath_Nurturer_FR_390x844.png`
  — 390x844, SHA-256
  `f84dc187885d5c9444f8a7b621b94184305d9e067123d4ebd3b024fae6f39f0b`.
- `Docs/Product/Evidence/LivingHiveStrategicPath/LivingHive_StrategicPath_Scout_EN_1600x900.png`
  — 1600x900, SHA-256
  `749ad358f25eb618e9d1cf49babfeba581085ae447057c5d007e7c5748ca96df`.

Le manifeste est
`Docs/Product/Evidence/LivingHiveStrategicPath/LivingHiveStrategicPath_CaptureManifest.md`.
Ces images ont été régénérées lors du jalon de préparation d’escouade; le
manifeste courant contient huit preuves et remplace les empreintes antérieures.

## Fondations protégées

- Carte canonique 50x50 :
  `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3`.
- Scène `LivingHive` :
  `ECCFE9AA81AE883317E4E951C8552DCEF1A156179F35480567466AB95A9708E7`.
- Image de base `background_hive.png` :
  `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`.

Les trois hashes sont inchangés. Aucun fichier Communication n’a été modifié et
le chantier Communication est resté gelé.

## Synchronisation VM

La synchronisation officielle tentée le 22 juillet 2026 à 10:53:14 UTC a
échoué avant toute copie : accès refusé à
`\\DESKTOP-D3D29K7\BeeKingdomHost`. Le rapport
`.codex/vm-sync-last-report.txt` demeure daté de 02:57:51 UTC, avec 0 conflit et
4 suppressions historiques en attente. Aucun accès direct à `Z:`, remappage ou
contournement du bac à sable n’a été tenté; ce jalon reste donc sur la copie
locale `C:` jusqu’à la synchronisation utilisateur.

## Fichiers client

- `Assets/BeeKingdom/Playground/HiveStrategicPathPresentation.cs` et `.meta`.
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`.
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveStrategicPathTests.cs`
  et `.meta`.
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveStrategicPathCapture.cs`
  et `.meta`.
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveManualCollectionTests.cs`.
- `Assets/_Project/Data/Localization/Resources/Localization/strings.fr-CA.json`.
- `Assets/_Project/Data/Localization/Resources/Localization/strings.en-US.json`.

## Fichiers serveur

- `Server/src/BeeKingdom.HiveOperations/HiveOperationModels.cs`.
- `Server/src/BeeKingdom.HiveOperations/StrategicPathService.cs`.
- `Server/src/BeeKingdom.HiveOperations/StrategicPathOptions.cs`.
- `Server/src/BeeKingdom.Server/Program.cs`.
- `Server/src/BeeKingdom.Server/appsettings.json`.
- `Server/src/BeeKingdom.Server/appsettings.Production.json`.
- `Server/tests/BeeKingdom.HiveOperations.Tests/StrategicPathTests.cs`.
- `Docs/ProductionIntegration/LivingHive_Phase4_StrategicPath_ServerCore_2026-07-22.md`.

## Vérification manuelle recommandée

Ouvrir `Assets/Scenes/LivingHive.unity`, entrer en Play/Game et fermer
l’introduction. Toucher le portrait de la reine dans l’en-tête, puis `Voie
stratégique`. Inspecter les cinq cartes et confirmer que le détail change, mais
que les deux avertissements d’aperçu local et de serveur requis restent visibles.
Fermer par `X`, puis rouvrir et vérifier aussi `Escape` ou un toucher extérieur.
Aucune ressource, classe, statistique ou progression ne doit changer.
