# BeeKingdom — producteurs et illustration de Banque (10 novembre 2026)

**Branche :** `fix/hive-producer-pointers-bank-banner-20261110`, issue de `fix/hive-splash-auth-tabs-click-20261109`.

## Constats issus de la validation dans GameView

- La **Banque s'ouvre correctement**. Il ne faut ni remplacer sa fenêtre, ni perturber ses dépôts, retraits, progression ou niveaux. Le défaut constaté est **l'absence d'image dans son en-tête**.
- La **Réserve de miel, l'Entrepôt et l'Atelier de cire** ne donnent aucune réponse au clic sur le bâtiment ou sur leurs petits « i », malgré la présence de leurs marqueurs dans la scène.
- Plusieurs fenêtres ont une image d'en-tête étirée et tellement assombrie qu'elle ne ressemble pas à l'illustration HD du bâtiment.
- Le projet Unity réellement ouvert est `C:\BeeKingdom\staging\BK-HudChat-Reconcile-20261105`; `main` n'est pas l'éditeur courant. Le travail est séparé des modifications locales de `main`.

## Corrections apportées

1. **Banque :** `HiveViewProductUiPresenter.Bank.cs` charge son propre `PremiumBeeReference/BuildingBanners/hive_bank` (1672 × 941 pixels), et le montre dans l'en-tête à proportions conservées. La bannière est placée à gauche du bouton Améliorer, derrière les commandes. Aucune opération bancaire modifiée.
2. **En-têtes communs :** `HiveMapPremiumBuildingWindow.cs` introduit `HeaderArtworkCropForProof`, qui recadre au centre l'illustration selon son aspect natif plutôt que l'étirer dans une bande 7:1. L'image est plus visible, les boutons ne sont pas masqués, l'illustration reste celle du bâtiment correspondant.
3. **Producteurs :** `HiveMapProductionInfoBootstrap.Pointer.cs` ajoute un chemin de clic indépendant de l'ancien `GUI.Button` (souris et tactile). Le hitbox du « i » est un carré de 44 px déplacé juste sous le HUD, identique à celui utilisé pour le dessin. `LateUpdate` n'intervient que lorsque la chaîne normale de sélection n'a pas déjà ouvert de modal, pour préserver les callbacks de collecte existants. En dernier recours, le raycast des bâtiments prime, avec refus si une autre cible comme la Banque est touchée. Seul le visuel du producteur peut servir de repli si aucun collider d'un autre bâtiment n'a été atteint.
4. **Propriété du clic :** `HiveMapOverlayInputGateBootstrap` bloque le raycast 3D lorsque le joueur clique sur un « i », pour empêcher une collecte involontaire derrière le bouton d'information.
5. **Diagnostic sûr :** des traces Editor-only `[BK-ProducerInfo]` indiquent si le clic provient du bâtiment, du « i » ou du visuel, et si le panneau a pu s'ouvrir. Aucune information de compte n'est journalisée.

## Tests et limites

- Compilation de l'assemblage **Assembly-CSharp** avec la nouvelle classe partielle : **PASS, 0 erreur**.
- Compilation de l'assemblage **Assembly-CSharp-Editor** avec la nouvelle batterie de tests : **PASS, 0 erreur**.
- Les fichiers projets C# générés ont été modifiés temporairement pour la compilation puis restaurés bit pour bit.
- La nouvelle batterie vérifie les zones tactiles des « i », les marges HUD, la conversion des coordonnées, l'existence de la bannière propre à la Banque et son absence de chevauchement avec Améliorer, ainsi que les proportions des autres bannières et le maintien de la collecte historique.
- **Les nouveaux tests EditMode ne sont pas encore exécutés dans Unity Test Runner** : l'éditeur interactif est ouvert dans cette même copie de projet. La compilation n'est pas une preuve de rendu réel.
- Le rendu visuel de la Banque et le clic utilisateur réel sur les trois producteurs doivent encore être confirmés après avoir arrêté et relancé Play dans Unity.
- Aucun changement SRVESDT, API, SQL, compte ni `main`.

## Essai utilisateur

Après compilation Unity : arrêter Play, attendre le recompilateur, relancer. Cliquer d'abord sur le « i » de la Réserve de miel, puis sur le bâtiment, et observer si une fiche premium s'ouvre. Faire de même pour Entrepôt et Atelier. Ouvrir enfin la Banque : l'illustration HD doit apparaître dans l'en-tête, sans masquer le bouton Améliorer. S'il reste un blocage, noter dans la Console la dernière ligne `[BK-ProducerInfo]`.
