# LivingHive — production après absence, autorité serveur et interface mobile

## Résultat

LivingHive possède désormais une tranche verticale fermée de production après absence. Le serveur calcule et persiste l’horloge UTC, les taux, les capacités, les quantités en attente, les soldes, les révisions et les reçus de collecte. Le mobile valide strictement ces données, peut conserver la dernière lecture dans un cache protégé et partitionné, puis affiche ou demande une collecte sans jamais créditer une ressource lui-même.

La fonctionnalité reste volontairement désactivée par défaut et en Production. Aucun catalogue économique réel, asset d’environnement, compte, candidat ou déploiement n’a été activé.

## Frontière appareil / serveur

### Sur l’appareil

- rendu, interaction tactile, localisation et état transitoire du panneau;
- access token en mémoire et refresh token dans le coffre Android prévu;
- dernier GET validé dans le cache AES-GCM, borné à 7 jours et partitionné joueur + ruche + contrat + route;
- même clé d’idempotence conservée pour une collecte dont le résultat réseau est incertain;
- lecture hors ligne explicite et strictement sans mutation.

### Sur le serveur

- temps UTC, durée d’absence reconnue et catalogue économique;
- taux, capacité de production, quantité décimale en attente et soldes;
- appartenance joueur/ruche, révision, validation de capacité et transaction atomique;
- reçu persistant et rejeu idempotent;
- conflits de révision, de clé, de capacité ou de production insuffisante.

Le mobile n’envoie au POST que la révision attendue et la clé d’idempotence. Il n’envoie aucun montant, taux, temps, capacité ou solde faisant autorité.

## Contrat et interface

- `GET /game/v1/hives/{hiveId}/offline-production`;
- `POST /game/v1/hives/{hiveId}/offline-production/{buildingKey}/collect`;
- contrat `living-hive-offline-production-v1`;
- correspondances strictes `honey_storage → honey`, `wax_workshop → wax`, `warehouse_cells → pollen`;
- une seule rotation de session après `401`, puis rejeu de la requête identique;
- aucune répétition aveugle d’un POST après une panne réseau;
- conflit serveur suivi d’une lecture fraîche, sans mutation locale compensatoire.

Le panneau LivingHive utilise un contrôleur injectable composé par le cycle de session mobile. Logout et changement de joueur ferment le contrôleur et purgent sa vue. Dans le build courant, le parcours officiel s’arrête au shell de connexion non configuré et le contrôleur du panneau reste indisponible; la démo locale demeure volontairement locale. L’état de panneau `Production officielle non configurée` / `Official production is not configured` est injecté uniquement par le harnais de mise en page : il désactive l’action et ne montre ni taux, ni stock, ni succès, ni reçu fictif. Il ne constitue pas une session live. Les anciennes données de démonstration restent identifiées comme aperçu local et ne deviennent jamais officielles.

## Validation

- serveur complet : **315 réussis, 0 échec, 8 SQL ignorés**;
- `HiveOfflineProductionServiceTests` : **25/25**;
- `HiveOfflineProductionEndpointTests` : **8/8**;
- build serveur Release : **0 erreur**, avertissement SqlClient déjà connu;
- harnais mobile global : **60/60**, dont 12 preuves client production et 5 preuves de présentation;
- `Assembly-CSharp` : **0 erreur**;
- F8 LivingHive final : marqueur `LivingHive manual collection checks passed`, aucun `error CS` ni `Compilation failed`, journal `Artifacts/LivingHiveOfflineProductionF8Final.log`;
- catalogues `fr-CA` et `en-US` : **1080/1080**, alignés et sans doublon;
- capture Unity finale : sortie propre, journal `Artifacts/LivingHiveOfflineProductionCaptureFinal.log`.

Deux premières preuves ont été refusées honnêtement : la passe `-nographics` ne pouvait pas produire une image, puis l’inspection native a détecté l’ancien texte `simulation locale` dans le panneau officiel. Le panneau a été corrigé, recompilé, repassé dans F8 et recapturé avant ratification.

## Preuves visuelles finales

- `Docs/Product/Evidence/LivingHiveOfflineProduction/LivingHive_OfflineProduction_NotConfigured_FR_390x844.png` — SHA-256 `e3a40a4c596e060b7c5575bd4fa35e83ecd11845e704819a7438fa7537d99cd3`;
- `Docs/Product/Evidence/LivingHiveOfflineProduction/LivingHive_OfflineProduction_NotConfigured_EN_1600x900.png` — SHA-256 `9887f2d29ccd5580190255c15e074b1f7222ab6c4803941f8e434a147dc7969e`;
- manifeste : `Docs/Product/Evidence/LivingHiveOfflineProduction/LivingHiveOfflineProduction_CaptureManifest.md`.

Les dimensions sont exactes, les deux images ont été inspectées à résolution native et l’action `Serveur requis / Server required` est désactivée. Ce sont des preuves d’état UI injectable sans données chiffrées, pas des captures d’une session serveur live. Aucun terrain 50x50, PNG de terrain, image de ruche ou scène n’a été modifié.

## Fichiers client principaux

- `Assets/BeeKingdom/Networking/HiveOfflineProductionClient.cs`;
- `Assets/BeeKingdom/Playground/HiveOfflineProductionPresentation.cs`;
- `Assets/BeeKingdom/Playground/MobileAccountSessionRuntimeBootstrap.cs`;
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`;
- `Assets/BeeKingdom/Tests/Editor/HiveOfflineProductionClientTests.cs`;
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveOfflineProductionCapture.cs`;
- `Assets/_Project/Data/Localization/Resources/Localization/strings.fr-CA.json`;
- `Assets/_Project/Data/Localization/Resources/Localization/strings.en-US.json`;
- `Artifacts/HivePerimeterClientHarness/HiveOfflineProductionPresentationHarnessTests.cs`;
- `Artifacts/HivePerimeterClientHarness/Program.cs`;
- `Artifacts/HivePerimeterClientHarness/HivePerimeterClientHarness.csproj`.

Le détail des fichiers serveur est dans `Docs/ProductionIntegration/LivingHive_OfflineProduction_ServerAuthority_2026-07-22.md`.

## Portes encore ouvertes

1. choisir et auditer le catalogue économique réel; les taux 10/5/8 vus dans les tests ne sont pas des valeurs produit;
2. prouver la persistance SQL avec `BEE_SQL_INTEGRATION_CONNECTION_STRING`;
3. configurer HTTPS/TLS staging, proxy de confiance, HiveId et asset d’environnement;
4. valider deux joueurs, changement de compte, reprise réseau et Android Keystore sur appareil physique;
5. terminer un paquet Android IL2CPP installable après migration/budget du contenu Wave6;
6. seulement ensuite ouvrir les flags, construire un candidat, déployer et observer la télémétrie.

Communication est resté gelé; aucun module, test, document ou ancrage chat n’a été modifié dans cette tranche.

## Synchronisation VM

La synchronisation officielle tentée à `2026-07-23T01:25Z` a échoué avant toute copie avec `Accès refusé` sur `\\DESKTOP-D3D29K7\BeeKingdomHost`. Le rapport `.codex/vm-sync-last-report.txt` est resté daté de `2026-07-22T02:57:51Z`, avec 0 conflit, 0 copie VM vers hôte et 4 suppressions historiques en attente. Aucun remappage, accès direct à `Z:` ou relâchement du bac à sable n’a été tenté. Tous les changements restent intacts sur `C:`.
