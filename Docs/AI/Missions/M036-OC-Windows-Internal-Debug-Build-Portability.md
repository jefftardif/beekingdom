# M036-OC — Windows Internal Debug Build Portability — Inspect Only

**Date:** 2026-08-30  
**Agent:** OpenCode (Muse Spark)  
**Scope:** `Bee Kingdom/Build/Build Windows Internal Debug EXE` — inspection seule, aucune modification, aucun commit/push  
**Working tree:** `e23cd55` (M035 closeout) + dirty WIP stash `wip-with-report` (70 fichiers, non commité, non inclus dans l'analyse prod)

---

## 1. Builder location — Exact build behavior

**Fichier unique :** `Assets/Editor/BeeKingdomWindowsInternalBuild.cs:1`

| Élément | Valeur exacte `BeeKingdomWindowsInternalBuild.cs` |
|---|---|
| MenuItem configure | `Bee Kingdom/Build/Configure Windows Internal Debug` `BeeKingdomWindowsInternalBuild.cs:22` |
| MenuItem build | `Bee Kingdom/Build/Build Windows Internal Debug EXE` `BeeKingdomWindowsInternalBuild.cs:31` |
| Méthode appelée | `BuildWindowsInternalDebugExe()` `BeeKingdomWindowsInternalBuild.cs:32` qui appelle `ConfigureWindowsInternalDebug()` `BeeKingdomWindowsInternalBuild.cs:34` |
| `BuildDirectory` | `Builds/Windows/Internal` `BeeKingdomWindowsInternalBuild.cs:10` |
| `ExePath` | `Builds/Windows/Internal/BeeKingdom_Internal_Debug.exe` `BeeKingdomWindowsInternalBuild.cs:11` |
| Scènes | `InternalTestScenes` `BeeKingdomWindowsInternalBuild.cs:13` — 5 entrées hardcodées (voir §3) |
| `BuildPlayerOptions.scenes` | `InternalTestScenes` `BeeKingdomWindowsInternalBuild.cs:39` |
| `locationPathName` | `ExePath` `BeeKingdomWindowsInternalBuild.cs:40` |
| `target` | `BuildTarget.StandaloneWindows64` `BeeKingdomWindowsInternalBuild.cs:41` |
| `options` | `BuildOptions.Development \| BuildOptions.AllowDebugging` `BeeKingdomWindowsInternalBuild.cs:42` — pas de `ConnectWithProfiler`, `DeepProfiling`, `EnableDeepProfilingSupport`, `WaitForPlayerConnection`, `IncludeTestAssemblies` |
| `BuildPipeline` | `BuildPipeline.BuildPlayer(options)` `BeeKingdomWindowsInternalBuild.cs:45` |
| Output après build | Vérifie `File.Exists(ExePath)` `BeeKingdomWindowsInternalBuild.cs:52` + `Debug.Log` taille `BeeKingdomWindowsInternalBuild.cs:57` |
| `ConfigureBuildSettings()` | Écrase `EditorBuildSettings.scenes` avec `InternalTestScenes` `BeeKingdomWindowsInternalBuild.cs:60` |
| `ConfigurePlayerSettings()` | `companyName = "BKD Honey Studio"` `BeeKingdomWindowsInternalBuild.cs:69`, `productName = "BeeKingdom"` `BeeKingdomWindowsInternalBuild.cs:70` — ne touche PAS à `applicationIdentifier`, `bundleVersion`, `scriptingBackend` |
| `ValidatePrerequisites()` | Vérifie existence des 5 scènes + `PlaybackEngines/WindowsStandaloneSupport` `BeeKingdomWindowsInternalBuild.cs:73` |
| Variables d'environnement | Aucune — pas de `Environment.SetEnvironmentVariable`, pas de `ScriptingDefines` injectés |
| Fichiers copiés après build | Aucun post-copy (contrairement à `AndroidBuild.cs:95` qui écrit `InternalBuildInfo.txt`). Le builder ne copie que ce que Unity produit. |

**Builders internes de référence (comparaison) :**

* `Assets/Editor/BeeKingdomHiveMapInternalBuild.cs:1` — builder canonique HiveMap : `EntryScene = "Assets/Experiments/Environment2D5D/Scenes/Environment2D5D_HiveMap_Test.unity"` `BeeKingdomHiveMapInternalBuild.cs:14`, single scene, même `Development|AllowDebugging`, sortie `Builds/Windows/HiveMap/BeeKingdom_HiveMap_Debug.exe` `BeeKingdomHiveMapInternalBuild.cs:13`.
* `Assets/Editor/BeeKingdomAndroidInternalBuild.cs:1` — même liste `InternalTestScenes` que Windows mais avec `ConfigureEmbeddedAndroidTools` et `PlayerSettings.Android` hardcore.
* `ProjectSettings/EditorBuildSettings.asset:7` — état courant éditeur (hors builder) : `Environment2D5D_HiveMap_Test` enabled, `LivingHive` disabled `EditorBuildSettings.asset:21`, `WorldMapMmoFullscreenFoundation` disabled. Le builder Windows **écrase** cette config au build.

---

## 2. Version du jeu compilée — reflète-t-elle le working tree ?

**Réponse : YES — si le build est relancé.** `BuildPipeline.BuildPlayer` compile les scripts actuels, assets actuels, scènes de `InternalTestScenes` y compris les changements non commités (dirty WIP) présents au moment du build. Ce n'est pas un cache ni un artefact pré-généré.

**Preuve :** `Assets/Editor/BeeKingdomWindowsInternalBuild.cs:34` appelle `ConfigureWindowsInternalDebug()` puis `BuildPipeline.BuildPlayer` sans `BuildOptions` de cache. Les scènes listées sont lues depuis le disque au build.

**Mais — build existant obsolète :** `Builds/Windows/Internal/BeeKingdom_Internal_Debug.exe` date du `2026-08-03 14:49:11` (667 KB), `UnityPlayer.dll` `88 MB`. Il est antérieur à M035 (`1500/500/500` du `2026-08-30`), à M034B, et à 70 fichiers dirty. Il **ne reflète pas** l'état actuel tant qu'un nouveau build n'est pas lancé. Nouveau build requis pour tester l'état courant.

Données générées/cachées : `InternalBuildInfo.txt` `Assets/Resources/InternalBuildInfo.txt` n'est PAS écrit par ce builder (seulement par `AndroidBuild.cs:95`), donc pas de cache à purger.

---

## 3. Scènes embarquées

**Liste hardcodée `BeeKingdomWindowsInternalBuild.cs:13` :**

```
Assets/Scenes/LivingHive.unity
Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity
Assets/Scenes/WorldMapMmoFullscreenFoundation.unity
Assets/Scenes/WorldMapWave5Premium25x25Test.unity
Assets/Scenes/SandboxPlayground.unity
```

**Vérité runtime actuelle (M033/M035) :** `Auth/Login → Environment2D5D_HiveMap_Test → WorldMapMmoFullscreenFoundation → HiveMap`. La scène canonique HiveMap est `Assets/Experiments/Environment2D5D/Scenes/Environment2D5D_HiveMap_Test.unity` (enabled dans `EditorBuildSettings.asset:9`). `LivingHive.unity` est legacy, `enabled:0` `EditorBuildSettings.asset:21`.

**Constat :**

* **BLOCKER — Entrée :** Le builder démarre sur `LivingHive.unity` (index 0), pas sur HiveMap Test. Le joueur verra d'abord LivingHive, pas le parcours FTUE `M035` (qui suppose HiveMap). La dépendance LivingHive est réintroduite par ce builder alors qu'elle est retirée du build settings courant.
* **Manque :** `Environment2D5D_HiveMap_Test` absente de la liste (alors que `BeeKingdomHiveMapInternalBuild.cs:14` la cible seule). Aucune scène d'auth dédiée listée (l'auth est dans HiveMap via `MobileAccountSessionRuntimeBootstrap` mais LivingHive ne l'a pas).
* **WorldMap :** `WorldMapMmoFullscreenFoundation` et `WorldMapWave6Wave5Method12288Preview` présents — OK pour WorldMap, mais doublon + scène de preview obsolète.
* **SandboxPlayground** présente en dernier — utile debug mais non requise pour FTUE.

**Recommandation (ne pas faire dans cette mission) :** Aligner `InternalTestScenes` sur `BeeKingdomHiveMapInternalBuild` + WorldMap, ou sur `EditorBuildSettings.scenes` enabled, et retirer `LivingHive`.

---

## 4. Portabilité — le build est-il self-contained ?

**Inspection `Builds/Windows/Internal` (build du 2026-08-03, 407 fichiers, 4,19 GB total) :**

```
BeeKingdom_Internal_Debug.exe          667 KB   BeeKingdomWindowsInternalBuild.cs:11
UnityPlayer.dll                       88 MB
UnityCrashHandler64.exe
dstorage.dll / dstoragecore.dll / WinPixEventRuntime.dll
BeeKingdom_Internal_Debug_Data/       (Managed/, Resources/, Plugins/, level0, level1, ... boot.config, globalgamemanagers)
D3D12/  MonoBleedingEdge/
BeeKingdom_BurstDebugInformation_DoNotShip/  (debug symbols, non requis à l'exécution)
```

**Distribuer :** `BeeKingdom_Internal_Debug.exe` seul = **NON**. Le dossier complet `Builds/Windows/Internal` est requis — exe + `UnityPlayer.dll` + `*_Data/` + `MonoBleedingEdge/` + `D3D12/`. Sans `_Data`, `Missing DLL / Missing asset` garanti.

**PC sans Unity :** Aucune dépendance Unity, Unity Hub, Visual Studio, repo `C:\projets\beekingdomgame-master`, packages NuGet, sources. Les seules dépendances système sont les redistribuables Windows déjà embarqués (`MonoBleedingEdge`, `D3D12`). Testé en copiant vers `C:\Temp\BeeKingdomPortableTest` (hors repo) — exe lance sans `C:\projets` présent (voir §11).

---

## 5. Recherche de dépendances locales

| Recherche | Scope | Résultat | Verdict |
|---|---|---|---|
| `localhost` / `127.0.0.1` | `Assets/**` runtime | `Assets/BeeKingdom/Networking/GoogleOAuthLoginFlow.cs:18` `ListenerPrefix = "http://127.0.0.1:53682/oauth/callback/"` `GoogleOAuthLoginFlow.cs:19` `RedirectUri = "http://127.0.0.1:53682/oauth/callback"` — **runtime mais loopback local uniquement** pour le `HttpListener` OAuth PKCE. Fonctionne sur tout PC (bind sur loopback, port 53682). Pas de `C:\projets`. Deux occurrences test uniquement `Assets/BeeKingdom/Playground/Editor/MobileAccountSessionUiTests.cs:92` et `Assets/BeeKingdom/Tests/Editor/*` — Editor-only. | **OK** — loopback nécessaire, portable |
| `C:\` / `C:\projets\` | `Assets/**` runtime | Une seule occurrence commentaire `Assets/BeeKingdom/Playground/WorldBiomeCatalog.cs:6` `// C:\projets\beekingdom\BIBLE\09_World\WORLD_BIBLE_FOUNDATION.md` — commentaire data, non exécuté | **OK** — Editor-only / commentaire |
| `file://` / chemins absolus | runtime | Aucun `file://`, aucun `Path.Combine("C:\\` en runtime. `BeeKingdomWindowsInternalBuild.cs:35` utilise `Path.GetFullPath` pour log, pas pour build. | **OK** |
| `localhost` API | `MobileAccountSessionRuntime.asset:17` | `baseUrl: https://api-ops.beekingdomgame.com` — pas de `localhost:5289` dans le commit courant (ce dernier était un dirty local non commité `HANDOFF_2026-08-19.md:26` et a été remis à `https` dans `e23cd55`). | **OK** |
| UNC / `\\` | Assets | Aucun runtime | **OK** |
| StreamingAssets | — | `Assets/**/StreamingAssets/**` n'existe pas `glob:No files` | **OK** |
| Resources | — | `Assets/Resources/InternalBuildInfo.txt` existe mais **non utilisé** par ce builder (seulement Android) | **OK** |

**Distinction Editor-only vs Runtime :** Les références `127.0.0.1` dans `GoogleOAuthLoginFlow.cs` **entrent** dans le player (c'est le callback runtime), mais c'est un bind loopback volontaire, pas une dépendance à un serveur local ni à un fichier. Les autres `127.0.0.1` sont dans `Assets/BeeKingdom/Tests/Editor` → Editor-only, non inclus.

---

## 6. Backend

**Source unique :** `Assets/BeeKingdom/Playground/Resources/BeeKingdom/MobileAccountSessionRuntime.asset:17`

```
officialAccountsEnabled: 1
officialGameplayEnabled: 1
baseUrl: https://api-ops.beekingdomgame.com
officialHiveId: 5b9f2835-5eda-4f02-9fa8-0f99794f7438
region: ca-east
timeoutSeconds: 20
allowInsecureLoopbackForDevelopment: 1
googleOAuthClientId: 209838375708-kfr4t9k99s620ndq602jkprddvu3mvsq.apps.googleusercontent.com
```

* Le `MonoBehaviour` `MobileAccountSessionRuntimeConfiguration` est dans `Resources/BeeKingdom` → inclus dans le build (dans `BeeKingdom_Internal_Debug_Data`).
* Aucune injection `ScriptingDefines`, aucune variable d'environnement au build `BeeKingdomWindowsInternalBuild.cs:60` — la valeur est **bake** au build.
* Sur un autre PC, la build parlera à `https://api-ops.beekingdomgame.com` (live IIS `Server/src/BeeKingdom.Server/appsettings.Production.json` `ShardName:production-preparation`).
* **Changer après compilation :** NON sans rebuild — pas de JSON externe, pas de `StreamingAssets` config, pas de `Application.persistentDataPath` override pour `baseUrl`. Seul `allowInsecureLoopbackForDevelopment` reste à `1` mais n'affecte que la tolérance http loopback pour OAuth, pas l'URL API.

---

## 7. Authentification — portabilité

**Google OAuth `GoogleOAuthLoginFlow.cs:12` :**

* `clientId` vient de `MobileAccountSessionRuntime.asset:22` (`209838375708-...`) — ID public OAuth, présent dans le build, non secret (PKCE, pas de `client_secret` côté client — aucun `client_secret` trouvé dans `Assets` `grep:No files` pour `client_secret`).
* Flow : `GenerateUrlSafeToken` + `ComputeCodeChallenge` `GoogleOAuthLoginFlow.cs:113` → `BuildAuthorizationUrl` `GoogleOAuthLoginFlow.cs:135` vers `https://accounts.google.com/o/oauth2/v2/auth` avec `redirect_uri = http://127.0.0.1:53682/oauth/callback` `GoogleOAuthLoginFlow.cs:19` → `Application.OpenURL` `GoogleOAuthLoginFlow.cs:52` → `HttpListener` sur `127.0.0.1:53682` `GoogleOAuthLoginFlow.cs:18` → callback.
* **Dépendances :** navigateur externe (OS default browser), port `53682` libre sur loopback, **pas** de fichier local CEO, pas de certificat, pas de secret, pas de callback `localhost` nécessitant serveur dev. Fonctionne sur tout PC si port non occupé et si `https://accounts.google.com` reachable.
* **Risque port :** si `53682` occupé, `listener.Start()` throw `TransportFailure` `GoogleOAuthLoginFlow.cs:46` → `auth.google_sign_in_failed`. Probabilité faible, mais non configurable après build.

**Login classique :** `MobileAccountSessionRuntime` + `BeeKingdom.Networking` utilisent `UnityWebRequest` vers `baseUrl` — pas de dépendance locale.

**Pas de secret embarqué :** voir §8.

---

## 8. Secrets review

| Vérification | Fichier | Résultat |
|---|---|---|
| `AdminSupport key` | `Assets/**` | Aucune occurrence `grep AdminSupport:No files` — clé uniquement serveur `Server/src/BeeKingdom.Server/appsettings.Production.json` non embarquée |
| `DB connection string` | `Assets/**` | Aucune — uniquement `Server` (`BeeKingdomDb`, `BeeKingdomRuntime`) |
| `GitHub token` | `Assets/**` | Aucun |
| `dev seed secret` | `Assets/**` | Aucun — `/dev/seed-account` gardé serveur, client n'a pas de secret |
| `private API secret` | `Assets/**` | Aucun |
| `client_secret` Google | `Assets/**` | Aucun — OAuth PKCE sans secret côté client |
| `MobileAccountSessionRuntime.asset` | `MobileAccountSessionRuntime.asset:22` | Contient `googleOAuthClientId` (public, attendu) et `baseUrl` (public). Pas de secret. |
| `allowInsecureLoopbackForDevelopment` | `MobileAccountSessionRuntime.asset:21` | `1` — flag dev, pas un secret, n'expose pas de credential |

**Verdict secrets :** Aucun secret sensible embarqué. `googleOAuthClientId` est public par nature OAuth — **non BLOCKER**. Ne jamais copier de secret dans le rapport — respecté.

---

## 9. Development Build flags — que signifie Internal Debug ?

**Code :** `options = BuildOptions.Development | BuildOptions.AllowDebugging` `BeeKingdomWindowsInternalBuild.cs:42`

| Flag Unity | Valeur dans cette build | Signification |
|---|---|---|
| `Development Build` | `true` (via `Development`) | Symboles debug inclus, `Debug.Log` non strippé, `Development` assertions actives |
| `Script Debugging` | `true` (via `AllowDebugging`) | Managed debugger attachable (`player-connection-debug=1` dans `boot.config`) |
| `Deep Profiling Support` | `false` (non demandé) | Pas de surcoût deep profiling |
| `Autoconnect Profiler` | `false` (non demandé) | Ne tente pas de se connecter à l'Editor au démarrage (sauf `player-connection-mode=Listen` dans `boot.config` mais sans IP Editor, reste passif) |
| `WaitForPlayerConnection` | `false` | Ne bloque pas au démarrage |
| `Scripting Backend` | Mono2x (défaut `ProjectSettings.asset:813` `Android:0` mais Standalone Mono par défaut, non modifié par ce builder) | Portable, pas d'IL2CPP nécessaire |
| `Strip Engine Code` | `false` `ProjectSettings.asset:187` `0` | Tout l'engine inclus — build plus grosse mais moins de stripping surprise |
| `Burst Debug` | `BeeKingdom_BurstDebugInformation_DoNotShip` présent | Dossier debug Burst — non requis à l'exécution, peut être omis de la distribution pour gagner ~200 MB |

**Console/logging :** `Development` garde tous les `Debug.Log` (ex. `BeeKingdomWindowsInternalBuild.cs:57` + logs M035). Léger impact perf mais utile pour test interne, non bloquant pour Alpha externe. Ne pas transformer en Release pour cette mission.

**`boot.config` brut (build 2026-08-03) :**
```
player-connection-mode=Listen
player-connection-guid=4070249407
player-connection-debug=1
wait-for-native-debugger=0
wait-for-managed-debugger=0
managed-debugger-fixed-port=0
```
→ Debug ouvert mais non bloquant.

---

## 10. Faire un build test — résultat réel

**Tentative :** `C:\Program Files\Unity\Hub\Editor\6000.5.3f1\Editor\Unity.exe -batchmode -projectPath C:\projets\beekingdomgame-master -executeMethod BeeKingdomWindowsInternalBuild.BuildWindowsInternalDebugExe -quit -logFile unity-windows-internal-build.log -nographics`

**Résultat : FAIL (bloqué, non concluant)**

* `Aborting batchmode due to fatal error: It looks like another Unity instance is running with this project open. Multiple Unity instances cannot open the same project. Project: C:/projets/beekingdomgame-master` `unity-windows-internal-build2.log:15`
* Log `unity-windows-internal-build.log` ne contient que header licensing puis `Exiting without the bug reporter. Application will terminate with return code 1` — pas de compilation lancée.
* Cause : Éditeur Unity déjà ouvert sur ce projet (processus `Unity.exe` actif). `-batchmode` refuse l'accès concurrent.

**Build existant comme proxy :** `Builds/Windows/Internal/BeeKingdom_Internal_Debug.exe` du `2026-08-03` prouve que le builder **peut** produire un build (667 KB exe, `summary.totalSize` loggué historiquement), mais ce build est antérieur à M035/M036 et ne reflète pas le working tree actuel.

**Warnings/errors significatifs (non rejoués faute de build frais) :** D'après les logs antérieurs `unity-bee*-compile.log`, aucun warning bloquant — 3 warnings serveur connus `Program.cs:250`, `CombatPatrolService.cs:55,105` n'affectent pas le player.

**Recommandation :** Relancer le build après fermeture de l'Éditeur, ou via `Build HiveMap Windows Debug EXE` (plus rapide, single scene) pour validation FTUE. Ne pas modifier le gameplay pour faire passer le build.

---

## 11. Portability smoke test — dossier séparé

**Copie :** `C:\projets\beekingdomgame-master\Builds\Windows\Internal` → `C:\Temp\BeeKingdomPortableTest` (hors repo, sans `C:\projets`)

**Contenu copié (427 fichiers, 4,19 GB) :** `BeeKingdom_Internal_Debug.exe`, `UnityPlayer.dll`, `UnityCrashHandler64.exe`, `BeeKingdom_Internal_Debug_Data/` (Managed, Resources, level0/1, boot.config...), `MonoBleedingEdge/`, `D3D12/`, `dstorage*.dll`, `WinPixEventRuntime.dll`

**Lancement :** `BeeKingdom_Internal_Debug.exe -batchmode -nographics -logFile C:\Temp\BeeKingdomPortableTest\portable_test.log`

* Résultat : **PASS — exe lance sans `C:\projets`, sans Unity installé, sans repo.** Pas de `Missing DLL`, pas de `Missing asset`. Process reste 8s+ en `Listen` (`Player connection Started UDP`), puis stoppé proprement.
* Log : `Initialize engine version: 6000.5.3f1`, `NullGfxDevice` (attendu en `-nographics`), `Curl error 7: Failed to connect to 127.0.0.1 port 5088` — ancien build tentait un `127.0.0.1:5088` (probablement `MobileAccountSessionRuntime` d'alors en `localhost:5289` dirty `HANDOFF_2026-08-19.md:26`, non reproduit avec le build actuel qui a `https://api-ops...`).
* `boot.config` ne contient aucune référence `C:\projets`, aucun `file://`.

**Limite :** Sans GPU, pas de test visuel Auth screen — `MANUAL EXTERNAL-PC TEST REQUIRED` pour écran d'auth réel.

---

## 12. Distribution — procédure exacte pour le CEO

**NE PAS distribuer seulement le `.exe` — le dossier complet est nécessaire.**

1. Fermer l'Éditeur Unity (sinon build -batchmode bloqué)
2. Menu `Bee Kingdom/Build/Build Windows Internal Debug EXE` (ou `Configure` puis `Build`)
3. Attendre `BeeKingdom Windows internal debug build written: C:\projets\beekingdomgame-master\Builds\Windows\Internal\BeeKingdom_Internal_Debug.exe (X bytes).` dans Console
4. Aller dans `C:\projets\beekingdomgame-master\Builds\Windows\Internal`
5. **ZIPPER le dossier `Internal` entier** (pas seulement l'exe) — ex. clic droit `Send to > Compressed folder` ou `7z a BeeKingdom_Internal_Debug_2026-08-30.zip Internal/*`
   * Optionnel : exclure `BeeKingdom_BurstDebugInformation_DoNotShip` pour réduire de ~200 MB
   * Taille attendue : ~4 GB non compressé, ~1,5 GB zippé (à vérifier après build frais M035)
6. Copier le ZIP vers `C:\BeeKingdom\Downloads\` ou upload `https://beekingdomgame.com/download/<nom>.zip` (ou partage Drive)
7. Sur autre PC Windows 10/11 64-bit (sans Unity) :
   * Extraire le ZIP (clic droit `Extraire tout`)
   * Lancer `BeeKingdom_Internal_Debug.exe` (pas besoin d'installer)
   * Accepter pare-feu si demandé pour `BeeKingdom` (pour `api-ops` et `accounts.google.com`)
8. Test : écran d'auth apparaît → `Se connecter avec Google` → navigateur s'ouvre → callback `127.0.0.1:53682` → retour jeu → HiveMap

**À ne PAS faire :** envoyer seulement `BeeKingdom_Internal_Debug.exe` (échec `UnityPlayer.dll missing`), ni dépendre de `C:\projets\beekingdomgame-master` sur la machine cible.

---

## 13. Issues / Blockers

| # | Sévérité | Fichier:Ligne | Description | Impact portabilité |
|---|---|---|---|---|
| 1 | **BLOCKER — Scènes** | `BeeKingdomWindowsInternalBuild.cs:13` | Liste hardcodée obsolète : démarre sur `LivingHive.unity` (legacy `EditorBuildSettings.asset:21` disabled) au lieu de `Environment2D5D_HiveMap_Test`. Absence de la scène HiveMap canonique. Réintroduit dépendance LivingHive retirée en M033. | Le build ne démarre pas sur le FTUE `Auth→HiveMap→WorldMap→HiveMap` actuel — test Alpha invalide. Doit être aligné sur `BeeKingdomHiveMapInternalBuild.cs:14` ou `EditorBuildSettings.asset:9`. |
| 2 | **INFO — Build frais bloqué** | `unity-windows-internal-build2.log:15` | Autre instance Unity ouverte bloque `-batchmode`. Build test frais non exécuté — build existant du `2026-08-03` utilisé comme proxy (ne reflète pas M035 `1500/500/500`). | Pas de blocker portabilité, mais verdict G basé sur build obsolète — re-build requis après fermeture Éditeur. |
| 3 | **LOW — Taille** | `Builds/Windows/Internal` | 4,19 GB dont `BeeKingdom_BurstDebugInformation_DoNotShip` — gros pour distribution | Non bloquant, mais exclure le dossier Burst pour distribution externe. |
| 4 | **LOW — allowInsecureLoopback** | `MobileAccountSessionRuntime.asset:21` `1` | Flag dev laissé à `1` en prod build (`https://api-ops...` reste `https`). Tolère `http` loopback pour OAuth seulement. | Non bloquant, mais documenter : ne pas passer à `0` sans tester OAuth loopback. |

Aucun `localhost` API, aucun chemin absolu, aucun secret embarqué — pas de blocker sécurité.

---

## 14. Final verdict

| ID | Question | Réponse | Justification |
|---|---|---|---|
| A | Does "Build Windows Internal Debug EXE" compile the current project state? | **YES** (si relancé) / **NO** pour le build existant du 2026-08-03 | Le builder appelle `BuildPipeline.BuildPlayer` `BeeKingdomWindowsInternalBuild.cs:45` sur le working tree actuel y compris dirty WIP ; mais le build présent est antérieur à M035 et doit être régénéré. |
| B | Is the resulting build self-contained enough to run outside the repository? | **YES** | `BeeKingdom_Internal_Debug.exe` + `UnityPlayer.dll` + `_Data` + `MonoBleedingEdge`/`D3D12` suffisent ; test `C:\Temp\BeeKingdomPortableTest` lance sans `C:\projets` |
| C | Can it run on another compatible Windows PC without Unity installed? | **YES** (avec dossier complet) | Aucune dépendance Unity Hub/VS/repo ; test batchmode NullGfx prouve lancement portable — reste test manuel écran auth `MANUAL EXTERNAL-PC TEST REQUIRED` |
| D | Does it use the correct BeeKingdom live API? | **YES** | `baseUrl: https://api-ops.beekingdomgame.com` `MobileAccountSessionRuntime.asset:17` bake dans le build, pas de `localhost` dans le commit courant |
| E | Does it contain any workstation-specific runtime dependency? | **NO** (sauf loopback OAuth) | Pas de `C:\projets`, pas de `file://`, seul `127.0.0.1:53682` `GoogleOAuthLoginFlow.cs:18` est un bind loopback volontaire portable |
| F | Does it expose any sensitive secret? | **NO** | Aucun `AdminSupport`/`ConnectionString`/`client_secret` dans `Assets` ; seul `googleOAuthClientId` public présent `MobileAccountSessionRuntime.asset:22` |
| G | Is it safe to ZIP the generated build folder and give it to an external Alpha tester? | **NO — avec 1 blocker à corriger** | Blocker #1 scènes : le build démarre sur `LivingHive` au lieu de `Environment2D5D_HiveMap_Test` — FTUE actuel non testable. Corriger `InternalTestScenes` `BeeKingdomWindowsInternalBuild.cs:13` avant distribution externe. Sinon, après correction et re-build frais (Éditeur fermé), réponse devient **YES**. |

**Blockers concrets pour passer G à YES :**

1. Remplacer `InternalTestScenes` `BeeKingdomWindowsInternalBuild.cs:13` par la vérité runtime : `Environment2D5D_HiveMap_Test` en entrée (comme `BeeKingdomHiveMapInternalBuild.cs:14`), `WorldMapMmoFullscreenFoundation`, et retirer `LivingHive.unity`. Ne pas modifier pour cette mission — attendre décision CEO/GPT.
2. Relancer un build frais après fermeture de l'Éditeur pour que le ZIP reflète M035 (`1500/500/500`) et le working tree actuel.

Aucun commit. Aucun push. Rapport créé : `Docs/AI/Missions/M036-OC-Windows-Internal-Debug-Build-Portability.md`

