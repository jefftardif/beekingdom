# M084-CL — Correctif confirmé : bouton "i" de production traversait Chat Royal

## Root cause confirmée (preuve : clic → panneau "Transformation" affiché)

`HiveMapProductionInfoBootstrap` (bouton "i" sur Réserve de miel/Entrepôt/
Transformation) maintenait sa propre liste d'exceptions dans son `OnGUI`
(Recherche, Activités, Palais Royal, Armée) au lieu de réutiliser le verrou
partagé `HiveMapOverlayInputGateBootstrap.IsAnyOverlayBlocking()`. Chat
Royal (et tout autre overlay plein écran non listé) n'y figurait donc pas :
le bouton restait dessiné **et cliquable** par-dessus, expliquant à la fois
la petite icône visible et le panneau "Transformation" qui s'ouvrait au clic.

C'est un bug distinct de M083-CL (curseur clavier orphelin, hypothèse
raisonnable mais qui ne visait pas la vraie cause) - M083 reste en place
(inoffensif) mais **M084 est le vrai correctif**.

## Correctif

Ajout de `HiveMapOverlayInputGateBootstrap.IsAnyOverlayBlocking()` à la
garde d'`OnGUI`, même verrou déjà utilisé par
`HiveMapBuildingUpgradeProgressBootstrap` pour la barre de construction.

- Fichier : `HiveMapProductionInfoBootstrap.cs`.
- Compilation vérifiée propre.

Commit local uniquement, aucun push.
