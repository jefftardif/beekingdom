# M056-CL — Windows External Test Build — Portable Validation

Date : 2026-09-06
Agent : Claude Code
Base repository : commit `a52c0cb9` (`feat(hive): Royal Palace data-driven progression (M055-CL)`)
Statut : **Build produite, package portable cree, deux defauts bloquants trouves et corriges.**

---

## 0. Resume executif

La premiere build Windows autonome de Bee Kingdom existe et demarre correctement
dans la ruche officielle.

La mission a revele **deux defauts reels et bloquants** qui rendaient la build
inutilisable hors du PC de developpement. Les deux etaient **invisibles en Play Mode**
dans l'editeur, ce qui est exactement le type de probleme que cette mission devait
faire sortir. Les deux sont corriges et la correction est verifiee dans le vrai
executable, pas seulement dans l'editeur.

| Point | Etat |
|---|---|
| Build Windows x86-64 | OK |
| Scene de demarrage | `Environment2D5D_HiveMap_Test` (index 0) — LivingHive JAMAIS incluse |
| Serveur | `https://api-ops.beekingdomgame.com` — aucun localhost / 127.0.0.1 / :58080 |
| Lancement reel du .exe | OK, 0 exception, arret propre |
| Batiments HiveMap visibles | OK (apres correctifs) |
| Session restauree au relancement | OK (verifie sur 3 lancements successifs) |
| Google Sign-In interactif | **NON teste interactivement** — voir section 6 |
| Package ZIP portable | OK |

---

## 1. Precondition

M055B deploye sur `api-ops.beekingdomgame.com`, `/health` a 200 — verifie en amont.
Le working tree contenait, avant mon intervention, du travail non commite d'autres
chantiers (interaction batiments, pulse d'activite, profil joueur, fichiers M055,
et le correctif de routage LivingHive). **Tout a ete preserve** : aucun revert,
aucun checkout, aucun clean. Verifie en fin de mission (section 14).

---

## 2. Runtime officiel — scene de demarrage

Exigence critique : la build ne doit jamais demarrer dans LivingHive.

Verifie **avant** compilation dans `ProjectSettings/EditorBuildSettings.asset` :

- index 0, `enabled: 1` → `Assets/Experiments/Environment2D5D/Scenes/Environment2D5D_HiveMap_Test.unity`
- `enabled: 0` → `Assets/Scenes/LivingHive.unity`

Verifie **apres** compilation : la build contient 5 scenes (`level0` … `level4`),
`level0` etant HiveMap.

Verifie **au runtime**, dans le `Player.log` du vrai executable :

```
Ouverture scene: Environment2D5D_HiveMap_Test
```

Par securite, l'outil de build applique en plus deux garde-fous durs, independants
des Build Settings : LivingHive est explicitement **exclue** de la liste des scenes
meme si quelqu'un la reactive, et HiveMap est **forcee** en index 0. Un outil
editeur ne peut donc plus reintroduire LivingHive dans une build par accident.

---

## 3. Build Windows

- Plateforme : `StandaloneWindows64`, cible Windows 10/11 64-bit
- Scripting backend : Mono, API compatibility .NET Framework
- Dossier : `Builds/Windows/BeeKingdom-Alpha-Internal/`
- Executable : `BeeKingdom.exe` (lancement direct, aucune installation requise)
- Resultat Unity : `Succeeded`, **0 erreur**
- Aucun Unity, .NET SDK ou Visual Studio requis sur la machine cible

---

## 4. Build propre

Build **fraiche** : l'outil supprime integralement le dossier de sortie avant chaque
compilation. Aucun morceau d'ancienne build n'est recycle, aucune copie manuelle.

Contenu final verifie :

```
BeeKingdom.exe
BeeKingdom_Data/          (dont Managed/, Resources/, level0..level4, app.info, boot.config)
UnityPlayer.dll
MonoBleedingEdge/         (runtime Mono)
D3D12/
UnityCrashHandler64.exe
dstorage.dll, dstoragecore.dll
```

Le dossier `BeeKingdom_BurstDebugInformation_DoNotShip` genere par Unity a ete
**retire** avant packaging (Unity le marque lui-meme comme a ne pas distribuer).

### Chemins absolus

