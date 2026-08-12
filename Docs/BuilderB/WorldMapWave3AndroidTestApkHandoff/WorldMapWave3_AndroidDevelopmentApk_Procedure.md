# World Map Wave3 - Procedure deterministe de Development APK

## 1. Statut et interdiction actuelle

Cette procedure ne devient executable qu'apres trois verdicts QA `PASS` strict:

1. QA runtime Step4D;
2. QA art/bundle Wave3;
3. QA integration Unity Wave3.

Etat au moment du handoff: `NO-GO`. Aucun APK Wave3 n'a ete construit.

Un `PASS_WITH_RESERVES`, une validation Builder-C, un rapport de pipeline ou un handoff Builder-B ne remplace pas un PASS QA. Chaque gate doit fournir le chemin de son rapport autoritatif et son SHA-256.

## 2. Contrat de scenes

Le futur APK doit contenir exactement ces deux scenes, dans cet ordre:

| Index | Scene | Role |
|---:|---|---|
| 0 | `Assets/Scenes/SandboxPlayground.unity` | splash et selecteur de scenes Development |
| 1 | `Assets/Scenes/WorldMapMmoFullscreenFoundation.unity` | carte mondiale Wave3 |

Les Build Settings actuels contiennent ces scenes aux index 0 et 1, puis onze scenes supplementaires. Le futur build doit fournir explicitement le tableau de deux scenes a `BuildPlayerOptions.scenes`; il ne doit pas construire toute la liste active et ne doit pas modifier `EditorBuildSettings.asset`.

Le selecteur WorldMap est protege par `UNITY_EDITOR || DEVELOPMENT_BUILD`. `BuildOptions.Development` est donc obligatoire.

## 3. Sorties reservees sans ecrasement

APK:

`C:\projets\beekingdomgame-master\Builds\Android\BeeKingdom_WorldMapWave3_uib-wave3-continuous-v1_Development_001.apk`

Log Unity:

`C:\projets\beekingdomgame-master\Builds\Android\BeeKingdom_WorldMapWave3_uib-wave3-continuous-v1_Development_001.log`

Manifest post-build:

`C:\projets\beekingdomgame-master\Builds\Android\BeeKingdom_WorldMapWave3_uib-wave3-continuous-v1_Development_001.build-manifest.json`

Regle: si l'un de ces trois chemins existe au preflight, le build est `NO-GO`. Il est interdit de supprimer ou d'ecraser le fichier. L'Architecte doit reserver une revision `_002` dans un nouveau handoff.

L'APK historique `Builds/Android/BeeKingdom.apk` reste intact. Il ne constitue pas le candidat Wave3.

## 4. Scripts existants interdits pour ce lot

Ne pas appeler:

- `WorldMapMmoAndroidBuild.BuildWorldMapMmoAndroidApk`;
- `AndroidBuild.BuildAndroidApk`.

Le premier supprime `BeeKingdom.apk`, n'inclut que la scene WorldMap, utilise `BuildOptions.None` et reecrit `Assets/Resources/InternalBuildInfo.txt`.

Le second supprime le meme APK, utilise les treize scenes actives, emploie `BuildOptions.None` et reecrit aussi `InternalBuildInfo.txt`.

Ces comportements violent le contrat de sortie non destructive, les deux scenes obligatoires et le mode Development.

## 5. Preflight outils et environnement

Executer le script en lecture seule avant toute ouverture de build:

```powershell
& 'C:\projets\beekingdomgame-master\Docs\BuilderB\WorldMapWave3AndroidTestApkHandoff\Test-WorldMapWave3AndroidPreflight.ps1'
```

Tant que les gates ne sont pas fermes, la sortie normale doit contenir:

```text
prerequisites_pass = true
gates.all_strict_pass = false
build_allowed_now = false
no_unity_invoked = true
no_build_invoked = true
```

Apres lecture et hash des trois rapports QA, l'operateur peut transmettre les statuts au preflight:

```powershell
& 'C:\projets\beekingdomgame-master\Docs\BuilderB\WorldMapWave3AndroidTestApkHandoff\Test-WorldMapWave3AndroidPreflight.ps1' `
  -QaRuntimeStep4D PASS `
  -QaArtBundleWave3 PASS `
  -QaUnityIntegrationWave3 PASS
