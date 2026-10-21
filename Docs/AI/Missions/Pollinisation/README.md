# BeeKingdom — Dossier maître : système de pollinisation

**Date de création :** 2026-10-20  
**Statut :** conception approuvée sur les principes ; implantation NON commencée par cette mission documentaire  
**Référence :** cette arborescence est la source de vérité des exigences de pollinisation, à rapprocher des systèmes BeeKingdom existants avant développement.

## Raison d'être

La carte du monde est le véritable terrain de jeu de BeeKingdom. Le paysage et les tuiles sont conservés : ce sont les **activités**, les **objets interactifs**, les **mouvements**, les **enjeux collectifs**, les **frontières/contrôles** et les **événements** qui doivent donner vie au royaume.

La pollinisation est désormais un pilier stratégique majeur, **au même niveau que la guerre et les alliances**. Formule directrice :

> Une ruche peut survivre seule. Une alliance peut faire prospérer une région. Une coalition peut faire fleurir un royaume.

## Décisions irrévocables sauf nouvelle instruction du créateur

1. Pas de refonte du paysage ni des tuiles.
2. Système de pollinisation **hybride** : zones publiques ouvertes, territoires d'alliance avec avantages et installations.
3. La pollinisation à grande échelle est **collective** ; le solo reste utile mais a peu d'impact régional.
4. Les **niveaux de l'alliance et de ses joueurs**, la qualité des membres et leur activité réelle ont un impact sur la capacité collective.
5. Essaims **civils spécialisés, distincts des armées** ; militaires pour escorte, protection et conquête.
6. Interactions entre pollinisation, Nurserie, Académie, Génétique, Recherche, Hall des championnes, économie, alliances, combats et carte.
7. Pollinisation = cause concrète de diplomatie, pactes, rivalités et conflits territoriaux.
8. La tanière de l'ours est conçue à l'origine comme un événement mondial : alliances coopèrent contre l'ours tout en rivalisant pour les meilleures récompenses.
9. La mécanique ne doit pas se réduire à une barre de progression ou à une corvée répétitive.
10. Les opérations économiques et multijoueurs doivent faire autorité **sur le serveur** et persister hors connexion.
11. Les interfaces doivent fonctionner en **français et anglais**.
12. Ne pas réécrire ni détourner les systèmes existants sans audit, tests et migration sûre.

## Comment utiliser ce dossier

| Fichier | But |
|---|---|
| 01-VISION-DECISIONS.md | Vision, règles approuvées, limites et statut des propositions |
| 02-BOUCLES-DE-JEU.md | Parcours joueur et boucles de gameplay |
| 03-POPULATION-CIVILE.md | Naissance, recrutement, effectifs, cycle de vie et réservations |
| 04-FORMATION-SPECIALISATIONS.md | Académie, Génétique, championnes et progression |
| 05-ECONOMIE-ESSAIMS.md | Coûts, entretien, stockage, rendement, pertes et équité |
| 06-SITES-ET-ECOLOGIE.md | Sites floraux, fertilité, biodiversité, cycles et régénération |
| 07-ROUTES-ET-EXPEDITIONS.md | Pollen, compatibilité florale, missions et logistique |
| 08-ALLIANCES-TERRITOIRES.md | Niveau d'alliance, relais, projets et souveraineté |
| 09-DIPLOMATIE-PACTES.md | Pactes, coalitions, droits d'accès et partage |
| 10-COMBATS-ESCORTES.md | PvP/PvE, protection, conquête et risques |
| 11-EVENEMENTS-MONDIAUX.md | Grandes floraisons, parasites, fleurs rares, ours |
| 12-UX-CARTE-FR-EN.md | Points d'intérêt, filtres, fenêtres, accessibilité, localisation |
| 13-ARCHITECTURE-DONNEES-API.md | Services, états, transactions, autorité, APIs proposées |
| 14-PLAN-IMPLEMENTATION.md | Lots, séquencement, dépendances, Alpha/Bêta, critères de sortie |
| 15-EQUILIBRAGE.md | Paramètres à simuler et valeurs exploratoires |
| 16-TESTS-ACCEPTATION.md | Cas de tests QA, sécurité, charge et anti-régression |
| 17-DECISIONS-OUVERTES.md | Points non tranchés et registre de décisions |
| 18-TRACABILITE-IDEES.md | Inventaire exhaustif de nos idées et où elles sont spécifiées |
| 19-AUDIT-EXISTANT.md | Points d'ancrage réels vérifiés dans le dépôt et limites de l'audit |
| 20-BACKLOG-DEVELOPPEMENT.md | Tickets de développement, dépendances et livrables QA |

