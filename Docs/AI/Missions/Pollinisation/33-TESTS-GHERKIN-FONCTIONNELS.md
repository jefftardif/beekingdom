# 33 — Recette comportementale Gherkin pour l'agent codeur

**Ces scénarios sont SPEC, pas tests exécutés.** Chaque Given suppose fixtures de tests authentifiées, horloge simulée déterministe et repositories isolés.

## Feature : essaims civils

~~~gherkin
Feature: Population civile séparée

Scenario: Démarrer une cohorte et éviter la création répétée
  Given une ruche propriétaire avec Nurserie construite et capacité libre
  When le joueur démarre une cohorte avec une clé K et une révision attendue
  And la même commande est réémise avec la même clé K
  Then le serveur conserve une seule cohorte
  And le reçu de la seconde réponse référence la même cohorte
  And aucun soldat militaire n'a été ajouté ni retiré

Scenario: Deux missions concurrentes sur les mêmes ouvrières
  Given une ruche possède 100 civils disponibles
  When deux commandes simultanées demandent chacune 80 civils
  Then au maximum une commande réussit
  And la population civile totale reste conservée

Scenario: Une cohorte ne disparaît pas quand Unity est fermé
  Given une cohorte démarrée
  When l'horloge serveur atteint sa date de maturité sans client connecté
  Then la cohorte devient récupérable selon sa règle officielle
  And une réclamation répétée n'ajoute qu'une fois les civils
~~~

## Feature : floral et routes

~~~gherkin
Feature: Réserves florales partagées

Scenario: Deux joueurs prélèvent une réserve limitée
  Given un site possède 100 unités de pollen non réservé
  When deux récoltes distinctes demandent 80 unités chacune simultanément
  Then le serveur ne retire jamais plus de 100 unités
  And les claims ne reçoivent que le pollen réellement alloué
  And aucun solde ni stock ne devient négatif

Scenario: Une route florale doit être compatible
  Given une source de trèfle et une destination incompatible selon catalogue
  When le joueur demande une expédition de livraison
  Then le serveur refuse avant débit
  And conserve les réserves et effectifs inchangés

Scenario: La durée de la mission dépend du serveur
  Given une mission commencée à T par le serveur
  When le client avance son horloge de deux jours
  Then aucun rendement écologique ni gain anticipé n'est autorisé
~~~

## Feature : mission hors ligne

~~~gherkin
Feature: Vie d'expédition persistante

Scenario: Fermer Unity pendant le trajet
  Given une mission engagée valide
  When le joueur ferme puis relance le jeu après le terme officiel
  Then le statut et le résultat sont reconstitués du serveur
  And la mission n'est pas perdue ni recommencée

Scenario: Réessayer le claim après la perte de réponse
  Given la récompense a été commitée mais la réponse HTTP a échoué
  When le client répète la requête avec la même clé
  Then il reçoit le même reçu de récompense
  And son honey/pollen n'est crédité qu'une seule fois
~~~

## Feature : collaboration

~~~gherkin
Feature: Projet floral collectif

Scenario: Un champion solitaire ne remplace pas douze joueurs
  Given un projet exige douze joueurs distincts ayant contribué utilement
  And un joueur très avancé produit tous les points de projet
  When le serveur tente de fermer le projet
  Then le palier final reste verrouillé faute de membres qualifiés
  And la raison de blocage est visible

Scenario: Le niveau des membres influence le rendement
  Given deux joueurs utilisant le même nombre de butineuses
  And l'un a les compétences florales serveur validées supérieures
  When leurs missions équivalentes sont résolues
  Then l'effet du joueur plus avancé est supérieur dans les plafonds prévus

Scenario: Une alliance inactive n'accomplit rien toute seule
  Given une alliance de cent membres dont aucun ne contribue
  When un projet est ouvert
  Then sa progression ne s'incrémente pas par simple présence des comptes
~~~

## Feature : droits, conflits et ours

~~~gherkin
Feature: Territoires et événements

Scenario: La capture d'un relais ne détruit pas la fertilité
  Given un relais d'alliance et un site fertile proche
  When une alliance ennemie capture le relais pendant une fenêtre légale
  Then le contrôle de relais change
  And la fertilité du site reste cohérente
  And les accès futurs sont réévalués selon le nouveau propriétaire

Scenario: L'événement ours réunit plusieurs alliances
  Given un raid mondial annoncé et plusieurs alliances admissibles
  When leurs contributions militaires et logistiques sont enregistrées
  Then le raid produit des récompenses de participation
  And un classement compétitif distinct
  And chaque résultat et récompense n'est réglé qu'une fois
~~~

## Feature : compatibilité UI

~~~gherkin
Feature: Carte et écrans premium

Scenario: Langue et accents
  Given le joueur ouvre la fiche florale en français
  When il passe l'interface en anglais puis revient en français
  Then le statut, le scroll et la sélection restent identiques
  And tous les accents sont corrects
  And aucun bouton ne clignote au rafraîchissement

Scenario: Contenu préservé
  Given la carte canonique actuelle
  When le module floral est désactivé par configuration
  Then le paysage, les tuiles et la collecte traditionnelle restent inchangés
~~~

## Preuves attendues

Pour chaque scénario implémenté : nom du test automatisé, commande d'exécution, résultat, JSON ou état avant/après, log si panne, captures UI quand visuel. Jamais considérer ce Markdown comme un résultat de tests.

