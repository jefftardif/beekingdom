# Ant Legion -> Bee Kingdom - Reference fonctionnelle

## Statut du relevé

- Date du relevé: 2026-07-19
- Environnement observé: nouveau compte Ant Legion, version française, BlueStacks
- Progression couverte: introduction et chapitres 1 à 6 terminés; chapitre 7 et chaîne principale associée en cours
- Objectif: relever les fonctionnalités, parcours, règles et écrans servant de base à Bee Kingdom
- Principe: reprendre la profondeur et la lisibilité fonctionnelles, avec une identité, des textes et des visuels propres aux abeilles

Ce document est enrichi au fil de l'exploration. Les nombres relevés décrivent le tutoriel observé et ne constituent pas encore l'équilibrage final de Bee Kingdom.

Contrainte Bee Kingdom: la carte 50 x 50 et les images qui la composent sont intouchables pour l'instant. Les adaptations concernent les systèmes, l'interface, les icônes de ressources et les contenus ajoutés autour de cette carte.

## Correspondances de l'univers

| Ant Legion | Bee Kingdom |
|---|---|
| Fourmilière | Ruche vivante |
| Reine fourmi | Reine des abeilles |
| Ouvrières | Ouvrières |
| Porteuses / Fourmis Pot-de-miel | Magasinières / Porteuses de nectar |
| Soldates de corps à corps | Gardiennes lourdes |
| Soldates véloces | Éclaireuses rapides |
| Tireuses | Lanceuses / gardiennes à distance |
| Nourriture | Miel et pain d'abeille selon l'usage |
| Feuilles | Pollen, cire ou propolis selon le système |
| Abri | Réserve protégée |
| Bassin de guérison | Alvéole médicale à la gelée royale |
| Entrée | Trou d'envol fortifié |
| Couveuse | Nurserie / couvain |
| Fourmis spécialisées | Abeilles héroïques / spécialisées |
| Corps à corps / vélocité / distance | Gardiennes / Voltigeuses / Lanceuses |
| Arène | Arène des Ailes / Épreuves de vol |
| Hall de l'Alliance | Pavillon de la Fédération des ruches |
| Phéromones | Danse des éclaireuses / Rapports de vol |
| Prédateur extérieur | Prédateur ou parasite de la ruche |

## Structure générale observée

### Vue de colonie

- La colonie est une scène verticale continue mêlant surface et chambres souterraines.
- Les bâtiments sont des éléments organiques intégrés au décor, avec un libellé et un badge de niveau discrets.
- La scène n'est jamais figée: des fourmis circulent dans les chambres, les unités produites se déplacent et de petits effets environnementaux maintiennent une impression d'activité.
- De nombreux écrans dédiés possèdent une bannière supérieure animée liée à leur fonction; le mouvement participe à l'identité du système, pas seulement à la décoration.
- La caméra se déplace automatiquement vers le prochain objectif pendant le tutoriel.
- Les zones non accessibles restent visuellement présentes, ce qui donne un objectif d'expansion.
- Les actions terminées produisent une bannière centrale temporaire, un gain de puissance et parfois une évolution visuelle du bâtiment.

### Interface persistante

- Barre supérieure: ressources principales, monnaie premium, VIP, puissance et promotions.
- Colonne gauche: file de construction et états rapides.
- Bas d'écran: accès aux abeilles/fourmis spécialisées, quêtes, sac, messages, alliance et menu supplémentaire.
- Bandeau de conversation immédiatement au-dessus de la navigation.
- Les promotions existent dans la vue principale, mais le tutoriel assombrit ou neutralise les commandes non pertinentes.

### Guidance du tutoriel

- Une seule cible dominante est active à la fois.
- La cible reçoit un halo vert pulsé et une grande flèche jaune.
- Le reste de l'écran est souvent assombri et non interactif.
- De courts dialogues contextuels expliquent la raison de l'action avant d'indiquer la commande.
- La caméra et la guidance reprennent automatiquement après une construction, un entraînement ou une récompense.
- Les chapitres sont des parcours d'objectifs: chaque objectif donne une récompense, puis une récompense de chapitre est réclamée séparément.

Adaptation Bee Kingdom: le tutoriel doit être scénarisé et interactif dès la première ouverture. La ruche doit rester vivante grâce à des ouvrières, nourrices, gardiennes et butineuses animées. Les bannières de menus peuvent montrer des boucles courtes propres au bâtiment: nourrissage du couvain, ventilation, stockage de nectar, façonnage de cire ou danse d'orientation.

### Récolte et production

- Les ressources produites ne sont pas versées automatiquement au stock général.
- Le joueur revient sur le bâtiment de production et clique dessus pour effectuer la collecte.
- Cette action crée une petite boucle de retour, rend la production visible et expose régulièrement les bâtiments au joueur.
- La capacité locale du bâtiment doit plafonner la production en attente afin que le rythme de visite reste significatif.

Adaptation Bee Kingdom: la récolte manuelle demeure la règle gratuite. Un service premium de `Butineuses intendantes` peut collecter automatiquement les productions arrivées à maturité. Il s'agit d'un confort, non d'un multiplicateur: même rendement, mêmes capacités locales et mêmes limites de stockage que la récolte manuelle. Ce service peut être temporaire, renouvelable ou inclus dans un abonnement de ruche.

### Protection des stocks et cadence des bâtiments

- L'`Abri` protège une quantité déterminée de chaque ressource contre les pertes. Au niveau 1, les limites observées sont de 300 000 nourritures, 300 000 feuilles, 60 000 eaux et 15 000 champignons; le miellat affiché séparément est de 14 810.
- Chaque niveau observé de l'Abri ajoute 50 000 nourritures, 50 000 feuilles, 10 000 eaux et 2 500 champignons protégés. Le niveau 3 protège ainsi 400 000, 400 000, 80 000 et 20 000 unités respectivement.
- Le passage de l'Abri du niveau 1 au niveau 2 coûte 1 310 feuilles, dure 3 secondes et ajoute +270 de puissance. Le niveau 2 vers 3 coûte 1 978 feuilles, dure 1 minute et ajoute +662 de puissance. Le niveau 3 vers 4 exige Reine niveau 4, coûte 3 148 feuilles, dure 1 minute et ajoute +1 304 de puissance.
- Le Dépôt de nourriture niveau 1 produit 120 unités par heure et en stocke 1 200. Son passage au niveau 2 exige Reine niveau 3, coûte 95 feuilles, dure 1 minute et ajoute +19 de puissance, +120 de production horaire et +1 200 de capacité.
- Le Dépôt niveau 2 produit 240 unités par heure et stocke 2 400 unités. Le niveau 2 vers 3 exige Reine niveau 5, coûte 143 feuilles, dure 1 minute et ajoute +45 de puissance, +126 de production horaire et +1 260 de capacité.
- Le niveau 3 produit 366 unités par heure et stocke 3 660 unités. Son passage au niveau 4 exige Reine niveau 5, coûte 228 feuilles, dure 5 minutes 28 secondes et ajoute +82 de puissance, +132 de production horaire et +1 320 de capacité.
- Une série guidée demande successivement Abri niveau 3, Dépôt de nourriture niveau 4, Abri niveau 4, puis deux Dépôts de nourriture niveau 4. Les récompenses intermédiaires remboursent une partie importante des feuilles dépensées.
- Chaque construction propose l'aide d'alliance, une file de construction principale, une seconde file inactive, puis un écran d'accélération séparant diamants, temps gratuit et objets de 1, 5 ou 15 minutes. Les accélérateurs gagnés en jouant peuvent terminer une durée sans achat.

Adaptation Bee Kingdom: une `Réserve scellée` protège nectar, pollen, cire et gelée royale. Ses seuils doivent être lisibles avant une attaque. Le tutoriel peut enseigner le lien entre production et protection en deux ou trois étapes variées, sans imposer plusieurs améliorations quasi identiques. Ces valeurs servent uniquement de repères de cadence; les coûts, durées et rendements de Bee Kingdom seront calibrés avec son économie propre.

### Ligne de monétisation Bee Kingdom

- Bee Kingdom est conçu comme un jeu commercial avec une boutique et des offres désirables, sans devenir pay-to-win.
- Les achats admissibles couvrent la personnalisation, le confort, les files supplémentaires, l'automatisation plafonnée, les accélérations, les passes d'événement et des ressources accessibles aussi par le jeu.
- Aucun achat ne doit fournir une unité militaire exclusive, un bonus de combat permanent impossible à rattraper ou une puissance sans plafond.
- Une proposition d'achat peut apparaître à un moment de friction, mais la voie gratuite, son coût en temps et la valeur de l'offre doivent rester explicites.
- Les offres du tutoriel seront relevées sans transaction: moment d'apparition, prix, contenu, durée, rareté, segmentation, récurrence et lien avec l'action précédente.
- Les achats doivent susciter l'envie par la valeur, l'esthétique et le gain de temps, jamais par la confusion ou la peur de rendre le compte non compétitif.

## Écrans fonctionnels

### Construction et amélioration

L'ouverture d'un bâtiment mène à un écran dédié plutôt qu'à un petit panneau superposé. L'écran contient:

- nom, illustration et description du bâtiment;
- niveau actuel et prochain niveau;
- durée initiale et durée réelle;
- prérequis de bâtiments ou de niveau de reine;
- matériaux requis avec quantité possédée / quantité nécessaire;
- puissance gagnée;
- effets chiffrés et fonctionnalités débloquées;
- action normale temporisée en vert;
- action immédiate en orange contre monnaie premium.

