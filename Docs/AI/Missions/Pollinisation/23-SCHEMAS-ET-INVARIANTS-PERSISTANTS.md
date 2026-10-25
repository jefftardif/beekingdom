# 23 — Schémas de données, indexes, invariants et migrations

## Orientation

Ce fichier décrit le **contrat logique durable** ; la forme SQL/JSON réelle doit découler des repositories actuels. Ne pas créer les tables ci-dessous sans audit des conventions et schémas officiels. Les types numériques et temps sont proposés pour être déterministes.

## Règles de types

- IDs joueurs/ruches/alliances : GUID métier officiels.
- IDs monde/région/site/famille/mission : identifiants stables, Unicode interdit dans clés internes si convention existante.
- Horodatage : DateTimeOffset UTC (offset zéro) côté C# ; TIMESTAMPTZ ou représentation SQL équivalente réellement disponible.
- Révisions : int64 monotones avec checked ; accès optimiste (CAS).
- Économie : int64 non négatif ; sécurité contre overflow, pas de double flottant pour débit/crédit.
- Indices écologiques : basis points (0..10000) recommandés, affichés en 0..100 %, pas d'incertitude par double.
- Contributions : unités entières de valeur écologique, avec plafond, source immutable et ID unique de livraison.

## Agrégat CivilPopulation

| Champ | Type logique | Note |
|---|---|---|
| hiveId / playerId | GUID | owner validation |
| schemaVersion | int | migration additive |
| revision | int64 | CAS |
| availableBySpecialty | map key -> int64 | seuls civils disponibles |
| incubatingCohorts | list bornée | commence/termine serveur |
| trainingReservations | ID -> amount | éventuel futur |
| expeditionReservations | expeditionId -> worker counts | empêche double départ |
| injured / recovering | map + échéance | libération idempotente |
| slots | int | version/caps |
| receipts | stockage anti-rejeu | durée garantie à définir |

**Conservation :** civils créés par cohortes/événements autorisés moins pertes explicites = disponibles + réservés + récupération + blessés. Transitions ne peuvent produire d'effectif positif sans événement de création traçable.

## Agrégat FloralSiteState (PARTAGÉ)

| Champ | Type logique | Note |
|---|---|---|
| worldId / regionId / siteId | clé unique | ne dépend pas des objets créés client |
| catalogVersion | texte versionné | stabilité des familles |
| flowerFamilyId | texte | définition du catalogue |
| lifecycle / phaseEndsAtUtc | enum/UTC | croissance, floraison, repos |
| fertilityBp / biodiversityBp | entier 0..10000 | borné |
| nectarStock / pollenStock | entiers | **réserves florales**, pas portefeuille |
| nectarCapacity / pollenCapacity | entiers | bornes |
| lastSettledAtUtc | UTC | régénération déterministe |
| serverRevision | int64 | CAS |
| regionModifiers | snapshot/version | appliqués une fois |

Ne pas stocker mécaniquement « ownerAllianceId » dans cet agrégat comme une vérité de propriété écologique. Les droits d'accès peuvent référencer territoire/relais séparés.

## Agrégat CivilExpedition

- expeditionId unique ; ownerPlayerId et hiveId.
- worldId/regionId/sourceSiteId/destinationSiteId nullable.
- reservedWorkers[{specialty,count}], championReservationId?, militaryEscortReservationId?.
- state, mode, startedAtUtc, reachesSourceUtc, workEndsUtc, reachesDestinationUtc, returnAtUtc, settledAtUtc?.
- plannedCargo/cargoActual conservés séparément ; pollenLoaded/pollenDelivered, ecoContribution.
- costReceiptId, completionReceiptId, stateRevision, clientIdempotencyKey+payloadHash.
- projectId et allianceAtStart (facultatifs), qualifyingMemberId.
- disruptionReason, abortPolicy, lastErrorCode, auditUpdatedAtUtc.

En cas de crash, la reprise traite l'expédition en cours et ses réservations, jamais un lancement « frais » à partir du dernier écran Unity.

## Projet partagé / contribution

- projectId, allianceId initiatrice, partenaires, region/site IDs, periodId, version.
- pointGoal, minQualifiedUniquePlayers, routeDiversityGoal, phase, deadlines, stateRevision.
- **PollinationContribution** ID unique = expeditionId + deliveryPhase (ou clé métier) ; playerId, hiveId, allianceIdAtContribution, projectId, siteId, amount, routeKind, settledAtUtc, credited/claimed.
- **ProjectMemberAggregate** (projectId, playerId) : totalPoints, qualifies, lastActivityAtUtc ; classement efficace.
- Compteur de membres distincts dérivé de participants qualifiés, jamais du nombre de missions.
- Récompenses de projet liées à (projectId, rewardTierId, recipientId) unique.

**Exemples de contraintes de base :**
- unique(worldId,siteId) ;
- unique(expeditionId) ;
- unique(expeditionId,deliveryOrdinal) ;
- unique(projectId,playerId) sur l'agrégat des membres ;
- unique(beneficiaryId,projectId,rewardTierId) pour crédits ;
- unique(ownerId,idempotencyKey,operationType) tant que le replay est autorisé.

## AllianceProgressionState recommandé

Ne pas supposer l'existence d'une propriété Alliance.Level. Le record AllianceEntity observé n'en contient pas. Stock séparé ou extension additive :
- allianceId, revision, level, experience, contributionTotalBySeason, unlockedProjectTier, activeProjectSlots, relaySlots, researchedFloralBonus.
- Niveau/XP gagné selon **activité collective admissible**, avec anti-exploit et garde-fous anti-farming.
- Récompenses/fonctions découlent du niveau authoritative, pas d'un champ client modifiable.
- Ne pas convertir arbitrairement les dons de recherche d'alliance en XP sans règle de conception.

## Stratégie migration / anciennes données

**Principe A :** ancienne ruche possède CivilPopulation=null -> vue « non initialisée », sans population inventée. Une activation explicite peut ouvrir un starter pack décidé en produit, via receipt serveur, une seule fois.

**Principe B :** les champs inconnus, modules existants et révisions restent intacts ; modèles additifs avec valeurs par défaut sûres.

**Principe C :** le rollback applicatif ne doit pas effacer la nouvelle donnée ; refuser les nouvelles mutations sous flag OFF ou DRAIN.

**Principe D :** migration qui lit un record ancien et le sérialise à nouveau : sauvegarde conservée byte/meaning, pas d'écrasement des champs ultérieurs.

## Stratégie données mondiales

Les sites/états/projets appartiennent à un repository partagé (DB) séparé du sous-état d'une ruche. L'agent doit **définir et tester** la transaction entre les deux. Le monde ne peut pas reposer sur 2 copies de fertilité dans les deux ruches.

## Horloge et régénération

Calcul à intervalle [lastSettledAtUtc,nowUtc] tronqué, arrondi déterministe. Règle de maximum de rattrapage après arrêt long : config explicite pour éviter une floraison infinie d'un site inactif ; le surplus éventuel est abandonné **uniquement pour un site**, jamais au détriment d'un solde de joueur.

## Journal auditable

Chaque fait modifiant économie/population/écologie/contribution reçoit correlationId, operationId, actor, target, before/after revision, amount, serverTimeUtc et source (cohorte/mission/projet). Pas de PII superflue dans les logs d'activité publique.

## Tests schémas obligatoires

Migration vieux compte ; sauvegarde avec modules inconnus ; crash après débit/avant mission ; crash après prélèvement/avant récompense ; deux claims ; projet terminé pendant changement d'alliance ; app redémarrée ; clean rollback ; SQL vs JSON si les deux backends restent pris en charge.

