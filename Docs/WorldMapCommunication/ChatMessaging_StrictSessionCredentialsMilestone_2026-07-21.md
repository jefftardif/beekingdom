# Chat — justificatifs de session stricts (2026-07-21)

## Résultat

Le client refuse désormais une session malformée avant tout appel réseau.

`ChatSessionSecurity` centralise les règles suivantes :

- identité joueur non vide, sans remplissage périphérique ni caractère de contrôle, maximum 256 caractères ;
- jeton Bearer de 1 à 8192 caractères ;
- alphabet `b64token` ASCII attendu par Bearer : lettres, chiffres, `-._~+/`, avec `=` uniquement en suffixe ;
- refus des espaces, retours de ligne, Unicode hors alphabet, remplissage au milieu et valeurs trop longues.

`ServerChatProvider` applique ces règles à la session initiale et à toute session rafraîchie. `UnityWebRequestChatRestTransport` valide de nouveau le jeton immédiatement avant de créer l’en-tête `Authorization`, en défense supplémentaire. Une valeur invalide place le client en `AuthenticationRequired` et produit `Unauthorized` sans requête HTTP.

## Validation

- Injection CR/LF, espace, remplissage mal placé, jeton surdimensionné et identité joueur rembourrée testés sans appel réseau.
- Parcours valides et rafraîchissement existants conservés.
- Suite isolée Communication : **95/95 tests réussis**.
- Compilation du harnais : aucune erreur ni aucun avertissement.

## Directive serveur

Le serveur et IIS doivent appliquer une borne d’en-tête cohérente et retourner `chat.session_required`/401 aux justificatifs absents ou invalides, sans journaliser le jeton ni sa valeur fautive. Les tests HTTP doivent couvrir espaces, CR/LF refusé avant émission, alphabet invalide, remplissage incorrect, dépassement de 8192 caractères et rotation valide. Aucun extrait de jeton, hash réversible ou identifiant joueur brut ne doit apparaître dans les diagnostics.

Le candidat reconstruit reste `DeploymentAuthorized=false` jusqu’aux portes staging, SQL, TLS et Android. Aucun transfert, déploiement, activation ni synchronisation n’est autorisé par ce jalon.
