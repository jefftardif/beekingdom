# 50 — Audit P0 : checklist de prise en charge exécutable

## 0. État initial et protection du travail

Avant d'ouvrir un IDE ou modifier un fichier :
- documenter date/heure, machine, commit, branche ;
- capturer git status --short en **ne supprimant rien** ;
- vérifier worktrees, rebases/merges en cours et fichiers non suivis ;
- ne pas stager/commiter globalement un worktree sale ;
- disposer d'une branche/worktree convenu si plusieurs développeurs se partagent le code.

Exemples de commandes PowerShell **en lecture seule** :

~~~powershell
$root = 'C:\projets\beekingdomgame-master'
git -C $root rev-parse --short HEAD
git -C $root branch --show-current
git -C $root status --short --untracked-files=all
git -C $root worktree list
~~~

## 1. Code Flower et monde

- [ ] Trouver tous les callers de FlowerManager.Harvest/Execute et FlowerPatch.Advance.
- [ ] Inventorier FlowerSpecies/BloomCycle/PollinationRules et états floraux existants.
- [ ] Vérifier RegionalResourceDistributionFramework et identité monde/site.
- [ ] Étudier WorldMapMmoFullscreenFoundationBootstrap pour POI/marqueurs et limites de performance.
- [ ] Consigner l'ancrage et les zones de non-spawn de la tanière actuelle.
- [ ] Valider quels catalogues de fleurs/ressources sont déjà exportés serveur.
- [ ] Choisir mapping stable ID site -> coordonnées client/serveur, après comparaison des scènes canon.

## 2. Économie/ruche

- [ ] Inspecter PlayerHiveState et les migrateurs pour ajout de CivilPopulation.
- [ ] Identifier Repository utilisé en production (SQL/JSON), transaction et concurrence.
- [ ] Inspecter WorldResourceCollectionService avec CommittedTroops ; ne pas confondre civils et militaires.
- [ ] Inspecter NurseryCohortService, NurseryGrowthRules, BroodVitalityCareService ; documenter cohorte active et réserve.
- [ ] Inspecter AcademyTrainingService et les quatre formations ; ne pas modifier leurs effets.
- [ ] Inspecter les contraintes honey/pollen/wax, capacity et RewardLedger.
- [ ] Décider si une nouvelle cohorte civile coûte des ressources et lesquelles **avant activation**.
- [ ] Tester migration d'une ancienne ruche sans ajout gratuit.

## 3. Social/alliance

- [ ] Inspecter AllianceEntity/AllianceMembership/AlliancePermissionPolicy et voies d'auth.
- [ ] Confirmer absence/présence d'un vrai Level d'alliance dans tous les sous-systèmes, ne pas confondre recherches.
- [ ] Inspecter AllianceResearchService/Models et adapter de bonus.
- [ ] Inspecter AllianceDiplomacy, relations et repository durable ; préserver ordre canonique des couples.
- [ ] Concevoir droits d'accès au champ / projet / relais à partir de permissions officielles.
- [ ] Spécifier contribution qualifiée par joueur distinct et politiques de changement de guilde.

## 4. Transaction multicompte — GATE OBLIGATOIRE

- [ ] Décider quel agrégat est l'autorité d'un site floral.
- [ ] Prouver comment deux clients sur le même site ne créent pas des ressources.
- [ ] Choisir transaction unique ou saga/outbox+réconciliation avec états durables.
- [ ] Définir ledger unique, idempotency policy et révisions.
- [ ] Tester crash après start, livraison et settlement.
- [ ] Définir politique de fin automatique/claim pour éviter missions bloquées.
- [ ] Plan de rollback/drain des missions sous flag OFF.

## 5. UX/tests

- [ ] Localisation FR/EN et accents ; ouvrir les deux JSON officiels.
- [ ] Choisir intégration à Activités v4 sans régression d'Alliance et de Nurserie.
- [ ] Préparer deux joueurs réels de test et preuve de partage d'état.
- [ ] Capturer capture de base carte et fenêtres pour comparaison.
- [ ] Exécuter baseline tests existants avant de toucher aux modules.
- [ ] Lister tests unitaires/domaines/Gherkin à créer pour lot.
- [ ] Dresser les risques, incertitudes et décisions ADR nécessaires.

## 6. Produit : sept décisions minimales à confirmer avant activation

1. Définition de l'effectif civil et la filière cohorte.
2. Coût/temps de naissance et entretien d'un essaim.
3. Missions disponibles à l'Alpha (route à 1 ou 2 sites, modes).
4. Récompense honey/pollen et conservation.
5. Définition de niveau de joueur/alliance et effets.
6. Seuils de participants qualifiés et plafonds anti-solo.
7. Politique de perte/attaque/protection des civils et droits du public.

Tout ce qui n'est pas encore décidé doit rester dans une config test/recommandation, jamais annoncé comme un équilibrage final.

## 7. Livrable P0 attendu

Un rapport dans ce dossier ou Docs/QA :
- GIT_HEAD + baseline état ;
- matrice source réellement réutilisable ;
- options architecture et choix ADR ;
- schéma complet des sources de vérité ;
- politique sécurisée des transactions croisées ;
- risques et problèmes bloquants ;
- première proposition de PR P1 (fichiers précis) ;
- tests qui prouvent compatibilité ;
- décision claire : « READY FOR P1 » ou « BLOCKED » avec raison.

## 8. Critère d'un P0 réussi

L'agent n'a pas « développé vite » un faux bouton ; il a identifié le vrai chemin sûr pour créer des civils et partager un champ entre joueurs, sans changer le terrain ni casser l'économie existante.

