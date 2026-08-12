# BeeKingdom - Android Internal Debug Build

Version : 1.0
Statut : procedure interne de test

## Objectif

Generer un APK Android Debug interne pour tester BeeKingdom sur tablette Android. Cette build n'est pas destinee au Google Play.

## Projet

Projet Unity : `C:\projets\beekingdomgame-master`

Version Unity attendue : `6000.5.3f1`

Scene de demarrage : `Assets/Scenes/LivingHive.unity`

## Prerequis

- Unity `6000.5.3f1`.
- Module Unity Android Build Support installe.
- SDK Android embarque Unity present dans `Editor\Data\PlaybackEngines\AndroidPlayer\SDK`.
- NDK Android embarque Unity present dans `Editor\Data\PlaybackEngines\AndroidPlayer\NDK`.
- OpenJDK embarque Unity present dans `Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK`.
- Aucun keystore Google Play requis : la build interne utilise le debug keystore Unity.

## Scenes incluses

L'outil de build interne configure les scenes suivantes, dans cet ordre :

1. `Assets/Scenes/LivingHive.unity`
2. `Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity`
3. `Assets/Scenes/WorldMapMmoFullscreenFoundation.unity`
4. `Assets/Scenes/WorldMapWave5Premium25x25Test.unity`
5. `Assets/Scenes/SandboxPlayground.unity`

`LivingHive` est volontairement en premiere position pour que l'APK demarre directement dans la ruche.

## Configuration Android appliquee

- Build target : Android.
- Type : APK Debug interne.
- Build options : Development Build + Script Debugging.
- Package : `com.bkdhoneystudio.beekingdom`.
- Scripting backend : Mono pour iteration debug interne.
- Architectures : ARMv7 + ARM64.
- Min SDK : Android API 26.
- Orientation : paysage uniquement (`LandscapeLeft` / `LandscapeRight`).
- Plein ecran Android active.
- Rendu hors safe area autorise.
- Input System actif via la configuration du projet.

## Procedure de generation

### Depuis Unity

1. Ouvrir le projet `C:\projets\beekingdomgame-master` avec Unity `6000.5.3f1`.
2. Menu : `Bee Kingdom > Build > Configure Android Internal Debug`.
3. Menu : `Bee Kingdom > Build > Build Android Internal Debug APK`.

### Depuis la ligne de commande

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.5.3f1\Editor\Unity.exe" `
  -batchmode `
  -quit `
  -projectPath "C:\projets\beekingdomgame-master" `
  -executeMethod BeeKingdomAndroidInternalBuild.BuildAndroidInternalDebugApk `
  -logFile "C:\projets\beekingdomgame-master\Builds\Android\Internal\android_build.log"
```

## Emplacement de l'APK

APK genere :

`C:\projets\beekingdomgame-master\Builds\Android\Internal\BeeKingdom_Internal_Debug.apk`

Log de build recommande :

`C:\projets\beekingdomgame-master\Builds\Android\Internal\android_build.log`

## Verification manuelle recommandee sur tablette

- Installer l'APK avec `adb install -r BeeKingdom_Internal_Debug.apk`.
- Verifier que l'application demarre directement sur `LivingHive`.
- Verifier la connexion.
- Verifier la navigation generale.
- Verifier la carte du monde depuis la ruche.
- Verifier construction et amelioration des batiments.
- Verifier collecte mondiale.
- Verifier patrouille de combat.
- Verifier Championnes.
- Verifier camera, zoom, scroll, clics et gestes tactiles.
- Verifier orientation paysage et resolution tablette.
- Surveiller les logs avec `adb logcat -s Unity`.

## Limitations connues

- Build interne uniquement, non signee pour publication Play Store.
- Aucun keystore de release configure.
- La verification tactile complete necessite une tablette Android physique.
- La verification des logs Android necessite l'installation et le lancement sur appareil.
- Les performances finales doivent etre mesurees sur appareil cible, pas uniquement dans l'Editeur.

## Recommandations Android suivantes

- Passer une build de qualification en IL2CPP ARM64 uniquement quand la build Debug interne est stable.
- Ajouter un profil qualite tablette Android dedie si les mesures appareil montrent un besoin.
- Verifier les tailles de textures et atlas UI apres les premiers tests sur tablette.
- Ajouter une passe de profiling Android : CPU, GPU, memoire, allocations et chargement de scenes.
- Preparer plus tard un keystore interne de distribution QA si plusieurs testeurs installent l'APK.
