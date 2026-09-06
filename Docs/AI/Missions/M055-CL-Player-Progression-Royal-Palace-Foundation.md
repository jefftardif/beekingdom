# M055-CL — Player Progression & Royal Palace Level Foundation (Alpha)

Date : 2026-09-06
Agent : Claude Code
Portée : fondation de progression serveur-authoritative autour du Palais Royal.
**Aucun commit, aucun push, aucun déploiement.** Tout est laissé en modifications de
working tree pour revue du CEO.

---

## 1. État initial réellement trouvé

### 1.1 L'identifiant interne réel du Palais Royal

Le point le plus important de l'inspection. Le Palais Royal **existait déjà côté serveur**,
mais sous un autre nom — c'est pour ça qu'une recherche `royal.?palace` dans `Server/src`
ne renvoyait rien.

| Couche | Identifiant |
|---|---|
| Unity — type de bâtiment | `BuildingTypes.RoyalPalace` = `"ROYAL_PALACE"` |
| Unity — clé « legacy » | `BuildingLegacyKeys.AdministrationCore` = `"administration_core"` |
| **Serveur + persistance** | **`administration_core`** |
| FTUE — cible | `"building.administration_core"` |
| Nom produit FR | « Palais Royal » / « Cœur royal » |

La traduction entre les deux est faite par `BuildingMappingTable`
(`ROYAL_PALACE` ↔ `administration_core`).

### 1.2 La source de vérité du niveau existait déjà

`administration_core` est un bâtiment **ordinaire** du catalogue générique d'améliorations :

- niveau stocké dans `PlayerHiveState.BuildingLevels["administration_core"]` ;
- niveau avancé par `BuildingUpgradeService.CompleteAsync` ;
- coûts / durées dans `BuildingUpgrades.Catalog` (appsettings), paliers 1→6 déjà présents ;
- endpoints `GET/POST /game/v1/hives/{hiveId}/building-upgrades[...]` ;
- côté Unity, `HiveViewProductUiPresenter.CoeurRoyalLevel()` lit ce même niveau et alimente
  **déjà** le profil joueur (lignes de profil « Niveau », « Cœur royal N »), le badge
  « COEUR N. », et les déblocages d'abeilles championnes.

**Conclusion structurante : il ne fallait créer aucun compteur de niveau.** M055 se greffe
sur celui qui existe.

### 1.3 Ce qui manquait réellement

Un commentaire du code disait déjà explicitement le trou :