Lorsque la condition n'est pas satisfaite, elle apparaît en rouge avec une commande `C'est parti` qui conduit directement vers le prérequis ou la quête nécessaire.

### Entraînement militaire

- Grande illustration haute définition de l'unité.
- Sélecteur de rôle sous forme d'icônes.
- Paliers T1, T2, T3 visibles, les paliers verrouillés restant exposés.
- Curseur de quantité avec boutons moins et plus.
- Coûts détaillés par ressource.
- Bouton d'entraînement temporisé et bouton d'entraînement immédiat premium.
- À la fin, un marqueur circulaire apparaît au-dessus du baraquement; le joueur clique pour récupérer les unités.
- La récupération augmente immédiatement la puissance et affiche une bannière de confirmation.
- Au niveau 1, le baraquement de corps à corps permet d'entraîner 70 unités R1 en 17 minutes pour 4 200 nourritures; 643 unités étaient déjà possédées lors du relevé. Le baraquement des unités véloces niveau 3 propose le même lot et la même durée pour 3 710 nourritures, avec 620 unités possédées.
- Le niveau 4 d'un baraquement débloque son unité R2. Une flèche de promotion apparaît alors près des unités R1 existantes, ce qui indique un chemin de conversion des troupes déjà formées plutôt qu'un abandon de l'investissement précédent.
- Le premier retour dans un baraquement après ce déblocage affiche un écran plein format `Débloquer de nouvelles unités`, avec modèle animé, nom du rang et puissance. Ce moment est valorisant, mais il arrive au prochain retour plutôt qu'à la fin du chantier.
- Pour un lot de 70 unités R2, la durée observée est toujours de 26 min 04 s, mais la combinaison de ressources varie par rôle: corps à corps, 4 760 nourritures et 1 470 feuilles; véloces, 5 250 nourritures et 1 190 feuilles; porteuses, 3 850 nourritures et 2 380 feuilles.
- Les Porteuses sont une caste logistique inspirée des fourmis pot-de-miel, décrites comme des garde-manger vivants utilisant la trophallaxie. Leur R1 coûte 2 100 nourritures pour 70 unités en 17 minutes. Elles ne constituent pas une quatrième classe dans le triangle corps à corps, vélocité et distance.
- Adaptation Bee Kingdom: des `Magasinières` ou `Porteuses de nectar` civiles peuvent augmenter la charge, le transport, le ravitaillement ou la quantité protégée au retour. Elles doivent rester séparées des classes militaires et de l'option premium d'auto-collecte.

### Soins

- Le premier combat guidé blesse une partie des unités.
- Le tutoriel fait immédiatement construire le bâtiment de soins.
- L'écran liste les unités blessées par type et rang.
- Un curseur choisit le nombre à soigner.
- La capacité totale de soins est affichée en en-tête.
- Les soins consomment des ressources, prennent du temps et offrent une option immédiate premium.
- Un second onglet, `Caverne royale`, est visible mais pas encore exploré.

### File de construction

- Fenêtre compacte ouverte depuis le raccourci gauche.
- Affiche le bâtiment, le type d'opération, le temps restant et une barre de progression.
- Bouton `Vitesse` ouvrant la liste des accélérateurs.
- Deuxième file visible mais verrouillée/inactive, ce qui annonce une future amélioration ou un avantage.

### Accélérateurs

- Écran spécialisé par type de file, ici `Construction`.
- Temps restant conservé en haut.
- Options: terminer avec diamants, accélération gratuite, objets de 1 ou 5 minutes spécifiques, objets universels.
- Boutons `Utiliser` et `Utiliser en continu`.
- Les objets possédés et manquants restent tous visibles.
- Le tutoriel offre et fait utiliser un accélérateur de 5 minutes pour la Reine niveau 3.

### Événement urgent: Tempête tropicale

- L'événement est introduit par une image plein écran, un avertissement rouge et une courte narration.
- Un compte à rebours d'environ une heure commence immédiatement.
- Les objectifs réutilisent la progression normale: améliorer l'Entrée au niveau 4 et construire/améliorer un dépôt de nourriture au niveau 3.
- Les récompenses sont montrées avant l'action.
- Un bandeau permanent `La tempête est imminente` apparaît ensuite dans la vue de colonie.
- La progression d'événement est guidée comme le tutoriel principal et peut être bloquée par les prérequis de chapitre.
- La commande `C'est parti` ouvre directement le bâtiment concerné, sans demander au joueur de le retrouver dans la colonie.
- Le guidage de l'événement peut entrer en concurrence avec celui du chapitre principal; Bee Kingdom devra conserver une seule priorité de guidage active à l'écran.

### Menace bloquant l'expansion

- Un prédateur présent dans une future chambre empêche entièrement les ouvrières de poursuivre la construction.
- Sa fiche affiche une description, le butin, la puissance ou taille d'armée exigée et une commande d'attaque.
- L'écran d'expédition montre la composition des trois familles de soldats et avertit lorsque l'armée ne possède aucune unité spécialisée.
- Le premier mille-pattes exige une armée de 100 unités; sa défaite lève immédiatement le blocage de construction.
- Adaptation Bee Kingdom: frelon, sphinx tête-de-mort, fausse teigne, araignée ou autre menace installée dans une zone de ruche. La menace doit être visible dans le décor et reliée explicitement à l'action qu'elle bloque.

### Entrée fortifiée

- L'Entrée possède des points de structure affichés (`8 100 / 8 100` au niveau observé) et une action distincte de réparation.
- Un écran `Infos Garde` annonce une garnison, ici `0 / 5` emplacements occupés.
- L'amélioration du niveau 2 au niveau 3 dure 4 secondes, coûte 3 086 feuilles et donne +1 030 de puissance ainsi que +100 DEF.
- Les prérequis sont Reine niveau 3 et Couveuse niveau 1; chaque prérequis manquant propose un raccourci direct.
- Une frise d'apparence montre certains jalons visuels du bâtiment, notamment les niveaux 1, 2 et 4.
- Adaptation Bee Kingdom: trou d'envol fortifié, gardiennes assignées, intégrité de propolis/cire, réparation et évolution visuelle du seuil.

### Couveuse spécialisée

- La Couveuse dispose d'un emplacement de construction propre et d'une introduction narrative.
- Elle sert à faire éclore les œufs rares en unités spécialisées.
- Sa première construction dure 1 seconde, exige Reine niveau 1 et coûte 68 unités de nourriture.
- Elle apporte +10 de puissance et déclenche une fiche de découverte avec illustration, rôle et confirmation.
- Adaptation Bee Kingdom: nurserie royale où des œufs rares deviennent des abeilles héroïques ou spécialisées. Le bâtiment doit être distinct du couvain ou de la nurserie servant à la population ordinaire.

### Réserves et capacité

- Le menu de construction indique le nombre possédé sur la limite, par exemple `1 / 6`, puis `2 / 6` pour le Dépôt de nourriture.
- Un dépôt niveau 1 dure 3 secondes, coûte 68 feuilles et exige Reine niveau 1.
- Effets observés: +10 de puissance, +120 nourriture par heure et +1 200 de capacité de nourriture.
- Adaptation Bee Kingdom: cellules de réserve de miel, pollen ou pain d'abeille. Production, capacité, protection et limite de bâtiments doivent rester des statistiques séparées.

### Reine et capacité globale

- La fiche de Reine la décrit comme centre de construction et des marches, et indique qu'elle déverrouille de nouveaux bâtiments.
- Niveau 3 observé: taille d'armée 100 et capacité de soins 34 000.
- Niveau 4 observé: taille d'armée 100 et capacité de soins 36 000.
- L'amélioration vers le niveau 4 dure 10 minutes, exige Entrée niveau 3, coûte 6 995 feuilles et donne +5 798 de puissance ainsi que +2 000 de capacité de soins.
- La file montre un premier emplacement actif et un second emplacement encore verrouillé.
- Le tutoriel fournit un accélérateur de construction de 10 minutes et oblige à l'utiliser.
- Adaptation Bee Kingdom: le niveau de la Reine représente la maturité de la ruche et gouverne sa capacité de construction, de patrouille, de défense et de soins.

### Formation et accélération

- La formation de 20 soldates de corps à corps R1 coûte 1 200 nourriture et dure 5 minutes.
- L'inventaire d'accélération distingue les objets de formation des accélérateurs universels.
- Quantités observées pendant le tutoriel: 20 accélérateurs de formation de 1 minute, 7 de 5 minutes et 33 accélérateurs universels de 1 minute.
- Un accélérateur de formation de 5 minutes offert est utilisé; la collecte des 20 unités ajoute +80 de puissance.
- Les objets manquants peuvent afficher une option d'achat premium. Cette commande ne doit jamais être confondue visuellement avec l'utilisation d'un objet possédé.

### Unités spécialisées et progression individuelle

- L'agrandissement de la sixième zone offre une deuxième Fourmi spécialisée, `Fourmi Oecophylla`, avec une animation d'obtention et +18 000 de puissance.
- Chaque Fourmi spécialisée de niveau 1 ajoute ici +30 à la taille de l'armée; la formation comporte cinq emplacements de héros.
- Le catalogue sépare les unités obtenues et non obtenues, avec des filtres par rôle: corps à corps, distance, vélocité et civil.
- La fiche individuelle possède au moins deux onglets, `Améliorer` et `Cultiver`, ainsi qu'une prévisualisation du potentiel maximal.
- La Fourmi Bleue passe du niveau 1 au niveau 10 avec une ressource d'expérience dédiée. Chaque niveau observé ajoute +30 de taille d'armée et environ +1 050 de puissance.
- Le niveau 10 constitue un premier plafond. Une percée consomme un objet de rang et une ressource secondaire, relève le plafond à 30 et débloque la poursuite de la progression.
- La première percée fait passer la Fourmi Bleue au niveau 11, ajoute +11 550 de puissance et porte son bonus de taille d'armée à +330.
- La percée de Sinensis confirme la même structure: 30 objets `Percée R1` et 200 unités d'expérience font passer directement du niveau 10 au niveau 11, relèvent le plafond de 10 à 30 et portent la puissance de 27 450 à 39 000.
- Pour les Fourmis militaires observées, la taille d'armée suit une règle très lisible: 100 unités de base, puis +30 par niveau de chaque héroïne placée. La formation Bleue 15, Oecophylla 12 et Pharaon 12 atteint ainsi 1 270 unités.
- Les unités civiles remplacent le bonus de taille d'armée par un avantage économique. Sinensis donne notamment du temps d'accélération gratuite et des dégâts de tour de défense.
- Les compétences de légion sont distribuées sur de grands jalons, notamment les niveaux 31 et 130, afin de soutenir une progression de long terme.
- Le catalogue propose les filtres `Tous`, `Corps à corps`, `À distance`, `Vélocité` et `Civil`. Chaque carte expose le rôle, le niveau, l'état obtenu ou non obtenu et les alertes de progression disponibles.
- La Fourmi Oecophylla passe du niveau 11 au niveau 12 pour 1 220 points d'expérience. Ce niveau ajoute +1 050 de puissance et fait passer son bonus de taille maximale d'armée de +330 à +360; la capacité d'escouade guidée monte ainsi de 1 090 à 1 120.
- La Fourmi Sinensis apparaît avant son obtention avec une jauge de 3 fragments sur 10, ce qui rend visible à la fois l'objectif de collection et la distance restante.
- Adaptation Bee Kingdom: abeilles héroïques ou lignées spécialisées réparties entre gardienne, butineuse à distance, éclaireuse rapide et caste civile. La ressource de percée peut devenir `Gelée royale R1` ou `Sceau d'élevage R1`.

