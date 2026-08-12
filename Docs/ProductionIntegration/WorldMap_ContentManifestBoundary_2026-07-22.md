# Manifeste de contenu WorldMap — frontière fermée

Le serveur expose, lorsque `WorldMapContentManifest:Enabled=true`, `GET /runtime/world-map-content-manifest`. Le contrat `world-map-content-v1` contient `channel`, `version`, `platform`, `minimumAppVersion` et des bundles `{bundleId,sizeBytes,sha256,uri}`. Les URLs sont HTTPS uniquement, sans userinfo, query ni fragment; le SHA-256 comporte exactement 64 hexadécimaux. Chaque bundle est limité à 512 MiB, le manifeste à 2 GiB cumulés, les IDs/channel/platform utilisent des tokens sûrs et les IDs sont uniques sans casse.

Le pointeur de channel est courtement cacheable (`ETag`, `Cache-Control: public, max-age=60, must-revalidate`). Les bundles référencés doivent rester immuables et être servis ultérieurement par un CDN autorisé; aucun bundle ni contenu WorldMap n'a été copié ou hébergé dans cette tranche. Le endpoint est public car il ne révèle qu'un catalogue statique, sans bearer ni état joueur.

Absent ou faux, le flag répond 503 `content.unavailable` avant toute lecture de contenu. Production reste fermée par défaut.

Configuration invalide ou flag fermé : 503 `content.unavailable` avec `Cache-Control: no-store`, sans refléter la configuration.

Preuves : `WorldMapContentManifestEndpointTests` 7/7 réussis sous net10.0 avec `DOTNET_ROLL_FORWARD=Major` (fermeture, HTTPS/URI, doublons, bornes individuelles et cumulées, ETag/304). La build via les tests a réussi; avertissement préexistant Microsoft.Data.SqlClient. Aucun candidat ou déploiement.

Fichiers :
- `Server/src/BeeKingdom.Server/WorldMapContentManifestOptions.cs`
- `Server/src/BeeKingdom.Server/WorldMapContentManifestContracts.cs`
- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/src/BeeKingdom.Server/appsettings.json`
- `Server/src/BeeKingdom.Server/appsettings.Production.json`
- `Server/tests/BeeKingdom.Tests/WorldMapContentManifestEndpointTests.cs`
