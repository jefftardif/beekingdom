# Réconciliation des branches Chat, Alliance, Nurserie — 2026-10-16

La branche deploy (609401a8, livrée le 13 octobre) contient les
sécurités Chat/Alliance qui manquaient à main (44de32f1 avant fusion).
Main possède la Nurserie à cohorts et les corrections Unity.
Ne pas remplacer globalement deploy par main.

Fusion dans le staging existant api-release-20261013, sur une
branche dédiée basée sur origin/deploy. Deux conflits résolus :
le modèle Hive conserve Banque et Nurserie simultanément, et un test
patrouille conserve sa vérification de réservation persistée.
Les réparations Chat/Alliance/Auth/News déployées sont préservées.
Les quatre suites BKStudio Chat/Alliance restent présentes.
La Nurserie reste désactivée dans les configurations normales et
Production; BroodVitality aussi. Les patrouilles restent actives
en production. Un espace final parasite a été corrigé dans un
fichier Unity sans modification fonctionnelle.

Preuves :
- Build Server Release : 0 erreur, 4 avertissements.
- 284/284 tests métier HiveOperations.
- 83/83 tests API Chat.
- 183/183 tests API Alliance et Auth.
- 106/106 tests API Nurserie, News, Banque, combat et migrations.
- Staged diff --check : 0 erreur.
- 26 fichiers à mettre à jour sur main, aucun ne recouvre
  les 20 fichiers Unity localement modifiés.

La suite API intégrale n'est pas certifiée : le testhost s'est
bloqué sans produire de verdict. Seul le processus de test local
a été arrêté, sans effet sur Unity ou SRVESDT.

Ne pas publier la branche deploy sans une certification complète.
Unity est actuellement en mode Play sur main. Préserver sa session.
Un futur déploiement doit sauvegarder et comparer les artefacts
de production avant toute mise à jour.