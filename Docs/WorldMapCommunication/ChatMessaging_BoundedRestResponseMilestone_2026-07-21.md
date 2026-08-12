# Chat — réponses REST bornées (2026-07-21)

## Résultat

Le transport Unity ne stocke plus une réponse REST de taille arbitraire dans un `DownloadHandlerBuffer` sans limite. Un gestionnaire progressif interrompt maintenant la réception dès que la borne configurée serait dépassée.

- défaut : 1 048 576 octets ;
- configuration : `RemoteChatClientOptions.MaxResponseBytes` ;
- minimum autorisé : 1 024 octets ;
- maximum autorisé : 4 194 304 octets ;
- la borne exacte est acceptée ;
- l’octet suivant provoque l’arrêt sans croissance supplémentaire du tampon.

Une réponse interrompue n’est jamais désérialisée, même si le statut HTTP reçu est 200. Le fournisseur la classe comme erreur de transport avec un message local générique; le corps partiel n’est ni retourné ni interprété. Une opération persistante demeure donc non acquittée et pourra suivre sa reprise idempotente normale.

## Validation

- remplissage progressif jusqu’à la borne exacte ;
- dépassement d’un octet sans croissance ;
- refus des bornes inférieure et supérieure invalides ;
- refus par la composition complète du client ;
- réponse 200 marquée incomplète rejetée comme erreur de transport.

Suite isolée Communication : **100/100 tests réussis**, compilation sans erreur ni avertissement.

## Directive serveur

Le serveur doit borner et paginer toutes les réponses chat afin de rester largement sous la limite client : conversations, messages, reçus, signalements, lecture, capabilities et traductions. IIS et le proxy ne doivent pas remplacer une erreur par une page HTML volumineuse. Les tests HTTP doivent couvrir `Content-Length` absent, transfert segmenté, taille exacte, dépassement d’un octet, compression et erreur HTML d’intermédiaire.

Une réponse trop grande doit être corrigée par pagination ou réduction du contrat, jamais par augmentation automatique de la limite mobile. Les logs peuvent indiquer `response_too_large` et le nombre d’octets borné, mais ne doivent enregistrer aucun corps, message, traduction, jeton ou identifiant brut.

Le prochain candidat reste `DeploymentAuthorized=false` jusqu’aux portes SQL, HTTP .NET 8, TLS et Android. Aucun transfert, déploiement, activation ni synchronisation n’est autorisé par ce jalon.
