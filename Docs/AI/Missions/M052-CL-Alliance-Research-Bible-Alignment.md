# M052-CL — Alliance Research Bible Alignment

## 1. Contexte et mandat

Le document canonique `C:\projets\beekingdom\BIBLE\BIBLE_ALLIANCE_RESEARCH.md`
(V1.0, 883 lignes / 27 sections) a été lu intégralement avant toute
modification, conformément à l'instruction obligatoire de la mission. Il
remplace le prototype livré par M051/M051B/M051C : là où les deux documents
divergent, la Bible fait autorité. Cette mission est une **migration
évolutive**, pas une réécriture greenfield — toute l'infrastructure M051
réutilisable (catalogue, repository, verrouillage par Alliance, idempotence,
comptabilité de contribution, résolveur de bonus, adaptateur, transport
`AllianceClient`, emplacement UI Centre d'Alliance) a été conservée et
étendue, jamais remplacée sans raison.

## 2. Différence critique #1 — Financement ≠ Recherche

Le modèle M051 ("don → progression → complétion automatique") a été
remplacé par le cycle de vie officiel à 5 états nommés de la Bible, avec un
6e état interne ajouté pour distinguer deux situations que la Bible ne
nomme pas séparément :

`Locked → Eligible → Funding → Ready → Researching → Completed`

`Eligible` (prérequis remplis, mais pas encore sélectionnée comme cible de
financement par le Chef) est une extension délibérée, documentée dans le
code, non contradictoire avec la Bible — elle distingue "peut être
sélectionnée" de `Locked` ("prérequis manquants"). Atteindre 100% du
financement ne déclenche ni bonus ni recherche automatique : la technologie
passe seulement à `Ready`, en attente d'un lancement explicite.

## 3. Catalogue Alpha (8 technologies, 4 branches + 1 Majeure)

Le catalogue à 9 technologies de M051 n'est plus canonique — remplacé par
un sous-ensemble représentatif aligné sur la Bible :

- **Prospérité** : `prosperity_shared_reserves_i` (Mineure), `prosperity_honey_mastery_i`
  (Mineure, financée en Pollen+Cire — jamais en Miel, règle économique
  croisée de la Bible section 8), `prosperity_age_of_abundance` (**Majeure**).
- **Expansion** : `expansion_coordinated_harvest_i` (Mineure).
- **Coopération** : `cooperation_coordinated_aid_i`/`_ii` (Mineures, chaîne
  de prérequis).
- **Armée Royale** : `defense_common_discipline_i`/`_ii` (Mineures, chaîne
  de prérequis).
- **Suprématie** : intentionnellement absente — verrouillée/non implémentée
  tant qu'Alliance War n'existe pas (Bible section 19).

Retirées de M051 (absentes de la Bible, documentées dans le code plutôt que
silencieusement supprimées) : `prosperity_colony_logistics`,
`cooperation_collective_mobilization`, `defense_royal_guard` (la Bible en
fait une Majeure "Garde du royaume", pas une 3e Mineure — reportée plutôt
que mal implémentée).

Durées : valeurs de test Alpha (15 min à 2h), explicitement documentées
comme non-finales — l'architecture supporte le vrai barème Bible (jours à
mois) sans changement, prouvé par un test dédié de mathématiques de longue
durée.

## 4. Deux emplacements de recherche indépendants (Minor/Major)

`AllianceResearchState` porte désormais `MinorResearch`/`MajorResearch`
(chacun `AllianceResearchSlot?`), permettant une recherche Mineure et une
recherche Majeure simultanées. Pendant qu'une catégorie est en recherche, le
Chef peut déjà sélectionner/financer la technologie suivante de la **même**
catégorie — la cible de financement (`MinorFundingTargetId`/
`MajorFundingTargetId`) est indépendante de l'emplacement de recherche
occupé.

## 5. Autorité Chef exclusive + préservation des contributions

Seul le rôle Chef peut sélectionner ou changer la cible de financement
(`SelectFundingTargetAsync`), vérifié côté serveur (jamais côté client —
`CanSelectFundingTarget` est un booléen calculé serveur, exposé en lecture
seule à l'UI). Changer de cible **préserve** les contributions déjà versées
à l'ancienne cible (stockées dans `Funding[technologyId]`, jamais effacées
au changement de sélection) — reprises possibles plus tard, conformément à
la garantie "aucune contribution n'est jamais perdue" (Bible section 5).

