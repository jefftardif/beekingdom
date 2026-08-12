# Chat et messagerie — intégrité des journaux restaurés

Date : 2026-07-21  
Responsable : Communication

## Résultat

Les quatre journaux persistants sont maintenant validés intégralement avant que leurs entrées soient matérialisées ou rejouées. Un journal dont le nombre d'entrées dépasse `ChatPendingJournalPolicy.MaxEntries` est refusé même s'il a été écrit par une ancienne version ou modifié hors de l'application.

Chaque entrée doit porter la version de schéma attendue, son identité idempotente obligatoire et un compteur de tentatives non négatif. Les messages exigent conversation, corps non nul et date client analysable; les créations exigent une requête et des participants valides; les signalements exigent message, catégorie et reçu; les lectures exigent conversation et séquence non négative. Les identités dupliquées dans un même journal sont interdites.

Une non-conformité produit `ChatPendingStoreException`, puis `LocalStorageUnavailable` à la frontière du fournisseur. La valeur persistée n'est ni tronquée, ni normalisée, ni réécrite automatiquement; elle reste disponible pour la quarantaine contrôlée.

## Validation

- Compilation isolée Communication : réussie, sans erreur ni avertissement.
- Suite ciblée : 73/73 réussie.
- Journal de messages dépassant sa capacité : refus et valeur exacte conservée.
- Création sans identité, signalements à identité dupliquée et lecture à séquence négative : refus et valeurs exactes conservées.
- Les anciens montages de test incomplets ont été remplacés par les mêmes reçus complets que le fournisseur produit réellement.
- Aucun déploiement, activation ni synchronisation effectué.

## Durcissement serveur reçu

L'Intégrateur a supprimé la chaîne SQL localhost de développement par défaut. En Production, les trois chaînes sont neutralisées et la validation runtime/migration `ValidateOnStart` est obligatoire lorsque `Provider=SqlServer`. Un démarrage réel sans chaîne échoue par `OptionsValidationException` avant listener, sans port 5091 résiduel. Le smoke InMemory depuis le répertoire publié reste Healthy, `chat-v1`, `server=false`, `realtime=false`, `PreparationOnly`. `Test-ProductionConfiguration.ps1` réussit; build serveur 0/0 et tests Persistence/Architecture 9/9.

## Directive d'intégration

Les tests SQL/HTTP de staging doivent injecter des reçus serveur complets et uniques, puis vérifier qu'aucun doublon d'identité n'est produit lors d'une reconstruction ou d'une concurrence. Côté Android, injecter localement un journal surdimensionné, une entrée de mauvaise version et deux reçus identiques : vérifier `LocalStorageUnavailable`, zéro HTTP, aucune réécriture et quarantaine possible. Le serveur ne doit jamais proposer au client de « réparer » silencieusement un reçu ambigu. Les portes de production restent fermées.
