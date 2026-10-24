# P18 - Population civile en lecture seule sur SRVESDT

Date : 24 octobre 2026.

## Livraison realisee
- Code : commit ae17ee38 (CI construit depuis GitHub Actions run 40068505101).
- Artefact API compilé depuis le depot GitHub, deux bibliotheques et deux PDB,
  SHA256SUMS.txt verifie sur SRVESDT. Aucun secret ou appsettings de prod inclus.
- Deployment controle avec mutex et controle des anciennes empreintes;
  sauvegarde des bibliotheques IIS et de la configuration active.
- Production backup:
  C:\BeeKingdom\deploy-backups\P18CivilReadOnly-20261024-020926
- Deployment log:
  C:\BeeKingdom\staging\pollination-P18-binary-20261024\deploy-P18-readonly.log
- Gate RESULT=DEPLOY_PASS, HEALTH HTTP 200, app pool Started.
- Aucune migration SQL appliquee a cette etape.

## Etat du serveur en production
- FloralSites.Enabled = true, deux sites publics authentifies.
- CivilPopulation.Enabled = true, CivilPopulation.ReadOnlyPilot = true.
- CivilPopulation GET : HTTP 401 sans token, retourne les donnees de la
  ruche existante avec un compte officiel valide (sans mutation SQL).
- CivilPopulation POST /cohorts/start : HTTP 503,
  game.civil_population_readonly, aucun cout ni recrutement possible.
- CivilExpeditions.Enabled = false : HTTP 503.
- FloralAllianceProjects.Enabled = false : HTTP 503.
- Les couts de cohorte restent des valeurs provisoires NON approuvees.

## Preuves automatiques
- Build Release : 0 erreur.
- Unitaires CivilPopulation : 9/9, API civil : 6/6.
- Suite complete sur PCJEFF : HiveOperations 329/329 PASS,
  API avec SQL LocalDB 724/724 PASS, aucun test ignore.
- Le magasin JSON, non utilise par la prod SQL, a ete durci contre
  les conflits File.Move sur Windows via FileShare.Delete.
- GitHub PR CI : gate independant axe sur la pollinisation;
  les campagnes totales restent certifiees sur PCJEFF.

## Blocage de validation utilisateur
Lors de la premiere requete authentifiee post-deploiement depuis Unity,
la session client etait expiree (auth.session_expired). Ne pas afficher
cette condition comme preuve d'un probleme SQL. La prochaine session
officielle doit relancer Refresh pour verifier la lecture reelle
Population.Capacity et AvailableWorkers, sans aucun POST.

## Suite
1. Connecter un compte valide dans Unity, afficher Essaims et les valeurs
   civiles sans activer leur formation.
2. Regler et approuver les couts, duree, capacite et gain par cohorte.
3. Effectuer les tests a deux joueurs avec une sauvegarde recente.
4. Ouvrir les expéditions seulement apres validation SQL, idempotence,
   retours hors connexion, economie et progression d'alliance.
5. Si defaillance : restaurer deux DLL, PDB et
   appsettings.Production.json depuis le backup P18 ci-dessus,
   redemarrer BeeKingdomApi et tester health/civil=503.