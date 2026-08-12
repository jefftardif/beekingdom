# Bee Kingdom - Pont Unity Communication, tranche 1

Date: 2026-07-21  
Agent: `Communication`  
Statut: contrat et coeur de reconciliation livres; branchement production central non applique.

## Resultat

La couche Unity distante est asynchrone, annulable et testable sans reseau reel.
Elle conserve `LocalChatProvider` intact et ne modifie aucune scene ni interface
LivingHive.

Livraison:

- session authentifiee fournie par `IChatSessionSource`;
- transport REST generique injectable `IChatRestTransport`;
- capacites, conversations, messages, lecture et signalement;
- envoi idempotent par `ClientRequestId`;
- fusion par sequence des reponses REST et evenements temps reel;
- ordre stable des evenements en retard ou hors ordre;
- repli explicite en polling si aucun transport temps reel compatible Unity n'est fourni;
- expiration de session exposee par `AuthenticationRequired`;
- annulation propagee jusqu'aux transports;
- contrat de traduction et cache `message_id + target_locale + model_version`;
- acces permanent au texte original, y compris apres erreur de traduction.

## Verification

- Compilation isolee des sources runtime et des tests NUnit: reussie, 0 erreur,
  0 avertissement, cible `netstandard2.1`.
- Les tests couvrent envoi/retry, doublon temps reel + REST, ordre et trou de
  sequence, repli polling, session expiree, changement de langue cible, cache,
  original et annulation.
- La suite serveur chat existante n'a pas pu etre executee: la VM possede le
  runtime .NET 10.0.10 mais pas `Microsoft.NETCore.App 8.0.0`, requis par le
  `testhost` net8. La compilation de ses projets Release reussit avant le
  demarrage du testhost.
- Unity installe est `6000.5.4f1`; le projet exige `6000.5.3f1`. Aucun lancement
  avec une version differente n'a ete tente afin d'eviter une migration du projet.

## Handoff Integrateur

Changements centraux requis, non appliques:

1. Ajouter un endpoint authentifie
   `POST /chat/v1/messages/{messageId}/translations` dans
   `Server/src/BeeKingdom.Server/Program.cs`.
2. Brancher un fournisseur de traduction serveur derriere une abstraction, avec
   limite de debit, taille maximale, autorisation de lecture et cache partage.
3. Persister la cle `(MessageId, TargetLocale, ModelVersion)` via une migration
   detenue par Integrateur. L'original reste la seule donnee moderee.
4. Installer le runtime .NET 8 x64 de test sur la VM, sans modifier la cible du
   serveur.

## Handoff Architecte

Le futur panneau peut posseder un `CancellationTokenSource` par ouverture,
appeler `ConnectAsync`, puis `ReconcileAsync(conversationId, lastSequence)`.
Il doit annuler le jeton et appeler `DisconnectAsync` a la fermeture. La commande
`Traduire` affiche `TranslatedText`; la commande `Original` relit toujours
`OriginalText(message)`. Le fournisseur local reste selectionnable pour le labo
et le mode hors serveur.

Un adaptateur concret UnityWebRequest doit serialiser les DTO et implementer
`IChatRestTransport`. SignalR reste optionnel: aucune bibliotheque compatible
Unity n'etant verifiee dans le projet, le coeur livre fonctionne en polling REST
borne sans nouvelle dependance.

## Fichiers crees

- `Assets/BeeKingdom/Gameplay/Communication/RemoteChatContracts.cs`
- `Assets/BeeKingdom/Gameplay/Communication/RemoteChatContracts.cs.meta`
- `Assets/BeeKingdom/Gameplay/Communication/ServerChatProvider.cs`
- `Assets/BeeKingdom/Gameplay/Communication/ServerChatProvider.cs.meta`
- `Assets/BeeKingdom/Tests/Editor/ServerChatProviderTests.cs`
- `Assets/BeeKingdom/Tests/Editor/ServerChatProviderTests.cs.meta`
- `Docs/WorldMapCommunication/ChatMessaging_UnityBridge_Report_2026-07-21.md`

Aucun fichier existant du produit, de LivingHive, de la carte, du serveur central,
de la base ou de la localisation n'a ete modifie. Aucune synchronisation ni aucun
deploiement n'a ete effectue.
