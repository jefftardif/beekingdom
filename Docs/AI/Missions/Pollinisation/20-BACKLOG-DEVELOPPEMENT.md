# 20 — Backlog opérationnel pour les développeurs (épics, tâches et garde-fous)

**Attention :** backlog à lancer seulement lorsqu'on décide de quitter le brainstorming. Aucune tâche ci-dessous n'a été exécutée dans cette mission de documentation.

## Cadre d'exécution

- Branche officielle main présente sur PCJEFF avec modifications préexistantes ; effectuer inventaire et travail isolé sans reset ni stage global.
- Ne modifier ni les tuiles, ni les biomes graphiques, ni l'art général de la carte.
- Garder systèmes militaires, collecte existante, Académie, Nurserie, Alliances, Recherche, Championnes et save compatibles.
- Unity ne détient jamais les soldes, effectifs, horloges, récompenses ou fertilité officiels.
- Chaque tâche inclut un test FR-CA/EN-US si elle expose une interface.
- Toujours citer dans les PR : numéro tâche, numéro idée POL-..., arbitrage D/Q-... et preuve de test.

## EPIC P0 — Spécification stabilisée

| Ticket | Tâche | Livrable vérifiable |
|---|---|---|
| P0-01 | Auditer IDs de bâtiments/niveaux et l'état réel des services | Matrice actuelle capacités / faux-semblants |
| P0-02 | Auditer NurseryCohorts, AcademyTraining, Research, Genetics, Champion Hall et recrutements | Compatibilité explicitement approuvée |
| P0-03 | Auditer WorldResourceCollection, WorldPresence, monde Unity, Alliance et combat | Délimitation des modifications + interfaces |
| P0-04 | Arbitrer Q-01 à Q-08 du registre 17 et paramètres Alpha | Contrat produit daté |
| P0-05 | Définir stratégie transactionnelle inter-agrégats/versions/flags | ADR approuvée et test de concurrence préparé |

**Bloquant :** pas de premier push de code civil sans P0-05.

## EPIC P1 — Population civile

| Ticket | Tâche | Livrable vérifiable |
|---|---|---|
| P1-01 | Créer modèle durable CivilPopulationState et migration additive | Vieille ruche conservée ; stock civil initial explicite |
| P1-02 | Cohorte civile Nurserie : démarrer, maturer, réclamer, receipts | Double clic exact-once |
| P1-03 | Réserver/libérer N civils par mission | 2 lancements concurrents ne partagent pas les civils |
| P1-04 | Gérer retour, fatigue, blessés et récupération initiale | Conservation de population démontrée |
| P1-05 | API authentifiée CivilPopulation et cohorte | 401/403/409 ; révisions et serveur UTC |
| P1-06 | Surface de Nurserie civile premium FR/EN | UI fonctionnelle sans régression de soins/stabilisation |

**Périmètre :** aucune armée militaire modifiée, pas encore de floraison.

## EPIC P2 — Monde floral et intégrité

| Ticket | Tâche | Livrable vérifiable |
|---|---|---|
| P2-01 | Catalogue floral stable et IDs identiques serveur/Unity | 2 à 3 sites publics pilotes |
| P2-02 | Stock durable FloralSiteState (réserves/fertilité/biodiversité/phase) | Même état lu par deux joueurs |
| P2-03 | Mise à jour écologique différée ou tick déterministe, bornée | Pas de réserve/fertilité négative |
| P2-04 | Vérification de concurrence pour récolte partagée | Pas de double prélèvement |
| P2-05 | Détail site en API avec révision, disponibilité, restriction | Contrat de lecture versionné |
| P2-06 | Marqueurs/filtres et fiche Unity dans la carte canonique | Terrain et tuiles inchangés ; clic/sélection corrects |

## EPIC P3 — Expédition civile E2E

