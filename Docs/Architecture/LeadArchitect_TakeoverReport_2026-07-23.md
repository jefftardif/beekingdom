# Bee Kingdom — Rapport de prise de fonction (Lead Software Engineer / Technical Architect)

Date : 2026-07-23
Auteur : Nouvel agent responsable technique (continuité, pas de refonte)

Ce document synthétise l'exploration complète du projet avant toute intervention, conformément
au mandat de prise de fonction. Il fait le lien entre les 4 documents de mémoire officielle
(désignés par `AGENTS.md`), les ~690 fichiers markdown de `Docs/`, le code client Unity et le
code serveur .NET.

---

## 1. Résumé du projet

**Concept.** Bee Kingdom est un jeu mobile (portrait 390×844 / paysage 1600×900) de gestion de
ruche dans le genre "kingdom builder" (base-building + collecte + PvE léger + classes de
progression + social/alliances), actuellement en vertical slice / pré-production. L'écran
d'accueil n'est pas un menu abstrait : c'est une **ruche vivante** peuplée d'abeilles animées qui
exécutent des rôles visibles (transport de nectar, façonnage de cire, tri de pollen, soin du
couvain, patrouille).

**Boucle centrale :**
- **Collecte manuelle obligatoire** : le miel, la cire, le pollen s'accumulent dans les
  bâtiments mais ne sont jamais crédités automatiquement — le joueur doit taper le bâtiment.
  C'est une règle de design fondamentale et protégée, pas un oubli : une automatisation future
  ("Butineuses intendantes") ne pourra être vendue que comme confort à rendement identique,
  jamais comme avantage compétitif.
- **Files persistantes** d'amélioration de bâtiments, d'entraînement de troupes (Caserne) et de
  recherche, qui survivent au redémarrage de l'app.
- **Tutoriel narratif guidé** de 7 chapitres ("Acte I") enseignant les systèmes via une fiction
  ("crise de la ruche") plutôt que des pop-ups.
- **Choix de classe explicite** vers le niveau 100 : Gardiennes (mêlée/lourd), Voltigeuses
  (vitesse), Lanceuses (distance) — un triangle pierre-papier-ciseaux, avec une deuxième classe
  déblocable plus tard sans invalider la première.
- **Carte monde 50×50 tuiles**, traitée comme fondation artistique protégée et intouchable.
  Scène canonique : `Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity`.
- **Couche commerciale future (Phase 6)** conçue explicitement pour éviter le pay-to-win :
  cosmétique, confort à rendement égal, files supplémentaires, accélérateurs rattrapables,
  passes d'évènement avec palier gratuit substantiel.

**Référence fonctionnelle "Ant Legion".** `Docs/Benchmarks/AntLegion/AntLegion_BeeKingdom_FunctionalReference.md`
est une étude de terrain très détaillée (675 lignes) d'un jeu concurrent réel, utilisée
explicitement comme *"référence fonctionnelle, pas un modèle à copier"* — reprise verbatim dans
`AGENTS.md`, le journal VM et le plan d'exécution. Elle documente l'UI, le rythme du tutoriel,
l'économie des bâtiments/troupes, les patterns de monétisation et les irritants UX du
concurrent, avec une table de correspondance thématique abeilles.

**Ordre de construction en 6 phases** (`Docs/Product/BeeKingdom_LivingHive_ExecutionPlan.md`) :
1. Boucle quotidienne de la ruche — largement implémentée.
2. Tutoriel scripté — chapitres 1-5 implémentés mais **rythme trop court** (786-951s vs cible
   1860-3180s), écart documenté et non résolu.
3. Ambiance "ruche vivante" premium — première tranche implémentée.
4. Progression & choix de classe — en cours, majoritairement côté serveur et derrière des
   feature flags, pas encore branché au client en direct.
5. Monde, alliances, activités — sorties/débriefing implémentés côté serveur ; le chat est isolé
   sous la responsabilité exclusive d'un agent "Communication" dédié.
6. Économie commerciale équitable — principes seulement, rien construit.

