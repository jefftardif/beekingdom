# 49 — Matrice des exigences : design -> implémentation -> preuve

Ce document relie les décisions/idéaux du jeu aux futures surfaces de code et à leurs preuves. Les fichiers C# futurs sont des **pistes d'implantation**, pas des chemins déjà vérifiés tant qu'ils sont notés « à créer ».

| Exigence | Décision / Idée | Couche code attendue | Tests et preuve | Lot |
|---|---|---|---|---|
| Conserver carte/tuiles | D-001, POL-001 | WorldMapMmoFullscreenFoundationBootstrap intégration overlay | screenshot avant/après, aucune tuile modifiée | P2 |
| Sites visibles et lisibles | POL-003, POL-045 | WorldMap / point-of-interest présentation | visuels zoom & filtre | P2 |
| Pollinisation 3e pilier | D-002, POL-004 | mission + écologie + économie | E2E récompense et effet réel | P3 |
| Zones publiques | D-003, POL-007 | FloralSiteAccessPolicy | joueur sans alliance | P2 |
| Territoires alliance | D-003, POL-021 | services alliance/relais | permissions et persistance | P5 |
| Solo limité régionalement | D-004, POL-005 | project minDistinct/ledger | QA-027, Gherkin solo | P4 |
| Niveau joueur impact | D-005, POL-006 | formation/génétique/recherche | QA-031, comparaison | P4 |
| Niveau alliance impact | D-005, POL-006 | AllianceFloralProgression | QA-030 + preuve level stocké | P4 |
| Activité vs roster inactif | POL-027 | contribution qualified | 30 actifs vs 100 inactifs | P4 |
| Essaim civil séparé | D-006, POL-008 | CivilPopulationState/Reservations | QA-009/011 | P1 |
| Cohorte naissance/capacité | POL-017 | Nursery/CivilCohort | QA-007/008/014 | P1 |
| Formation et spécialisations | POL-018/019 | Academy/Research/Genetics adapter | tests disponibilité/caps | P4 |
| Championne civile | POL-019 | reservation championne | non-double assignation | P4 |
| Escorte militaire | D-007, POL-009 | port CombatSquadReservation | QA-037/039 | P5 |
| Stocks et gains honey/pollen | POL-016/043 | FloralSite + RewardLedger | QA-013/016/021 | P3 |
| 3 modes de mission | POL-012/028 | MissionModeCatalog | QA-018/ALPHA-07 | P4 |
| Familles et routes | POL-015 | FloralCompatibility + expedition | QA-019 | P3 |
| Fertilité/régénération | POL-010/011/029 | FloralWorldService | QA-017/022 | P2/P3 |
| Points de groupe/participants | POL-014/025 | PollinationProject/ledger | QA-027/028 | P4 |
| Relais et conquête | POL-021/022/026 | FloralOutpost, droits, Combat | QA-040 | P5 |
| Pacte et coalition | POL-024/025 | AllianceDiplomacy extension | QA-034/035 | P6 |
| Petites alliances viables | POL-027 | coûts, caps, coalitions | simulation 15 + test | P6 |
| Menaces parasites et floraison | POL-031/032 | FloralEventService | QA-043 | P6 |
| Ours mondial coopératif/compétitif | D-008, POL-033 | BearRaidWorldEvent | QA-041/042 | P6 |
| Visibilité UI, FR/EN | POL-041/045 | localisation/screens | QA-044..048 | chaque |
| Hors ligne/officiel | POL-020/040 | timed state + receipts | QA-020/021/025 | P3 |
| Pas de régression | POL-043 | migrations + contrat | QA-050/053 | chaque |
| Anti-exploit | POL-027/040 | auth, CAS, unique ledger | QA-001..006, QA-049/054 | chaque |

## Matrice d'interface avec code EXISTANT

| Source existante | Adapter (si justifié) | Ne pas casser |
|---|---|---|
| Assets/BeeKingdom/World/FlowerManager.cs | catalogue/cycles/visuels | simulation labo + tests |
| FlowerPatch.cs / PollinationRules.cs | concepts régionaux/horloge | aucun crédit client |
| WorldMapMmoFullscreenFoundationBootstrap.cs | marqueurs/POI/sélection | paysage et tuiles |
| WorldMapMarchRegistry.cs | projection nouvelle catégorie civile | combat/collection existants |
| WorldResourceCollectionService.cs | patterns d'auth/recettes | CommittedTroops militaires |
| NurseryCohortService.cs | règles cohorte et récupération | RecruitsInReserve et nutrition/stabilisation |
| AcademyTrainingService.cs | pattern formation/certificats | 4 matières existantes |
| HiveOperationModels.cs / HiveStateMigrator.cs | sous-état civil additionnel | anciennes ruches/soldes |
| AllianceService/AllianceModels | permissions, adhésion | alliance/chat/membres |
| AllianceDiplomacy.cs | pacte floraison spécialisé | relations déjà modélisées |
| AllianceResearchBonusResolverAdapter.cs | inspiration ports/bonus | pas de dépendance circulaire |
| WorldMapBearDenLandmark.cs | activer rendu événement | repère permanent |
| WorldPresenceService.cs | éventuellement affichage ambiant | **ne PAS l'utiliser comme registre mondial** |

## Définition d'une exigence vraiment terminée

- code autoritaire présent, feature flag documenté ;
- migration et compatibilité validées ;
- test automatisé + scénario deux clients si partagé ;
- révisions et receipts inspectables ;
- interface réelle et FR/EN si visible ;
- comportement OFF/rollback ;
- preuves et commit liés à l'exigence ;
- trace de décision mise à jour.

Une exigence seulement décrite dans un .md reste au statut SPECIFIED, jamais IMPLEMENTED.