## Conventions de développement

- **[APPROUVÉ]** = décision explicite du créateur.
- **[RECOMMANDÉ]** = proposition de conception, amendable.
- **[À TRANCHER]** = choix ouvert, ne pas coder comme règle fixe.
- **[VÉRIFIÉ]** = point observé dans un fichier réel au 2026-10-20, non garantie de déploiement.
- Les coûts, niveaux, durées, tailles de groupe, formules et chemins d'API en exemple sont **indicatifs**, jamais des valeurs finales approuvées.
- Chaque travail de développement devra pointer vers les paragraphes concernés, mettre à jour les tests et le registre de décisions.
- Aucun des documents ne certifie que les fonctions de pollinisation sont déjà implémentées.
- Préserver les états/sauvegardes, API existantes, flags, autorisations, Unicode FR/EN et historique Git. Ne pas déployer sans vérifications.


---

## INDEX COMPLEMENTAIRE AGENT CODEUR

**A transmettre ensemble :** tous les fichiers Markdown de ce répertoire. L agent codeur doit commencer par [00-AGENT-CODEUR-LIRE-EN-PREMIER.md](00-AGENT-CODEUR-LIRE-EN-PREMIER.md), puis suivre [40-PROMPT-AGENT-CODEUR.md](40-PROMPT-AGENT-CODEUR.md).