> « No prerequisite redirect exists on the official path today (BuildingUpgradeService only
> checks the single building's own level, not a cross-building Coeur Royal cap) »

Concrètement, avant M055 :

- **aucun prérequis inter-bâtiments** n'existait côté serveur ;
- une notion de prérequis existait **uniquement en mode preview local** (plafond « aucun
  bâtiment ne dépasse le Cœur royal »), donc non-authoritative et invisible pour un vrai compte ;
- aucune notion de **déblocage** attaché à un niveau ;
- aucune manière pour le FTUE de demander « RoyalPalaceLevel >= X » ;
- la fenêtre du Palais Royal affichait le niveau + un bouton « Améliorer », sans prochain
  niveau, sans conditions, sans coûts détaillés, sans déblocages.

---

## 2. Architecture retenue

Principe directeur : **étendre l'existant, ne pas construire une architecture parallèle.**

- Le **niveau** reste `BuildingLevels["administration_core"]`. Aucun second compteur.
- Les **coûts et durées** restent exclusivement dans `BuildingUpgrades.Catalog`.
  La nouvelle configuration ne les duplique pas.
- La nouvelle section `RoyalPalaceProgression` ne porte **que ce qui n'existait pas** :
  prérequis inter-bâtiments, déblocages, description de niveau.
- Les **timers, files, pulses et validation au clic** restent ceux de HiveMap :
  aucun nouveau système de timer n'a été créé. Une amélioration de Palais Royal est une
  `HiveOperation` de type `BuildingUpgrade` comme les autres, donc elle hérite
  automatiquement du pulse bleu de construction, du badge « prêt à valider », de la
  complétion au clic, de la file, et de l'éligibilité Alliance Help.
- La progression est **fail-open** : section absente ou `Enabled: false` ⇒ comportement
  strictement identique à avant M055.

### Modèle data-driven

```
RoyalPalaceLevelDefinition {
  Level                 // niveau ATTEINT (amélioration N-1 → N)
  UpgradeRequirements[] // { BuildingKey, MinimumLevel }
  Unlocks[]             // { Key, Description, Enforced }
  Description
}
```

`Enforced` est délibéré et honnête : `true` = règle réellement appliquée par du code
existant aujourd'hui ; `false` = vitrine de progression, pas encore imposée. L'UI affiche
« (à venir) » pour les seconds — on n'annonce jamais un système qui n'existe pas.

---

## 3. Fichiers créés / modifiés

### Créés

| Fichier | Rôle |
|---|---|
| `Server/src/BeeKingdom.HiveOperations/RoyalPalaceProgression.cs` | Modèle, options, validation, évaluation, autorité prérequis |
| `Server/tests/BeeKingdom.Tests/RoyalPalaceProgressionTests.cs` | 14 tests serveur |
| `Assets/BeeKingdom/Tutorial/Runtime/FtueRoyalPalaceConditions.cs` | Condition `RoyalPalaceLevel >= X` pour le FTUE |
| `Assets/BeeKingdom/Playground/Editor/RoyalPalaceProgressionPresentationTests.cs` | 10 tests Unity EditMode |
| `Docs/AI/Missions/M055-CL-Player-Progression-Royal-Palace-Foundation.md` | Ce rapport |

### Modifiés

| Fichier | Nature |
|---|---|
| `Server/src/BeeKingdom.HiveOperations/BuildingUpgradeContracts.cs` | Paramètre optionnel de progression + contrôle des prérequis dans `StartAsync` + vue dans le snapshot |
| `Server/src/BeeKingdom.Server/Program.cs` | Enregistrement des options + injection dans les 3 endpoints |
| `Server/src/BeeKingdom.Server/appsettings.json` | Paliers palais 6→10 (additifs) + section `RoyalPalaceProgression` |
| `Server/src/BeeKingdom.Server/appsettings.Production.json` | Idem (fichier de config du repo — **rien n'a été déployé**) |
| `Assets/BeeKingdom/Networking/HiveBuildingUpgradeClient.cs` | DTO de progression + validation additive bornée |
| `Assets/BeeKingdom/Playground/HiveBuildingUpgradePresentation.cs` | Modèles de progression, `RoyalPalaceLevel()`, `PrerequisitesSatisfied()`, code d'erreur |
| `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` | **Bloc purement additif** de méthodes `RoyalPalace*ForExternalHost` |
| `Assets/BeeKingdom/Playground/HiveMapRoyalPalaceBootstrap.cs` | Nouvelle UX de la fenêtre |

**Travail des autres agents préservé.** Les fichiers déjà modifiés par d'autres agents
(`BuildingInteractionController.cs`, les `HiveMap*Bootstrap.cs` de UI, `LivingHiveMenuCanvas.cs`,
`Cinzel-Regular SDF.asset`, `Claude_Continuation.md`, `EditorBuildSettings.asset`, les
nouveaux fichiers `BuildingActivityPulse*`, `HiveMapResearchVisualStateBootstrap`, les
rapports `M04*`/`M05*`) n'ont **pas** été touchés. Les deux seuls fichiers partagés que
j'ai édités (`HiveViewProductUiPresenter.cs`, et les fichiers d'upgrade) l'ont été de
façon strictement additive et chirurgicale, jamais par réécriture.

---

## 4. Niveaux Alpha configurés

Palais Royal **1 → 10**, comme demandé. Les autres bâtiments plafonnent à 6 dans le
catalogue existant, donc **aucun prérequis Alpha ne dépasse le niveau 5** — la progression
reste entièrement atteignable sans toucher au reste du catalogue.

| Niveau | Prérequis | Coût (miel / cire) | Durée | Déblocage |
|---|---|---|---|---|
| 1 | — | — | — | — |
| 2 | aucun | 1 944 / 502 | 3 min | Améliorations coordonnées *(à venir)* |
| 3 | Nursery 2 | 2 088 / 564 | 4 min | **Championnes rares** *(réellement imposé)* |
| 4 | Caserne 2, Nursery 3 | 2 232 / 626 | 5 min | Aide d'alliance *(à venir)* |
| 5 | Recherche 2, Réserve de miel 3 | 2 376 / 688 | 6 min | Recherche soutenue *(à venir)* |
| 6 | Académie 3, Caserne 3 | 2 520 / 750 | 7 min | Entraînement soutenu *(à venir)* |
| 7 | Entrepôt 4, Nursery 4 | 2 664 / 812 | 9 min | Expéditions prolongées *(à venir)* |
| 8 | Recherche 4, Caserne 4 | 2 808 / 874 | 11 min | Recherche d'alliance *(à venir)* |
| 9 | Banque 5, Académie 5 | 2 952 / 936 | 13 min | Patrouilles étendues *(à venir)* |
| 10 | Hall d'alliance 5, Recherche 5, Caserne 5 | 3 096 / 998 | 15 min | **Championnes légendaires** *(réellement imposé)* |

Notes :

- Les **coûts/durées 1→6 existants n'ont pas été modifiés** (consigne : conserver l'existant).
  Seuls les paliers 6→10 sont nouveaux, en prolongeant exactement la courbe arithmétique
  déjà en place (+144 miel, +62 cire, +2 min par palier).
