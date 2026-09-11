# M052B-CL — Alliance Research Deployment + CEO Certification Prep

## Contexte

M052 (Alliance Research Bible Alignment) était techniquement accepté. Cette
mission couvrait exclusivement le commit, le push et le déploiement du
travail M052, puis la préparation de la production pour une certification
humaine contrôlée par le CEO — **aucune certification n'a été effectuée par
Claude**.

## 1. Isolation du worktree partagé

Le répertoire de travail contenait du travail non lié en cours (confirmé par
`git status`/`git diff` avant tout `git add`) : `BuildingInteractionController.cs`,
des bootstraps HiveMap (`HiveMapBuildingUpgradeVisualStateBootstrap.cs`,
`HiveMapOverlayInputGateBootstrap.cs`, `HiveMapQueueSidebarBootstrap.cs`,
`HiveMapResourceHudBootstrap.cs`, `HiveMapRuntimeBootstrapInitializer.cs`,
`HiveMapUiOcclusion.cs`), `LivingHiveMenuCanvas.cs`, la police Cinzel,
`EditorBuildSettings.asset`, des tests de clic (`BuildingInteractionControllerClickPriorityTests.cs`,
`SandboxLivingHiveUiStabilizationTests.cs`), des fichiers non suivis
(`BuildingActivityPulse.cs`, `HiveMapResearchVisualStateBootstrap.cs`) et
plusieurs rapports de mission antérieurs non commités (M047-CX, M048-CL,
M049-CL, M049B-CL, M049C-CL, M051B-CL).

`Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` contenait un
mélange réel : 5 hunks appartenant à M052 (l'onglet Recherches d'Alliance,
lignes ~36767-37060) entremêlés avec 24 hunks non liés (overlay
Communication, contenu Baraquement, panneau de complétion de construction,
HUD stratégie, panneau de profil joueur, menu Recherche personnelle, mini
chat, etc.). Isolé via extraction manuelle des hunks (`git diff` →
sélection des 5 blocs `@@` pertinents → fichier `.patch` → `git apply
--cached --check` puis `--cached`). Vérifié après coup : `git diff --cached`
montrait exactement les 5 hunks M052 ; `git diff` (non indexé) montrait les
24 hunks non liés intacts, toujours présents dans l'arbre de travail.

