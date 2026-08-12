# Chat — liaison stricte entre compte et partition locale (2026-07-21)

## Résultat

Le client Communication lie désormais chaque fournisseur construit par `RemoteChatClientFactory` à l’identité stable ayant servi à créer sa partition locale.

Avant tout accès aux journaux persistants d’envoi, de création, de modération ou de lecture, la session doit être valide et son `PlayerId` doit correspondre exactement à `StoragePartitionId`. Une différence est refusée localement avec :

- erreur `LocalAccountMismatch` ;
- code sûr `local_account_mismatch` ;
- état de connexion `Offline` ;
- zéro lecture ou écriture du journal attribué à l’autre compte ;
- zéro requête HTTP.

La session ainsi validée est conservée pour la première tentative réseau. Un renouvellement après 401 reste permis pour le même joueur. Si le renouvellement retourne un autre joueur, la seconde requête n’est pas envoyée et l’opération déjà persistée demeure disponible dans la partition d’origine.

## Invariants

- la comparaison d’identité est ordinale et sans normalisation silencieuse ;
- `StoragePartitionId` demeure requis et valide selon les bornes du contrat joueur ;
- les fournisseurs construits directement sans identité attendue conservent leur compatibilité actuelle ;
- les lectures et mutations distantes authentifiées utilisent toutes la même validation de session ;
- aucun identifiant joueur, jeton ou contenu n’est ajouté aux diagnostics.

## Validation

- compte `p2` sur partition `p1` : envoi, création, signalement et lecture refusés avant journal et réseau ;
- renouvellement `p1` vers `p2` après 401 : une seule requête réseau, aucune seconde tentative, opération en attente conservée ;
- parcours normaux et renouvellement du même joueur inchangés ;
- suite isolée Communication : **120/120 tests réussis**, compilation sans erreur ni avertissement.

## Directive serveur et staging

Le serveur doit maintenir la même séparation par identité authentifiée sur les conversations, messages, reçus idempotents, curseurs de lecture, signalements et traductions. Les essais staging doivent alterner deux comptes sur le même appareil et vérifier qu’aucun reçu, curseur, message en attente ou donnée restaurée du compte A n’est observable ou rejouable par B.

Le candidat courant `BeeKingdom.Server.20260721T195742Z` reste une validation locale uniquement, avec `DeploymentAuthorized=false`. Un successeur éventuel doit conserver ce verrou tant que les portes SQL jetable, .NET 8 natif, TLS/SNI/IIS et Android staging ne sont pas toutes franchies.

Aucun transfert, déploiement, activation ni synchronisation n’est autorisé par ce jalon.
