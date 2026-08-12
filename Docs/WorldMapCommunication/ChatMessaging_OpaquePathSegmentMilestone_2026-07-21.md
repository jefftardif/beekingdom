# Chat et messagerie — segments de route opaques et bornés

Date : 2026-07-21  
Responsable : Communication

## Résultat

Tous les identifiants de conversation et de message insérés dans une route REST passent maintenant par `OpaquePathSegment`. La valeur doit contenir de 1 à 256 caractères, ne peut pas avoir d'espace de bord et est encodée avec `Uri.EscapeDataString` comme un unique segment.

La protection couvre la réconciliation simple et paginée, l'envoi, le curseur de lecture, le signalement et la traduction. Les caractères `/`, `?` et `#` ne peuvent plus modifier la route, injecter une query ou créer un fragment. Le corps JSON conserve l'identifiant opaque original lorsque le contrat l'exige.

Une valeur vide, rembourrée ou supérieure à 256 caractères est refusée avant journal, session ou réseau. Les reçus persistés sont revalidés au moment de construire leur route de rejeu.

## Validation

- Compilation isolée Communication : réussie, sans erreur ni avertissement.
- Suite ciblée : 88/88 réussie.
- `c/a?#` devient exactement `c%2Fa%3F%23` dans les routes messages et lecture.
- `m/a?#` devient exactement `m%2Fa%3F%23` dans les routes signalement et traduction.
- Identifiant de 257 caractères et identifiant avec espaces de bord : zéro journal et zéro requête.
- Les 86 essais précédents restent verts.
- Aucun déploiement, activation ni synchronisation effectué.

## Directive d'intégration

Les endpoints ASP.NET Core doivent continuer à traiter les identifiants comme des segments opaques et appliquer une borne de 256 caractères après décodage, sans double décodage. Ajouter des tests HTTP pour `%2F`, `%3F`, `%23`, `%252F`, caractères Unicode, 256/257 caractères et espaces encodés. Une valeur réservée doit aboutir à un identifiant littéral ou à `chat.invalid_request`, jamais à une autre route, un autre message ou une redirection. IIS/proxy doivent préserver les segments encodés sans les normaliser dangereusement. Intégrer ces contrôles au préflight/candidat local tout en gardant `DeploymentAuthorized=false`.
