# Ambiance réactive — frontière appareil/serveur

États honnêtement disponibles :

- amélioration active, via `HiveOperationResumeSummary` (`Kind=BuildingUpgrade`, destination et temps UTC);
- production en attente/saturée, via `HiveOfflineProductionSnapshot` (pending, capacité, taux, bâtiment);
- activité générique d'opération lorsque le type serveur est connu.

États à garder absents ou génériques : soin/couvain et alerte défense, car les modèles actuels ne portent pas de preuve serveur distincte; météo, faute de snapshot fiable. L'appareil ne doit jamais inférer sécurité, danger, progression ou économie depuis une préférence locale, une sélection UI ou une animation.

Mouvement réduit et mode économie restent locaux. Aucun contrat serveur supplémentaire, mutation ou endpoint n'est requis; les drapeaux/routes existants restent fermés.

Fichier ajouté : ce rapport uniquement. Aucun Assets, chat, build, déploiement ou candidat modifié.
