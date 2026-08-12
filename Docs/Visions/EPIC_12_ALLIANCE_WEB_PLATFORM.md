# EPIC 12 — Alliance Web Platform

**Statut :** Vision
**Priorité :** Future
**Implémentation :** À planifier
**Auteur :** Architecte
**Projet :** BeeKingdom

---

# Vision

L'objectif de cette EPIC est de transformer chaque alliance BeeKingdom en une véritable communauté.

Une alliance ne doit pas exister uniquement dans le jeu.

Elle doit également posséder une présence officielle sur le Web.

Chaque alliance pourra disposer de sa propre page publique, de son identité graphique, de ses outils de recrutement et de ses liens communautaires.

Cette plateforme deviendra le prolongement naturel du jeu.

---

# Objectifs

- Favoriser le recrutement.
- Renforcer le sentiment d'appartenance.
- Valoriser les créations des joueurs.
- Faciliter le partage sur les réseaux sociaux.
- Offrir une vitrine publique des alliances.
- Encourager la création de communautés durables.

---

# Philosophie

BeeKingdom ne doit pas être uniquement un jeu.

Il doit devenir un véritable écosystème communautaire.

L'alliance continue d'exister même lorsque le joueur ferme l'application.

---

# Authentification

Aucun nouveau compte.

Le portail Web utilise directement le compte BeeKingdom.

Connexion via le serveur officiel.

Le portail récupère automatiquement :

- PlayerId
- AllianceId
- Rôle
- Permissions

Aucun mot de passe supplémentaire.

---

# Page publique d'une alliance

Chaque alliance possède une page accessible publiquement.

Exemple :

https://beekingdom.com/alliance/guardians-of-honey

Cette page peut être référencée par Google.

Elle peut être partagée librement.

---

# Informations affichées

## Identité

- Nom
- Blason HD
- Bannière
- Devise
- Description

---

## Statistiques

- Niveau
- Puissance totale
- Nombre de membres
- Date de création
- Classement
- Serveur
- Langue principale

---

## Direction

- Chef
- Bras droit
- Officiers

---

## Recrutement

- Ouvert
- Fermé
- Sur invitation

Critères :

- Niveau minimum
- Puissance minimum
- Langue
- Fuseau horaire

Bouton :

POSTULER

---

# Réseaux sociaux

Le portail peut afficher :

- Discord
- Facebook
- X
- Instagram
- TikTok
- YouTube
- Twitch
- Site Web

Chaque lien est facultatif.

---

# Discord

Le chef ou le Web Manager peut renseigner une invitation Discord.

La page affiche un bouton :

Rejoindre le Discord

Les joueurs sont redirigés vers le serveur Discord officiel de l'alliance.

---

# Export du blason

Le constructeur de blasons permet :

- Export PNG transparent
- Export PNG HD
- Export SVG

Le joueur peut utiliser librement son emblème sur :

- Discord
- Site Web
- Réseaux sociaux
- Impression
- Goodies

---

# Galerie

Les responsables de l'alliance peuvent publier :

- captures d'écran
- illustrations
- victoires
- événements

---

# Actualités

Les responsables peuvent publier :

- annonces
- recrutements
- événements
- guerres
- résultats

---

# Calendrier (Version future)

Les alliances pourront créer :

- événements
- guerres
- rallyes
- soirées Discord

---

# Historique

Conserver :

- créations
- guerres importantes
- changements de chef
- records
- trophées

---

# API publique

Prévoir une API REST.

Exemples :

GET /api/alliance/{id}

GET /api/alliance/{id}/crest

GET /api/alliance/{id}/members

GET /api/alliance/{id}/stats

GET /api/alliance/{id}/wars

Cette API permettra :

- bots Discord
- sites communautaires
- classements externes
- outils de recrutement

---

# Gestion des permissions

Le portail ne repose pas uniquement sur les rôles.

Chaque permission est indépendante.

Exemples :

- Modifier le blason
- Modifier la bannière
- Modifier la description
- Gérer Discord
- Gérer les réseaux sociaux
- Publier des actualités
- Gérer les candidatures
- Gérer les événements
- Exporter les ressources graphiques

---

# Rôles proposés

## Chef

Contrôle total.

---

## Bras droit

Toutes les permissions sauf dissolution.

---

## Général

Gestion militaire.

---

## Diplomate

Relations entre alliances.

---

## Recruteur

Gestion des candidatures.

---

## Web Manager

Administration complète du portail Web.

Peut :

- modifier la page publique
- gérer le Discord
- gérer les réseaux sociaux
- gérer la galerie
- modifier les critères de recrutement
- publier les actualités

---

## Community Manager

Publication uniquement.

Aucune permission administrative.

---

## Officier

Permissions configurables.

---

## Membre

Aucune permission particulière.

---

# Objectifs techniques

Le portail doit être :

- responsive
- mobile first
- rapide
- sécurisé
- SEO friendly

Il doit reprendre intégralement la direction artistique de BeeKingdom.

---

# Vision long terme

À terme, cette plateforme doit devenir le point central de la communauté BeeKingdom.

Le jeu continuera de vivre en dehors de l'application mobile grâce :

- aux pages d'alliances,
- au partage des blasons,
- aux outils communautaires,
- aux intégrations Discord,
- aux API publiques,
- aux statistiques,
- au recrutement.

L'objectif est que chaque alliance puisse développer sa propre identité, sa propre communauté et sa propre présence en ligne, tout en restant intégrée à l'écosystème officiel de BeeKingdom.

---

# Évolutions futures

Cette EPIC pourra être découpée ultérieurement en plusieurs sprints dédiés :

- Sprint — Authentification Web
- Sprint — Pages publiques d'alliances
- Sprint — Export des blasons (PNG/SVG)
- Sprint — Gestion des rôles Web
- Sprint — Recrutement
- Sprint — Intégration Discord
- Sprint — API publique
- Sprint — Actualités & Galerie
- Sprint — Calendrier d'événements
- Sprint — Bots Discord officiels