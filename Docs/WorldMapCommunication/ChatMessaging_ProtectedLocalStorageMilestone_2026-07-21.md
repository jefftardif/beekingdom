# Chat et messagerie — stockage local protégé

Date : 2026-07-21  
Responsable : Communication

## Résultat

Les journaux locaux nécessaires à la reprise du chat passent maintenant par une enveloppe de protection obligatoire avant toute écriture. La fabrique du client distant exige explicitement un `IChatDataProtector`; elle ne peut donc plus assembler silencieusement une outbox, des reçus, des curseurs ou des rapports persistants en texte clair.

`ProtectedChatStringStore` lie cryptographiquement chaque enveloppe à sa clé de stockage au moyen d'un usage (purpose) versionné. Une mauvaise clé, une enveloppe altérée ou un protecteur qui retourne le texte original provoque une erreur sûre. La valeur persistée existante est conservée afin de permettre le diagnostic ou une récupération contrôlée; elle n'est jamais supprimée automatiquement.

## Frontière de plateforme

Le code commun ne choisit ni algorithme ni gestionnaire de clés. La composition mobile devra fournir une implémentation authentifiée appuyée par le stockage sécurisé de la plateforme : Android Keystore sur Android et Keychain sur iOS. Aucune clé, aucun secret et aucune valeur de remplacement ne doivent être inclus dans le dépôt ou dans une ressource Unity.

Avant activation sur appareil, il faut définir et tester le cycle de vie de la clé : création, rotation, déconnexion, réinstallation/restauration d'appareil et traitement explicite d'une enveloppe devenue illisible.

## Validation

- Compilation isolée des assemblages Communication : réussie, sans erreur ni avertissement.
- Suite ciblée : 61/61 réussie.
- Preuves ajoutées : aller-retour sans texte clair, rejet d'une mauvaise clé, détection d'altération sans effacement et refus d'un protecteur sans effet.
- Aucun terrain, scène canonique, image ou module hors Communication modifié.
- Aucun déploiement, activation ni synchronisation effectué.

## Alignement serveur reçu

Le lot consolidé de l'Intégrateur de production est pris en compte : build Release 0 erreur/0 avertissement, suite serveur 20/20, pagination opaque liée au joueur, atomicité SQL Serializable rapport/reçu, purge transactionnelle des reçus et journalisation de traduction sans contenu ni identifiant.

Les portes restent ouvertes jusqu'aux preuves sur SQL jetable (migrations, reconstruction et concurrence), aux tests HTTP sous .NET 8, puis à la validation TLS/SNI/Full strict et Unity Android sur un hôte staging explicitement autorisé. La configuration client staging demeure injectée extérieurement sous la forme `https://<hote-staging>/chat/v1`.

## Directive d'intégration

L'Intégrateur doit conserver le service en préparation seulement et prévoir, dans la composition mobile/staging, l'injection d'un protecteur de plateforme avec cycle de vie documenté. Aucun secret ne doit entrer dans le dépôt et aucun déploiement public ne doit être entrepris sans autorisation distincte.
