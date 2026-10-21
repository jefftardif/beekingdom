# 37 — Déploiement sûr, feature flags, rollback et reprise après panne

## Contexte utilisateur

PCJEFF sert au développement Unity et aux tests visuels, SRVESDT à l'API officielle et aux tests fonctionnels, GitHub est l'historique du code. L'Alpha n'ayant pas encore commencé lors des décisions précédentes, le produit peut évoluer vite, **mais cela n'autorise pas la perte ou l'écrasement silencieux des données existantes**.

**Documenter ne vaut pas autorisation de déploiement**. L'agent codeur suit le mandat qui lui est donné séparément. En l'absence de mandat explicite, il crée code/tests/PR mais ne pousse pas en production.

## Avant développement

1. Établir l'état Git réel : branche, HEAD, fichiers modifiés et commits non publiés.
2. Ne pas écraser les modifications des autres agents ni reset le worktree main.
3. Lire le README et les docs 01, 17, 19, 21, 23, 25, 39 et 40.
4. Vérifier sauvegarde et restauration réalisable des données du SRVESDT avant migration.
5. Confirmer environnement serveur (.NET, options, SQL/JSON, chemins et configuration), sans exposer de secrets.
6. Définir des feature flags indépendants et comportements OFF/READ_ONLY/ON/DRAIN.

## Stratégie de livraison recommandée

- Déployer d'abord les migrations **additives** et services sous flag OFF.
- Lire/sérialiser les anciens états et rejouer tests complets.
- Activer lecture de sites pour compte QA autorisé seulement.
- Activer population civile et première expédition pour comptes QA autorisés.
- Tester deux comptes sur même site : concurrence et revendication d'un projet.
- Étendre graduellement sans réinitialiser les scores et sans toucher aux anciens nodes militaires.
- Surveiller création/destruction de honey/pollen/wax et écarts de population.
- Activer Alliance puis capacités territoriales/PvP dans une phase ultérieure.

## Rollback

**Rollback binaire** : ancien binaire doit ignorer champs inconnus ou une version compatible est conservée. Ne jamais supprimer automatiquement de nouvelles colonnes en revenant à un code antérieur.

**Rollback fonctionnel** :
- OFF interdit tout nouveau départ ; DRAIN accepte fin/rappel/récompense des missions existantes.
- Maintenir les ledgers de contributions et les snapshots lisibles.
- Restaurer une ancienne version de configuration sans faire gagner/perdre ressources.
- Documenter comment arrêter les jobs de règlement sans laisser de réservations impossibles à libérer.

**Rollback données (dernier recours)** : uniquement sauvegarde validée, fenêtre/impact documentés, rapprochement des transactions survenues entre sauvegarde et retour en arrière. Pas de RESTORE sauvage qui efface les autres projets.

## Incident : serveur s'arrête pendant un raid floraison

1. Vérifier état DB et outbox.
2. Redémarrer en lecture seule si le règlement est suspect.
3. Exécuter rapport de réconciliation avant toute réparation.
4. Résoudre missions « dues » idempotemment.
5. Redémarrer consommateur de ledger et notifications.
6. Ouvrir mutations une fois absence de duplications confirmée.
7. Informer les testeurs si effets observables.

## Critères de publication

- Tests multi-joueurs réels et hors connexion passés.
- QA Unity FR/EN, zéro régression connue majeure dans Nurserie, Académie, collecte, Alliances, Défense/Infirmerie.
- Sauvegarde et restauration démontrées.
- Volume/ressources/charge observés.
- Journaux idempotency/erreurs fonctionnels et fenêtre de retour arrière.
- Rapport versionné avec SHA/commit des composants code et configuration.
- Validation produit des règles économiques et mécaniques concernées.

## Déclaration de livraison

L'agent précise pour chaque lot :
- fichiers modifiés et pourquoi ;
- tests exécutés + résultats vérifiables ;
- feature flags et statut OFF/READ_ONLY/ON ;
- migrations appliquées et sauvegardes ;
- API réellement disponibles vs propositions ;
- points restants et risques ;
- commit et PR, déploiement oui/non et endpoint de preuve.

