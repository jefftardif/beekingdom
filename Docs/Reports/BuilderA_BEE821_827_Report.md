# Builder-A BEE-821 à BEE-827 Report

Date : 2026-07-12  
Rôle : Builder-A / Unity runtime  
Statut : Completed with recommendations  
READY_FOR_DEMO_067 = YES

## Résumé

Tranche BEE-821 à BEE-827 implémentée côté Unity pour consolider la Ruche jouable produit preview. Le travail reste volontairement centré sur la Ruche : polish téléphone portrait, polish tablette paysage, protocole tactile/preuve, rapid tap Améliorer, rapid tap entraînement et checks déterministes coût/queue/troupes/niveau.

Aucun travail BEE-828+, aucune grosse carte monde, aucun serveur live, aucune sauvegarde officielle, aucune économie officielle et aucune armée persistante officielle.

## Fichiers créés

* `Assets/BeeKingdom/Playground/Editor/SandboxBee827PlayableHiveAutomationTests.cs`
* `Assets/BeeKingdom/Playground/Editor/SandboxBee827PlayableHiveProductTrancheCapture.cs`
* `Docs/Reports/BuilderA_BEE821_827_Report.md`

## Fichiers modifiés

* `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`

## Décisions d'architecture

* La boucle reste dans la surface Ruche existante, sans nouvel écran ni nouvelle logique serveur.
* Les preuves BEE-821 à BEE-827 sont exposées via méthodes `ForProof` et capture Editor DEMO-067.
* Les tests automatisés vérifient les invariants locaux sans déclarer de progression officielle.
* Le portrait téléphone utilise un bottom sheet plus haut et plus lisible, tout en gardant la Ruche visible.
* Le paysage tablette garde la Ruche dominante avec HUD, rails, panneau et navigation fixes.

## APIs publiques ajoutées

* `HiveViewProductUiPresenter.PlayableHiveDeviceTouchProtocolForProof()`
* `SandboxBee827PlayableHiveAutomationTests`
* Menu Editor : `Bee Kingdom/Playground/Capture DEMO-067 BEE-821-827 Source`

## Changements importants

* Bornes de zoom Ruche ajustées pour téléphone portrait et tablette paysage.
* Rectangles UI fixes ajustés pour empêcher les gestes Ruche sous HUD, panneau et navigation.
* Télémétrie geste enrichie : pan only, pinch only, HUD/panneaux/navigation fixes, seuils, damping, vitesse max.
* Checks déterministes enrichis : double tap upgrade, double tap training, coût unique, queue cohérente, niveau +1, troupes +1 lot.
* HUD ressources enrichi avec feedback court de tick local.

## Compatibilité

Respecte ARCH-180, UI-065, Builder-B support et Builder-C regression matrix pour la tranche BEE-821 à BEE-827 uniquement.

Non-claims conservés :

* Progression serveur officielle : false
* Sauvegarde officielle : false
* Économie officielle : false
* Armée persistante officielle : false
* Carte monde étendue : false
* BEE-828+ : non implémenté

## Tests

Tests ajoutés :

* `SandboxBee827PlayableHiveAutomationTests.RapidTapUpgradeTrainingAndDeterministicLoopChecksRemainGuarded`
* `SandboxBee827PlayableHiveAutomationTests.HiveDeviceTouchProtocolKeepsPreviewNonServerScope`
* `SandboxBee827PlayableHiveAutomationTests.GestureTelemetryExposesPanPinchAndFixedUiRules`

Résultat local : à exécuter dans Unity Editor/Test Runner.

## Compilation

Non confirmée par Unity dans ce tour. Les fichiers C# ont été préparés pour compilation Unity Editor.

## Preuves DEMO-067

Captureur ajouté :

* `Assets/BeeKingdom/Playground/Editor/SandboxBee827PlayableHiveProductTrancheCapture.cs`

Chemins générés par le captureur Unity :

* `C:/projets/beekingdom/prompt_demo/rapports/DEMO-067_BEE821_827_Source/BEE827_01_PhonePortrait_390x844.png`
* `C:/projets/beekingdom/prompt_demo/rapports/DEMO-067_BEE821_827_Source/BEE827_02_TabletLandscape_1280x720.png`
* `C:/projets/beekingdom/prompt_demo/rapports/DEMO-067_BEE821_827_Source/BEE827_03_OneFingerPan_1280x720.png`
* `C:/projets/beekingdom/prompt_demo/rapports/DEMO-067_BEE821_827_Source/BEE827_04_TwoFingerPinch_1280x720.png`
* `C:/projets/beekingdom/prompt_demo/rapports/DEMO-067_BEE821_827_Source/BEE827_05_UiBlocksHiveGesture_1280x720.png`
* `C:/projets/beekingdom/prompt_demo/rapports/DEMO-067_BEE821_827_Source/BEE827_06_RapidTapUpgrade_1280x720.png`
* `C:/projets/beekingdom/prompt_demo/rapports/DEMO-067_BEE821_827_Source/BEE827_07_RapidTapTraining_1280x720.png`
* `C:/projets/beekingdom/prompt_demo/rapports/DEMO-067_BEE821_827_Source/DEMO-067_BEE821_827_Manifest.md`

## Limitations

* La preuve tactile physique réelle doit encore être produite par Demo/QA sur appareil.
* Les captures ne sont pas générées dans ce tour hors Unity Play Mode.
* Le rapport officiel demandé dans `C:/projets/beekingdom/prompts_codex/rapports` sera généré par le captureur Unity ; un miroir est fourni ici dans le workspace.

## Recommandations

* Exécuter le menu DEMO-067 dans Unity Editor pour produire les captures et le manifeste aux chemins officiels.
* Exécuter les tests Editor ajoutés avant QA-067.
* Garder BEE-828+ bloquée jusqu'au retour Demo/QA de cette tranche.

## Risques

* La preuve tactile réelle reste dépendante d'un appareil physique.
* Le presenter Ruche reste dense ; une extraction future du modèle local rendrait les tests encore plus robustes.
* Les captures Editor ne remplacent pas une validation tactile tablette/téléphone.

## Ready for next brick

YES, pour DEMO-067 / QA-067 uniquement.  
BEE-828+ doit rester en attente.
