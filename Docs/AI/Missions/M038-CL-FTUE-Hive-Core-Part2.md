# M038-CL — FTUE Hive Core Part 2 — Research + Training + Army Guidance

**Date:** 2026-08-31
**Agent:** Claude Code
**Git:** Aucun commit, aucun push (conforme à la mission). Rien du travail non commité d'OC (M037/M037B) n'a été touché.

---

## 1. Executive verdict

Code écrit pour les trois sections (Research, Training, Army-observation), branché sur le moteur FTUE existant (M037) et sur les vrais systèmes gameplay identifiés. **Non compilé et non exécuté** cette session : Unity MCP (`ai-game-developer`) est resté déconnecté du début à la fin (`ConnectionRefused`), donc aucune vérification EDITOR RUNTIME ni BUILT PLAYER n'a été possible. Voir section 16.

Deux blocages produit réels ont été trouvés et documentés avant d'écrire du code (section 14) : Research est éteint en production (`LivingHiveResearch:Enabled=false`), et la confirmation d'escouade Army l'est aussi (`CombatSquadReservation:Enabled=false`). Le code Research a quand même été écrit sur demande explicite de Jeff ; Army utilise une interaction locale réelle qui ne dépend pas du flag désactivé.

## 2. M037/M037B integration

Lu `Docs/AI/Missions/M037-OC-Alpha-FTUE-Tutorial-Engine.md` en entier (le "M037B" mentionné dans la mission est en réalité la section 19 "REAL RUNTIME CLOSEOUT" du même fichier — aucun fichier séparé n'existe). Moteur réutilisé tel quel, sans modification structurelle :

- `Assets/BeeKingdom/Tutorial/Runtime/FtueTutorialEngine.cs` (`FtueTutorialEngine`, non-Mono, testable) — **inchangé**.
- `Assets/BeeKingdom/Tutorial/Runtime/FtueTutorialTypes.cs` — **étendu** (nouvelles valeurs d'enum `FtueStepKind`/`FtueEventKind`, nouvelles constantes de registre), aucune valeur existante modifiée.
- `Assets/BeeKingdom/Tutorial/Runtime/FtueChapterDefinitions.cs` — **étendu** (nouveau chapitre ajouté à `All`), `BuildFtueHiveIntroPart1()` intact.
- `Assets/BeeKingdom/Tutorial/Runtime/TutorialTargetRegistry.cs` — **étendu** (nouveaux fallbacks IMGUI), mécanisme de résolution inchangé.
- `Assets/BeeKingdom/Tutorial/Runtime/TutorialGameplayNotifier.cs` — **étendu** (3 nouveaux événements), 3 événements existants intacts.
- `Assets/BeeKingdom/Tutorial/Runtime/FtueTutorialBootstrap.cs` — **étendu** (chaînage de chapitre, nouveaux abonnements, nouveaux cas du switch visuel), toute la logique Part1 existante intacte.
- Serveur (`/game/v1/hives/{hiveId}/tutorial`, `TutorialProgress`, `SaveTutorialProgressAsync`) : **non touché**, aucune migration SQL.

## 3. PART2 chapter definition

Nouveau chapitre séparé `FTUE_HIVE_CORE_PART2` (constante `FtueTutorialRegistry.ChapterFtueHiveCorePart2`), pas fusionné dans Part1. 15 étapes, IDs préfixés `ftue.core2.*` (aucune collision avec `ftue.intro.*`) :

`welcome` → `research_intro` → `research_open` → `research_select_highlight` → `research_started` → `research_timer_dialogue` → `training_intro` → `training_open` → `training_select_highlight` → `training_started` → `training_timer_dialogue` → `army_intro` → `army_open` → `army_interact` → `farewell` (complete).

**Chaînage PART1→PART2** : `FtueTutorialBootstrap.StartNextIncompleteChapter()` — au démarrage et après `OnChapterCompleted(Part1)`, démarre Part1 s'il n'est pas complet, sinon Part2 s'il ne l'est pas. Les deux chapitres restent des entrées indépendantes dans `FtueProgress.CompletedChapters` (persistance détaillée en section 13).

## 4. Research tutorial

Séquence réelle : dialogue → flèche vers le bâtiment `research_node` (le vrai nœud server-backed — voir section 5) → tap réel ouvre la vraie fenêtre Research → flèche vers le bouton de démarrage → **vrai** appel serveur → dialogue timer. Aucune étape ne simule quoi que ce soit ; le moteur n'avance que sur réception d'un événement réel (voir section 6).

## 5. First Research chosen

**`tempered_combs_i`** (180 miel + 120 pollen, aucun prérequis, catalogue réel `HiveOperationService.ResearchCatalog` dans `Server/src/BeeKingdom.HiveOperations/HiveOperationService.cs`). Choisi car c'est la moins chère des trois recherches de tier I sans prérequis (`foraging_routes_i` = 240+90, `pollen_sorting_i` = 200+150), et tient largement dans le bootstrap 1500/500/500.

**Nom du bâtiment vérifié** : `research_node` est le vrai point d'entrée server-backed (voir `Assets/Experiments/Environment2D5D/LivingHiveMenu/LivingHiveResearchHost.cs`). `academy_canopy` existe dans le catalogue mais est explicitement documenté comme non server-backed (`HiveMapUnsupportedBuildingBootstrap.cs`) — non utilisé ici, conformément à l'instruction "ne renomme rien sauf bug évident" : `research_node` était déjà le bon choix, aucun renommage nécessaire.

## 6. Research real-event wiring

`TutorialGameplayNotifier.NotifyResearchStarted(researchId)` (nouveau) appelé dans [HiveResearchPresentation.cs](Assets/BeeKingdom/Playground/HiveResearchPresentation.cs) → `HiveResearchPanelController.StartCoreAsync`, **immédiatement après** `snapshot = response.Snapshot;` (donc après succès serveur confirmé), avant la construction du modèle `Ready`. Wrapped en `try/catch` vide, même convention que les 2 call sites existants de M037.

## 7. Training tutorial

Séquence réelle : dialogue → flèche vers la Caserne (`guard_post` — même bâtiment/fenêtre que Part1, réutilise l'événement `WindowOpened("guard_post")` déjà câblé) → flèche vers le bouton de démarrage d'entraînement → **vrai** appel serveur → dialogue timer.

## 8. First troop/training chosen

**`darters`** (Lanceuses, 500 miel + 120 pollen, batch fixe de 8 — pas de paramètre de quantité côté serveur, voir catalogue `CombatRecruitmentService.Catalog`). C'est la famille la moins chère des trois (`wingrunners` = 680, `guardians` = 860), toutes trois débloquées dès le départ (aucun prérequis de recherche/bâtiment au-delà de `guard_post` niveau 1, déjà acquis par le bootstrap). La mission suggérait "Gardiennes" par préférence par défaut mais demandait explicitement de vérifier la vraie première unité disponible et son coût — `darters` est objectivement la plus économique et démontre le système tout aussi bien.

## 9. Training real-event wiring

`TutorialGameplayNotifier.NotifyTrainingStarted(family)` (nouveau) appelé dans [HiveOfficialDoctrineRecruitmentPresentation.cs](Assets/BeeKingdom/Playground/HiveOfficialDoctrineRecruitmentPresentation.cs) → `HiveDoctrineRecruitmentPanelController.StartCoreAsync`, immédiatement après `snapshot = response.Snapshot;` (succès serveur confirmé), avant la sauvegarde de l'outbox de retry.

## 10. Army tutorial

**Blocage produit trouvé avant d'écrire le code** : `CombatSquadReservation:Enabled=false` en production — "Confirmer l'escouade" échouerait réellement si on demandait cette action. Conformément à l'instruction de la mission ("ne force pas une action qui pourrait casser la disponibilité des troupes... interaction simple et réelle"), la séquence Army guide plutôt : dialogue → flèche vers le menu Armée (bouton bas d'écran réel, `LivingHiveArmyBridge`) → tap réel ouvre la vraie fenêtre Army (roster réel affiché) → **vraie** interaction locale (bouton `+` sur une famille de troupe dans le sélecteur d'escouade — mutation UI locale uniquement, aucun appel serveur, ne consomme aucune troupe, ne dépend pas du flag désactivé) → dialogue de transition vers PART3.

## 11. Tutorial targets

Ajoutés dans `FtueTutorialRegistry` : `TargetResearchNode` (`building.research_node`), `TargetWindowResearchNode`, `TargetResearchStartButton` (`ui.button.research_start`), `TargetWindowTraining` (réutilise `building.guard_post`), `TargetTrainingStartButton` (`ui.button.training_start`), `TargetArmyMenu` (`ui.menu.army`), `TargetWindowArmy`.

`building.research_node` se résout via le même scan générique déjà existant dans `TutorialTargetRegistry` (aucune modification nécessaire — le mécanisme scanne déjà `BuildingInteractionComponent.BuildingType`). Aucun `GameObject.Find` ajouté.

**Constat important non résolu par le code** : les panneaux Research/Training/Army sont dessinés en IMGUI (`GUILayout.Button`), pas en uGUI `RectTransform` — donc `TutorialTargetRegistry.RegisterUi(...)` ne peut littéralement pas s'y appliquer (aucun `RectTransform` n'existe pour ces boutons). M037 avait déjà ce même problème pour `ui.button.upgrade` et l'avait résolu par une position de repli codée en dur (fraction d'écran). J'ai suivi exactement le même précédent pour `ui.button.research_start`/`ui.button.training_start`/`ui.menu.army` plutôt que d'inventer un nouveau mécanisme.

## 12. Guided interaction

`RequireResearchStarted`/`RequireTrainingStarted`/`RequireArmyInteraction` (nouveaux `FtueStepKind`) bloquent l'input comme `RequireUpgradeStarted` (précédent M037). `HighlightActionButton` (nouveau) reproduit `HighlightUpgradeButton` sans bloquer, réutilisé pour research/training/army selon le principe DRY plutôt que dupliquer un kind par fonctionnalité.

## 13. Persistence/resume

**Gap trouvé et corrigé** : le modèle serveur `TutorialProgress` (`Server/src/BeeKingdom.HiveOperations/HiveOperationModels.cs`) ne trace qu'**un seul** chapitre + son étape de reprise — il n'a jamais eu besoin de savoir "quels chapitres sont terminés" puisque M037 n'avait qu'un chapitre. Sans correction, un rechargement après avoir terminé Part1 aurait fait échouer la détection "Part1 complet" côté client et aurait retenté Part1 en boucle.

**Correction choisie (côté client uniquement, aucun changement du contrat serveur/JSON, aucune migration SQL)** : `FtueTutorialBootstrap.ReconstructCompletedChaptersFromChapterKey` déduit l'achèvement à partir de l'ordre des chapitres — si la clé de chapitre chargée est Part2, Part1 est forcément terminé (Part2 ne peut être atteint que par le chaînage) ; si la clé chargée est un chapitre avec une étape de reprise vide, ce chapitre lui-même vient de se terminer (`FtueTutorialEngine.CompleteChapter` vide `CurrentStepId`). Alternative rejetée : étendre `TutorialProgress` avec une liste de chapitres terminés — plus robuste mais touche serveur+client+DTOs pour un gain marginal vu qu'il n'existe que 2 chapitres aujourd'hui ; documenté ici comme piste si un PART3 rend l'inférence insuffisante.

Test de reprise minimal écrit : `Part2Persistence_ResumeCorrectStep` (Research + Training complétés comme étapes FTUE → étape Army active → nouveau moteur depuis le même store → reprise exacte à `army_open`, aucune re-complétion de Research/Training re-testée explicitement idempotente).

## 14. Economy feasibility

Vérifié AVANT d'écrire le code (aucune triche, aucun bonus de ressources arbitraire) :

| Action | Coût réel | Depuis catalogue |
|---|---|---|
| Research `tempered_combs_i` | 180 miel + 120 pollen | `HiveOperationService.ResearchCatalog` |
| Training `darters` | 500 miel + 120 pollen | `CombatRecruitmentService.Catalog` |
| Bootstrap Alpha (`CreateInitialHiveState`) | 1500 miel / 500 pollen / 500 cire | `Server/src/BeeKingdom.Server/Program.cs:2707` |

Research + Training combinés = 680 miel + 240 pollen — largement dans le bootstrap, même en gardant la marge pour l'upgrade Palais/Caserne de Part1 (972 miel/251 cire) si séquencé plutôt que simultané. **Aucune correction de bootstrap nécessaire.**

**BLOCKERS produit réels (pas des bugs de code)** :
- `LivingHiveResearch:Enabled = false` dans `appsettings.Production.json` (ligne 138) → `GET/POST /game/v1/hives/{hiveId}/research*` retourne `503 game.unavailable`.
- `LivingHiveResearch:Catalog` (liste blanche d'IDs autorisés à démarrer, distincte du catalogue de coûts en C#) est **vide** dans le fichier suivi → même avec `Enabled=true`, `POST .../research/{researchId}/start` renverrait `400 invalid_request` pour `tempered_combs_i` tant que cet ID n'est pas ajouté à la liste.
- `CombatSquadReservation:Enabled = false` en production → "Confirmer l'escouade" échouerait ; contourné dans la conception Army (section 10), pas de code écrit contre ce flag.

`CombatRecruitment:Enabled = true` en production — Training n'a aucun blocage de ce type.

## 15. Tests

Tous écrits selon le format demandé (nouveau fichier `Assets/BeeKingdom/Tutorial/Tests/Editor/FtueHiveCorePart2Tests.cs`, même style que `FtueTutorialEngineTests.cs` de M037, réutilise son `InMemoryTutorialStore`) :

| Test | Statut |
|---|---|
| PART2 initialization | AUTOMATED TEST (écrit) |
| PART1 → PART2 transition | AUTOMATED TEST (écrit) |
| Research target resolution | AUTOMATED TEST (écrit) |
| Wrong Research target rejected | AUTOMATED TEST (écrit) |
| Research window detection | AUTOMATED TEST (écrit) |
| Real ResearchStarted advances | AUTOMATED TEST (écrit) |
| Tutorial does not start Research itself | AUTOMATED TEST (écrit) |
| Training target resolution | AUTOMATED TEST (écrit) |
| Wrong Training target rejected | AUTOMATED TEST (écrit) |
| Real TrainingStarted advances | AUTOMATED TEST (écrit) |
| Tutorial does not start Training itself | AUTOMATED TEST (écrit) |
| Army target resolution | AUTOMATED TEST (écrit) |
| Army window detection | AUTOMATED TEST (écrit) |
| Army interaction advances | AUTOMATED TEST (écrit) |
| PART2 persistence | AUTOMATED TEST (écrit) |
| Resume correct step | AUTOMATED TEST (écrit) |
| Completed steps idempotent | AUTOMATED TEST (écrit) |
| No LivingHive runtime dependency | AUTOMATED TEST (écrit) |

**Aucun de ces tests n'a pu être exécuté** (voir section 16). Ils sont rédigés dans le même idiome exact que les 11 tests M037 déjà passants (mêmes helpers, même style d'assertions), donc j'ai une confiance raisonnable dans leur logique, mais "écrit" n'est pas "vérifié".

## 16. Runtime validation

| Niveau | Statut |
|---|---|
| AUTOMATED TEST | Écrit, **NON EXÉCUTÉ** — Unity MCP (`ai-game-developer`) déconnecté (`ConnectionRefused`) toute la session |
| EDITOR RUNTIME | **NON TESTÉ** — même raison |
| BUILT PLAYER | **NON TESTÉ** |
| LIVE BACKEND | **NON TESTÉ** — Research est de toute façon éteint en prod (section 14) |
| CEO CLEAN ACCOUNT | **NON TESTÉ** (attendu, hors périmètre de cette mission) |

C'est la limitation la plus importante de ce rapport : le code a été écrit avec le plus grand soin de cohérence avec les patterns existants et relu plusieurs fois, mais **n'a jamais compilé dans Unity** cette session. Un risque réel d'erreur de compilation ou de comportement runtime imprévu existe tant qu'une session avec accès Unity MCP ne l'a pas vérifié.

## 17. Files changed

- `Assets/BeeKingdom/Tutorial/Runtime/TutorialGameplayNotifier.cs` — 3 nouveaux événements
- `Assets/BeeKingdom/Tutorial/Runtime/FtueTutorialTypes.cs` — nouveaux enums + constantes de registre
- `Assets/BeeKingdom/Tutorial/Runtime/FtueChapterDefinitions.cs` — nouveau chapitre PART2
- `Assets/BeeKingdom/Tutorial/Runtime/TutorialTargetRegistry.cs` — 2 nouveaux fallbacks IMGUI
- `Assets/BeeKingdom/Tutorial/Runtime/FtueTutorialBootstrap.cs` — chaînage de chapitre, nouveaux abonnements/cas visuels
- `Assets/BeeKingdom/Playground/HiveResearchPresentation.cs` — 1 ligne (notifier après succès réel)
- `Assets/BeeKingdom/Playground/HiveOfficialDoctrineRecruitmentPresentation.cs` — 1 ligne (notifier après succès réel)
- `Assets/BeeKingdom/Playground/HiveMapArmyBootstrap.cs` — notifier ouverture fenêtre + interaction locale réelle
- `Assets/BeeKingdom/Playground/HiveMapResearchBootstrap.cs` — notifier sélection/ouverture bâtiment
- `Assets/BeeKingdom/Tutorial/Tests/Editor/FtueHiveCorePart2Tests.cs` — nouveau fichier de tests
- `Docs/AI/Missions/M038-CL-FTUE-Hive-Core-Part2.md` — ce rapport

Aucun fichier serveur touché. Aucune migration SQL.

## 18. Blockers

1. **Unity MCP déconnecté** — bloque toute vérification EDITOR RUNTIME/BUILT PLAYER de ce travail. Fix minimum requis : reconnecter `ai-game-developer` puis relancer les 11 tests M037 + les 18 nouveaux tests M038 en EditMode, et valider une fois en Play Mode dans HiveMap.
2. **`LivingHiveResearch:Enabled=false` + `Catalog` vide en production** — Research reste injouable en conditions réelles tant que ce n'est pas activé et que `tempered_combs_i` n'est pas ajouté à `LivingHiveResearch:Catalog`. Décision produit à prendre séparément (hors périmètre technique de cette mission).
3. **`CombatSquadReservation:Enabled=false` en production** — contourné pour Army (interaction locale), pas un blocker pour cette mission, mais documenté pour référence future si une vraie confirmation d'escouade devait un jour faire partie du FTUE.

## 19. PART3 WorldMap readiness

Non implémenté (hors périmètre, comme demandé). Le chapitre PART2 se termine par un dialogue narratif ("Ta colonie grandit...") et une complétion de chapitre propre (`FtueTutorialEngine.CompleteChapter` ajoute `FTUE_HIVE_CORE_PART2` à `CompletedChapters`). `FtueTutorialBootstrap.StartNextIncompleteChapter()` est déjà structuré pour accueillir un futur `FTUE_WORLD_MAP_PART3` par un simple ajout de branche `if`, sans retoucher Part1/Part2.

## 20. Final verdict

- **A. FTUE_HIVE_CORE_PART2 implémenté avec le moteur M037 ?** OUI (aucune nouvelle mécanique de moteur ajoutée — seulement de nouvelles définitions de chapitre/étapes/événements suivant les patterns existants).
- **B. Research guidance opère sur le vrai système Research ?** OUI côté câblage (vrai endpoint, vrai catalogue, vrai événement post-succès) — mais le système lui-même est éteint en production (blocker documenté, pas un défaut du câblage).
- **C. Training guidance opère sur le vrai système Training ?** OUI, et ce système est actif en production.
- **D. Army guidance opère sur le vrai système Army ?** OUI pour l'ouverture de fenêtre et le roster (réels) et l'interaction de sélection d'escouade (réelle, locale) — PAS pour la confirmation d'escouade serveur (désactivée en prod, volontairement non utilisée).
- **E. PART2 survit à une fermeture/réouverture à la bonne étape ?** OUI par construction (test écrit) — non vérifié en exécution réelle (Unity MCP indisponible).
- **F. Le tutoriel évite toute mutation gameplay par lui-même ?** OUI — chaque notifier n'est appelé qu'après un succès serveur confirmé (`snapshot = response.Snapshot`), jamais avant, jamais par le moteur lui-même.
- **G. PART1 → PART2 jouable comme un tutoriel Hive cohérent ?** OUI par construction (chaînage explicite) — non vérifié en exécution réelle.
- **H. Le FTUE Hive Alpha Core est-il prêt pour la validation CEO clean-account ?** **NON.**

**Blockers requis pour H = OUI :**
1. Reconnecter Unity MCP et faire passer les 11 tests M037 + 18 tests M038 en EditMode.
2. Valider au moins une fois en Play Mode dans HiveMap (Research, Training, Army, reprise après fermeture).
3. Décision produit sur `LivingHiveResearch` (activer + peupler `Catalog`) si Research doit faire partie du parcours clean-account testé par le CEO — sinon, exclure explicitement Research de ce test tant que le flag reste désactivé.

---

## 21. M038B Runtime Closeout (2026-08-31, suite)

Mission de suivi : faire passer H de NON à OUI. Aucune nouvelle partie du tutoriel construite — validation + activation Research ciblée + correction de l'économie, comme demandé.

### 21.1 Compilation

Même problème que M037B avant correction : 3 processus `Unity.exe` orphelins (+ `UnityCrashHandler64`/`UnityAutoQuitter`/`UnityShaderCompiler`) et un `Temp/UnityLockfile` obsolète bloquaient toute nouvelle session batchmode. Tués proprement (`Stop-Process`), lockfile supprimé. **Cause racine distincte trouvée en plus** : combiner `-runTests` avec `-quit` fait sortir Unity après le refresh initial des assets, **avant** que le test runner ne s'exécute (Unity quitte sur le `-quit` explicite avant que la tâche de test asynchrone ne démarre) — aucun test n'avait alors réellement tourné malgré un exit code 0. Corrigé en retirant `-quit` (le test runner Unity gère sa propre sortie).

Un run "suite EditMode complète" (sans filtre) s'est ensuite bloqué (0% CPU sur 5s, process réellement figé) sur un test sans rapport avec M038 (dernière trace visible : `Assets/BeeKingdom/Networking/MobileAccountSessionClient.cs`). Non creusé plus loin (hors périmètre de cette mission, pré-existant) — contourné en filtrant sur les seules classes FTUE.

`dotnet build Server/src/BeeKingdom.Server/BeeKingdom.Server.csproj` — **PASS**, 0 avertissement, 0 erreur (confirme que le code C# du serveur touché par M038B compile aussi).

### 21.2 Tests M037 (11) + M038 (18)

Run filtré : `-testFilter "BeeKingdom.Tests.Editor.FtueTutorialEngineTests|BeeKingdom.Tests.Editor.FtueHiveCorePart2Tests"`, sans `-quit`.

**RÉSULTAT : 29/29 PASS, 0 FAIL, 0 inconclusive, 0 skipped** (`test-run ... total="29" passed="29" failed="0"`). 11 tests M037 (`FtueTutorialEngineTests`) + 18 tests M038/M038B (`FtueHiveCorePart2Tests`, un test supplémentaire `RealProductionCollectedAdvances_WrongBuildingRejected` ajouté avec l'étape de collecte — section 21.6). Ceci est une exécution réelle vérifiée machine, pas une estimation.

Tests serveur directement affectés (impactés par l'activation Research, section 21.4) :
- `BeeKingdom.Tests` filtré sur `Research` : **6/6 PASS**.
- `BeeKingdom.HiveOperations.Tests` filtré sur `Research` : **4/4 PASS**.

### 21.3 Play Mode

**NON RÉALISÉ.** Le pont MCP Unity (`ai-game-developer`, `http://localhost:23770`) reste injoignable (`Connection refused`) en dehors d'une session Editor interactive — le batchmode utilisé pour les tests EditMode n'expose pas ce pont. Ouvrir manuellement l'Editor et interagir avec l'UI (clics, vérification visuelle des flèches) nécessiterait soit que Jeff ouvre l'Editor et m'accorde un accès contrôle-à-distance/computer-use, soit une future session avec le MCP reconnecté. Aucune vérification visuelle de l'alignement des flèches (Research/Training/Army, IMGUI, résolutions Windows/mobile) n'a donc été possible cette session — reste un vrai blocker pour H=OUI.

### 21.4 Research — investigation du flag

Recherche complète (docs, tests, code) menée avant toute modification. Résultat :

- **Aucun bug de sécurité/données trouvé** dans `StartResearchAsync`/`CompleteResearchAsync` : idempotence par hash+clé, vérification de revision, débit unique confirmé par test (`StartDebitsOnceAndReplayIsStable`, `CompletionCannotBeEarlyAndThenAppliesEffectOnce`). Même pattern que Combat/DailyRound, rien de plus faible.
- **Pourquoi c'était `false`** : `Docs/ProductionIntegration/LivingHive_Research_ServerAuthority_2026-07-22.md` documente que c'est resté au statut "jamais promu", même schéma que `BroodVitality`/`WorkshopBatchQualification`/`HiveStockSnapshot` — pas une décision de sécurité spécifique à Research.
- **Point d'attention réel trouvé** : `Docs/AI/Missions/M016E-CL-Research-Click-Hard-Freeze.md` documente un freeze client reproductible sur la fenêtre Research **officielle**, cause racine jamais formellement isolée, contourné à l'époque en routant vers un fallback local. Lecture directe du code actuel (`HiveMapResearchBootstrap.cs`) montre qu'un routage conditionnel vers la fenêtre officielle existe déjà (uniquement si session authentifiée + contrôleur disponible, fallback local sinon) — semble être une correction ultérieure au freeze documenté, mais **non re-vérifié en Play Mode** cette session. C'est le risque résiduel principal si Research est activé.
- **Aucun test contre une vraie base SQL Server** pour Research (uniquement `DurableJsonHiveStateRepository` et InMemory) — gap de couverture plus large que Research seul (`Persistence:Provider` reste `InMemory` par défaut en Production suivie).

### 21.5 Research — activation

Décision prise avec Jeff (question posée explicitement, pas de choix arbitraire) : préparer l'activation en local, **non commit, non déployé**.

`Server/src/BeeKingdom.Server/appsettings.Production.json` :
```
"LivingHiveResearch": {
  "Enabled": true,
  "CatalogVersion": "hive-gameplay-sprint-v1",
  "Catalog": ["foraging_routes_i", "tempered_combs_i", "pollen_sorting_i"]
}
```
Catalogue minimal = les 3 recherches tier-I sans prérequis (dont `tempered_combs_i`, choisie pour le FTUE) — pas tout le catalogue. Les tiers II/III restent hors liste blanche pour l'instant ; ils sont de toute façon protégés par leurs propres vérifications de prérequis côté `HiveOperationService`, donc les ajouter plus tard n'aurait aucun effet tant que tier I n'est pas complété par un vrai joueur. Reste à commit/déployer par Jeff quand le freeze historique (21.4) aura été revérifié en Play Mode.

### 21.6 Research — test réel

| Test | Statut |
|---|---|
| GET Research | AUTOMATED TEST (endpoint testé, `LivingHiveResearchEndpointTests`) — PASS |
| Catalog | AUTOMATED TEST — PASS (catalogue vide testé explicitement ; le nouveau catalogue peuplé n'a pas de test dédié à sa valeur exacte, seulement à son comportement) |
| tempered_combs_i visible | NON VÉRIFIÉ EN LIVE — nécessite Play Mode/backend réel (21.3) |
| Start real research | AUTOMATED TEST (idempotence/débit) — PASS. LIVE BACKEND — NON TESTÉ |
| Resource deduction | AUTOMATED TEST — PASS (`StartDebitsOnceAndReplayIsStable`) |
| Timer created | AUTOMATED TEST — PASS (`ActiveOperation`/`EndsAtUtc` couverts par les tests existants) |
| Tutorial event emitted | Câblage relu manuellement (post-succès uniquement, section 6 du rapport initial) — pas de test d'intégration bout-en-bout Unity+Serveur réel cette session |

Le notifier reste strictement post-succès — confirmé par relecture du point d'appel (`snapshot = response.Snapshot;` avant l'appel `NotifyResearchStarted`).

### 21.7 Training real runtime

`CombatRecruitment:Enabled=true` en production — non modifié (fonctionnait déjà). Aucune régression : les tests EditMode `WrongTrainingTargetRejected_RealTrainingStartedAdvances`/`TutorialDoesNotStartTrainingItself_OnlyObservesRealEvent` passent (21.2). LIVE BACKEND — non re-testé en HTTP réel cette session (hors périmètre de cette mission de validation FTUE ciblée, déjà couvert par la suite serveur existante).

### 21.8 Army runtime

`CombatSquadReservation` **non touché** (reste `false`), conformément à l'instruction explicite. Interaction locale (stepper `+`) validée par les tests EditMode (`ArmyInteractionAdvances_AnyFamilyAccepted`) — confirme qu'aucune troupe n'est consommée/réservée par cette interaction (mutation purement locale au modèle `selGuardians`/etc., aucun appel réseau). Ouverture de fenêtre et roster réel non re-vérifiés visuellement (21.3).

### 21.9 Arrow alignment

**NON VÉRIFIÉ** — nécessite Play Mode (21.3). Les fallbacks IMGUI en fraction d'écran (`ui.button.research_start`, `ui.button.training_start`, `ui.menu.army`) suivent exactement le même mécanisme déjà en place pour `ui.button.upgrade` (M037) — même risque hérité, pas nouveau, mais pas non plus revérifié aux résolutions demandées (Windows actuel + un ratio mobile).

### 21.10 FTUE economy — calcul exact et correction

Calcul de Jeff/GPT vérifié et confirmé exact :

| Étape | Miel | Cire | Pollen |
|---|---|---|---|
| Bootstrap | 1500 | 500 | 500 |
| Upgrade `guard_post` (Part1) | −972 | −251 | 0 |
| Research `tempered_combs_i` | −180 | 0 | −120 |
| **Sous-total avant Training** | **348** | 249 | 380 |
| Training `darters` | −500 | 0 | −120 |
| **Résultat sans correction** | **−152 (impossible)** | 249 | 260 |

**Root cause trouvée** : `honey_storage` (bâtiment réel de production passive, 480 miel/heure au niveau 1 — confirmé dans `HiveOfflineProductionService.cs:114`, niveau par défaut = 1 même sans entrée explicite dans `BuildingLevels`) accumule bien du miel en continu, MAIS dans un **pool `Pending` séparé** (`HiveOfflineProductionState.PendingByBuilding`) — jamais crédité automatiquement au solde dépensable. Sans étape de collecte explicite, ce miel n'existe pas pour l'achat Training, peu importe le temps réel écoulé.

**Note annexe trouvée en creusant** : le bootstrap initialise `BuildingLevels["honey_reserve"]` (`Program.cs:2722`), mais le catalogue de production réel utilise la clé `honey_storage` (confirmé via `BuildingMappingTable` : `BuildingTypes.HoneyReserve` → `BuildingLegacyKeys.HoneyStorage`). Incohérence de nommage préexistante, inoffensive en pratique (les bâtiments absents de `BuildingLevels` défaultent au niveau 1 pour la production), non corrigée ici (hors périmètre, pas un blocker).

**Décision prise avec Jeff : Option C — vraie étape de collecte.** Deux étapes ajoutées entre `research_timer_dialogue` et `training_intro` : `ftue.core2.collect_intro` (flèche vers la Réserve de miel) et `ftue.core2.collect_started` (`RequireProductionCollected`, complétion sur l'événement réel `ProductionCollected("honey_storage")`). Câblé sur le vrai point de succès serveur (`HiveOfflineProductionPresentation.cs`, `CollectCoreAsync`, après `snapshot = response.Snapshot;`), jamais avant. Aucun changement d'économie (bootstrap, coûts, catalogues) — uniquement une nouvelle boucle pédagogique réelle.

Avec la collecte : miel disponible avant Training = 348 + (miel en attente réellement accumulé depuis le bootstrap, ≥152 après ~19 minutes réelles à 480/h) − 0 (la collecte ne coûte rien) → 500 (coût Training) est atteignable **si au moins ~19 minutes séparent le bootstrap de la collecte**. Ce délai est plausible (dialogue Part1 + upgrade 3 min + recherche 2 min + lecture des dialogues Part2) mais **non garanti** — non vérifié en Play Mode chronométré réel cette session. Si un test réel montre que ce n'est pas encore suffisant, il faudra soit ralentir légèrement la séquence pédagogique (rien à changer côté économie), soit revisiter les options B/D/E — pas décidé unilatéralement ici.

Fichier test mis à jour en conséquence : `FtueHiveCorePart2Tests.cs` (nouvelle étape dans tous les chemins Research→Training, nouveau test `RealProductionCollectedAdvances_WrongBuildingRejected`) — 29/29 PASS confirmé (21.2).

### 21.11 Persistence/resume PART1/PART2

Non re-testé en conditions réelles (Play Mode requis, 21.3). Le test automatisé `Part2Persistence_ResumeCorrectStep` (déjà présent, section 15 du rapport initial) couvre le mécanisme via le moteur seul et passe (21.2) — mais ne remplace pas un vrai cycle fermeture/réouverture de l'application.

Point explicitement documenté comme demandé : **avant `FTUE_WORLD_MAP_PART3`, il faudra décider si `CompletedChapters` doit devenir une vraie liste persistée côté serveur** plutôt que la déduction basée sur `ChapterKey` (section 13 du rapport initial) — cette déduction reste correcte pour 2 chapitres mais ne se généralise pas proprement à un 3ᵉ (un chapitre PART3 en cours ne permettrait plus de déduire que PART2 est terminé simplement à partir de la clé de chapitre chargée, puisque PART2 pourrait être soit "en cours" soit "terminé" avec la même clé si l'inférence n'est pas étendue). Non implémenté ici (hors périmètre, PART3 non construit).

### 21.12 Remaining blockers

1. **Play Mode réel jamais exécuté** — nécessite soit une session Editor interactive ouverte par Jeff avec accès contrôle-à-distance, soit un MCP Unity reconnecté. Bloque la vérification visuelle des flèches, l'ouverture réelle des fenêtres Research/Training/Army, et le vrai test de reprise après fermeture.
2. **Freeze historique Research (M016E) non re-vérifié** — le routage conditionnel actuel semble sûr à la lecture du code mais n'a pas été rejoué en Play Mode depuis l'activation.
3. **Timing économie non chronométré réellement** — la fenêtre de ~19 minutes nécessaire à la collecte n'a pas été mesurée contre le vrai rythme de lecture des dialogues FTUE.
4. **Activation Research non déployée** — reste dans `appsettings.Production.json` local, non commit/push, en attente de décision finale de Jeff après vérification Play Mode.

## Final Verdict — M038B

- **A. Le projet compile avec M038 ?** OUI (confirmé : `dotnet build` serveur PASS, et le run EditMode Unity a atteint l'exécution des tests sans erreur de compilation Unity — aucune `error CS` dans les logs).
- **B. Tous les tests M037 passent ?** OUI — 11/11 (partie des 29/29 confirmés en 21.2).
- **C. Tous les tests M038 passent ?** OUI — 18/18 (partie des 29/29 confirmés en 21.2).
- **D. Research est-il sûr et activé pour l'Alpha ?** ACTIVÉ LOCALEMENT (non déployé) ; sûr côté code (aucun bug trouvé) mais avec un risque résiduel non re-vérifié (freeze historique M016E) — OUI avec réserve documentée, pas un OUI inconditionnel.
- **E. `tempered_combs_i` peut-il réellement démarrer via du vrai gameplay ?** Câblage confirmé correct par lecture de code + tests d'idempotence/débit ; **non exécuté en LIVE BACKEND réel** cette session — NON vérifié en conditions réelles.
- **F. Le vrai Training peut-il faire avancer le FTUE ?** OUI par le câblage (test automatisé PASS) ; NON vérifié en Play Mode réel.
- **G. Le guidage Army fonctionne-t-il en runtime HiveMap réel ?** NON vérifié (Play Mode requis).
- **H. Les flèches du tutoriel s'alignent-elles réellement sur leurs contrôles ?** NON vérifié (Play Mode requis).
- **I. PART1→PART2 reprend-il correctement après redémarrage ?** OUI par construction/test automatisé ; NON vérifié en conditions réelles (redémarrage d'application réel).
- **J. L'économie FTUE complète est-elle faisable depuis un bootstrap frais 1500/500/500 sans intervention admin ?** OUI avec la correction apportée (étape de collecte réelle, aucune modification d'économie) — **sous réserve du timing réel non chronométré** (21.10). Pas de modification d'économie faite sans décision — celle prise (Option C) a été explicitement validée par Jeff avant implémentation.
- **K. Le FTUE Hive-side Alpha Core est-il prêt pour la validation CEO clean-account ?** **NON.**

**Blockers concrets requis pour K = OUI :**
1. Une vraie session Play Mode (Editor interactif, accès accordé par Jeff ou MCP reconnecté) validant visuellement : flèches Research/Training/Army à au moins 2 résolutions, ouverture réelle des 3 fenêtres, reprise après fermeture réelle de l'application.
2. Reverifier le freeze Research historique (M016E) ne se reproduit pas avec l'activation actuelle, avant tout déploiement du flag en production.
3. Décision de Jeff : déployer (commit+push) l'activation Research locale, ou la garder en attente.
4. Confirmation empirique que le délai réel de la séquence FTUE (Part1 + Part2 jusqu'à la collecte) atteint bien les ~19 minutes nécessaires à combler le miel manquant — sinon ajuster le rythme pédagogique (pas l'économie).

---

## 22. M038B — Play Mode réel (2026-08-31, suite avec accès Unity Editor)

Jeff a ouvert Unity et accordé un accès contrôle-à-distance. Session Play Mode réelle effectuée (scène `Environment2D5D_HiveMap_Test`, compte neuf créé pour l'occasion — le compte vétéran de Jeff routait vers une scène différente `SandboxPlayground` avec un ancien système de tutoriel narratif sans rapport, non pertinent pour ce test).

### 22.1 BLOCKER RÉEL #1 trouvé et corrigé — le moteur FTUE ne démarrait JAMAIS en jeu réel

**Root cause** : `FtueTutorialBootstrap.AutoStart()` est un `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`, qui ne se déclenche **qu'une seule fois**, sur la toute première scène chargée au démarrage de Play Mode (l'écran de connexion, qui ne commence jamais par "Environment2D5D") — jamais à nouveau lors de la transition ultérieure vers la vraie scène HiveMap. `FtueTutorialBootstrap.InitializeForScene(scene)` existe justement pour ce cas de figure, mais **rien dans tout le code du dépôt ne l'appelait**. Confirmé en direct : sur une session réelle dans `Environment2D5D_HiveMap_Test`, `FindFirstObjectByType<FtueTutorialBootstrap>()` retournait `null`.

Comparaison avec le vrai mécanisme central existant : `HiveMapRuntimeBootstrapInitializer.cs` appelle explicitement `InitializeForScene(scene)` pour **20 autres bootstraps** (Army, Barrack, RoyalPalace, Construction, Nursery, etc.) via l'événement `SceneManager.sceneLoaded` — mais **`FtueTutorialBootstrap` et `HiveMapResearchBootstrap` étaient tous les deux absents de cette liste**. Ce n'est pas un bug introduit par M038 — c'est un trou de câblage préexistant de M037 lui-même, jamais détecté car le FTUE n'avait jamais tourné en Play Mode réel avant aujourd'hui.

**Fix minimal appliqué** (`Assets/BeeKingdom/Playground/HiveMapRuntimeBootstrapInitializer.cs`) : ajout des deux lignes manquantes, exactement le même appel que les 20 autres. **Vérifié en direct après correction** : `FtueTutorialBootstrap` trouvé, dialogue de Zephyra ("Bienvenue dans ta ruche !") affiché réellement à l'écran pour la toute première fois.

### 22.2 BLOCKER RÉEL #2 trouvé et corrigé — la flèche ne pointait jamais nulle part

**Root cause** : `TutorialTargetRegistry`'s fallback de résolution de bâtiment comparait `BuildingInteractionComponent.BuildingType` (valeurs réelles : `ROYAL_PALACE`, `BARRACK`, `RESEARCH`, etc. — constantes UPPER_SNAKE_CASE de `BuildingTypes.cs`) directement, en minuscule, contre les target ids du tutoriel (`administration_core`, `guard_post`, `research_node` — les clés legacy/hotspot). Ces deux espaces de nommage ne se sont **jamais recoupés**, y compris dans Part1 de M037 : `"royal_palace" != "administration_core"`, `"barrack" != "guard_post"`. Confirmé en direct via reflection : `_visible=True`, `_targetId="building.administration_core"` correct, mais **aucune flèche visible à l'écran**, sur aucun bâtiment.

La vraie table de correspondance existait déjà et est utilisée ailleurs dans le code (`BuildingMappingTable`, ex. `RoyalPalace → administration_core`) — jamais utilisée ici. **Fix minimal appliqué** (`Assets/BeeKingdom/Tutorial/Runtime/TutorialTargetRegistry.cs`) : route désormais par `BuildingMappingTable.TryGetByBuildingType(...)` au lieu d'une comparaison devinée.

**Vérifié en direct après correction** : flèche jaune clairement visible pointant sur le Palais Royal ; toucher réellement le bâtiment a ouvert la vraie fenêtre et fait avancer le tutoriel au bon dialogue suivant (`royal_tap → colony_dialogue`) ; reprise après redémarrage de Play Mode confirmée (le tutoriel a repris exactement à `royal_intro`, pas de répétition de `welcome`).

### 22.3 Chaîne réelle testée en direct (Part1)

| Action | Résultat |
|---|---|
| Dialogue Zephyra (welcome) | Affiché réellement, texte correct |
| Flèche vers Palais Royal | Visible, bien positionnée (après fix 22.2) |
| Tap réel sur le Palais Royal | Fenêtre officielle ouverte, tutoriel avance |
| Reprise après fermeture/réouverture Play Mode | Reprend exactement à `royal_intro`, pas de doublon |
| `OpenBarrackOverlayForExternalHost()` (vrai chemin de code, appelé directement — équivalent exact d'un tap réel) | Fenêtre Caserne réelle ouverte (Gardiennes/Voltigeuses/Lanceuses, vrais coûts affichés) ; tutoriel a avancé jusqu'à `upgrade_highlight` |
| Flèche vers bouton Upgrade (fallback fraction d'écran) | Visible |
| `TryStartUpgradeWithPrerequisiteRedirectForExternalHost("guard_post")` (appelé directement) | Retourne `true`, **mais** aucune ressource débitée, aucune opération active créée, le tutoriel n'a PAS avancé à `upgrade_started` |

### 22.4 Point non résolu — statut INCERTAIN, pas confirmé comme bug

Le dernier test (démarrage réel de l'amélioration `guard_post`) n'a pas produit l'effet attendu. Deux explications possibles, non départagées faute de temps :
- **Artefact de mon test** : j'ai appelé la méthode directement via `script-execute`, en dehors du contexte UI normal (pas de `selectedHotspot` pré-établi par un vrai clic) — plausible que ce chemin nécessite un état UI préalable que je n'ai pas reproduit.
- **Vrai bug pré-existant M037** : possible que ce chemin n'ait, comme le reste du FTUE, jamais été vérifié en conditions réelles.

Cliquer physiquement à la position de repli de la flèche (fraction d'écran 50%/75%) a ouvert un popup **sans rapport** ("Défense" — bâtiment non supporté), confirmant que cette position de repli ne correspond à aucun vrai bouton visible sur l'écran principal HiveMap — le vrai bouton "Améliorer" vit probablement dans un sous-panneau (fenêtre propre au bâtiment) jamais ouvert pendant ce test. **Non tranché** — à re-tester par un vrai clic humain complet (Caserne → panneau du bâtiment lui-même, pas seulement le panneau d'entraînement de troupes) plutôt que par appel direct de méthode.

### 22.5 Research/Training/Army (Part2) — non re-testés visuellement cette session

Faute de temps restant après la découverte et la correction des deux blockers réels ci-dessus (22.1/22.2, qui affectaient TOUT le FTUE, pas seulement Part2), Research/Training/Army n'ont pas été re-testés visuellement en Play Mode après application des fixes. Le raisonnement : les deux fixes sont partagés par tous les targets `building.*` (donc Research/Training bénéficient du même mécanisme déjà vérifié sur Royal Palace/Barrack), mais **ce n'est pas une vérification directe** — reste un vrai NON TESTÉ pour les nouveaux targets spécifiques à Part2 (`ui.button.research_start`, `ui.button.training_start`, `ui.menu.army`).

### 22.6 Verdict mis à jour

Avec les deux blockers réels trouvés et corrigés, la situation change fondamentalement par rapport à la section 21 :

- **H (Play Mode réel) — anciennement "jamais testé"** → maintenant **testé, et deux vrais bugs bloquants trouvés et corrigés en direct**, avec preuve visuelle (flèche + dialogue + avancement réel + reprise après redémarrage) pour Part1 jusqu'à `upgrade_highlight`.
- **Le point 22.4 (démarrage réel de l'upgrade) reste ouvert** — ni confirmé cassé, ni confirmé fonctionnel.
- **Research/Training/Army (Part2) restent NON re-testés visuellement** après les fixes, malgré une forte présomption qu'ils bénéficient des mêmes corrections.

**K (prêt pour validation CEO clean-account) reste NON**, mais la liste de blockers change entièrement :

1. ~~Play Mode jamais exécuté~~ — **RÉSOLU** (fait cette session, 2 bugs réels trouvés et corrigés).
2. **Nouveau** : clarifier le point 22.4 (upgrade réel) par un vrai clic humain complet, pas un appel de méthode direct.
3. **Nouveau** : re-tester visuellement Research/Training/Army en Play Mode après les fixes 22.1/22.2 (forte présomption favorable, non confirmée).
4. Re-vérifier le freeze historique Research (M016E) ne se reproduit pas — probe `[M016E-FREEZE-PROBE]` vu activement dans les logs pendant cette session, aucun freeze observé, mais Research n'a pas été ouvert directement pendant ce test.
5. Décision de Jeff : commit/push des trois fixes de cette session (bootstrap wiring, targeting, activation Research) — **tout reste local, non commité, non déployé**, conformément à la mission.
6. Timing économie (~19 min) toujours non chronométré réellement (inchangé depuis 21.12).

---

## 23. M038C — End-to-End Play Mode Closeout (2026-08-31, suite)

Objectif : fermer la boucle Play Mode réelle, en particulier PRIORITÉ 1 (Caserne/Upgrade), sans construire PART3 ni nouvelle fonctionnalité (sauf demande explicite de Jeff en cours de route — voir 23.3).

### 23.1 IMGUI targeting — mécanisme réel mis en place

Conformément à la demande explicite de la mission ("obtenir la vraie géométrie du contrôle" plutôt que deviner une fraction d'écran), ajout de `TutorialTargetRegistry.RegisterScreenRect(targetId, Rect)` / `TryResolveScreenRect(...)` — le code IMGUI qui dessine réellement un bouton publie son `Rect` exact **chaque frame où il est dessiné** (avec un TTL d'une frame pour ignorer les valeurs périmées si le panneau se ferme). Câblé sur les vrais points de dessin :

- **Bouton "Améliorer" de la Caserne** (`DrawBarrackTopBar`, `Rect upgradeBadge`) — géométrie réelle publiée pour `TargetUpgradeButton`.
- **Bouton "Former" (Training)** (`DrawOfficialBarrackContent`, action button) — géométrie réelle publiée pour `TargetTrainingStartButton`.
- **Bouton de démarrage Research** (carte `tempered_combs_i` spécifiquement, pas les autres recherches) — géométrie réelle publiée pour `TargetResearchStartButton`.
- **Bouton "Armée" de la Caserne** (nouveau, voir 23.3) — géométrie réelle publiée pour `TargetArmyMenu`.
- **Ligne "Armée" du sous-menu "Plus"** (`LivingHiveMenuCanvas.cs`, uGUI réel) — RectTransform réel publié via le mécanisme `RegisterUi` existant, à travers un nouveau pont `LivingHiveArmyBridge.SetArmyRowRectQuery`/`GetArmyRowRect` (cet assembly ne peut pas référencer `BeeKingdom.Tutorial` directement — même pattern de pont que l'existant `SetHandlers`/`OpenOverlay`).

**Vérifié en direct** : en ouvrant la Caserne réelle, les trois vrais boutons ("31 TROUPES", "N.0 Améliorer", "Armée") sont visibles exactement où le code les dessine — confirmé par capture d'écran, pas seulement par lecture de code.

Les fractions d'écran (0.5×Largeur, 0.75×Hauteur) restent en place uniquement comme **repli ultime** si aucune géométrie réelle n'est publiée, conformément à la mission.

### 23.2 PRIORITÉ 1 — Caserne/Upgrade — BLOCKER RÉEL CONFIRMÉ

**Upgrade real click = FAIL.**

Testé exactement comme demandé : Caserne ouverte réellement (via le même chemin de code qu'un tap, déjà validé en M038B), **vrai clic de souris** sur le bouton "N.0 Améliorer" à sa position réelle affichée à l'écran (confirmé visuellement avant le clic). Résultat : aucun changement — pas de débit de ressources, pas d'opération active créée, le tutoriel n'avance pas.

**Root cause identifiée par inspection directe de l'état réel du modèle** (`HiveBuildingUpgradeScreenModel`, via reflection sur l'instance réellement utilisée par l'UI, pas une supposition) :

```
State = Error
ErrorCode = invalid_response
Revision = 0
```

Ceci correspond exactement au texte "Réponse serveur refusée · aucune progression locale appliquée" (`OfficialBuildingUpgradeErrorText`, code `invalid_response`) — **PAS** un problème de ressources insuffisantes (ce code a son propre texte distinct, jamais déclenché ici). Un rafraîchissement forcé du contrôleur (`buildingUpgradeController.Refresh()`, appelé directement, deux fois, à quelques secondes d'intervalle) **n'a pas résolu l'erreur** — elle est persistante pour cette session, pas transitoire.

**Preuve que ce n'est pas spécifique à `guard_post`** : le même texte d'erreur exact ("Réponse serveur refusée · aucune progression locale appliquée") avait déjà été observé en ouvrant le panneau du **Palais Royal** plus tôt dans cette session Play Mode (section 22.3 du présent rapport, capture d'écran). Le système d'amélioration officiel semble avoir échoué à charger correctement son instantané initial pour **toute la session**, pas seulement pour la Caserne.

**Cause racine précise non isolée** (nécessiterait les logs serveur ou une capture réseau pour voir la réponse HTTP exacte que le client a refusé de parser) — piste la plus probable : la désérialisation côté client de la réponse `/building-upgrades` échoue pour un compte fraîchement créé sans historique d'amélioration préalable (cas limite jamais testé avant cette session, puisque tout le FTUE n'avait jamais tourné en conditions réelles avant M038B).

**MINIMUM FIX REQUIS** (pour une prochaine session, hors périmètre de M038C) : instrumenter `HiveBuildingUpgradeClient`/le contrôleur pour logger le corps brut de la réponse serveur au lieu d'avaler l'exception silencieusement en `ErrorCode=invalid_response`, puis reproduire avec un compte neuf pour capturer la vraie réponse fautive.

**Effet secondaire découvert** : `DrawPreviewActionButton(upgradeBadge, ..., true, true)` — le paramètre `enabled` du bouton est **câblé en dur à `true`**, indépendamment de l'état réel du modèle. Le bouton affiche donc toujours un aspect "cliquable normal" même quand le système est en erreur permanente — trompeur pour le joueur (et pour moi, la première fois). Non corrigé ici (changerait le comportement visuel du bouton au-delà du périmètre ciblage/FTUE de cette mission) — signalé pour décision séparée.

**Conformément à l'instruction explicite de la mission ("Ne continue pas vers PART2 tant que Upgrade real click = PASS"), les Priorités 2 à 5 (Research/Collecte/Training/Army en conditions réelles complètes) n'ont PAS été testées en Play Mode cette session.** Leur ciblage réel (23.1) est en place et prêt, mais non exercé de bout en bout à cause de ce blocker en amont.

### 23.3 Changement hors-périmètre demandé explicitement par Jeff en cours de session

Pendant le test, Jeff a demandé explicitement (en direct, remplaçant la contrainte "aucune nouvelle fonctionnalité" de cette mission) : ajouter un accès réel à l'Armée directement depuis la Caserne, pas seulement via le sous-menu "Plus".

**Fait** : nouveau bouton "Armée" dans `DrawBarrackTopBar` (même style que les badges existants), qui appelle le **même** gestionnaire réel que l'entrée existante (`LivingHiveArmyBridge.OpenOverlay()` — pas une seconde implémentation). L'entrée existante dans "Plus" n'a pas été retirée (Jeff n'a pas demandé de la retirer, seulement d'en ajouter une dans la Caserne) — à confirmer si un retrait est aussi souhaité.

### 23.4 Tests automatisés après corrections

**NON RE-EXÉCUTÉS cette session** — la session Editor interactive de Jeff était active en continu pour le Play Mode (nécessaire pour les tests visuels), et une exécution batchmode séparée entre en conflit avec une session interactive déjà ouverte sur le même projet (confirmé section 22, log identique). Aucune régression de compilation détectée (chaque changement a été suivi d'un `assets-refresh` avec vérification explicite de `0 erreur CS`), mais **les 11 tests M037 + 18 tests M038 n'ont pas été re-passés formellement après les changements de cette section 23** — à faire dès que la session Editor interactive sera fermée.

### 23.5 CompletedChapters — recommandation pour PART3

**Réponse demandée : NON** — ne pas remplacer l'inférence actuelle par une vraie persistance serveur de `CompletedChapters` avant PART3, **sauf si** PART3 introduit un scénario où un chapitre antérieur pourrait être "en cours" plutôt que "terminé" au moment où un chapitre plus récent est chargé (l'inférence actuelle suppose un ordre strictement linéaire Part1→Part2→Part3 sans jamais revenir en arrière). Tant que PART3 suit la même progression strictement linéaire, l'inférence par `ChapterKey` reste suffisante et évite de toucher au contrat serveur `TutorialProgress` pour un gain marginal. Si un jour un chapitre optionnel/parallèle est envisagé, alors OUI il faudra une vraie liste persistée.

## Verdict final — M038C

- **A. PART1 complet de bout en bout par interaction UI réelle ?** **NON** — bloqué à `upgrade_started` par le blocker 23.2 (Welcome → Palais Royal → tap réel → Caserne → tout confirmé PASS ; Upgrade réel = FAIL).
- **B. Le vrai Upgrade fonctionne et fait avancer le FTUE ?** **NON** — voir 23.2, blocker réel confirmé, cause précise identifiée.
- **C. La fenêtre Research s'ouvre sans le freeze historique ?** **NON TESTÉ** cette session (bloqué en amont par A/B, conformément à l'instruction de la mission).
- **D. La vraie Research réussit et fait avancer le FTUE ?** **NON TESTÉ**.
- **E. La collecte de miel réussit et fait avancer le FTUE ?** **NON TESTÉ**.
- **F. L'économie d'un joueur neuf atteint assez de miel pour Training ?** **NON TESTÉ empiriquement** (le blocker 23.2 empêche même d'atteindre cette étape en conditions réelles).
- **G. Le vrai Training réussit et fait avancer le FTUE ?** **NON TESTÉ**.
- **H. L'interaction UI Army réussit et fait avancer le FTUE ?** **NON TESTÉ** (ciblage prêt, action réelle non exercée).
- **I. PART2 se termine de bout en bout ?** **NON TESTÉ**.
- **J. La reprise après interruption fonctionne sans double mutation ?** **PARTIELLEMENT OUI** — confirmé pour Part1 (reprise exacte à `royal_intro` puis à `upgrade_started` à travers plusieurs redémarrages de Play Mode réels cette session) ; non re-testé pour Part2 (jamais atteint).
- **K. Toutes les flèches FTUE pointent-elles vers les VRAIS contrôles utilisés par le joueur ?** **OUI pour ce qui a pu être vérifié** (Palais Royal, Caserne, bouton Upgrade — vus visuellement à leur vraie position) ; **ciblage réel mis en place mais non vérifié visuellement** pour Research/Training/Army (bloqué par 23.2).
- **L. Les tests automatisés M037+M038 passent-ils toujours ?** **NON RE-VÉRIFIÉ** cette session (23.4) — aucune erreur de compilation détectée, mais suite de tests non repassée formellement.
- **M. Le FTUE Hive-side Alpha est-il prêt pour la validation CEO clean-account ?** **NON.**

**Blockers concrets requis pour M = OUI :**
1. **Corriger le blocker 23.2** (`HiveBuildingUpgradeScreenModel` en `Error/invalid_response` persistant pour un compte neuf) — c'est le seul blocker qui empêche TOUT le reste de la boucle FTUE Part1/Part2 d'être testable en conditions réelles. Priorité absolue pour la prochaine session.
2. Une fois 23.2 résolu : compléter Part1 réellement (Upgrade → timer → fin), puis re-tester Research/Collecte/Training/Army en conditions réelles (Priorités 2 à 5 de cette mission, non atteintes).
3. Re-passer les 11+18 tests automatisés après tous les changements de cette session (23.4).
4. Décision de Jeff sur le retrait ou non de l'entrée "Armée" du sous-menu "Plus" maintenant qu'elle existe aussi dans la Caserne (23.3).
5. Décision de déploiement des correctifs accumulés (M038B + M038C), tous locaux et non commités à ce jour.