- Durées volontairement courtes (max 15 min) pour que les tests humains restent possibles.
- Les deux déblocages marqués « réellement imposé » ne sont pas inventés : ils correspondent
  à `ChampionBeeCatalog.RareUnlockCoeurRoyalLevel = 3` et
  `LegendaryUnlockCoeurRoyalLevel = 10`, **déjà appliqués** par le code existant. M055 ne
  fait que les déclarer dans la table.

---

## 5. Comportement serveur (autorité)

Dans `BuildingUpgradeService.StartAsync`, pour `administration_core` uniquement, la
vérification des prérequis est faite **avant tout débit de ressources et avant toute
création d'opération**. En cas d'échec : code `game.royal_palace_prerequisites`, et
**aucune mutation** (revision inchangée, ressources inchangées, aucune opération).

Le serveur vérifie donc : niveau courant, prérequis, ressources, chantier déjà en cours,
règles d'upgrade existantes, idempotence et concurrence (revision + receipts) — tous
préexistants sauf les prérequis, qui sont l'ajout de M055.

Le client ne peut pas contourner : `StartAsync` est le seul chemin, les endpoints REST
y appellent directement. Un client modifié qui poste directement la requête reçoit le
même refus.

Le snapshot de lecture expose `RoyalPalace` : niveau courant, niveau suivant, max configuré,
prérequis avec état satisfait/manquant, raison de blocage, bâtiment bloquant, déblocages.
Il est évalué à partir des **mêmes** niveaux que ceux renvoyés au client, donc la fenêtre
ne peut pas afficher une règle différente de celle qui sera imposée.

---

## 6. Comportement Unity

La fenêtre du Palais Royal (`HiveMapRoyalPalaceBootstrap`) affiche désormais :

1. **Niveau actuel** en titre (« Palais Royal — Niveau N ») + rappel que c'est le niveau de
   la colonie + mention « Équilibrage Alpha provisoire ».
2. **Prochain niveau** avec sa description narrative.
3. **Conditions**, une ligne par prérequis, visuellement distinctes :
   `✓` vert quand satisfait, `✕` rouge quand manquant, avec niveau requis et niveau actuel.
4. **Coût et durée réels** du palier, lus dans l'offre serveur existante.
5. **Déblocages du prochain niveau**, avec mention « (à venir) » pour ceux qui ne sont pas
   encore réellement imposés.
6. **Raison exacte du blocage** — jamais un simple bouton grisé — plus un bouton
   « Voir le bâtiment requis » qui réutilise le mécanisme de mise en avant déjà existant
   (`highlightedPrerequisiteBuildingType`). Aucun système de caméra/tutoriel n'a été créé.

La fenêtre est passée en zone défilante (le contenu déborde sur mobile).

Le **profil joueur n'a pas été refait** et lit toujours `CoeurRoyalLevel()`, c'est-à-dire
exactement la même source de vérité que la nouvelle fenêtre. Vérifié par lecture du code :
aucun second système.

---

## 7. Compatibilité avec les comptes existants (critique)

Aucun compte n'est reset, aucune donnée de production n'a été modifiée, aucun déploiement
n'a été fait. La compatibilité est assurée par construction, pas par migration :

1. **Les prérequis sont des minimums.** Un compte de test dont les bâtiments dépassent
   largement les seuils Alpha les satisfait trivialement. Testé.
2. **Aucun prérequis n'est ajouté aux autres bâtiments.** Le reste du catalogue se comporte
   exactement comme avant. Testé.
3. **Un compte dont le Palais Royal dépasse déjà la table** n'est ni rétrogradé ni déclaré
   invalide : il constate simplement qu'il n'y a plus de palier configuré devant lui. Testé.
4. **Aucun système actuellement accessible n'est soudainement verrouillé.** Les déblocages
   sont déclaratifs (`Enforced: false`) sauf les deux qui étaient déjà imposés avant M055.
5. **Fail-open** : progression absente/désactivée ⇒ comportement d'avant M055.
6. **Contrat réseau additif** : `RoyalPalace` est un champ optionnel en fin de contrat.
   Un snapshot mis en cache avant M055 (donc sans ce bloc) reste valide et exploitable.
   Testé.