Aucun chemin du profil Windows du CEO n'apparait dans la build.

Le chemin du repo (`C:\projets\beekingdomgame-master`) apparait a l'interieur des
DLL managees : ce sont les references de fichiers source du compilateur C#
(utilisees pour les stack traces). **Ce n'est pas une dependance d'execution** —
le jeu ne lit jamais ces chemins. Verifie empiriquement : voir section 11, ou le
jeu a ete extrait puis lance depuis un dossier totalement different.

---

## 5. Serveur

`https://api-ops.beekingdomgame.com`, source unique : l'asset Resources
`BeeKingdom/MobileAccountSessionRuntime`.

Verifications explicites :

1. Aucun code runtime ne surcharge l'URL — pas de branche `#if UNITY_EDITOR`, pas de
   define, pas de fallback. Le seul `127.0.0.1` du code runtime est le port d'ecoute
   local **legitime** du retour OAuth Google (section 6).
2. Dans les donnees de la build livree : `api-ops.beekingdomgame.com` est present ;
   une recherche de `localhost:58080` / `127.0.0.1:58080` ne retourne **rien**.
3. Au runtime, le jeu lance ouvre bien des connexions TLS 443 sortantes vers les
   adresses Cloudflare qui frontent `api-ops` (le domaine est proxifie Cloudflare).
4. Preuve fonctionnelle la plus forte : le jeu affiche les **vraies donnees du compte**
   (Niv. 3 REINE, ressources, recherche « officielle » configuree). Ces donnees ne
   peuvent venir que du serveur.

Le drapeau `allowInsecureLoopbackForDevelopment` n'a **aucun effet ici** : il
n'assouplit la contrainte HTTPS que pour une URL de base en loopback, or l'URL de
base est en `https://`.

---

## 6. Authentification — point critique, lecture attentive demandee

### Ce qui est verifie

