# M078B-CL — Ronde quotidienne : vraies missions joueur + UX compréhensible

Date : 2026-09-10

## 1. Ce que signifiaient réellement les 3 anciennes conditions

`HiveDailyRoundState` (3 booléens) n'avait que deux véritables déclencheurs
en production, tous les deux purement techniques ou trop larges :

| Ancien libellé | Déclencheur réel |
|---|---|
| "Collecte reçue" (`CollectionReceived`) | `HiveOfflineProductionService.CollectAsync` — récupérer le miel/cire/pollen accumulé dans les bâtiments de stockage. Réel, mais rien à voir avec la World Map. |
| "Opération lancée" (`OperationLaunched`) | 4 sources différentes à la fois : amélioration de bâtiment, recherche/entraînement/production (file générique), soin de couvain. Réel mais fourre-tout, jamais spécifique à "améliorer un bâtiment" comme le libellé le laissait entendre. |
| "Stocks lus" (`SnapshotRead`) | `GET /hive-stock` — se déclenchait **automatiquement à chaque simple ouverture de l'écran Sac**, sans aucune action du joueur. C'est exactement le signal purement technique dénoncé par le CEO. |

`RecordCollectionReceiptAsync`/`RecordOperationLaunchAsync` (avec
vérification d'un `OperationId` réel) existent dans `HiveOperationService`
mais ne sont **appelés par aucun endpoint** — code mort, non utilisé en
production.

## 2. Ce qui a été conservé / remplacé

- **`OperationLaunched` → conservé tel quel.** Déjà une vraie action de
  gameplay (améliorer un bâtiment, lancer une recherche, entraîner des
  troupes, soigner le couvain) — correspond exactement à la suggestion de
  repli du CEO ("entraîner des troupes ; terminer une recherche"). Aucun
  code touché.
- **`CollectionReceived` → conservé + étendu.** Le déclenchement existant
  (collecte de stockage) reste réel et valable ; un second déclenchement a
  été ajouté dans `WorldResourceCollectionService.ClaimAsync` (récolter une
  ressource sur la World Map), pour que l'objectif "Récolteur du royaume"
  couvre bien l'action explicitement demandée par le CEO.
- **`SnapshotRead` → remplacé.** Retiré de `GET /hive-stock` (qui ne marque
  plus rien dans la Ronde quotidienne — lire son Sac n'est plus une
  "action"). Ré-affecté à `WorldResourceCollectionService.LaunchAsync`
  (envoyer une expédition sur la World Map) : une vraie action délibérée du
  joueur, déjà authoritative côté serveur.

Aucun nouveau champ, aucune migration : les 3 booléens existants
(`CollectionReceived`/`OperationLaunched`/`SnapshotRead`) sont réutilisés
tels quels, seul CE QUI les déclenche a changé pour deux d'entre eux.

## 3. Les 3 nouvelles missions (affichage joueur)

| Titre | Description affichée | Déclencheur serveur |
|---|---|---|
| Récolteur du royaume | Collecter une ressource (Sac de la ruche ou World Map) | Collecte de stockage OU `world-resources/{flightId}/claim` |
| Développement de la ruche | Lancer une opération (amélioration, recherche, entraînement...) | Amélioration bâtiment / recherche / entraînement / soin couvain |
| En mission | Envoyer une expédition sur la World Map | `world-resources/{nodeId}/launch` |

Plus aucun libellé technique ("collecte reçue", "stocks lus", nom de champ
serveur) dans l'UI. Progression numérique `0 / 1` → `1 / 1` par mission
(remplace la case `[ ]`/`[x]`), demande CEO explicite.

Seul `HiveMapActivitiesBootstrap.DrawDailyRoundSection` (l'écran Activités
réellement utilisé par le CEO, confirmé par sa capture d'écran) a été
modifié. L'ancien panneau IMGUI `DrawOfficialDailyRoundContent` (menu
"Défi" hérité, non utilisé dans le flux CEO actuel) n'a pas été touché —
hors périmètre de cette mission.

## 4. Reset quotidien (vérifié, inchangé)

Déjà entièrement correct côté serveur, aucune modification :

- Le jour de référence est `new DateTimeOffset(clock.UtcNow.UtcDateTime.Date, TimeSpan.Zero)`
  — horloge serveur (`IServerClock`), jamais l'horloge Windows locale.
- `HiveDailyRoundFacts.ApplyFreshFact` recrée un round vierge dès que le
  `DayUtc` stocké diffère du jour UTC courant.
- `ClaimDailyRoundAsync` revérifie explicitement `round.DayUtc.Date == now.Date`
  et refuse (`daily_round_incomplete`) si le jour a changé entre-temps.
- Anti-double-réclamation à deux niveaux : `round.ClaimedAtUtc is not null`
  bloque toute seconde réclamation, et le rejeu de la même clé
  d'idempotence renvoie le même reçu stocké sans re-créditer.
- Rien de tout cela ne dépend du client : un redémarrage Unity ne fait que
  relire l'état déjà persisté, aucune remise à zéro locale possible.

## 5. Récompense

Inchangée : 120 miel, 60 pollen, une seule réclamation quand les 3 missions
sont à 1/1.

## Tests

Ciblés uniquement :
- `WorldResourceCollectionServiceTests.cs` : 3 nouveaux tests (`Launch`
  marque `SnapshotRead` quand activé / ne marque rien quand désactivé,
  `Claim` marque `CollectionReceived`).
- `HiveStockEndpointTests.cs` : le test qui vérifiait l'ancien comportement
  ("lire le Sac marque la Ronde quotidienne") a été réécrit pour vérifier le
  nouveau contrat (aucun effet sur `DailyRound`).
- Suite complète `BeeKingdom.HiveOperations.Tests` : 199/199 verts (aucune
  régression, y compris les tests Combat Patrol/Offline Production/Building
  Upgrade qui dépendent du même mécanisme `HiveDailyRoundFacts`).
- `BeeKingdom.Tests` (`HiveStockEndpointTests`) : 4/4 verts.
- Compilation Unity (`assets-refresh`) : 0 erreur.

## Ce qui nécessite le compte CEO

Aucune action ne nécessite spécifiquement le compte CEO au-delà de ce qui
était déjà vrai pour M078 — n'importe quel compte authentifié suffit pour :
lancer/réclamer une collecte World Map, lancer une opération de ruche,
réclamer la Ronde quotidienne une fois les 3 missions à 1/1.

## Commit

Commit local uniquement. Aucun push — un déploiement serveur sera
nécessaire pour que le contenu change en production (mêmes endpoints déjà
actifs, seule leur logique interne change), à effectuer séparément selon la
procédure existante quand le CEO sera prêt à retester.

## READY FOR CEO DAILY ROUND GAMEPLAY RETEST
