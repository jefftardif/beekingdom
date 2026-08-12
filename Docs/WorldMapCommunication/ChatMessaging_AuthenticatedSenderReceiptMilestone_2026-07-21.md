# Chat — reçu d’envoi lié à l’identité authentifiée (2026-07-21)

## Résultat

Le reçu d’envoi doit désormais retourner un `senderPlayerId` exactement égal au `PlayerId` de la session qui a signé la requête HTTP.

La comparaison s’ajoute aux invariants déjà exigés :

- même conversation ;
- même `clientRequestId` ;
- même corps ;
- séquence positive ;
- `serverSequence` identique à la séquence du message ;
- `messageId` présent.

La validation est exécutée dans la couche d’envoi avec la session effectivement utilisée. Si un 401 déclenche un rafraîchissement, c’est l’identité de la session rafraîchie qui sert à corréler le reçu. Aucune seconde lecture de session n’est effectuée après réponse.

## Changement de compte sûr

Si l’identité change pendant une opération ou si le serveur retourne un message appartenant à un autre expéditeur :

- le reçu est rejeté avec `message_receipt_mismatch` ;
- l’entrée d’outbox demeure persistante ;
- aucun cache ni séquence locale n’est modifié ;
- aucune fausse quittance n’est créée.

## Validation

- reçu parfaitement corrélé accepté ;
- reçu de `p2` pour une requête signée par `p1` rejeté et conservé ;
- session exacte réutilisée sans accès supplémentaire ;
- parcours de rafraîchissement 401 existant conservé ;
- toutes les validations de reçu précédentes conservées.

Suite isolée Communication : **118/118 tests réussis**, compilation sans erreur ni avertissement.

## Directive serveur

Le `senderPlayerId` retourné par POST message doit provenir exclusivement de l’identité authentifiée, jamais du corps de requête. Le reçu idempotent doit être indexé par joueur, conversation et `clientRequestId`, puis relire le message durable correspondant avant réponse.

Les tests HTTP/SQL doivent tenter : même `clientRequestId` entre deux joueurs, même conversation, corps identique et différent, rotation de jeton du même joueur, changement réel de compte, coupure après commit puis reprise. Aucun reçu du joueur A ne doit être observable ou utilisable par B.

Le candidat `BeeKingdom.Server.20260721T195555Z` prouve l’enregistrement correct de la migration 064 mais ne couvre pas encore cette corrélation d’identité ni les deux jalons de reprise envoyés ensuite. Son successeur doit intégrer l’ensemble, le révoquer et rester `DeploymentAuthorized=false`.

Aucun transfert, déploiement, activation ni synchronisation n’est autorisé ici.
