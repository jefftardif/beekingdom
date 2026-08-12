# Bee Kingdom - Jalon fabrique du client Unity distant

Date: 2026-07-21  
Agent: `Communication`

## Livraison

`RemoteChatClientFactory` assemble maintenant en un point unique:

- `UnityWebRequestChatRestTransport`;
- codec JSON et decodeur d'erreurs;
- source de session injectee;
- transport temps reel facultatif;
- diagnostics facultatifs;
- quatre journaux durables sur un stockage protege injecte;
- retry borne;
- synchroniseur de conversation et politique de recuperation.

La fabrique ne connait aucun compte, jeton, domaine fixe ou secret. Elle exige une
URL absolue HTTPS. HTTP est refuse, sauf boucle locale explicitement autorisee pour
le developpement. Cette exception ne s'applique jamais a une adresse distante.

Le stockage n'est pas impose a `PlayerPrefs`: l'appelant doit fournir un
`IChatStringStore` adapte au stockage protege de la plateforme. Les quatre cles
sont separees par prefixe: messages, conversations, signalements et lectures.

## Verification

- 58 tests Communication executes;
- 58 reussis;
- 0 echec;
- compilation: 0 erreur, 0 avertissement.

Les nouveaux tests couvrent assemblage HTTPS, URL invalide, prefixe invalide,
refus HTTP public et exception loopback explicite.

## Handoff Integrateur

Le futur environnement client recevra l'URL publique par configuration externe,
jamais par secret ou constante de code. Le certificat et la chaine TLS doivent
etre valides avant branchement staging. Les endpoints doivent rester sous le
prefixe versionne `/chat/v1` annonce par les capacites.

## Fichiers du jalon

Crees:

- `Assets/BeeKingdom/Gameplay/Communication/RemoteChatClientFactory.cs`
- `Assets/BeeKingdom/Gameplay/Communication/RemoteChatClientFactory.cs.meta`
- `Docs/WorldMapCommunication/ChatMessaging_UnityCompositionFactoryMilestone_2026-07-21.md`

Modifie:

- `Assets/BeeKingdom/Gameplay/Communication/RemoteChatContracts.cs`
- `Assets/BeeKingdom/Tests/Editor/ServerChatProviderTests.cs`

Aucun deploiement, secret, drapeau de production ou synchronisation n'a ete
ajoute ou active.
