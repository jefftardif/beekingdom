# BeeKingdom — occupation stable de la World Map (27 octobre 2026)

## Périmètre de la branche

Branche : fix/worldmap-stable-world-occupation-20261027.

Cette branche contient un correctif ciblé du placement initial des entités **locales héritées** dans le monde. Elle part de main (d74d8aa9) et ne récupère aucun fichier modifié ou non suivi appartenant aux autres agents dans le worktree principal.

Le code :
- Réserve la position de la ruche du joueur avant les autres chunks, puis traite ruches, ressources, bestiaire et points d'intérêt par une mécanique commune à coordonnées du monde.
- Utilise uniquement des ancrages déjà examinés dans le masque visuel Wave5-method lorsqu'il correspond à la scène de prévisualisation. Aucun emplacement de démonstration n'est importé comme placement officiel.
- Conserve la position originale si aucune destination admissible n'est libre.
- Ne réattribue jamais localement les ressources reconnues comme officielles, ni l'eau sans un registre hydrologique vérifié.
- N'effectue aucun changement serveur, aucune attribution de ressources ni migration SQL.

## Causes constatées

1. Le bestiaire et la ruche réutilisaient souvent le même ancrage de masque; un T3 et une araignée étaient ajoutés près de la ruche par l'ancien générateur, sans admission anticollision globale.
2. L'ancienne correction tentait de déplacer les icônes en pixels d'écran et les faisait glisser au zoom.
3. Les ressources d'eau historiques réutilisent certains ancrages. La source spatiale officielle et sa réservation SQL atomique sont présentes dans un lot serveur, mais le déploiement P14 est en mode legacy-only-water-route-disabled.
4. res_pollen_core est reconnue comme ressource officielle par la session Unity. Sa position proche de la ruche ne doit pas être modifiée par le client.

## Preuves réellement exécutées

Dans le projet Unity actif de PCJEFF (dirty worktree, avec d'autres changements concurrentiels) :
- Après entrée en jeu : beast_t3_core_demo et spider_next_to_hive ont obtenu des coordonnées distinctes de la ruche.
- 18 combinaisons de zoom et de panoramique : ZERO changement des quatre positions mondiales observées.
- Cinq scénarios C# directs dans l'éditeur : ruche réservée, ennemi à une destination distincte, refus préservant l'ancienne position, terrain refusé, identité unique. Cinq réussis.
- Les tests du moteur de géométrie d'écran avaient réussi 5/5, mais ce moteur de translation d'icônes est désormais désactivé pour le jeu.
- Test de zones cliquables sur le client actif : 76 régions dessinées, quatre conflits résiduels, uniquement sur des ressources d'eau. Cette amélioration UI de clic/cartouches n'est **pas incluse** dans le commit minimal de réservation mondiale; elle reste dans le worktree principal non commité.

## Bloqueurs

- Aucun audit de la sémantique géographique complète (terre/rivière/lac) n'est disponible pour valider les nouvelles positions d'eau.
- Le lot de production 7a9e3af1 du 26 octobre est publié mais maintient désactivées les fonctions de réservation spatiale et d'eau. Le serveur doit valider la géométrie et la mutation atomiquement avant tout déplacement officiel; aucune autorisation implicite par ce patch client.
- La compilation de cette branche dans un **checkout Unity propre et indépendant** ainsi qu'une nouvelle capture visuelle de l'ensemble des zones et zooms restent à effectuer avant fusion à main.
- Les tests Unity EditMode ont rencontré un Test Runner verrouillé après interruption du mode Play; cinq assertions directes dans l'éditeur ont été exécutées en remplacement et ne doivent pas être présentées comme une suite NUnit complète.

## Décision

Correctif ciblé de génération locale conservé sur une branche dédiée. Aucun changement de données officielles et aucune mise en production du serveur. La suppression complète des chevauchements nécessite l'adoption vérifiée des placements officiels et la certification hydrologique.
