# Chat — requêtes JSON bornées en UTF-8 (2026-07-21)

## Résultat

Le transport mesure maintenant chaque corps JSON selon sa taille réelle en UTF-8 avant de créer le tableau d’octets et avant tout appel réseau.

- défaut : 65 536 octets ;
- configuration : `RemoteChatClientOptions.MaxRequestBytes` ;
- minimum autorisé : 1 024 octets ;
- maximum autorisé : 1 048 576 octets ;
- la borne exacte est acceptée ;
- un dépassement produit `LocalRequestTooLarge` avec le code local sûr `local_request_too_large`.

Cette borne s’applique uniformément aux créations de conversation, messages, curseurs de lecture, signalements et traductions. Elle complète la limite fonctionnelle en caractères du corps de message : un caractère Unicode multioctet est compté selon les octets réellement transmis.

Une opération trop grande n’émet aucune requête HTTP. Lorsqu’elle provient d’une file persistante, elle n’est pas acquittée comme si le serveur l’avait reçue; les mécanismes de diagnostic et de récupération peuvent donc la traiter explicitement.

## Validation

- 512 caractères `é` produisent exactement 1 024 octets et sont acceptés à cette borne ;
- 513 caractères `é` sont refusés avec l’erreur locale attendue ;
- défaut 64 Kio ;
- refus des configurations sous 1 Kio et au-dessus de 1 Mio ;
- refus d’une mauvaise configuration par la composition complète du client.

Suite isolée Communication : **102/102 tests réussis**, compilation sans erreur ni avertissement.

## Directive serveur

ASP.NET Core, IIS et le proxy doivent imposer une limite de corps cohérente, au plus proche des routes chat, et répondre `chat.invalid_request`/400 ou 413 sans lire ni journaliser le corps excédentaire. Les tests HTTP doivent couvrir taille UTF-8 exacte, dépassement d’un octet, Unicode multioctet, `Content-Length` mensonger, transfert segmenté et compression éventuelle.

Les limites fonctionnelles par champ demeurent plus strictes que cette enveloppe générale. Aucun message, traduction, signalement, identifiant, jeton ni extrait de corps ne doit apparaître dans les logs de rejet.

Le candidat serveur suivant doit inclure ce contrat et le jalon de réponses bornées, révoquer le candidat antérieur et rester `DeploymentAuthorized=false` jusqu’aux portes SQL, .NET 8, TLS/IIS et Android staging. Aucun transfert, déploiement, activation ni synchronisation n’est autorisé ici.