7. **Aucun changement de schéma de persistance.** `BuildingLevels` était déjà là.

---

## 8. Tests exécutés et résultats exacts

### Serveur — `dotnet test`

- **Nouveaux : `RoyalPalaceProgressionTests` — 14/14 réussis.**
- `BeeKingdom.HiveOperations.Tests` : **181/181 réussis**.
- `BeeKingdom.Tests` (suite complète, exécution finale) : **616 réussis, 8 ignorés, 1 échec**.

L'échec restant est **préexistant et étranger à M055** :

| Test | Diagnostic |
|---|---|
| `CatalogSqlMatchesCheckedInScriptFiles` | Échoue de façon déterministe. Le fichier `091_alliance_help.sql` du working tree (travail Alliance Help non commité d'un autre agent) diffère du SQL attendu. **Vérifié : échoue aussi avec `RoyalPalaceProgression` désactivé.** |
| `AllianceAnnouncementRequiresLeaderRoleAndFanOutParticipants` | Instable selon l'ordre d'exécution. Échouait sur une exécution, **réussit sur l'exécution finale et en isolation avec la progression activée**. Flake préexistant, non causé par M055. |

Régression : un état intermédiaire de mon implémentation faisait échouer 132 tests
(les définitions imbriquées n'étaient pas liables depuis la configuration, ce qui faisait
échouer la validation au démarrage). Corrigé — les types de configuration sont désormais
des classes à propriétés settables. Le détail est documenté en commentaire dans le code
pour éviter que quelqu'un ne « re-simplifie » ça en records positionnels plus tard.

### Unity — EditMode

Compilation Unity propre (aucune erreur console après refresh).

| Classe | Résultat |
|---|---|
| **`RoyalPalaceProgressionPresentationTests` (nouveau)** | **10/10 réussis** |
| `HiveBuildingUpgradeClientTests` | 15/15 réussis |
| `BuildingMappingTableTests` | 17/17 réussis |
| `BuildingCatalogTests` | 15/15 réussis |
| `SandboxLivingHiveBuildingUpgradeTests` | 9/10 — 1 échec préexistant |
| `SandboxLivingHiveUiStabilizationTests` | 20/22 — 2 échecs préexistants |

Les 3 échecs Unity sont **préexistants et étrangers à M055** :

| Test | Diagnostic |
|---|---|
| `MonotonicProjectionNeverAuthorizesCompletion` | `Assert.That(model.Remaining(...), Is.Zero)` : NUnit compare un `TimeSpan` à l'entier 0 (« Expected: 0 / But was: 00:00:00 »). Le fichier de test **et** la méthode `Remaining()` sont **inchangés** (vérifié : absents de mon diff, fichier non modifié dans `git status`). Problème de sémantique d'assertion, sans lien avec la progression. |
| `ClosingAllianceProfileReleasesCapturedGuiControls` | Chemin **profil de membre d'alliance**. Le fichier de test est en cours de modification non commitée par un **autre agent** (travail de stabilisation UI en vol, avec `HiveMapUiOcclusion.cs` / `HiveMapOverlayInputGateBootstrap.cs`). |
| `WorldInputIsBlockedWhileAnyFullScreenIsOpen` | Idem : échoue sur l'étape « profil d'alliance ». Même travail en vol du même agent. |

Mon ajout dans `HiveViewProductUiPresenter.cs` est une **pure addition de méthodes
statiques** : il n'introduit aucun état d'overlay et ne touche à aucun code de profil
d'alliance ni à `Remaining()`.

⚠️ **Limite de vérification à signaler honnêtement** : la suite EditMode **complète**
(1 543 tests) n'a pas pu être menée à son terme — elle dépasse le délai d'inactivité du
canal MCP, et la tentative en batchmode CLI a échoué parce que l'éditeur Unity détenait le
verrou du projet. J'ai donc validé **par classes ciblées** couvrant tout ce que M055
touche. Une passe EditMode complète (éditeur fermé, `-runTests` **sans** `-quit`) reste
souhaitable avant l'Alpha.

---

## 9. Ce qui a été vérifié programmatiquement vs. ce qui demande vos yeux

**Vérifié programmatiquement :** l'autorité serveur, le refus des prérequis sans mutation,
l'impossibilité de contourner depuis le client, le coût réel appliqué, l'absence de double
consommation au retry, la persistance du niveau et sa relecture, la cohérence d'un compte
au-dessus des seuils, la cohérence d'un compte au-dessus du maximum configuré, la tolérance
d'un snapshot sans progression, la projection des prérequis satisfaits/manquants, la
distinction déblocage imposé vs. annoncé.

**Non vérifié — demande votre œil :** le rendu réel de la fenêtre en Play Mode (lisibilité,
débordement, couleurs vert/rouge, taille de police sur mobile), le parcours complet
d'amélioration avec pulse/file/validation au clic contre le vrai serveur, et le
comportement du bouton « Voir le bâtiment requis » dans la scène.
**Je ne déclare pas M055 validée visuellement à votre place.**

---

## 10. Dettes et limitations

1. **Passe EditMode Unity complète non terminée** (voir §8). C'est la limitation la plus
   importante à lever.
2. **Textes de progression en configuration serveur, en français.** L'UI passe par
   `BeeLocalization` avec le texte serveur en repli (clés `royal_palace.*` et
   `royal_palace.unlock.*`). Les entrées de localisation restent à ajouter proprement.
3. **`CatalogVersion` volontairement non incrémentée** (`hive-gameplay-sprint-v1`) pour
   minimiser le risque ; le contrat client ne l'épingle pas. À rediscuter.
4. **Déblocages majoritairement déclaratifs.** C'est un choix explicite de sécurité pour les
   comptes existants, pas un oubli. Les promouvoir en règles imposées est une décision de
   game design qui vous revient.
5. **Le plafond « aucun bâtiment ne dépasse le Cœur royal » du mode preview local n'a pas
   été porté côté serveur.** Ce serait un vrai changement de game design touchant tous les
   bâtiments et tous les comptes existants — hors périmètre M055.
6. **8 tests serveur ignorés** : préexistants, non touchés.

---

## 11. Décisions de game design temporaires (Alpha, à valider par vous)

- La table de prérequis §4 est une **proposition Alpha représentative**, pas un équilibrage.
  Elle est marquée `IsAlphaBalance: true` dans la configuration et affichée comme telle au joueur.
- Durées 3→15 min : choisies pour la testabilité humaine, pas pour l'économie finale.
- Palais Royal plafonné à 10 : conforme à la demande, et cohérent avec le catalogue existant
  (les autres bâtiments plafonnant à 6, aucun prérequis ne dépasse 5).
- Le niveau 2 n'a **aucun** prérequis, délibérément : la première amélioration doit rester
  immédiate pour le FTUE.

---

## 12. Scénario de validation CEO

Prérequis : un compte de test avec suffisamment de miel/cire, serveur local lancé avec la
configuration mise à jour.

1. Ouvrir HiveMap et cliquer le **Palais Royal**.
2. Constater **« Palais Royal — Niveau N »** et la mention « Équilibrage Alpha provisoire ».
3. Constater la section **« Prochain niveau — Niveau N+1 »** avec sa description.
4. Constater les **conditions** : lignes `✓` vertes (satisfaites) et `✕` rouges (manquantes),
   avec niveau requis et niveau actuel.
5. Constater le **coût** et la **durée** réels affichés.
6. Constater les **déblocages** annoncés, dont ceux marqués « (à venir) ».
7. Sur une condition manquante : constater la **raison explicite en rouge** (ex. « Nursery
   niveau 3 requise ») et non un simple bouton grisé.
8. Cliquer **« Voir le bâtiment requis »** → la fenêtre se ferme et le bâtiment concerné est
   mis en avant.
9. **Améliorer le bâtiment requis** jusqu'au niveau demandé (chantier normal : pulse bleu,
   file, validation au clic).
10. Rouvrir le Palais Royal → la condition est passée au **`✓` vert** et le bouton
    d'amélioration est actif.
11. **Lancer l'amélioration** du Palais Royal → constater le débit des ressources, le
    **pulse bleu de construction**, l'entrée en **file**, et le **timer**.
12. À échéance, constater l'**indicateur de fin** et **valider au clic** sur le bâtiment.
13. Constater le **nouveau niveau** dans la fenêtre **et dans le profil joueur** (les deux
    doivent afficher la même valeur — c'est le point à vérifier en priorité).
14. **Se déconnecter et se reconnecter** → confirmer que le niveau a persisté.
15. Optionnel (anti-triche) : avec un compte dont un prérequis manque, poster directement
    `POST /game/v1/hives/{hiveId}/building-upgrades/administration_core/start` →
    doit renvoyer `409 game.royal_palace_prerequisites` sans rien débiter.

---

M055 IMPLEMENTED — READY FOR CEO ROYAL PALACE PROGRESSION TEST.
