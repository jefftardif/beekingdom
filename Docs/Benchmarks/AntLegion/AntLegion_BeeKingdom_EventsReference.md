# Ant Legion — Événements (référence fonctionnelle pour Bee Kingdom)

Source : `https://ant-legion.fandom.com/wiki/Category:Events` et les 21 pages
d'événements qui y sont listées (18 documentées, 3 stubs sans contenu :
Alliance Showdown, Conquest, Spore Meadow, Bug Flipper, Strike First).

Comme pour `AntLegion_BeeKingdom_FunctionalReference.md`, ceci est une
référence fonctionnelle à transposer, pas un plan à copier. Rappel du cap
produit : collecte manuelle conservée, aucun bonus payant qui rend la
puissance militaire irremplaçable, textes localisables, VIP = confort
seulement.

## Vue d'ensemble du calendrier (Ant Legion)

Les événements réguliers tournent sur un cycle de 2 semaines, avec certains
qui reviennent chaque semaine. Exemple observé :

- **Semaine 1** : Global Ace Ant tous les jours ; le week-end ajoute Food
  Tussle, Bug Flipper, Break Through, Global Stump Contest, Specialized Ant
  Operation, Spore Meadow, Strike First.
- **Semaine 2** : Ant Quiz, Collect Supplies, Honor & Diligence, Nest
  Development, Territory War, Improve Gear, Joy 777, Pollen Battle (2
  phases : inscription puis bataille), Army Expansion, Snail & Cell.

Adaptation Bee Kingdom : un calendrier de 2 semaines aussi, avec un thème
constant (butinage, défense de la ruche, pollinisation) plutôt qu'un decalque
un-pour-un de chaque nom. La cadence (quotidien / hebdomadaire / bihebdomadaire)
est la vraie leçon à retenir, pas les noms.

---

## Catégorie A — Course à points avec paliers de récompense

Le patron le plus commun : une action du jeu normal (collecter, entraîner,
construire, rechercher) rapporte des points ; des paliers de points cumulés
débloquent des récompenses progressives sur la durée de l'événement.

### Army Expansion / Collect Supplies / Nest Development / Personal Activity / Snail & Cell / Honor & Diligence / Improve Gear / Improve Specialized Ant

