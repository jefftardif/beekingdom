# M078-CL — Daily Round server_unavailable

Date : 2026-09-10

## Cause

Même cause exactement que M077B-CL (Objectifs du Royaume) et le correctif
Courrier avant lui : `HiveDailyRoundOptions.Enabled` vaut `false` par défaut
(`appsettings.json` du dépôt, jamais commité à `true` par design — les
secrets/flags de prod vivent en variables d'environnement IIS, jamais dans
le dépôt) et cette variable n'avait jamais été positionnée sur le serveur
`api-ops`.

Confirmé par requête directe, sans authentification :

```
GET /game/v1/hives/<guid>/daily-round → 503 (avant correctif)
```

Un `503` (et non un `404`) prouve que la route existe déjà en production —
le code serveur Daily Round est déjà déployé, correctement mappé, et le
contrat client (`HiveDailyRoundClient.cs`, chemin `/daily-round`,
`ContractVersion = "living-hive-daily-round-v1"`) correspond exactement à
celui du serveur. Aucun désalignement client/serveur, aucune route
incorrecte, aucun système à reconstruire.

## Correction

Aucun code touché. Flag activé en variable d'environnement IIS sur
`api-ops` (procédure BeeKingdom existante, même geste que M077B-CL) :

```
appcmd set config "BeeKingdomApi/" -section:system.webServer/aspNetCore /+"environmentVariables.[name='HiveDailyRound__Enabled',value='true']" /commit:apphost
appcmd recycle apppool "BeeKingdomApi"
```

M076 Combat Patrol, M077 Objectifs du Royaume et l'Évènement jalon non
touchés.

## Validation

Depuis le même serveur que le CEO :

```
GET /game/v1/hives/<guid>/daily-round  → 401 (session requise) au lieu de 503
```

Confirme que la route est maintenant active et attend une session
authentifiée réelle, exactement le même comportement que Objectifs du
Royaume et Évènement jalon.

## Compilation / tests

Aucun changement de code — aucune compilation ni suite de tests requise.

## Commit

Aucun commit de code (rien à committer). Ce rapport seul.

## READY FOR CEO DAILY ROUND RETEST
