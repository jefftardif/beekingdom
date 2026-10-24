# 42 — Structures d'alliance, relais, balises et traités : design approfondi

**Statut : futur Lot P5/P6, recommandations et interfaces d'extension.** Le principe collectif/hybride est approuvé, la forme exacte des infrastructures reste à arbitrer.

## Principes

- Monde public toujours jouable sans alliance.
- Les alliances renforcent leur activité par **installations sur carte**, pas par refonte des 14 bâtiments de ruche.
- Contrôle et droit d'accès ≠ fertilité écologique.
- Capture d'une position n'efface pas les efforts floraux passés.
- Les frontières territoriales doivent être lisibles, mais rester une couche d'interface.
- Une grande alliance doit assumer une logistique et un coût d'entretien.

## Infrastructure candidate minimale : Relais floral

Fonctions possibles :
1. Dépôt régional (stockage transitoire), sans créer de monnaie.
2. Coordination de missions de plusieurs membres sur sites à portée.
3. Avantage de **réseau de routes** et vitesse de ravitaillement, plafonné.
4. Point d'information : sites, menaces, missions et floraison.
5. Site stratégique attaquable selon diplomatie/règles de guerre.
6. Point de projet collectif : construction, amélioration, entretien, réparation.

Propriété : allianceId + revision + outpostId + position stable + niveau + état + timers serveur. Missions civiles peuvent référencer un relais sans en devenir propriétaires.

## Éventuels étages d'infrastructure

**Ruche-Mère d'alliance** : centre politique de l'alliance si compatible avec le futur système territoire ; pas automatiquement un nouveau bâtiment dans la ruche personnelle.

**Balises de phéromones** : matérialisent la continuité territoriale et servent d'indices d'influence, non pas des bonus infinis.

**Avant-postes** : position militaire/stratégique pour défendre réseau de relais.

Éviter la création de 3 infrastructures à la fois : première preuve avec Relais floral, puis harmoniser la hiérarchie avec le système de territoire global.

## Droit d'accès

Définir AccessPolicy au moment de chaque commande :
- Public : tout joueur autorisé à butiner avec protections régionales.
- Member : tout membre actif de l'alliance propriétaire.
- Partner : alliance signataire d'un pacte actif couvrant cette activité/site.
- Hostile : accès non autorisé sans conquête/mécanique PvP.
- Disputed : règles de danger et de combat explicites, public pas automatiquement supprimé.

Les règles d'un champ public ne peuvent être converties en propriété privée par simple présence d'un relais voisin sans contrat de conquête approuvé.

## Gouvernance

Les permissions sur projet/construction/attaque/traité suivent AlliancePermissionPolicy. Officier/chef peuvent posséder des capacités différentes ; rôles serveur, jamais simple UI.

Changement de chef et dissolution ne doit pas perdre l'historique des relais. Capture crée événement et journal d'ancien/nouveau propriétaire, permissions invalidées et durée de transition.

## Conflit territorial

Une attaque de relais n'est pas un combat libre contre chaque butineuse. Elle exige :
- fenêtre de vulnérabilité prévue ;
- état de relation/autorisation du serveur ;
- déplacement/réservation militaire officiel ;
- calcul d'issue et changement de propriétaire atomique ;
- recalcul droits d'accès avec politique de retour sûr des missions déjà en vol ;
- intégrité du site écologique et des contributions passées.

## Pactes d'accès et traités

La diplomatie existe déjà dans AllianceDiplomacy.cs ; Pacte de Floraison doit s'inscrire comme **clauses économiques spécialisées** ou objet lié à une relation. Une relation « Ally » ne confère pas automatiquement accès total aux fleurs si l'alliance ne l'a pas négocié.

Clauses : sites, installations, types de missions, durée, escorte, quotas, récompenses, résiliation ; évitez les contrats indéfinis difficiles à mettre fin.

## Interactions de game design

- Alliance économique protège/prête ses routes contre gardes militaires d'une autre.
- Grande alliance contrôle un relais stratégique mais dépend des compétences civiles de petites alliées.
- Alliance rivale prend une position pour accéder à des champs productifs, sans créer ou détruire les fleurs.
- Des accords commerciaux/ecologiques permettent la prospérité et les trahisons, avec coût politique lisible.

## Équité

Nombre de relais limité par progression réelle et coût d'entretien, possibilité de coopération, zones protégées, règles anti-harcèlement, système de revendication qui n'empêche pas un novice de jouer.

## Tests futurs

Construction concurrente sur même position, changement de chef, alliance dissoute, pacte expiré, capture simultanée, civil en vol pendant capture, revente/reprise de relais, absence de changement de fertilité sur capture.

