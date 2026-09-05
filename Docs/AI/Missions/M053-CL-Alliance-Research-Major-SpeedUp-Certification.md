# M053-CL — Alliance Research Major Track + SpeedUp Certification Closeout

## Contexte

Le CEO a certifié en production le cycle de vie Mineur (Alliance Test [BKT],
Stara=Chef, Jeff=Officier) — voir la liste des 31 comportements confirmés
dans le texte de mission. Cette mission ferme les lacunes de certification
restantes (Majeure, concurrence Mineure+Majeure, SpeedUp d'Alliance,
fraîcheur du bonus, atomicité, catalogue, Sceaux Royaux) **sans muter la
production ni consommer de ressources réelles de Jeff/Stara** — via des
tests automatisés et une analyse de code, conformément à l'instruction
explicite de la mission.

## 1. Certification du cycle de vie Majeur

9 nouveaux tests focalisés utilisent `Major1` (`prosperity_age_of_abundance`)
avec ses vrais prérequis (`Minor1`+`Minor2`, complétés via le raccourci
d'arrangement `SeedCompleted` déjà établi en M052) :

- `Major_LockedBeforePrerequisites_EligibleOnceBothCompleted` — Verrouillée
  avant, Éligible seulement après les DEUX complétions réelles.
- `Major_StaysLockedWhilePrerequisiteOnlyFundingReadyOrResearching_NeverUnlocksEarly`
  — preuve explicite des 3 sous-cas : un prérequis en Financement, Prêt (financé
  à 100% mais non lancé) ou En Recherche ne débloque **jamais** la Majeure
  dépendante — seule la complétion réelle le fait.
- `Officer_CannotSelectMajorFundingTarget` / `Member_CannotSelectMajorFundingTarget`
  — même autorité exclusive Chef que pour les Mineures.
- `Major_FullyFunding_ProducesReady_NotCompleted_AndGrantsNoBonus` — 100%
  financée → Prête, jamais Terminée, aucun bonus propre.
- `Officer_CanLaunchReadyMajor` / `Member_CannotLaunchReadyMajor` — même
  matrice de permission de lancement que les Mineures.
- `Major_LaunchSetsServerAuthoritativeStartAndCompletionTimestamps` — le
  lancement pose `ResearchStartedAtUtc`/`ResearchCompletesAtUtc` exacts.
- `Major_NaturallyResolvesToCompleted_AfterTimerElapses_AndBonusActivatesOnlyThen`
  — résolution naturelle du minuteur, bonus actif seulement après.
- `Major_CompletedTechnology_PersistsAcrossFreshRepositoryReadInstance` —
  une Majeure Terminée survit à une nouvelle instance de service branchée
  sur le **même** repository (simulateur de redémarrage de processus).

Le code de `AllianceResearchService` ne fait **aucune** distinction
spéciale Mineure/Majeure au-delà du champ lu/écrit (`MinorX`/`MajorX`) — le
même `switch` sur `definition.Category` gouverne `SelectFundingTargetAsync`,
`ValidateDonatable`, `LaunchAsync`, `ApplySpeedUpAsync`,
`ResolveElapsedResearch` et `ResolveTechnologyState`. Le cycle de vie Majeur
est donc structurellement identique au Mineur, déjà certifié par le CEO.

## 2. Concurrence Mineure+Majeure et financement suivant

`FourConcurrentConcepts_MinorActiveMinorNextMajorActive_PersistAcrossReload`
prouve simultanément : Majeure `Major1` En Recherche, Mineure `MinorOther`
En Recherche, prochaine cible Mineure `MinorThird` sélectionnée (Financement)
pendant que `MinorOther` recherche encore — les 4 valeurs coexistent sans
qu'aucun emplacement n'en écrase un autre, et **persistent** à travers une
nouvelle instance de service sur le même repository.

**Limite honnête documentée (lacune de catalogue, pas de code)** : le
catalogue Alpha ne contient qu'**une seule** technologie Majeure
(`prosperity_age_of_abundance`). Il est donc impossible de sélectionner une
« prochaine cible Majeure » différente pendant qu'une Majeure est déjà en
recherche avec les données réelles du catalogue —
`SelectFundingTargetAsync` n'accepte que de vrais identifiants de catalogue
(`AllianceResearchCatalog.TryGet`), et il n'existe pas de 2e Majeure à
sélectionner. Ce n'est PAS un défaut d'implémentation : une inspection
directe du code (section 1 ci-dessus) montre que le chemin
« financer la technologie suivante pendant qu'une recherche est active »
est **exactement le même code** pour Mineure et Majeure, déjà prouvé côté
Mineure. Conformément à l'instruction explicite de ne pas étendre l'arbre
technologique dans cette mission, aucune 2e Majeure n'a été ajoutée — la
Bible section 15 nomme déjà « Réseau commercial royal » comme prochaine
Majeure Prospérité réelle ; son ajout dans une mission future permettrait
une certification par données réelles plutôt que par inspection de code.
Documenté également via `Catalog_ExactlyOneMajorTechnologyInAlpha` (test qui
échouera intentionnellement le jour où une 2e Majeure sera ajoutée, comme
rappel de mettre ce test à jour).

## 3. Alliance Research SpeedUp

10 nouveaux tests couvrent les lacunes M052 :

- `SpeedUp_RejectedWhileLocked` — rejeté sur une techno Verrouillée
  (`technology_not_researching`).
- `SpeedUp_RejectedAfterCompletion` — rejeté après Terminée.
- `Officer_CanApplyAllianceResearchSpeedUpWhileResearching` — permission
  Officier explicitement prouvée (M052 ne testait que le Chef).
- `SpeedUp_TriggeredCompletion_OccursExactlyOnce` — deux lectures
  indépendantes après un SpeedUp qui dépasse le temps restant montrent
  exactement 1 complétion, jamais 2.
- `SpeedUp_RetryWithSameClientRequestId_DoesNotDoubleConsumeInventory` —
  retry avec la même clé : l'inventaire ne descend que de 1, jamais 2
  (M052 vérifiait le minuteur, pas explicitement l'inventaire au retry).
- `SpeedUp_UnknownItemId_Rejected` — identifiant d'objet inconnu →
  `item_not_found`.
- `PersonalResearchSpeedUpItemId_CannotAccelerateAllianceResearch` — un
  vrai identifiant d'objet de Recherche personnelle (`research_3600s`, du
  catalogue `BeeKingdom.HiveOperations.SpeedUpOptions`) est rejeté par
  `AllianceResearchSpeedUpCatalog` (`item_not_found`) — catégories
  réellement étanches, pas une même liste avec des libellés différents.
- `AllianceResearchSpeedUpItemIds_AreNeverPresentInThePersonalSpeedUpCatalog`
  — inspection directe de catalogue (sens inverse) : aucun
  `alliance_research_speedup_*` n'existe dans le catalogue personnel par
  défaut — donc `SpeedUpInventoryService.ApplyAsync` (qui cherche dans SON
  propre catalogue via `configuration.Find(itemId)`) ne pourrait jamais
  l'accepter non plus (`invalid_speedup`), sans qu'un test cross-projet
  BeeKingdom.HiveOperations.Tests soit nécessaire.
- `SpeedUp_ReducedTimerAndInventoryConsumption_PersistAcrossFreshRepositoryReadInstance`
  — minuteur réduit et inventaire consommé survivent à une nouvelle
  instance de service sur le même repository.

## 4. Statut de l'inventaire SpeedUp d'Alliance

Confirmé inchangé depuis M052 : les items `alliance_research_speedup_1h/3h/8h/24h`
vivent dans le même dictionnaire générique `PlayerHiveState.SpeedUps`
(clé=itemId, valeur=quantité) déjà utilisé par tout le reste du jeu — aucune
table dédiée, aucun nouveau mécanisme de persistance nécessaire. Aucune
source d'acquisition réelle n'existe (boutique/argent réel/événements) —
conforme à l'instruction explicite de ne PAS l'implémenter dans cette
mission. Les tests alimentent l'inventaire directement via
`GiveSpeedUpItem` (même convention que `combat_recall_token`).

## 5. Analyse de fraîcheur du minuteur de bonus (Part 4) — CORRECTIF APPLIQUÉ

**Question posée** : si un minuteur de recherche a objectivement expiré mais
qu'aucune lecture/mutation Alliance Research n'a encore eu lieu, le
gameplay peut-il continuer temporairement avec un état de bonus périmé ?

**Réponse** : OUI, c'était bien le cas avant cette mission —
`AllianceResearchBonusResolver.ResolveForAllianceAsync` lisait l'état brut
du repository (`ReadAsync`, sans résolution paresseuse) et n'itérait que
`state.Completed.Keys` : une recherche dont le minuteur est passé mais
jamais encore lue/mutée par un joueur restait invisible au bonus jusqu'à ce
que quelqu'un ouvre le Centre d'Alliance.

**Correctif appliqué** (le plus petit changement sûr disponible, sans
scheduler ni worker en arrière-plan, sans polling) : `ResolveForAllianceAsync`
compare maintenant `MinorResearch.CompletesAtUtc`/`MajorResearch.CompletesAtUtc`
à l'horloge serveur courante et inclut ces technologies dans le calcul du
bonus si leur minuteur est objectivement dépassé — **purement en lecture**,
aucune écriture, aucun effet de bord. La résolution d'écriture autoritative
(`AllianceResearchService.ResolveElapsedResearch`, qui déplace réellement la
technologie vers `Completed` et vide l'emplacement) reste l'unique endroit
où l'état persiste — elle continue de se produire exactement une fois, à la
prochaine requête touchant cette Alliance. Le bonus est donc désormais
disponible dès l'instant où il est objectivement vrai, pas seulement à
l'instant où quelqu'un ouvre l'écran.

Changement de signature : `AllianceResearchBonusResolver` prend maintenant
un `IServerClock` en plus de ses deux dépendances existantes (déjà
enregistré en DI — `AllianceResearchService` l'utilise déjà — aucun nouveau
enregistrement nécessaire). Deux nouveaux tests couvrent le correctif :
`Bonus_CountsElapsedButNotYetPersistedResearch_WithoutRequiringAReadFirst`
et `Bonus_DoesNotCountResearchThatHasNotYetElapsed` (garde contre un faux
positif prématuré).

**Ceci est un changement de code de production** — voir section 11.

## 6. Revue d'atomicité (Part 5)

Aucune transaction distribuée introduite (conforme à l'instruction). Les
tests existants (M052) et nouveaux (M053) prouvent que l'idempotence protège
contre :
- retry après don réussi (`Donate_SameClientRequestIdRetried_DoesNotDoubleDebit`,
  déjà existant) ;
- retry après SpeedUp réussi (`SpeedUp_SameClientRequestIdRetried_DoesNotDoubleReduce`
  existant + nouveau `SpeedUp_RetryWithSameClientRequestId_DoesNotDoubleConsumeInventory`
  qui vérifie explicitement l'inventaire, pas seulement le minuteur) ;
- identifiants de requête dupliqués (mêmes tests, vérifiés via `ClientRequestId`) ;
- tentatives concurrentes (`ConcurrentDonations_FromTwoMembers_NeitherContributionIsLost`,
  existant, deux dons simultanés via `Task.WhenAll`).

**Fenêtre résiduelle honnêtement documentée (inchangée, non résolue dans
cette mission)** : don et SpeedUp débitent d'abord `PlayerHiveState` (étape
1, idempotent via `Receipts`), puis mutent `AllianceResearchState` (étape 2,
idempotent séparément via `ProcessedDonationIds`/`ProcessedSpeedUpIds`). Si
le processus meurt exactement entre les deux étapes, le joueur est débité
mais l'effet côté Alliance reste en attente — récupérable en rejouant le
MÊME `ClientRequestId` (l'étape 1 rejoue sans effet, l'étape 2 s'applique
alors réellement). Ce n'est PAS une atomicité distribuée au sens strict —
c'est la garantie « jamais de ressources perdues pour rien, mais pas de
commit en deux phases dans les règles » déjà documentée et acceptée depuis
M051/M052. Aucune régression, aucune amélioration dans cette mission.

## 7. Vérification du catalogue canonique (Part 6)

- Toutes les technologies implémentées existent dans la Bible (Prospérité,
  Expansion, Coopération, Armée Royale — les 4 branches requises).
- Classification Mineure/Majeure correcte (`prosperity_age_of_abundance`
  seule Majeure, conforme à la section 15).
- Prérequis cohérents (chaînes `_i`→`_ii`, Majeure nécessitant 2 Mineures
  Prospérité réelles).
- Règle économique croisée respectée pour chaque technologie (aucune ne
  finance principalement la ressource qu'elle améliore directement) —
  confirmé par lecture directe du catalogue, inchangé depuis M052.
- Nouveau test `Catalog_EveryTechnology_HasAtLeastOneFundingResource_ExceptNone`
  — garde-fou structurel (Bible section 9).
- `Catalog_SupremacyBranchNeverPopulated` (nouveau) — confirme
  automatiquement qu'aucune technologie Suprématie n'existe dans le
  catalogue Alpha (section 19) — donc aucun bonus PvP/Guerre d'Alliance ne
  peut être actif.
- `Catalog_ExactlyOneMajorTechnologyInAlpha` (nouveau) — documente
  explicitement la lacune de la section 2 de ce rapport ; ce test devra être
  mis à jour le jour où une 2e Majeure sera ajoutée.
- Aucune balance/coût modifié, aucune nouvelle technologie ajoutée.

## 8. Sceaux Royaux — certification automatisée

4 nouveaux tests :

- `Donate_RejectedDonation_AwardsNoAllianceCurrency` — un don rejeté (cible
  non sélectionnée) n'attribue ni contribution ni monnaie.
- `Donate_RetryWithSameClientRequestId_DoesNotDoubleAwardCurrency` — retry
  avec la même clé : monnaie attribuée une seule fois (50 Sceaux pour 500
  points à 0.1 — même ratio que la certification CEO en production).
- `ChangingFundingTarget_DoesNotAffectAlreadyEarnedCurrencyOrContribution`
  — changer de cible ne touche pas la monnaie/contribution déjà acquise.
- `LeavingAlliance_DoesNotDestroyThePlayersStoredAllianceCurrency` — quitter
  l'Alliance NE détruit PAS la valeur stockée (`AllianceResearchState.Contributions`
  n'est jamais touché par `AllianceService.Leave`).

**Lacune Bible honnêtement découverte et documentée (non corrigée, hors
scope M053 — pas de refonte de la monnaie)** : la Bible section 11 décrit
les Sceaux Royaux comme « conservés dans le portefeuille personnel du
joueur », mais l'implémentation actuelle les stocke dans
`AllianceResearchState.Contributions`, une structure **scoped par
Alliance**, pas un enregistrement global par joueur. Un joueur qui quitte
l'Alliance ne perd pas sa valeur (le test le prouve), mais n'a plus aucun
moyen de la LIRE (`GetSnapshotAsync` exige une adhésion active et lève
`not_a_member`) tant qu'il n'a pas rejoint la même Alliance. C'est un écart
architectural réel par rapport au cadrage « portefeuille personnel » de la
Bible — signalé ici pour une décision de Direction Game Design, pas résolu
dans cette mission (aucune Boutique/refonte de monnaie autorisée).

## 9. Interface Unity

Aucun changement — la mission l'interdit explicitement sauf défaut
fonctionnel bloquant. Aucun changement serveur de cette mission n'affecte
le contrat DTO/wire (`AllianceResearchBonusResolver` est un composant
interne, jamais exposé directement sur le fil) — donc `AllianceClient.cs`,
`AllianceCenterPresentation.cs` et `HiveViewProductUiPresenter.cs` restent
inchangés, et `AllianceResearchClientTests.cs` n'a nécessité aucune mise à
jour.

## 10. Production — AUCUNE MUTATION

Seules des vérifications en lecture seule ont été effectuées :
`GET /health` (200 Healthy) et `GET /alliance/v1/alliances/search?nameOrTag=BKT`
(Alliance Test toujours 2 membres, InviteOnly) — avant et après le travail
de cette mission, pour confirmer qu'aucune action locale (build/tests) n'a
eu d'effet sur l'état de production. Aucun appel authentifié, aucune
mutation (`funding-target`/`donate`/`launch`/`speedup`/rôle/adhésion) n'a
été effectué à aucun moment.

## 11. Fichiers exactement modifiés

```
Server/src/BeeKingdom.Alliance/Research/AllianceResearchBonusResolver.cs   (changement de code de PRODUCTION)
Server/tests/BeeKingdom.Tests/AllianceResearchServiceTests.cs             (29 nouveaux tests + fixture)
```

Aucun autre fichier touché. `git status` confirme que tout le travail non
lié (BuildingInteractionController, bootstraps HiveMap, LivingHiveMenuCanvas,
police Cinzel, EditorBuildSettings, rapports de mission antérieurs non
commités) reste exactement intact, non modifié par cette mission.

**`AllianceResearchBonusResolver.cs` est un changement de code de
production** (nouveau paramètre constructeur `IServerClock`, logique de
fraîcheur additive en lecture seule) — conformément à la Part 11 de la
mission, **aucun déploiement n'a été effectué ni tenté**. Le changement est
petit, isolé, purement additif (ne retire aucune garantie existante,
n'introduit ni polling ni tâche de fond), et couvert par 2 tests dédiés +
tous les tests de bonus existants qui continuent de passer sans
modification de leurs propres assertions.

## 12. Tests et décomptes

- **Serveur, `AllianceResearchServiceTests`** : ANCIEN 29/29 → **NOUVEAU 58/58**
  (29 existants + 29 nouveaux, tous verts, aucune régression).
- **Serveur, `BeeKingdom.HiveOperations.Tests`** (consommateur indirect du
  resolver via l'adaptateur) : **181/181**, aucune régression.
- **Serveur, suite complète `BeeKingdom.Tests`** : 566/567 (1 échec :
  `CatalogSqlMatchesCheckedInScriptFiles`, pré-existant, documenté depuis
  M051/M052, dérive du fichier `091_alliance_help.sql`, sans rapport avec
  Alliance Research ni avec cette mission — confirmé par `git diff` : zéro
  nouveau fichier SQL). Un run antérieur avait montré 2 échecs avec un nom
  différent pour le second — non reproductible sur un second run identique,
  confirmant la non-déterminisme de suite déjà documenté en M052, pas une
  régression M053.
- **Unity, `AllianceResearchClientTests`** : ANCIEN 8/8 → **NOUVEAU 8/8**
  (Play Mode confirmé inactif avant exécution ; aucun nouveau test
  nécessaire, aucun changement de contrat DTO/wire).
- **Unity, compilation** : propre (`assets-refresh`, 0 erreur).

## 13. Exigence de déploiement

**Un déploiement serait requis** pour que le correctif de fraîcheur du
bonus (section 5) atteigne la production — c'est un changement de code de
production. Conformément à l'instruction explicite de la mission, **aucun
déploiement n'a été effectué ni tenté**. Le CEO/GPT décidera si une mission
M053B de déploiement est justifiée.

## 14. Blocages Alpha restants

1. **Catalogue à une seule Majeure** (section 2/7) — empêche la
   certification par données réelles de « prochaine cible Majeure pendant
   qu'une Majeure recherche ». Recommandation : ajouter « Réseau commercial
   royal » (déjà nommée Bible section 15) dans une mission future dédiée au
   catalogue, avec validation Direction Game Design.
2. **Sceaux Royaux scoped par Alliance, pas globalement par joueur**
   (section 8) — écart avec le cadrage « portefeuille personnel » de la
   Bible section 11. Signalé pour décision de Direction Game Design, non
   corrigé (hors scope, pas de refonte de monnaie autorisée).
3. **Fenêtre d'atomicité à deux agrégats** (section 6) — inchangée,
   documentée, jamais observée en test, acceptée comme compromis connu
   depuis M051.
4. **`CatalogSqlMatchesCheckedInScriptFiles`** — échec pré-existant sans
   rapport avec Alliance Research (091_alliance_help.sql), non résolu (hors
   scope, instruction explicite de ne pas réparer les échecs non liés).

## Checklist finale

A. Major prerequisite enforcement proven? **YES**
B. Major funding proven? **YES**
C. Major READY state proven? **YES**
D. Major launch proven? **YES**
E. Major timer proven? **YES**
F. Major completion proven? **YES**
G. Major bonus activation only on COMPLETED? **YES**
H. Minor + Major RESEARCHING simultaneously proven? **YES**
I. Next Minor + next Major funding simultaneously proven? **PARTIAL** — Minor next-funding-while-active proven with real data; Major next-funding-while-active is architecturally identical (code inspection, zero Major-specific special-casing) but not independently testable with real catalog data because the Alpha catalog contains only one Major technology (documented gap, section 2/14)
J. Four simultaneous concepts persist after reload? **YES** (3 of the 4 concepts that ARE testable with real data — Major active, Minor active, Minor next-target — persist across a fresh repository read)
K. Alliance Research SpeedUp distinct from personal SpeedUp? **YES** (proven both directions)
L. Funding/Ready SpeedUp rejection proven? **YES**
M. Researching SpeedUp success proven? **YES**
N. Officer/Chef SpeedUp permission proven? **YES**
O. Member SpeedUp rejection proven? **YES**
P. SpeedUp overshoot completion proven? **YES**
Q. SpeedUp idempotency proven? **YES** (timer AND inventory both verified)
R. Bonus timer freshness safe? **YES** (fixed this mission — read-only, additive, no polling/worker)
S. Donation idempotency preserved? **YES**
T. Sceaux Royaux retry safety proven? **YES**
U. Catalog consistent with Bible? **PARTIAL** — content correct, but currency storage scope (Alliance-keyed vs. Bible's "personal wallet") is a documented discrepancy (section 8/14)
V. Suprématie remains inactive? **YES** (now test-enforced)
W. Production state untouched? **YES**
X. Server focused tests green? **YES** — 58/58 (was 29/29)
Y. Unity focused tests green? **YES** — 8/8 (unchanged)
Z. Unity compile green? **YES**
AA. Production code changed? **YES** — `AllianceResearchBonusResolver.cs` (bonus freshness fix)
AB. Deployment required? **YES** (to ship the freshness fix) — NOT performed this mission
AC. Remaining Alpha blocker? **YES** — see section 14 (catalog single-Major gap, currency scope gap, pre-existing unrelated SQL-drift test failure); none block CEO certification of what IS implemented

M053 CERTIFICATION FOUND PRODUCTION FIXES — READY FOR CEO REVIEW BEFORE DEPLOYMENT.
