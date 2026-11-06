# BeeKingdom — Point de contrôle Git serveur du 5 novembre 2026

## État : WIP / aucun déploiement autorisé

Branche : checkpoint/bk-server-20261105.
Base : origin/main d74d8aa95c579ce8610e89dcfaa11216ede4bec8.

Ce point de contrôle préserve 65 fichiers serveur provenant de PCJEFF :
6 fichiers C# suivis modifiés, 50 nouveaux fichiers C#, 3 migrations SQL,
3 projets QA .csproj et 3 README QA. Tous ont été copiés dans un worktree
indépendant et vérifiés par SHA-256 sans changement concurrent détecté.

Aucun fichier appsettings, clé, certificat, mot de passe ou script PowerShell
non examiné n'est inclus. Le balayage des formats de jetons connus n'a
détecté aucun résultat. Cette vérification ne remplace pas un audit de sécurité.

## Tests réellement exécutés

- Tests métier BeeKingdom.HiveOperations.Tests : 328 / 328 réussis, 0 échec.
- Tests API filtrés Civil / Floral / Territory : 20 / 20 réussis, 0 échec.
- Suite API complète : commencée, mais aucun résultat final après plus de
  trois minutes et journal inchangé. Le processus dotnet de cette copie QA
  uniquement a été interrompu et son journal conservé. Résultat : INCOMPLET.
- Garde de compatibilité P18 : NO-GO. Le code local manque des protections
  de lecture seule dans CivilPopulationService, FloralAllianceProjectModels
  et les endpoints Program, présentes dans la référence P18 déployée.

## Garde de régression

Le fichier Server/ops/release-gates/Validate-P18SafetyContracts.py est une
sonde conservatrice sans mutation : il doit retourner NO-GO si les expressions
des garde-fous read-only / CanRead / CanCreate sont absentes. Sa réussite ne
suffira pas seule : les tests SQL, autorisations, concurrence et rollback
doivent ensuite être exécutés.

## Restrictions

- Ne PAS fusionner cette branche dans main.
- Ne PAS publier ces DLL, migrations ou paramètres sur SRVESDT.
- Ne PAS mettre à jour les permissions ou l'accès SQL pour contourner le gate.
- Ne PAS interpréter les 328 + 20 tests comme une validation complète.
- Réconcilier la logique P18 dans une branche de code testée, sans écraser
  les évolutions récentes. Les fonctionnalités de pollinisation doivent rester
  protégées jusqu'à une preuve complète de compatibilité.
- Refaire la suite complète API sous exécution bornée, relever la cause du
  blocage, puis valider une restauration SQL et les parcours multijoueurs.

## Préservation des travaux

Le projet C:\projets\beekingdomgame-master est resté sur sa branche main,
avec tous ses fichiers non commités et non suivis inchangés.
Aucun reset --hard, force push, stash destructeur, migration SQL ou
déploiement de production n'a été effectué.

Worktree isolé :
C:\BeeKingdom\staging\BK-Server-Checkpoint-20261105

Journaux QA dans C:\BeeKingdom\staging :
BK-Server-Checkpoint-20261105-domain-tests.log
BK-Server-Checkpoint-20261105-api-tests.log
BK-Server-Checkpoint-20261105-api-test-stall.txt
BK-Server-Checkpoint-20261105-api-targeted.log
