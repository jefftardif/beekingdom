# LivingHive Chat — contrat du pont de cycle de session

Date : 2026-07-21  
Responsable : Communication  
État : **coordinateur implémenté et testé**, raccordement au shell officiel encore ouvert

## But

Relier `LivingHiveChatBootstrap` au futur shell mobile d’authentification sans placer de secret, de compte fictif ou de logique d’autorité dans l’interface LivingHive.

Le shell demeure propriétaire de la session authentifiée, du renouvellement du jeton et des événements de connexion/déconnexion. Communication reçoit uniquement des dépendances injectées et sérialise leur application au runtime chat.

## Entrées injectées

Une liaison de session doit fournir ensemble :

- `RemoteChatClientOptions`, dont l’URL canonique et `StoragePartitionId` ;
- un `IChatSessionSource` vivant, capable de refléter le jeton renouvelé ;
- le `IChatStringStore` de l’appareil ;
- le `IChatDataProtector` associé au compte/appareil ;
- le transport temps réel optionnel ;
- le puits de diagnostics optionnel.

Le pont ne conserve jamais une copie du bearer. `StoragePartitionId` doit correspondre exactement au `PlayerId` retourné par la source de session avant toute composition.

## Transitions obligatoires

### Première authentification

1. vérifier l’identité et la partition ;
2. composer le client Communication ;
3. configurer le runtime ;
4. laisser l’ouverture de l’overlay déclencher la lecture, tout en permettant au shell d’ouvrir la connexion en arrière-plan selon sa politique.

### Renouvellement du jeton du même joueur

La source de session existante doit exposer le nouveau jeton. Aucun second contrôleur, aucune seconde connexion temps réel et aucune seconde boucle de polling ne doivent être créés simplement parce que le bearer change.

### Changement de joueur

1. incrémenter l’époque locale et annuler les opérations de l’ancien joueur ;
2. attendre la fermeture de son polling et de son transport temps réel ;
3. vider messages, conversations, compteurs, traductions et brouillon volatils ;
4. conserver ses journaux et son cache récent uniquement dans sa partition protégée ;
5. vérifier la nouvelle identité ;
6. composer le nouveau contrôleur avec une partition distincte.

À aucun instant une réponse tardive A ne peut devenir visible dans la session B.

### Logout ou arrêt du shell

Le pont appelle `LivingHiveChatBootstrap.LogoutAsync`/`LivingHiveChatRuntime.ResetAsync`. Une simple fermeture de l’overlay n’appelle jamais cette transition.

## Concurrence et annulation

- une seule transition de session est appliquée à la fois ;
- toute nouvelle transition annule la précédente avant d’attendre le verrou de cycle de vie ;
- une activation annulée doit vérifier son jeton d’annulation avant de publier le nouveau contrôleur ;
- en cas d’échec partiel, le runtime revient à `NotConfigured` et ne conserve pas un contrôleur de l’ancien joueur ;
- le logout est idempotent ;
- aucune boucle `async void` ni tâche non observée n’est autorisée.

## Preuves exigées avant ratification

1. première session : une composition, partition exacte ;
2. notification répétée du même joueur : aucune recomposition ;
3. renouvellement bearer via la même source : aucune reconnexion ;
4. A vers B : fermeture A terminée avant activation B ;
5. activation A retardée puis logout : A ne peut pas configurer le runtime après le logout ;
6. activation A retardée puis B : aucune donnée, reçu ou événement A visible dans B ;
7. mismatch `PlayerId`/partition : refus avant création des stores et avant HTTP ;
8. échec d’activation : runtime `NotConfigured`, nouvelle tentative possible ;
9. logout répété : sans effet secondaire ni exception ;
10. fin de test : aucune tâche de polling ou connexion temps réel résiduelle.

## Frontière serveur

Le serveur continue de dériver le joueur exclusivement du bearer, de vérifier les appartenances avant toute lecture et de cloisonner reçus, curseurs et cache de traduction. Il ne fait jamais confiance à `StoragePartitionId`, aux corps restaurés, aux compteurs ou à l’époque déclarée par l’appareil.

Une validation staging devra prouver la séquence A connecté → requête retardée → logout → B connecté → réponse A ignorée → retour A → reprise idempotente.

## Porte ouverte explicite

Aucun shell d’authentification de production n’est présent dans la copie actuelle. Cette spécification ne rend donc pas le chat jouable en production et ne remplace pas l’appel réel à `LivingHiveChatBootstrap.ActivateAsync`.

Le coordinateur `LivingHiveChatSessionCoordinator` et le contrat `IChatAccountSessionReadiness` sont maintenant livrés dans `LivingHiveChatBootstrap.cs`. `DelegateChatAccountSessionReadiness` permet au futur shell d’exposer son garde-fou vivant sans créer de référence inverse entre les assemblages `BeeKingdom.Gameplay` et `BeeKingdom.Networking`.

Preuve autonome : 145/145 tests Communication réussis dans `LivingHiveChatSessionBridgeFinal145.trx`. La compilation Unity globale 6000.5.3f1 a également été confirmée par Architecte avec zéro `error CS` après suppression du couplage d’assemblage.

Aucun Asset, présentateur, secret, déploiement, transfert, activation ou synchronisation n’a été modifié pour ce document.
