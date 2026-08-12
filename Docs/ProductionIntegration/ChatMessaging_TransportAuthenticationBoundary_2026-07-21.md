# Frontière d’authentification du transport

La route `GET /chat/v1/capabilities` est l’unique route publique: aucun bearer, cookie ou corps n’est requis; elle émet les en-têtes anti-cache. Les routes REST métier et le hub temps réel passent par l’authentification et l’autorisation chat; les erreurs restent JSON structurées, sans redirection HTML.

Le candidat local inclut la migration additive 064 du contrat SQL (ClientRequestId 256, Body 4000, traductions locale 35/modèle 128/texte 16000) et conserve `server=false`, `realtime=false`, `PreparationOnly`.

Candidat: `BeeKingdom.Server.20260721T183655Z`, 54 fichiers, `DeploymentAuthorized=false`. Smoke local Healthy; suite complète net10: 247 réussis, 7 SQL ignorés.

La matrice exhaustive transport Unity reste à rejouer sur l’hôte HTTP .NET 8 autorisé; aucun déploiement public n’est effectué.
