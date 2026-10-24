# 43 — Tanière de l'ours : cahier des charges du raid mondial

## Décision de produit approuvée

**[APPROUVÉ]** La tanière de l'ours sert dès l'origine à un **événement mondial où plusieurs alliances coopèrent contre l'ours tout en rivalisant pour les meilleures récompenses**.

Cette finalité ne doit pas être remplacée par un boss solo, une animation ou une mission classique de patrouille.

## Lieu permanent

Le repère visuel est déjà prévu : WorldMapBearDenLandmark.cs contient l'asset, les coordonnées/ancre et rayon de spawn. L'état consulté indique un repère « dormant » : BearVisible=false, ActiveEvent=false. La future expérience ne doit pas changer les tuiles ni faire disparaître la tanière hors événement.

## Déroulement recommandé

**Phase 0 — Sommeil :** tanière visible, informations historiques/règles, prochain événement si planifié.

**Phase 1 — Alerte :** avis mondial, période de préparation, mobilisation des alliances, coopérations.

**Phase 2 — Réveil :** ours actif, objectifs PvE, changement d'apparence du repère, menace localisée.

**Phase 3 — Mobilisation :** missions militaires, reconnaissance, ravitaillement et protection des civils. Les alliances peuvent coordonner des vagues de combat tout en maintenant leurs objectifs propres.

**Phase 4 — Raid :** attaque collective du même boss mondial/du même état de raid. Santé, phases, seuils, attaques et contrôle de combat calculés serveur. Plusieurs alliances enregistrent leurs contributions.

**Phase 5 — Victoire/retrait :** l'ours est repoussé selon objectif global et revient à la tanière, la carte se normalise.

**Phase 6 — Récompenses :** part de participation individuelle, part collective par alliance, classement compétitif selon règle publiée ; paiements idempotents.

**Phase 7 — Récupération :** délai/cooldown, rapport officiel accessible, historique de la saison.

## Coopération ET compétition : pas une contradiction

- **Coopération :** le serveur fixe un objectif global commun (ex. repousser l'ours) qui nécessite cumul des efforts d'alliances différentes.
- **Compétition :** classements par alliance selon contribution validée ; meilleurs efforts obtiennent des prix additionnels, mais les récompenses de participation restent accessibles à tous les vrais contributeurs.
- **Petit groupe :** doit pouvoir aider via défense, transport, reconnaissance et ressources, pas seulement dégâts absolus d'un gros clan.
- **Protection PvP :** interdire par défaut qu'une alliance monopolise physiquement la tanière et bloque l'événement mondial.

## Intégration à la pollinisation

**Propositions** :
- Des régions riches en miel ont besoin de vigilance et de logistique.
- Les butineuses peuvent participer au ravitaillement du raid **sans attaquer l'ours**.
- Les escortes assurent des trajets sûrs vers l'événement.
- Le raid peut menacer temporairement une floraison proche, sans détruire définitivement les champs.
- Des alliances spécialisées en pollinisation deviennent des partenaires utiles de gros clans guerriers.

Le déclenchement de l'ours en fonction de la fertilité régionale est **à arbitrer** : éviter de punir les alliances les plus actives.

## Modèle de données futur

BearWorldEvent: eventId, denLandmarkId, worldId, phase, announcedAt, beginsAt, endsAt, encounterState, bossHealth/phase, eligibleAlliances, cooldown, rewardCatalogVersion, revision.

BearRaidContribution: eventId, playerId, allianceIdAtContribution, contributionType, amount, verifiedAtUtc, uniqueSourceActionId.

BearRaidReward: eventId, recipientId, rewardTierId, amountResourceKey, state, receiptId, settledAtUtc.

Pas de doublon de récompense ni de contribution après retry, aucun inventaire de soldes client autoritaire.

## Classements potentiels

Trois tableaux possibles : alliance globale, joueur individuel et contributions logistiques/civiles. Limiter nombre de classements pour une V1 ; en l'absence d'approbation, garder au moins la **séparation participation vs compétition**.

## Compatibilité de combat

Auditer CombatPatrolService et progression militaire ; ces patrouilles ont des contrats conçus pour d'autres cibles. Un raid mondial nécessite état partagé, non une cible locale « chaque joueur a son propre ours ». Toutes les attaques doivent viser **le même eventId** et la même progression globale, avec concurrence contrôlée.

## UX du raid

Carte : icône forte de la tanière, badge sommeil/alerte/active, décompte serveur, accès événements. Fenêtre premium : objectif, progression mondiale, participants/alliances, calendrier, contribution perso, récompenses et rapports, en fr-CA et en-US.

## Critères QA spécifiques

Deux alliances peuvent attaquer la même cible mondiale ; état boss partagé ; récompenses exact-once ; classement juste ; déconnexion milieu raid ; leader déconnecté ; changement d'alliance ; protection contre farming de dégâts ; événement se termine sans laisser de cibles déjà vaincues ; repère conservé entre éditions.

