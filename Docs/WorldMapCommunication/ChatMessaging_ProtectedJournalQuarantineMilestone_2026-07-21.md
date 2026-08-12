# Chat et messagerie — quarantaine contrôlée des journaux protégés

Date : 2026-07-21  
Responsable : Communication

## Résultat

La composition du client expose maintenant `ChatPendingPartitionRecovery`. Lorsqu'une partition locale est illisible, l'interface peut lancer explicitement `QuarantineAndReset` avec un identifiant de récupération GUID. Les quatre enveloppes restent chiffrées : elles sont copiées sous des clés de quarantaine liées à la même partition, relues et comparées intégralement avant toute suppression des clés actives.

Si une copie ou sa vérification échoue, les sources sont conservées. Une quarantaine réussie retourne un reçu indiquant le nombre de fichiers, la suppression des sources et la conservation du secours. Aucune valeur n'est déchiffrée par ce mécanisme.

`Restore` remet les enveloppes en place seulement si aucune nouvelle donnée active n'existe. Il refuse tout écrasement, vérifie chaque restauration puis supprime les copies de secours. Une panne intermédiaire conserve les sauvegardes autant que le support le permet et produit une exception structurée précisant l'état source/secours.

## Validation

- Compilation isolée Communication : réussie, sans erreur ni avertissement.
- Suite ciblée : 72/72 réussie.
- Deux enveloppes sont mises en quarantaine, les sources disparaissent, puis la restauration reconstitue exactement les octets et supprime les secours.
- Une nouvelle donnée active bloque la restauration et reste intacte; l'ancienne enveloppe demeure en quarantaine.
- Une panne d'écriture du secours survient avant toute suppression et conserve la source.
- Le premier passage de test a détecté puis permis de corriger une clé de suppression incomplète; la suite complète finale passe.
- Aucun déploiement, activation ni synchronisation effectué.

## Directive d'intégration

Cette opération doit rester une action locale explicite, précédée d'une explication localisée indiquant que des opérations non envoyées seront temporairement retirées de la file active. Elle ne doit jamais être déclenchée automatiquement au démarrage ni depuis le serveur. En staging Android : altérer une enveloppe, vérifier l'état `LocalStorageUnavailable`, mettre en quarantaine, redémarrer avec une file active vide, restaurer avant toute nouvelle écriture puis drainer idempotemment. Répéter avec une nouvelle écriture après reset et vérifier que la restauration est refusée sans écrasement. Ne jamais téléverser les enveloppes de quarantaine au serveur ou dans les journaux.
