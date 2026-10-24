# P18 - Population civile consultable, formation et missions OFF

Date : 24 octobre 2026.
Objet : phase incrementale entre le catalogue floral en lecture et
l'activation irreversible du cout des cohortes.

## Implementation
- CivilPopulationOptions.ReadOnlyPilot (bool), false par defaut.
- Quand CivilPopulation.Enabled=true et ReadOnlyPilot=true, GET population
  repond avec la vraie ruche existante, sous authentification et autorite.
- GET ne passe pas par ExecuteAtomicallyAsync et ne regle pas de cohorte:
  aucune modification de revision, ressource, stock ou population n'est faite.
- Snapshot.CanStart=false dans ce mode, meme si le joueur a les ressources.
- POST /civil-population/cohorts/start refuse toutes les requetes HTTP 503
  avec code game.civil_population_readonly, avant toute operation.
- CivilPopulationService.StartAsync refuse aussi la commande sous ReadOnlyPilot,
  meme s'il est invoque en dehors de la route HTTP.
- Le chemin Enabled=true, ReadOnlyPilot=false continue a gerer les cohortes
  payantes existantes sans modification de leur regle metier.
- Le partage de lecture du magasin JSON autorise les remplacements atomiques
  de fichiers sous Windows (FileShare.Delete) : la production reste SQL Server.

## Produit
- Le catalogue floral reste public aux utilisateurs authentifies.
- Les effectifs civils peuvent desormais etre projetes dans Essaims sans
  fabrication de credits ni entrainement non approuve.
- Les options par defaut (20 travailleurs, 50 miel, 10 pollen,
  60 minutes) sont provisoires et ne constituent pas une approbation
  produit pour la production.
- CivilExpeditions.Enabled=false et FloralAllianceProjects.Enabled=false.
- Aucune depense, mission ou XP d'alliance ne doit etre activee par P18.

## Gate de promotion
1. Compiler Release et valider unit/API (service 9/9, endpoint 6/6).
2. Executer une regression complete SQL LocalDB et GitHub PR avant promotion.
3. Construire un paquet DLL seul, avec SHA-256, sans aucune configuration ou secret.
4. Sauvegarder les DLL IIS courantes, comparer les empreintes, confirmer
   qu'aucune autre publication ne tourne avant tout remplacement.
5. Activer uniquement CivilPopulation.Enabled=true et ReadOnlyPilot=true.
6. Controler apres redemarrage : health 200, floral 401 sans token,
   civil population 401 sans token, start cohort 503 sans token,
   expedition 503, projets d'alliance 503.
7. En session Unity authentifiee : Essaims affiche les valeurs reelles
   sans qu'un bouton de formation puisse etre lance.
8. En cas d'echec, restaurer DLL et configuration puis redemarrer IIS.

Aucune migration SQL supplementaire requise : 096-098 sont deja appliquees.
Les essais d'expeditions avec deux joueurs necessitent une phase de
certification distincte et une validation de l'equilibrage.