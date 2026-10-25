# 24 — API contract-first : routes candidates, formes JSON et erreurs

**Tous les exemples ci-dessous sont PROPOSÉS, non existants.** Les routes /game/v1 de la collecte et /alliance/v1 du système d'alliance EXISTENT déjà ; choisir les nouveaux chemins sans collision après audit de Program.cs.

## Convention obligatoire

- Authenticité OAuth/session conforme au projet, résolution de PlayerId uniquement depuis jeton serveur.
- Toute mutation inclut idempotencyKey (même clé = même charge) et expectedRevision de l'agrégat requis.
- Une réponse réussite comprend contractVersion, serverTimeUtc, révisions, état mis à jour et receipt de l'opération.
- Erreurs : code stable localisable + HTTP cohérent + détails de blocage sans fuite des données d'autres joueurs.
- Les sérialisations C#/Unity respectent la casse et les noms dans les transports déjà utilisés.

## API civils proposées

GET /game/v1/hives/{hiveId}/civil-population  
POST /game/v1/hives/{hiveId}/civil-cohorts/start  
POST /game/v1/hives/{hiveId}/civil-cohorts/{cohortId}/claim

~~~json
{
  "contractVersion": "bk-civil-population-v1-proposed",
  "serverTimeUtc": "2026-10-20T18:00:00Z",
  "hiveId": "00000000-0000-0000-0000-000000000001",
  "revision": 10,
  "availableBySpecialty": {"general_forager": 240},
  "reservedBySpecialty": {"general_forager": 100},
  "recoveringBySpecialty": {"general_forager": 20},
  "civilMarchSlots": {"used": 1, "capacity": 2},
  "activeCohort": null,
  "activeExpeditionIds": ["11111111-1111-1111-1111-111111111111"]
}
~~~

Les valeurs sont illustratives et ne créent aucune population réelle. Distinguer population déclarée des effectifs calculés en projection.

## API monde proposées

GET /game/v1/world/floral-sites?regionId={regionId}&cursor={cursor}&limit={limit}  
GET /game/v1/world/floral-sites/{siteId}

~~~json
{
  "contractVersion": "bk-floral-site-v1-proposed",
  "serverTimeUtc": "2026-10-20T18:00:00Z",
  "siteId": "clover-field-a",
  "regionId": "region-a",
  "familyId": "clover",
  "revision": 74,
  "phase": "blooming",
  "fertilityBp": 7200,
  "biodiversityBp": 5800,
  "nectarStock": 700,
  "pollenStock": 310,
  "isPublic": true,
  "access": {"canForage": true, "canStartProject": false, "reasonCode": null},
  "activeProjectSummary": {"projectId": "project-a", "progressPoints": 6400, "goalPoints": 10000, "qualifiedPlayers": 8, "requiredPlayers": 12}
}
~~~

Important : l'indice fertilité (7200 bp = 72 %) est différent du nombre de points du projet.

## Préparer puis lancer une expédition

POST /game/v1/hives/{hiveId}/civil-expeditions/preview : **option de lecture sans mutation**, TTL court ; retourne verdict, coûts, ETA et raisons de blocage. Le serveur **recalcule** à l'appel start, même si preview « possible ».

POST /game/v1/hives/{hiveId}/civil-expeditions/start :

~~~json
{
  "expectedHiveRevision": 128,
  "expectedSourceRevision": 74,
  "idempotencyKey": "launch-20261020-abc1234567",
  "sourceSiteId": "clover-field-a",
  "destinationSiteId": "clover-field-b",
  "missionMode": "balanced",
  "workers": [{"specialty": "general_forager", "count": 100}],
  "championId": null,
  "escortReference": null,
  "projectId": "project-a"
}
~~~

