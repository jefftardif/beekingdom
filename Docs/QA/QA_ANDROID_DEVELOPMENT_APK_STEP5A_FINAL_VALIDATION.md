# QA Android Development APK Step5A Final Validation

## Statut

- Date QA : `2026-07-14`
- Portee : APK Android Development local Step5A, validation read-only hors appareil
- APK : `C:\projets\beekingdomgame-master\Builds\Artifacts\WorldMapStep5AAndroidDevelopment\BeeKingdom_WorldMapStep5A_Development.apk`
- Rapport produit : `C:\projets\beekingdom\prompts_codex\rapports\BuilderA_WorldMapStep5AAndroidDevelopmentApk_Report.md`
- Rapport correctif navigation : `C:\projets\beekingdom\prompts_codex\rapports\BuilderA_HiveToCanonicalWorldMapNavigationFix_Report.md`
- Gate Step5A amont : `QA_DEMO_100_WORLD_MAP_WAVE3_SHARED_TRANSFORM_STEP5A = PASS`
- Verdict : `PASS_WITH_RESERVES`

Le paquet Android Development est integre, decodable, signe debug et coherent avec les sources Step5A acceptees. Le correctif Ruche vers carte mondiale canonique est present dans le code IL2CPP du build. Aucun appareil ni emulateur ADB n'etant connecte, le smoke Android reel reste obligatoire et non ferme.

## 1. Integrite APK

| Controle | Resultat | Preuve QA |
|---|---|---|
| Fichier present et decodable | PASS | Archive ZIP ouverte; `666` entrees; manifeste, donnees Unity et bibliotheques natives presents. |
| SHA-256 | PASS | `A313A873BA160943B236F22A2CC8B0E110D66700AC67AB9135D355132BF12A70`, identique a la valeur attendue. |
| Taille | PASS | `257621234` octets. |
| Date fichier | PASS | UTC `2026-07-14T20:41:13.4572018Z`; locale `2026-07-14T16:41:13.4572018-04:00`. |
| Signature | PASS | `apksigner` code `0`; v1 et v2 valides; un signataire `C=US, O=Android, CN=Android Debug`, RSA 2048. |
| Build debug/development | PASS | Manifeste `application-debuggable`; `BuildOptions.Development | BuildOptions.AllowDebugging`; player connection debug present. |

Observation documentaire non bloquante : le log Unity affiche `2530138002 bytes` depuis `BuildReport.summary.totalSize`. Cette valeur represente le total rapporte par Unity et non la longueur du fichier APK. La taille filesystem recalculee est `257621234` octets et correspond au rapport Builder-A.

## 2. Identite et cible Android

| Propriete | Valeur observee | Resultat |
|---|---|---|
| ApplicationId | `com.bkdhoneystudio.beekingdom` | PASS |
| Version | `1.0` / code `1` | PASS |
| minSdk | `23` | PASS |
| targetSdk | `36` | PASS |
| compileSdk | `36` | PASS |
| Activite de lancement | `com.unity3d.player.UnityPlayerGameActivity` | PASS |
| Backend | IL2CPP (`libil2cpp.so` et `global-metadata.dat`) | PASS |
| ABI | `arm64-v8a` et `armeabi-v7a`; aucun x86 embarque | PASS |

Les deux ABI contiennent chacune `libil2cpp.so`. Aucun claim production, staging ou serveur live n'est deduit de la permission Android `INTERNET` presente dans le manifeste.

## 3. Scenes et entree produit

- Le build source declare, dans cet ordre :
  1. `Assets/Scenes/SandboxPlayground.unity`;
  2. `Assets/Scenes/WorldMapMmoFullscreenFoundation.unity`.
- L'APK contient exactement les niveaux Unity `level0` et `level1`, sans `level2`.
- `globalgamemanagers` contient les deux chemins et place `SandboxPlayground` avant `WorldMapMmoFullscreenFoundation`.
- Le log Unity charge les deux scenes pendant le build et termine par `Build Finished, Result: Success` puis `Exiting batchmode successfully now!`.

Resultat : scene d'entree Sandbox et carte canonique incluse, `PASS`.

## 4. Correctif Ruche vers carte canonique

Verification croisee source, artefact genere et smoke runtime :

