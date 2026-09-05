# M054-CL — Royal Seals Personal Wallet

## Contexte

M053 avait découvert que Sceaux Royaux (`AllianceCurrencyBalance`) vivaient
dans `AllianceResearchState.Contributions[playerId]` — donc scopés par
Alliance, contrairement au cadrage « portefeuille personnel du joueur » de
la Bible section 11. Cette mission déplace l'autorité de cette monnaie vers
un **portefeuille réellement possédé par le joueur**, sans jamais réinitialiser
ou dupliquer les soldes déjà gagnés, et sans muter la production.

## 1. Architecture précédente

```
AllianceResearchState.Contributions[playerId] = AllianceResearchContribution(
    PlayerId, TotalPoints, DonationCount, AllianceCurrencyBalance)
```

Une ligne `AllianceResearchState` par Alliance — donc autant de « soldes »
indépendants qu'un joueur a d'historiques de contribution dans des Alliances
différentes. Quitter une Alliance ne détruisait pas la valeur, mais la
rendait pratiquement illisible (`GetSnapshotAsync` exige une adhésion active
et lève `not_a_member`).

## 2. Nouvelle autorité du portefeuille

`PlayerHiveState.RoyalSeals` (nouveau champ `long`, défaut `0`) — lu/crédité
exclusivement via la nouvelle classe statique `RoyalSealsWallet`
(`BeeKingdom.HiveOperations`). `AllianceResearchState.Contributions[playerId].AllianceCurrencyBalance`
devient un **champ figé, non-autoritaire** : conservé pour compatibilité de
lecture par l'outil de migration uniquement, plus jamais écrit par le code
de production (`DonateAsync` ne l'incrémente plus).

## 3. Emplacement de stockage exact — justification

`PlayerHiveState` a été choisi plutôt qu'un nouveau module/table, après
inspection :

- `IAccountRepository`/`AccountRecord` (module `BeeKingdom.Accounts`) est
  bien scopé strictement par joueur, mais **ne possède aucune machinerie
  d'idempotence/mutation atomique** (`Save` est un simple read-modify-write,
  pas d'`ExecuteAtomicallyAsync`, pas de `Receipts`) — l'utiliser aurait
  exigé de construire cette infrastructure depuis zéro, un chantier bien
  plus large que « le plus petit changement robuste ».
- `PlayerHiveState` possède déjà **tout** : `ExecuteAtomicallyAsync(playerId, hiveId, mutation)`,
  le dictionnaire `Receipts` idempotent, et est **déjà** le « seau générique
  des possessions du joueur » de facto dans ce codebase — VIP progress,
  progression des Champion Bees, et l'inventaire `SpeedUps` y vivent déjà
  bien qu'aucun ne soit une mécanique de ruche à proprement parler.
- **Risque multi-ruche identifié et neutralisé** : `PlayerHiveState` est
  indexé par `(PlayerId, HiveId)`, pas uniquement `PlayerId` —
  `IHiveStateRepository.ListHiveIdsAsync` retourne une LISTE, prouvant que
  l'architecture supporte plusieurs ruches par joueur. Preuve directe que ce
  n'est **jamais** exploité dans le jeu réel aujourd'hui : aucun endpoint de
  création de ruche n'existe dans `Program.cs` (grep vérifié), les lignes
  `PlayerHiveState` sont créées paresseusement (`newStateFactory`) au premier
  accès, et `ListHiveIdsAsync` n'est utilisé nulle part ailleurs que pour la
  validation de propriété (`ownedHiveIds.Contains(hiveId)`). Pour rester
  correct même dans ce cas théorique futur, `RoyalSealsWallet.GetBalanceAsync`
  **somme défensivement sur toutes les ruches possédées** au lieu de lire une
  seule ruche arbitraire — un joueur avec une seule ruche (réalité actuelle)
  ne voit aucune différence ; un joueur qui aurait un jour une 2e ruche
  resterait correctement agrégé sans migration de schéma supplémentaire.

## 4. Flux de transaction de don (nouvelle topologie)

**Étape 1 (PlayerHiveState, atomique, idempotente via `Receipts`)** : débit
des ressources réelles **ET** crédit des Sceaux Royaux dans **la même**
mutation — `currencyAwarded = floor(clampedAmount * ratio)`, calculé à
partir du montant réellement débité (`clampedAmount`), jamais du montant
« appliqué » côté Alliance (voir section 11, divergence documentée).

**Étape 2 (AllianceResearchState, atomique, idempotente via `ProcessedDonationIds`)** :
financement + `ContributionPoints`/`DonationCount` exactement comme avant
M054 — **n'écrit plus jamais** `AllianceCurrencyBalance`.

## 5. Séparation Contribution / Monnaie

`ContributionPoints`/`DonationCount` restent stockés et calculés **exactement
comme avant** dans `AllianceResearchState.Contributions` — aucun changement
de leur logique, de leur portée (par Alliance) ni de leurs valeurs.
`RoyalSeals` seul change de propriétaire. Testé explicitement
(`ContributionPointsAndDonationCount_RemainAllianceScoped_UnaffectedByWalletMove`).

## 6. Composition du snapshot / API

`AllianceResearchReadSnapshot.MyAllianceCurrencyBalance` (champ déjà existant,
**wire contract totalement inchangé**) est désormais rempli par
`RoyalSealsWallet.GetBalanceAsync(hiveStateRepository, actorPlayerId.Value, ct)`
au lieu de `Contributions[playerId].AllianceCurrencyBalance`. Tous les 6
points d'appel de `BuildSnapshot` passent maintenant par un nouveau wrapper
asynchrone `BuildSnapshotAsync` qui récupère le solde avant de construire le
DTO — la fonction pure `BuildSnapshot` elle-même reste synchrone et
testable isolément. **Aucun changement Unity requis** : le DTO est
identique, seule la source serveur du champ a changé.

## 7. Comportement quitter/rejoindre

Testé explicitement
(`LeaveThenJoinDifferentAlliance_WalletPersists_ContributionHistoryStaysIndependentPerAlliance`) :
un joueur gagne 50 Sceaux dans l'Alliance A, quitte, rejoint l'Alliance B,
son solde reste 50 ; un don dans B ajoute 30 Sceaux de plus (solde = 80) ;
l'historique de contribution de A (500 pts) reste intact et indépendant de
celui de B (300 pts) — aucun n'hérite de l'autre.

## 8. Stratégie de migration

`RoyalSealsMigrationService.MigrateAsync()` (nouveau, `BeeKingdom.Alliance.Research`) :
1. Énumère toutes les lignes `AllianceResearchState` via la nouvelle méthode
   `IAllianceResearchRepository.ListAllAllianceIdsAsync()` (ajoutée aux deux
   implémentations, In-Memory et SQL — `SELECT AllianceId FROM dbo.AllianceResearch`).
2. Pour chaque `Contributions[playerId]` avec `AllianceCurrencyBalance > 0`,
   crédite le portefeuille du joueur via une mutation `PlayerHiveState`
   atomique, gardée par une clé d'idempotence `Receipts` dédiée
   (`"royal-seals-migration:" + allianceId + ":" + playerId"`) — **exactement
   le même mécanisme** que chaque autre action payante de ce codebase.
3. Retourne un résumé (`MigrationOutcome`) : lignes scannées, soldes legacy
   trouvés, joueurs crédités, déjà-migrés ignorés, joueurs sans ruche
   possédée, total migré.

**Preuve du cas multi-Alliance (section demandée explicitement par la
mission)** : inspection directe du code confirme qu'**aucun chemin** ne
copie/reporte une entrée `Contributions` d'une Alliance vers une autre —
chaque dictionnaire n'est muté que par `DonateAsync` opérant sur
`membership.AllianceId.Value` (l'Alliance courante au moment exact de CE
don). Deux soldes legacy non nuls pour le même joueur dans deux Alliances
différentes représentent donc deux montants **indépendamment gagnés**,
jamais un doublon de la même valeur — les sommer une fois chacun est la
règle de migration correcte et non-inflationniste. Documenté et testé
(`Migration_SumsIndependentLegacyBalancesAcrossMultipleAlliances_ExactlyOnceEach`).
**Aucune ambiguïté rencontrée** — pas d'arrêt nécessaire sur ce point.

## 9. Inventaire de production en lecture seule

**NON réalisé** — honnêtement. Les seuls endpoints `/ops/*` existants
(`migrations/pending`, `players/lookup-display-name`, `migrations/rollback-plan`,
`monitoring`, `readiness`, `sql-production-dry-run`) exigent la Admin Key
(non détenue dans cette session) et **aucun** ne permet une requête de
comptage arbitraire sur `dbo.AllianceResearch`. Construire un nouvel
endpoint uniquement pour cette inspection serait lui-même un changement de
code de production nécessitant un déploiement avant de pouvoir l'utiliser —
contraire à l'esprit d'une vérification « en lecture seule avant migration ».
**Recommandation** : soit une requête SQL en lecture seule exécutée
directement par le CEO/DBA (`SELECT COUNT(*), SUM(...) FROM dbo.AllianceResearch`
côté JSON nécessiterait un parsing applicatif, pas une requête SQL triviale
puisque `Contributions` est dans le JSON opaque), soit — plus simple et déjà
prévu — laisser `RoyalSealsMigrationService.MigrateAsync()` lui-même
produire cet inventaire en temps réel via son `MigrationOutcome` lors d'une
exécution **en environnement de test/staging d'abord**, avant toute
application en production. Ceci est un blocage documenté, pas contourné.

## 10. Stratégie d'idempotence

Un seul mécanisme réutilisé partout (`PlayerHiveState.Receipts`) :
- Don réussi → crédite une fois (`RoyalSeals_StoredOnPlayerHiveState_NotInAllianceResearchState`).
- Retry même `ClientRequestId` → aucun crédit supplémentaire
  (`Donate_CreditsWalletOnce_RetryDoesNotDoubleCredit_FailedDonationCreditsNothing`).
- Don échoué → aucun crédit (même test).
- Migration rejouée → aucun doublon
  (`Migration_RerunIsIdempotent_DoesNotDuplicateExistingBalance`).
- Migration + don ultérieur → total correct
  (`Migration_MovesLegacyBalanceIntoPlayerWallet_WithoutAlteringContributionPointsOrDonationCount`
  suivi conceptuellement par tout don normal, qui utilise sa propre clé
  distincte `alliance-research-donate:*`, jamais en collision avec
  `royal-seals-migration:*`).

## 11. Risque d'atomicité résiduel — honnêteté exigée

**Amélioration réelle** : les Sceaux Royaux sont désormais crédités dans la
**même** mutation atomique que le débit de ressources (étape 1) — avant
M054, cette valeur vivait dans l'agrégat Alliance (étape 2). Le crash entre
étape 1 et étape 2 laisse maintenant le joueur avec ses ressources débitées
**et** ses Sceaux déjà crédités, mais sans financement/contribution Alliance
encore appliqués — récupérable par retry du même `ClientRequestId` (étape 1
rejoue sans effet, étape 2 s'applique alors). C'est le **même type** de
fenêtre déjà documentée depuis M051 (jamais de ressources perdues pour rien,
jamais un vrai commit en deux phases), simplement son contenu a changé de
place.

**Divergence de calcul assumée et documentée** : les Sceaux sont calculés à
partir de `clampedAmount` (montant réellement débité, connu à l'étape 1),
et non plus de `applied` (montant réellement accepté côté financement
Alliance, calculé à l'étape 2 — ce qui a change en cas de course de dons
concurrents sur exactement la même ressource de la même technologie au
moment où elle dépasse 100 %). Dans cette fenêtre déjà pré-existante et
extrêmement étroite (déjà documentée par le test M052
`ConcurrentDonations_FromTwoMembers_NeitherContributionIsLost`), le joueur
pourrait recevoir des Sceaux Royaux marginalement supérieurs à sa
contribution effectivement comptée côté Alliance — jamais l'inverse, jamais
un vol, un écart favorable au joueur uniquement. `ContributionPoints`
continue d'utiliser `applied`, inchangé. Aucune transaction distribuée
introduite.

## 12. Comportement réinitialisation/suppression de compte

Inspection : `AccountService.DeleteAccount` ne fait que basculer
`AccountProfile.Status` à `Deleted` — **aucun mécanisme n'efface
réellement** `PlayerHiveState` (ni Honey/Pollen/Cire, ni VIP, ni Champion
Bees, ni SpeedUps) nulle part dans ce codebase aujourd'hui. Royal Seals
hérite donc exactement du même comportement que **toute** autre donnée déjà
stockée dans `PlayerHiveState` — ce n'est pas une nouvelle donnée orpheline
introduite par cette mission, c'est la continuité du comportement existant.
Aucun changement de sémantique de suppression n'a été fait (hors scope —
la mission demande de ne pas changer cette sémantique).

## 13. Fichiers exactement modifiés

```
Server/src/BeeKingdom.HiveOperations/HiveOperationModels.cs        (+RoyalSeals field)
Server/src/BeeKingdom.HiveOperations/HiveStateMigrator.cs          (+validation guard)
Server/src/BeeKingdom.HiveOperations/RoyalSealsWallet.cs           (nouveau)
Server/src/BeeKingdom.Alliance/Research/AllianceResearchService.cs (donation flow + snapshot)
Server/src/BeeKingdom.Alliance/Research/IAllianceResearchRepository.cs   (+ListAllAllianceIdsAsync)
Server/src/BeeKingdom.Alliance/Research/InMemoryAllianceResearchRepository.cs (impl)
Server/src/BeeKingdom.Alliance/Research/SqlAllianceResearchRepository.cs      (impl)
Server/src/BeeKingdom.Alliance/Research/RoyalSealsMigrationService.cs   (nouveau)
Server/tests/BeeKingdom.Tests/AllianceResearchServiceTests.cs     (14 nouveaux tests + 1 réécrit)
```

Aucun autre fichier touché. `git status` confirme tout le travail non lié
(BuildingInteractionController, bootstraps HiveMap, LivingHiveMenuCanvas,
police Cinzel, EditorBuildSettings, rapports de mission antérieurs non
commités) intact, inchangé.

## 14. Migration SQL/application requise ?

**Aucune migration SQL.** `dbo.HivePlayerStates` et `dbo.AllianceResearch`
stockent déjà leur état en JSON opaque — `RoyalSeals` (un `long` avec
défaut `0`) se désérialise sans risque sur d'anciennes lignes, aucune
colonne à ajouter. Une **migration applicative** (`RoyalSealsMigrationService`)
existe, entièrement codée et testée (5 tests dédiés), mais **non exécutée**
contre la production — conforme à l'instruction explicite « create and test
it, but DO NOT APPLY IT ».

## 15. Tests et décomptes

- `AllianceResearchServiceTests` : **72/72 verts** (58 hérités de M053 + 14
  nouveaux ; 1 test M053 devenu obsolète a été réécrit pour la nouvelle
  architecture plutôt que supprimé silencieusement —
  `LeavingAlliance_DoesNotDestroyThePlayersStoredAllianceCurrency` →
  `LeavingAlliance_DoesNotDestroyThePlayersRoyalSealsWallet`).
- `BeeKingdom.HiveOperations.Tests` : **181/181**, aucune régression
  (`PlayerHiveState`/`HiveStateMigrator` touchés mais rétrocompatibles).
- Suite complète `BeeKingdom.Tests` : ~572/581 sur deux exécutions, seul
  échec constant `CatalogSqlMatchesCheckedInScriptFiles` (pré-existant,
  091_alliance_help.sql, documenté depuis M051) ; la non-déterminisme
  occasionnelle d'un 2e nom de test différent à chaque run reste le même
  phénomène déjà documenté en M052/M053, sans rapport avec M054.
- Unity `AllianceResearchClientTests` : **8/8 verts**, inchangé (Play Mode
  confirmé inactif). Compilation Unity propre. **Aucun changement Unity** —
  contrat DTO identique.

## 16. Étapes de déploiement requises

1. Déployer le code (aucune migration SQL).
2. **Avant** toute application en production, exécuter
   `RoyalSealsMigrationService.MigrateAsync()` une première fois en
   environnement de test/staging pour observer son `MigrationOutcome` réel
   (sert aussi d'inventaire — voir section 9).
3. Exécuter la migration en production **une seule fois**, sous supervision
   CEO, via un mécanisme à construire en M054B (ex. endpoint ops protégé
   miroir de `/ops/migrations/apply`) — **non construit dans M054**.
4. Vérifier après coup : `MyAllianceCurrencyBalance` de Jeff/Stara dans un
   snapshot réel correspond aux valeurs déjà certifiées par le CEO
   (Stara ≥ 50, Jeff ≥ 600).

## 17. Plan de certification humaine

Une fois M054B déployé et la migration exécutée : le CEO se reconnecte en
Jeff et Stara dans Alliance Test, vérifie que « Ma contribution : X pts · Y
dons · Z Sceaux Royaux » affiche exactement les mêmes valeurs qu'avant
migration (aucune perte, aucun doublement), effectue un don supplémentaire
et confirme que Z augmente correctement, puis (test optionnel, mutation
réelle) quitte et rejoint une autre Alliance pour confirmer visuellement la
persistance du solde — reproduisant en production ce que les tests
automatisés prouvent déjà en isolation.

## Checklist finale

A. Sceaux Royaux truly player-owned? **YES**
B. Independent of AllianceResearchState? **YES**
C. ContributionPoints remain Alliance-scoped? **YES**
D. DonationCount remain Alliance-scoped? **YES**
E. Existing UI contract preserved? **YES** (DTO field unchanged, source changed)
F. Successful donation credits wallet? **YES**
G. Retry safe? **YES**
H. Failed donation safe? **YES**
I. Leave preserves balance? **YES**
J. Join new Alliance preserves balance? **YES**
K. New Alliance donations add to old balance? **YES**
L. Old contribution history preserved? **YES**
M. Migration strategy idempotent? **YES**
N. Existing production balances inventoried? **NO** — no safe existing read-only tool exposes this without either secrets not held this session or a new production endpoint (see section 9); recommend a staging dry-run of the migration itself as the practical inventory step
O. Multi-Alliance legacy duplication risk resolved? **YES** (code-evidence based, documented section 8)
P. Jeff/Stara balances preservable? **YES** (general mechanism, not hard-coded to any player)
Q. No balance reset required? **YES**
R. Gelée Royale unaffected? **YES**
S. Alliance Research SpeedUps unaffected? **YES**
T. Unity compile green? **YES**
U. Focused tests green? **YES** — 72/72 (was 58/58)
V. SQL migration required? **NO**
W. Production backfill required? **YES** (application-level, via RoyalSealsMigrationService — not yet run anywhere)
X. Production code changed? **YES**
Y. Production state mutated? **NO**
Z. READY FOR CEO REVIEW / M054B DEPLOYMENT? **YES**

M054 ROYAL SEALS PERSONAL WALLET READY — EXISTING BALANCES PRESERVED — NO PRODUCTION MUTATION — AWAITING CEO DEPLOYMENT AUTHORIZATION.
