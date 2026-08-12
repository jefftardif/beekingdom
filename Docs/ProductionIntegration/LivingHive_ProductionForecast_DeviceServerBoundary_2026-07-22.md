# Prévision de production — audit appareil/serveur

Le cœur serveur existant `HiveOfflineProductionSnapshotFactory` fournit déjà les éléments autoritaires nécessaires à une prévision locale : `ServerUtc`, `ProductionAsOfUtc`, durée reconnue bornée, version de catalogue, taux horaire, capacité et pending plafonné par bâtiment, avec tri déterministe et identités joueur/ruche/monde/serveur.

L'appareil peut calculer « temps avant plein » et « prochain retour utile » uniquement à partir de ce snapshot confirmé, puis naviguer vers `BuildingKey` via « Voir ». Il ne doit ni modifier les soldes, ni avancer l'horloge, ni collecter automatiquement, ni substituer sa propre heure/taux/capacité.

Aucun nouveau contrat serveur n'est requis maintenant. Le flag `HiveOfflineProduction:Enabled` reste fermé et aucune route HTTP n'est exposée tant que session/auth/transport ne sont pas raccordés. Le modèle durable ne persiste toujours pas encore un marqueur/pending de production par bâtiment : le snapshot reste une projection bornée, pas une comptabilisation complète de production.

Fichier ajouté : ce rapport uniquement. Aucun Assets, chat, candidat ou déploiement modifié.