| Ticket | Tâche | Livrable vérifiable |
|---|---|---|
| P3-01 | Définir états/horodatages/réservation/cargaison de CivilExpedition | Automate versionné |
| P3-02 | Démarrer mission server-authoritative avec coût/idempotence | Double start = même reçu |
| P3-03 | Résoudre trajet, source/destination, compatibilité et fin hors ligne | Coupure du client sans perte |
| P3-04 | Crédit de récolte, effet écologique, libération civils, rapport | Exact-once multi-agrégats |
| P3-05 | Contrôleur Unity lié identité/ruche, préparation et suivi | Pas de bouton qui clignote |
| P3-06 | Simulation multijoueur de deux missions sur même site | Pas de duplication de réserves |
| P3-07 | Ajouter modes intensive/équilibrée/restauration après arbitrage | Résultats distincts vérifiés |

## EPIC P4 — Alliance et progression

| Ticket | Tâche | Livrable vérifiable |
|---|---|---|
| P4-01 | Ledger global de contributions par expédition | Une contribution = un reçu immuable |
| P4-02 | Projet floral et jauges partagées, membres distincts qualifiés | Solo bloqué au grand palier |
| P4-03 | Bonus/slots/paliers selon niveau d'alliance avec caps | Alliance inactive ne « gagne » pas seule |
| P4-04 | Formation civile Académie compatible avec les 4 matières existantes | Certificats anciens inchangés |
| P4-05 | Effets Recherche/Génétique/Championnes bornés et affichés | Bonus explicable, zéro double application |
| P4-06 | Projets floraux dans Alliance Activités v4, avec permissions | UI FR/EN, rôles serveur, scroll |
| P4-07 | Bilan contributions et récompenses individuelles/collectives | Historique et replays sûrs |

## EPIC P5 — Territoires / militaire

| Ticket | Tâche | Livrable vérifiable |
|---|---|---|
| P5-01 | Décider modèle Ruche-Mère/balises/relais avec architecture existante | ADR et règles de contrôle |
| P5-02 | Relais floral en position mondiale, propriétés et droits | Place, lit, modifie sous autorité |
| P5-03 | Escorte de soldats via système officiel sans double réservation | Civils et militaires indépendants |
| P5-04 | Capture, défense et transfert de relais | Fertilité inchangée |
| P5-05 | Protection novices/zone publique et gestion des pertes | Aucun griefing trivial |
| P5-06 | Filtre/frontières d'alliance sur la carte | Lisible sans changer le terrain |

## EPIC P6 — Diplomatie / événements mondiaux

| Ticket | Tâche | Livrable vérifiable |
|---|---|---|
| P6-01 | Pactes de floraison, accès et expiration | Deux alliances signent avec rôles |
| P6-02 | Coalition et projet multi-alliances | Bonus et classements tracés par source |
| P6-03 | Grande floraison / parasites / fleur rare | État temporaire serveur persistant |
| P6-04 | Tanière de l'ours comme événement mondial coopératif-compétitif | Raid interalliances, classement et retrait |
| P6-05 | Variantes saisonnières et biomes sans refaire tuiles | Paramètres contrôlés, tests charge |

## Ordre des dépendances

P0 -> P1 -> P2 -> P3 -> P4 -> P5 -> P6.  
Les UI de lecture peuvent avancer avec mocks de tests, **mais ne jamais simuler une mutation en production**.

## Livrables de preuve par ticket

- Fichiers modifiés explicités.
- Compilations/tests automatisés pertinents.
- Contrats et schémas migrés/documentés.
- Preuve E2E à deux sessions si état partagé.
- Capture Unity FR/EN pour interactions UI.
- Audit rapide des effets sur la collecte militaire et les bâtiments.
- Validation feature flag/rollback.
- Mise à jour des documents 01, 17, 18 en cas de modification de scope.

## Définition de « prêt pour le développement »

Le créateur a approuvé les paramètres minimums et la politique de risques ; P0 termine l'audit des API et états ; une équipe peut prendre P1-01 sans inventer les règles fondamentales.