### Choix de caste du joueur dans Bee Kingdom

- Ant Legion ne demande pas de choisir explicitement une classe. La spécialisation du compte émerge des recherches, compétences, bâtiments et Fourmis spécialisées que le joueur privilégie entre corps à corps, vélocité et distance.
- Bee Kingdom demandera au contraire un choix explicite de caste principale: `Gardiennes` pour le corps à corps, `Voltigeuses` pour la vitesse et `Lanceuses` pour la distance.
- La matrice de contre d'Ant Legion sert de base d'équilibrage, sans obligation de la reproduire intégralement:

| Caste Bee Kingdom | Forte contre | Faible contre |
|---|---|---|
| Gardiennes / corps à corps | Lanceuses / distance | Voltigeuses / vitesse |
| Voltigeuses / vitesse | Gardiennes / corps à corps | Lanceuses / distance |
| Lanceuses / distance | Voltigeuses / vitesse | Gardiennes / corps à corps |

- Bee Kingdom doit conserver une boucle de contres lisible afin qu'aucune caste ne domine toutes les autres. Les coefficients exacts, exceptions de compétences et synergies hybrides seront propres au jeu et validés par simulation puis par tests. L'interface affiche l'avantage avant un combat, dans la préparation d'escouade et sur les fiches de caste. Le contre apporte un avantage significatif, mais ne garantit jamais à lui seul la victoire: puissance, rang, compétences, composition et synergies restent déterminants.
- Le tutoriel doit d'abord faire essayer les trois familles, montrer une courte simulation de leur style et résumer leurs forces, limites et synergies avant de verrouiller le choix principal.
- La caste principale détermine l'arbre de maîtrise prioritaire, une capacité de commandement, les recommandations d'escouade, certains objectifs et l'identité visuelle du profil. Les unités des deux autres familles restent utilisables; elles ne reçoivent simplement pas tous les bonus de maîtrise principale.
- Vers le niveau 100, un parcours de prestige permet de débloquer une seconde caste. Ce jalon doit être gagné en jouant et ne peut pas être acheté directement; une offre peut accélérer des étapes raisonnables sans supprimer l'accomplissement requis.
- La seconde caste ouvre des formations hybrides et peut couvrir la faiblesse naturelle de la caste principale. Elle relance l'expérimentation de fin de progression, sans remplacer la première ni rendre obsolètes les joueurs qui restent spécialisés dans une seule caste.
- Le choix initial doit être engageant sans devenir un piège: aperçu complet avant confirmation, avertissement clair, et au moins une voie de réorientation rare mais accessible par le jeu.

### Carte extérieure, recherche et chasse

- La commande en bas à gauche alterne entre la colonie et le monde extérieur.
- La carte affiche les coordonnées, les colonies voisines sous bouclier, les prédateurs et les nœuds de ressources.
- Un panneau de recherche permet de choisir une catégorie (`Prédateur`, `Nid de Prédateur`, `Nourriture`, `Feuille`) puis un niveau au moyen de boutons et d'un curseur.
- Une recherche de niveau 1 a trouvé un Scarabée Géant aux coordonnées `(420, 626)`, à quatre pieds de distance.
- La fiche de cible affiche l'armée recommandée, le coût de 10 points d'endurance, les récompenses de première victoire et la commande d'attaque.
- La formation choisit les héros, remplit les soldats disponibles, affiche le temps de marche et permet l'envoi en expédition.
- Le combat extérieur se résout automatiquement après la marche; le joueur revient sur la carte et reçoit un rapport détaillé.
- Le rapport observé contient la date, la cible, ses coordonnées, le butin, la puissance perdue, les blessés, les combattants, les survivants et les récompenses de première victoire.
- Une chasse de niveau 5 recommande 1 080 Soldates R1. Une victoire observée avec 1 270 combattantes et aucune blessée donne 1 500 unités d'expérience spécialisée, plusieurs paquets de ressources et trois accélérateurs de 5 minutes de familles différentes.
- La commande `Attaquer 3x` consomme 30 points d'endurance, lance une seule marche de six secondes et produit trois rapports et trois butins séparés. L'endurance observée passe de 78/120 à 48/120.
- La messagerie distingue `Note Système`, `Déclaration d'État`, `Email de l'Alliance`, `Rapport de Bataille`, `Rapport de Chasse`, `Nid de Prédateur`, `Commande de Recharge` et `Autres Rapports`.
- La commande `+` de l'expérience spécialisée indique ses sources: Prédateurs, Arène, Phéromones, packs cadeaux et Nids de Prédateur après la Reine niveau 8. Les achats sont donc présentés comme une source parmi plusieurs, pas comme le seul chemin.
- Les quatre raccourcis compacts placés près du changement de vue sont visuellement ambigus. Dans l'étape guidée observée, la loupe et le radar ont tous deux recentré la caméra sur la colonie au lieu d'ouvrir clairement la recherche attendue.
- Adaptation Bee Kingdom: territoire autour de la ruche avec fleurs, eau, bois, pollen et nectar; menaces telles que fausse teigne, petit coléoptère des ruches, guêpe ou frelon; rapports de patrouille avec trajet, endurance et survie des escouades.

### Laboratoire et arbre de recherche

- La victoire contre l'araignée veuve rouge qui bloque la zone 7 révèle et construit automatiquement un Laboratoire de niveau 1. Le bâtiment ressemble à une masse poreuse rouge et le dialogue le présente comme une culture de bactéries souterraines améliorant la viabilité de la colonie.
- L'écran principal répartit les technologies entre `Développement` (145), `Économie` (179), `Combat` (353) et `DEF` (166). Des familles tardives restent visibles mais verrouillées: Tactiques d'unités au Laboratoire 16, Formation au niveau 28, Combat II au niveau 30, Événement au niveau 31, puis Combat III et Développement II au niveau 36.
- Les premières recommandations sont `Construction I` et `Emplacement de Marche I`. L'arbre Combat part de l'emplacement de marche, puis se ramifie vers PV, ATQ et DEF du corps à corps, vitesse de marche et bonus des différentes familles.
- `Emplacement de Marche I` exige Laboratoire niveau 1, coûte 1 410 feuilles et 3 270 nourritures, dure 10 minutes, ajoute +323 de puissance et ouvre un emplacement de marche supplémentaire.
- La recherche utilise une file distincte de la construction, peut demander l'aide de l'alliance et consomme des accélérateurs de recherche dédiés. Deux accélérateurs de 5 minutes ont terminé cette première technologie.
- Les améliorations du Laboratoire observées sont: niveau 1 vers 2 en 1 min 02 s, Reine niveau 4, 873 nourritures et 1 456 feuilles, +480 de puissance; niveau 2 vers 3 en 2 min 24 s, Reine niveau 4, 1 309 nourritures et 2 198 feuilles, +1 177 de puissance.
- Le bonus de Sinensis retranche jusqu'à deux minutes au lancement. Il termine donc immédiatement le premier palier et laisse environ 24 secondes au second, avec une animation `-00:02:00` au-dessus du bâtiment.
- Adaptation Bee Kingdom: `Laboratoire des alvéoles`, alimenté par levures, propolis et microbiome de la ruche. Les catégories doivent rester peu nombreuses au départ, afficher les branches futures et expliquer l'effet concret de chaque technologie avant sa durée ou son coût.

### Accélération automatique

- Une commande `Accélération rapide` sélectionne automatiquement la combinaison d'objets possédés la plus utile pour couvrir le temps restant.
- La première utilisation explique le comportement et demande une confirmation avant de consommer les objets.
- Pour une amélioration de 20 minutes, le système a combiné un accélérateur de construction de 5 minutes avec des accélérateurs de 1 minute.
- Bee Kingdom doit conserver cette fonction, montrer exactement les objets qui seront consommés et privilégier les accélérateurs spécifiques avant les accélérateurs universels.

### Utilisation automatique des paquets de ressources

- Lorsqu'une amélioration manque d'une ressource, la commande `+` ouvre directement l'inventaire correspondant sans proposer d'abord un achat.
- `Utilisation Auto` calcule la combinaison de paquets possédés nécessaire et affiche le détail avant confirmation. Pour les 37 981 feuilles manquantes de la Reine niveau 7, le système a proposé trois paquets de 10 000 et huit paquets de 1 000, soit 38 000 feuilles.
- Un réglage séparé permet d'autoriser ou non l'emploi automatique des boîtes de ressources génériques.
- Les options premium d'achat restent visibles plus bas, mais sont distinctes des objets déjà possédés.
- Bee Kingdom doit reprendre ce confort en minimisant le surplus consommé, en affichant le reliquat attendu et en séparant strictement `Utiliser mes réserves` de `Acheter`.

### Arène et défis quotidiens

- L'Arène se construit en 1 seconde au niveau de Reine 5, coûte 68 unités de nourriture et ajoute +10 de puissance.
- Elle présente plusieurs modes avant même leur déblocage: `Reine · Guerre de Légion` au niveau 5, `Duel spécialisé` au niveau 7 et `Reine · Combat de Légion` au niveau 15.
- Un magasin d'échange complète les modes de combat.
- La formation comporte cinq emplacements héroïques et une répartition ajustable entre les trois familles de soldats.
- Le joueur dispose de dix tentatives, d'un classement, d'une défense, d'un journal, d'une liste d'adversaires actualisable et d'une option pour passer l'animation.
- Les cinq premiers combats observés ont été gagnés. Chaque victoire a donné 125 insignes de Guerre de Légion.
- Une piste quotidienne récompense le nombre de combats aux seuils 1, 2, 4, 6, 8, 10 et 12 avec insignes, ressource d'expérience héroïque et accélérateurs.
- Adaptation Bee Kingdom: `Arène des Ailes`, défis de formation entre escouades de championnes. Les tentatives, la défense et les paliers quotidiens restent lisibles, mais les combats doivent évoquer interception aérienne, escorte et défense de trajectoire plutôt qu'une guerre de fourmis reskinnée.

### Aventure scénarisée

