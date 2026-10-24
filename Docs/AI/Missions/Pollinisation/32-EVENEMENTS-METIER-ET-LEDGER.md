# 32 — Événements métier et journal de comptabilité

## Règle directrice

**Une action de gameplay durable = fait observable, versionné et idempotent.** Les animations, notifications et classements sont des projections, pas des sources d'autorité.

## Événements logiques recommandés

- CivilCohortStarted / CivilCohortReady / CivilCohortClaimed
- CivilWorkersReserved / CivilWorkersReleased / CivilWorkersInjured / CivilWorkersRecovered
- CivilExpeditionStarted / ArrivedAtSource / Harvested / ArrivedAtDestination
- PollenDelivered / FloralSiteRegenerated / FloralSitePressureApplied
- CivilExpeditionRecalled / CivilExpeditionReturned / CivilExpeditionSettled
- PollinationContributionAccepted / PollinationProjectMilestoneReached
- AllianceFloralExperienceAwarded / AllianceFloralLevelIncreased
- FloralOutpostBuilt / FloralOutpostCaptured / FloralAccessRevoked
- BloomPactProposed / Accepted / Expired / Terminated
- BloomEventAnnounced / Started / Completed
- BearRaidStarted / BearRaidContributionRecorded / BearRaidCompleted / BearRaidRewardSettled

Ces noms sont **vocabulaire de domaine**, pas une instruction d'inventer un bus de messages entier.

## Enveloppe commune

~~~json
{
  "eventId": "unique-guid",
  "eventType": "PollenDelivered",
  "eventVersion": 1,
  "occurredAtUtc": "2026-10-20T18:25:00Z",
  "worldId": "world-a",
  "aggregateType": "CivilExpedition",
  "aggregateId": "expedition-guid",
  "aggregateRevision": 5,
  "actorPlayerId": "player-guid",
  "correlationId": "request-guid",
  "causationId": "delivery-guid",
  "catalogVersion": "floral-content-v1",
  "payload": {"siteId": "clover-b", "pollenUnits": 80, "projectId": "project-a"}
}
~~~

**Exemple non contractuel**. Log public : minimiser données personnelles et ne pas exposer IDs de session.

## Ledger vs cumul

Le ledger conserve le fait qu'une mission X a apporté Y pollen écologique au projet Z. La jauge visible est un **agrégat matérialisé ou recomputé** de ces faits. Une relecture d'un même eventId ne change jamais la jauge.

Ne pas faire « score += Y » sans contrainte unique par deliveryId ; sinon une panne réseau réémettra deux fois un gain.

## Séparation des trois soldes

**Ressources du joueur :** honey/pollen/wax dans PlayerHiveState.Resources.  
**Stocks floraux du site :** nectarStock/pollenStock de FloralSiteState.  
**Points écologiques/projet :** ledger PollinationContribution, pas monnaie spendable.

Le pollen « source » peut devenir du pollen du portefeuille **OU** être livré comme cargaison pollinisatrice selon une allocation comptable, pas les deux intégralement.

## Idempotence des phases

Clé recommandée : (expeditionId,phaseOrdinal,eventType). Une mission peut récolter plusieurs fois sur des arrêts programmés uniquement si le contrat de mission l'autorise (Alpha V1 non). Un champ « processedThroughPhase » évite qu'un événement de transit réémette toutes les phases précédentes.

Les receipts locaux en dictionnaire avec limite de taille **ne suffisent pas seuls** à protéger une économie MMO contre un rejeu historique après éviction : maintenir une contrainte de déduplication durable pour settlements/récompenses tant que l'entité existe.

## Synchronisation atomique et outbox

Si le ledger projet est dans un autre agrégat :
- transaction SQL commune, unique contrainte + révision atomique **ou**
- outbox transactionnelle et consommateur idempotent + mécanisme de compensation/reprise.

Jamais message externe émis avant que le débit du joueur ou la livraison soit réellement commis.

## Effets secondaires permis après commit

Notification joueur, activité alliance, mise à jour HUD, classement, mail in-game, analytics. Tous doivent tolérer événements en double et lecture dans un ordre différent.

## Reconciliation

Tâche de contrôle périodique ou à la lecture :
- missions bloquées en phase due ;
- réservations sans mission active ;
- récompenses marquées versées mais absentes du ledger ;
- projet dont cumul != somme ledger ;
- site à stock hors bornes ;
- événements non publiés par outbox.

Proposer réparation sûre uniquement à partir des événements sources ; ne pas inventer des crédits compensatoires automatiquement sans preuve.

## Audit

Chaque rapport QA peut fournir les lignes de ledger impactées, l'ID d'expédition, ID site, ID projet, soldes avant/après et méthode d'attribution. Les grands raids de l'ours suivront ces mêmes garanties de règlement.