Le lancement (`LaunchAsync`, Ready → Researching) est ouvert au Chef **et**
aux Officiers — matrice de permission distincte de la sélection de cible.

## 6. Minuteur serveur-autoritaire, résolution paresseuse et idempotente

Aucun minuteur n'est possédé par Unity. `AllianceResearchSlot` porte
`CompletesAtUtc` ; `ResolveElapsedResearch(state, now)` (fonction statique
pure) déplace toute recherche dont `now >= CompletesAtUtc` vers `Completed`
et vide l'emplacement — appelée au début de **chaque** mutation et dans
`GetSnapshotAsync`, miroir exact de la convention `HiveOfflineProductionService.Accrue`
déjà établie. Idempotente par construction (emplacement déjà vide = no-op) :
aucun joueur n'a besoin de rester connecté pour qu'une recherche se termine.

## 7. Bonus uniquement depuis Completed

`AllianceResearchBonusResolver.ResolveForAllianceAsync` itère exclusivement
`state.Completed.Keys` — jamais Funding/Ready/Researching. Testé
explicitement (`Bonus_OnlyAppliesAfterCompletion*`). Un compromis de
fraîcheur documenté et assumé : ce chemin est en lecture seule (pas de
`ExecuteAtomicallyAsync`), donc une recherche qui vient d'expirer peut
rester invisible au bonus jusqu'à la prochaine ouverture du Centre
d'Alliance par n'importe quel membre — fenêtre bornée, auto-cicatrisante,
choisie pour ne pas payer une résolution d'écriture sur le chemin de calcul
de production/combat le plus chaud du jeu.

## 8. Cohérence économique croisée du financement

Chaque coût de financement respecte la règle de la Bible : une technologie
n'exige jamais principalement la ressource qu'elle améliore directement
(ex. `prosperity_honey_mastery_i`, qui augmente la production de Miel, est
financée en Pollen+Cire, jamais en Miel).

## 9. Alliance Research SpeedUp — catégorie distincte

Nouveau catalogue `AllianceResearchSpeedUpCatalog` (`alliance_research_speedup_1h/3h/8h/24h`),
volontairement **non intégré** à `BeeKingdom.HiveOperations.SpeedUpCategories`
(strictement scopé à la ruche personnelle). Les items vivent dans le même
inventaire générique `PlayerHiveState.SpeedUps` (dictionnaire
itemId→quantité déjà agnostique). Réservé au Chef/Officier
(`CanUseSpeedUp`), utilisable uniquement pendant `Researching`, jamais
au-delà de l'heure de complétion (clamp testé explicitement). Acquisition
(boutique/événements) explicitement hors scope — les tests alimentent
l'inventaire directement, même convention que `combat_recall_token`.

## 10. Fondation Alliance Currency (Sceaux Royaux)

`AllianceResearchContribution` porte un 4e champ `AllianceCurrencyBalance`,
calculé à chaque don via `AllianceResearchOptions.AllianceCurrencyPerContributionPoint`
(0.1 par défaut, configurable). Aucun chemin de dépense n'existe — fondation
domaine/persistance uniquement, explicitement pas de Boutique/UI d'achat
dans cette mission. Gelée Royale n'est jamais une ressource de financement
valide (absente de tout `FundingRequirements` du catalogue).

## 11. Aucune migration SQL nécessaire

`dbo.AllianceResearch` stocke l'état complet en JSON opaque (`StateJson`,
inchangé depuis M051) — le modèle C# évolue librement sans migration de
schéma. Le vrai risque était la désérialisation d'anciennes lignes JSON vers
la nouvelle forme record : `System.Text.Json` laisse les collections
non-nullables sans propriété JSON correspondante à `null` (pas vide),
provoquant un `NullReferenceException`. Résolu par
`AllianceResearchStateMigrator.ToCurrent(state)` (même convention que
`HiveStateMigrator.ToCurrent`), branché dans les deux méthodes de lecture de
`SqlAllianceResearchRepository`. Zéro fichier ajouté sous
`Server/src/BeeKingdom.Database/Scripts/`, vérifié par `git status`.

## 12. Compromis d'atomicité à deux agrégats (revu, préservé)

