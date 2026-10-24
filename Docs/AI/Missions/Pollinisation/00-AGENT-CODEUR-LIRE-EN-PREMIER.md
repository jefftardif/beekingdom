# LIRE EN PREMIER — Mission complète de pollinisation BeeKingdom

**DESTINATAIRE : AGENT CODEUR / ARCHITECTE UNITY + .NET**

Ce répertoire a été rédigé pour transférer **toutes les idées de conception** de la pollinisation et rendre leur implantation réalisable, contrôlable et sécuritaire.

## Ce que le fondateur a VALIDÉ

- La carte du monde actuelle est **belle**, on ne refait pas son paysage ni ses tuiles.
- Le problème est l'insuffisance des **activités, objectifs, interactions et installations** qu'elle contient.
- La pollinisation devient un **grand pilier économique et territorial**, au même niveau que les combats et les alliances.
- Système **hybride** : lieux publics accessibles aux solitaires ; territoires d'alliance avec installations, avantages et projets.
- Un joueur seul ne peut pas polliniser un grand champ ou une région ; la **coopération réelle** des membres est déterminante.
- Le **niveau de l'alliance**, le **niveau et les compétences de ses joueurs**, et leur **activité effective** ont un impact concret sur la capacité de pollinisation.
- Les pollinisatrices sont des **essaims civils spécialisés**, distincts des armées ; les militaires servent aux escortes, défenses et conquêtes.
- La pollinisation doit engendrer **alliances, pactes, coalitions, rivalités et conflits économiques**.
- La tanière de l'ours a déjà pour vocation d'être un **événement mondial coopératif et compétitif entre alliances**.
- Il faut construire une activité **stratégique, durable et plaisante**, pas une micro-corvée répétitive.

## Avertissement de statut

Les fichiers utilisent quatre étiquettes :
- **[APPROUVÉ]** décision explicite du fondateur ;
- **[RECOMMANDÉ]** direction technique/game design proposée, révisable ;
- **[À TRANCHER]** règle encore ouverte ;
- **[VÉRIFIÉ]** constat ponctuel effectué dans le code local (ne prouve pas un déploiement).

Les exemples de chiffres/coûts/durée et les API indiquées « proposées » ne sont pas déjà implémentés ni validés comme équilibrage officiel.

## Parcours de lecture conseillé — 20 minutes pour une première compréhension

1. README.md + 01-VISION-DECISIONS.md (vision et non négociables).
2. 17-DECISIONS-OUVERTES.md + 18-TRACABILITE-IDEES.md (éviter d'oublier une idée).
3. 21-AUDIT-TECHNIQUE-CODE-CANONIQUE.md (ce qui existe et pièges).
4. 03-POPULATION-CIVILE.md + 05-ECONOMIE-ESSAIMS.md (coeur des civils).
5. 07-ROUTES-ET-EXPEDITIONS.md + 08-ALLIANCES-TERRITOIRES.md (boucles MMO).
6. 22-ARCHITECTURE-CIBLE-COMPOSANTS.md + 23-SCHEMAS-ET-INVARIANTS-PERSISTANTS.md + 25-AUTOMATES-SEQUENCES-ET-CAS-LIMITES.md (autorité serveur).
7. 14-PLAN-IMPLEMENTATION.md + 20-BACKLOG-DEVELOPPEMENT.md (ordre et tickets).
8. 16-TESTS-ACCEPTATION.md + 33-TESTS-GHERKIN-FONCTIONNELS.md (preuves).
9. 38-PROTOCOLE-AGENT-CODEUR-SANS-REGRESSION.md + 39-PARAMETRES-ALPHA-RECOMMANDES.md (méthode et paramètres ouverts).
10. 40-PROMPT-AGENT-CODEUR.md (ordre de mission réutilisable intégralement).

Les autres documents approfondissent un domaine particulier : formation, écologie, mathématiques, catalogue, UI, diplomatie, conflit, événements, déploiement et performance.

## À faire EN PREMIER (Lot 0)

- Lire Git status/HEAD et inventaire de modifications préexistantes.
- Examiner FlowerManager/FlowerPatch/PollinationRules côté Unity : simulation réelle existante, pas moteur MMO officiel.
- Examiner WorldResourceCollectionService côté serveur : collecte actuelle mobilisant des troupes militaires.
- Examiner NurseryCohorts, Academy, ressources, HiveStateMigrator et WorldMapMarchRegistry.
- Examiner AllianceEntity : pas de Level explicite dans le record inspecté ; le niveau d'alliance doit donc être conçu sérieusement.
- Examiner AllianceDiplomacy : relations déjà modélisées, ne pas recréer diplomatie parallèlement.
- Choisir la stratégie d'**atomicité partagée ruche + champ + projet** avant la première mutation.
- Dresser les choix réellement bloquants et proposer une ADR sans inventer des règles irréversibles.

## Premier résultat à réaliser sous flag de test

Une cohorte d'abeilles civiles -> une expédition sur site floral officiel -> pollen transporté/état de site partagé -> retour des civils et ressources -> rapport. Puis faire contribuer DEUX joueurs au même projet et vérifier que le même joueur ne peut se faire passer pour plusieurs.

Un « bouton Polliniser » qui ne fait qu'animer une scène n'est PAS une livraison valide.

## Interdictions

Aucune refonte de la carte/tiles, double solde, conversion des armées en civils, suppression de données, reset Git, conflit avec travaux d'autres agents, faux état de production, nouvelles monnaies/15e bâtiment par défaut, promesse de PvP sans sécurité ni tests.

## Livrables minimaux de chaque incrément

- Contrat d'API/schema et choix de migration.
- Code testé et flags explicités.
- Preuve d'intégrité des populations, stocks et soldes.
- Test hors connexion et concurrence si mutation partagée.
- Rendu Unity FR/EN si concerné.
- Non-régression des fonctions existantes.
- Rapport de commit/PR/déploiement réellement effectué (ou non).
- Docs 17/18 actualisées si décisions/règles changent.

**Commencer par 40-PROMPT-AGENT-CODEUR.md pour prendre la mission en charge.**

