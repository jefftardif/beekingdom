# Bee Kingdom - Jalon contrat JSON Unity

Date: 2026-07-21  
Agent: `Communication`

## Livraison

Le transport REST possede maintenant un codec JSON concret compatible avec
`JsonUtility`. Des DTO filaires internes utilisent explicitement les noms camelCase
du serveur ASP.NET Core, sans dependre d'une bibliotheque JSON additionnelle.

Le codec couvre:

- capacites;
- pages de conversations et messages;
- creation de conversation;
- envoi et resultat idempotent;
- curseur de lecture;
- signalement de moderation;
- traduction a la demande;
- conservation du corps original dans `OriginalBody`.

Le backend JSON est injectable. Le runtime emploie `UnityJsonBackend`; les tests
emploient un backend gere sans fonction native Unity. Un type de requete ou de
reponse inconnu est refuse explicitement plutot que serialize silencieusement avec
une forme incorrecte.

## Verification

- 11 tests executes;
- 11 reussis;
- 0 echec;
- compilation: 0 erreur, 0 avertissement.

La suite valide notamment les noms camelCase, le mapping du message original, la
sequence serveur et le refus des DTO inconnus. Aucun appel reseau reel n'est fait.

## Contrat remis a Integrateur

Integrateur doit verifier que la politique JSON ASP.NET Core reste camelCase et
que les reponses exposent les champs attendus, en particulier `body`,
`senderPlayerId`, `acceptedAtUtc`, `sequence`, `clientRequestId`,
`nextAfterSequence`, `deduplicated` et `serverSequence`.

Tout ecart doit etre corrige par contrat versionne ou signale a Communication;
aucun renommage silencieux ne doit atteindre la production.

## Fichiers du jalon

Crees:

- `Assets/BeeKingdom/Gameplay/Communication/UnityChatJsonCodec.cs`
- `Assets/BeeKingdom/Gameplay/Communication/UnityChatJsonCodec.cs.meta`
- `Docs/WorldMapCommunication/ChatMessaging_UnityJsonContractMilestone_2026-07-21.md`

Modifies:

- `Assets/BeeKingdom/Gameplay/Communication/RemoteChatContracts.cs`
- `Assets/BeeKingdom/Gameplay/Communication/ServerChatProvider.cs`
- `Assets/BeeKingdom/Tests/Editor/ServerChatProviderTests.cs`

Aucun fichier LivingHive, scene, image, carte ou configuration de production n'a
ete modifie. Aucune synchronisation ou activation n'a ete effectuee.
