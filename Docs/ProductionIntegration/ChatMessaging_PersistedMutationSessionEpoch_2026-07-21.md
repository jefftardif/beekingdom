# Époque de session des mutations persistantes

Le serveur conserve les frontières suivantes pour les mutations chat:

| Frontière | Preuve attendue |
|---|---|
| avant commit | annulation/session changée: aucune écriture, reçu, quota, séquence ou publication |
| après commit sans reçu client | le message/reçu durable existe; reprise même joueur avec le même `ClientRequestId` retourne le résultat dédupliqué |
| drainage même joueur | la partition restaurée est envoyée dans l’ordre, chaque reçu une fois |
| cross-player | même identifiant/conversation d’un autre joueur: 401/403 avant lecture du reçu ou mutation |
| zéro octet HTTP | rejet local/session epoch: aucune requête REST comptée côté serveur |

Preuves locales existantes:

- `CancellationBeforeCommitHasNoEffectAndDisconnectAfterCommitReplaysReceipt`;
- `Idempotency_receipt_cannot_cross_player_boundary`;
- `Translation_cache_is_authorized_before_read_for_other_player`.

## Matrice staging à exécuter après dégel

1. A commence une écriture locale, logout pendant la sauvegarde: l’entrée reste conservée, `local_session_changed`, zéro HTTP.
2. Réponse/commit tardif après logout: ne pas acquitter localement; reprendre avec A et le même reçu, sans doublon.
3. B tente le même reçu: refus avant accès et aucune mutation.
4. A revient, renégocie capabilities/session, puis draine; vérifier une seule mutation, séquence et reçu.

Le gel Unity est respecté. Candidat conservé `BeeKingdom.Server.20260721T201425Z`; `DeploymentAuthorized=false`, aucun transfert/activation/synchronisation.
