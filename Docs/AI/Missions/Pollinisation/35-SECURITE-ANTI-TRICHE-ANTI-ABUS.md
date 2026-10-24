# 35 — Modèle de menace : sécurité, triche, comptes multiples, griefing

## Menaces principales

### Fraude d'identité
Un client modifie playerId/hiveId/allianceId d'une requête, vise une ruche étrangère ou invoque une permission d'officier.

**Contrôle** : identité dérivée exclusivement de l'authentification, correspondance serveur de ruche et adhésion, permission calculée à l'instant du commit. Ne jamais croire allianceAtStart fourni par Unity.

### Double crédit économique
Rejouer claim de collecte, démarrage cohorte ou événement de fin après un timeout ; plusieurs appareils attaquent le même API.

**Contrôle** : receipts idempotents + contrainte durable unique par fait (cohortId, expeditionId, deliveryId, rewardId), CAS et tests de concurrence à plusieurs processus. Les receipts bornés en mémoire ne remplacent pas un ledger durable.

### Double mobilisation de civils
Deux marches simultanées attribuent les 100 mêmes butineuses ; même championne ou escorte apparaît sur plusieurs lieux.

**Contrôle** : réservation officielle transactionnelle, conservation par état, tickets d'escorte militaires distincts, verrouillage par ruche, réconciliation après crash.

### Client falsifie rendement ou temps
Unity affiche une cargaison énorme, change l'heure du PC, injecte un trait génétique non acquis.

**Contrôle** : serveur calcule compatibilité, valeurs depuis données sauvegardées, bonus et états officiels, UTC et catalogue signé/versionné côté serveur. Client ne propose que type de mission, compte d'ouvrières et IDs.

### Captation de ressources par bot
Un bot exploite chaque micro-renouvellement et vide les champs ; avalanche de requêtes.

**Contrôle** : limites de requête, coûts d'opportunité, opérations planifiées, cooldowns raisonnables et pas de bonus à spam de démarrage. Ajuster sans punir les joueurs humains occasionnels.

### Ferme à comptes (Sybil)
Un joueur crée 12 comptes pour simuler une grande alliance.

**Contrôle** : seuil de **contribution réelle minimale** par joueur, maturité minimum de ruche éventuelle, part de contributions plafonnée, détection de comportements répétitifs. Aucun mécanisme invasif intrusif de traçage personnel nécessaire pour l'Alpha ; respecter la vie privée.

### Monopole territorial / griefing
Une grande alliance bloque les nouveaux ou attaque sans pause les essaims civils.

**Contrôle** : champs publics protégés, fenêtre d'attaque, possibilité de repli, pertes plafonnées, coalitions, nombre de relais limité et coût d'entretien, accès sans alliance.

### Course concurrente sur la même fleur
Deux consommateurs lisent 80 pollen sur un site et chacun prélève 80.

**Contrôle** : somme des allocations atomique et CAS siteRevision ; pas de calcul local. Si site épuisé entre aperçu et départ, rejeter/adapter conformément au contrat avant débit irréversible.

### Manipulation de classement mondial
Un joueur reçoit plusieurs fois score de même raid de l'ours, change d'alliance pour compter ses points ailleurs.

**Contrôle** : contributionId unique, affiliation figée au moment de l'action, période serveur et type de score, classement calculé du ledger immuable.

## Surface de confiance

- Auth : tokens et contexte session valides.
- API : validation, longueur, limite d'objets, ownership et visibilité.
- Repos : permission et intégrité transactionnelle.
- Jobs : idempotence, revision et audit.
- Notifications : aucune information privée d'une ruche exposée aux concurrents.
- Unity : interface non fiable du point de vue du serveur.
- Clients modifiés ou de versions anciennes : sûrs par validation de contrat.

## Permissions par action (préliminaire)

| Action | Joueur simple | Membre alliance | Officier/chef | Adversaire |
|---|---|---|---|---|
| Voir champ public | oui | oui | oui | oui |
| Polliniser champ public | oui | oui | oui | oui |
| Ouvrir projet d'alliance | non | non | selon politique officielle | non |
| Contribuer au projet | si public admis | oui si admissible | oui | pacte si admis |
| Construire relais | non | non | selon politique officielle | non |
| Signer pacte | non | non | sous permission | non |
| Attaquer relais | si règles PvP l'autorisent | selon alliance | selon alliance | selon guerre/fenêtre |
| Réclamer récompense perso | propriétaire seulement | propriétaire seulement | propriétaire seulement | non |

Cette matrice est **à faire valider** par les rôles exacts du serveur. Si l'interface cache un bouton mais API le permet, c'est une vulnérabilité.

## Protection PII

Ne pas exposer une liste exhaustive des autres joueurs en route (WorldPresence actuel cache délibérément noms). Les données collectives peuvent afficher surnoms/profils uniquement si les contrats sociaux du jeu l'autorisent. Leaderboard public : nom d'alliance/tag et score, pseudonymes individuels selon politique de confidentialité.

## Tests sécurité obligatoires

Reprise des QA-001 à QA-006 + fuzz de payload, dépassement entier, IDs non existants, clé idempotence même/différent payload, accès alliance inactive, bannissement/exclusion, appel sur ruche du compte B, token expiré, concurrent multi-client, auto-claim vieux raid, déréférencement de site privé.

## Règle de conformité économique

Aucune possibilité de créer honey/pollen/wax, civils, XP d'alliance ou crédits de raid depuis une requête fabriquée sans état source valide. Conserver les preuves de refus et les balances avant/après.

