# Chat et messagerie — contournement du cache capabilities

Date : 2026-07-21  
Responsable : Communication

## Résultat

`ChatTransportRequest` porte maintenant l'intention `BypassCache`. Seul `GetCapabilitiesAsync` l'active. `UnityWebRequestChatRestTransport` traduit cette intention en deux en-têtes :

- `Cache-Control: no-cache, no-store, max-age=0`
- `Pragma: no-cache`

La requête reste sans bearer. Les listes, messages, mutations et autres requêtes métier n'héritent pas de cette option et conservent leur authentification normale.

Cette défense client complète le bail capabilities : une renégociation ne doit pas accepter silencieusement une représentation ancienne conservée par la couche HTTP mobile, un reverse proxy ou un CDN.

## Validation

- Compilation isolée Communication : réussie, sans erreur ni avertissement.
- Suite ciblée : 84/84 réussie.
- Première requête : `/chat/v1/capabilities`, `BypassCache=true`, bearer absent.
- Requête métier suivante : `BypassCache=false`, bearer présent.
- Les 83 essais précédents de bail, rétention, persistance et sécurité restent verts.
- Aucun déploiement, activation ni synchronisation effectué.

## Directive d'intégration

Le serveur, IIS et tout proxy/CDN doivent répondre à `/chat/v1/capabilities` avec une politique cohérente, au minimum `Cache-Control: no-store, no-cache, max-age=0, must-revalidate` et un `Vary` approprié si une représentation varie. Le préflight staging doit refuser une réponse cacheable, une redirection ou un `Age` positif. Tester deux lectures encadrant une modification locale de configuration : la seconde doit refléter immédiatement `idempotencyReceiptRetentionDays`, limites et portes. Conserver la route sans bearer et sans contenu sensible. Reconstruire le candidat seulement après intégration du champ de rétention et de ces en-têtes; maintenir `DeploymentAuthorized=false`.
