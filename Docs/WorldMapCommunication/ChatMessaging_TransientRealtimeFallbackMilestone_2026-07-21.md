# Chat — fallback temps réel strictement transitoire (2026-07-21)

## Résultat

La connexion temps réel ne bascule plus silencieusement vers le polling pour toute erreur.

Le fallback est autorisé uniquement pour :

- `Transport` ;
- `Offline` ;
- `RateLimited`.

Les erreurs permanentes restent visibles :

- `Unauthorized` déclenche au plus un rafraîchissement de session, puis devient `AuthenticationRequired` si elle persiste ;
- `Forbidden`, `Incompatible`, `InvalidResponse` et les erreurs locales ne basculent pas vers polling ;
- la connexion passe à `Offline` et l’exception typée est propagée.

La même matrice s’applique à la seconde tentative après rafraîchissement. Une erreur réseau brute non typée reste considérée comme transitoire, tandis qu’une annulation est toujours propagée.

## Pourquoi

Le polling est une solution de disponibilité à une panne de transport. Il ne doit pas masquer :

- un hub mal configuré ;
- une autorisation insuffisante ;
- un protocole ou handshake incompatible ;
- une réponse structurellement invalide.

Masquer ces défauts aurait donné une apparence de fonctionnement tout en laissant le temps réel durablement cassé en production.

## Validation

- échec `Transport` à la connexion : état `Polling` ;
- handshake `InvalidResponse` : exception préservée et état `Offline` ;
- première connexion `Unauthorized`, session rafraîchie, seconde `Forbidden` : deux tentatives seulement, refus préservé et état `Offline` ;
- parcours temps réel normal et absence de temps réel annoncée conservés.

Suite isolée Communication : **117/117 tests réussis**, compilation sans erreur ni avertissement.

## Directive serveur

Le hub doit retourner des catégories différenciables et stables pour authentification, autorisation, incompatibilité de protocole, limitation et indisponibilité. Les tests de staging doivent vérifier la connexion initiale et après rotation du jeton pour chaque catégorie, ainsi que l’absence de fallback silencieux sur 401/403 ou handshake invalide.

La négociation capabilities ne doit annoncer `realtime=true` que lorsque le hub est réellement prêt sur le même déploiement. En cas d’indisponibilité transitoire, REST/polling doit rester cohérent avec les événements déjà commis. Aucun jeton `access_token` de WebSocket ne doit apparaître dans les logs ou URL diagnostiquées.

Le candidat courant ne couvre pas ce jalon. Son successeur doit intégrer ces tests, révoquer l’ancien courant et rester `DeploymentAuthorized=false` jusqu’aux validations SQL jetable, .NET 8, TLS/IIS et Android staging.

Aucun transfert, déploiement, activation ni synchronisation n’est autorisé ici.
