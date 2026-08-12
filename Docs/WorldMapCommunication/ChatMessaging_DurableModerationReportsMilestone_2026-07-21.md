# Bee Kingdom - Jalon signalements de moderation durables

Date: 2026-07-21  
Agent: `Communication`

## Livraison

Le contrat de signalement exige maintenant un `ClientRequestId` stable en plus de
la categorie. Le client journalise avant reseau:

- message vise;
- categorie originale normalisee par trim;
- identifiant de requete;
- compteur de tentatives;
- version de schema.

Une reprise apres redemarrage soumet le meme dossier. Une cle reutilisee pour un
autre message ou une autre categorie est refusee avant reseau.

Politique:

- succes: journal acquitte et supprime;
- 401, 429, 5xx, annulation ou perte reseau: journal conserve;
- 403, 404, 409 ou reponse definitive invalide: journal retire;
- corruption/version inconnue: contenu preserve et erreur explicite.

Le journal concret utilise un `IChatStringStore` injecte. Aucun contenu du message
signale n'est copie dans le journal: seul son identifiant est conserve. La
moderation continue de relire et traiter l'original cote serveur.

## Verification

- 38 tests Communication executes;
- 38 reussis;
- 0 echec;
- compilation: 0 erreur, 0 avertissement.

Les nouveaux scenarios couvrent reprise apres redemarrage, identite stable,
collision locale, 403 definitif, 429 conservable et journal schema v1.

## Changement serveur requis

La requete devient:

```json
{
  "clientRequestId": "report-uuid-stable",
  "category": "spam"
}
```

Integrateur doit ajouter un recu persistant unique par rapporteur et
`ClientRequestId`, lie au `ReportId` et au hash de `MessageId + Category`. Un retry
identique retourne le meme rapport; un payload different retourne 409 sans creer
un second dossier. Le debit ne doit pas etre recompte pour un retry connu.

## Fichiers du jalon

Crees:

- `Docs/WorldMapCommunication/ChatMessaging_DurableModerationReportsMilestone_2026-07-21.md`

Modifies:

- `Assets/BeeKingdom/Gameplay/Communication/RemoteChatContracts.cs`
- `Assets/BeeKingdom/Gameplay/Communication/UnityChatJsonCodec.cs`
- `Assets/BeeKingdom/Gameplay/Communication/ServerChatProvider.cs`
- `Assets/BeeKingdom/Gameplay/Communication/VersionedChatPendingSendStore.cs`
- `Assets/BeeKingdom/Tests/Editor/ServerChatProviderTests.cs`

Aucun deploiement, secret, drapeau de production ou synchronisation n'a ete
ajoute ou active.
