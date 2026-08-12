# HIVE_MASTER_LAYOUT.md

Version : 1.0

Statut : Fondation

Auteur : Studio Director

---

# Philosophie

La Ruche est l'écran principal de BeeKingdom.

Le joueur y reviendra plusieurs centaines de milliers de fois durant sa vie dans le jeu.

Son organisation doit donc être :

- intuitive
- élégante
- spectaculaire
- évolutive

Le layout de la Ruche constitue une fondation permanente.

Il ne devra plus être modifié une fois validé.

---

# Objectifs

Le layout doit permettre :

- l'évolution graphique des bâtiments
- l'ajout d'animations
- l'ajout de décorations
- l'évolution de la Ruche selon son niveau
- les effets météo
- les événements saisonniers
- les changements liés aux Doctrines
- une excellente lisibilité

---

# Principe fondamental

Le terrain de la Ruche est permanent.

Les bâtiments sont indépendants.

Les bâtiments ne sont jamais fusionnés avec le décor.

Chaque bâtiment peut être remplacé sans modifier le terrain.

---

# Architecture graphique

La Ruche est composée de plusieurs couches.

Layer 1

Terrain permanent

- roche
- cire
- végétation
- chemins
- fleurs
- rivière de miel
- décor naturel

---

Layer 2

Emplacements des bâtiments

14 zones officielles.

Chaque zone possède son SVG.

Chaque zone est indépendante.

---

Layer 3

Bâtiments

Chaque bâtiment est une image indépendante.

Le bâtiment évolue selon :

- son niveau
- la Doctrine active
- les événements
- les skins

---

Layer 4

Décorations

Objets décoratifs.

Statues.

Arbres.

Champignons.

Fleurs.

Bancs.

Étangs.

Aucune décoration ne doit empêcher un bâtiment d'évoluer.

---

Layer 5

Personnages

Championnes.

Abeilles.

Visiteurs.

PNJ.

Ils circulent dans la Ruche.

---

Layer 6

Effets

Ombres.

Lumières.

Particules.

Météo.

Saisons.

Animations.

---

Layer 7

Interface

Icônes.

Notifications.

Timers.

Sélections.

Bulles.

---

# Les 14 emplacements majeurs

La Ruche possède exactement quatorze emplacements majeurs.

Ils constituent l'ensemble des bâtiments principaux du jeu.

Aucun quinzième emplacement ne sera ajouté sans une décision de design majeure.

Liste actuelle :

1. Chambre de la Reine

2. Nurserie

3. Réserves de Miel

4. Caserne

5. Défense

6. Génétique

7. Laboratoire de Recherche

8. Entrepôt

9. Centre de Transformation

10. Infirmerie

11. Académie

12. Banque Royale

13. Hall des Championnes

14. Centre d'Alliance

---

# Hiérarchie visuelle

Tous les bâtiments n'ont pas la même importance.

## Monuments

Les bâtiments suivants doivent immédiatement attirer le regard :

- Chambre de la Reine
- Hall des Championnes
- Centre d'Alliance

Ils sont plus grands.

Plus détaillés.

Plus prestigieux.

---

## Bâtiments majeurs

- Académie
- Caserne
- Recherche
- Génétique

---

## Bâtiments secondaires

- Banque
- Entrepôt
- Réserves
- Transformation
- Infirmerie
- Nurserie
- Défense

---

# Zones libres

Le layout doit conserver plusieurs zones libres.

Ces zones permettront :

- les décorations
- les événements
- les animations
- les PNJ
- les championnes
- les effets spéciaux

Le terrain ne doit jamais être saturé.

---

# Circulation

Les bâtiments doivent être reliés par des chemins naturels.

Les Championnes et les abeilles doivent pouvoir sembler circuler librement.

La Ruche doit donner l'impression d'être vivante.

---

# Évolution graphique

Le terrain reste permanent.

Les bâtiments évoluent.

Les décorations évoluent.

La végétation évolue.

Les effets évoluent.

La Ruche devient progressivement une véritable cité.

---

# Doctrine

La Doctrine active modifie :

- les couleurs
- les bannières
- certaines décorations
- certains effets

Elle ne modifie jamais le terrain principal.

---

# Saisons

Le terrain supporte les saisons.

Printemps

Été

Automne

Hiver

Halloween

Noël

Anniversaire BeeKingdom

Les bâtiments restent compatibles avec tous les thèmes.

---

# Caméra

Le layout est conçu pour une caméra fixe.

Tous les bâtiments importants doivent rester immédiatement identifiables.

Aucun bâtiment ne doit masquer un autre.

---

# SVG

Chaque emplacement possède :

- un SVG indépendant
- un identifiant permanent
- une position permanente

Le SVG constitue la référence officielle.

Toute modification de celui-ci devra être approuvée.

---

# Graphistes

Les graphistes travaillent uniquement sur :

- le terrain
- les bâtiments
- les décorations

Ils ne modifient jamais :

- la disposition
- les coordonnées
- les SVG

---

# Unity

Unity assemble les différentes couches.

Le moteur ne contient jamais une image "complète" de la Ruche.

Il affiche :

Terrain

+

Bâtiments

+

Décorations

+

Championnes

+

Effets

+

UI

Cette architecture garantit l'évolutivité du jeu pendant plusieurs années.

---

# Contraintes de conception

Avant toute nouvelle fonctionnalité, toujours se poser les questions suivantes :

Cette fonctionnalité nécessite-t-elle réellement un nouvel emplacement ?

Peut-elle être intégrée à un bâtiment existant ?

Peut-elle être réalisée uniquement par l'interface ?

Peut-elle être réalisée via une Championne ?

Peut-elle être réalisée via une Doctrine ?

Le nombre d'emplacements majeurs doit rester stable.

---

# Vision finale

La Ruche doit être immédiatement reconnaissable.

Le joueur doit avoir envie de la contempler.

Chaque évolution doit être visible.

Chaque bâtiment doit raconter une histoire.

Le terrain représente les fondations du royaume.

Les bâtiments représentent sa croissance.

Les Championnes représentent son âme.

La Ruche représente le joueur.