- les deux boutons Monde de `HiveViewProductUiPresenter` appellent le helper unique `OpenCanonicalWorldMap`;
- le helper appelle `SceneManager.LoadScene("WorldMapMmoFullscreenFoundation")`;
- le code C++ IL2CPP conserve les deux sites d'appel, la methode generee et l'appel `SceneManager_LoadScene`;
- le source du correctif date de `2026-07-14T20:01:11Z`, le smoke de `20:10:59Z` et l'APK final de `20:41:13Z`;
- le smoke runtime a invoque ce helper, observe la scene active `WorldMapMmoFullscreenFoundation` et trouve le bootstrap Step5A, avec exit Unity `0`;
- le hash du recu smoke est conforme : `A1DEC855FBF3D4574644E2D7BE4DB2D7AAD694DD6BF6D6CA174161B80CCE5516`.

La presence dans l'APK est donc demontree sans se limiter au rapport producteur. Le clic/tap sur appareil reste couvert par la reserve device, mais ne remet pas en cause l'inclusion du correctif.

## 5. Renderer Wave3 et absence de fallback terrain

- Hash source renderer recalcule : `8281EE0294AF44F24F8EBDB454A535C79F33DD21F4706DCE45CEA5FE04A5E63E`, identique a la source Step5A acceptee.
- Ressources runtime : un seul jeu `UIB_ContinuousMaster5x5_v1`, `25/25` PNG.
- Le provider runtime charge strictement `5 x 5`, exige les textures `516 x 516`, applique `TextureWrapMode.Clamp` et echoue ferme si une tuile manque.
- Aucun fichier ou chemin `15x15` / `ContinuousMaster15` n'est present dans les ressources runtime ou les noms d'entrees APK controles.
- Aucun fallback terrain UV statique ou modulo n'est joignable : `canonical_static_uv_fallback_reachable:false`, `canonical_modulo_tile_fallback_reachable:false`, `wave3_no_modulo_repeat:true`.
- Les usages `Mathf.Repeat` encore presents concernent uniquement l'animation/progression des vols; ils ne pilotent ni l'adressage des tuiles ni le pan/zoom du terrain.

Resultat : master `15x15` non integre et aucun fallback Repeat/modulo terrain, `PASS`.

## 6. Build Unity et etat local

- SHA-256 log Unity : `DEC9D2F0BBBDACAB75AA7C1ECEA8D1E51DD0D9F4E46B45892128BD681A338F23`, conforme au rapport Builder-A.
- Methode : `WorldMapMmoAndroidBuild.BuildWorldMapStep5ADevelopmentApk`.
- Resultat Unity : `Success`, sortie batchmode reussie, `0` erreur compilateur C# et `0` marqueur d'echec de build.
- Processus Unity actif au controle QA : `0`.
- `Temp\UnityLockfile` : absent.
- `Library\UnityLockfile` : absent.
- Aucun lancement Unity ni changement produit effectue par QA.

## 7. Smoke Android

`adb devices -l` retourne une liste vide. Aucun appareil physique ni emulateur autorise n'est disponible.

Non executes et non revendiques :

- installation de l'APK;
- lancement Android;
- tap Ruche vers Monde;
- pan/zoom/deplacement sur appareil;
- HUD fixe, absence de couture/grille et absence de crash sur GPU Android;
- collecte `logcat`.

Cette reserve est non bloquante pour l'integrite du paquet Development, mais elle interdit un verdict `PASS` complet et toute affirmation de validation device.

## Decision finale

Le paquet APK final correspond au hash attendu, est signe debug, contient les cibles Android et scenes annoncees, et embarque effectivement le correctif Ruche vers carte canonique. La source Wave3 acceptee est preservee, le pilote reste borne au `5x5`, et aucun master `15x15` ni fallback terrain Repeat/modulo n'est integre.

Le gate APK local est accepte avec une seule reserve : smoke reel Android a effectuer des qu'un appareil ou emulateur ADB est disponible. Ce verdict n'autorise ni publication, ni staging, ni production, ni service officiel/live.

`APK_PACKAGE_INTEGRITY=PASS`

`HIVE_TO_CANONICAL_WORLD_MAP_INCLUDED=PASS`

`ANDROID_DEVICE_SMOKE=PENDING`

`QA_ANDROID_DEVELOPMENT_APK=PASS_WITH_RESERVES`

`LOCAL_DEVELOPMENT_ONLY_NO_PRODUCTION_STAGING_LIVE_CLAIM=YES`
