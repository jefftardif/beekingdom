# 22 — Architecture cible par domaine / contrats d'interface

## Vue logique

~~~text
Unity PCJEFF (présentation)
  |  lecture de snapshots officiels + intentions authentifiées
  v
BeeKingdom.Server (auth, API, validation de contrats)
  |-------- ICivilPopulationService  -------- PlayerHiveState/CivilPopulation
  |-------- ICivilExpeditionService  -------- Expedition + reservations
  |-------- IFloralWorldService      -------- FloralSiteState + région + catalogue
  |-------- IPollinationProjectService ----- projets/ledger + agrégation
  |-------- IFloralAllianceResolver  -------- alliance, rôles et niveau
  |-------- IFloralRightsResolver    -------- territoire/pactes
  |-------- IEscortReservationPort   -------- comptabilité militaire
  |-------- IFloralRewardWriter      -------- ressources/ledger officiels
  |-------- IServerClock             -------- temps serveur
  |-------- IFloralEventPublisher    -------- notification/push
  v
Repositories transactionnels + journal durable + versioning
~~~

**Toutes les interfaces sont des propositions ; ne pas coller ce diagramme en classes avant revue des couches actuelles.**

## Découpage par responsabilités

**Domaine civil** : crée des cohortes civiles, conserve effectifs, formation/récupération/blessés, maîtrise de slots et réservations. Ne connaît pas la forme des écrans.

**Domaine floral** : possède les réserves et les phases écologiques d'un site partagé, et la compatibilité des fleurs ; dépend d'un catalogue, non du rendu des tuiles Unity.

**Expéditions** : valide trajet/mode/coûts/sécurité, réserve civils+escorte, passe par automate, clôture une seule fois, publie son résultat. Ne crédite pas directement une dizaine de services via callbacks fragiles.

**Projets** : reçoit les événements d'expédition validés, garantit contribution unique, met à jour paliers, participants distincts et récompenses.

**Alliances / droits** : résout adhésion, rôle, autorisations, niveau et statut de pacte depuis BeeKingdom.Alliance. Fournir des ports de lecture si HiveOperations est consommé par Alliance (éviter références circulaires).

**World map client** : affiche les objets de façon identique à deux clients, saisit commandes, maintient filtres/zoom, jamais des montants d'autorité.

## Stratégie de code par lots

**Lot 0** : trancher P0 (README/14/17/20), définir interfaces minimales et stockage commun.  
**Lot 1** : CivilPopulation + sous-état append-only/migration, sans sortie sur carte.  
**Lot 2** : FloralCatalog + FloralSite en base autoritaire et lectures publiques.  
**Lot 3** : expédition bout en bout + conservation de ressources et de civils.  
**Lot 4** : projet d'alliance + niveaux réels et participation distincte.  
**Lot 5** : extensions championne/recherche/génétique puis relais/escorte/pactes.  
**Lot 6** : événements et raid de l'ours.

Une nouvelle classe ne doit pas être ajoutée uniquement pour « faire architecture » ; préférer services spécialisés petits et testables alignés au dépôt.

## Contraintes de dépendance

- Le backend ne référence pas UnityEngine.
- Un client Unity ne dépend pas d'un assembly de persistance serveur.
- HiveOperations n'ajoute pas de dépendance directe vers BeeKingdom.Alliance si celle-ci le référence déjà.
- Les modèles API (DTO) ne doivent pas exposer des entités de repository ni des données confidentielles d'autres ruches.
- La couche d'agrégation peut orchestrer transactions globales, pas les composants de présentation.
- Réutiliser IServerClock et le pattern receipts, pas DateTime.Now ou timers Unity pour le temps officiel.

## Mode « simulation » vs mode « officiel »

Le code Assets/BeeKingdom/World/FlowerManager.cs existe ; il gère des fleurs en mémoire. Deux modes explicites :
1. **Simulation locale/Editor/lab** : peut appeler FlowerManager.Advance, Harvest et déclencher événements de démonstration.
2. **Monde MMO officiel** : lecture seule d'un état floral serveur ; aucun Harvest local créditant ressources, pollen transporté ou score alliance. Des animations peuvent interpoler visuellement des phases officielles.

**Fournir une preuve de non-double-simulation** sur la scène canonique. Au besoin, l'adaptateur filtre les effets purement cosmétiques selon le contexte.

## Source d'identité des objets

L'identifiant d'un site doit rester stable et globalement unique, indépendant du zoom, d'un prefab recréé, de l'ordre d'itération ou du device. Proposition : monde/region/site + version de catalogue, avec ID métier figé.

Le fournisseur de contenu Unity utilise ces IDs pour placer/repérer un objet. Le serveur valide leur existence et leur propriétaire ; les coordonnées d'un client ne font jamais autorité sur la collecte.

## Feature flags et compatibilité

Flags distincts et graduels : civil_population, floral_sites, civil_expeditions, alliance_floral_projects, floral_relay, escort, bloom_pacts, events.

- OFF : les anciennes APIs continuent de fonctionner comme avant.
- READ_ONLY : voir les sites sans actions, utile pour QA.
- ON pour comptes QA sélectionnés : vraie transaction sur serveur.
- DRAIN : blocage des nouveaux départs, clôture des missions lancées.

Les noms et implémentations des flags doivent suivre les options/config du projet ; ne pas créer un deuxième mécanisme parallèle.

## Affichage temps réel

Pour petite Alpha, une lecture paginée/rafraîchissement raisonnable suffit ; des notifications événementielles sont un plus si le contrat temps réel existant le permet. Les clients dédupliquent par revision/eventId, n'écrasent pas une lecture plus récente par une réponse périmée et ne clignotent pas à chaque refresh.

## Scalabilité

- Éviter une lecture de **toutes les ruches** à chaque interrogation du site.
- Index par siteId/projetId, pagination des participants, agrégats pour jauges.
- Calculer régénération depuis lastSettledAtUtc jusqu'à serverNow avec bornes et version, au lieu de lancer un tick chaque frame pour toutes les fleurs.
- Distinguer marche civile, marche militaire et « présence ambiante » échantillonnée.

## Qualité

Contrats versionnés, test concurrent multi-session, reprise après crash, migration additive, ressources exact-once, contrôle de droits, tests de non-régression militaires/Alliance/Académie, localisation FR/EN, QA visuelle.

