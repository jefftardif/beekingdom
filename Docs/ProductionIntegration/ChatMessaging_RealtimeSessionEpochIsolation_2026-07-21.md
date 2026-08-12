# Isolation d’époque des sessions realtime

`ChatRealtimeHub.OnConnectedAsync` valide le bearer à chaque nouvelle connexion, capture le `PlayerId` dans `Context.Items` propre à la connexion et n’autorise `JoinConversation` qu’après `EnsureCanRead` pour ce joueur. Les groupes sont ajoutés avec le `ConnectionId` courant; une reconnexion crée donc une nouvelle association auth/groupes. SignalR retire automatiquement l’ancienne connexion de ses groupes à sa fermeture; aucun état de groupe n’est partagé dans un singleton applicatif.

Les événements publiés par le service sont liés au commit et à la conversation; ils ne sont pas rejoués vers une nouvelle connexion par le serveur. La vérification d’époque après gap REST et avant fusion reste une responsabilité du client, couverte par le jalon Communication.

## Course staging à ajouter

1. Connexion A, authentification et abonnement conversation.
2. Émettre un événement retardé; bloquer la livraison côté client sur un gap REST.
3. Logout A puis connexion B; vérifier nouvelle authentification, nouveau `ConnectionId`, groupes B uniquement.
4. Libérer la réponse REST/événement retardé A: l’événement est ignoré (`local_session_changed`), sans message, séquence, reçu ou compteur volatil.
5. Retour A: reconnexion et réabonnement; drainage du journal persistant sans doublon.

Candidat conservé: `BeeKingdom.Server.20260721T201425Z`; `DeploymentAuthorized=false`. Aucun déploiement ou activation.
