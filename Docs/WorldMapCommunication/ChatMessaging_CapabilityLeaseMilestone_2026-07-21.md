# Chat et messagerie — bail de capabilities

Date : 2026-07-21  
Responsable : Communication

## Résultat

Une négociation capabilities n'est plus valable indéfiniment. `ChatCapabilityLeasePolicy` lui attribue une durée de cinq minutes par défaut, configurable entre 30 secondes et une heure par `RemoteChatClientOptions.CapabilityLeaseDuration`.

La date de négociation utilise l'horloge injectée. À l'expiration exacte du bail ou si l'horloge recule, la composition stricte invalide le contrat, restaure la fenêtre locale de rejeu non négociée et retourne `capability_lease_expired`, HTTP 0. Aucun journal, aucune session et aucun appel métier ne sont touchés.

`DisconnectAsync` invalide le contrat même si la déconnexion temps réel échoue. `InvalidateCapabilities` permet au cycle de vie mobile de forcer explicitement une nouvelle négociation au passage en arrière-plan, changement de réseau, changement de compte ou changement de configuration distante.

Après renégociation, les nouvelles limites, fonctionnalités et rétention reprennent autorité avant la prochaine opération.

## Validation

- Compilation isolée Communication : réussie, sans erreur ni avertissement.
- Suite ciblée : 83/83 réussie.
- Bail valide à cinq minutes, refus à cinq minutes plus un tick sans session ni réseau.
- Renégociation réussie puis lecture authentifiée avec une seule acquisition de session.
- Déconnexion suivie d'une lecture : `capability_negotiation_required`.
- Durées inférieures à 30 secondes ou supérieures à une heure rejetées.
- Aucun déploiement, activation ni synchronisation effectué.

## Directive d'intégration

Sur Unity Android, appeler `InvalidateCapabilities` dès l'entrée en arrière-plan, avant changement de compte et lors d'un changement de connectivité significatif. Au retour, exécuter capabilities avant session, drainage, synchronisation, traduction ou websocket. En staging, modifier `idempotencyReceiptRetentionDays` et une limite pendant que l'application est suspendue; vérifier que l'ancien contrat n'est jamais réutilisé et que les nouvelles valeurs gouvernent le drainage. Les réponses capabilities doivent être non mises en cache par un intermédiaire au-delà du bail client; fournir des en-têtes de cache explicites appropriés. Le nouveau candidat serveur intégrant la rétention reste local et `DeploymentAuthorized=false`.