- L'amélioration de la Reine au niveau 7 débloque un accès `Aventure` dans la colonie. Un dialogue plein écran explique que le monde extérieur contient dangers et opportunités, puis une flèche animée conduit au nouveau mode.
- Le chargement possède sa propre animation: une fourmi progresse sur une feuille en gros plan. Le parcours devient ensuite un sentier illustré de niveaux numérotés.
- Une jauge de 10 points d'énergie encadre les tentatives. Une série commencée a consommé un point, puis a permis d'enchaîner immédiatement les niveaux suivants tant que l'équipe gagnait.
- Chaque niveau montre d'abord la formation ennemie, sa puissance totale, les récompenses de première complétion et une récompense quotidienne.
- La préparation comporte cinq emplacements héroïques, une sélection manuelle et une commande de formation recommandée. Trois Fourmis spécialisées de niveau 11 totalisaient 117 000 de puissance contre 15 000 au premier niveau.
- Le combat est automatique, animé en duel successif, avec portraits, ordre des combattants, barres de vie, dégâts et une commande de passage rapide.
- Les niveaux 1 à 4 ont été gagnés en continu. Les récompenses observées comprennent une ressource d'aventure, quatre accélérateurs de 5 minutes dont le type varie selon le niveau, dix paquets de nourriture de 1 000 et dix paquets de feuilles de 1 000.
- Le niveau 5 crée un premier mur volontaire: deux ennemis violets de niveau 3 et 80 000 de puissance chacun ont vaincu l'équipe de 117 000. L'écran de défaite recommande explicitement d'améliorer la puissance des unités héroïques.
- Adaptation Bee Kingdom: `Expéditions des Championnes`, un parcours de clairières, vergers, ruchers abandonnés et zones à prédateurs. Les combats opposent des escouades d'abeilles à des menaces crédibles, avec une mise en scène aérienne et des objectifs d'escorte, de reconnaissance ou de défense, pas un simple duel de fourmis remplacées par des abeilles.

### Fédération de ruches

- Le Hall de l'Alliance se construit en 3 secondes au niveau de Reine 5 et coûte 728 feuilles.
- Son premier niveau donne +105 de puissance, +2 aides d'alliance, +3 000 de renforts et +30 d'effet d'aide.
- Sa vue principale sert à envoyer, recevoir et rappeler des renforts. La capacité affichée passe de 3 000 au niveau 1 à 6 000 au niveau 2, puis à 9 000 au niveau 3.
- Le niveau 1 vers 2 dure 1 min 03 s, exige Reine niveau 5, coûte 1 019 feuilles et ajoute +210 de puissance, +3 000 renforts et +3 d'effet d'aide. Le niveau 2 vers 3 dure 1 min 47 s, coûte 1 538 feuilles et ajoute +515 de puissance avec les mêmes incréments fonctionnels.
- Le tutoriel impose de créer ou rejoindre une alliance et accorde 300 diamants au premier ralliement.
- La fiche de prévisualisation expose le chef, la langue, le nombre de membres, la puissance et le niveau de cadeau.
- Les modules visibles sont: territoire, cadeaux, guerre, technologie, trésorerie, classement, événements, membres, candidatures et journal. Des raccourcis mènent au magasin, au courrier, à l'aide et aux options.
- Le joueur peut demander une aide de construction; une confirmation explicite indique que la demande a été envoyée.
- Adaptation Bee Kingdom: `Fédération de ruches`. Le pavillon fédéral gère l'entraide, les renforts, la recherche collective, le territoire floral, les expéditions communes et les cadeaux. La communication reste reliée au serveur réel de Bee Kingdom et ne doit pas dépendre d'un simulacre local.

### Reine niveau 6 et marchand

- Le niveau 5 affiche une taille d'armée de 100 et une capacité de soins de 38 000.
- Le niveau 6 conserve la taille d'armée de 100, porte la capacité de soins à 40 000 et débloque le Marchand.
- L'amélioration dure 40 minutes et exige un Baraquement des Soldates véloces niveau 3, un Hall de l'Alliance niveau 1, 11 347 nourriture et 21 383 feuilles.
- Le gain est de +14 786 de puissance et +2 000 de capacité de soins.
- La montée de niveau observée a porté la puissance totale du compte à 148 147.
- Les récompenses de jalon comprennent un bouclier de 8 heures, 30 objets de percée R2, dix accélérateurs de recherche de 5 minutes, cinq objets de 10 points VIP et de l'expérience de commandement.
- Le niveau 7 est déjà prévisualisé: taille d'armée 100, capacité de soins 42 000 et aucune nouvelle fonctionnalité annoncée.
- Adaptation Bee Kingdom: au niveau 6, la Chambre royale débloque un `Comptoir des butineuses` ou un `Marchand itinérant`, et augmente la capacité de l'Alvéole médicale. Les prérequis croisent défense, entraide et logistique.

### Menace météorologique à préparation longue

- Après la Reine niveau 6, une nouvelle `Tempête tropicale - En approche` démarre avec environ 24 heures de préparation.
- Le bandeau reste visible dans la colonie avec un compte à rebours.
- Les objectifs sont: Entrée niveau 7, construction de la Grotte d'évolution et 30 Quêtes de phéromones.
- Les récompenses annoncées comprennent dix fragments héroïques violets, un accélérateur de 8 heures, dix paquets de nourriture de 1 000 et dix paquets de feuilles de 1 000.
- Cette version longue de la tempête sert de cadence quotidienne et agrège plusieurs systèmes nouvellement débloqués.
- Adaptation Bee Kingdom: un front météo saisonnier, une vague de froid ou un vent chargé de pesticides. La préparation peut demander de renforcer le trou d'envol, développer une alvéole d'adaptation et compléter des rapports d'éclaireuses. L'événement doit modifier l'ambiance de la ruche sans toucher à la carte 50 x 50 ni à ses images.

### Phéromones et missions régionales

- Le système se débloque après la Reine niveau 6 et place des événements sur une carte régionale avec une actualisation d'environ 3 h 30.
- Le niveau 1 exige une quête de phéromones complétée et une Fourmi spécialisée possédée.
- La première mission, `Éliminez la Menace`, conduit directement à un Scarabée Géant proche. La fiche donne coordonnées, distance, puissance recommandée et récompenses.
- La formation automatique a réparti 790 soldats entre trois familles et trois unités spécialisées, puis lancé une marche de 12 secondes contre une cible recommandant 900 de puissance.
- Après la victoire, la récompense doit être réclamée sur la carte des phéromones. Un message confirme que tous les événements disponibles sont terminés.
- Le passage au niveau 2 est immédiat et sans coût. Le prochain palier exige quatre quêtes complétées et trois unités spécialisées possédées.
- Adaptation Bee Kingdom: les abeilles n'utilisent pas cette mécanique de communication comme les fourmis. Le système devient `Danse des éclaireuses` ou `Carte des butineuses`: des éclaireuses reviennent à la ruche, dansent pour indiquer direction, distance et valeur d'une cible, puis génèrent des missions régionales périodiques. Le niveau du réseau de danse dépend du nombre de rapports complétés et des championnes éclaireuses possédées.

## Déroulé du tutoriel

### Introduction

1. La Reine échappe à un prédateur.
2. Le joueur creuse à plusieurs reprises jusqu'à environ 30 cm.
3. Une première chambre est créée.

### Chapitre 1 - Première génération d'Ouvrières

1. Incuber les premières larves d'ouvrières.
2. Creuser une nouvelle chambre.
3. Découvrir une source de nourriture de type cloporte/isopode.
4. Récolter la nourriture.
5. Améliorer la Reine au niveau 2.
6. Construire l'Abri qui protège les ressources.
7. Réclamer les objectifs puis la récompense du chapitre.

### Chapitre 2 - Éliminez les envahisseurs

1. Agrandir la fourmilière jusqu'à une troisième zone.
2. Construire le baraquement de corps à corps.
3. Entraîner 20 unités de corps à corps R1.
4. Construire le baraquement des unités véloces.
5. Entraîner 20 unités véloces R1.
6. Construire le baraquement des Tireuses.
7. Entraîner 20 Tireuses R1.
8. Déclencher un premier affrontement largement automatique.
9. Construire un Bassin de guérison après que 3 unités ont été blessées.
10. Soigner les unités en 3 secondes.
11. Améliorer la Reine au niveau 3; durée affichée de 5 minutes.
12. Utiliser un accélérateur offert de 5 minutes.
13. Réclamer les objectifs puis la récompense de chapitre.

Récompenses observées pour le passage au niveau 3 de la Reine: accélérateurs, paquets de ressources et unités. La montée augmente aussi la puissance et la capacité du Bassin de guérison.

### Chapitre 3 - Expansion prudente

1. Améliorer l'Entrée au niveau 2.
2. Étendre la colonie jusqu'à la chambre 4.
3. Éliminer un mille-pattes qui bloque physiquement la construction; l'expédition exige 100 soldats.
4. Construire trois Dépôts de nourriture au total.
5. Construire la Couveuse, qui débloque les unités spécialisées.
6. Construire deux Bassins de guérison au total. Le second dure 3 secondes, coûte 146 feuilles et ajoute +21 de puissance ainsi que +3 000 de capacité de soins.
7. Améliorer l'Entrée au niveau 3 pour satisfaire le prérequis de la Reine.
8. Améliorer la Reine au niveau 4 avec un accélérateur de construction de 10 minutes offert.
9. Former 20 soldates de corps à corps R1 avec un accélérateur de formation de 5 minutes offert.
10. Réclamer les sept objectifs puis la récompense de chapitre.

Objectifs affichés dans le chapitre: Entrée niveau 2, chambre 4, trois Dépôts de nourriture, Couveuse, deux Bassins de guérison, Reine niveau 4 et 20 soldates de corps à corps R1 ou plus.

Récompense finale observée: 50 diamants, cinq paquets de nourriture, 20 accélérateurs universels de 1 minute, 40 accélérateurs de construction de 1 minute, 20 accélérateurs de formation de 1 minute et cinq accélérateurs de formation de 5 minutes.

### Chapitre 4 - Préparation pour l'aventure

1. Construire un Baraquement des Soldates.
2. Élargir la colonie jusqu'à la zone 6.
3. Recruter gratuitement la Fourmi Oecophylla obtenue dans cette zone.
4. Éliminer trois scarabées qui verrouillent successivement la chambre des nurseries.
5. Atteindre les seuils d'armée de 160, 280 puis 400 pour ces trois combats intérieurs.
6. Construire deux Nurseries dans la chambre libérée.
7. Chasser un Prédateur de niveau 1 sur la carte extérieure.
8. Améliorer une Fourmi spécialisée jusqu'au niveau 11, percée comprise.
9. Améliorer la Reine au niveau 5.
10. Construire la Fortification et réclamer la récompense finale du chapitre.

