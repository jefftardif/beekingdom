# Chat et messagerie — négociation obligatoire avant mutation

Date : 2026-07-21  
Responsable : Communication

## Résultat

La composition créée par `RemoteChatClientFactory` active maintenant une garde stricte de capabilities. Aucun message, création de conversation, curseur de lecture, signalement ou drainage persistant ne peut commencer avant une négociation réussie avec un serveur annoncé actif.

Avant négociation, ou après une négociation où `server=false`, ces opérations retournent `RemoteChatError.Incompatible`, code `capability_negotiation_required`, HTTP 0. La garde s'exécute avant validation métier, lecture/écriture de journal, acquisition de session ou appel REST.

Cette séquence garantit notamment que `idempotencyReceiptRetentionDays`, les limites de corps/destinataires et les fonctionnalités lecture/modération sont connues avant de créer une intention durable. Après une négociation valide, la garde s'ouvre et les mutations utilisent les valeurs effectives négociées.

Le constructeur bas niveau conserve un mode non strict par défaut pour les laboratoires et doubles historiques; la fabrique distante destinée à l'intégration Unity active toujours le mode strict.

## Validation

- Compilation isolée Communication : réussie, sans erreur ni avertissement.
- Suite ciblée : 81/81 réussie.
- Cinq chemins avant négociation retournent `capability_negotiation_required`.
- Zéro appel réseau et quatre journaux vides après les refus.
- Cinq diagnostics sûrs, sans contenu métier.
- Après négociation `chat-v1` valide, un envoi identique est accepté et acquitté.
- Aucun déploiement, activation ni synchronisation effectué.

## Directive d'intégration

L'ordre de démarrage Unity/staging devient contractuel : construire la partition protégée → lire `/capabilities` → vérifier protocole, serveur, limites, fonctionnalités et rétention → acquérir/rafraîchir la session → drainer → synchroniser. Ne jamais lancer automatiquement un drainage au simple retour réseau avant la nouvelle négociation. Le serveur doit garder `/capabilities` accessible sans bearer et sans divulgation, y compris lorsque le chat est désactivé. Mettre à jour les tests Android de démarrage à froid et retour d'arrière-plan pour prouver zéro stockage/mutation avant négociation. Le candidat serveur doit être reconstruit après ajout du champ de rétention et rester `DeploymentAuthorized=false`.
