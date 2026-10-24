# P20 - Consultation des projets floraux d alliance sans creation

Date : 24 octobre 2026. Statut : candidate locale / PR, PAS EN PRODUCTION.

## Fonctionnement
- FloralAllianceProjects.ReadOnlyPilot est desactive par defaut.
- CanRead = Enabled || ReadOnlyPilot. Le GET exige une
  session authentifiee et l'appartenance a une alliance active.
- CanCreate = Enabled && !ReadOnlyPilot. Le POST retourne HTTP 503 et
  game.floral_alliance_projects_readonly quand le pilote est seul actif.
- CanApplyDeliveries = CanCreate || DrainExistingDeliveries : le GET seul
  ne credite ni XP, ni points, ni missions. Le drainage explicite peut
  seulement finaliser des anciennes expeditions deja en cours.
- Le SQL existant lit les projets deja persistes, sans INSERT ni UPDATE.
- Un environnement sans SQL garde le service Unavailable.
- Le code disabled demeure quand Enabled et ReadOnlyPilot sont OFF.

## Unity
- Le panneau Alliance affiche Consultation seulement / Read-only lorsque
  les expeditions ne sont pas ouvertes.
- Aucun faux bouton orange de creation dans ce mode.
- Les membres et les points affiches proviennent toujours du serveur.

## Validation
- Build serveur Release 0 erreur.
- Tests cibles P20 : 7/7 metier et 4/4 API PASS.
- Tests Unity Pollination : 11/11 PASS en EditMode batch isole.
- Regression SQL LocalDB complete en cours sur PCJEFF.
- Route de projet : GET non authentifie 401; GET membre simule 200;
  POST creation 503 et zero invocation de CreateAsync.
- Aucune migration SQL requise, 096-098 existent deja sur SRVESDT.

## Conditions avant production
1. Terminer la regression complete et CI independante GitHub.
2. Recuperer Valoraxo Remote SRVESDT (hors ligne lors de cette preparation).
3. Sauvegarder les DLL et la configuration IIS, et verifier les empreintes.
4. Mettre uniquement le code certifie, en conservant les autres DLL.
5. Activer FloralAllianceProjects.ReadOnlyPilot=true et garder
   FloralAllianceProjects.Enabled=false, CivilExpeditions.Enabled=false.
6. Health 200, GET non authentifie 401, POST 503, route civils conservee.
7. Tester deux vrais comptes membres, autorites serveur et GameView FR/EN.
8. Aucun credit ni progression collective generee par la simple lecture.

Cette candidate ne forme pas de butineuses et ne lance pas de mission.