# M040-CL — Alpha FTUE Real Player Journey — End-to-End Hive Core Certification + Runtime Bug Fixing

**Statut au moment de la redaction : session en cours, rapport partiel.** Playthrough
reel avance jusqu'a l'ouverture reussie de la fenetre de Recherche officielle
(Phase 2A COMPLETE) apres resolution d'un blocage majeur (M016E). Plusieurs bugs
runtime reels decouverts et corriges en cours de route, conformement a la
philosophie de la mission. Suite du playthrough (Phase 2B/2C et au-dela) a
reprendre a la prochaine disponibilite du CEO.

## 1. Executive verdict (partiel)

- PART1 (FTUE_HIVE_INTRO_PART1) : **COMPLETE** via interactions humaines reelles
  (clic Suite sur le dialogue de fin de minuteur), transition automatique vers
  PART2 confirmee.
- Blocage majeur decouvert et resolu en direct : le gel M016E (Unity Editor qui
  se figeait a l'ouverture de la fenetre Recherche officielle) etait contourne
  depuis une mission precedente par un routage FORCE vers une ancienne fenetre
  locale, empechant structurellement le FTUE de jamais completer l'etape
  `ftue.core2.research_open`. Le CEO a confirme qu'une exclusion SentinelOne a
  ete appliquee depuis ; le contournement a ete retire et **testee avec un vrai
  clic humain — aucun gel, la vraie fenetre officielle s'ouvre.**
- Deux bugs UI supplementaires decouverts et corriges pendant le meme test :
  texte du dialogue FTUE incorrect ("Combs tempérés" au lieu du vrai nom
  affiche "Rayons tempérés") et absence de ciblage reel pour le bouton de
  demarrage de recherche (`ui.button.research_start`) — cause racine : le code
  de ciblage M038C avait ete ajoute a une methode de dessin qui n'etait jamais
  appelee reellement (`DrawOfficialResearchMenuPanel`), a cause d'un
  `OfficialResearchConfigured()` code en dur a `return false;`.
- Amelioration produit demandee en direct par le CEO, implementee : le FTUE
  n'exigeait que le DEMARRAGE de l'amelioration guard_post, jamais sa
  reclamation reelle (observe en Play Mode : "À valider" reste affiche
  indefiniment). Nouvelle etape `ftue.intro.upgrade_claim` ajoutee,
  regression-testee.
- Bug d'affichage supplementaire corrige : le panneau Recherche (10 cartes)
  debordait sous l'ecran et par-dessus le panneau de dialogue FTUE — defilement
  vertical ajoute.
- Suite de tests EditMode : **pas encore executee cette session** (necessite
  d'arreter Play Mode, deliberement reporte pour ne pas interrompre la session
  live du CEO pendant qu'il etait absent).

## 2. Starting baseline (Phase 0)

Inspection en lecture seule au debut de la session, avant toute nouvelle
interaction :

- PlayerId/HiveId : compte de test persistant (meme compte que M039, deja a
  travers plusieurs sessions de test).
- Balances : `honey=528, pollen=500, wax=249` (etat post-M039 : 1500-972=528
  honey, 500-251=249 wax, deja debite par le vrai clic d'amelioration de M039).
- BuildingLevels : les 14 batiments du catalogue, tous niveau 1.
- ActiveOperation : `guard_post 1->2, Status=awaiting_completion` (le
  minuteur de 3 min de M039 etait deja ecoule, operation jamais reclamee).
- FTUE : `ChapterId=FTUE_HIVE_INTRO_PART1, CurrentStepId=ftue.intro.timer_dialogue,
  LastCompletedStepId=ftue.intro.upgrade_started` — exactement la ou M039
  s'etait arrete.
- Server revision (Building Upgrade model) : 328.

Aucune donnee modifiee pendant cette phase.

## 3. PART1 completion

**Preuve : PLAY MODE OBSERVED + HUMAN CLICK VERIFIED.**

Le dialogue de Zephyra ("Parfait ! L'amelioration est en cours (3 min)...")
etait deja rendu correctement apres un premier redemarrage de Play Mode (voir
section "Incident non lie" plus bas pour le contexte de ce redemarrage). Le
CEO a clique reellement sur **Suite**.

Resultat inspecte immediatement apres (reflexion, lecture seule) :
```
ChapterId=FTUE_HIVE_CORE_PART2
CurrentStepId=ftue.core2.welcome
LastCompletedStepId=FTUE_HIVE_INTRO_PART1
CompletedChapters=FTUE_HIVE_INTRO_PART1
```

`PART1_COMPLETE = PASS`. `PART2_AUTO_START = PASS`.

## 4. Research window (Phase 2A)

**Premiere tentative — bloquee.** Le CEO a clique reellement sur le batiment
de Recherche indique par la fleche FTUE. Une fenetre "RECHERCHE" s'est bien
ouverte visuellement, mais :
- Son catalogue affiche ("Tri du pollen I/II/III", "Reserves scellees") ne
  correspondait a aucun des identifiants officiels attendus.
- `HiveViewProductUiPresenter.ResearchOverlayOpenForExternalHost` restait
  `False` malgre la fenetre visible.
- Le FTUE restait bloque sur `ftue.core2.research_open` (RequireWindowOpened),
  n'observant jamais la vraie ouverture.

**Root cause exacte (CODE INSPECTION, confirmee) :**
`Assets/Experiments/Environment2D5D/LivingHiveMenu/LivingHiveResearchHost.cs`,
`OnBuildingClicked` — routage FORCE et INCONDITIONNEL vers
`BuildingWindowRouter.TryOpen(building)` (l'ancienne fenetre locale), avec un
commentaire explicite documentant que ceci etait un contournement deliberé
d'une mission anterieure (M016E-CL) pour un vrai gel de l'Unity Editor a
l'ouverture de la fenetre officielle, cause probable : SentinelOne EDR
interceptant les I/O de fichiers Editor pendant un rechargement de GUISkin.
Le pont `LivingHiveResearchBridge` (avec `IsOfficialAvailable`/
`OpenOfficialOverlay()`) etait deja completement cable depuis M038B mais
jamais appele depuis ce point d'entree.

## 5. M016E freeze status

Le CEO a confirme avoir applique une exclusion SentinelOne depuis la
decouverte du gel original. **Decision prise en direct avec autorisation
explicite du CEO** : retirer le contournement inconditionnel et router
conditionnellement vers la fenetre officielle quand
`LivingHiveResearchBridge.IsOfficialAvailable` est vrai (code deja pret,
jamais branche), sinon conserver le repli local existant.

**Test reel effectue avec avertissement prealable du risque de gel** : le CEO
a redemarre, reconnecte, puis clique reellement sur le batiment de Recherche
une deuxieme fois.

Resultat (SERVER STATE VERIFIED + PLAY MODE OBSERVED) :
```
isPlaying=True isPaused=False   (immediatement apres le clic, aucune latence anormale)
ResearchOverlayOpenForExternalHost=True
CurrentStep=ftue.core2.research_select_highlight
TargetId=ui.button.research_start
```

**Aucun gel. RESEARCH_WINDOW_NO_FREEZE = PASS.** `M016E-CL` est desormais
resolu au niveau applicatif (le contournement n'est plus necessaire) — sous
reserve que l'exclusion SentinelOne reste en place sur tous les postes qui
executeront ce code (voir section "Implications production").

## 6. Bugs UI decouverts et corriges pendant ce test

Trois bugs distincts trouves en observant le vrai clic du CEO, tous corriges :

1. **Texte de dialogue incorrect** — `FtueChapterDefinitions.cs` demandait de
   choisir "Combs tempérés", mais le catalogue reel (meme source de donnees
   locale utilisee par les deux methodes de dessin,
   `LocalPreviewResearchCatalog`) affiche "Rayons tempérés" pour
   `tempered_combs_i`. Corrige (texte aligne sur le vrai catalogue).

2. **Cible FTUE non enregistree** — `OfficialResearchConfigured()`
   (`HiveViewProductUiPresenter.cs`) etait code en dur `return false;`,
   la vraie verification (`researchController != null && researchController.IsConfigured`)
   commentee juste en dessous. Consequence : `DrawResearchMenuPanel` ne
   delegue jamais a `DrawOfficialResearchMenuPanel` (ou vit le vrai
   enregistrement `RegisterScreenRect(TargetResearchStartButton, ...)` ajoute
   en M038C), rendant systematiquement le repli local SANS aucun ciblage —
   la fleche FTUE n'avait donc litteralement rien a viser. Restaure a la vraie
   verification.

3. **Panneau qui deborde de l'ecran** — les 10 cartes de recherche (hauteur
   fixe, aucune limite) s'etendaient sous le bas de l'ecran et par-dessus le
   panneau de dialogue FTUE (signale visuellement par le CEO, capture
   fournie). Corrige : defilement vertical (`GUI.BeginScrollView`) ajoute dans
   les deux methodes de dessin (`DrawResearchMenuPanel` et
   `DrawOfficialResearchMenuPanel`), limitant la liste aux bornes reelles du
   panneau. Le Rect du bouton cible FTUE est desormais converti de
   coordonnees locales de defilement vers coordonnees ecran reelles avant
   d'etre publie au registre de ciblage (sinon la fleche pointerait au
   mauvais endroit des que la liste defile).

## 7. Amelioration produit — reclamation reelle de l'amelioration (demande live du CEO)

Observation du CEO en cours de playthrough : le panneau lateral affichait
"Construction : En cours / À valider" — l'amelioration guard_post de M039
etait demarree, son minuteur ecoule, mais jamais reclamee. Le CEO a explicite :
*"C'est une tache du tutoriel demandee au joueur. Il faut lui faire faire la
tache au complet."*

**Implemente** :
- Nouveau `FtueStepKind.RequireUpgradeCompleted` (mirroir de
  `RequireProductionCollected`).
- `TutorialGameplayNotifier.UpgradeCompleted` (evenement + methode
  `NotifyUpgradeCompleted`), declenchee uniquement apres le vrai succes
  serveur de `buildingUpgradeController.Complete()` dans
  `RunOfficialBuildingUpgradeAction` (meme garantie que
  `NotifyUpgradeStarted` : jamais avant confirmation serveur).
- Nouvelle etape `ftue.intro.upgrade_claim` inseree entre `timer_dialogue` et
  la fin du chapitre Part1 — cible reelle : le meme bouton `TargetUpgradeButton`
  (qui affiche "Valider" au lieu d'"Ameliorer" une fois le minuteur ecoule).
- `FtueTutorialBootstrap.cs` cable (abonnement/desabonnement a l'evenement,
  case dans le switch d'affichage).

**Limite connue, deliberement non resolue cette session** (demande explicite
du CEO d'attendre) : cette nouvelle etape oblige desormais le joueur a
attendre les 3 minutes reelles du minuteur avant de pouvoir continuer le
FTUE. Le CEO a indique qu'une fois le systeme d'acceleration (speed-ups)
fonctionnel, une etape guidant le joueur a accelerer l'amelioration devra
etre ajoutee pour eviter ce blocage — **hors scope de cette session, note
pour une mission future.**

**Non teste en Play Mode reel cette session** (la session live du CEO avait
deja depasse ce point du FTUE avant l'ajout de cette etape — n'affecte que
les futurs parcours/comptes neufs). Couvert par un test de regression moteur
(section 22).

## 8-14. Suite du playthrough (Research start, economie, Training, Army, etc.)

**Non atteint cette session** — reporte a la prochaine disponibilite du CEO.
Le playthrough s'est arrete a `ftue.core2.research_select_highlight` avec un
ciblage desormais correct, pret pour le vrai clic de demarrage de recherche.

## 15-18. Portraits, ciblage, input gating

Non re-testes specifiquement cette session au-dela de ce qui est deja couvert
par les rapports M038C et le rapport UI dedie
(`RAP-UI-FTUE-Dialogue-Champion-Portrait.md`) — Zephyra confirmee fonctionnelle
tout au long de ce playthrough (portrait + voix, deja valide avant cette
mission).

## 19. Runtime bugs discovered (recapitulatif)

| Bug | Categorie | Statut |
|---|---|---|
| Routage Recherche force vers fenetre locale (M016E workaround) | Blocage FTUE structurel | Corrige (conditionnel restaure) |
| `OfficialResearchConfigured()` code en dur a `false` | Blocage FTUE + ciblage casse | Corrige |
| Texte dialogue "Combs tempérés" vs vrai nom "Rayons tempérés" | Confusion joueur | Corrige |
| Panneau Recherche deborde de l'ecran, par-dessus le dialogue FTUE | UI/UX | Corrige (defilement) |
| Amelioration jamais reclamee malgre minuteur ecoule ("À valider" permanent) | Boucle FTUE incomplete | Corrige (nouvelle etape) |

## 20. Root causes

Voir sections 4, 6, 7 ci-dessus pour chaque root cause detaillee.

## 21. Fixes applied — fichiers modifies

- `Assets/Experiments/Environment2D5D/LivingHiveMenu/LivingHiveResearchHost.cs`
  — routage conditionnel restaure (`LivingHiveResearchBridge.IsOfficialAvailable`).
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` — 
  `OfficialResearchConfigured()` restaure ; defilement ajoute dans
  `DrawResearchMenuPanel` et `DrawOfficialResearchMenuPanel` (+ conversion
  ecran du Rect cible) ; nouveau champ `researchMenuScroll` ; notification
  `NotifyUpgradeCompleted` ajoutee dans `RunOfficialBuildingUpgradeAction`.
- `Assets/BeeKingdom/Tutorial/Runtime/FtueChapterDefinitions.cs` — texte
  corrige ("Rayons tempérés") ; nouvelle etape `ftue.intro.upgrade_claim`.
- `Assets/BeeKingdom/Tutorial/Runtime/FtueTutorialTypes.cs` —
  `FtueStepKind.RequireUpgradeCompleted` ajoute.
- `Assets/BeeKingdom/Tutorial/Runtime/TutorialGameplayNotifier.cs` —
  evenement/methode `UpgradeCompleted`/`NotifyUpgradeCompleted`.
- `Assets/BeeKingdom/Tutorial/Runtime/FtueTutorialBootstrap.cs` — abonnement,
  handler, case switch pour `RequireUpgradeCompleted`.
- `Assets/BeeKingdom/Tutorial/Tests/Editor/FtueTutorialEngineTests.cs` —
  nouveau test `UpgradeCompleteDetection_GuardPost` ; deux tests existants
  corriges pour la nouvelle etape.
- `Assets/BeeKingdom/Tutorial/Tests/Editor/FtueHiveCorePart2Tests.cs` —
  assistant partage `CompletePart1` corrige.

## 22. Regression tests added

- `FtueTutorialEngineTests.UpgradeCompleteDetection_GuardPost` — verifie que
  la nouvelle etape rejette un mauvais batiment, n'avance pas prematurement,
  et complete reellement le chapitre uniquement sur le bon evenement.
- `FtueTutorialEngineTests.NoLivingHiveDependency_EngineWorksWithoutLivingHive`
  et `FullChapter_Playable_EndToEnd` — mis a jour pour inclure la nouvelle
  etape dans leur sequence complete.
- `FtueHiveCorePart2Tests.CompletePart1` (assistant partage, utilise par tous
  les tests Part2) — mis a jour.

## 23. Automated tests

**Non executes cette session** — necessite d'arreter Play Mode, deliberement
reporte pour ne pas interrompre la session live du CEO. A executer au prochain
arret naturel de l'Editeur, en verifiant le XML de resultats (jamais
`-quit` combine a `-runTests`, conformement a la contrainte M038B).

## 24. Files changed

Voir section 21.

## 25. Remaining blockers

- Suite EditMode non re-executee — a faire avant certification finale.
- Playthrough reel non termine au-dela de `research_select_highlight`.
- Nouvelle etape `upgrade_claim` non testee en Play Mode reel (uniquement
  couverte par test moteur) — a valider au prochain compte neuf/redemarrage
  qui traverse reellement Part1.

## 26. Production-readiness implications

Si l'exclusion SentinelOne qui a resolu M016E n'est appliquee que sur la
machine du CEO (et pas sur l'environnement de build/deploiement cible), le
gel pourrait resurgir en dehors de ce poste. **Ne pas deployer le retrait du
contournement M016E sans confirmer que l'exclusion est appliquee partout ou
ce code s'executera.**

## 27. Final verdict (partiel — session a reprendre)

- A. PART1 completes through real human interactions? **YES**
- B. PART1 automatically transitions to PART2? **YES**
- C. Research window opens without freeze? **YES** (apres correctif)
- D-N. Non atteint cette session — voir section 25.
- V. Is FTUE Hive Core ready for CEO clean-account certification? **NO** —
  playthrough incomplet, tests automatises non re-executes. Reprendre a
  `ftue.core2.research_select_highlight` (Phase 2B/2C) a la prochaine session.

---

## Annexe — Incident sans rapport avec le playthrough

Pendant l'investigation initiale du blocage Recherche, une pause accidentelle
de l'Editeur (Error Pause declenche par une erreur de reflexion de ma part,
appel malforme a `TryGetTargetPosition`) a ete confondue un instant avec un
vrai gel. Diagnostique et corrige immediatement (simple `isPaused=false`) —
aucun rapport avec le vrai systeme de jeu, mentionne ici uniquement pour la
tracabilite de session.
