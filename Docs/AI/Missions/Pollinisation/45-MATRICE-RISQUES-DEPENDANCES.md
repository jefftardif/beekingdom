# 45 — Matrice de risques et décisions bloquantes

## Pourquoi ce registre

Les idées de pollinisation exigent de nouveaux états partagés et peuvent déstabiliser combat, ressources, alliances et sauvegardes si elles sont ajoutées sans garde-fous. Classer les risques aide l'agent à travailler dans le bon ordre.

| Risque | Probabilité initiale | Gravité | Mesure attendue |
|---|---|---|---|
| Deux joueurs récoltent le même pollen | élevée si stockage par ruche | critique | site central partagé + transaction/CAS |
| Double claim via retries | élevée si absence receipt durable | critique | idempotency + contrainte unique |
| Cohortes dupliquent civils/recrues | moyenne | critique | population civile dédiée, audit Nurserie |
| Championne et soldats utilisés deux fois | moyenne | élevée | réservation multi-système côté serveur |
| UI montre gains fictifs | moyenne | élevée | client projections snapshot officiel |
| Simulation Unity contredit site serveur | élevée sans adaptation | élevée | FlowerManager local en lecture/cosmétique MMO |
| Level d'alliance inexistant | certaine dans record inspecté | élevée | nouveau vrai modèle/progression, pas mock |
| Nouvel objet de map change tuiles/collision | moyenne | élevée | overlay/POI, IDs stables, QA scènes |
| Changement d'alliance pendant mission | moyenne | élevée | snapshot affiliation + droits au commit |
| Alliance coupe accès d'un civil en route | moyenne | élevée | règle de repli/missions existantes |
| Exploitation des bonus level | moyenne | élevée | caps, minimum membres distincts |
| Récompenses ours monopolisées | élevée sans politique | élevée | participations et classements séparés |
| Corps de code partagé écrasé par autre agent | élevé sur main modifié | critique | travail isolé, aucun reset/stage global |
| Migration efface données anciennes | moyenne | critique | additive, sauvegarde, tests vieux snapshots |
| Déploiement sans QA | moyenne | critique | flags OFF/QA, rollback, preuves E2E |

Les niveaux d'appréciation sont des hypothèses de planification, pas des statistiques de bugs mesurées.

## Dépendances techniques incontournables

- Population civile -> cohortes/Nurserie et persistance PlayerHiveState.
- Départ mission -> existence et réserve des civils + sites et catalogue.
- Récolte/transport -> transaction monde partagé + wallet officiel.
- Projet collectif -> ledger expédition unique + identité des joueurs.
- Niveau d'alliance -> véritable progression d'alliance + permissions officielles.
- Relais -> contrôle territorial et accès + identité stable.
- Escorte -> réservations militaires officielles + système de combat.
- Pactes -> modèles de diplomatie existants + droits d'accès.
- Ours mondial -> moteur événement partagé + ranking + récompenses.

Aucune dépendance n'est résolue par une simple illustration ou booléen côté Unity.

## Critères de blocage

**Bloquer lancement de nouvelles mutations** si :
- absence de stratégie transactionnelle autoritaire ;
- risque connu de duplication de soldes/population ;
- migration non testée ;
- auteur/rôle de commande non authentifié ;
- nouvelle fonctionnalité dépend d'un « niveau d'alliance » affiché en mock ;
- incapacité à désactiver ou drainer des missions déjà actives.

**Ne pas bloquer une PR de lecture seule** si :
- API renvoie site officiel sans mutation ;
- écran est explicitement « prévisualisation » ;
- simulation editor/mock reste hors prod et ne prétend pas créditer.

## Plan de mitigation par lots

P0 : architecture et tests transactionnels.  
P1 : population civile conservation.  
P2 : site partagé et version.  
P3 : mission + ledger + crash/replay.  
P4 : vrais niveaux/projets/participants distincts.  
P5 : territoriale, combat et pactes.  
P6 : événements, ours, grande échelle.

## Validation du risque après chaque PR

Mesurer en tests : solde net, effectifs engagés, stock site, nombre contributions, idempotency replay, reconstitution après arrêt serveur, temps réponse et état Unity. Chaque risque critique exige une preuve dédiée, pas une affirmation narrative.

