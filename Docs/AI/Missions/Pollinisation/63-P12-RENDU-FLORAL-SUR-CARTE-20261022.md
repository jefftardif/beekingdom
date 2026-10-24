# P12 — Habitats floraux visibles sur la carte officielle (22 octobre 2026)

Branche isolée Unity : feature/pollinisation-alpha-20261021.
Cible de rendu : WorldMapMmoFullscreenFoundationBootstrap, terrain Wave6 conservé.
Livraison : quatre textures dédiées sous Resources/Pollination, générateur reproductible,
ancrage à MapSurfaceId/MapAnchorId du catalogue serveur et état du stock partagé.

## Règles appliquées
- Un site ne s'affiche que si l'API officielle fournit surface et ancrage existants,
  que le compte authentifié active la fonction et que l'identité de surface concorde.
- Les fleurs varient suivant la réserve et la phase renvoyées par le serveur.
- La jauge de pollen reflète la réserve serveur. Le rendu ne débite aucun stock.
- Les butineuses animées correspondent uniquement à la mission civile persistée
  de ce joueur ; aucune activité d'autres joueurs n'est inventée.
- Le clic conserve la navigation vers Champs/Essaims/Alliance dans le panneau royal,
  sans refaire le terrain. Les assets sont à transparence PNG et dessinés en un seul
  appel par habitat; nombre de butineuses animées borné à trois par site actif.

## Preuves et limites
- Unity 6000.5.3f1 : campagne Pollination P12 11/11 réussie en EditMode,
  incluant la compilation du runtime et le chargement des quatre textures.
- WorldMap générique : 7/8, avec un TaskCanceledException de communication
  provenant du module externe Unity MCP pendant AtlasTileRegionReadinessComputesVisibleTiles.
  Ce résultat demeure ouvert, pas déclaré PASS.
- Le catalogue officiel SRVESDT reste non configuré et les flags client/serveur OFF :
  les sites n'apparaissent pas encore en session production.
- Aucun cliché de GameView authentifié ni cycle serveur multi-comptes réel
  n'est fourni par cette campagne. L'apparence sur écran mobile reste à certifier.

## Ce qui reste à livrer pour acceptation produit
1. Harmonisation du catalogue de sites officiels de l'Alpha avec les ancrages Wave6.
2. Migration SQL vérifiée puis activation surveillée sur SRVESDT.
3. Import contrôlé dans le projet Unity officiel main avec conservation des modifications.
4. Captures vraies GameView FR/EN aux formats portrait et paysage, inspection des clics,
   navigation Entrées/Champs/Essaims/Alliance et de l'activité.
5. Deux membres d'alliance contribuant réellement à un projet commun.
