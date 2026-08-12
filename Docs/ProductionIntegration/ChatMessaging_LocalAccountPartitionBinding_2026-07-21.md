# Liaison partition locale / compte — préparation staging

## Audit serveur

Toutes les opérations chat reçoivent le `PlayerId` issu de l’authentification et le transmettent au service/repository: conversations, liste/pages, messages, lectures, signalements, reçus d’idempotence et limites. Les reçus d’envoi sont indexés `(PlayerId, ConversationId, ClientRequestId)`; créations `(PlayerId, ClientRequestId)`; signalements `(ReporterPlayerId, ClientRequestId)`. Les curseurs de conversations intègrent une empreinte joueur et les traductions exigent une autorisation de lecture du message avant accès au cache/fournisseur.

Le corps des requêtes ne fournit aucune identité d’autorité. Un joueur B ne peut donc pas lire ou rejouer les ressources/récus de A avec le même identifiant de requête; une rotation de session du même joueur conserve la portée A.

## Matrice Android staging à exécuter

| Étape | Partition active | Vérifications attendues |
|---|---|---|
| A hors ligne | A | créer un envoi, création, lecture, signalement; journaux protégés et curseur restaurés, aucun HTTP |
| logout | aucune | fermer le client A; aucune suppression ou migration automatique de la partition |
| connexion B | B | négociation/capabilities puis session B; journaux, reçus, curseurs et traductions de A invisibles; même `ClientRequestId` ne rejoue rien de A |
| retour A | A | restaurer les quatre journaux et curseurs A; drainage idempotent, traduction cache A accessible seulement après autorisation; aucun doublon |
| refresh A→B | B | si le renouvellement change de joueur, bloquer avant lecture/écriture locale et avant seconde requête; conserver l’opération dans A |

Le scénario reste PreparationOnly: aucun hôte staging, secret ou transfert n’est utilisé dans la VM.

Preuve serveur ajoutée: `Idempotency_receipt_cannot_cross_player_boundary` (suite ciblée ChatTransport: 17/17). Nouveau candidat local: `BeeKingdom.Server.20260721T201425Z`, smoke Healthy, 54 fichiers, `DeploymentAuthorized=false`; le précédent 195742Z est révoqué automatiquement.
