# Vérification de parité serveur — 2026-10-16

La branche deploy correspond au commit 609401a8 du 2026-10-13 à 15:21,
et les DLL serveur actives datent du 2026-10-13 à 15:21:58.
Le dépôt main (commit cd8fd289) a les mêmes blobs Git que deploy pour :
CombatPatrolService, CombatSquadReservationService,
HiveTroopDeploymentAccounting et RoyalReserveService.
Les deux branches déclarent la championne dans le contrat HTTP
de réservation et ont le modèle Hive version 13.

Vérification SRVESDT : / et /health HTTP 200 ; routes GET
combat/squad-reservation, combat/patrol et bank/royal-reserve
sans authentification : HTTP 401. Les options CombatRecruitment,
CombatSquadReservation et CombatPatrol sont activées sur SRVESDT.
BroodVitality reste désactivé.

Tests précédents de réconciliation Git : 284/284 métier, 35/35 HTTP
ciblés, compilation Release sans erreur. La suite HTTP complète
n'est pas certifiée à cause d'un processus de test bloqué.

La publication de main sur deploy serait risquée : les branches
divergent sur des fonctions Chat, Alliance et Nurserie. Aucune
publication n'est nécessaire pour les contrats Caserne et Banque
déjà en service ; aucun fichier ni donnée du serveur n'a été modifié.

La branche main locale possède 29 commits de plus que origin/main.
L'éventuelle publication Git de ces changements est distincte
du serveur actif et n'a pas été lancée lors de cette vérification.