Les sept objectifs du chapitre sont: Baraquement des Soldates, zone 6, deux Nurseries, Fortification, Prédateur niveau 1, Fourmi spécialisée niveau 11 et Reine niveau 5.

La première Nurserie exige Reine niveau 2, coûte 208 feuilles et dure 5 secondes. Elle apporte +30 de puissance, +5 unités par formation et +0,5 % de vitesse d'entraînement. La limite affichée est de cinq Nurseries.

La Reine niveau 5 exige une Nurserie niveau 1, coûte 11 892 feuilles et dure 20 minutes. Elle donne +9 750 de puissance et +2 000 de capacité de soins, qui passe de 36 000 à 38 000; la taille d'armée de base reste 100.

La récompense de montée de la Reine niveau 5 comprend 100 diamants, des boosts de production de 24 heures et plusieurs paquets de nourriture et de feuilles.

La récompense finale annoncée pour le chapitre comprend cinq paquets de ressources, 20 accélérateurs universels de 1 minute, 40 accélérateurs de construction de 1 minute et trois lots de 50 éléments liés aux Fourmis spécialisées.

Adaptation Bee Kingdom: une chambre de couvain infestée doit être nettoyée par paliers avant que les nourrices puissent s'y installer. Les seuils de force enseignent la progression des abeilles héroïques sans employer les espèces, noms ou illustrations d'Ant Legion.

### Chapitre 5 - Guerre & Croissance

Le chapitre fait passer le joueur de la survie locale aux systèmes sociaux, compétitifs et mondiaux. Ses huit objectifs sont:

1. Obtenir la Fourmi Pharaon.
2. Améliorer la Reine au niveau 6.
3. Construire une Arène.
4. Construire un Hall de l'Alliance.
5. Effectuer deux Duels Légion dans l'Arène.
6. Posséder deux Fourmis spécialisées de niveau 11.
7. Chasser un Prédateur de niveau 2.
8. Chasser un Prédateur de niveau 3.

Chaque objectif donne 5 000 unités de nourriture. La récompense finale observée comprend 50 éléments héroïques violets, 20 accélérateurs universels de 1 minute, 40 accélérateurs de construction de 1 minute, 20 accélérateurs de formation de 1 minute, 10 paquets de ressource blanc/vert et 5 paquets de nourriture.

La Fourmi Pharaon est obtenue après cinq Guerres légionnaires dans l'Arène. Son obtention ajoute +18 000 de puissance et augmente la taille maximale d'armée. Son potentiel maximal annonce +5 400 de taille d'armée et +12,5 % à l'ATQ, la DEF et les PV de l'armée.

Adaptation Bee Kingdom: ce chapitre devient l'ouverture de la ruche au réseau de ruches voisines. Une Abeille championne est gagnée après cinq Épreuves de vol; elle apporte un bonus de capacité d'escouade et des bonus globaux mesurés. La progression doit introduire compétition, fédération et chasse sans perdre la cohérence de la ruche.

### Chapitre 6 - Cherchez des Champions

- L'écran d'introduction plein format porte le titre `Cherchez des Champions`.
- Le chapitre commence immédiatement après la fermeture complète du chapitre 5.
- L'intention fonctionnelle est de faire des unités héroïques le prochain axe majeur de progression.
- Le chapitre contient neuf objectifs: totaliser 1 800 soldates, améliorer trois Fourmis spécialisées au niveau 11, améliorer deux Nurseries au niveau 3, agrandir la Fourmilière jusqu'à la zone 7, compléter trois Phéromones, rechercher `Emplacement de Marche`, obtenir et activer la Fourmi Sinensis, obtenir 30 points de quête quotidienne et améliorer la Reine au niveau 7.
- Chaque objectif identifié donne 5 000 unités de nourriture.
- La récompense de chapitre annoncée comprend 100 éléments héroïques violets, 20 accélérateurs universels de 1 minute, 20 accélérateurs de construction de 1 minute, 20 accélérateurs de formation de 1 minute, 15 paquets de ressource et un lot de nourriture à confirmer.
- Deux Nurseries niveau 3 donnent ensemble un seuil d'entraînement de 10 et un volume global de 30 unités. Chaque niveau ajoute 5 places de formation et 0,5 % de vitesse.
- Le passage d'une Nurserie du niveau 2 au niveau 3 dure 1 minute, exige Reine niveau 3, coûte 439 feuilles et ajoute +147 de puissance.
- L'agrandissement de la zone 7 révèle un prédateur qui bloque le chantier du laboratoire. Le combat exige une armée de 1 100 unités alors que l'escouade guidée atteint d'abord 1 090; cette micro-dépendance force une progression complémentaire avant la recherche.
- L'amélioration de la Fourmi Oecophylla au niveau 12 porte l'armée à 1 120 et permet de vaincre ce prédateur. La victoire donne notamment deux accélérateurs de 5 minutes et révèle immédiatement le Laboratoire niveau 1.
- La recherche `Emplacement de Marche I` termine un sixième objectif du chapitre. Les trois objectifs restants sont alors: compléter deux Phéromones supplémentaires, obtenir 30 points de quête quotidienne, puis obtenir et activer Sinensis.
- L'objectif Sinensis ouvre une fiche dédiée. Son potentiel maximal annonce +14,95 minutes de temps d'accélération gratuite, +15 % au seuil du bassin de guérison et +10 % à la vitesse de formation; sa condition d'obtention est de vaincre un Prédateur de niveau 5.
- La Reine niveau 7 exige Entrée niveau 6 et Baraquement des Tireuses niveau 6. Son amélioration dure 2 h 46 min 40 s, coûte 30 582 nourritures et 40 628 feuilles, ajoute +20 900 de puissance et +2 000 de capacité de soins. L'achèvement immédiat est affiché à 334 diamants.
- L'achèvement de la Reine niveau 7 ouvre immédiatement le mode Aventure et déclenche un écran de jalon animé. Les récompenses visibles comprennent dix accélérateurs de guérison de 30 minutes, cinq paquets de feuilles de 1 000, cinq paquets de nourriture de 1 000 et cinq objets d'expérience de commandement de 100, plus un élément partiellement masqué à confirmer.
- L'Entrée passe du niveau 4 au niveau 5 en 10 minutes pour 8 347 feuilles, puis au niveau 6 en 25 minutes pour 15 009 feuilles. Ces paliers ajoutent respectivement +3 412 et +5 175 de puissance ainsi que +100 de défense chacun.
- Adaptation Bee Kingdom: `À la recherche des Championnes`, centré sur les lignées rares, les rôles de caste et la constitution d'une escouade de cinq abeilles héroïques.

### Chapitre 7 - Stock de Nourriture

