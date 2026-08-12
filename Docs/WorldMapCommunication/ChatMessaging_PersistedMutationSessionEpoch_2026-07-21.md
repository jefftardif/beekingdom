# Chat — mutations persistées liées à la génération de session (2026-07-21)

## Résultat

La génération de session capturée au début d’une mutation est maintenant propagée jusqu’à sa requête HTTP et son reçu.

Cette règle couvre :

- création de conversation ;
- envoi de message ;
- signalement de modération ;
- curseur de lecture ;
- reprise de chacune de ces files persistantes.

Après chaque écriture de journal et avant tout départ HTTP, le client vérifie que la génération n’a pas été révoquée. Un logout pendant une écriture lente produit `Cancelled` / `local_session_changed` : l’entrée durable reste dans la partition d’origine, mais l’ancienne session ne peut plus envoyer la mutation.

## Garantie

Une opération possède une seule identité et une seule génération de bout en bout. Une déconnexion ne peut donc pas couper le parcours entre persistance et réseau de manière à faire partir ensuite une requête avec la session capturée avant logout.

La conservation du journal est volontaire : au retour du même compte, la reprise utilise le même identifiant idempotent et obtient l’effet ou le reçu durable sans doublon.

## Validation

Scénario déterministe :

1. l’envoi de A valide sa session ;
2. l’écriture du journal est bloquée avant sa fin ;
3. A se déconnecte ;
4. l’écriture est libérée et terminée ;
5. la génération initiale est rejetée ;
6. zéro appel HTTP, une entrée persistante conservée.

Suite isolée Communication : **125/125 tests réussis**, compilation sans erreur ni avertissement.

## Directive serveur et staging

Le serveur doit conserver ses garanties idempotentes pour les reprises après une coupure située avant ou après commit. La matrice staging doit distinguer clairement : journal durable terminé mais zéro octet HTTP, requête reçue sans commit, commit sans reçu client, puis drainage sous le même joueur. Aucun de ces cas ne doit produire un doublon ni permettre à B de reprendre l’opération de A.

Le candidat serveur courant reste `BeeKingdom.Server.20260721T201425Z`, `DeploymentAuthorized=false`. Un nouveau candidat n’est requis que si les preuves serveur révèlent un manque réel.

Aucun transfert, déploiement, activation ni synchronisation n’est autorisé par ce jalon.