**Définition de "terminé"** pour chaque tranche : comportement jouable de bout en bout, aucune
altération des fondations visuelles protégées, vérification automatisée du calcul/état
critique, vérification visuelle portrait+paysage, zones tactiles alignées sur l'art visible,
états fermé/erreur/plein/vide vérifiés, frontière device/cache/serveur documentée et testée
contre replay, nouvelle règle documentée.

---

## 2. Architecture

### 2.1 Client Unity

Toutes les ~20 assemblies modulaires vivent sous `Assets/BeeKingdom/<Module>/`, chacune avec son
propre `.asmdef` et son `.csproj` à la racine du repo :

| Module | Rôle réel |
|---|---|
| **Core** | Fondation : services/DI (`IServiceContainer`, `IEventBus`), moteur de simulation (`SimulationTickEngine`), moteur de sauvegarde (`SaveEngine`, `SaveManager`, `SaveMigrationManager`), framework façon Gameplay Ability System (Attributes/Abilities/Effects/Modifiers), registre d'entités. |
| **Hive** | Le module le plus riche : `HiveManager`/`HiveAggregate` (identité de ruche, abeilles/bâtiments/inventaire, capacité), `QueenManager`/`QueenAggregate` (cycle de vie de la reine : Œuf→Larve→Nymphe→Reine vierge→Reine fécondée→Reine active→Essaimage/Blessée/Morte), `TaskManager`/`TaskAllocator`/`TaskQueue` (création/réservation/assignation de travail). |
| **AI** | Comportement bas niveau des abeilles : `BeeAIManager` (mise à jour étalée par tick, visant 50k+ abeilles), `BeeBrain`, machine à états (Idle/Harvesting/Building/Guarding/Exploring/Waiting/Dead), `BeeBlackboard`. |
| **Population** | Couche stratégique/attributaire au-dessus du cycle de vie : décision, castes, génétique, santé/fatigue/besoins/expérience/personnalité/mémoire, intelligence collective, coordination multi-agents, stratégie de colonie. |
| **Buildings** | Cycle de vie de construction : placement, file de construction, validation, priorité, amélioration, graphe de dépendances, planification d'expansion. |
| **Chambers** | Agencement interne de la ruche : catégories de chambres, connexions, moteur de couloirs, intégrité structurelle. |
| **Economy** | Seul point de passage autorisé pour muter le stockage : `ResourceFlowManager/Engine/Graph`, `HiveInventoryManager`, `StorageGrid/Cell/Cluster`, réservations/transactions. |
| **World** | Carte/écosystème : `WorldManager/Generator/State`, grille hexagonale, gestion des fleurs/pollinisation, eau, météo/saisons, régénération, régions/biomes/chunks. |
| **Config / Data** | Chargement et validation de configuration typée ; registre central de définitions (Data a des sous-dossiers réservés vides pour futures ScriptableObjects). |
| **Gameplay** | Bootstrap de la ruche jouable, profils de démarrage, domaine partagé façon DDD, chat en jeu, **arbre de compétences du joueur** (conforme à la spec, voir 2.3). |
| **Networking** | Clients REST/session vers le serveur : comptes, production hors-ligne, amélioration de bâtiments, recherche, **sorties de périmètre (`HivePerimeterSortieClient`)** — c'est ici, pas dans `Combat`, que vit la représentation client du combat. |
| **Services** | Racine de composition/DI (`BeeKingdomCompositionRoot` — note dans le code : "les systèmes de gameplay ne sont volontairement pas créés ici pour l'instant"), implémentations Unity des abstractions Core. |
| **Save** | Uniquement des fichiers "Frameworks" de *gouvernance/readiness* de la persistance — le vrai moteur de sauvegarde vit dans **Core/Save**, pas ici. |
| **Combat** | ⚠️ **Coquille vide** — un seul fichier marqueur, aucune logique réelle. |
| **UI** | ⚠️ **Coquille vide** — un seul fichier marqueur. La vraie logique de productisation UI vit dans **Colony** (`HiveProductUiArtPassFrameworks`, etc.). |
| **Editor / QA** | Modules très légers (marqueurs + quelques frameworks de gouvernance). |
| **Colony** | Pas de gestion d'identité de colonie ici (ça, c'est côté serveur) : progression/prestige/scénarios de colonie + un gros volume de frameworks social/MMO et de productisation UI. |
| **Tests** | 145 fichiers, tests en mode édition uniquement (pas de PlayMode dédié), bonne couverture de la plupart des modules. |

