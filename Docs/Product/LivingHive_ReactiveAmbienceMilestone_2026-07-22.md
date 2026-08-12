# LivingHive — jalon ambiance réactive

Date : 22 juillet 2026

## Résultat produit

La ruche réagit maintenant à deux faits suffisamment fiables pour être rendus
sans inventer d'état : une amélioration active et une production arrivée à
capacité. L'amélioration prend priorité si les deux faits coexistent. Le
bâtiment concerné reçoit un halo borné, des repères de ressource et, pour la
construction, de courts éclats radiaux. Ces éléments ne captent aucun clic.

Les préférences mobiles existantes sont respectées : mouvement réduit fige la
phase; mode économie limite la couche à un signal. Aucun agent, minuterie,
stock, niveau, résultat ou règle de collecte n'est ajouté ou modifié. La
collecte reste manuelle dans le bâtiment.

## Frontière appareil / serveur

- Appareil : rendu, phase d'animation et préférences mouvement réduit / économie.
- Serveur officiel requis : opération d'amélioration reconnue, bâtiment cible,
  pending, capacité, taux et temps UTC.
- État actuel de la démo : `local_preview_non_official`; il ne doit pas être
  présenté comme une preuve serveur.
- Météo : désactivée, car aucun snapshot fiable n'existe.
- Soin/couvain et alerte défense : génériques ou cachés, car les contrats
  actuels ne distinguent pas honnêtement ces sous-états.

L'audit de frontière est consigné dans
`Docs/ProductionIntegration/LivingHive_ReactiveAmbience_DeviceServerBoundary_2026-07-22.md`.
Aucun endpoint, contrat, candidat, déploiement ou fichier `Server/` n'a été
ajouté par ce jalon.

## Validation

- Compilation Unity 6000.5.3f1 : sortie 0, zéro `error CS` dans
  `Artifacts/LivingHiveReactiveAmbience_Compile.log`.
- Suite F8 LivingHive : 85 contrôles, sortie 0, marqueur
  `LivingHive manual collection checks passed` dans
  `Artifacts/LivingHiveReactiveAmbience_F8.log`.
- Compilation de secours jeu + éditeur : 0 erreur et 217 avertissements
  historiques dans `Artifacts/LivingHiveReactiveAmbience_DotnetBuild.log`.
- Catalogues : 793/793 entrées uniques, clés strictement alignées.
- Capture Unity : sortie 0 dans
  `Artifacts/LivingHiveReactiveAmbience_Capture.log`.
- Processus finaux : Unity 0, dotnet 0, testhost 0.

Preuves inspectées à résolution native :

- `Docs/Product/Evidence/LivingHiveReactiveAmbience/LivingHive_ReactiveAmbience_Upgrade_FR_390x844.png`
  — 390x844, SHA-256
  `be6cf139ff73f39a293c0a3bfd7c100233b51ce3daa968b05702e0488a3cce02`.
- `Docs/Product/Evidence/LivingHiveReactiveAmbience/LivingHive_ReactiveAmbience_FullProduction_EN_1600x900.png`
  — 1600x900, SHA-256
  `19fd929116c531ffa4208a5344762b5e1ebe3884caca0cf5820ce5435ad9c102`.

## Fondations protégées

- Carte canonique 50x50 :
  `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3`.
- Scène `LivingHive` :
  `ECCFE9AA81AE883317E4E951C8552DCEF1A156179F35480567466AB95A9708E7`.
- Image de base `background_hive.png` :
  `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`.

Les trois hashes sont inchangés. Communication est restée entièrement gelée.

## Synchronisation VM

La synchronisation officielle tentée le 22 juillet 2026 à 10:19:53 UTC a
échoué avant toute copie : accès refusé à
`\\DESKTOP-D3D29K7\BeeKingdomHost`. Le rapport
`.codex/vm-sync-last-report.txt` reste daté de 02:57:51 UTC, avec 0 conflit et
4 suppressions historiques en attente. Aucun accès direct à `Z:` ni
contournement du bac à sable n'a été tenté; ce jalon demeure donc sur la copie
locale `C:` jusqu'à la synchronisation utilisateur.

## Fichiers de produit

- `Assets/BeeKingdom/Playground/HiveReactiveAmbiencePresentation.cs` et `.meta`.
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`.
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveReactiveAmbienceTests.cs`
  et `.meta`.
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveReactiveAmbienceCapture.cs`
  et `.meta`.
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveManualCollectionTests.cs`.

## Vérification manuelle recommandée

Ouvrir `Assets/Scenes/LivingHive.unity`, entrer en Play/Game et fermer
l'introduction. Lancer une amélioration de bâtiment : le halo et les éclats
doivent suivre uniquement le bâtiment réellement engagé. Laisser ensuite une
production atteindre sa capacité : le bâtiment plein doit pulser sans créditer
le HUD; toucher son marqueur doit seulement ouvrir la fiche et la collecte doit
encore exiger l'action manuelle normale.