```

Les parametres ne sont pas des preuves: le manifeste de build doit aussi enregistrer les trois chemins de rapports et leurs SHA-256.

### Minimum obligatoire

- Unity `6000.2.10f1` exact;
- module Android Build Support;
- SDK, NDK, OpenJDK et Gradle embarques;
- `adb`, `aapt2`, `apksigner`, `zipalign` disponibles;
- au moins `20 GiB` libres sur le disque projet et temporaire;
- licence Unity valide;
- aucun autre processus Unity n'utilisant le projet;
- les deux scenes existent et sont activees;
- custom keystore desactive;
- les trois sorties futures absentes;
- baseline/hashes Unity pris avant build.

Instantane du 2026-07-14, a reverifier le jour du build:

- OpenJDK `17.0.9`;
- SDK plateformes `34`, `35`, `36`;
- Build Tools `36.0.0`;
- NDK `27.2.12479018` (`r27c`);
- Gradle `8.13` observe dans le dernier log;
- ADB `36.0.0-13206524`;
- espace libre observe: `463.19 GiB`.

## 6. Methode de build future a creer apres les gates

Builder-A doit ajouter apres QA une methode Editor dediee. Le contrat minimal est:

```csharp
string[] scenes =
{
    "Assets/Scenes/SandboxPlayground.unity",
    "Assets/Scenes/WorldMapMmoFullscreenFoundation.unity"
};

if (File.Exists(apkPath) || File.Exists(logPath) || File.Exists(manifestPath))
    throw new InvalidOperationException("Reserved Wave3 output already exists; do not overwrite.");

BuildPlayerOptions options = new BuildPlayerOptions
{
    scenes = scenes,
    locationPathName = apkPath,
    target = BuildTarget.Android,
    options = BuildOptions.Development | BuildOptions.AllowDebugging
};

BuildReport report = BuildPipeline.BuildPlayer(options);
```

Contraintes de la methode:

1. verifier les trois receipts QA avant `BuildPipeline.BuildPlayer`;
2. ne jamais appeler `File.Delete` sur une sortie;
3. ne jamais ecrire sous `Assets` pour les metadonnees du build;
4. ne pas utiliser de keystore custom/release;
5. forcer APK, pas AAB, puis restaurer la valeur initiale;
6. conserver au minimum `arm64-v8a`;
7. configurer temporairement AutoRotation avec portrait, landscape gauche/droite et sans portrait inverse;
8. restaurer dans `finally` chaque setting temporaire;
9. comparer les hashes `ProjectSettings` avant/apres;
10. produire le manifest externe adjacent a l'APK seulement apres succes;
11. refuser `ConnectWithProfiler` pour ce smoke standard;
12. ne configurer aucun endpoint ou serveur.

Le manifest externe doit inclure: scenes exactes, flags Development, hash APK, taille, UTC, version Unity, package, architectures, trois receipts QA, hashes Wave3 et non-claims.

## 7. Commande batch future

Le nom de methode ci-dessous est reserve par le handoff; elle n'existe pas encore et ne doit etre creee qu'apres les gates:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.2.10f1\Editor\Unity.exe' `
  -batchmode `
  -nographics `
  -quit `
  -projectPath 'C:\projets\beekingdomgame-master' `
  -executeMethod WorldMapWave3AndroidDevelopmentBuild.BuildAfterQaGate `
  -logFile 'C:\projets\beekingdomgame-master\Builds\Android\BeeKingdom_WorldMapWave3_uib-wave3-continuous-v1_Development_001.log'
```

Succes exige:

- code retour Unity `0`;
- `BuildResult.Succeeded`;
- APK au chemin exact;
- log sans erreur/exception bloquante;
- aucun fichier historique ecrase;
- hashes des fichiers Unity attendus stables apres restauration.

## 8. Validation statique post-build

Configurer les outils embarques pour la session:

```powershell
$android = 'C:\Program Files\Unity\Hub\Editor\6000.2.10f1\Editor\Data\PlaybackEngines\AndroidPlayer'
$env:JAVA_HOME = "$android\OpenJDK"
$env:Path = "$env:JAVA_HOME\bin;$env:Path"
$apk = 'C:\projets\beekingdomgame-master\Builds\Android\BeeKingdom_WorldMapWave3_uib-wave3-continuous-v1_Development_001.apk'
$tools = "$android\SDK\build-tools\36.0.0"
```

Puis:

```powershell
Get-FileHash -Algorithm SHA256 -LiteralPath $apk
& "$tools\aapt2.exe" dump badging $apk
& "$tools\zipalign.exe" -c -P 16 -v 4 $apk
& "$tools\apksigner.bat" verify --verbose --print-certs $apk
```

Exiger:

