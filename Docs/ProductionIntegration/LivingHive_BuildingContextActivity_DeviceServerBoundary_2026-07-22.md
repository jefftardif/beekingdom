# Activité contextuelle des bâtiments — audit serveur

Les snapshots existants suffisent pour un rendu dérivé et non officiel : `HiveOperationResumeSummaryFactory` expose les opérations actives, leur `Kind`, destination et temps UTC; `HiveOfflineProductionSnapshotFactory` expose production, pending, capacité et taux par bâtiment. Le client peut mapper ces faits vers production/remplissage, amélioration, soin/formation ou patrouille uniquement lorsque le `Kind`/catalogue serveur le permet.

Limite honnête : les modèles actuels ne distinguent pas toujours soin et formation, ni formation et patrouille, et ne persistent pas encore un marqueur de production par bâtiment. Le client doit alors afficher un statut générique ou absent, jamais inventer une activité officielle. Mouvement réduit reste purement local.

Aucun nouveau contrat serveur n'est requis maintenant : aucun coût, badge, notification, progression ou mutation n'est touché. Les snapshots restent read-only, derrière leurs drapeaux fermés, sans route HTTP tant que l'auth/session/transport officiel n'est pas raccordé.

Fichier ajouté : ce rapport uniquement. Aucun Assets, chat, candidat ou déploiement modifié.