| Document | Objet |
|---|---|
| [00-AGENT-CODEUR-LIRE-EN-PREMIER.md](00-AGENT-CODEUR-LIRE-EN-PREMIER.md) | LIRE EN PREMIER — Mission complète de pollinisation BeeKingdom |
| [21-AUDIT-TECHNIQUE-CODE-CANONIQUE.md](21-AUDIT-TECHNIQUE-CODE-CANONIQUE.md) | 21 — Audit supplémentaire vérifié du code canonique (PCJEFF, main, HEAD afc9c31e) |
| [22-ARCHITECTURE-CIBLE-COMPOSANTS.md](22-ARCHITECTURE-CIBLE-COMPOSANTS.md) | 22 — Architecture cible par domaine / contrats d'interface |
| [23-SCHEMAS-ET-INVARIANTS-PERSISTANTS.md](23-SCHEMAS-ET-INVARIANTS-PERSISTANTS.md) | 23 — Schémas de données, indexes, invariants et migrations |
| [24-CONTRATS-API-JSON-ERREURS.md](24-CONTRATS-API-JSON-ERREURS.md) | 24 — API contract-first : routes candidates, formes JSON et erreurs |
| [25-AUTOMATES-SEQUENCES-ET-CAS-LIMITES.md](25-AUTOMATES-SEQUENCES-ET-CAS-LIMITES.md) | 25 — Automates de vie, séquences et comportement en cas de panne |
| [26-REGLES-MATHEMATIQUES-DE-REFERENCE.md](26-REGLES-MATHEMATIQUES-DE-REFERENCE.md) | 26 — Mathématiques déterministes du système floral |
| [27-CATALOGUES-CONFIGURABLES.md](27-CATALOGUES-CONFIGURABLES.md) | 27 — Catalogues et données initiales (exemples non approuvés) |
| [28-PROGRESSION-JOUEUR-CIVILE.md](28-PROGRESSION-JOUEUR-CIVILE.md) | 28 — Progression civile et intérêt des bâtiments de la ruche |
| [29-NIVEAU-ALLIANCE-ET-COOPERATION.md](29-NIVEAU-ALLIANCE-ET-COOPERATION.md) | 29 — Niveau d'alliance : proposition de système concret |
| [30-UX-PARCOURS-ECRANS-DETAILLES.md](30-UX-PARCOURS-ECRANS-DETAILLES.md) | 30 — Parcours UX complets de la pollinisation |
| [31-PARCOURS-E2E-TROIS-JOUEURS.md](31-PARCOURS-E2E-TROIS-JOUEURS.md) | 31 — Scénarios de référence : un joueur, une alliance, une coalition |
| [32-EVENEMENTS-METIER-ET-LEDGER.md](32-EVENEMENTS-METIER-ET-LEDGER.md) | 32 — Événements métier et journal de comptabilité |
| [33-TESTS-GHERKIN-FONCTIONNELS.md](33-TESTS-GHERKIN-FONCTIONNELS.md) | 33 — Recette comportementale Gherkin pour l'agent codeur |
| [34-OBSERVABILITE-PERFORMANCE-MMO.md](34-OBSERVABILITE-PERFORMANCE-MMO.md) | 34 — Observabilité, performance MMO et exploitation |
| [35-SECURITE-ANTI-TRICHE-ANTI-ABUS.md](35-SECURITE-ANTI-TRICHE-ANTI-ABUS.md) | 35 — Modèle de menace : sécurité, triche, comptes multiples, griefing |
| [36-QUALITE-VISUELLE-LOCALISATION-FR-EN.md](36-QUALITE-VISUELLE-LOCALISATION-FR-EN.md) | 36 — Qualité visuelle et localisation bilingue : exigences d'implémentation |
| [37-DEPLOIEMENT-ROLLBACK-ET-DR-PLAN.md](37-DEPLOIEMENT-ROLLBACK-ET-DR-PLAN.md) | 37 — Déploiement sûr, feature flags, rollback et reprise après panne |
| [38-PROTOCOLE-AGENT-CODEUR-SANS-REGRESSION.md](38-PROTOCOLE-AGENT-CODEUR-SANS-REGRESSION.md) | 38 — Protocole obligatoire de travail pour l'agent codeur |
| [39-PARAMETRES-ALPHA-RECOMMANDES.md](39-PARAMETRES-ALPHA-RECOMMANDES.md) | 39 — Proposition de décisions par défaut pour avancer sans perdre de temps |
| [40-PROMPT-AGENT-CODEUR.md](40-PROMPT-AGENT-CODEUR.md) | 40 — Prompt complet pour l'agent de développement BeeKingdom |
| [41-ADAPTATION-MOTEUR-FLORAL-UNITY.md](41-ADAPTATION-MOTEUR-FLORAL-UNITY.md) | 41 — Adapter FlowerManager et le moteur écologique existant sans le réinventer |
| [42-RELAIS-DIPLOMATIE-INSTALLATIONS-MONDE.md](42-RELAIS-DIPLOMATIE-INSTALLATIONS-MONDE.md) | 42 — Structures d'alliance, relais, balises et traités : design approfondi |
| [43-OURS-EVENEMENT-MONDIAL-RAID.md](43-OURS-EVENEMENT-MONDIAL-RAID.md) | 43 — Tanière de l'ours : cahier des charges du raid mondial |
| [44-MATRICE-LIVRAISON-ARCHITECTE-PROGRAMMEURS-QA.md](44-MATRICE-LIVRAISON-ARCHITECTE-PROGRAMMEURS-QA.md) | 44 — Répartition de tâches possibles entre architecte, programmeurs Unity/API, QA et game design |
| [45-MATRICE-RISQUES-DEPENDANCES.md](45-MATRICE-RISQUES-DEPENDANCES.md) | 45 — Matrice de risques et décisions bloquantes |
| [46-GLOSSAIRE-ONTOLOGIE-METIER.md](46-GLOSSAIRE-ONTOLOGIE-METIER.md) | 46 — Glossaire contrôlé et ontologie BeeKingdom |
| [47-PROTOCOLE-RECETTE-ALPHA-MULTIJOUEUR.md](47-PROTOCOLE-RECETTE-ALPHA-MULTIJOUEUR.md) | 47 — Recette Alpha reproductible à réaliser avant validation |
| [48-JOURNAL-DE-CONCEPTION-MAINTENANCE.md](48-JOURNAL-DE-CONCEPTION-MAINTENANCE.md) | 48 — Comment maintenir cette documentation sans perdre les idées |
| [49-MATRICE-EXIGENCES-CODE-TESTS.md](49-MATRICE-EXIGENCES-CODE-TESTS.md) | 49 — Matrice des exigences : design -> implémentation -> preuve |
| [50-CHECKLIST-P0-POUR-AGENT.md](50-CHECKLIST-P0-POUR-AGENT.md) | 50 — Audit P0 : checklist de prise en charge exécutable |

**Statut :** documentation seulement, sans modification de code de gameplay, sans commit ni déploiement dans cette étape.

**Intégrité des fichiers du dossier :** consulter [51-MANIFEST-INTEGRITE.md](51-MANIFEST-INTEGRITE.md) avant envoi à un agent externe.


## État d'implantation — suivi du 2026-10-21

