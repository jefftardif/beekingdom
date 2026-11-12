# BeeKingdom — icônes HD des cartes de production

**Date :** 12 novembre 2026
**Projet :** `C:\projets\beekingdomgame-master`
**Portée :** Réserve de miel, Entrepôt et Atelier de cire.

## Défaut observé

Sur la capture GameView de la Réserve de miel, les icônes des sections « Ressources et capacité » et « Suivi de production » sont de petites images à cadre doré qui paraissent tronquées et moins soignées que les autres illustrations premium.

L'ancien `CardAt` affichait toutes les icônes par `RawImage` dans un rectangle fixe de 48 × 48, sans ajustement des proportions ni espace suffisant autour du cadre. Les cartes de production utilisaient `PremiumBeeIcons/inventory` et `PremiumBeeIcons/research`.

## Correctif partagé

- `HiveMapPremiumBuildingWindow.CardAt` accepte un mode `spaciousPremiumHeader`, par défaut désactivé : les cartes des autres bâtiments conservent donc strictement leur disposition existante.
- Les trois cartes de production utilisent ce mode via le composant partagé : en-tête à 84 unités, cadre 72 × 72, marge interne de 6 unités et texture dessinée en mode **contain**, sans recadrage des bordures décoratives.
- `PremiumCardIconContainRectForProof` calcule une géométrie déterministe, centrée et conservant le ratio source. La texture `RawImage` occupe toute sa plage UV, sans masquer des parties de l'icône. Les images décoratives ne reçoivent pas de clic.
- « Ressources et capacité » utilise l'emblème premium `PremiumBeeIcons/resources` (nid d'abeilles or). « Suivi de production » utilise `PremiumBeeIcons/book` (livre de gestion or). L'icône d'état de production conserve l'identité de ressource du bâtiment.
- Les titres disposent de plus de place et de réduction automatique de taille pour les petites fenêtres. Les statistiques, le bouton Actualiser et les notes sont repositionnés sous les nouveaux en-têtes, sans changement aux appels du serveur.
- `resources.png` et son `.meta` sont ajoutés explicitement à Git pour qu'une installation neuve dispose de la même illustration ; l'icône `book` était déjà versionnée.

## Validation

- Vérification de la compilation du jeu et du projet de tests C# avec **0 erreur**.
- Nouveau jeu de tests `HivePremiumProductionCardIconTests` : présence des ressources, respect du ratio et absence de découpe pour plusieurs proportions, et structure des trois cartes dans les trois fenêtres de production.
- Contrôle Git : aucun autre fichier du projet principal n'est introduit dans ce changement.
- Le rendu visuel final à 1920 × 1080 et en format mobile reste à confirmer dans la GameView; une compilation ne prouve pas à elle seule la qualité au pixel près.
- Aucune mutation de production, migration SQL, modification du serveur SRVESDT ou publication Android.

## Vérification joueur

Dans Unity sur le projet principal : arrêter Play, laisser Unity recompiler, puis relancer. Ouvrir la Réserve de miel, l'Entrepôt et l'Atelier de cire. Les trois icônes de section doivent être entières, visuellement uniformes, et les montants/rates de production doivent rester lisibles. Tester aussi un format portrait de téléphone.
