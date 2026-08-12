# Reçu d’envoi lié à l’identité authentifiée

Le service construit chaque `ChatMessage.SenderPlayerId` depuis le `PlayerId` authentifié transmis par `AuthenticationManager`; le DTO de requête ne contient aucune autorité permettant de choisir l’expéditeur. Le reçu durable et la relecture REST utilisent cette même valeur issue du commit.

Preuve ajoutée: `Authenticated_sender_receipt_is_derived_from_session_player` vérifie que l’expéditeur du message et de la page REST est exactement le joueur de session.

Nouveau candidat local: `BeeKingdom.Server.20260721T195742Z`, smoke Healthy, 54 fichiers, `DeploymentAuthorized=false`, `Chat/Realtime=false`, `PreparationOnly`. Il regroupe la migration SQL 064 et les contrôles de transport transitoires existants. Aucun déploiement ni activation publique.

Les essais SQL jetables, .NET 8, TLS/SNI et mobile staging restent des portes externes.
