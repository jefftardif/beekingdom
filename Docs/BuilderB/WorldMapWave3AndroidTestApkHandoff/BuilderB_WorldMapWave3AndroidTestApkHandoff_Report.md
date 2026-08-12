# Builder-B - World Map Wave3 Android Test APK Handoff

## Resume

Le handoff de compilation Android Development Wave3 est prepare hors Unity. Aucun build n'a ete lance et aucun fichier Unity, scene, `ProjectSettings`, asset, `.meta` ou APK n'a ete modifie.

Le chemin canonique du rapport se trouve hors de la racine inscriptible de cette session. Ce fichier est le fallback autorise:

`C:\projets\beekingdomgame-master\Docs\BuilderB\WorldMapWave3AndroidTestApkHandoff\BuilderB_WorldMapWave3AndroidTestApkHandoff_Report.md`

## Sources lues

- handoff d'integration Unity Wave3 Builder-B;
- `Assets/Editor/WorldMapMmoAndroidBuild.cs`;
- `Assets/Editor/AndroidBuild.cs`;
- `ProjectSettings/EditorBuildSettings.asset`;
- `ProjectSettings/ProjectSettings.asset`;
- `ProjectSettings/ProjectVersion.txt`;
- configuration splash Development;
- protocoles APK/device Builder-B et Builder-C;
- historique `Builds/Android/BeeKingdom.apk` et son log du 2026-07-13.

Toutes les sources Unity ont ete lues seulement.

## Conclusions d'audit

Les scripts Android existants ne conviennent pas a ce build:

- sortie fixe `BeeKingdom.apk` supprimee avant build;
- `BuildOptions.None` au lieu de Development;
- script WorldMap sans scene splash;
- script general avec treize scenes;
- ecriture de `Assets/Resources/InternalBuildInfo.txt`.

Le handoff impose donc une future methode dediee apres QA, sans ecrasement et avec exactement:

1. `Assets/Scenes/SandboxPlayground.unity`;
2. `Assets/Scenes/WorldMapMmoFullscreenFoundation.unity`.

Flags: `BuildOptions.Development | BuildOptions.AllowDebugging`. Signature: debug Unity uniquement. Aucun secret ou keystore release.

## Sortie future exacte

`C:\projets\beekingdomgame-master\Builds\Android\BeeKingdom_WorldMapWave3_uib-wave3-continuous-v1_Development_001.apk`

Le fichier est absent au moment du handoff. Aucune reservation vide n'a ete creee. S'il apparait avant le futur build, la tentative est NO-GO et une revision `_002` doit etre emise sans suppression.

## Preflight observe

Au 2026-07-14:

- Unity `6000.2.10f1`: present;
- Android Build Support: present;
- OpenJDK `17.0.9`: present;
- SDK 34/35/36 et Build Tools 36.0.0: presents;
- NDK r27c: present;
- Gradle 8.13: observe dans le dernier build;
- ADB 36.0.0: present;
- espace libre: environ 463.19 GiB;
- custom keystore: desactive;
- cible `_001`: absente.

Cet instantane documente les prerequis; il ne ferme aucun gate et doit etre rejoue le jour du build.

## Historique APK conserve

Dernier APK existant, historique seulement:

- path: `Builds/Android/BeeKingdom.apk`;
- taille: `54,716,472` octets;
- SHA-256: `10456d141510ae2441e77f46903723a90593ce010611475f5daa1e2caa437696`;
- last write UTC: `2026-07-13T23:46:41.2770961Z`;
- package: `com.bkdhoneystudio.beekingdom`;
- signature: Android Debug;
- build Wave3 integre: non.

Il n'a pas ete installe, copie, supprime ou modifie pendant ce travail.

## Gate actuel

Decision actuelle: `NO-GO`.

Raisons:

- QA runtime Step4D strict PASS non etabli dans ce handoff;
- QA art/bundle Wave3 strict PASS non etabli dans ce handoff;
- integration Unity Wave3 non realisee et donc non validee QA;
- methode de build dediee volontairement non creee avant les gates.

Le handoff est toutefois exploitable immediatement apres fermeture de ces gates.

## Livrables

- procedure deterministe Development APK;
- preflight PowerShell en lecture seule;
- manifest machine-readable;
- matrice GO/NO-GO pre-build, package et smoke;
- smoke avec et sans appareil;
- strategie rollback/nettoyage non destructive;
- chemins et non-claims explicites.

## Non-claims

- aucun APK Wave3 construit;
- aucune preuve appareil;
- `PHYSICAL_DEVICE_PROOF = PENDING`;
- aucune signature release;
- aucun serveur/reseau live;
- aucune economie ou persistance officielle;
- aucune publication production;
- aucune route terrestre pour les vols.

## Verdicts

WORLD_MAP_WAVE3_ANDROID_TEST_APK_HANDOFF = PASS

ANDROID_BUILD_PREREQUISITES_DOCUMENTED = YES

NO_APK_BUILT_BEFORE_GATES = YES

READY_FOR_ANDROID_BUILD_AFTER_UNITY_QA = YES
