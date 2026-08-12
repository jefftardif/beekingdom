# Validation finale — réservation d’escouade

Les reçus publics commit/release sont rejouables depuis la charge persistée,
quantités incluses, sans `payloadHash` ni clé interne. La rétention est bornée à
128 reçus, avec éviction déterministe et conservation du reçu courant.

Les preuves couvrent aussi le rejeu du commit après release et reconstruction,
le snapshot courant libéré associé au reçu historique exact, `long.MaxValue`
refusé en `400` sur commit et release, ainsi que l'isolation joueur/ruche.

Résultats : `CombatSquadReservationTests` 3/3 ;
`CombatSquadReservationEndpointTests` 5/5 ; suite serveur net10 346 réussis,
0 échec, 8 SQL ignorés ; build Release 0 erreur, 1 avertissement
`Microsoft.Data.SqlClient` préexistant. Flags fermés par défaut et en Production.
Aucun candidat, transfert, activation ou déploiement.
