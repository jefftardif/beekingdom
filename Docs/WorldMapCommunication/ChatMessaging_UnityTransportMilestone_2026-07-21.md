# Bee Kingdom - Jalon transport Unity Communication

Date: 2026-07-21  
Agent: `Communication`

## Livraison

Le pont distant dispose maintenant d'un transport concret base sur
`UnityWebRequest`, sans attente bloquante du thread Unity. Le transport:

- accepte une URL de base et un codec JSON injectables;
- ajoute le jeton de session par en-tete Bearer sans le persister;
- serialise les corps JSON et deserialise les reponses typees;
- propage l'annulation et interrompt la requete Unity;
- ne contient aucun domaine, compte ou secret de production.

`ServerChatProvider` prend aussi en charge:

- la creation de conversation avec `ClientRequestId` obligatoire;
- le polling REST de secours avec un maximum configurable de 1 a 8 tentatives;
- une temporisation injectable et annulable;
- l'arret immediat des retries sur erreur d'authentification;
- le passage explicite a `Offline` lorsque la limite est atteinte;
- la reprise et la fusion ordonnee des messages par sequence.

## Verification

Compilation contre les modules Unity locaux et execution NUnit autonome:

- 9 tests executes;
- 9 reussis;
- 0 echec;
- compilation: 0 erreur, 0 avertissement.

Scenarios couverts: envoi et retry idempotent, doublon SignalR/REST, ordre et trou
de sequence, polling de secours, deux pertes reseau puis reprise, limite stricte
de retry, session expiree, creation idempotente, cache/changement de langue,
retour a l'original et annulation avant transport.

La validation utilise uniquement des transports simules. Aucun appel public,
deploiement ou activation de fonctionnalite n'a eu lieu.

## Contrat serveur remis a Integrateur

Le serveur doit conserver l'idempotence de `ClientRequestId` pour la creation de
conversation et l'envoi, renvoyer des sequences monotones, accepter
`afterSequence` pour combler les trous et produire des statuts HTTP distincts
pour 401, 403 et 429. La traduction doit conserver le contrat du rapport
precedent.

Avant une activation production, Integrateur doit verifier que les DTO JSON
reels correspondent aux noms et formes du client, puis fournir une preuve
d'integration sur un environnement non public. Les drapeaux de production
restent desactives.

## Fichiers du jalon

Crees:

- `Assets/BeeKingdom/Gameplay/Communication/UnityWebRequestChatRestTransport.cs`
- `Assets/BeeKingdom/Gameplay/Communication/UnityWebRequestChatRestTransport.cs.meta`
- `Docs/WorldMapCommunication/ChatMessaging_UnityTransportMilestone_2026-07-21.md`

Modifies:

- `Assets/BeeKingdom/Gameplay/Communication/RemoteChatContracts.cs`
- `Assets/BeeKingdom/Gameplay/Communication/ServerChatProvider.cs`
- `Assets/BeeKingdom/Tests/Editor/ServerChatProviderTests.cs`

Aucune scene, interface LivingHive, image, carte ou configuration de production
n'a ete modifiee. Aucune synchronisation n'a ete lancee.
