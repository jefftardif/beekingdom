# Chat — cibles HTTP et curseurs bornés (2026-07-21)

## Résultat

Les chemins et chaînes de requête du client sont maintenant bornés avant la création de `UnityWebRequest`.

- cible HTTP par défaut : maximum 8 192 octets UTF-8 ;
- configuration autorisée : 1 024 à 16 384 octets ;
- caractères de contrôle interdits ;
- dépassement : `LocalRequestTooLarge` avec `local_request_target_too_large` ;
- curseur de conversations : 1 à 1 024 caractères, sans remplissage périphérique ni contrôle ;
- encodage du curseur exactement une fois avec `Uri.EscapeDataString` ;
- taille de page directe : 1 à 100 ;
- séquences de messages négatives refusées avant réseau.

Un curseur excessif ou malformé reçu du serveur est classé `InvalidResponse` avec `invalid_conversation_cursor`; il n’est jamais réutilisé dans une requête suivante. Cela évite les URL surdimensionnées, l’injection de séparateurs ou d’en-têtes et les boucles alimentées par une valeur opaque hostile.

## Validation

- mesure UTF-8 multioctet à la borne et au dépassement ;
- refus CR/LF et bornes de configuration invalides ;
- curseur contenant `?` et `&` correctement encodé ;
- refus d’un curseur client de 1 025 caractères ;
- refus d’un curseur serveur de 1 025 caractères comme réponse invalide ;
- refus d’une page de 101 éléments et d’une séquence négative.

Suite isolée Communication : **104/104 tests réussis**, compilation sans erreur ni avertissement.

## Directive serveur

Le serveur, IIS et le proxy doivent accepter les cibles normales sous `/chat/v1` tout en appliquant une borne cohérente. Les curseurs doivent rester opaques, liés au joueur, expirer proprement et ne jamais contenir d’état sensible lisible. Une valeur invalide ou expirée doit produire une erreur structurée sans redirection.

Les tests HTTP doivent couvrir curseur à la borne, dépassement, `%3F`, `%26`, `%25`, Unicode, CR/LF encodé, double encodage, réutilisation par un autre joueur, expiration et cible totale au-dessus de la limite. Les logs ne doivent contenir ni curseur brut, ni URL complète avec paramètres, ni identifiant joueur.

Le prochain candidat local doit inclure ce contrat avec les bornes de corps et de réponse, révoquer son prédécesseur et rester `DeploymentAuthorized=false` jusqu’aux portes SQL, .NET 8, TLS/IIS et Android staging. Aucun transfert, déploiement, activation ni synchronisation n’est autorisé ici.
