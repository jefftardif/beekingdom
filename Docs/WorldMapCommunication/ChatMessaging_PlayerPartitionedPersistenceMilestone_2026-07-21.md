# Chat et messagerie — persistance cloisonnée par joueur

Date : 2026-07-21  
Responsable : Communication

## Résultat

Les journaux persistants du client distant ne partagent plus un espace global entre les comptes d'un même appareil. `RemoteChatClientOptions.StoragePartitionId` est désormais obligatoire et doit recevoir l'identité stable du joueur authentifié au moment de la composition.

`ChatStoragePartition` transforme cette identité avec SHA-256 et un domaine versionné, puis n'utilise que les 128 premiers bits encodés en hexadécimal dans les clés locales. L'identifiant brut du joueur n'apparaît donc ni dans les noms de clés ni dans les enveloppes de protection. Chaque partition possède ses propres envois, créations de conversation, signalements et curseurs de lecture.

Cette empreinte sert au cloisonnement et non à l'authentification. L'enveloppe protégée du jalon précédent reste l'autorité de confidentialité et d'intégrité. La composition doit fournir une partition correspondant exactement à la session authentifiée; un changement de compte exige la reconstruction du client avec la nouvelle partition.

## Validation

- Compilation isolée Communication : réussie, sans erreur ni avertissement.
- Suite ciblée : 63/63 réussie.
- Deux identités produisent des clés distinctes et stables sans exposer leur valeur brute.
- Un message en attente écrit dans la partition A est absent de la partition B.
- Le contenu du message demeure absent du stockage brut grâce à l'enveloppe protégée.
- La fabrique refuse une partition vide.
- Aucun déploiement, activation ni synchronisation effectué.

## Preuve serveur reçue

L'Intégrateur a ajouté `Server/tools/Test-ProductionLocal.ps1`, compatible Windows PowerShell 5.1. Le smoke test réel force Production, InMemory, workers désactivés et les deux portes chat fermées; il valide `/health`, capabilities et readiness, puis arrête toujours le processus. Résultat reçu : Healthy, protocole `chat-v1`, `server=false`, `realtime=false`, `PreparationOnly`, sans listener 5088 résiduel. La politique Android Keystore/iOS Keychain et son cycle de vie sont documentés sans secret.

## Directive d'intégration

La composition Unity/staging doit dériver `StoragePartitionId` de l'identité stable attestée par la session, reconstruire le client lors d'un changement de compte et ne jamais réutiliser une partition anonyme commune. L'Intégrateur doit vérifier que la reconnexion/rotation de jeton conserve la même partition, tandis que la déconnexion suivie d'un autre compte sélectionne une partition différente. Les portes Production restent fermées jusqu'aux validations SQL jetable, HTTP .NET 8, TLS et Android staging déjà consignées.
