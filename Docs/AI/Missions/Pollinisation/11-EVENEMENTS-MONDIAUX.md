# 11 — Événements écologiques et tanière de l'ours

## Objectif

La carte doit proposer des occasions régulières, des urgences coopératives, des récompenses et des conflits. Ces événements s'appuient sur les lieux et les états de gameplay, **pas sur une refonte du terrain**.

## Catalogue d'événements proposé

### Grande Floraison
Une région entre temporairement en productivité exceptionnelle. Ressources plus rares, participation accrue et missions liées à plusieurs familles florales. Les alliances convergent, coopèrent ou rivalisent. Bonus limités dans le temps ; pas de ressources infinies.

### Invasion de pucerons / parasites
Menace qui réduit provisoirement certaines ressources ou la fertilité ; les joueurs doivent organiser missions civiles de soin/restauration et protection PvE. Les non-combattants ont de vrais objectifs.

### Éclosion légendaire
Apparition d'une fleur rare dont l'ouverture exige plusieurs participants et une pollinisation compatible. Récompenses rares possibles, dont des composants de Génétique **si le catalogue de cette fonctionnalité les accepte**.

### Conditions saisonnières
Cycles météorologiques/saisonniers peuvent déplacer les opportunités, modifier la régénération, rendre des familles florales prioritaires et stimuler le jeu en différents endroits de la carte.

## Tanière de l'ours — intention initiale APPROUVÉE

**[APPROUVÉ]** La tanière de l'ours est un **événement mondial** auquel plusieurs alliances participent **ensemble contre l'ours**, tout en **rivalisant pour les meilleures récompenses**. Ce n'est ni une chasse solo ni un NPC standard.

**[VÉRIFIÉ]** WorldMapBearDenLandmark.cs existe comme élément visuel du monde ; les propriétés du code consulté indiquent BearVisible=false et ActiveEvent=false. La mécanique d'événement est à implémenter séparément.

### Cycle envisagé (RECOMMANDÉ)

1. Annonce serveur et phase de préparation accessible à toutes les alliances concernées.
2. Réveil de l'ours et perturbations localisées (routes, ressources, présence).
3. Phase de défense/protection et reconnaissance, avec objectifs militaires et civils.
4. Raid collectif : plusieurs alliances contribuent au combat, à la logistique et à la survie de leurs essaims.
5. Résolution du boss, retrait vers la tanière, reprise normale de la carte.
6. Récompenses individuelles, participation collective et classements compétitifs **séparés**.
7. Historique mondial et période de récupération avant prochaine édition.

### Conditions anti-monopole

- Participation minimale accessible aux petites alliances ;
- le classement de dégâts ne doit pas capturer toutes les récompenses ;
- récompenses de coopération distinctes du podium compétitif ;
- pas de blocage permanent de la tanière par un détenteur de relais ;
- règles de contribution, de cumul et de partage interalliances explicites.

### Couplage potentiel à la pollinisation (RECOMMANDÉ)

L'ours peut être attiré par l'abondance de miel d'une région, perturber temporairement des routes ou menacer des relais floraux. À ne pas rendre obligatoire si cela punit mécaniquement les alliances économiquement actives. Préférer événements annoncés, prévisibles et défendables.

## Cadences et équité

Les horaires doivent tenir compte des joueurs de différents fuseaux, permettre contribution asynchrone et éviter de donner systématiquement un avantage à une alliance connectée à une seule heure. Une période de préparation et une fenêtre de participation assez large sont préférables à un déclenchement instantané.

## Périmètre

- **Alpha :** 1 événement simple de floraison au besoin, après la boucle civile validée.
- **Bêta :** saisons, parasites, fleur légendaire, coalition, contrôle territorial.
- **Ours :** développement autonome coordonné avec système raid d'alliances ; son importance est élevée, mais il ne bloque pas la première preuve de pollinisation.

## Tests de bout en bout

Annonce identique à 2 clients, raid multi-alliance, classement correct, pas de double récompense, joueur hors ligne, interruption serveur, leader déconnecté, reprise de la carte après événement, tanière toujours visible entre événements.