- Le chapitre s'ouvre sur l'approche de l'hiver et transforme la croissance économique en préparation à une menace saisonnière.
- Huit objectifs sont annoncés. Les objectifs confirmés comprennent: effectuer un défi dans l'Aventure, améliorer un Tas de Feuilles au niveau 6, attaquer quatre Prédateurs de niveau 5, améliorer la Reine au niveau 8, améliorer quatre Fourmis spécialisées au niveau 11 et atteindre 2 000 Soldates. Un objectif lié au classement des oeufs de légion et un dernier objectif restent à préciser.
- Chaque objectif réclamé jusqu'ici donne 7 000 unités de nourriture. Trois objectifs sur huit sont terminés.
- Le Tas de Feuilles passe successivement de 180 à 1 170 unités produites par heure et de 1 800 à 11 700 unités de stockage entre les niveaux 1 et 6.
- Ses durées observées sont 17 secondes, 1 minute, 5 min 29 s, 8 min 23 s et 12 min 20 s. Les coûts alternent entre feuilles et nourriture selon le palier; les gains de puissance et de production augmentent progressivement.
- L'aide d'alliance a retranché environ 1 min 40 s à chacune des améliorations longues observées.
- La chaîne principale parallèle au chapitre demande ensuite deux Bassins de guérison niveau 4. Pour le second bassin, les passages 1 vers 2, 2 vers 3 et 3 vers 4 durent respectivement 50 s, 1 min et 1 min 51 s, coûtent 204, 308 et 491 feuilles, ajoutent +42, +103 et +202 de puissance, et augmentent chacun la capacité de soins de 500. Le niveau 4 vers 5 monte déjà à 6 min 59 s, 834 feuilles et +341 de puissance.
- La chaîne militaire fait ensuite monter le baraquement de corps à corps du niveau 1 au niveau 4. Les paliers observés coûtent 873 feuilles en 10 s, 1 319 feuilles en 1 min 06 s, puis 1 259 nourritures et 2 098 feuilles en 3 min 07 s. Ils ajoutent +288, +706 et +1 391 de puissance; le dernier palier débloque les soldates de corps à corps R2.
- Le Baraquement des Porteuses est ensuite construit puis porté au niveau 4. Sa construction dure 3 secondes et coûte 624 feuilles pour +144 de puissance. Les améliorations coûtent 873 feuilles en 10 s, 1 319 feuilles en 1 min 06 s, puis 1 259 nourritures et 2 098 feuilles en 3 min 07 s; elles ajoutent +288, +706 et +1 391 de puissance et débloquent les Porteuses R2.
- L'Abri niveau 4 vers 5 dure 13 min 21 s, exige Reine niveau 5, coûte 5 351 feuilles et ajoute +2 193 de puissance. Chaque niveau ajoute 50 000 de protection de nourriture, 50 000 de feuilles, 10 000 d'eau et 2 500 de champignons; au niveau 4, les seuils sont 450 000, 450 000, 90 000 et 22 500.
- Le second Bassin de guérison niveau 4 vers 5 confirme 6 min 59 s, 834 feuilles, +341 de puissance et +500 de capacité de soins.
- Les baraquements de corps à corps et de véloces niveau 4 vers 5 partagent les mêmes paramètres: 7 min 55 s, 2 139 nourritures, 3 567 feuilles et +2 340 de puissance. Le baraquement des Tireuses était déjà niveau 6 et son objectif niveau 5 a été validé automatiquement.
- La Nurserie devient ensuite un prérequis militaire explicite. Son passage du niveau 3 au niveau 4 dure 3 min 07 s, exige Reine niveau 4, coûte 699 feuilles et donne +289 de puissance, +5 unités par formation et +0,5 % de vitesse d'entraînement. Le niveau 4 vers 5 dure 8 min 48 s, exige Reine niveau 5, coûte 1 189 feuilles et donne +487 de puissance avec les mêmes +5 unités et +0,5 %. Le niveau 5 vers 6 dure 24 min 22 s, exige Reine niveau 6, coûte 2 138 feuilles et donne +739 de puissance, +10 unités et +0,5 %.
- Les baraquements de corps à corps et de véloces niveau 5 vers 6 ont encore des valeurs identiques: 20 min 50 s, 3 848 nourritures, 6 415 feuilles et +3 548 de puissance. Le premier exige Nurserie niveau 6; le second exige Abri niveau 6. Le raccourci d'un prérequis manquant ouvre directement le bâtiment concerné, puis la quête reprend automatiquement après son amélioration.
- Après ces deux chantiers, l'objectif Tireuses niveau 6 est validé automatiquement, suivi de la même validation pour Entrée niveau 6 et Reine niveau 7. La séquence mène enfin à la construction du Terrain de Rassemblement, interrompue par un scorpion obligatoire.
- La chaîne valide aussi automatiquement les objectifs déjà dépassés, notamment Reine niveau 6, construction du Laboratoire et Fortification. Ce rattrapage évite de bloquer un joueur ayant progressé dans un ordre différent, mais plusieurs récompenses successives devraient être regroupées pour rester compréhensibles.
- La commande `Utiliser en continu` des accélérateurs choisit automatiquement le plus grand nombre d'objets d'une minute sans dépasser le temps restant: cinq objets pour 5 min 21 s, laissant 21 secondes. Cette prévention du gaspillage est excellente et doit être conservée.
- La chaîne suivante fait monter le Laboratoire aux niveaux 2 puis 3 et le Hall de l'Alliance aux niveaux 2 puis 3 avant de demander un Bassin de guérison niveau 6.
- Grâce au bonus civil de Sinensis, les 3 min 07 s du dernier chantier sont ramenées immédiatement à environ 1 min 06 s. La demande d'aide d'alliance reste une action distincte et affiche une confirmation d'envoi explicite.
- Une quête de croissance accordant 216 points d'expérience de commandement a fait passer le Commandant du niveau 3 au niveau 4, avec deux points de talent et +4 780 de puissance. Cette progression de compte s'ajoute aux niveaux de la Reine, des bâtiments, des unités et des héroïnes.
- L'écran d'accélération regroupe l'achèvement immédiat en diamants, l'obtention gratuite, les accélérateurs de construction et les accélérateurs universels. Cette densité rend la dépense premium trop facile à confondre avec l'emploi d'un objet possédé.
- La commande `Attaquer 3x` contre un Prédateur de niveau 5 consomme 30 points d'endurance, lance une seule marche et résout trois combats successifs. Les trois occurrences comptent pour les objectifs.
- La montée de la Reine du niveau 7 au niveau 8 dure 5 h 33 min 20 s et propose un achèvement immédiat à 664 diamants. Elle exige Terrain de Rassemblement niveau 1, Entrée niveau 7 et Laboratoire niveau 7, puis 81 489 nourritures et 81 256 feuilles.
- Le niveau 8 ajoute +28 084 de puissance, porte la capacité de soins de 42 000 à 44 000 et débloque `Équipement` et `Gènes`; la taille d'armée de base reste 100.
- Un cadeau flottant près de la Reine ouvre une offre unique `Coffre Reine Niv.7 en Promotion` à 6,99 $, limitée à environ 20 heures. Elle annonce 1 100 diamants et de nombreux accélérateurs de 60 et 5 minutes pour la construction, la recherche et la formation.
- Adaptation Bee Kingdom: `Réserves pour l'hivernage`. Le chapitre fait renforcer les alvéoles de pollen et de miel, constituer une population minimale, accomplir des sorties de ravitaillement et préparer la Chambre royale à débloquer l'équipement des championnes et les lignées génétiques.

### Défense intérieure débloquée pendant le chapitre 7

- Le raccourci vers le prérequis `Terrain de Rassemblement niveau 1` conduit vers une chambre occupée par trois scorpions. Le raccourci voisin `Repousser les envahisseurs` ouvre toutefois un mode de défense indépendant et ne nettoie pas ces scorpions. Cette proximité crée une fausse relation de cause à effet.
- Les scorpions physiques se combattent dans le système d'expédition normal. Le premier exige 1 250 unités; le suivant passe brutalement à 1 800. La formation initiale de 1 150 doit être renforcée avant même le premier combat.
- Lors d'un second relevé du verrou à 1 800, les trois Fourmis spécialisées militaires disponibles totalisent 1 170 de taille d'armée, auxquels s'ajoutent 100 soldates régulières, soit 1 270. Les deux emplacements d'héroïne suivants sont visibles mais aucune autre militaire n'est encore obtenue; Sinensis est civile et verrouillée pour cette expédition.
- Un clic sur l'expédition insuffisante ouvre `Augmentation Taille de l'Armée` et juxtapose trois solutions: compléter des quêtes pour obtenir davantage de Fourmis spécialisées, acheter des packs, ou améliorer les spécialistes existantes. Le pack payant occupe la rangée centrale, tandis que l'amélioration gratuite disponible reste clairement signalée.
- Chaque niveau permanent observé ajoute 30 à la taille d'armée d'une spécialiste. Pharaon passe des niveaux 12 à 13 puis 14 pour 1 290 et 1 370 unités de ressource d'amélioration; chaque niveau donne aussi +1 050 de puissance. La capacité totale de l'expédition passe ainsi de 1 270 à 1 330, encore sous le seuil de 1 800. Cette hausse lente rend l'obtention d'une quatrième militaire nettement plus pertinente que la seule dépense de ressources.
- Le rattrapage gratuit a finalement porté Fourmi Bleue et Pharaon au niveau 19, puis Oecophylla au niveau 19. Les coûts relevés sont progressifs: 1 560 pour 15 vers 16, 1 660 pour 16 vers 17, 1 770 pour 17 vers 18, 1 880 pour 18 vers 19 et 2 000 pour 19 vers 20. Chaque niveau ajoute toujours 30 de capacité et +1 050 de puissance.
- Au niveau 19, chacune des trois militaires fournit 570 places. Avec les 100 soldates de base, la formation atteint 1 810 sur 1 810 et se répartit automatiquement en 604, 603 et 603 unités entre les trois familles. Elle franchit donc le seuil de 1 800 avec seulement dix places de marge.
- Le scorpion à 1 800 donne deux accélérateurs d'entraînement de 5 minutes, un objet de percée R2 et trois objets de percée R1. La victoire affiche encore `WINNER` en anglais malgré l'interface française.
- Immédiatement après cette victoire, un second scorpion obligatoire bloque le même chantier avec un seuil de 2 250. Le déficit remonte donc à 440 places, soit quinze niveaux supplémentaires de spécialiste si aucune quatrième militaire n'est obtenue.
- Le rattrapage a ensuite porté Oecophylla, Fourmi Bleue et Pharaon au niveau 22. Chacune fournit alors 660 places; avec les 100 soldates de base, la capacité totale atteint 2 080. Le niveau 22 vers 23 coûte 2 380 miellats et ajoute encore 30 places. Il reste donc 170 places à gagner, soit six niveaux répartis entre les trois spécialistes, pour franchir le seuil de 2 250 avec une capacité minimale de 2 260.
- Adaptation Bee Kingdom: une série de gardiennes peut escalader, mais le joueur doit voir le prochain seuil et la voie gratuite avant d'investir. Deux murs de puissance successifs ne doivent jamais donner l'impression que la solution gratuite précédente était un leurre.
- Le combat devient un mini-jeu de défense de chemin. Jusqu'à cinq Fourmis spécialisées peuvent composer la formation et occuper des emplacements défensifs qui se déverrouillent progressivement autour d'un parcours sinueux.
- La formation observée réunit Fourmi Pharaon niveau 12, Oecophylla niveau 12, Fourmi Bleue niveau 11 et Sinensis niveau 1. Le total de dégâts affiché passe de 161 avec une combattante à 571 avec les quatre.
- La commande `Entraîner` consomme 5 unités d'une ressource commune et fait apparaître une carte temporaire. Deux cartes identiques de même niveau fusionnent vers le niveau suivant; une carte peut ensuite être glissée sur un poste de défense.
- Une même Fourmi spécialisée ne peut pas occuper deux postes. Les doubles servent à améliorer temporairement sa carte, tandis que la diversité de la formation permet de couvrir plusieurs postes.
- Les premiers paliers observés vont du niveau temporaire 1 ou 3 au niveau 6. Une Pharaon temporaire de niveau 5 inflige 915 dégâts par tir et suffit aux premières vagues.
- Une carte temporaire de niveau supérieur peut être déposée sur une tour déjà occupée pour l'améliorer; l'ancienne carte revient alors dans la réserve. Tous les cinq niveaux permanents de la Fourmi spécialisée, sa tour gagne aussi un niveau de départ: la Fourmi Bleue passe ainsi à une tour niveau 4 au niveau 15.
- La `Grotte abyssale` apparaît vers la vague 6 et permet de recycler des soldates. La `Fusion auto` est introduite vers les vagues 9 à 10, puis une commande de vitesse `x1` apparaît autour de la vague 11.
- La Fusion auto propose un essai gratuit unique de 3 heures et une activation permanente à 6,99 $. L'essai a été activé; aucun achat réel n'a été effectué. L'automatisation fusionne les cartes compatibles sans modifier directement leurs dégâts affichés.
- Un indicateur de boss apparaît vers la vague 15 et un poste défensif supplémentaire vers la vague 16. Le coût d'entraînement monte de 5 à 8 vers la vague 20.
- La progression a été conservée jusqu'à la vague 27. Le premier palier gratuit, à la vague 25, donne 1 000 unités de nourriture. Une piste `Ravitaillement Avancé` à 21,99 $ ajoute des récompenses payantes aux paliers 25, 30, 40, 50 et suivants.
- La commande de vitesse `x1` annonce un `Essai gratuit 3 mins`, mais son bouton `Activer` ouvre en réalité le `Pass Hebdomadaire Argent` à 6,99 $, qui regroupe accélération de combat, combat automatique, +20 % de vitesse de construction et de recherche et cadeaux quotidiens. Aucun achat n'a été effectué et l'essai n'a pas été activé.
- Une bannière `VICTOIRE` apparaît après chaque vague. Le bouton `Retour` quitte immédiatement le mode sans confirmation et conserve la progression atteinte.

Adaptation Bee Kingdom: `Défense du couvain`. Des larves de fausse teigne, petits coléoptères de la ruche ou intrus progressent dans les galeries de cire. Le joueur place ses Championnes sur des postes, fusionne des renforts temporaires et débloque graduellement recyclage, vitesse et automatisation. Les jetons fusionnés représentent des escouades de soutien et non des doubles permanents d'une même héroïne.

Principes à conserver: variété tactique, total de puissance lisible, déverrouillage progressif et apprentissage manuel avant l'automatisation. Irritants à corriger: confusion entre mode secondaire et menace obligatoire, saut de force 1 250 vers 1 800 sans chemin annoncé, formation initiale artificiellement limitée à une seule héroïne, bannière répétée à chaque vague, transition brutale vers un genre différent, sortie sans confirmation pendant une partie active et faux essai gratuit conduisant directement à une offre payante.

### Quêtes quotidiennes et détour gratuit par l'Aventure

- La piste quotidienne utilise des coffres aux seuils de 30, 60, 90, 120 et 150 points. Les petites tâches valent généralement 5 points; l'achat d'un coffre en vaut 20. Les tâches observées couvrent Aventure, endurance, incubation, amélioration de spécialistes, bonus de production, entraînement, classement et entraide d'alliance.
- Le premier coffre, à 30 points, donne exactement 10 Endurance et dix accélérateurs universels d'une minute. La récompense est immédiatement lisible et proportionnée à un premier palier quotidien.
- Le niveau 5 de l'Aventure oppose deux ennemis violets de niveau 3 pour une puissance recommandée de 160 000. Les trois militaires seules restent à 140 100; l'ajout de Sinensis civile dans le quatrième emplacement porte la formation à 179 100 et rend le combat favorable.
- La victoire du niveau 5 donne 20 000 miellats et 10 Épis de blé. Cette récompense gratuite finance les derniers niveaux nécessaires au scorpion de 1 800, mais aucun lien n'est présenté depuis l'écran de blocage vers cette solution.
- Le raccourci `C'est parti` d'une quête peut fermer la liste sans recentrer la caméra, et celui de la question quotidienne a conduit une fois vers la boutique plutôt que vers le questionnaire. Les raccourcis doivent être testés comme des parcours complets, pas seulement comme des changements d'écran.
- Adaptation Bee Kingdom: les tâches quotidiennes servent de filet de rattrapage et de découverte. Lorsqu'une activité gratuite contient la ressource qui résout un verrou de tutoriel, le panneau du verrou doit l'indiquer explicitement et y conduire en un clic.

