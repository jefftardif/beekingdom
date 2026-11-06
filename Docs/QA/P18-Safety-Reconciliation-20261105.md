# BeeKingdom — garde-fous P18 restaurés sans perdre les routes récentes

Date : 5 novembre 2026. Branche : fix/bk-server-p18-gates-20261105.

## Résolution
Partant du point de contrôle checkpoint/bk-server-20261105, ce correctif reprend les protections de lecture seule du lot P18 pour la population civile et les projets floraux d’alliance. Il ajoute également FileShare.Delete aux lectures du stockage JSON atomique.

Quatre fichiers métier sont alignés sur la source P18 précédemment validée, ainsi que quatre fichiers de tests. Les modifications de Program.cs sont limitées aux gardes des routes de lecture/création florale et de lancement de cohortes.

**Conservé sans remplacement :** démarrage du positionnement canonique des ruches, nouvelles routes des territoires, identité et API du serveur, migrations et configurations existantes.

## Garde de sécurité
Le script Server/ops/release-gates/Validate-P18SafetyContracts.py retourne maintenant SOURCE_TRIPWIRE_PASS. Cela vérifie la présence des gardes dans le code, mais ne remplace pas les tests.

## Tests réellement exécutés
- Suite métier complète : 330/330 tests réussis, 0 échec.
- Suite API complète : 716 réussis, 9 ignorés / 725, 0 échec (4 minutes 27 secondes).
- Sous-ensemble métier Civil/Floral/Territory : 40/40 réussis.
- Sous-ensemble API Civil/Floral/Territory : 22/22 réussis.
- Contrôle du diff : 9 fichiers modifiés, aucun remplacement des nouveaux endpoints canonical/territory.

## Non validé
- SQL production : migrations, autorisations, transactions, données existantes et restauration effective.
- Worker de règlement automatique en production et réel parcours à deux joueurs.
- Flag de lecture seule dans l’appsettings réellement déployé.
- Fusion de cette branche dans main, et déploiement SRVESDT.

**Décision : source serveur apte à poursuivre la revue et l’intégration en QA, mais pas à déployer.** Le code est sur une branche isolée, sans reset, force push, changement d’accès ou mutation SQL.

Rapports de tests locaux dans C:\BeeKingdom\staging\BK-Server-P18Safe-20261105-*.log