~~~json
{
  "contractVersion": "bk-civil-expedition-v1-proposed",
  "serverTimeUtc": "2026-10-20T18:00:01Z",
  "hiveRevision": 129,
  "expedition": {
    "expeditionId": "11111111-1111-1111-1111-111111111111",
    "state": "outbound",
    "sourceSiteId": "clover-field-a",
    "destinationSiteId": "clover-field-b",
    "workers": [{"specialty": "general_forager", "count": 100}],
    "startedAtUtc": "2026-10-20T18:00:01Z",
    "estimatedCompleteAtUtc": "2026-10-20T18:25:01Z"
  },
  "receipt": {"operation": "start", "idempotencyKey": "launch-20261020-abc1234567", "code": "game.civil_expedition_started"}
}
~~~

GET /game/v1/hives/{hiveId}/civil-expeditions?cursor=... : liste paginée.  
GET /game/v1/hives/{hiveId}/civil-expeditions/{expeditionId} : détail.  
POST /game/v1/hives/{hiveId}/civil-expeditions/{expeditionId}/claim : achèvement/récompense si modèle claim manuel.  
POST /game/v1/hives/{hiveId}/civil-expeditions/{expeditionId}/recall : retour si autorisé.

**À trancher :** règlement des missions en tâche de fond avec auto-claim ou fin « prête à réclamer ». Ne pas exiger claim manuel pour contribuer au projet si cela rend les opérations hors ligne fausses ; variante recommandée : **effet écologique/collectif automatique**, récolte créditée exactement une fois au retour/claim selon modèle retenu.

## API alliance proposées

Sous /alliance/v1 si cela correspond à la couche choisie :
- GET /alliance/v1/alliances/{allianceId}/floral-progression
- GET /alliance/v1/alliances/{allianceId}/floral-projects
- POST /alliance/v1/alliances/{allianceId}/floral-projects
- GET /alliance/v1/alliances/{allianceId}/floral-projects/{projectId}
- GET /alliance/v1/alliances/{allianceId}/floral-contributions
- POST /alliance/v1/alliances/{allianceId}/bloom-pacts (Bêta, si compatible diplomatie).

L'allianceId demandé ne constitue pas une autorisation d'ouvrir un projet ; vérifier que le player acteur est membre ayant la permission.

## Nommage des résultats

Exemples de codes stables (à aligner sur convention du dépôt) :
- game.civil_population_disabled
- game.civil_cohort_busy
- game.civil_population_capacity
- game.civil_workers_insufficient
- game.civil_march_slots_exhausted
- game.floral_site_unknown
- game.floral_site_not_blooming
- game.floral_site_depleted
- game.floral_route_incompatible
- game.floral_access_forbidden
- game.floral_project_not_qualified
- game.civil_expedition_recalled
- game.civil_expedition_not_ready
- game.civil_expedition_already_claimed
- game.idempotency_conflict
- game.revision_conflict
- game.insufficient_resources

L'UI doit traduire chaque code en fr-CA/en-US et proposer un recours pertinent.

## Sémantique HTTP proposée

- 200/201 réussite ; 202 accepté en traitement asynchrone si contrat le prévoit.
- 400 payload invalide/limites.
- 401 session absente ; 403 droits insuffisants.
- 404 cible absente/non accessible (ne pas divulguer informations secrètes).
- 409 revision, simultanéité, occupation/capacité ou idempotency conflict.
- 422 prérequis fonctionnel incompatibilité non syntaxique si convention du projet la supporte.
- 503 fonctionnalité désactivée/indisponible.

Les 409 doivent contenir un code et un snapshot/état actuel si autorisé, sans réessayer automatiquement avec une **nouvelle clé** comme si le premier appel n'avait jamais existé.

## Versionnement et parité client/serveur

Toutes les modifications de schéma en version 1 sont additives. En cas de champ inconnu, client applique comportement sûr ; en cas de contractVersion incompatible, désactiver mutation et expliquer la mise à jour nécessaire. Tester serialization dans les deux langues et plusieurs sessions.

## Exploitation et limites

Rate-limit par acteur et cible, longueur bornée des IDs et des maps/arrays, pagination, auth de tous les endpoints sensibles. Les snapshots publics ne doivent pas divulguer roster privé ou l'identité de tous les joueurs non autorisés.

