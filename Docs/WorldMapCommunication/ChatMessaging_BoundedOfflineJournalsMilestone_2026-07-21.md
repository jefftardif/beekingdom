# Chat et messagerie — journaux hors ligne bornés

Date : 2026-07-21  
Responsable : Communication

## Résultat

Les quatre journaux persistants du client distant ont désormais une capacité commune explicite. `ChatPendingJournalPolicy` fixe par défaut 256 entrées et accepte une configuration comprise entre 1 et 4096. `RemoteChatClientOptions.MaxPendingEntriesPerJournal` transmet cette politique à la composition complète.

La limite couvre les messages à envoyer, les créations de conversation, les signalements de modération et les curseurs de lecture. À capacité atteinte, une nouvelle identité est refusée avec `ChatPendingJournalFullException`; aucune entrée antérieure n'est supprimée ou remplacée silencieusement. Une reprise idempotente portant la même identité demeure autorisée, tout comme l'avancement monotone d'un curseur déjà présent.

Cette politique évite une croissance locale sans borne tout en conservant la promesse de reprise. L'interface devra présenter l'état « file hors ligne pleine » et inviter à rétablir la connexion; elle ne doit pas prétendre qu'une opération refusée sera envoyée plus tard.

## Validation

- Compilation isolée Communication : réussie, sans erreur ni avertissement.
- Suite ciblée : 65/65 réussie.
- Les quatre journaux refusent une seconde identité à capacité 1 et conservent exactement la valeur persistée précédente.
- Les mises à jour idempotentes d'un message et d'un curseur existants restent possibles à capacité atteinte.
- Les capacités nulles, négatives ou supérieures à 4096 sont rejetées.
- Aucun déploiement, activation ni synchronisation effectué.

## Préflight staging reçu

L'Intégrateur a ajouté `Server/tools/Test-ChatStagingPreflight.ps1`. Le script est en lecture seule et sans bearer; il exige une URL HTTPS exacte sous `/chat/v1`, refuse credentials, query, fragment et loopback, valide SNI, chaîne et nom TLS, marge de certificat, issuer optionnel, absence de redirection et contrat capabilities `chat-v1`. Sa syntaxe ainsi que les refus HTTP/loopback avant connexion ont été validés. Il n'a pas été exécuté contre un domaine faute d'hôte staging autorisé.

Le rapport serveur inclut aussi le scénario Android partitionné A hors ligne → déconnexion → B → retour A, la stabilité de partition lors d'une rotation de jeton et l'absence de données brutes. Les portes `Chat=false`, `Realtime=false` et `PreparationOnly` restent inchangées.

## Directive d'intégration

Le serveur doit conserver ses limites et reçus idempotents afin qu'une reprise après saturation soit sûre. En staging, vérifier que la remise en ligne d'une file pleine draine les entrées existantes sans doublon, puis permet une nouvelle opération seulement après libération d'une place. Les codes d'erreur serveur ne doivent pas être utilisés pour masquer une saturation locale. Aucun hôte public ne doit être testé ni activé sans autorisation distincte.
