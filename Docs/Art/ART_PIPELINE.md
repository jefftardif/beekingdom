# ART_PIPELINE.md

Version : 1.0

Statut : Fondation

Auteur : Studio Director

---

# Philosophie

L'objectif de BeeKingdom n'est pas de produire de belles images.

L'objectif est de produire un univers cohérent, évolutif et durable.

Chaque asset doit pouvoir évoluer sans remettre en cause les autres.

Nous privilégions toujours une architecture modulaire.

Un asset ne doit jamais en empêcher un autre d'évoluer.

---

# Objectifs

Le pipeline artistique doit permettre :

- une évolution du jeu pendant plusieurs années
- une intégration simple dans Unity
- une maintenance facile
- une production parallèle par plusieurs graphistes
- des remplacements d'assets sans régression

---

# Principe fondamental

Les assets ne sont jamais dessinés ensemble.

Ils sont toujours produits indépendamment.

Unity est responsable de leur assemblage.

---

# Les couches graphiques

Toute scène est composée des couches suivantes.

## Layer 01

Terrain

Le terrain représente :

- le sol
- les falaises
- les chemins
- les rivières
- les fleurs
- les arbres
- les rochers
- les éléments permanents

Le terrain ne contient jamais de bâtiments.

---

## Layer 02

Bâtiments

Chaque bâtiment est indépendant.

Chaque bâtiment possède :

- un PNG
- un point d'ancrage
- une zone SVG

Les bâtiments peuvent évoluer sans modifier le terrain.

---

## Layer 03

Décorations

Statues

Buissons

Champignons

Fleurs

Mobilier

Objets saisonniers

---

## Layer 04

Personnages

Championnes

Abeilles

PNJ

Visiteurs

---

## Layer 05

Effets

Ombres

Lumières

Particules

Pluie

Neige

Pollen

---

## Layer 06

Interface

Notifications

Sélections

Bulles

Timers

Icônes

---

# Pipeline officiel

Le développement artistique suit toujours cet ordre.

## Étape 1

Concept

Définir le gameplay.

Aucun dessin.

---

## Étape 2

Blueprint

Disposition.

Volumes.

Hiérarchie.

Aucun détail artistique.

---

## Étape 3

Terrain

Création du décor permanent.

Sans bâtiments.

---

## Étape 4

SVG

Création définitive des zones interactives.

Le SVG devient permanent.

---

## Étape 5

Bâtiments

Chaque bâtiment est produit individuellement.

---

## Étape 6

Décorations

Ajout progressif des éléments décoratifs.

---

## Étape 7

Animations

Ajout des animations.

---

## Étape 8

Effets

Ajout des effets visuels.

---

# Convention de nommage

Terrain

HIVE_TERRAIN_001.png

---

Bâtiments

QUEEN_001.png

ACADEMY_001.png

GENETICS_001.png

CHAMPIONS_HALL_001.png

etc.

---

Décorations

TREE_001.png

FLOWER_001.png

STATUE_001.png

ROCK_001.png

---

Effets

FX_POLLEN_001.png

FX_RAIN_001.png

FX_MAGIC_001.png

---

# Règles

Les graphistes ne modifient jamais :

- les coordonnées
- le SVG
- les dimensions officielles
- les points d'ancrage

Ces éléments sont définis par le Studio.

---

# Évolution

Les bâtiments peuvent évoluer :

- graphiquement
- selon leur niveau
- selon la Doctrine
- selon les saisons
- selon les événements

Sans modifier le terrain.

---

# Réutilisation

Tout asset doit pouvoir être réutilisé.

Aucun élément ne doit être créé pour une seule utilisation.

---

# Qualité

Chaque asset doit répondre aux critères suivants :

Lisibilité

Beauté

Personnalité

Compatibilité

Performance

---

# Direction artistique

Chaque asset doit immédiatement évoquer BeeKingdom.

Même isolé.

Un joueur doit reconnaître un asset BeeKingdom sans voir le reste du jeu.

---

# Vision finale

Notre objectif n'est pas simplement de créer de beaux assets.

Notre objectif est de construire un univers.

Un univers capable d'évoluer pendant plusieurs années sans être reconstruit.

Chaque asset représente une brique.

Unity construit le royaume.