**Hors modules** : `Assets/BeeKingdom/Playground/` (pas de `.asmdef`, compile dans
`Assembly-CSharp` par défaut) contient les bootstraps de démo et présentateurs de scène
concrets (`LivingHiveDemoBootstrap`, `HiveViewProductUiPresenter`, etc.) — c'est la couche de
présentation Unity effective. `Assets/_Project` est un squelette générique hérité, non
utilisé par aucun des 20 modules. `Assets/_Recovery` contient 7 scènes orphelines
(vraisemblablement des autosaves de crash Editor rescapés), sans documentation.

**Dérive documentaire majeure** : `Docs/Architecture/ColonyArchitecture.md` et
`SimulationArchitecture.md` décrivent en réalité le **serveur** (`Server/src/BeeKingdom.Colony`,
`Server/src/BeeKingdom.Simulation`), pas les modules client homonymes. Les docs eux-mêmes sont
cohérents (ils précisent "service backend"), mais le rangement dans le même dossier que les
docs client crée un piège de lecture.

### 2.2 Serveur (.NET, `Server/`)

Monolithe modulaire ASP.NET Core (.NET 8), solution séparée `Server/BeeKingdom.Server.slnx`,
~15 projets sous `Server/src/` : Accounts, Admin, Authentication, Chat, Colony, Database,
Gateway, HiveOperations, Infrastructure, Persistence, Protocol, Server, Shared, Simulation,
Tools.

- **Topologie** : très majoritairement REST/HTTP (minimal API, `Program.cs` ~2140 lignes,
  ~90 routes). **Un seul canal temps réel réel** : un hub SignalR pour le chat
  (`/chat/v1/realtime`). La couche "Protocol" (enveloppe de message versionnée) existe comme
  contrat mais n'est pas branchée sur le fil en production (le serveur envoie du JSON REST brut).
- **Authentification maison** : email/mot de passe (PBKDF2-SHA256 + sel), tokens opaques
  hashés en base, rotation au refresh. Providers Google/Apple/Steam/Epic/invité : interface
  prévue, non implémentée.
- **Persistance** : `InMemory` par défaut ; `SqlServer` disponible en option mais non déployé en
  production. Scripts SQL et migrations existent (`BeeKingdom.Database`, `BeeKingdom.Tools`).
- **Simulation** : `SimulationEngine` charge/décharge chaque colonie individuellement
  (`Dictionary<ColonyId, LoadedSimulationColony>`), exécute les ticks par lots sur les colonies
  chargées, décharge automatiquement les colonies inactives. **C'est une simulation instanciée
  par joueur/colonie, pas un monde MMO partagé et synchronisé en continu.**
