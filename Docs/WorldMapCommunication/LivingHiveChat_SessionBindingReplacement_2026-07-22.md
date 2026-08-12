# LivingHive Chat — remplacement sûr de liaison de session

Date : 2026-07-22  
Responsable : Communication  
État : **implémenté et testé**, staging non activé

## Risque corrigé

Le coordinateur considérait auparavant toute notification portant le même `StoragePartitionId` comme idempotente. Si le shell remplaçait réellement sa source de session, son store, son protecteur ou son transport, Communication conservait silencieusement l’ancienne liaison et pouvait continuer à consulter un fournisseur de jeton obsolète.

L’idempotence exige maintenant la même instance complète de `LivingHiveChatSessionBinding` :

- même joueur + même liaison : aucune reconnexion ;
- même joueur + nouvelle liaison : logout de l’ancienne liaison, puis activation de la nouvelle ;
- joueur A → joueur B : comportement inchangé, A est fermé avant B ;
- échec ou annulation : l’identité de liaison active est effacée avec l’identité joueur.

Le renouvellement normal du bearer continue de passer par la même source de session vivante et ne provoque aucune reconnexion.

## Fichiers

Modifiés :

- `Assets/BeeKingdom/Gameplay/Communication/LivingHiveChatBootstrap.cs`
- `Assets/BeeKingdom/Tests/Editor/ServerChatProviderTests.cs`
- `Docs/WorldMapCommunication/LivingHiveChat_SessionLifecycleBridge_2026-07-22.md`

Créé :

- `Docs/WorldMapCommunication/LivingHiveChat_SessionBindingReplacement_2026-07-22.md`

Aucun présentateur, ancrage chat partagé, catalogue, scène, terrain ou image n’a été modifié.

## Preuve

Commande :

`dotnet test CommunicationCompile.csproj --no-restore -v:minimal --logger "trx;LogFileName=LivingHiveChatSessionBindingReplacement146.trx"`

Résultat : 146/146 réussis, 0 échec, 0 erreur, 0 ignoré. Le test ajouté vérifie l’ordre exact `activate:p1 → logout → activate:p1` et confirme que la seconde activation reçoit la nouvelle source de session.

Fin de passe : Unity=0, dotnet=0, testhost=0.

## Porte serveur

Le serveur ne doit jamais déduire la continuité de compte d’un identifiant local stable. Chaque requête et chaque reconnexion doivent revalider le bearer; si un nouveau jeton représente un autre joueur, la requête doit être rejetée avant effet et avant accès aux reçus ou caches du premier joueur.

Aucun secret, déploiement, transfert, activation ou synchronisation n’a été effectué.