### Phéromones et expéditions régionales

- Le Poste de garde ouvre un tableau cartographique de missions renouvelé sur un cycle d'environ huit heures. Au niveau 2, cinq épingles étaient visibles et l'amélioration exigeait quatre quêtes de phéromone terminées ainsi que trois Fourmis spécialisées possédées.
- Une mission `Éliminez la Menace Niv.1` demandait de vaincre un Scarabée géant niveau 1 à cinq unités de distance, avec une puissance recommandée de 900. L'expédition automatique a engagé les trois spécialistes militaires et 1 883 soldates sur une capacité de 2 080, pour une puissance affichée de 5 649.
- Le déplacement aller durait environ 24 secondes et le retour environ 6 secondes. La cible disparaît après la victoire, mais la récompense n'est ni créditée ni comptée immédiatement dans l'objectif d'amélioration.
- Le joueur doit revenir au tableau et toucher l'épingle devenue rouge avec un paquet cadeau. Cette seconde action crédite 1 000 miellats, 10 000 nourritures, 10 000 feuilles et deux accélérateurs de recherche de 5 minutes; le compteur passe alors seulement de 3/4 à 4/4.
- L'amélioration du tableau du niveau 2 au niveau 3 est gratuite une fois les conditions satisfaites. Le niveau 3 affiche huit missions et demande huit quêtes terminées ainsi que quatre spécialistes possédées; les quatre quêtes précédentes restent comptabilisées.
- Adaptation Bee Kingdom: le système devient un `Tableau des signaux de butinage`. Les missions se renouvellent par cycle et proposent défense locale, exploration, secours, collecte et chasse. Une expédition réussie doit afficher sans ambiguïté `Récompense à réclamer` et offrir un retour direct vers sa récompense; une option de crédit automatique peut aussi éviter ce double passage sans supprimer la mise en scène du retour des abeilles.

### Aventure, récompenses et boutique d'échange

- Les niveaux 6 à 8 ont été franchis avec quatre spécialistes pour une puissance d'environ 190 650. Le niveau 6 recommandait 120 000 et opposait cinq ennemis bleus niveau 4; le niveau 8 employait déjà des ennemis niveau 5.
- Les récompenses récurrentes observées comprennent 10 Épis de blé, quatre accélérateurs universels de 5 minutes, 10 000 nourritures et 10 000 feuilles. Une récompense quotidienne distincte a ajouté 40 Épis; le joueur était non classé et ne recevait encore aucun bonus de classement.
- Le bouton `Niveau suivant` lance immédiatement le combat suivant sans repasser par un aperçu d'équipe ou d'adversaires. Cette cadence est fluide, mais elle peut engager le joueur avant qu'il ait compris le changement de difficulté.
- La boutique d'échange Aventure utilise les Épis de blé. Elle proposait un accélérateur gratuit de 10 minutes renouvelé quotidiennement, réclamé pendant l'observation, un oeuf spécialisé intermédiaire à 1 000 Épis avec remise annoncée de 50 %, et un oeuf supérieur à 2 500 Épis verrouillé jusqu'au niveau d'Aventure 200.
- Les matériaux de percée R1 coûtaient 15 Épis, avec 30 unités disponibles, tandis que les matériaux R2 coûtaient 30 Épis et restaient verrouillés jusqu'au niveau 50. Les limites, stocks possédés et conditions de déblocage sont visibles avant l'achat.
- Le raccourci quotidien `Aventure: Récupérable` recentre seulement la colonie sur l'entrée physique de l'Aventure. Le joueur doit encore toucher le bâtiment, entrer dans le mode, puis trouver l'icône de récompense. Ce parcours comporte trop d'étapes pour une action présentée comme récupérable.
- Adaptation Bee Kingdom: l'`Exploration des prairies` conserve la carte de progression, les récompenses quotidiennes et une monnaie gagnée en jeu. Le prochain combat doit offrir un aperçu bref avec une commande explicite, et tout raccourci `À réclamer` doit ouvrir directement la récompense concernée.

### Arène et formation compétitive