- **Le modèle MMO monde partagé** (`WorldMmoServerModel.md` : `WorldId`/`GameServerId`,
  territoires d'alliance, chemins de vol, ressources partagées) est **un document de design
  futur explicitement non implémenté** — les endpoints `/runtime/*-readiness` correspondants
  renvoient volontairement `NonLive:true`.
- **Déploiement cible** : Windows Server 2025, IIS (Hosting Bundle, in-process), HTTPS 443, SQL
  Server dédié. Scripts de publication/rollback prêts (`Server/deploy/`). Les runbooks
  eux-mêmes répètent explicitement : **"Not Production Ready Yet"**.
- **Tests** : bonne couverture des endpoints `/game/v1/hives/*` et des services HiveOperations
  (centaines de tests), tests architecturaux (`ArchitectureTests.cs`), tests de garde-fou
  anti-survente (`AuthenticationProductionBoundaryTests`, `GameReadModelSecurityTests`).
  `BeeKingdom.ChatTranslation.Tests` est vide/abandonné.

### 2.3 Processus multi-agents (contexte de continuité)

Le projet a été développé par un essaim d'agents IA à rôles fixes, avec deux générations de
workflow documentées :

1. **Ancien pipeline (ARCH-nnn)** : Architecte (coordinateur) → BuilderA/B/C (implémenteurs) →
   Demo (rejoue/valide) → QA (verdict PASS / PASS_WITH_RESERVES / FAIL) → Planner (dispatch du
   lot suivant de tickets `BEE-nnn`). Ce cycle s'arrête à `ARCH-241` (12/07) sans clôture
   formelle — voir section 4.
2. **Workflow actuel, plus léger** (`Docs/AgentCoordination/*_VM_Assignment.md`, 21/07) : trois
   rôles à partition stricte de fichiers travaillant en parallèle sur la même copie —
   **Architecte** (expérience LivingHive/mobile), **Communication** (chat/temps réel/traduction,
   isolé), **Intégrateur** (persistance serveur autoritaire). Chacun documente sa tranche et
   communique par notes de handoff plutôt que de toucher aux fichiers de l'autre.

**Aucun git fonctionnel n'existe** : `.git/` est un dossier vide (pas de HEAD/objects/refs).
Tout l'historique repose sur les rapports markdown horodatés et un outil de synchronisation
VM↔ordinateur principal (`tools/vm-sync/BeeKingdom-VmSync.ps1`), avec consigne explicite de ne
jamais utiliser git dans la VM.

---

## 3. État d'avancement

### ✅ Terminé (prouvé par tests + preuves visuelles)
- Boucle quotidienne locale : collecte manuelle, files persistantes upgrade/entraînement/recherche.
- Tutoriel narratif Acte I (7 chapitres) jouable localement, profil versionné (`v12`).
- Carte monde 50×50 (Wave6) : art figé, sans couture visible, verrouillé sur la scène canonique.
- Contours de zones de bâtiments de la ruche : 14 zones tracées à la main (SVG), importées en JSON runtime, aucune zone non reconnue.
- Fondations serveur : auth, comptes, colonies (CRUD in-memory + SQL en option), simulation par instance, migrations.
- Chat backend réel (hub SignalR) + service de traduction.
- Systèmes "Hive Operations" serveur : soin du couvain, ronde quotidienne, recrutement de doctrine, réservation d'escouade, sortie de périmètre, recherche, amélioration de bâtiments — tous testés (centaines de tests verts par tranche).

### 🟡 En cours / partiel
- **Connexion live client↔serveur** : la quasi-totalité des feature flags des systèmes ci-dessus sont fermés par défaut *et en Production* — rien de tout cela n'est aujourd'hui accessible à un vrai joueur.
- Rythme du tutoriel Acte I (chapitres 1-5) : écart de durée documenté et non corrigé.
- Packaging Android : build IL2CPP ARM64 atteint la compilation native mais échoue à `compressDebugAssets` (manque d'espace disque) ; APK estimé à 2,02-2,25 GiB, largement à cause du tileset 50×50 non compressé embarqué dans `Resources`. Aucun APK installable n'existe encore.
- Synchronisation VM ↔ ordinateur principal : en échec en boucle depuis le 22/07 ("accès refusé" sur le partage réseau) — le travail des 22-23/07 existe uniquement sur la copie locale VM.
- Modèle MMO monde partagé : conçu (doc + contrat de chunks non enregistré) mais non branché au runtime.

### ❌ À faire
- Vraies implémentations pour les assemblies client **Combat** et **UI** (actuellement des coquilles vides).
- Vrai monde MMO partagé/synchronisé en continu (aujourd'hui : instances isolées par colonie).
- Vraie passerelle réseau (Gateway) — actuellement du scaffolding en mémoire, pas de reverse-proxy réel.
- Providers d'authentification tiers (Google/Apple/Steam/Epic/invité).
- Persistance SQL en production réelle (InMemory reste le défaut).
- Phase 6 (économie commerciale) — principes seulement.
- Résolution du conflit de synchronisation non résolu du 21/07 (`BeeKingdom_LivingHive_ExecutionPlan.md` / `Codex_VM_Continuation.md`, édités des deux côtés).
- Application des 4 suppressions en attente côté sync (fichiers `PerformanceTestRunInfo/Settings` obsolètes).
- Dépôt git fonctionnel (actuellement inexistant).

---

## 4. Dette technique

1. **Absence de git fonctionnel** malgré un volume de changement considérable — aucun blame/diff/rollback fiable possible, risque réel de perte de travail. *Priorité maximale.*
2. **Synchronisation VM/hôte fragile et actuellement en échec** — travail récent non répliqué vers la machine principale ; un conflit non résolu dort depuis le 21/07 sur deux documents de mémoire officielle.
3. **Dérive documentaire Colony/Simulation** : deux docs d'architecture décrivent le serveur sous un nom identique à des modules client, sans renvoi croisé suffisamment visible pour éviter la confusion.
4. **Modules "coquilles vides" (Combat, UI, Editor)** côté client : ils suggèrent une couverture modulaire qui n'existe pas ; la vraie logique est dispersée dans Networking/Colony/Playground, ce qui contredit le principe de séparation des responsabilités affiché par l'architecture à 20 assemblies.
5. **Playground sans `.asmdef`** compilant dans `Assembly-CSharp` par défaut — accroc à la modularité déclarée.
6. **Débris non nettoyés** : `Assets/_Recovery` (scènes orphelines probablement issues de crashs), scripts `build_v3*.py` à la racine (chaîne d'expériences abandonnées de génération de tuiles), dossier `outputs/` (export ponctuel).
7. **`HiveOperations` (serveur)** organisé de façon ad hoc (pas de sous-dossiers Configuration/DI/etc. comme les autres projets) — signe d'itération rapide sans convention appliquée.
8. **`BeeKingdom.ChatTranslation.Tests`** : projet de test vide/abandonné.
9. **Volume documentaire très élevé avec redondance et rupture de continuité** : la saga des contours de ruche (ARCH-233→241) s'est en réalité résolue le lendemain, mais **hors de la numérotation ARCH**, dans des rapports Builder ad hoc — un lecteur qui ne suit que `Docs/Architecture/` conclurait à tort que le sujet est toujours bloqué. Ce pattern (résolution non reflétée dans le journal officiel) est un risque de continuité récurrent.
10. **Feature flags fermés partout en production** sans plan d'activation documenté — risque de dérive "quasi-vaporware" si cela perdure sans jalon explicite de mise en ligne.
11. **Poids du build Android** (~2+ GiB) directement lié au tileset non compressé — dette de livraison qui bloque tout test mobile réel.
12. **Pas de tests PlayMode séparés** côté client (uniquement edit-mode).

---

## 5. Priorités (backlog)

**P0 — Risque/blocage**
1. Sécuriser l'historique : comprendre pourquoi `.git` est vide et mettre en place un vrai suivi de version.
2. Résoudre le conflit de sync du 21/07 sur les deux documents de mémoire officielle et rétablir la synchronisation VM↔hôte.
3. Appliquer ou explicitement rejeter les 4 suppressions en attente côté sync.

**P1 — Fondations produit**
4. Combler l'écart de rythme du tutoriel Acte I (chapitres 1-5).
5. Débloquer le packaging Android (compression/streaming du tileset 50×50, premier APK installable).
6. Documenter un critère clair d'activation des feature flags fermés en production.

**P2 — Dette d'architecture**
7. Clarifier la documentation Colony/Simulation (server vs client) pour éliminer l'ambiguïté.
8. Statuer sur le sort des assemblies vides Combat/UI/Editor (implémenter, fusionner, ou documenter pourquoi elles restent réservées).
9. Nettoyer `Assets/_Recovery`, `outputs/`, `build_v3*.py` après validation utilisateur (aucune suppression sans confirmation).

**P3 — Prochaines fonctionnalités (suivant l'Execution Plan)**
10. Poursuivre la Phase 4 (progression/choix de classe) et la connecter au client en direct.
11. Phase 5 (monde/alliances/activités) hors chat.
12. Phase 6 (économie commerciale), une fois les fondations P0/P1 stabilisées.

---

## 6. Compréhension fonctionnelle (état réel du code, même incomplet)

- **Abeilles** : comportement bas niveau dans `AI` (machine à états Idle/Harvesting/Building/Guarding/Exploring/Waiting/Dead, mise à jour étalée par tick pour viser 50k+ abeilles) ; couche stratégique/attributaire dans `Population` (décision, castes, génétique, besoins, personnalité, intelligence collective) ; cycle de vie (naissance→mort) géré par `Hive/BeeLifecycleManager`.
- **Ruche** : `Hive/HiveManager` + `HiveAggregate` détiennent l'identité de la ruche (reine, abeilles, bâtiments, inventaire, capacité) et publient des évènements (`HiveCreated`, etc.) via le moteur de simulation `Core`. `QueenManager`/`QueenAggregate` gèrent le cycle de vie complet de la reine.
- **Bâtiments** : `Buildings` gère placement/file/validation/priorité/amélioration/dépendances ; `Chambers` gère l'agencement interne (chambres, couloirs, intégrité structurelle).
- **Économie** : `Economy/ResourceFlowManager` est le seul point de passage légitime pour muter le stockage (rien ne doit toucher l'inventaire directement) — collecte manuelle obligatoire par design.
- **Combat** : l'assembly client `Combat` est vide. La logique réelle vit dans `Networking` (`HivePerimeterSortieClient`) côté client et dans `Server/HiveOperations` (doctrine, recrutement, réservation d'escouade, sortie de périmètre) côté serveur — testée et fonctionnelle en tests automatisés, mais **derrière des feature flags fermés**, donc non jouable en l'état par un vrai joueur.
- **Alliances** : conçues au niveau design (territoires d'alliance, plafond de 100 membres/alliance dans le modèle MMO), mais non implémentées — seulement des endpoints "readiness" explicitement non-live.
- **Ressources** : cf Économie, plus l'écosystème du module `World` (fleurs/pollinisation, eau, météo/saisons, régénération).
- **Carte** : module `World` (génération, grille hexagonale) + le système de carte monde 50×50 figé (Wave6), verrouillé sur `WorldMapWave6Wave5Method12288Preview.unity` — visuellement fonctionnel et sans couture, mais le contenu gameplay (ruches, ressources, bestiaire visibles sur la carte) reste à re-prouver comme calques par-dessus ce terrain figé.
- **IA** : comportement individuel (`AI`) + couches stratégiques collectives (`Population` : intelligence collective, coordination multi-agents, communication d'essaim, stratégie de colonie) — étendue en code, mais son intégration bout-en-bout avec une partie jouable réelle n'est pas clairement démontrée dans les preuves de démo.
- **Sauvegardes** : le vrai moteur (`SaveEngine`, `SaveManager`, snapshots, migrations) vit dans `Core/Save` ; le module top-level `Save` ne contient que des frameworks de gouvernance/readiness de la persistance — même piège de nommage que Colony/Simulation.
- **Multijoueur** : serveur .NET séparé, REST HTTP majoritairement, un seul canal temps réel réel (chat SignalR). Auth maison. Simulation instanciée par colonie (pas de monde partagé synchronisé en continu). "Not Production Ready" est répété explicitement dans les runbooks.
- **Interface** : le vrai code de productisation UI vit dans `Colony` (pas dans `UI`, qui est vide) ; les présentateurs concrets de scène vivent dans `Playground` (couche de présentation Unity, hors asmdef modulaire).

---

## 7. Prise de fonction

À partir de ce rapport, je prends la responsabilité technique du développement de Bee Kingdom
en continuité complète avec le travail déjà effectué : aucune réécriture de système existant,
aucune réorganisation non justifiée, respect des fondations protégées (carte 50×50, image de
base de la ruche) et du périmètre `Communication` sur le chat. Toute nouvelle fonctionnalité
sera précédée d'une analyse d'impact et de dépendances ; toute décision durable sera documentée
dans les documents produit correspondants, en cohérence avec les conventions déjà en place dans
ce dépôt.
