# BeeKingdom — Pollinisation : contrôle P18 et correctifs UI du 24 octobre 2026

## État de livraison

- Git branche d'intégration : `integration/pollinisation-unified-20261023`.
- Correctif de synchronisation d'alliance : commit `8379ec55`, basé sur `4998bd6a`.
- Correctif masquage des contrôles développeur : `main` commit `d1f1b735`.
- Correctifs visuels compilés dans Unity 6000.5.3f1 sur PCJEFF. Aucune erreur C# détectée dans le journal de compilation.
- Ne **pas** fusionner aveuglément cette branche dans `main` : `main` comprend des modifications importantes non commitées sur le serveur, les bâtiments, la recherche, la génétique et l'Académie.
- La validation visuelle du correctif avec le véritable joueur connecté reste à effectuer.

## Cause de la fausse absence d'alliance

La fenêtre premium de pollinisation n'exécutait jamais `ConfigureAlliance` car cet appel n'existait que dans l'ancien rendu IMGUI, court-circuité quand le Canvas premium était ouvert. Le correctif lit `MobileAccountSessionRuntimeBootstrap.AllianceCenterControllerForHiveMap` pendant l'ouverture et les mises à jour du panneau, sans inventer d'alliance locale. État `NoAlliance` confirmé requis avant de suggérer une adhésion ; `Loading`/`Error` restent temporaires. L'identité visuelle et la liste des projets sont réactualisées lorsque l'identifiant de l'alliance change.

Le 24 octobre, le journal IIS de l'API production atteste de réponses HTTP 200 pour l'adhésion personnelle, la liste des membres et l'activité de l'alliance. Aucun membre n'a été recréé ni déplacé.

## Nettoyage des messages techniques

Les états préparatoires FR/EN dans la fenêtre premium ont été rédigés pour les joueurs, sans débloquer les actions serveur fermées. L'ancien rendu IMGUI de secours ne s'affiche plus au joueur. Sur la branche `main`, les outils de développement sont masqués par défaut, même dans Unity Editor et dans les Development Builds ; bascule explicitement disponible par `Bee Kingdom / Development / Show Developer Controls`.

Il peut subsister d'autres libellés techniques dans les prototypes hors pollinisation ; ces autres écrans n'ont pas été déclarés intégralement nettoyés.

## Production actuelle SRVESDT

- API `/health` : HTTP 200, IIS BeeKingdomApi démarré.
- `Alliance.Enabled=true`.
- `FloralSites.Enabled=true`.
- `CivilPopulation.Enabled=true`, `ReadOnlyPilot=true`.
- `CivilExpeditions.Enabled=false`.
- `FloralAllianceProjects.Enabled=false`.
- La population civile a retourné HTTP 200 pour des lectures authentifiées dans les logs du 24 octobre. Les expéditions restent HTTP 503 sous garde de fonctionnalité.
- Les DLL API/HiveOperations du lot P18 avaient été publiées avec sauvegarde et contrôle de santé ; le correctif présent est **uniquement Unity/Git**, sans nouvelle publication serveur.

## Preuves exécutées sur PCJEFF

- Tests domaine `Civil|Floral` : **38/38 réussis**.
- Tests API `Civil|Floral` : **20/20 réussis**.
- QA client P5 de pollinisation : `P5_PRESENTATION_QA_PASS` (contrats serveur, idempotence, absence de crédits locaux).
- SQL QA isolée, vraie base `(localdb)\BKPollinationQA`, base `BeeKingdomPollinationQA` :
  - `SQL_QA_PASS` : prélèvement partagé par deux joueurs, non-dépassement du stock, rollback SQL forcé, rejeu, règlement et index de missions dues.
  - `P4_SQL_QA_PASS` : progression collective à deux joueurs, anti-solo, niveau d'alliance crédité une fois, refus de transfert de progrès au changement d'alliance, survie après redémarrage.
  - `SQL_QA_CLEANUP=COMPLETE` et `P4_SQL_QA_CLEANUP=COMPLETE`.
- Journaux locaux des QA : `C:\BeeKingdom\RECOVERY\pollination-*-20261024.log`.

## Portes avant activation des mutations P19

1. Vérifier, par un chemin SQL autorisé et **en lecture seule**, les migrations et l'état des tables de production, puis la sauvegarde/rollback. Le compte distant actuel fonctionne sous SYSTEM et ne dispose pas de l'accès intégré à la base `BeeKingdom` ; ne pas contourner cette barrière.
2. Vérifier le worker SQL de règlement automatique et son fonctionnement quand les nouvelles missions sont fermées.
3. Faire un parcours authentifié Unity : propriétaire membre d'alliance, fenêtre premium sur les trois onglets, puis deux joueurs réels avec ressources communes.
4. Vérifier les rôles des membres, la révision du catalogue, la surveillance des transactions, la cadence des opérations et les garde-fous anti-duplication.
5. Ensuite seulement, ouvrir `CivilExpeditions` et `FloralAllianceProjects` de manière contrôlée, avec retour arrière défini. **Ces deux flags ne sont pas activés dans ce lot.**

État : **P18 lecture autoritaire opérationnelle ; P19 SQL locale prouvée ; activation production conditionnée à la revue SQL/live multijoueur.**

## Vérification authentifiée et tests Unity du 24 octobre

- Avec la session de jeu réelle dans Unity Editor, une interrogation directe **en lecture seule** a confirmé : joueur authentifié, ruche identifiée, service Alliance configuré et état **Ready**, adhésion présente à « Alliance Test ».
- Lors de cette lecture la scène était Environment2D5D_HiveMap_Test et Unity était **en pause** : le modèle de pollinisation était encore en chargement, et son AllianceId non initialisé. Le branchement se fait à l'ouverture de la fenêtre de pollinisation **sur la carte du monde**. Ne pas interpréter l'AllianceId vide de la scène Ruche comme une perte d'adhésion.
- Le créateur a autorisé l'arrêt du mode Play. Le mode Play a été arrêté pour les tests, **sans supprimer ni modifier son adhésion**.
- Unity 6000.5.3f1, tests EditMode exécutés par l'API de tests Unity : **7/7** PollinationPremiumWindowEditorTests réussis (dont l'assertion positive liant l'alliance confirmée au contrôleur), **4/4** PollinationP5EditorTests réussis. Total : **11/11 sans échec**.
- Le garde-fou interdit de traiter Loading ou Error comme une adhésion absente et ne réinitialise l'identifiant d'alliance qu'après un état NoAlliance confirmé par le serveur. Les droits d'officier/chef proviennent exclusivement de la réponse officielle.
- Le parcours visuel réel **Carte du monde → Pollinisation → Alliance** reste à confirmer après connexion et ouverture : les tests EditMode ne remplacent pas une recette visuelle.
- Les outils de test temporaires créés dans Unity ont été retirés ; les résultats de test sont archivés dans C:\projets\beekingdomgame-master\Temp\BKPollinationMcpTest2.results.txt et BKPollinationMcpTestP5.results.txt.