Tous partagent la même mécanique : une table `Source -> Points` (ex.
entraîner une troupe T5 = 30 pts, obtenir un accélérateur de recherche de 1
min = 15 pts) et une table `Palier de points requis -> Récompense`, sur 7 à
10 paliers, la dernière récompense étant nettement plus généreuse (objet
d'unité spéciale rare, gros lot de diamants).

**Adaptation Bee Kingdom** : un seul gabarit générique "Défi de la semaine"
réutilisable, où la SOURCE change selon le thème :
- *Expansion de la colonie* : points pour entraîner des ouvrières/gardiennes.
- *Récolte* : points pour collecter miel/cire/pollen (branché sur le vrai
  système de production officiel construit aujourd'hui).
- *Chantier* : points pour compléter des niveaux d'amélioration de bâtiment.
- *Recherche* : points pour terminer des recherches (une fois l'arbre de
  recherche généralisé).
- *Quotidien* : points pour les quêtes journalières déjà existantes.

Un seul système serveur générique (catalogue de sources de points + paliers
de récompense, façon `BuildingUpgradeOptions` généralisé aujourd'hui) peut
servir les 6 variantes ci-dessus simplement en changeant sa configuration.

### Break Through

Variante à échelle plus fine : 3 mini-événements de 8h par jour, sur 3
jours, avec la même mécanique de paliers mais des sources orientées
équipement/cellule. Adaptation : version courte du même gabarit générique,
déclenchée par surprise (moins prévisible que le calendrier fixe) pour
garder de la fraîcheur.

---

## Catégorie B — Classements compétitifs

### Ant Quiz

Quiz de 15 questions (30 s chacune), récompense proportionnelle au temps de
réponse et au nombre de bonnes réponses ; classement des 30-50 meilleurs
joueurs avec récompenses de rang. Les alliés peuvent aider pendant le quiz.

**Adaptation Bee Kingdom** : `Quiz de la ruche` — questions sur le lore, les
ressources et les mécaniques du jeu (bon outil d'onboarding déguisé en
événement). Aide des fédérées possible via le chat une fois ce chantier
réactivé.

### Food Tussle

De la nourriture apparaît sur la carte du monde ; les joueurs se battent
pour la collecter, sans perte de troupes. Classement des 200 meilleurs
"dégâts infligés" (en pratique, force de collecte) avec récompenses de rang
généreuses.

**Adaptation Bee Kingdom** : `Ruée vers le nectar` sur la carte du monde
existante — des gisements de nectar temporaires apparaissent, capture sans
perte de troupes réelle (cohérent avec la collecte manuelle, pas de
sanction militaire). S'intègre bien avec le Tableau des signaux de
butinage déjà prévu (cf. référence fonctionnelle principale).

### Global Ace Ant

Événement d'une semaine, activité différente chaque jour (collecte,
construction/recherche, dépense d'endurance, formation, matériaux
d'amélioration, éclosion, puis un jour "Chemin ultime" qui combine tout).
Récompense principale : fragments d'unité spéciale rare.

**Adaptation Bee Kingdom** : `Semaine de la Reine` — chaque jour met en
avant un système différent (récolte lundi, chantier mardi, défense
mercredi, formation jeudi, recherche vendredi, éclosion d'abeille
championne samedi, "Grand jour" dimanche qui cumule tout). Structure
narrative forte, bon véhicule pour enseigner tous les systèmes du jeu sur
une semaine.

---

## Catégorie C — Événements d'alliance / fédération

### Territory War

Le chef ou un officier d'alliance niveau 2+ déclenche l'événement ; des
vagues de prédateurs apparaissent sur le territoire de l'alliance, à
chasser en 25 minutes, sans perte de troupes (défense de nid).

**Adaptation Bee Kingdom** : `Défense du territoire floral`, déclenchée par
la Fédération de ruches (chantier Communication, actuellement gelé). Vagues
de menaces (fausse teigne, frelon) sur le territoire fédéral, chassées sans
perte réelle.

### Lure Trap

Attaques de ralliement contre des pièges à chenilles dans une fenêtre de
temps limitée ; récompenses selon les dégâts totaux infligés par
l'alliance. Réservé aux membres depuis 72h+.

**Adaptation Bee Kingdom** : `Piège à frelons` — ralliement fédéral contre
une menace commune, récompenses selon la participation cumulée. La
condition d'ancienneté (72h) est une protection anti-abus raisonnable à
conserver.

### Pollen Battle

Le plus complexe des événements documentés : compétition alliance contre
alliance sur un champ de bataille dédié, avec ligues (Novice -> Légende),
"affinité" qui monte/descend selon les résultats, points personnels et
d'alliance pour l'occupation de points d'intérêt (fleurs, mares, cirres),
et un système d'endossement où des commandants tiers parient des diamants
sur l'issue.

**Adaptation Bee Kingdom** : c'est littéralement déjà à propos du pollen et
des abeilles dans Ant Legion — adaptation quasi directe en `Bataille du
pollen` entre deux fédérations de ruches, sur une carte dédiée avec des
points d'intérêt (grandes fleurs, mares, buissons). Le système de ligues et
d'affinité est un bon modèle de progression compétitive à long terme. Le
système d'endossement (parier des diamants sur l'issue) est à évaluer avec
prudence — proche d'un mini-pari, à valider avec Jeff avant de l'implanter
tel quel.

### Hunt Assassin Bug

Des insectes assassins apparaissent sur la carte du monde à des niveaux
croissants (comparé à des "nids de prédateurs" niveau 5 à 40) ; les
vaincre rapporte une monnaie d'événement dédiée ("Aile d'insecte assassin")
échangeable dans une boutique à échelle de prix croissante.

**Adaptation Bee Kingdom** : `Chasse au frelon` — menaces de niveau croissant
sur la carte, monnaie dédiée ("Dard de frelon") échangeable dans une
boutique d'événement. Modèle réutilisable pour d'autres menaces thématiques
(coléoptère, fausse teigne).

---

## Catégorie D — Chance / gacha

### Joy 777

Quêtes quotidiennes rapportent des "pièces de joie" dépensées sur une
machine à sous ; chaque tirage augmente un "point de chance" cumulatif
(2%/tirage) qui améliore la probabilité du gros lot.

### Wheel of Fortune

Une roue de la fortune consommant une monnaie dédiée, avec un tirage
gratuit par jour, et des récompenses de palier basées sur le nombre total
de tirages effectués (10, 30, 50, 100...). Le joueur peut personnaliser la
liste des unités spéciales pouvant sortir de la roue.

**Adaptation Bee Kingdom** : ces deux mécaniques de type gacha méritent une
**question de conception explicite avant implémentation** — Bee Kingdom
s'est engagé (cap produit) à ne jamais rendre les achats pay-to-win, et un
système de chance à monnaie premium peut facilement glisser vers ça si la
monnaie s'achète avec de l'argent réel plutôt que de se gagner en jeu.
Recommandation : monnaie d'événement gagnée uniquement par le jeu actif
(quêtes, connexion quotidienne), jamais achetable directement.

---

## Catégorie E — Mini-jeu

### Listless Butterfly

Un match-3 (aligner 3+ tuiles de fleurs de la même couleur) sur 4 jours,
avec niveaux normaux et niveaux "défi" qui coûtent plus d'énergie mais
rapportent davantage. Monnaie d'événement échangeable en boutique dédiée.

**Adaptation Bee Kingdom** : `Danse des fleurs` — même mécanique de match-3,
thème floral déjà cohérent avec l'univers. Bon candidat pour un événement
occasionnel "détente" entre deux cycles de compétition.

---

## Catégorie F — Opération contre boss

### Specialized Ant Operation

5 opérations différentes selon le jour, chacune exigeant un type d'unité
spécifique (mêlée/distance/vélocité/civile) pour affronter un boss. Le
combat peut être répété ; seuls les dégâts maximum en une seule attaque
comptent. Classement final en plus des paliers de dégâts.

**Adaptation Bee Kingdom** : `Opération anti-nuisible` — boss différent
chaque jour, exigeant une composition de caste différente (une fois le
choix de caste Gardiennes/Voltigeuses/Lanceuses implanté), encourageant à
diversifier l'armée plutôt qu'à sur-investir une seule caste.

---

## Stubs sans contenu documenté

`Alliance Showdown`, `Conquest`, `Spore Meadow`, `Bug Flipper`, `Strike
First` n'ont aucun texte sur le wiki. À partir du nom seul (faible
confiance) : Alliance Showdown et Conquest sont probablement des variantes
compétitives d'alliance proches de Territory War/Pollen Battle ; Spore
Meadow sonne comme une zone de collecte thématique (mousse/champignon,
cohérent avec le décor de la ruche déjà en place) ; Bug Flipper et Strike
First sonnent comme des mini-jeux ou événements de vitesse. Aucune
implémentation ne devrait se baser sur ces suppositions sans confirmation.

## Autres catégories du wiki (aperçu, contenu limité)

- `Category:Buildings` ne contient qu'une seule page (Queen) — le wiki est
  très pauvre sur les bâtiments ; la référence fonctionnelle principale
  (`AntLegion_BeeKingdom_FunctionalReference.md`, construite à partir d'une
  vraie session de jeu) reste bien plus riche sur ce sujet.
- `Category:Specialized Ant` (37 membres) liste seulement des noms d'unités
  spéciales (Amber Stinger, Atta Leafcutter, Blue Ant, Carpenter, Death Ant,
  Doorkeeper, Dorylus, Eciton Army Ant, Fire Ant, Flathead, Gardener, Giant
  Ant, Golden Carpenter, Green Ant, Harvester, Jumper, Long Jaw, Longhorn,
  Lumia Ant, Meat Ant, Metallica, Mimicking Jumper, Northern Leafcutter,
  Pavement Ant, Pharaoh Ant, Ponerine, Red Foot, Redhead, Redwood, Rock Ant,
  Slender Ant, Spiderant, Sugar Ant, Velvet Ant, Weaver Ant), sans fiche
  détaillée par unité. Utile uniquement comme inspiration de nommage pour
  les futures abeilles championnes, pas comme référence de stats.

## Traduction des monnaies/matériaux (pour cohérence future)

| Ant Legion | Bee Kingdom (proposition) |
|---|---|
| Nourriture / Feuilles / Eau / Champignon | Miel / Cire / Pollen (déjà en place) |
| Fragment d'unité spéciale | Fragment d'abeille championne |
| Volonté assidue / Marque d'honneur | Éclat de dévouement / Marque de la ruche |
| Ticket de combat de légion | Jeton d'expédition fédérale |
| Accélérateur (construction/recherche/formation/soin) | Élixir de confort (même principe, jamais militaire) |
| Points Escargot (civil/guerre) | Points de prestige (économie/défense) |

## Questions ouvertes pour Jeff

1. Veux-tu un seul gabarit serveur générique "défi à paliers" réutilisable
   pour toute la Catégorie A (recommandé), ou un système distinct par
   thème ?
2. Les mécaniques de type gacha (Joy 777, Wheel of Fortune) doivent-elles
   entrer dans Bee Kingdom du tout, et si oui avec quelle monnaie
   (uniquement gagnée en jeu, jamais achetable) ?
3. Le système d'endossement de Pollen Battle (parier des diamants sur
   l'issue d'un combat entre fédérations) — à garder, adapter, ou écarter ?
4. Priorité relative entre ces événements et le reste de la feuille de
   route (arbre de recherche, chantier Communication, etc.) ?
