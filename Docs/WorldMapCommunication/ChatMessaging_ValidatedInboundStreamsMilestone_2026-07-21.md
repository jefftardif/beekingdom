# Chat — flux entrants validés avant fusion (2026-07-21)

## Résultat

Les pages REST et événements temps réel sont maintenant validés avant toute modification des caches, séquences confirmées ou index de déduplication.

### Pages de conversations

- objet et liste présents ;
- nombre d’éléments inférieur ou égal à la limite demandée ;
- `conversationId` opaque valide et unique dans la page ;
- `lastSequence` non négatif ;
- curseur suivant conforme au contrat borné.

### Pages de messages

- objet et liste présents ;
- nombre d’éléments inférieur ou égal à la limite demandée ;
- chaque message appartient exactement à la conversation demandée ;
- `messageId` et `clientRequestId` opaques valides ;
- expéditeur valide, corps présent et sous la limite négociée ;
- séquence strictement supérieure au curseur demandé et unique dans la page ;
- curseur suivant non inférieur à la plus grande séquence reçue.

Une page valide peut arriver non triée : le cache ordonné existant la normalise. Les doublons de séquence, valeurs antérieures au curseur et éléments croisés sont refusés.

### Temps réel

L’enveloppe et son message doivent référencer la même conversation. Si l’enveloppe fournit une séquence, elle doit égaler celle du message. Un événement incohérent produit `realtime_event_mismatch` avant acquisition du verrou de fusion et n’avance aucun curseur.

## Validation

- message REST provenant d’une autre conversation rejeté avant fusion ;
- événement temps réel croisé rejeté avant fusion ;
- conversation dupliquée dans une page rejetée ;
- séquence de message dupliquée rejetée ;
- page valide non triée toujours normalisée ;
- curseur confirmé inchangé après chaque rejet.

Suite isolée Communication : **109/109 tests réussis**, compilation sans erreur ni avertissement.

## Directive serveur

Les requêtes REST et événements publiés après commit doivent provenir du même modèle durable et respecter les mêmes identifiants, séquences et limites. Les tests doivent injecter : conversation croisée, joueur non autorisé, ID vide/surdimensionné, séquence dupliquée ou antérieure, curseur inférieur au maximum, page au-dessus de la limite, corps trop long et divergence REST/temps réel.

Le serveur ne doit jamais laisser un filtre, cache ou projection réassocier un message à une autre conversation. Les diagnostics peuvent indiquer le type d’invariant violé et un compteur, jamais les identifiants, corps, curseurs ou joueurs bruts.

Le candidat `180651Z` ne couvre pas ce nouveau jalon. Son successeur doit intégrer ces preuves, le révoquer dans `CANDIDATE-STATUS.json` et rester `DeploymentAuthorized=false` jusqu’aux portes SQL, .NET 8, TLS/IIS et Android staging. Aucun transfert, déploiement, activation ni synchronisation n’est autorisé ici.