- package `com.bkdhoneystudio.beekingdom`;
- application debuggable/Development;
- activite de lancement presente;
- minimum SDK coherent avec le projet;
- `arm64-v8a` present au minimum;
- signature `Android Debug`, jamais release;
- zip alignment PASS;
- SHA-256 et taille reportes dans le manifest;
- log de build prouvant exactement deux scenes;
- ancien `BeeKingdom.apk` toujours hash-identique a sa baseline.

La permission Android `INTERNET` peut etre presente dans le package Unity. Elle ne constitue pas une preuve de serveur live. Le smoke doit rester local/demo et ne doit ouvrir aucun endpoint.

## 9. Smoke test avec appareil reel disponible

### Identification

```powershell
$adb = "$android\SDK\platform-tools\adb.exe"
& $adb devices -l
```

Noter serial, modele, version Android, resolution, type phone/tablette, orientation et UTC. Ne jamais marquer une preuve physique sans ces donnees.

### Installation

```powershell
& $adb -s '<SERIAL>' install -r -t $apk
```

Si la signature installee est incompatible, ne pas desinstaller silencieusement. Marquer `BLOCKED_SIGNATURE_MISMATCH` et demander l'autorisation d'une installation propre.

### Lancement

```powershell
& $adb -s '<SERIAL>' logcat -c
& $adb -s '<SERIAL>' shell am start -W -n 'com.bkdhoneystudio.beekingdom/com.unity3d.player.UnityPlayerGameActivity'
```

Verifier:

1. lancement sans crash/ecran noir;
2. splash `SandboxPlayground` visible;
3. selecteur Development visible;
4. bouton WorldMap non muet;
5. ouverture de `WorldMapMmoFullscreenFoundation`;
6. carte plein ecran et HUD fixe;
7. pan un doigt seulement;
8. pinch deux doigts seulement, doux et borne;
9. selection ruche/ressource;
10. vol aerien actif pendant pan/zoom;
11. aucune route terrestre utilisee;
12. aucune grille/couture visible;
13. aucun claim serveur/live/persistance.

### Orientations

- telephone: portrait obligatoire, puis rotation paysage de controle;
- tablette: paysage obligatoire, puis portrait de controle;
- tourner physiquement l'appareil; ne pas modifier durablement les reglages systeme via ADB;
- HUD/panneaux restent fixes et lisibles;
- aucun controle critique coupe ou masque.

Conserver captures/video/logcat. `PHYSICAL_DEVICE_PROOF = PASS` reste reserve a QA apres revue de ces artefacts.

## 10. Branche sans appareil

Si `adb devices -l` ne retourne aucun appareil autorise:

1. executer toutes les validations statiques de la section 8;
2. conserver le log `BuildPlayer` et son resultat;
3. lancer en Editor Play Mode la scene splash;
4. ouvrir WorldMap par le selecteur dev;
5. tester Game View `720x1280` portrait et `1920x1080` paysage;
6. verifier pan/zoom, HUD fixe, vols et absence de grille;
7. declarer `DEVICE_INSTALL = NOT_TESTED`;
8. declarer `PHYSICAL_DEVICE_PROOF = PENDING`.

Le Play Mode, le BuildReport et l'analyse du package ne remplacent jamais une preuve appareil.

## 11. Rollback et nettoyage non destructif

En cas d'echec avant build:

- ne rien supprimer;
- corriger le gate ou le prerequis;
- ne pas reutiliser un chemin deja occupe.

En cas d'echec pendant/apres build:

1. conserver le log;
2. si un APK partiel existe, le renommer avec suffixe `.FAILED_<UTC>.apk`, sans ecraser;
3. enregistrer hash/taille et cause;
4. ne pas supprimer les APK precedents;
5. restaurer les settings temporaires dans `finally`;
6. comparer `ProjectSettings` et `EditorBuildSettings` aux hashes avant build;
7. ne pas supprimer `Library`, `Temp`, caches Gradle ou SDK manuellement;
8. ne pas desinstaller l'application d'un appareil sans autorisation;
9. reserver `_002` pour une nouvelle tentative.

Si l'integration Wave3 echoue au smoke, le rollback runtime reste celui du handoff Unity: reactiver Step4C. L'APK en echec reste un artefact trace, pas un candidat valide.

## 12. Non-claims obligatoires

- APK Development local/demo;
- aucune signature release;
- aucun secret;
- aucun serveur live;
- aucune collecte/economie/persistance officielle;
- aucun monde immense/live livre;
- aucune preuve device sans appareil reel;
- aucune logique de route terrestre.