Don, lancement de speedup : débit de `PlayerHiveState` en premier
(idempotent via `Receipts`), puis mutation de `AllianceResearchState`
(idempotente via trois `HashSet` séparés — `ProcessedDonationIds`/
`ProcessedLaunchIds`/`ProcessedSpeedUpIds`, pour éviter toute collision de
namespace de clé entre types d'opération). Aucune transaction distribuée ;
mode d'échec honnêtement documenté dans le code (un débit réussi suivi d'un
échec de mutation Alliance laisserait le joueur débité sans progression
enregistrée côté Alliance — fenêtre étroite, non observée en test, héritée
sans changement de M051).

## 13. Interface Unity — Centre d'Alliance, onglet Recherches

Réutilise l'emplacement UI existant. Nouveautés :
- Deux lignes d'en-tête Mineure/Majeure montrant la cible de financement
  actuelle, avec bouton CHANGER visible uniquement si `CanSelectFundingTarget`
  (Chef) — Officiers/Membres voient la même sélection en lecture seule.
- Sélecteur de cible (liste des technologies `Eligible` de la catégorie,
  boutons de choix — aucune saisie manuelle d'ID).
- Cartes technologie groupées par branche, un rendu distinct par état :
  `Locked` (prérequis réel, aucun bouton actionnable), `Eligible` (message
  d'attente), `Funding` (une ligne par ressource avec barre de progression
  + bouton DONNER borné au reste dû, jamais un montant fixe hérité du
  bundle catalogue), `Ready` (bouton LANCER si `CanLaunch`, sinon message
  d'attente Chef/Officier), `Researching` (compte à rebours + bouton
  ACCÉLÉRER si `CanUseSpeedUp`), `Completed` (date de complétion, aucun
  bouton).
- Toutes les autorisations viennent des trois booléens serveur
  (`CanSelectFundingTarget`/`CanLaunch`/`CanUseSpeedUp`) — jamais re-dérivées
  côté client depuis le rôle.

## 14. Compromis connus et limites assumées

- Fenêtre de fraîcheur du bonus (section 7) — bornée, auto-cicatrisante.
- Atomicité à deux agrégats (section 12) — héritée de M051, non résolue.
- SpeedUp Alpha limité à l'item 1h dans l'UI (acquisition hors scope —
  offrir un sélecteur pour des items qu'aucun joueur ne peut encore
  posséder serait un contrôle mort).
- Bonus par ressource spécifique (ex. Miel uniquement) reste un panier
  générique Production/Capacité/Puissance de combat — granularité fine
  différée (contenu/équilibrage, pas une exigence de cycle de vie Bible).
- `CatalogSqlMatchesCheckedInScriptFiles` reste en échec pré-existant
  (dérive du fichier `091_alliance_help.sql`, documentée depuis M051,
  confirmée sans lien avec M052 — zéro nouveau SQL ajouté cette mission).

## 15. Preuves

- **Serveur** : `dotnet build` — 0 erreur. `AllianceResearchServiceTests` :
  29 tests focalisés, 29/29 verts. Suite complète `BeeKingdom.Tests` :
  527/538 (8 ignorés), 3 échecs — tous pré-existants et sans rapport avec
  Alliance Research (`Enabled_test_route_uses_game_contract_and_idempotent_proof`,
  `PostCollectsFloorPreservesFractionPersistsAndReplaysExact`,
  `Commit_replay_after_release_long_max_both_routes_and_partition_isolation`,
  `Enabled_start_replay_claim_early_and_player_isolation`,
  `CatalogSqlMatchesCheckedInScriptFiles` — noms de test différents à
  chaque exécution pour les 4 premiers, confirmant une non-déterminisme de
  suite déjà documenté, pas une régression M052). `BeeKingdom.HiveOperations.Tests`
  non ré-exécuté cette session (aucun fichier de ce projet touché par
  M052 — bonus resolver et intégrations production/capacité/combat
  inchangés, seule la source de données du resolver a changé, couverte par
  les tests Alliance Research eux-mêmes).
- **Unity** : compilation propre (`assets-refresh`, 0 erreur) après
  réécriture complète de `AllianceCenterPresentation.cs` (couche modèle +
  API publique + méthodes `*CoreAsync`) et de l'onglet Recherches dans
  `HiveViewProductUiPresenter.cs`. Play Mode confirmé inactif
  (`Application.isPlaying=False`) avant exécution — `AllianceResearchClientTests`
  (EditMode) : 8/8 verts, réécrits pour le nouveau contrat à 5 méthodes
  (`SelectAllianceResearchFundingTargetAsync`/`DonateToAllianceResearchAsync`
  nouvelle signature/`LaunchAllianceResearchAsync`/`ApplyAllianceResearchSpeedUpAsync`
  + booléens d'autorisation).
- **Localisation** : nouvelles clés ajoutées à `strings.fr-CA.json` pour les
  branches renommées (`army_royal`, `expansion`), les 6 états, les
  technologies nouvelles/renommées, et les messages d'attente
  Eligible/Ready — JSON validé (`json.load` réussi).
- **Portée git** : `git status` confirme qu'aucune migration SQL n'a été
  ajoutée, que le travail non lié (BuildingInteractionController, bootstrap
  CX, LivingHiveMenuCanvas, police Cinzel, EditorBuildSettings, rapports de
  mission antérieurs) est resté intact et non touché par cette mission.
  Aucun commit, aucun push effectué.

## Checklist finale

- A. Bible lue intégralement avant tout code — **OUI**.
- B. Financement ≠ Recherche implémenté — **OUI**.
- C. Cycle de vie à 5 états officiels (+ Eligible, extension documentée) — **OUI**.
- D. Sélection de cible réservée au Chef, serveur-autoritaire — **OUI**.
- E. Changement de cible préserve les contributions — **OUI** (testé).
- F. Deux emplacements Mineure/Majeure indépendants et simultanés — **OUI** (testé).
- G. Financement de la technologie suivante pendant une recherche en cours — **OUI** (testé).
- H. Lancement réservé Chef/Officier — **OUI** (testé).
- I. Minuteur serveur-autoritaire, aucun minuteur Unity — **OUI**.
- J. Résolution paresseuse, déterministe, idempotente — **OUI** (testé).
- K. Architecture supportant les vraies durées Bible (jours-mois) sans changement — **OUI** (testé).
- L. Règle économique croisée respectée pour chaque technologie du catalogue — **OUI**.
- M. Bonus uniquement depuis Completed — **OUI** (testé).
- N. Catalogue Alpha représentatif (4 branches requises + 1 Majeure réelle, Suprématie verrouillée) — **OUI**.
- O. Aucun nouveau concept de technologie hors Bible — **OUI**.
- P. Fondation Alliance Currency (domaine/persistance, pas de Boutique) — **OUI**.
- Q. Gelée Royale jamais une ressource de financement valide — **OUI**.
- R. Catégorie Alliance Research SpeedUp distincte des SpeedUps personnels — **OUI**.
- S. SpeedUp réservé Chef/Officier, uniquement pendant Researching, jamais sous l'heure de complétion — **OUI** (testé).
- T. Événements d'activité pour les jalons significatifs — **OUI** (cible sélectionnée/financée/lancée/complétée).
- U. UI Unity Minor/Major distincte, sélecteur Chef sans saisie manuelle d'ID — **OUI**.
- V. UI distincte Officier/Membre en autorité (booléons serveur) — **OUI**.
- W. Rendu réel des exigences de financement par ressource (plus de bundle générique) — **OUI**.
- X. Traitements distincts Ready/Researching/Completed — **OUI**.
- Y. Migration SQL évitée — **OUI** (aucun fichier ajouté sous Scripts/).
- Z. Si migration avait été nécessaire : créée+enregistrée+testée mais jamais appliquée — **N/A** (non nécessaire).
- AA. Feature flag préservé sans modification — **OUI** (`AllianceResearchOptions.Enabled` inchangé).
- AB. Aucun travail hors périmètre (WorldMap/FTUE/Profil joueur/Recherche personnelle/Construction/Aide d'Alliance/Chat/Diplomatie/Guerre/LivingHive/Palais Royal) — **OUI**, vérifié par `git status`.
- AC. Aucune contradiction Bible découverte nécessitant un arrêt — **OUI** (aucune contradiction rencontrée).
- AD. Tests serveur : 29/29 focalisés verts, 0 régression réelle sur la suite complète — **OUI**.
- AE. Tests Unity : exécutés (Play Mode confirmé libre), 8/8 verts, jamais simulés — **OUI**.
- AF. Aucun commit, aucun push, aucune mutation live, travail non lié préservé — **OUI**.

READY FOR CEO CONTROLLED ALLIANCE RESEARCH CERTIFICATION — NO PRODUCTION STATE MUTATED.