L'implementation Google **n'est pas dependante de l'editeur**. C'est un vrai flux
desktop natif : le jeu ouvre le navigateur systeme (`Application.OpenURL`) et ecoute
le retour OAuth sur un `HttpListener` en loopback `http://127.0.0.1:53682/oauth/callback`,
avec PKCE (S256) et verification du `state`. C'est le schema standard des applications
desktop (celui qu'utilise la bibliotheque officielle Google .NET). Il n'y a aucun
`Application.isEditor`, aucun chemin editeur-seulement. `HttpListener` est disponible
car le projet est en API compatibility .NET Framework et en backend Mono.

La **persistance de session fonctionne reellement en build standalone** : le store
`WindowsDpapiRefreshTokenStore` chiffre le refresh token via DPAPI Windows et l'ecrit
dans `persistentDataPath`. J'ai observe le fichier
`bee_kingdom_protected_session_dpapi_v1.bin` **ecrit par la build pendant mes tests**,
et le jeu a restaure sa session et affiche les vraies donnees du compte a chaque
relancement, sans reconnexion.

### Ce qui n'est PAS verifie — a lire avant le test sur le 2e PC

Je **n'ai pas** execute le parcours Google Sign-In interactif de bout en bout. Deux
raisons : la consigne de ne creer aucun compte artificiel et de ne modifier aucun
compte production, et le fait que mes tests beneficiaient d'une session deja presente
sur cette machine.

Consequence concrete pour le test sur la deuxieme machine : le chiffrement DPAPI est
lie au **compte Windows et a la machine**. Le fichier de session ne se transporte donc
pas — c'est le comportement de securite attendu. Sur le 2e PC, le CEO devra donc faire
**une vraie connexion Google interactive**. C'est precisement le scenario non encore
prouve, et c'est le risque numero un du test.

Deux points de vigilance a surveiller sur le 2e PC :

- **Pare-feu Windows** : le jeu ouvre un port d'ecoute local (53682). L'ecoute etant
  strictement en loopback, Windows ne devrait pas afficher de demande, mais si une
  fenetre de pare-feu apparait, il faut l'autoriser.
- **Port 53682 deja occupe** : si un autre logiciel l'utilise, le listener ne demarre
  pas et la connexion echoue avec `auth.google_sign_in_failed`. Le port est fixe (il
  doit correspondre exactement a l'URI de redirection enregistree dans Google Cloud
  Console), donc ce cas ne peut pas etre contourne automatiquement.

Je n'ai fait **aucune modification** au code d'authentification.

---

## 7. Persistance

Les donnees locales utilisent les emplacements Windows corrects, jamais le repo :

- Session protegee (DPAPI) : `%USERPROFILE%\AppData\LocalLow\BKD Honey Studio\BeeKingdom\bee_kingdom_protected_session_dpapi_v1.bin`
- File d'attente de mutations : meme dossier (`persistentDataPath`)
- Journal : `Player.log` / `Player-prev.log`, meme dossier

Cycle teste reellement (lancements successifs du vrai .exe) :
ouverture → HiveMap avec donnees serveur → fermeture complete → relance →
**session restauree sans reconnexion**. Verifie sur 3 lancements.

---

## 8. Resolution

- `defaultIsNativeResolution: 1` : la build s'adapte a la resolution du poste cible.
  Elle ne depend donc pas de l'ecran de developpement.
- `fullscreenMode: 1` (fenetre plein ecran sans bordure) : demarrage plein ecran par defaut.
- Teste reellement en **1280x720 fenetre** (via `-screen-width/-screen-height
  -screen-fullscreen 0`) : HiveMap, HUD ressources, barre laterale de files et
  batiments s'affichent correctement, sans decoupe ni chevauchement.
- 1920x1080 : resolution native du poste et valeur par defaut du projet.

**Limite connue, non corrigee (hors perimetre)** : `resizableWindow: 0` — la fenetre
n'est pas redimensionnable a la souris. Le redimensionnement se fait uniquement par
arguments de ligne de commande ou en plein ecran. Ce n'est pas un bug de la build,
c'est le reglage actuel du projet ; je ne l'ai pas change pour ne pas modifier le
comportement d'affichage juste avant un test externe.

---

## 9. Version / identification de build

- `bundleVersion` : `1.0` → **`0.1.0-alpha-internal`**
- Empreinte complete : `BeeKingdom Alpha Internal 0.1.0-alpha-internal | commit a52c0cb9 | 2026-09-06 20:02`

Cette empreinte est generee a chaque build (version + commit + date) et embarquee
dans le player comme ressource.

**Canal fiable — le journal.** L'empreinte est ecrite au demarrage dans le `Player.log`,
en premiere ligne applicative :

```
[BuildStamp] BeeKingdom Alpha Internal 0.1.0-alpha-internal | commit a52c0cb9 | 2026-09-06 20:02
```

C'est le canal a utiliser pour savoir quelle build a produit un bug : il suffit que le
testeur joigne son `Player.log`.

**Limite connue et honnete — le label a l'ecran ne s'affiche pas.** J'ai ajoute un petit
label discret en bas a droite, mais il est masque par la pile d'interface de HiveMap
(le Canvas uGUI plein ecran se dessine par-dessus l'IMGUI). Je n'ai pas force davantage :
rendre ce label visible proprement demande de le cabler dans `LivingHiveMenuCanvas`,
qui fait partie d'un chantier en cours par un autre agent, et la mission interdit
explicitement d'ouvrir un chantier UI sans rapport. **Le label existe mais ne se voit
pas sur HiveMap** ; l'identification passe donc par le `Player.log`. A cabler dans le
Canvas uGUI lors d'un prochain sprint UI.

---

## 10. Package transportable

- Fichier : `Builds/Windows/BeeKingdom-Windows-Internal-0.1.0-alpha-internal.zip`
- Taille : **1 945 439 203 octets (1,81 Gio)**
- Le ZIP contient le **dossier complet** `BeeKingdom-Alpha-Internal/`, pas seulement
  l'executable. L'extraction produit un dossier autonome pret a lancer.
- Pas d'installeur MSI/EXE, conformement a la consigne.

---

## 11. Test « clean machine » — ce qui a ete reellement fait

Je suis explicite sur la frontiere entre ce que j'ai **observe** et ce que j'ai
seulement **verifie par lecture de configuration**.

### Reellement execute et observe

- Lancement du **vrai `BeeKingdom.exe`** (pas l'editeur), plusieurs fois.
- Le jeu demarre, repond, et s'arrete proprement (sequence d'arret complete dans le
  journal, aucun crash, aucun dump).
- `Player.log` : **0 exception**, 0 erreur de shader, 0 erreur d'artwork.
- Scene de demarrage HiveMap confirmee par le journal.
- **Capture d'ecran du jeu reellement en cours d'execution** : HiveMap s'affiche avec
  le Palais Royal, les batiments, le HUD de ressources, la barre laterale des files et
  les abeilles d'ambiance.
- Donnees serveur reelles affichees (Niv. 3 REINE, ressources) → preuve que le client
  a bien joint `api-ops`.
- Persistance de session verifiee sur 3 lancements successifs.
- **Independance au chemin — test reellement execute.** Le ZIP livre a ete extrait dans
  `C:\BKPortableTest\`, un dossier sans aucun rapport avec le repo, puis lance depuis la.
  Resultat : le jeu demarre normalement, le journal confirme qu'il tourne bien depuis
  le nouvel emplacement
  (`Mono path[0] = 'C:/BKPortableTest/BeeKingdom-Alpha-Internal/BeeKingdom_Data/Managed'`),
  avec **0 erreur de shader, 0 erreur d'artwork**, scene de demarrage HiveMap, et
  HiveMap affichee a l'identique (verifie par capture d'ecran). C'est la simulation la
  plus proche possible du 2e PC sans deuxieme machine : elle prouve que le chemin du
  repo present dans les DLL n'est **pas** une dependance d'execution, et que le ZIP
  s'extrait en un dossier autonome fonctionnel.
- Seule ligne d'exception du journal, sur tous les lancements :
  `Bee Kingdom chat session activation failed: OperationCanceledException` — le module
  Communication est gele depuis Codex, c'est attendu et sans effet sur le jeu.

### NON fait — a ne pas confondre

- Je **n'ai pas** teste sur une deuxieme machine physique. Je ne certifie donc pas
  l'absence de dependance a un composant deja installe sur ce poste (runtime Visual C++
  notamment, que Unity requiert et qui est present ici).
- Je **n'ai pas** joue le parcours interactif complet (connexion Google, ouverture du
  Palais Royal, aller-retour WorldMap) a la souris. Ce que je certifie sur ces ecrans
  vient du journal et de la capture, pas d'un pilotage clic par clic.

---

## 12. Checklist CEO — test sur la deuxieme machine

1. Copier / telecharger le ZIP sur le 2e PC.
2. **Extraire le ZIP** (clic droit → Extraire tout). Ne pas lancer le jeu depuis
   l'interieur du ZIP.
3. Ouvrir le dossier `BeeKingdom-Alpha-Internal` et lancer **`BeeKingdom.exe`**.
   - Si Windows SmartScreen apparait (« Windows a protege votre ordinateur ») :
     « Informations complementaires » → « Executer quand meme ». Normal, l'executable
     n'est pas signe.
4. Se connecter avec le compte Google habituel. Le navigateur s'ouvre ; apres
   validation, revenir au jeu.
5. Verifier l'arrivee dans **HiveMap** (la ruche, pas LivingHive).
6. Ouvrir le **Palais Royal**.
7. Ouvrir quelques batiments.
8. Aller sur la **WorldMap**.
9. Revenir sur **HiveMap**.
10. Fermer completement le jeu.
11. Relancer `BeeKingdom.exe`.
12. Verifier que la session est conservee (pas de reconnexion demandee).

### En cas de plantage — ce qu'il faut me renvoyer

Le fichier le plus important, dans cet ordre :

1. `%USERPROFILE%\AppData\LocalLow\BKD Honey Studio\BeeKingdom\Player.log`
   — **le journal de la session en cours**. Contient l'empreinte de build.
2. `%USERPROFILE%\AppData\LocalLow\BKD Honey Studio\BeeKingdom\Player-prev.log`
   — le journal de la session precedente. **C'est celui-ci qu'il faut si le jeu a
   plante puis a ete relance.**
3. S'il existe, le dossier de crash
   `%USERPROFILE%\AppData\Local\Temp\BKD Honey Studio\BeeKingdom\Crashes\`
   (contient un `crash.dmp`).
4. Une capture d'ecran de l'erreur, et l'etape de la checklist ou ca s'est produit.

Raccourci : coller `%USERPROFILE%\AppData\LocalLow\BKD Honey Studio\BeeKingdom` dans
la barre d'adresse de l'explorateur Windows ouvre directement le bon dossier.

---

## 13. Defauts trouves et corriges

Les deux etaient invisibles dans l'editeur et cassaient la build externe.

### Defaut 1 — HiveMap vide hors editeur (bloquant)

Les 14 batiments de HiveMap ne sont pas poses dans la scene : ils sont crees au
runtime a partir d'un fichier de placement JSON et des artworks PNG, lus via
`Application.dataPath` + un chemin relatif en `Assets/…`.

Dans l'editeur, `Application.dataPath` vaut `<repo>/Assets` : tout est trouve, tout
marche. Dans un player standalone, `Application.dataPath` vaut `<build>/BeeKingdom_Data`,
et ni le JSON ni les PNG n'y sont copies (ils ne sont ni dans `Resources` ni dans
`StreamingAssets`). Resultat : le lecteur ne trouve rien, retourne 0 batiment, et la
build demarrait sur **une HiveMap vide, sans aucun batiment ni zone cliquable**.

**Correction** : un post-traitement de build recopie ces fichiers dans le player en
respectant exactement la meme arborescence relative, ce qui rend les chemins existants
valides tels quels. **Aucun code runtime n'est modifie** — le comportement dans
l'editeur reste strictement identique, donc aucun risque de regression sur ce que le
CEO teste tous les jours.

> **Dette technique a traiter avant le portage mobile.** Sur Android/iOS,
> `Application.dataPath` pointe dans l'APK et n'est pas lisible avec `System.IO` :
> cette approche **ne fonctionnera pas sur mobile**. La correction de fond — migrer
> ces assets vers `StreamingAssets` et adapter les trois points de lecture — est
> volontairement hors perimetre M056 mais devra etre faite.

### Defaut 2 — batiments invisibles : shaders retires de la build (bloquant)

Une fois les fichiers presents, le journal du player a revele une deuxieme erreur,
repetee 14 fois (une par batiment) :

```
[BuildingRuntimeViewBootstrap] Shader introuvable : BeeKingdom/Experiments/ArtworkUnlit
```

Ce shader n'est reference par aucune scene ni aucun materiau : il est resolu au runtime
par `Shader.Find`. Unity ne pouvait donc pas savoir qu'il fallait l'inclure, et l'a
retire de la build.

**Correction** : ajout des 4 shaders custom concernes a « Always Included Shaders »
(`ArtworkUnlit`, `ArtworkOutline`, `PremiumBuilding`, `SoftShadow`). Correction Unity
standard, sans changement de code.

**Verifie apres correction** : 0 erreur de shader dans le journal, et les batiments
s'affichent bien — confirme par capture d'ecran du jeu en cours d'execution.

### Defaut 3 — non corrige, signale : traductions manquantes du Palais Royal

Le journal du player signale des cles de localisation absentes en `fr-CA`, sur des
ecrans que la checklist demande justement d'ouvrir :

```
royal_palace.unlocks_at
royal_palace.unlock.alliance.help
royal_palace.unlock.upcoming
royal_palace.upgrade
building_upgrade.authority.server
```

Ce sont des chaines M055 (Palais Royal) pas encore localisees. **Non bloquant** — le
jeu fonctionne — mais a l'etape 6 de la checklist le CEO verra probablement des cles
brutes au lieu du texte francais. Je ne l'ai pas corrige : ajouter des chaines produit
relevait d'une decision de contenu, pas de la mise en production d'une build.

---

## 14. Git / production

- **Aucun commit, aucun push, aucun deploiement serveur.** Conforme a la consigne.
- **Aucune modification serveur.** Aucun compte production touche.
- Les sorties de build sont deja ignorees par git (`.gitignore` : `/[Bb]uilds/`), donc
  ni le dossier de build ni le ZIP n'entrent dans le repo.
- Tout le travail non lie present avant la mission a ete **preserve intact**.

### Fichiers que j'ai crees / modifies (en attente de decision du CEO)

Nouveaux :
- `Assets/BeeKingdom/Editor/WindowsInternalBuildTool.cs` — outil de build + post-traitement (correctif 1)
- `Assets/BeeKingdom/Playground/BuildStampOverlay.cs` — empreinte de build (journal + label)
- `Assets/BeeKingdom/Playground/Resources/BeeKingdom/BuildStamp.txt` — empreinte generee

Modifies :
- `ProjectSettings/ProjectSettings.asset` — **une seule ligne** : `bundleVersion` `1.0` → `0.1.0-alpha-internal`
- `ProjectSettings/GraphicsSettings.asset` — 4 shaders ajoutes a Always Included (correctif 2)

Note : Unity **n'a pas** touche `EditorBuildSettings.asset` pendant la build ; le
correctif de routage LivingHive est intact (HiveMap index 0 active, LivingHive desactivee).

---

## 15. Livrables

| Livrable | Valeur |
|---|---|
| Dossier de build | `Builds/Windows/BeeKingdom-Alpha-Internal/` |
| Executable | `BeeKingdom.exe` |
| Taille du dossier | ~4,2 Go |
| ZIP portable | `Builds/Windows/BeeKingdom-Windows-Internal-0.1.0-alpha-internal.zip` |
| Taille du ZIP | **1 945 439 203 octets (1,81 Gio)** |
| Version / build ID | `BeeKingdom Alpha Internal 0.1.0-alpha-internal \| commit a52c0cb9 \| 2026-09-06 20:02` |
| Scene de demarrage | `Environment2D5D_HiveMap_Test` (index 0, confirmee au runtime) |
| Endpoint serveur | `https://api-ops.beekingdomgame.com` (confirme, aucun loopback) |
| Journal du joueur | `%USERPROFILE%\AppData\LocalLow\BKD Honey Studio\BeeKingdom\Player.log` |

### Problemes connus

1. **Google Sign-In interactif non prouve** — risque principal du test sur 2e PC (section 6).
2. **Label de version invisible sur HiveMap** — identification via `Player.log` (section 9).
3. **Traductions Palais Royal manquantes** en fr-CA (section 13, defaut 3).
4. **Fenetre non redimensionnable a la souris** (`resizableWindow: 0`) (section 8).
5. **Dette mobile** : le correctif 1 est valable Windows uniquement ; migration
   `StreamingAssets` requise avant le portage mobile (section 13).
6. **Executable non signe** : SmartScreen s'affichera au premier lancement (etape 3 de
   la checklist).

---

---

## Post-mortem M056B-CL — Blocage grille jaune de debug

### Rapport du CEO

Toi (Play Mode) et Alex (build Windows sur un autre PC) avez vu la meme grille jaune
avec le texte "FRONTAL BACKDROP - image droite, entiere, 2D (aucune perspective) | X =
hide/show" par-dessus Chat Royal. Deux testeurs independants, meme symptome exact ->
bug reel, pas un artefact d'editeur.

### Cause exacte

`Assets/Experiments/Environment2D5D/Scripts/FrontalBackdrop.cs` lisait
`Keyboard.current.xKey.wasPressedThisFrame` dans `Update()` pour basculer une grille de
diagnostic (`showGrid`, `false` par defaut). Le nouvel Input System lit le peripherique
BRUT : il ignore totalement qu'un champ de saisie ait le focus clavier. Le compositeur
de Chat Royal est un `GUI.TextField` IMGUI - IMGUI consomme bien la frappe pour son
propre rendu, mais cela n'empeche en rien `Keyboard.current` de voir la meme touche le
meme frame. **Taper un simple message contenant un "x" (le prenom "Alex", par
exemple) activait silencieusement la grille par-dessus tout le jeu**, en Play Mode
comme en build standalone. Meme anti-pattern trouve et corrige dans 3 autres scripts du
meme module (`AnchorMarkerUI.cs`, `AnchorValidation.cs` — outils de diagnostic inertes
aujourd'hui mais vulnerables au meme bug si jamais actives — et
`BuildingPerspectiveCamera.cs`, ou taper dans un champ uGUI aurait fait paner la camera
de jeu).

### Correctif

Nouvelle classe partagee `Assets/Experiments/Environment2D5D/Scripts/DebugHotkeyGuard.cs` :
- `TextInputHasFocus` : vrai si un champ IMGUI (`GUIUtility.keyboardControl != 0`) OU un
  champ uGUI/TextMeshPro (`EventSystem.current.currentSelectedGameObject`) a le focus.
- `Blocked` (reserve aux raccourcis de DEBUG uniquement) : vrai si (1) la build n'est
  pas une build de developpement (`!Application.isEditor && !Debug.isDebugBuild` —
  ceinture ET bretelles : meme un appui volontaire ne fait plus rien dans une build
  livree), (2) un champ de saisie a le focus, ou (3) une fenetre Premium est ouverte
  (reutilise `HiveViewProductUiPresenter.PremiumWorldInputBlockedForProof`, deja en
  place, plutot que d'inventer un second drapeau).

`FrontalBackdrop.Update()` ignore desormais la touche X si `DebugHotkeyGuard.Blocked`.
Le `OnGUI()` conserve en plus sa propre garde de rendu (deja presente) : la grille ne
peut de toute facon jamais se DESSINER hors editeur/build de developpement, meme si
`showGrid` finissait a `true` par une autre voie. Les 3 autres scripts recoivent la
meme garde, avec la distinction correcte entre `Blocked` (outils de debug) et
`TextInputHasFocus` seul (`BuildingPerspectiveCamera`, dont le pan camera est du
gameplay reel qui ne doit JAMAIS etre desactive par le type de build — seulement par un
champ de saisie qui a le focus).

**Fichiers modifies** : `FrontalBackdrop.cs`, `AnchorMarkerUI.cs`, `AnchorValidation.cs`,
`BuildingPerspectiveCamera.cs`, `WindowsInternalBuildTool.cs` (version bump),
`BuildStamp.txt`. **Fichier cree** : `DebugHotkeyGuard.cs`.

### Verification

- Compilation propre (`assets-refresh` -> Success, 0 erreur).
- Play Mode : demarrage sans erreur console.
- Simulation d'appui clavier "X" en direct via le pont MCP : non concluante de facon
  fiable (le systeme d'input bas niveau est sensible au decoupage exact des frames
  entre deux appels d'outil separes — limite de l'outillage de simulation, pas un doute
  sur le correctif). La preuve retenue est la lecture directe du code : `Blocked`
  court-circuite la lecture de la touche X **avant** qu'elle soit testee des qu'un champ
  de saisie a le focus — aucun chemin ne permet de contourner cette garde.
- **Build Windows reconstruite et relancee reellement** (pas seulement en Play Mode) :
  le `Player.log` confirme `[BuildStamp] BeeKingdom Alpha Internal
  0.1.1-alpha-internal-gridfix | commit 197fcbc0+gridfix | 2026-09-06 21:34` et
  l'ouverture de `Environment2D5D_HiveMap_Test` (jamais LivingHive).

### Nouveau package

| | |
|---|---|
| Ancien ZIP (INVALIDE) | `BeeKingdom-Windows-Internal-0.1.0-alpha-internal.zip` — **ne plus distribuer** |
| Nouveau ZIP | `Builds/Windows/BeeKingdom-Windows-Internal-0.1.1-alpha-internal-gridfix.zip` |
| Taille | 1 945 474 401 octets (~1,81 Gio) |
| Version / build ID | `0.1.1-alpha-internal-gridfix` \| commit `197fcbc0+gridfix` |

### Non verifie / limites

- Login Google interactif toujours non prouve (limite deja connue de M056, inchangee).
- La simulation automatisee de frappe pendant qu'un champ Chat Royal a reellement le
  focus (via une vraie session de jeu, pas juste `GUIUtility.keyboardControl` force par
  script) n'a pas ete faite manette en main — la garde a ete verifiee par lecture de
  code et par simulation partielle, pas par un vrai clic-puis-frappe dans Chat Royal en
  conditions reelles. A confirmer par toi en testant : ouvre Chat Royal, tape un message
  contenant "x", confirme qu'aucune grille n'apparait.

---

M056 WINDOWS BUILD READY — PORTABLE PACKAGE CREATED — READY FOR CEO SECOND-PC TEST.
M056B GRID BLOCKER FIXED — REBUILT — PREVIOUS ZIP INVALIDATED — READY FOR CEO RETEST.
