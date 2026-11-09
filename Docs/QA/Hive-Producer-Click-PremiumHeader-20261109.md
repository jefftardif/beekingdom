# BeeKingdom — correctifs ciblés après validation visuelle du 9 novembre 2026

## Retour utilisateur et contexte

Les captures de Nurserie, Infirmerie, Génétique, Défense et Caserne montrent
un contraste visuel important : le bouton Retour de la fenêtre partagée
affichait « Reto / ur », la bannière était trop proéminente, et la
Nurserie exposait `(server_unavailable)`. En parallèle, un clic sur
les bâtiments de production (Réserve de miel, Entrepôt, Atelier de cire)
ne présentait aucun écran.

**Attention au contexte :** l'éditeur Unity interactif de PCJEFF était
lancé depuis `C:\projets\beekingdomgame-master` (main `d74d8aa9`,
53 modifications locales protégées) alors que la refonte précédente
se trouve dans `C:\BeeKingdom\staging\BK-HudChat-Reconcile-20261105`.
La branche de cette correction reste isolée jusqu'à une intégration
sécurisée; ouvrir le mauvais projet ne permet pas de voir la correction.

## Correctifs en code

1. **Retour uniformisé avec la Caserne :**
   `HiveMapPremiumBuildingWindow.PremiumBackButton` construit désormais
   un `UnityEngine.UI.Image` véritable pour `Button.targetGraphic`,
   contrairement à l'ancien `ButtonAt` basé sur `RawImage`.
   `WorldMapHudLayout.StyleReturnButton` accepte ainsi le bouton et
   insère la flèche or de 38 pixels sur une surface de 52 pixels.
   Plus aucun texte `Retour` ou `Back` pouvant se couper.
2. **En-tête :** chaque image HD reste celle de son propre bâtiment,
   mais elle occupe seulement la partie droite et à une opacité de 14 %,
   comme accent sobre sur l'en-tête anthracite de la Caserne. Les
   boutons d'amélioration et les onglets restent inchangés.
3. **Bâtiments de production :**
   `HiveMapProductionInfoBootstrap` s'abonne maintenant à
   `BuildingInteractionController.Selection.BuildingClicked` et ouvre
   la fiche premium pour chacune des trois familles
   `HONEY_RESERVE`, `WAREHOUSE`, `TRANSFORMATION`.
   Le bouton « i » continue de fonctionner. L'abonnement est annulé
   dans `OnDestroy`. L'action existante de collecte par clic est
   conservée dans `HiveMapProductionBootstrap` — aucune récompense
   n'est créée par la nouvelle fenêtre d'information en lecture seule.
4. **Messages serveur :** la Nurserie ne montre plus l'identifiant
   technique `model.ErrorCode`. Les messages de chargement ou de
   données non confirmées sont explicites et bilingues; l'affichage
   ne présente pas un stock ou des soins fictifs. Les états « En
   attente du serveur » de l'Infirmerie/Nurserie sont simplifiés;
   les indications d'effectifs non confirmés de Défense/Génétique
   sont harmonisées dans les catalogues FR/EN. Les erreurs
   techniques demeurent accessibles aux diagnostics; ces changements
   ne prétendent pas rétablir un service distant indisponible.
5. **Anciens tests :** les tests Académie/Génétique s'appuyaient sur
   une recherche du texte `Retour`; ils vérifient désormais le
   composant `PremiumBack`, sa flèche et la fermeture réelle.
   La barrière de geste de fermeture est remise à zéro entre les
   cas de test EditMode, sans altérer son comportement dans le jeu.

## Validation

Unity 6000.5.3f1 / EditMode :
`C:\BeeKingdom\qa\BK-Hive-ClickHeader-Release-20261109.xml`
— **101/101 tests PASS**, zéro erreur de compilation, couvrant
14 fenêtres de bâtiments, la navigation, le rendu des boutons et
la nouvelle ouverture des producteurs.

La vérification des 5 685 fichiers du manifeste Unity est conservée
dans `C:\BeeKingdom\qa\BK-Hive-ClickHeader-AssetGate-20261109.json`.

**Limites :** tests de comportement et de hiérarchie UI, pas encore
une démonstration pixel par pixel dans la GameView de l'utilisateur;
il faudra rouvrir la bonne version dans Unity pour revoir les captures.
Le compte du jeu et les routes API de la Nurserie n'ont pas été
testés en session authentifiée durant ce lot.
Aucune migration SQL ni modification de la production SRVESDT.

## Prochain test utilisateur

Sur PCJEFF, ouvrir dans Unity le projet
`C:\BeeKingdom\staging\BK-HudChat-Reconcile-20261105`
(branch `fix/hive-building-click-premium-header-20261109`) et
lancer `Environment2D5D_HiveMap_Test`.
Tester les trois producteurs par clic sur le bâtiment puis les boutons
Retour/en-tête de Nurserie, Infirmerie, Génétique et Défense.
Ne pas confondre cette copie d'intégration avec la fenêtre Unity
ouverte sur `C:\projets\beekingdomgame-master`.