Aucune commande destructive utilisée (`git add .`, `git add -A`,
`git reset --hard`, `git checkout .`, `git restore .`, `git clean`,
`git stash` d'un fichier non lié) — chaque fichier a été ajouté
explicitement par chemin, ou par patch de hunk précis.

## 2. Vérifications pré-déploiement

1. Tests serveur focalisés `AllianceResearchServiceTests` : **29/29 verts**
   (`dotnet test --filter FullyQualifiedName~AllianceResearchServiceTests`).
2. Tests Unity `AllianceResearchClientTests` : **8/8 verts** (Play Mode
   confirmé inactif via une sonde `script-execute` en lecture seule avant
   exécution).
3. Compilation Unity : **propre** (`assets-refresh`, 0 erreur) après
   isolation des hunks.
4. Aucune migration SQL : confirmé par `git diff --cached --stat --
   Server/src/BeeKingdom.Database/Scripts/` (vide) et par le journal du
   pipeline de déploiement (0 mention de "migration").
5. Architecture du feature flag `AllianceResearch:Enabled` inchangée :
   confirmé par `git diff --cached -- AllianceResearchOptions.cs` — seul
   ajout, `AllianceCurrencyPerContributionPoint` ; le champ `Enabled` et sa
   sémantique sont identiques à M051/M051B.
6. Aucun état Alliance de production muté à ce stade — aucune commande de
   mutation exécutée.
7. `BIBLE_ALLIANCE_RESEARCH.md` présent et lu intégralement lors de M052
   (confirmé toujours présent à `C:\projets\beekingdom\BIBLE\BIBLE_ALLIANCE_RESEARCH.md`,
   hors de ce dépôt — voir section 4).

## 3. Commit

18 fichiers commités (16 fichiers d'implémentation/tests M052 + le rapport
M052 + la mise à jour de `Docs/Claude/Claude_Continuation.md`) :

```
Assets/BeeKingdom/Networking/AllianceClient.cs
Assets/BeeKingdom/Playground/AllianceCenterPresentation.cs
Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs   (5 hunks M052 seulement)
Assets/BeeKingdom/Tests/Editor/AllianceResearchClientTests.cs
Assets/_Project/Data/Localization/Resources/Localization/strings.fr-CA.json
Docs/AI/Missions/M052-CL-Alliance-Research-Bible-Alignment.md
Docs/Claude/Claude_Continuation.md
Server/src/BeeKingdom.Alliance/Models/AllianceActivity.cs
Server/src/BeeKingdom.Alliance/Research/AllianceResearchBonusResolver.cs
Server/src/BeeKingdom.Alliance/Research/AllianceResearchCatalog.cs
Server/src/BeeKingdom.Alliance/Research/AllianceResearchModels.cs
Server/src/BeeKingdom.Alliance/Research/AllianceResearchOptions.cs
Server/src/BeeKingdom.Alliance/Research/AllianceResearchService.cs
Server/src/BeeKingdom.Alliance/Research/AllianceResearchSpeedUpCatalog.cs (nouveau)
Server/src/BeeKingdom.Alliance/Research/AllianceResearchStateMigrator.cs (nouveau)
Server/src/BeeKingdom.Alliance/Research/SqlAllianceResearchRepository.cs
Server/src/BeeKingdom.Server/Program.cs
Server/tests/BeeKingdom.Tests/AllianceResearchServiceTests.cs
```

Hash : `2e673d78eff0560fd296e62381eda81d1f4c0ba7` — message
`feat(alliance): align research with canonical progression bible`.

Après le commit, `git status --short` confirme que tous les fichiers non
liés listés en section 1 restent modifiés/non suivis, exactement comme
avant — aucun n'a été touché.

## 4. Bible canonique

`C:\projets\beekingdom\BIBLE\BIBLE_ALLIANCE_RESEARCH.md` vit dans
`C:\projets\beekingdom\`, un répertoire **distinct** de la racine du dépôt
Git (`C:\projets\beekingdomgame-master\`, confirmé via `git rev-parse
--show-toplevel`). Elle n'est donc **pas** versionnée dans ce dépôt et rien
n'a été ajouté au commit à ce sujet — rien à faire ici, conforme à la
mission ("if the Bible is inside the repository and not yet versioned").

## 5. Push / Déploiement

- `git push origin main` : réussi (`48e8bec5..2e673d78 main -> main`).
- `git push origin main:deploy` : réussi (`48e8bec5..2e673d78 main -> deploy`),
  déclenchant le workflow GitHub Actions `Deploy BeeKingdomApi`
  (run `33919421032`).
- Workflow : **succès** (`gh run watch --exit-status`). Étapes confirmées
  dans le journal : site mis hors ligne (`app_offline.htm` + `Stop-WebAppPool`),
  fichiers publiés copiés, site remis en ligne (`Start-WebAppPool`), test
  de fumée réussi. **Zéro mention de "migration"** dans le journal complet
  du run — aucune migration n'a été demandée ni exécutée, conforme à
  l'attente de la mission.

## 6. Feature flag

Aucune commande `appcmd`/IIS n'a été exécutée pendant cette mission — la
configuration de l'app pool (incluant la variable d'environnement
`AllianceResearch__Enabled`, activée à `true` lors de M051B et confirmée
par le CEO à l'époque) n'a **pas été touchée**. Le déploiement ne fait
qu'un remplacement de fichiers + recyclage du pool (confirmé par le journal
du workflow), ce qui ne réinitialise jamais les variables d'environnement
de l'app pool. Le drapeau reste donc, par construction, dans l'état laissé
par M051B : activé. Aucun autre drapeau (Alliance War, PvP, Diplomatie) n'a
été touché ou consulté.

## 7. Santé post-déploiement

```
GET https://api-ops.beekingdomgame.com/health -> 200
{"service":"BeeKingdom.Server","status":"Healthy", ...}
```

## 8. Vérifications de production en lecture seule

- `GET /alliance/v1/alliances/search?nameOrTag=BKT` (public, sans
  authentification) : **Alliance Test [BKT] existe toujours**,
  `memberCount: 2`, `joinMode: InviteOnly`.
- `GET /alliance/v1/alliances/{allianceId}` et `/by-slug/alliance-test`
  (public) : `leader.displayName: "Stara"` — **Stara reste Chef**. Statut
  `Active`, `createdAtUtc` inchangé depuis M051B, aucune guerre/diplomatie
  active (`activeWarCount: 0`).
- `GET /alliance/v1/research` (sans jeton) : `401 alliance.session_required`
  — confirme la route M052 est bien déployée et vivante (pas de 404), sans
  révéler ni muter d'état.
- `POST /alliance/v1/research/funding-target` (sans jeton, corps vide) :
  `401 alliance.session_required` — même confirmation pour la nouvelle
  route de sélection de cible, aucune mutation possible sans session
  valide.
- **Limite honnête** : aucune session Unity/joueur authentifiée n'était
  disponible pendant cette mission (Play Mode confirmé inactif, le CEO
  s'est absenté de l'écran avant le déploiement) — je n'ai donc **pas**
  effectué de lecture authentifiée du snapshot complet Alliance Research
  (rôle exact de Jeff = Officier, contenu Minor/Major détaillé,
  contributions). Je n'ai pas simulé ni fabriqué ce résultat. Cette lecture
  authentifiée est de toute façon la première étape naturelle de la
  certification du CEO (connexion en tant que Jeff) — elle servira donc de
  première preuve live, faite par le CEO lui-même.
- Aucune commande de mutation n'a été exécutée à aucun moment : pas de
  sélection de cible, pas de don, pas de lancement, pas de SpeedUp, pas de
  changement de rôle/membre.

## Checklist finale

A. M052 hunks isolated safely? **YES**
B. Unrelated working-tree changes preserved? **YES**
C. Bible versioned in repository? **NO** — vit hors du dépôt à `C:\projets\beekingdom\BIBLE\BIBLE_ALLIANCE_RESEARCH.md`
D. Focused server tests 29/29? **YES**
E. Unity tests 8/8? **YES**
F. Unity compile green? **YES**
G. SQL migration required? **NO**
H. Commit created? **YES** — `2e673d78eff0560fd296e62381eda81d1f4c0ba7`
I. Push successful? **YES**
J. Deployment successful? **YES** — run `33919421032`, succès
K. /health Healthy? **YES**
L. AllianceResearch enabled? **YES** (état préservé — aucune config IIS touchée par cette mission, hérité de M051B)
M. Alliance Test preserved? **YES** — 2 membres, InviteOnly, Active
N. Stara still Chef? **YES** — confirmé `leader.displayName: "Stara"`
O. Jeff still Officier? **NON VÉRIFIÉ DIRECTEMENT** — le endpoint public ne révèle pas les rôles individuels des membres (limite déjà documentée en M051B) ; `memberCount: 2` inchangé est cohérent avec sa présence continue, mais son rôle exact n'a pas pu être lu sans session authentifiée
P. New Minor/Major Research contract live? **PARTIELLEMENT** — routes confirmées déployées et actives (401, pas 404) ; contenu du snapshot non lu faute de session authentifiée disponible
Q. Production research state remained untouched? **YES**
R. No donation performed? **YES**
S. No funding target selected? **YES**
T. No research launched? **YES**
U. No SpeedUp consumed? **YES**
V. READY FOR CEO HUMAN CERTIFICATION? **YES**

READY FOR CEO ALLIANCE RESEARCH CERTIFICATION — START WITH JEFF (OFFICIER). NO PRODUCTION RESEARCH STATE MUTATED.
