# M054A-CL — Royal Seals Exact-Award Correctness

## Contexte

M054 avait déplacé les Sceaux Royaux vers `PlayerHiveState.RoyalSeals`, mais
les créditait depuis `clampedAmount` (le montant réellement débité au
joueur), pas depuis `applied` (le montant réellement accepté par le
financement de l'Alliance). Sous une course de dons concurrents proche du
plafond de financement, un joueur pouvait débiter 500, l'Alliance n'accepter
que 100, mais recevoir des Sceaux pour 500 — inacceptable pour une monnaie
dépensable. Cette mission corrige cette divergence.

## 1. Nouvelle topologie de la transaction de don (3 étapes)

**Étape 1** (`PlayerHiveState`, atomique, idempotente via `Receipts`) —
débit des ressources réelles UNIQUEMENT. Les Sceaux Royaux ne sont plus
crédités ici.

**Étape 2** (`AllianceResearchState`, atomique, idempotente via
`ProcessedDonationIds`) — calcule `applied = min(debitedAmount, room)`,
applique au financement et à `ContributionPoints`/`DonationCount` (inchangé
depuis M052/M054), et persiste `applied` dans un nouveau champ
`DonationAppliedAmounts[donationKey]` — nécessaire pour qu'un **rejeu**
(retry) de ce don puisse retrouver le montant exact appliqué la première
fois, puisque le corps de la mutation court-circuite avant tout recalcul dès
que `ProcessedDonationIds` contient déjà la clé.

**Étape 3** (nouvelle, `PlayerHiveState`, atomique, idempotente via sa
**propre** clé `"alliance-research-seals:" + ClientRequestId`, distincte de
l'étape 1 et du préfixe de migration `"royal-seals-migration:"`) — crédite
`floor(applied * ratio)` — **jamais** `floor(clampedAmount * ratio)`.

Ceci garantit l'invariant canonique exigé : `RoyalSealsAward = floor(applied
* ratio)`, exactement la même valeur `applied` qui gouverne
`ContributionPoints`, toujours, sous toute concurrence.

## 2. Pourquoi une nouvelle structure `DonationAppliedAmounts`

Une carte séparée (`Dictionary<string, long>`), pas un changement de type de
`ProcessedDonationIds` (qui reste un `HashSet<string>`) : changer le type
d'un champ déjà persisté en production (`HashSet` sérialisé en tableau JSON)
vers un `Dictionary` casserait la désérialisation des lignes réelles
existantes (`dbo.AllianceResearch` contient déjà l'historique de dons
certifié CEO de M052B/M053B). Une carte additive et normalisée à vide par
`AllianceResearchStateMigrator` pour les anciennes lignes est sans risque et
suit exactement la convention déjà établie pour `Funding`/`Completed`/etc.

## 3. Surpaiement de ressources — analyse et décision de ne PAS l'éliminer

Inspecté explicitement, comme demandé. Deux façons d'éliminer le surpaiement
ont été considérées et **rejetées toutes les deux** :

- **(a) Réserver la place côté Alliance avant de débiter le joueur** : si le
  débit échoue ensuite (ressources insuffisantes), l'Alliance aurait déjà du
  financement enregistré que personne n'a payé — un bug d'économie
  irréversible et strictement pire que le surpaiement actuel (de la monnaie
  fabriquée à partir de rien, visible par tous les membres).
- **(b) Transaction distribuée entre les deux agrégats** : explicitement
  interdite par la mission.

**Décision : STOP sur ce point précis**, conformément à l'instruction
explicite de la mission. Le surpaiement résiduel reste possible dans la
fenêtre de course déjà documentée (deux dons visant exactement la même
ressource de la même technologie au moment où elle dépasse 100 %), mais il
n'est **plus invisible** : `DonateAsync` journalise maintenant explicitement
un avertissement (`LogWarning`) chaque fois que `applied < debitedAmount`,
avec le montant exact surpayé, le joueur et la technologie concernés — visible
en observabilité de production, jamais silencieux.

## 4. Test de concurrence déterministe

`ConcurrentFinalDonations_TotalAppliedNeverExceedsRequired_RoyalSealsAndContributionMatchActualApplied` :
scénario exact de la mission (technologie ne nécessitant plus que 100 Miel,
deux membres tentent chacun de donner 500 simultanément). Obtenir une
véritable course déterministe contre des repositories en mémoire qui
complètent chaque opération de façon synchrone s'est avéré non trivial —
`Task.WhenAll` seul laissait la première tâche se terminer entièrement avant
que la seconde ne lise même son propre état préalable, et un simple signal
`TaskCompletionSource` souffrait d'un chemin rapide « déjà complété » qui
laissait le DERNIER arrivant continuer avec une longueur d'avance
synchrone. Résolu avec un **rendez-vous à deux barrières** (classe de test
`Rendezvous`, réutilisée pour synchroniser à la fois la lecture de
pré-vérification ET la tentative de mutation atomique côté Alliance) —
chaque barrière force TOUS les appelants à travers un `Task.Yield()` réel
avant de continuer, garantissant qu'aucun des deux ne peut prendre une
avance irrattrapable. Validé stable sur **8 exécutions consécutives**
(0/8 échec) après la correction, contre 3/5 échecs avec l'implémentation
naïve à une seule barrière.

**Invariants prouvés par ce test** :
- Le total appliqué au financement ne dépasse jamais 100 (l'exigence
  réelle), peu importe l'issue de la course.
- La somme des `ContributionPoints` attribués aux deux dons concurrents
  égale exactement 100, jamais 1000.
- La somme des Sceaux Royaux frappés par les deux dons concurrents égale
  exactement `floor(100 * 0.1) = 10`, jamais `floor(1000 * 0.1) = 100`.
- Sceaux et Points dérivent tous deux de la MÊME valeur `applied` (vérifié
  par recoupement direct dans le test).

## 5. Idempotence préservée

- Retry avec le même `ClientRequestId` : aucun double débit, aucune double
  application Alliance, aucun double crédit de Sceaux — testé
  explicitement (`Donate_RetryAfterExactAwardSplit_RemainsIdempotent_...`)
  y compris dans un scénario ayant déjà traversé le partage exact
  applied/debited.
- Les clés de réception de migration (`"royal-seals-migration:"`) restent
  totalement indépendantes des nouvelles clés de don (`"alliance-research-donate:"`,
  `"alliance-research-seals:"`) — aucune collision possible, aucun
  changement de préfixe. Testé explicitement
  (`Migration_StillIndependentFromDonationReceiptKeys_AfterExactAwardChange`).

## 6. Migration M054 — sémantique inchangée

`RoyalSealsMigrationService` n'a **pas été modifié**. Il continue de lire
`AllianceCurrencyBalance` (le champ legacy figé) comme source de vérité pour
les soldes historiques déjà gagnés — jamais recalculé depuis
`ContributionPoints`, exactement comme exigé (« The legacy balance is
authoritative for migration because those Sceaux already exist »). Testé de
nouveau après le changement de flux de don pour confirmer l'absence de
régression.

## 7. Fichiers exactement modifiés

```
Server/src/BeeKingdom.Alliance/Research/AllianceResearchModels.cs         (+DonationAppliedAmounts)
Server/src/BeeKingdom.Alliance/Research/AllianceResearchStateMigrator.cs  (+null-safety pour le nouveau champ)
Server/src/BeeKingdom.Alliance/Research/AllianceResearchService.cs        (DonateAsync restructuré en 3 étapes)
Server/tests/BeeKingdom.Tests/AllianceResearchServiceTests.cs            (4 nouveaux tests)
```

(Fichiers déjà modifiés par M054, inchangés davantage par M054A :
`IAllianceResearchRepository.cs`, `InMemoryAllianceResearchRepository.cs`,
`SqlAllianceResearchRepository.cs`, `HiveOperationModels.cs`,
`HiveStateMigrator.cs`, `RoyalSealsMigrationService.cs`, `RoyalSealsWallet.cs`
— listés pour mémoire, non retouchés cette mission.)

Aucun autre fichier touché. Travail non lié (BuildingInteractionController,
bootstraps HiveMap, LivingHiveMenuCanvas, police Cinzel, EditorBuildSettings,
rapports de mission antérieurs) intact et vérifié par `git status`.

## 8. Tests et décomptes

- `AllianceResearchServiceTests` : **AVANT 72/72 (fin M054) → APRÈS 76/76**
  (4 nouveaux tests M054A), stable sur 8 exécutions consécutives (aucune
  instabilité résiduelle après la correction à deux barrières).
- `BeeKingdom.HiveOperations.Tests` : **181/181**, aucune régression.
- Suite complète `BeeKingdom.Tests` : échecs résiduels confirmés
  pré-existants et sans rapport — reproduits même en EXCLUANT tous les tests
  Alliance Research du filtre (`FullyQualifiedName!~AllianceResearch`),
  prouvant qu'ils ne proviennent pas de cette mission
  (`CatalogSqlMatchesCheckedInScriptFiles`, et des tests d'idempotence
  HivePerimeterSortie déjà documentés comme non-déterministes depuis
  M052/M053).
- Unity `AllianceResearchClientTests` : **8/8**, inchangé (Play Mode
  confirmé inactif). Compilation Unity propre. Aucun changement Unity —
  contrat DTO totalement identique.

## Checklist finale

A. Royal Seals calculated from actual applied? **YES**
B. Contribution calculated from same applied? **YES** (inchangé depuis M054 — toujours `applied`)
C. Excess Royal Seals impossible under concurrency? **YES** (prouvé par test déterministe)
D. Excess resource debit impossible? **NO** — analysé et documenté (section 3) : éliminer complètement exigerait soit une réservation Alliance-side pré-débit (pire bug économique potentiel : financement non payé) soit une transaction distribuée (interdite) ; le surpaiement résiduel est borné, rare, et maintenant explicitement journalisé (jamais invisible)
E. Concurrent final-donation test green? **YES**
F. Donation retry remains idempotent? **YES**
G. Legacy migration unchanged/safe? **YES**
H. DTO/UI unchanged? **YES**
I. Alliance Research lifecycle unchanged? **YES**
J. Focused tests green? **YES** — 76/76 (was 72/72)
K. Unity tests green? **YES** — 8/8
L. Unity compile green? **YES**
M. Production mutated? **NO**
N. Commit/push/deploy performed? **NO**
O. READY FOR M054B? **YES**

M054A EXACT-AWARD CORRECTNESS PASS — ROYAL SEALS CANNOT BE MINTED FROM EXCESS CONTRIBUTIONS — READY FOR M054B REVIEW.
