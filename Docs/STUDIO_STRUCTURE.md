# STUDIO_STRUCTURE.md

# BeeKingdom Studio Structure

Version : 1.0

Auteur : Architecte

Statut : Document fondateur

---

# Vision

BeeKingdom n'est pas seulement un jeu.

BeeKingdom est un studio de développement organisé autour d'une documentation vivante.

Chaque fonctionnalité, chaque illustration, chaque système et chaque décision possède une place précise.

L'objectif est que le projet puisse continuer à évoluer pendant plusieurs années sans perdre sa cohérence.

---

# Philosophie

Le code n'est pas la source de vérité.

La documentation est la source de vérité.

Le code implémente cette documentation.

Les assets illustrent cette documentation.

Les Sprints exécutent cette documentation.

---

# Les cinq couches du projet

Toute la documentation est organisée selon cinq niveaux.

```
VISION

↓

DESIGN

↓

ARCHITECTURE

↓

PRODUCTION

↓

IMPLEMENTATION
```

Chaque document appartient obligatoirement à une couche.

---

# 1 — VISION

Répond à la question :

Pourquoi ?

Cette couche décrit :

- la vision globale
- les objectifs
- les grandes fonctionnalités
- les valeurs du projet

Elle évolue peu.

Exemples :

```
Roadmap/

EPIC_*.md

ROADMAP.md
```

---

# 2 — DESIGN

Répond à la question :

Comment le joueur vit-il cette fonctionnalité ?

Cette couche décrit :

- Gameplay
- UX
- UI
- Boucles de jeu
- Économie
- Progression
- Psychologie du joueur
- Rétention
- Social

Exemples :

```
Design/

GAMEPLAY_LOOPS.md

PLAYER_PSYCHOLOGY.md

RETENTION.md

GAME_ECONOMY.md
```

---

# 3 — ARCHITECTURE

Répond à la question :

Comment le système est construit ?

Exemples :

- Networking

- Save System

- Database

- Events

- Inventory

- SpeedUps

- World

- Hive

- Heraldry

---

# 4 — PRODUCTION

Répond à la question :

Comment le studio produit-il le jeu ?

Cette couche contient :

- Sprints

- Pipeline Graphiste

- Pipeline OC

- Guides

- Standards

- Bibliothèques

- Assets Registry

---

# 5 — IMPLEMENTATION

Cette couche correspond au projet Unity.

Elle contient :

- code

- prefabs

- textures

- scènes

- audio

- animations

---

# Organisation documentaire

```
Docs/

    STUDIO_STRUCTURE.md

    Roadmap/

    Design/

    Architecture/

    Art/

    Gameplay/

    Production/

    Claude/
```

---

# Responsabilités

## Jeff

CEO

Product Owner

Game Designer

Décisions finales.

Vision.

Gameplay.

Priorités.

---

## Architecte

Responsable de :

- cohérence globale

- architecture fonctionnelle

- roadmap

- documentation

- direction artistique

- UX

- préparation des Sprints

- anticipation

L'Architecte remet les décisions en question lorsque cela améliore BeeKingdom.

Il protège le projet.

Pas les idées.

---

## OC

Lead Software Engineer.

Responsable de :

- Unity

- gameplay

- architecture logicielle

- optimisation

- réseau

- implémentation

- tests

- documentation technique

---

## Graphiste

Responsable de :

- illustrations

- UI

- bâtiments

- icônes

- backgrounds

- blasons

- champions

- ressources

- effets

Tous les assets doivent respecter la Direction Artistique officielle.

---

# Flux de production

Une idée suit toujours le même cycle.

```
Idée

↓

Discussion

↓

Vision

↓

Documentation

↓

Validation

↓

Sprint

↓

Implémentation

↓

Tests

↓

Documentation

↓

Livraison
```

Aucun Sprint n'est créé sans documentation.

Aucune implémentation n'est réalisée sans Sprint.

---

# Documentation vivante

Tous les documents sont considérés comme vivants.

Ils peuvent évoluer.

Toute modification importante doit être répercutée dans les documents concernés.

---

# Qualité

BeeKingdom recherche un niveau de qualité comparable aux meilleurs studios mobiles.

Chaque élément produit doit répondre aux critères suivants :

- Premium

- Cohérent

- Maintenable

- Documenté

- Testable

- Évolutif

---

# Philosophie de développement

Nous privilégions :

Une excellente fonctionnalité

plutôt que

cinq fonctionnalités moyennes.

Nous préférons :

des systèmes profondément intégrés

à

une accumulation de mécaniques indépendantes.

---

# Philosophie artistique

Chaque illustration produite doit pouvoir devenir une image promotionnelle du jeu.

Chaque asset doit être considéré comme une œuvre destinée à durer plusieurs années.

---

# Philosophie technique

Les systèmes doivent être :

- modulaires

- data-driven

- extensibles

- testables

- performants

Le code doit être pensé pour les cinq prochaines années.

Pas seulement pour le prochain Sprint.

---

# Le rôle de la documentation

La documentation est considérée comme un actif du studio.

Elle possède autant de valeur que le code.

Elle permet :

- l'intégration rapide de nouveaux collaborateurs

- la continuité du projet

- la réduction des erreurs

- une vision commune

---

# Décisions

Toutes les décisions importantes doivent pouvoir être retrouvées dans la documentation.

Le projet ne doit jamais dépendre uniquement de la mémoire des participants.

---

# Objectif ultime

Construire BeeKingdom comme le ferait un véritable studio AAA indépendant.

Créer une organisation capable de produire, maintenir et faire évoluer le jeu pendant de nombreuses années sans perdre sa cohérence technique, artistique ou fonctionnelle.

Le studio doit survivre aux changements de collaborateurs, d'outils et de technologies.

La documentation constitue le patrimoine intellectuel de BeeKingdom.