# 46 — Glossaire contrôlé et ontologie BeeKingdom

## Concepts fondamentaux

**Ruche (Hive)** : unité de progression et ressources associée au joueur, identifiée par HiveId propriétaire.  
**Joueur (Player)** : identité authentifiée du compte ; peut participer aux projets d'alliance.  
**Alliance** : organisation de joueurs avec rôles, recherches, relations et progression future ; l'identité officielle est AllianceId.  
**Population civile** : abeilles non combattantes appartenant à une ruche et comptabilisées distinctement des troupes.  
**Butineuse (Forager)** : type civil généraliste V1.  
**Essaim civil (Civilian swarm)** : sous-ensemble d'abeilles civiles **réservé temporairement** pour une mission. Ne crée pas de population supplémentaire.  
**Cohorte (Cohort)** : unité de croissance de la population dont le début et la maturité ont une preuve serveur.  
**Formation (Training)** : qualification/compétence, distincte d'une cohorte de naissance et du recrutement militaire.  
**Génétique (Genetics)** : traits biologiques de spécialisation, distincts de la formation.  
**Championne** : personnage d'encadrement affecté selon son contrat de disponibilité.

## Fleurs et écologie

**Espèce/famille florale** : clé du catalogue autorisant certaines routes de pollinisation (compatibilité explicite, pas inférée de l'icône).  
**Site floral** : point d'intérêt stable (FlowerPatch conceptuel côté client / FloralSiteState autoritaire côté serveur).  
**Région** : agrégat de plusieurs sites, pas nécessairement une tuile.  
**Fertilité (Fertility)** : potentiel écologique et de régénération future.  
**Biodiversité (Biodiversity)** : diversité/rareté potentielle des flores présentes.  
**Réserves florales (Floral reserves)** : stock de nectar/pollen immédiatement disponible sur un site.  
**Nectar** : ressource naturelle récoltée ; peut devenir du honey officiel selon catalogue, **pas nouvelle monnaie automatiquement**.  
**Pollen de site** : stock floral, distinct du wallet pollen.  
**Pollen transporté** : cargaison extraite d'une source et déposée à une destination compatible pour pollinisation ; pas simultanément une monnaie sans règle de conversion.  
**Pollinisation effective** : livraison validée + effet écologique, enregistrée côté serveur.  
**Floraison** : phase du cycle des fleurs ou événement de grande production selon contexte.  
**Restauration** : action visant fertilité/biodiversité futures plutôt que gain immédiat.  
**Surexploitation** : pression temporaire sur stocks et renouvellement, **pas destruction définitive**.

## Opérations

**Expédition civile** : mission serveur avec essaim réservé, phases, itinéraire, coûts, résultats et retour.  
**Route de pollinisation** : source -> destination compatible -> retour à la ruche.  
**Slot civil** : capacité de missions civiles parallèles, distincte de la limite de marches militaires si contrat décidé.  
**Escorte** : soldats officiels réservés pour protéger une expédition sans devenir butineuses.  
**Receipt** : reçu de mutation lié à idempotencyKey et résultat stable.  
**Ledger** : registre durable des changements, utile contre double attribution.  
**Révision (Revision)** : version optimiste d'une entité/agrégat pour éviter écritures concurrentes perdues.  
**Settlement** : règlement officiel de l'issue d'une mission, d'un cycle, d'un projet ou d'un événement.  
**Claim** : geste de récupération d'un gain quand le produit le prévoit ; peut être distinct de la résolution écologique automatique.

## Collaboratif / territorial

**Projet floral** : objectif de contribution partagé, avec points, participants distincts et autres conditions.  
**Contribution qualifiée** : apport réellement livré et suffisant pour compter le joueur dans le minimum de participants.  
**Niveau d'alliance** : progression durable à introduire ou vérifier dans une source officielle, distincte des recherches d'alliance.  
**Relais floral** : installation stratégique d'alliance sur la carte.  
**Balise de phéromones** : infrastructure d'influence territoriale envisagée, non décidée dans ses détails.  
**Ruche-Mère d'alliance** : centre territorial potentiel, distinct de la ruche personnelle.  
**Pacte de Floraison** : accord économique/diplomatique donnant accès, objectifs, protections et bénéfices selon clauses.  
**Coalition** : plusieurs alliances contribuant à une opération qui dépasse les capacités de chacune.  
**Propriété** : contrôle d'une structure ou droits, jamais automatiquement possession de la fertilité de la région.

## Menaces/événements

**Grande Floraison** : occasion régionale/temporaire de pollinisation/ressources.  
**Éclosion légendaire** : événement de fleur rare exigeant effort collectif.  
**Infestation** : parasite/pucerons, action PvE de protection/rétablissement.  
**Tanière de l'ours** : lieu permanent et futur **raid mondial interalliances** avec coopération et concurrence sur les récompenses.

## Vocabulaire d'implémentation

**[APPROUVÉ]** décisions explicites utilisateur.  
**[RECOMMANDÉ]** design proposé.  
**[À TRANCHER]** arbitrage encore ouvert.  
**[VÉRIFIÉ]** observation ponctuelle du dépôt.  
**Alpha** : premier système testable cohérent.  
**Bêta** : élargissement territorial, diplomatique, PvP et événements.  
**Feature flag OFF** : ne pas lancer de nouvelles opérations.  
**DRAIN** : terminer les opérations déjà acceptées avant désactivation.

## Faux synonymes à éviter

- « 72 % de fertilité » **n'est pas** « 72 % d'objectif collectif ».
- « 100 points projet » **n'est pas** « 100 pollen en inventaire ».
- « 1 membre d'alliance » **n'est pas** « 1 participant qualifié ».
- « Fleur visible sur carte » **n'est pas** « site serveur partageable ».
- « Niveau de Recherche alliance » **n'est pas** « niveau d'alliance ».
- « Troupe de collecte actuelle » **n'est pas** « butineuse civile ».
- « Ours visible » **n'est pas** « événement ours jouable ».

