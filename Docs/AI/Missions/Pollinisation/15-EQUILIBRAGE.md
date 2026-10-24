# 15 — Équilibrage, formules exploratoires et métriques

**AUCUN chiffre ci-dessous n'est une décision approuvée.** Tous les paramètres doivent être configurables, simulés et ajustés après tests Alpha.

## Objectifs de balance

- Un solo contribue utilement, mais ne peut finaliser un projet régional.
- Niveau du joueur = amélioration tangible, sans effacer les avantages de coordination.
- Niveau d'alliance = déblocage d'**échelle et capacité organisationnelle**, pas domination automatique de tous les rendements.
- Alliance de 30 actifs bien coordonnés peut battre 100 inactifs.
- Pactes de plusieurs petites alliances peuvent rivaliser avec une grande alliance.
- Le butinage compétitif crée des choix, pas des pénalités de connexion forcée.
- Les joueurs pacifiques créent une valeur comparable à d'autres voies de spécialisation, sans surpasser automatiquement tout PvP.

## Variables paramétrables proposées

| Paramètre | Interprétation |
|---|---|
| civilPopulationCap | nombre civil max dans la ruche |
| civilActiveCap | civils mobilisables simultanément |
| civilianMarchSlots | missions civiles simultanées |
| workersPerExpeditionCap | taille max d'un essaim |
| broodDuration, broodCost | maturation, consommation |
| trainingLevel / formationBonuses | compétence appliquée à l'essaim |
| geneticFlowerAffinity | qualité génétique selon famille florale |
| championCivilBonus | effets d'une championne réservée |
| travelTime / carryCap | temps, cargaison, portée |
| siteFloralReserve / reserveRegen | ressources présentes et renouvellement |
| fertility, biodiversity | états écologiques bornés |
| projectGoal / minDistinctPlayers | objectif + nombre réel de joueurs |
| projectRouteDiversity | variété de routes nécessaire |
| perPlayerProjectCap | limite de points par joueur/cycle |
| allianceProjectSlots | projets simultanés selon niveau |
| relaySlots / relayUpkeep | expansion territoriale, coût d'entretien |
| pactParticipantCap | coalitions contrôlées |
| pveRisk / cargoLossCap | exposition et pertes civiles |

## Formules de départ (conceptuelles)

**Puissance brute d'une mission :**
Contribution brute = Nombre de butineuses actives × Efficacité de base × Facteur formation × Facteur génétique × Facteur championne × Facteur route × Facteur écologique.

**Contribution au projet :**
Contribution utile = minimum(Contribution brute, plafond par joueur/cycle) × coefficient de saturation du site.

**Achèvement d'un projet :**
Points utiles >= Objectif ET Participants qualifiés >= Seuil distinct ET Routes validées >= Seuil routes ET Fenêtre ouverte.

**Effet écologique :**
Nouvelle fertilité = bornage 0..100 de (fertilité + effet pollinisation + régénération naturelle - pression de surexploitation - événement).

**Rendement d'une récolte :**
Récolte créditée <= minimum(réserve réelle du site, capacité cargo, rendement de travail), en conservant exactement les ressources retirées.

Recommandations :
- Les multiplicateurs d'un même domaine ont un **plafond** et des rendements décroissants.
- Les bonus alliance sont surtout des **déblocages de capacité et projets**.
- Une perte de 1 joueur très fort ne doit pas empêcher mécaniquement la plupart des projets d'alliance.
- La fertilité et les réserves évoluent à des vitesses distinctes pour créer de vrais arbitrages.

## Hypothèses de test pour simulation seulement

| Situation simulée | Attendu |
|---|---|
| 1 compte avancé, 1 champ majeur | atteint un plafond, ne satisfait pas le seuil collectif |
| 5 débutants actifs | réussissent petite mission collective |
| 12 membres mixtes actifs | progressent sur une prairie intermédiaire |
| 30 membres avancés mais peu coordonnés | ont du potentiel, pas de récompense auto |
| 5 petites alliances coordonnées | réussissent un projet régional via pacte admissible |
| 100 membres presque tous inactifs | ne surpassent pas automatiquement les 30 actifs |

Chiffres de démonstration à ne pas reprendre dans le code sans décision : projet 10 000 points ; 12 participants qualifiés ; 3 routes compatibles ; floraison initiale 72/100 ; seuils de palier à 2 500/6 000/10 000 points.

## Équité entre profils

Créer 5 profils simulés :
- Nouveau joueur sans alliance ;
- joueur pacifique spécialisé ;
- combattant avec civils standards ;
- chef d'une petite alliance active ;
- joueur de très haut niveau d'une grande alliance.

Mesurer progression par jour, valeur collective, engagement requis, exposition PvP, capacité économique, récompenses, attrition, friction d'interface.

## Anti-monopole

- Diminution des gains par congestion d'un site.
- Entretien croissant par nombre de relais/territoires.
- Régénération minimale des champs publics.
- Événements qui apparaissent dans différentes régions.
- Limite de puissance exploitable par un seul compte sur un projet.
- Pactes d'accès et coalitions légitimes pour petits groupes.
- Pas de « destruction éternelle » d'un site après une attaque.

## Télémétrie à suivre en Alpha

Taux de lancement/complétion/abandon ; temps connecté vs contribution ; missions par jour ; % de joueurs solo actifs ; contribution médiane par niveau ; concentration top 1/5/10 % ; ressources nettes créées ; fertilité moyenne et pire région ; utilisation des trois modes ; projets collectifs réussis ; taux de doubles requêtes ; conflits 409 ; charge backend ; poids des alliances inactives.

**Condition d'alerte suggérée :** si un seul joueur peut finaliser seul un projet régional ou si 90 % de la valeur va à une minorité infime par simple bonus de niveau, revoir plafonds avant sortie.

