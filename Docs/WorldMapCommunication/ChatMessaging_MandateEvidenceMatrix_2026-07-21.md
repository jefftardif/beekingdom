# Chat/Messagerie — matrice de preuve du mandat Communication

Date : 2026-07-21  
Responsable : Communication  
État : **pont distant couvert; branchement au shell et staging encore ouverts**

## Portée auditée

Cette matrice confronte l’état courant au mandat `Docs/Communication_VM_Assignment.md` et à `Communication_Agent_ParallelProduction_Goal.md`. Elle n’utilise pas l’intention ni les anciens rapports comme preuve : chaque exigence est reliée au code ou à un test déterministe actuel.

## Première tranche distante

| Exigence | Preuve actuelle | État |
|---|---|---|
| Contrat distant asynchrone et transport injectable | `RemoteChatContracts.cs`, `ServerChatProvider.cs`, `IChatRestTransport` | Acquis |
| Capacités publiques | `PublicCapabilitiesDoNotRequireOrSendPlayerSession`, `ActiveCapabilitiesRejectUnknownProviderUnsafeBoundsAndChannels` | Acquis |
| Authentification abstraite | `IChatSessionSource`, `UnauthorizedRequestRefreshesExactlyOnceThenSucceeds`, `SecondUnauthorizedResponseStopsWithoutRefreshLoop` | Acquis |
| Conversations accessibles | `ConversationPaginationDeduplicatesAcrossPages`, validation des reçus et curseurs | Acquis |
| Messages paginés et ordonnés | `MessagePaginationLoadsEveryPageInSequence`, `ConversationAndMessagePagesRejectDuplicatesRegressionAndExcessItems` | Acquis |
| Création idempotente | `ConversationCreationRequiresAndForwardsStableRequestId`, journaux versionnés de création | Acquis |
| Envoi avec `ClientRequestId` stable | `SendRetryAndRealtimeRestDuplicateAreIdempotent`, `PendingSendSurvivesProviderRestartAndKeepsOriginalIdentity` | Acquis |
| Lecture monotone et durable | `ReadCursorSurvivesRestartAndRetriesMaximumSequence`, `OlderReadNeverRegressesStoredMaximum`, `AckInFlightDoesNotEraseNewerRead` | Acquis |
| Modération durable | `ModerationReportSurvivesRestartAndKeepsStableRequestId`, conflits et rate limit couverts | Acquis |
| Temps réel puis repli REST/polling | `MissingRealtimeFallsBackToPolling`, `RealtimeTransportFailureFallsBackButContractFailureRemainsVisible` | Acquis |
| Reprise réseau bornée | `PollingRetriesNetworkLossThenRecoversWithinBound`, `PollingStopsAtRetryLimit`, drainage partiel couvert | Acquis |
| Mode local conservé | `LocalChatProvider.cs`, `ChatMessagingLocalDataLayerTests.cs` | Acquis, sans modification de son contrat |
| Aucun blocage réseau du thread Unity | API entièrement `Task`/`CancellationToken`; aucun appel synchrone au transport distant | Acquis par architecture |

## Réconciliation et isolation

| Cas obligatoire | Test déterministe |
|---|---|
| doublon SignalR + REST | `SendRetryAndRealtimeRestDuplicateAreIdempotent` |
| trou de séquence | `RealtimeGapTriggersRestBeforeSequenceIsConfirmed` |
| événement hors ordre | `OutOfOrderRealtimeEventsRemainUnconfirmedUntilGapArrives` |
| événement tardif après logout | `RealtimeEventsQueuedBeforeLogoutCannotMergeAfterDisconnect` |
| perte réseau pendant l’envoi | `StatusZeroIsNetworkFailureAndKeepsPendingSend` |
| accès ou reçu d’un autre compte | `DifferentAccountCannotReadOrWritePartitionJournals`, `ReceiptFromDifferentSenderCannotAcknowledgeAuthenticatedPlayersSend` |
| changement de compte pendant refresh | `AccountChangeDuringRefreshCannotReplayPendingOperation` |
| fermeture/annulation | `SynchronizerCancelsAndDisconnectsWhenPanelCloses`, `CancellationPropagatesWithoutNetworkCall` |
| cache hors ligne puis autorité serveur | `LivingHiveControllerRestoresProtectedRecentCacheWhenServerIsOffline`, `LivingHiveControllerReconcilesRestoredCacheWithServerAuthority` |
| cache protégé et partitionné | tests `RecentCache*`, dont rotation de quarantaine et sélection au-delà de 100 |

## Traduction

| Exigence | Preuve actuelle | État |
|---|---|---|
| demande explicite et cache `(message, locale, modèle)` | `TranslationCacheUsesMessageLocaleAndModelAndOriginalRemainsAvailable` | Acquis |
| changement de langue cible | même test et clé composite | Acquis |
| réponse corrélée avant cache | `TranslationResponseMustMatchRequestBeforeItCanBeCached` | Acquis |
| erreur sans perte de l’original | `TranslationErrorKeepsOriginalAndOriginalToggleIsPermanent` | Acquis |
| annulation avec original visible | `TranslationCancellationRestoresOriginal` | Acquis |
| isolation entre comptes | `CachedTranslationCannotCrossAccountBoundary` | Acquis |
| bornes avant réseau | `TranslationParametersAreBoundedBeforeNetwork` | Acquis |
| aucun fournisseur externe en test | transports et fournisseurs factices injectés | Acquis |

## Preuves exécutées

- harnais Communication autonome final : 138/138 réussis, 0 échec, 0 erreur, 0 ignoré ;
- TRX : `LivingHiveChatRecentCacheFinal.trx` ;
- compilation Unity globale ratifiée précédemment dans `Artifacts/LivingHiveChatFinalF8.log` avec 0 `error CS` ;
- disposition LivingHive : 3/3 ;
- preuves natives : 390x844 et 1600x900, état honnête `NotConfigured` ;
- aucune carte, scène canonique ou image LivingHive modifiée.

## Écarts qui empêchent la fin produit

1. Aucun shell mobile d’authentification de production n’appelle encore `LivingHiveChatBootstrap.ActivateAsync`.
2. Le pont de cycle de session décrit dans `LivingHiveChat_SessionLifecycleBridge_Spec_2026-07-21.md` reste à implémenter et tester après le gel Assets.
3. La contrepartie serveur la plus récente compile, mais ses nouveaux tests ne sont pas découverts sans runtime .NET 8 natif; elle reste non promouvable.
4. SQL jetable, TLS/SNI/IIS, Cloudflare Full strict et Android staging doivent être prouvés sur l’environnement autorisé.
5. `Chat:Enabled`, `Chat:RealtimeEnabled` et `DeploymentAuthorized` doivent rester faux avant ces preuves et une autorisation explicite.

## Conclusion d’audit

La première tranche du pont Unity demandée par le mandat est techniquement couverte et testée. Le système complet jouable en production ne l’est pas encore : le branchement réel de session et les validations staging constituent des exigences manquantes, pas des améliorations facultatives.

Aucun Asset, secret, déploiement, transfert, activation ou synchronisation n’a été modifié pour cette matrice.