Le statut documentaire historique du 2026-10-20 n'est plus le statut du code : un **lot technique P1, sous flag OFF**, a été créé dans `feature/pollinisation-alpha-20261021` avec population civile persistante et routes API serveur authentifiées. L'équilibrage reste expérimental, et aucune fonction complète de pollinisation n'est déployée. Voir `52-ADR-ARCHITECTURE-ET-EXECUTION-20261021.md` pour les décisions techniques, tests et limitations.


**Suivi du Lot 2.1 (2026-10-21) :** premières lectures d'états floraux persistants partagés en code serveur sous flag OFF, migration SQL 096 rédigée et routes GET authentifiées. Aucun prélèvement, aucune expédition complète ni rendu Unity n'est encore livré. Détails et limitations dans 52-ADR.

### État réel des lots, mise à jour P3.1 (2026-10-21)

L'implantation en branche `feature/pollinisation-alpha-20261021` comprend maintenant une expédition civile persistante et un règlement SQL atomique ruche + champ + contribution, avec un ordonnanceur de missions dues. **Le test de concurrence et de rollback a été exécuté sur SQL Server LocalDB isolé sur PCJEFF** via `Server/ops/pollination-sql-qa`; aucun déploiement sur SRVESDT. Le mode équilibré et tous ses paramètres restent expérimentaux et sous flag OFF. L'interface Unity, les routes à deux champs et les projets d'alliance ne sont pas encore intégrés. Voir `52-ADR-ARCHITECTURE-ET-EXECUTION-20261021.md`.
## État P4 collectif, 21 octobre 2026

Une première boucle serveur SQL de projets de pollinisation **collectifs** est maintenant implantée sur la branche de travail, sous flag OFF : création sécurisée par officiers/chef, contributions durables à deux membres distincts et niveau d'alliance persistent. La preuve d'anti-solo, de rollback SQL et de changement d'alliance après départ a été exécutée sur SQL LocalDB dédié PCJEFF. **Pas de déploiement SRVESDT ni interface Unity branchée.** Détails : 54-VALIDATION-COLLECTIVE-P4-20261021.md.
## P5 — continuation du 21 octobre 2026

Client Unity et présentation opt-in ajoutés : contrats réseau, contrôleur de
session, écran Champs / Essaims / Alliance en FR/EN, gardes anti-clic-traversant
dans la carte existante, sans modifier les sprites ou les tuiles. 33 tests
du banc .NET PASS et compilation de six fichiers runtime dans Unity minimal PASS.
Le projet Unity complet exige encore un Unity Package Manager fonctionnel.
Les quatre tests Unity Editor ajoutés restent non exécutés.
Rapport : 55-IMPLANTATION-CLIENT-UNITY-P5-20261021.md.
## P6 — ancrages de carte vérifiés (21 octobre 2026)

Le client Wave6 peut maintenant afficher et sélectionner un marqueur de site
floral lorsque l'API renvoie explicitement le bon MapSurfaceId et un MapAnchorId
qui existe déjà dans le monde. Aucune coordonnée arbitraire ni tuile n'a été
ajoutée. La configuration de deux sites servant aux tests est uniquement un
exemple QA sous flags OFF. Voir 56-SITES-FLORAUX-MARQUEURS-CARTE-P6-20261021.md.

## P7 — Centre de pollinisation premium (21 octobre 2026)

Le premier panneau IMGUI P5 a été remplacé pour l'utilisation normale par une fenêtre
uGUI/TMP qui réutilise le Canvas, les dimensions, la safe area, la barre de défilement
et la typographie d'Activités v4. Les trois onglets Champs/Essaims/Alliance restent
pilotés exclusivement par les contrats serveur P1–P4. **P7 vérifié** :
compilation Unity complète sans erreur C#, tests Unity Editor Pollination 8/8,
tests WorldMap 7/7, contrôle client .NET 36/36.
Le rendu PlayMode visuel, les tests sur appareils et la fusion dans main ne sont
pas encore validés. Voir 57-FENETRE-PREMIUM-ACTIVITES-P7-20261021.md.

## P8 — Recette PlayMode (21 octobre 2026)

Deux tests fonctionnels exécutés en PlayMode Unity 6000.5.3f1 sont réussis :
fenêtre royale au-dessus d'un Canvas de carte, clics interceptés, scrollbar
stable, affichages FR/EN, onglets sans mutation, et chargement de la scène
WorldMapMmoFullscreenFoundation avec tous les flags OFF. Le Test Runner
batchmode ne produit pas de captures ; l'approbation visuelle GameView
sur appareil n'est pas encore terminée.
Voir 58-RECETTE-PLAYMODE-P8-20261021.md.
