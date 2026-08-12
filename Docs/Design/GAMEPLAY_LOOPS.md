# GAMEPLAY_LOOPS.md

## BeeKingdom Gameplay Loop Architecture

**Projet :** BeeKingdom

**Auteur :** Architecte

**Version :** 1.0

**Statut :** Document fondateur

---

# Vision

BeeKingdom n'est pas une collection de fonctionnalités.

BeeKingdom est une collection de **boucles de gameplay**.

Une fonctionnalité isolée possède peu de valeur.

Une fonctionnalité qui nourrit plusieurs autres systèmes devient un moteur de rétention.

Chaque nouveau système développé doit donc participer à une ou plusieurs Gameplay Loops.

---

# Principe fondamental

Un système n'est jamais considéré comme terminé lorsqu'il fonctionne.

Il est considéré comme terminé lorsqu'il alimente une boucle de gameplay complète.

---

# Boucle principale

Le joueur ouvre BeeKingdom.

↓

Collecte les ressources.

↓

Améliore sa ruche.

↓

Débloque de nouveaux bâtiments.

↓

Recherche de nouvelles technologies.

↓

Entraîne davantage d'abeilles.

↓

Progresse.

↓

Débloque de nouveaux objectifs.

↓

Revient plus tard.

Cette boucle constitue le cœur du jeu.

---

# Boucle Construction

Construction

↓

Temps d'attente

↓

Accélérations

↓

Récompenses

↓

Mission terminée

↓

Nouvelle construction

↓

Progression

---

# Boucle Recherche

Recherche

↓

Temps d'attente

↓

Accélérations

↓

Nouvelle technologie

↓

Déblocage

↓

Nouveaux bâtiments

↓

Nouvelle recherche

---

# Boucle Armée

Entraînement

↓

Nouvelle armée

↓

Combat

↓

Récompenses

↓

Amélioration

↓

Nouvel entraînement

---

# Boucle Exploration

Explorer

↓

Découverte

↓

Récompense

↓

Événement

↓

Progression

↓

Nouvelle exploration

---

# Boucle Alliance

Don

↓

Aide

↓

Discussion

↓

Événement

↓

Guerre

↓

Récompense

↓

Renforcement de l'alliance

---

# Boucle Quotidienne

Connexion

↓

Récompense

↓

Mission quotidienne

↓

Progression

↓

Récompense

↓

Connexion suivante

---

# Boucle Événement

Nouvel événement

↓

Objectifs

↓

Participation

↓

Classement

↓

Récompenses

↓

Nouvel événement

---

# Boucle Battle Pass

Mission

↓

Progression

↓

Niveau

↓

Récompense

↓

Mission suivante

---

# Boucle Boutique

Besoin

↓

Boutique

↓

Achat

↓

Progression

↓

Nouvel objectif

---

# Boucle Communauté

Alliance

↓

Discord

↓

Site Web

↓

Recrutement

↓

Nouveaux joueurs

↓

Alliance plus forte

---

# Boucle Héraldique

Création du blason

↓

Export PNG

↓

Discord

↓

Site Web

↓

Visibilité

↓

Prestige

↓

Nouveaux membres

↓

Alliance plus forte

---

# Boucle Premium

Collection

↓

Déblocage

↓

Personnalisation

↓

Prestige

↓

Nouvelle collection

---

# Boucle Saison

Nouvelle saison

↓

Objectifs

↓

Progression

↓

Classement

↓

Récompenses exclusives

↓

Nouvelle saison

---

# Règle d'architecture

Chaque nouveau système doit répondre aux questions suivantes :

- Quelles boucles utilise-t-il ?
- Quelles boucles enrichit-il ?
- Quelles récompenses génère-t-il ?
- Quelles récompenses consomme-t-il ?

Si aucune réponse n'est trouvée, le système doit être repensé.

---

# Définition d'un Sprint terminé

Un Sprint n'est considéré comme terminé que lorsque :

- le système fonctionne ;
- il est documenté ;
- il est testé ;
- il est intégré à au moins une Gameplay Loop.

---

# Objectif

Le joueur ne doit jamais avoir l'impression de réaliser une action isolée.

Chaque action doit naturellement entraîner une autre.

Le jeu doit constamment donner une nouvelle raison de continuer à jouer.

---

# Vision long terme

Toutes les futures fonctionnalités de BeeKingdom devront être conçues comme des extensions des Gameplay Loops existantes.

L'objectif n'est pas d'ajouter toujours plus de systèmes.

L'objectif est de rendre chaque système plus connecté aux autres.

Cette philosophie guidera l'ensemble du développement du projet.