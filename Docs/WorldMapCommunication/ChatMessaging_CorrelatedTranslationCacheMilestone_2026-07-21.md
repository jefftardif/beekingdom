# Chat — traduction corrélée avant cache (2026-07-21)

## Résultat

Une traduction n’est plus mise en cache uniquement parce que le serveur retourne `completed`. La réponse doit correspondre exactement à la demande :

- même `messageId` ;
- même `targetLocale` sans sensibilité à la casse ;
- même `modelVersion` ;
- statut reconnu : `pending` ou `completed` ;
- texte traduit présent et limité à 16 000 caractères pour `completed` ;
- `sourceLocale`, lorsqu’elle est fournie, conforme au format borné attendu.

Une discordance produit `InvalidResponse` avec `translation_response_mismatch` et ne pollue aucun cache. Seules les réponses `completed` valides sont mémorisées; `pending` reste non caché afin qu’un appel futur puisse observer l’achèvement.

## Paramètres et clé de cache

- `targetLocale` : balise ASCII de 2 à 35 caractères, segments alphanumériques séparés par un seul tiret ;
- `modelVersion` : 1 à 128 caractères ASCII alphanumériques ou `-`, `_`, `.` ;
- validation avant tout appel réseau ;
- clé de cache préfixée par les longueurs des trois composantes, empêchant les collisions par concaténation ambiguë.

## Validation

- réponse d’un autre message rejetée sans cache ;
- appel valide suivant réellement envoyé puis mis en cache ;
- troisième appel identique servi par le cache ;
- locale avec double tiret, locale trop longue, version contenant un espace et version trop longue refusées avant réseau.

Suite isolée Communication : **111/111 tests réussis**, compilation sans erreur ni avertissement.

## Directive serveur

La route de traduction doit toujours renvoyer `messageId`, `sourceLocale`, `targetLocale`, `modelVersion`, `translatedText` et `status` depuis la même entrée de cache/idempotence. La clé serveur reste `(MessageId, TargetLocale, ModelVersion)` avec comparaison de locale canonique clairement définie.

Les tests HTTP doivent injecter chaque champ discordant, statut inconnu, texte absent, texte excessif, locale invalide et deux clés autrefois ambiguës. Aucun résultat d’un autre message, joueur ou modèle ne doit pouvoir être servi. Les logs gardent uniquement code d’état, latence, résultat cache hit/miss et version non sensible autorisée; jamais les textes ni identifiants bruts.

Le candidat `180651Z` et son éventuel successeur construit uniquement pour les flux entrants ne couvrent pas ce jalon. Le prochain candidat doit intégrer les deux lots, révoquer l’ancien courant et rester `DeploymentAuthorized=false` jusqu’aux portes SQL, .NET 8, TLS/IIS et Android staging. Aucun transfert, déploiement, activation ni synchronisation n’est autorisé ici.