- L'Arène regroupe trois modes sur une même page: `Reine-Guerre légionnaire`, `Duel Spécialisé` et `Reine-Combat légionnaire`. Le troisième reste verrouillé jusqu'au niveau 15 de la Reine. La saison observée durait encore environ six jours et vingt-deux heures.
- La Guerre légionnaire place le nouveau compte au classement 9 980 et annonce une récompense quotidienne de 100 jetons d'Arène. Dix essais sur dix étaient disponibles, ainsi qu'une réserve affichée de trois objets de renouvellement.
- La sélection d'adversaire présente quatre `Gardes d'Élite` de niveau 1 avec leur rang, leur armée et la récompense quotidienne associée. Les armées proposées allaient de 12 à 183, contre 1 420 avant correction de la formation du joueur; ces adversaires semblent servir de palier d'initiation contrôlé.
- La préparation du défi offre cinq emplacements de spécialiste, mais seulement deux étaient remplis automatiquement. L'ajout manuel de Pharaon a porté la taille d'armée de 1 420 à 2 080. La composition finale comptait 697 corps à corps R2, 688 tireuses R2 et 695 véloces R2.
- La composition révèle directement les rôles: Fourmi Bleue est `Soldate véloce`, tandis qu'Oecophylla et Pharaon sont `Tireuses`. Les trois spécialistes donnent ensemble +1 980 de taille d'armée et des bonus de PV affichés de 5 % aux Tireuses, 5 % aux Véloces et 2,5 % à l'armée globale.
- Les quantités de chaque famille peuvent être ajustées par curseur, boutons moins/plus et statistiques détaillées. La formation reste éditable avant le dernier bouton `Défi`; aucun combat n'a été lancé pendant ce relevé.
- Adaptation Bee Kingdom: l'Arène devient une série d'`Épreuves de vol` séparant duel de Championnes, bataille d'escouades et guerre de Reines. La formation doit appliquer le choix de classe explicite de Bee Kingdom, montrer les relations Gardiennes/Voltigeuses/Lanceuses et préremplir toutes les Championnes admissibles sans cacher une amélioration majeure derrière un emplacement vide.

### Leçons commerciales du chapitre 7

- Une offre liée à un jalon précis est plus pertinente qu'une publicité générique, mais elle doit apparaître après le résultat de l'action et ne jamais recouvrir le bâtiment ou la commande principale.
- Bee Kingdom peut proposer des coffres de progression temporaires, en séparant clairement les objets possédés, les récompenses gratuites, les prix en monnaie premium et les achats en argent réel.
- La `Boutique de cash` contient un coffre quotidien explicitement marqué `Gratuit (Progression : +1)`, limité à une réclamation par jour. Le relevé observé donne un accélérateur de 5 minutes, un oeuf spécialisé commun et 500 points d'expérience de commandement.
- Cette réclamation gratuite fait progresser la même jauge que les achats, avec des jalons visibles à 5, 15, 30, 50, 80 et 150 points et des récompenses intermédiaires à 10, 20, 40, 65 et 100. Elle installe ainsi une visite quotidienne dans la boutique avant d'exposer des coffres à 1,39 $, 6,99 $ et 14,99 $, ainsi qu'un lot `Tout acheter` à 39,99 $ annoncé à -16 %.
- Le `Pass Hebdomadaire Fourmi` est affiché à 6,99 $ et promet des récompenses chaque jour pendant sept jours après l'achat, avec choix d'une Fourmi spécialisée mythique préférée. Les rangées visibles incluent notamment 500 diamants au jour 5, dix objets violets et 5 000 miellats au jour 4, vingt accélérateurs de 60 minutes au jour 6, puis de très gros paquets de ressources et un choix mythique au jour 7.
- Ce pass illustre une bonne cadence commerciale, mais son héroïne mythique choisie et ses volumes de ressources peuvent créer un avantage durable. Bee Kingdom doit réserver le choix payant à une Championne déjà accessible gratuitement, à des fragments de rattrapage ou à une variante cosmétique, jamais à une caste de combat exclusive.
- Bee Kingdom peut reprendre le rendez-vous quotidien sous la forme d'une `Caissette de la ruche`, mais le bouton gratuit doit rester sans ambiguïté, ne jamais ouvrir un moyen de paiement et ne pas rendre une Championne indispensable exclusive aux paliers payants.
- Une option groupée comme `3 sorties` est un bon confort lorsque son coût total et son résultat attendu sont visibles. Elle ne doit pas augmenter les récompenses par point d'endurance ni offrir un avantage de combat inaccessible au parcours gratuit.
- Un écran de résolution d'un blocage peut présenter une offre payante parmi plusieurs voies, mais Bee Kingdom doit placer le parcours jouable en premier, annoncer son effort réel et garantir qu'il permet de franchir le seuil dans un délai raisonnable. Une Championne achetée ne doit jamais être la seule réponse pratique à une exigence de composition.
- Les offres doivent accélérer un projet choisi sans vendre directement la supériorité durable, la seconde caste ou une victoire garantie.
- Une automatisation comme la fusion automatique peut être vendue comme confort après une période d'essai. La version manuelle doit rester complète et permettre exactement les mêmes dégâts, récompenses et paliers à effort égal.
- Bee Kingdom ne doit pas vendre séparément chaque petite réduction de friction. Un abonnement de confort peut regrouper automatisation et vitesse, mais ses essais doivent réellement commencer gratuitement et ne jamais masquer un achat derrière le mot `Activer`.
- Une piste premium de défense peut financer le mode grâce à des cosmétiques, accélérations mesurées et ressources de rattrapage. Elle ne doit pas réserver des Championnes de combat exclusives ni modifier les règles compétitives.

### Couveuse et cadence des tirages

- La Couveuse sépare trois qualités d'incubation: basique, intermédiaire et supérieure. L'incubation basique annonce jusqu'à cinq essais gratuits quotidiens; après le premier essai observé, le prochain gratuit est revenu avec un compte à rebours d'environ 10 minutes. L'intermédiaire affichait environ 18 h 50 min avant sa gratuité et la supérieure 1 j 18 h 50 min.
- Les tirages possèdent leurs propres oeufs. Un tirage basique coûte un oeuf commun, tandis qu'un lot de dix coûte neuf oeufs. Cette remise de 10 % transforme naturellement l'accumulation en rituel d'ouverture groupée.
- Le tirage basique gratuit a donné deux tessons de Sinensis plutôt qu'une spécialiste complète. Le lot de dix oeufs communs a donné trois tessons de spécialistes, quatre lots de 1 000 miellats, sept accélérateurs de 5 minutes, 5 000 points d'expérience d'Escargot et 20 000 points supplémentaires. Aucun nouveau combattant complet n'a été obtenu.
- Les oeufs communs sont décrits comme une source possible de spécialistes rares, mais les résultats observés sont surtout des fragments et des consommables. Une tentative sans oeufs premium ouvre `Objets insuffisants`, indique `0/9` et propose `En obtenir plus`, créant un raccourci direct vers l'acquisition payante.
- Pour Bee Kingdom, chaque oeuf doit afficher les probabilités, le seuil de fragments et la garantie éventuelle avant l'ouverture. Le résultat doit distinguer fortement une Championne complète de ses fragments. Un lot de dix peut coûter neuf oeufs, mais la remise ne doit pas modifier les probabilités cachées ni être nécessaire pour obtenir une classe jouable.
- Le premier tirage de chaque qualité doit montrer son animation complète; les suivants peuvent être regroupés ou ignorés. La mise en scène est une récompense sensorielle, mais elle ne doit pas ralentir artificiellement l'accès au résultat ni masquer la nature exacte des objets reçus.

### Résolution de la tempête tropicale

- L'événement demande finalement Entrée niveau 4 et trois Dépôts de nourriture, dont un au niveau 3.
- Le Dépôt de nourriture niveau 2 produit 240 unités par heure et stocke 2 400 unités.
- Son passage au niveau 3 exige Reine niveau 5, coûte 143 feuilles et dure 1 minute.
- Le niveau 3 ajoute +45 de puissance, +126 de production horaire et +1 260 de capacité.
- Une fois les deux conditions remplies, l'écran affiche `Préparation terminée` et une commande `Prêt`.
- La validation transforme immédiatement l'ambiance de la colonie: pluie soutenue, lumière froide et nouveau contexte narratif `Conflit des Terres humides`.
- Ce changement visuel donne du poids à l'événement. Bee Kingdom peut utiliser orage, froid, sécheresse ou intrusion pour modifier temporairement la ruche et ses priorités sans altérer la carte permanente.

### Irritants observés à améliorer dans Bee Kingdom

- Les guidages simultanés du chapitre et de la tempête peuvent détourner la caméra ou conduire au mauvais écran.
- Le format étroit tronque parfois les noms, descriptions et quantités; les panneaux doivent se recomposer plutôt que couper le texte.
- Les objectifs réclamés se réordonnent ou défilent de façon difficile à suivre.
- Les cadeaux flottants et coffres peuvent recouvrir la navigation et les bâtiments.
- Certaines bannières annoncent des objectifs accomplis sans relation évidente avec l'action qui vient d'être faite.
- Le déblocage visuel des unités R2 n'apparaît qu'au prochain retour dans le baraquement, au lieu de coïncider avec la fin de l'amélioration qui l'a provoqué.
- Le raccourci vers une recherche peut conduire à une chaîne de blocages sans afficher d'abord son chemin complet: zone à ouvrir, prédateur à vaincre, laboratoire à construire, puis technologie à lancer.
- Une demande de notation apparaît immédiatement après la récompense du chapitre 5 et interrompt la transition vers le chapitre suivant.
- Le guidage vers une mission régionale peut faire ouvrir par erreur la colonie d'un autre joueur située près de la cible; les zones tactiles et la priorité des marqueurs doivent être sans ambiguïté.
- Sur la carte extérieure, une zone tactile invisible de la mission recommandée peut rester active au-dessus des raccourcis pourtant visibles. Un clic sur la loupe renvoie alors dans la ruche et sélectionne le bâtiment de l'objectif. Bee Kingdom doit désactiver les raycasts et zones tactiles de tout panneau masqué.
- Après une déconnexion `-10013`, la confirmation de reconnexion peut masquer la fenêtre sans rétablir la simulation: compteurs figés, clics ignorés ou traités avec retard et absence d'indicateur persistant. Bee Kingdom doit bloquer les actions pendant la resynchronisation, afficher un état réseau sans ambiguïté et ne réactiver l'interface qu'après validation complète de l'état serveur.
- Bee Kingdom doit garder une hiérarchie claire: action courante, résultat immédiat, progression de chapitre, puis événement secondaire.

## Règles UX à retenir pour Bee Kingdom

1. Chaque action complexe doit avoir un écran dédié lisible.
2. La ruche reste visuelle; les données détaillées n'apparaissent qu'après une action volontaire. Les bâtiments ne portent pas d'icône centrale permanente: le contour suffit pour l'état et la sélection.
3. Le tutoriel montre une seule décision à la fois et déplace lui-même la caméra.
4. Les prérequis verrouillés doivent rester visibles et conduire directement vers leur résolution.
5. Toute action temporisée doit avoir une file persistante, un temps restant et des moyens d'accélération clairement séparés.
6. Les récompenses utilisent une séquence en deux niveaux: objectifs individuels, puis récompense de chapitre.
7. La progression produit des changements visibles dans la colonie, pas seulement des nombres.
8. Les systèmes sont introduits par une conséquence narrative: faim, invasion, blessures, météo, expansion.
9. Les événements temporaires réemploient les systèmes de base et ajoutent une urgence lisible.
10. L'identité Bee Kingdom doit privilégier cire, propolis, pollen, miel, gelée royale, couvain, gardiennes et menaces naturelles de la ruche.
11. Les icônes de ressources doivent être grandes, nettes, premium et compréhensibles sans libellé permanent; aucun encadré de texte vide ne reste visible.
12. Les panneaux contextuels doivent se fermer par leur X et par un clic hors bâtiment, ce qui désélectionne aussi le bâtiment.
13. La ruche et les bannières de menus doivent être animées en continu par des boucles légères, lisibles et peu coûteuses en performance.
14. L'automatisation premium ne doit jamais augmenter le rendement maximal par rapport à un joueur gratuit actif.
15. Toute offre commerciale doit exposer sa durée, sa valeur et l'alternative gratuite sans ambiguïté.

## Questions à relever pendant la suite

- Fin exacte du tutoriel guidé et moment où le joueur obtient une liberté complète.
- Fonctionnement détaillé de la carte mondiale, des marches et de l'occupation territoriale.
- Composition d'escouade, héros spécialisés, compétences et contre-unités.
- Recherche, arbre technologique et dépendances entre bâtiments.
- Alliance, chat, messagerie, rapports de combat et aides de construction.
- Quêtes quotidiennes, événements, boutique, VIP et cadence des offres.
- Défense de colonie, réparations, garnison et pertes en JcJ.
- Boucles de récolte, production hors ligne, capacité et protection des ressources.
- Cadence réelle des chapitres après les premières minutes.
- États vides, erreurs, confirmations, badges, notifications et retours de commande.

## Comparaison future avec un second jeu

Lorsque le second jeu similaire sera disponible, il sera documenté séparément avec la même grille d'observation. Une matrice de synthèse comparera ensuite:

1. la progression du tutoriel et le temps avant autonomie;
2. la lisibilité de la ruche, de la carte, des files et des menus;
3. les boucles de construction, collecte, entraînement, exploration et combat;
4. les classes, formations, contres, héros et profondeur tactique;
5. les animations, retours sensoriels et changements visibles du monde;
6. les alliances, le chat, les événements et la coopération;
7. la boutique, les offres, les abonnements et la pression commerciale;
8. les irritants: interruptions, clics superflus, ambiguïtés, attentes et murs de progression.

Bee Kingdom retiendra le meilleur principe de chaque jeu seulement lorsqu'il sert l'univers des abeilles, la compréhension du joueur et un modèle commercial non pay-to-win. Les systèmes jugés efficaces mais irritants seront simplifiés; les ressemblances de noms, textes, illustrations, écrans ou paramètres propriétaires ne seront pas reproduites.
