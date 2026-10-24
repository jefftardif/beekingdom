# 34 — Observabilité, performance MMO et exploitation

## Pourquoi ce chantier est sensible

La pollinisation devient une source de mutations **partagées par beaucoup de joueurs**. Un service de snapshot qui parcourt toutes les ruches pour compter les participants est inadapté. Le serveur doit fonctionner hors connexion et résister à des pics d'événement mondial.

## Indicateurs de santé proposés

- pollination_site_reads_total, pollination_site_read_latency_ms (p50/p95/p99).
- civilian_mission_starts_total, civilian_mission_rejected_total{reason}, civilian_mission_settlements_total.
- civilian_mission_settlement_lag_seconds (temps entre échéance et règlement).
- civilian_reservation_conflicts_total, flora_cas_conflicts_total, project_ledger_dedup_total.
- floral_resource_delta{resourceKey} par jour, by source/settlement type.
- floral_fertility_average et floral_depletion_region_count.
- floral_project_completion_rate, min_distinct_members_bottleneck_count.
- floral_rare_reward_concentration, champion_bonus_distribution.
- floral_active_missions, floral_active_project_count, floral_active_relay_count.
- backend outbox_pending_count, outbox_retry_count, reconciliation_mismatch_count.
- bloom_event_participants, unique_alliances, bear_reward_claims (phase ultérieure).

L'agent doit utiliser la télémétrie déjà en place dans le serveur, pas réinventer un monitoring concurrent.

## Journaux de diagnostic

Sur une mutation : correlationId, actorPlayerId interne, hiveId, siteId, expeditionId, previousRevision, nextRevision, resultCode, latency, catalogVersion, idempotencyReplay (bool). Les montants de ressources sont des champs audit contrôlés ; éviter les tokens, identifiants de connexion, données privées de comptes.

Ne pas journaliser la charge complète de chaque requête si elle expose des informations confidentielles.

## Objectifs de charge à valider avant implémentation

**Tests exploratoires proposés** :
- 2 joueurs sur 1 site pour la preuve de cohérence.
- 20 joueurs contribuant au même champ.
- 100 joueurs répartis sur 10 sites.
- 500 expéditions concurrentes sur un ensemble de régions en laboratoire.
- 2 à 5 alliances partenaires sur un événement multi-alliance.

Ces nombres ne sont pas des SLA approuvés. Capturer CPU/mémoire, temps de SQL, p95, nombre de conflit CAS, retard des règlements, dérive des soldes.

## Stratégies de performance

- Lectures de régions/sites paginées ; index siteId, regionId, ownerHiveId, projectId.
- Agrégats de classement et projets maintenus par ledger indexé ; pas recomptés à chaque frame Unity.
- Calcul de régénération « lazy settlement » depuis UTC avec cap de rattrapage.
- Rafraîchissement UI par changement de revision, pas full rebuild.
- Fallback polling modéré si pas de push ; les événements peuvent augmenter la fréquence localement.
- Compression/limitation des snapshots lourds ; résultats riches seulement sur demande.
- Ajustement du débit de règlements aux limites du serveur, sans perte de cohérence.
- Contrats de stockage durables testés simultanément dans les implémentations réellement actives.

## Alarmes de comportement anormal

- Récompenses positives alors que site est vide.
- Nombre de civils engagés supérieur à la population.
- Rapport de projet 100 % avec un seul joueur si min distinct >1.
- Transactions non clôturées depuis longtemps.
- Hausse inhabituelle des retries sans clé stable.
- Écart entre total des contributions et progression matérialisée.
- Alliance capturant trop de relais trop vite malgré coût d'entretien.
- Top 1 % obtenant la majeure partie de l'économie sans action proportionnelle.

## Outillage QA

Un rapport quotidien de test peut calculer :
- nombre de missions et rejets par cause ;
- delta net de honey/pollen/wax provenant des expéditions ;
- distribution des temps ;
- plages de fertilité par site ;
- succès et échecs des projets ;
- opérations non résolues et recettes en attente.

Ne pas créer d'alertes automatiques / tâches planifiées côté production sans validation opérationnelle.

## Événements mondiaux

Prévoir une montée en charge sur des fenêtres de raid/Grande Floraison ; annoncer des périodes suffisamment larges pour divers fuseaux. Classements calculés côté serveur sur contributions authentifiées, récompenses exact-once et mécanisme de coupure/retour en cas de panne.

## Résultats à livrer par agent

Tableau mesures avant/après, charge du serveur testée, endpoints instrumentés, index ajoutés, régressions identifiées, limites connues et rollback. Ce document est une spécification, pas un diagnostic de capacité du SRVESDT réel.

