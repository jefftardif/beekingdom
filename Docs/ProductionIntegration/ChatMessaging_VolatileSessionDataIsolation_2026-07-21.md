# Isolation des états de session volatils

Le serveur autorise une traduction seulement après résolution du message et vérification de la participation en lecture; le cache `(MessageId, TargetLocale, ModelVersion)` n’est donc jamais servi à un joueur non autorisé. Les reçus d’envoi/création/signalement restent indexés par joueur et conversation selon leur contrat idempotent.

Preuve ajoutée: `Translation_cache_is_authorized_before_read_for_other_player`. Elle remplit d’abord le cache avec A, puis vérifie que B reçoit `UnauthorizedAccessException` avant toute lecture utile du cache.

## Matrice Android étendue

| Transition | États volatils attendus | Journaux persistants |
|---|---|---|
| A hors ligne | messages fusionnés, index ClientRequestId, traductions et séquences A | conservés dans la partition A |
| logout | purge complète des messages/traductions/séquences/compteurs volatils | aucune suppression de la partition A |
| B connecté | état volatil vide; aucun cache ou reçu A visible | partition B distincte |
| retour A | nouvel appel de traduction requis après déconnexion; séquences rechargées depuis A | reçus/journaux A restaurables et drainables une fois |
| mismatch A→B | purge immédiate avant lecture/écriture ou HTTP suivant | opération A conservée |

Le candidat reste `DeploymentAuthorized=false`; SQL/.NET8/TLS/SNI/IIS et Android staging demeurent les portes externes.
