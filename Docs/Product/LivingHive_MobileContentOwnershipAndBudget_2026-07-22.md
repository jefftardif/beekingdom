# LivingHive — propriété et budget du contenu mobile

Date : 22 juillet 2026  
Responsable : Architecte  
État : audit local terminé; migration des terrains non autorisée dans cette tranche

## Cause mesurée

Le projet contient 995,1 Mio de fichiers source sous des dossiers Unity
`Resources`. À lui seul,
`Assets/BeeKingdom/Playground/Resources/WorldMapWave6Runtime` représente
922,8 Mio et 5 004 fichiers livrables. Unity inclut automatiquement tout ce qui
se trouve sous `Resources`, même si le Player ne contient que la scène
`LivingHive`.

| Groupe | Fichiers | Taille source |
|---|---:|---:|
| Terrain canonique `wave5method_12288_preview` | 2 502 | 825,0 Mio |
| Ancien terrain `v1` | 2 502 | 97,8 Mio |
| Autres ressources du jeu | 269 | 72,3 Mio |
| Total `Resources` | 5 273 | 995,1 Mio |

Le terrain canonique utilise 2 500 textures de 516 × 516 pixels plus son
manifeste et ses métadonnées. Les 2 500 métadonnées Android auditées ont toutes
`textureCompression: 0` et aucun override Android. Le garde d'import exact-crop
force explicitement `TextureImporterCompression.Uncompressed`.

La scène canonique conserve bien
`useWave5Method12288PreviewRuntimePackageForPlayMode: 1`; son chargeur utilise
`Resources.Load` et `Resources.LoadAsync`. L'ancien paquet `v1` est donc inclus
dans le Player par sa seule présence dans `Resources`, même si cette scène ne le
choisit pas.

Cette structure explique la mesure Unity de 2,02 Gio compressés / 2,25 Gio non
compressés et l'échec d'emballage Android. Les PNG source ne sont pas la taille
réelle du contenu importé : les textures non compressées gonflent dans le
Player.

## Décision de sûreté de cette tranche

Aucun PNG, manifeste terrain, fichier `.meta`, scène, chemin de ressource ou
chargeur de la carte n'a été déplacé ou modifié. Les empreintes protégées et le
rendu canonique restent intacts. Une migration de 5 004 fichiers ou un changement
de compression Android est un chantier terrain explicite qui exige ses propres
preuves visuelles, hashes, tests de streaming et autorisation de créneau.

## Cible de propriété

### Dans l'installation mobile de base

- exécutable IL2CPP et assemblages requis;
- scène LivingHive, image de base actuelle et interface essentielle;
- localisation, shell de compte, contrats réseau et écrans hors ligne;
- icônes et médias nécessaires avant le premier téléchargement;
- manifeste public minimal indiquant la version de contenu attendue;
- aucun secret, compte, `HiveId` ou flag Production prérempli.

Budget interne proposé, à ratifier sur un vrai package : moins de 200 Mio pour
l'installation de base et moins de 150 Mio de téléchargement initial. Ce sont
des objectifs Bee Kingdom, pas une preuve acquise.

### Téléchargé sur l'appareil

- terrain canonique seulement, jamais les paquets d'audit ou versions obsolètes;
- bundles immuables découpés par secteurs, avec taille et SHA-256;
- cache local révocable, quota et éviction LRU;
- reprise de téléchargement, vérification avant publication et conservation de
  l'ancienne version complète tant que la nouvelle n'est pas validée;
- consentement clair sur réseau cellulaire et affichage de la taille;
- aucune récompense, progression ou autorité économique dans ces bundles.

Première cible technique à mesurer : secteurs 5 × 5 ou 10 × 10 tuiles,
compression Android adaptée et pack canonique total inférieur à 300 Mio. Le
choix de compression ne sera accepté qu'après comparaison visuelle native avec
la référence protégée.

### Sur serveur/CDN

- le serveur applicatif publie un manifeste versionné et fermé par configuration;
- le stockage objet/CDN sert les bundles statiques immuables;
- un pointeur de canal peut avoir une durée de cache courte; une version figée
  et ses bundles utilisent des URLs et ETags immuables;
- le manifeste contient version de contrat, plateforme, version minimale de
  l'app, identifiant de bundle, taille et SHA-256;
- aucune URL ne contient de secret durable et aucune donnée joueur n'entre dans
  le manifeste public;
- comptes, session, ruche, temps, mutations et récompenses restent sur le
  serveur de jeu, indépendamment des contenus visuels.

L'Intégrateur a livré la frontière Server/tests/docs :
`GET /runtime/world-map-content-manifest`, contrat
`world-map-content-v1`, public car strictement statique et sans état joueur.
Le manifeste impose HTTPS, URI sans userinfo/query/fragment, tokens sûrs,
identifiants uniques, 512 Mio par bundle et 2 Gio cumulés. Il renvoie ETag/304
et un cache public de 60 secondes; une porte fermée ou une configuration
invalide renvoie `503 content.unavailable` avec `Cache-Control: no-store`.

Le flag est faux par défaut et en Production. La relance indépendante de
l'Architecte passe 7/7 sous net10.0 et compile le serveur sans erreur; seul
l'avertissement SqlClient préexistant demeure. Aucun binaire de terrain n'a été
transféré, aucun candidat n'a été construit et aucun déploiement n'est autorisé.
Rapport :
`Docs/ProductionIntegration/WorldMap_ContentManifestBoundary_2026-07-22.md`.

## Séquence de migration proposée

1. Ajouter le contrat client de source de tuiles, avec l'implémentation
   `Resources` actuelle inchangée comme référence.
2. Construire une preuve AssetBundle locale à partir de copies logiques des
   assets canoniques, sans toucher aux pixels ni à leurs hashes source.
3. Comparer plusieurs compressions Android sur des tuiles représentatives et
   ratifier la qualité en 390×844 et 1600×900, puis sur appareil.
4. Découper et signer le seul paquet canonique; ne pas publier `v1`.
5. Introduire téléchargement, vérification, reprise, cache et rollback.
6. Seulement après équivalence fonctionnelle et visuelle, retirer le terrain du
   `Resources` embarqué selon un créneau terrain explicite.
7. Refaire F8, captures carte, package IL2CPP, inspection APK/AAB et tests
   appareil.

## Portes encore ouvertes

- autorisation explicite du chantier de migration terrain;
- Unity Hub/licence pour F8 et preuves visuelles;
- choix et mesure de la compression Android;
- capacité du serveur/CDN et contrat de manifeste ratifié;
- budget réseau, disque, mémoire et performance sur appareil bas de gamme;
- stratégie de mise à jour et rollback;
- APK/AAB complet, signature de distribution et staging.

Rapport de construction associé :
`Docs/Product/LivingHive_AndroidIl2CppPackagingAudit_2026-07-22.md`.

La synchronisation finale tentée à `2026-07-22T21:52:53Z` a échoué avant copie
avec accès refusé au partage hôte. Le rapport `.codex` antérieur reste inchangé,
sans conflit. Cette série demeure uniquement sur `C:` jusqu'à une
synchronisation lancée dans un contexte ayant accès au partage.
