# Chat et messagerie — frontière sûre des pannes de stockage local

Date : 2026-07-21  
Responsable : Communication

## Résultat

Toutes les lectures, écritures et suppressions des quatre journaux persistants passent désormais par une frontière d'erreur commune dans `ServerChatProvider`. Une enveloppe non authentifiable, un schéma corrompu ou une panne du support local produit `RemoteChatError.LocalStorageUnavailable`, le code stable `local_storage_unavailable` et un statut HTTP 0.

L'opération est arrêtée avant le réseau lorsque son intention ne peut pas être journalisée avec certitude. Après une réussite serveur, un échec de suppression est également signalé : le reçu local reste présent et pourra être rejoué idempotemment au lieu d'être déclaré faussement acquitté.

La valeur persistée existante n'est jamais supprimée automatiquement. Le diagnostic `local_storage_unavailable` contient seulement le type d'opération et la catégorie d'erreur. Il ne contient ni enveloppe, corps, identifiant, catégorie, jeton, clé ou détail cryptographique.

## Validation

- Compilation isolée Communication : réussie, sans erreur ni avertissement.
- Suite ciblée : 70/70 réussie.
- Journal JSON corrompu : erreur locale stable, valeur conservée, zéro appel REST.
- Mauvaise clé de protection : erreur locale stable, enveloppe conservée, zéro appel REST.
- Échec d'écriture du support : erreur locale stable, zéro appel REST.
- Les diagnostics ne contiennent aucune valeur sensible injectée par les essais.
- Aucun déploiement, activation ni synchronisation effectué.

## Directive d'intégration

L'interface Unity doit distinguer `LocalStorageUnavailable` de `LocalQueueFull` : la première suspend toutes les mutations persistantes de la partition et propose une récupération locale contrôlée, sans bouton de réessai réseau en boucle; la seconde concerne seulement une capacité atteinte. Le serveur ne doit recevoir aucune métrique de requête pour une opération bloquée avant transport. En staging Android, altérer une enveloppe de test et simuler une clé indisponible, vérifier zéro HTTP et aucune suppression, puis restaurer la clé/enveloppe et drainer idempotemment. Les portes de production restent fermées.
