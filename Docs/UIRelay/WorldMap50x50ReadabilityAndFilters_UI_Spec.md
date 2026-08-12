# WorldMap 50x50 Readability And Filters - UI Spec

Date locale: 2026-07-15
Role: UI-Relay read-only
Portee: specification UX pour la releve Unity. Aucun changement Unity, PNG, terrain Wave5, BearDen, APK, serveur ou donnee reelle.

## Base constatee

- `WORLD_MAP_50X50_READINESS_P1=PASS`: catalogue logique 50x50/2500 coordonnees, terrain visible Wave5 25x25 preserve, stress 50x50 desactive par defaut.
- `MAP_READING_TOOLS_P2=PASS`: panneau compact `LECTURE CARTE`, filtres Ruches/Ressources/Menaces/BearDen, selection du noeud le plus proche, legende tier/richesse, HUD fixe, terrain non masque.
- `FINAL_VISUAL_SMOKE_QA=PASS_WITH_NOTES`: ruches, ressources, menaces, raid T7, BearDen et pan/zoom visibles en preuve composee.
- Contrainte production: le client affiche et previsualise seulement; aucun resultat officiel, loot, spawn, quantite ou combat ne doit etre decide par l'UI.

## Objectif UI

Rendre lisible une densite 50x50 sans couvrir la carte: le panneau doit aider a filtrer, localiser et comprendre les noeuds actifs, tout en restant secondaire face au pan/zoom et a l'inspection directe de la carte.

## Panneau compact repliable

- Nom visible: `LECTURE CARTE`.
- Position tablette paysage: coin haut gauche, hors zone de geste principale, largeur cible 280 px, hauteur repliee 44 px, hauteur ouverte maximum 34% de l'ecran.
- Position telephone portrait: ancre bas gauche ou bas centre, largeur `min(92% ecran, 360 px)`, hauteur ouverte maximum 38% de l'ecran; la carte doit rester majoritaire.
- Etat replie: barre compacte avec titre, compteur court des overlays actifs et bouton icone ouvrir/fermer.
- Etat ouvert: trois groupes empiles: filtres, recherche/proche, legende. Pas de panneau modal, pas de fond plein ecran, pas de marketing.
- Interaction carte: le panneau reste fixe pendant pan/zoom et bloque seulement les touches commencees sur son rectangle; hors panneau, les gestes carte restent prioritaires.

## Filtres overlays

Les filtres n'affectent que les overlays runtime, jamais le terrain.

- `Ruches`: affiche/cache ruches joueur, ennemies et neutres; conserve les overlays faction separes.
- `Ressources`: affiche/cache R1/R2/R3; conserve les etats quantite, epuise, respawn.
- `Menaces`: affiche/cache bestiaire et combat local; T1..T4 marquables solo, T5..T7 raid.
- `BearDen`: affiche/cache uniquement l'overlay BearDen; ne modifie pas la source BearDen ni le terrain.
- Etat par defaut recommande: tous visibles, panneau replie, terrain non masque.
- Chaque bouton filtre doit combiner couleur, icone/forme et libelle court; la couleur seule ne porte jamais l'information.

## Recherche et selection du noeud le plus proche

- Action principale: `Proche` selectionne le noeud visible le plus proche du centre courant de la carte.
- Les filtres actifs limitent la recherche: un type cache ne doit pas etre selectionne par `Proche`.
- En cas d'egalite de distance: priorite au noeud deja selectionne, puis Menace raid, Menace solo, Ruche, Ressource, BearDen.
- Resultat affiche en une ligne courte: famille, nom/id court, distance arrondie, tier/richesse si applicable. Exemple: `Ruche proche: hive_player_test (23u)`.
- Si aucun noeud visible n'est eligible: afficher `Aucun noeud visible` et ne pas deplacer la camera.
- La selection doit centrer ou pointer le noeud sans zoom brutal; preference: anneau/pulse local sur le noeud et leger recentrage seulement si hors viewport.

## Legende richesse et tier

- Legende compacte, toujours visible dans l'etat ouvert.
- Ressources: `R1 pauvre`, `R2 moyen`, `R3 riche`.
- Menaces: `T1-T4 solo`, `T5-T7 raid`.
- Ruches: `N<10 neutre`, `N10+ classe`, `N20/35/50 evolution`.
- BearDen: entree separee `Zone speciale`.
- La legende doit utiliser les memes symboles que les marqueurs carte, pas seulement des textes.

## Etats visuels des noeuds

Tous les marqueurs doivent garder une silhouette lisible a petite taille et avoir un etat forme + couleur.

- Normal: opacite 100%, contour fin, icone de famille visible.
- Hover/focus: halo fin clair, infobulle courte, pas d'agrandissement qui deplace les voisins.
- Selected: anneau double ou pulse lent, detail court dans le panneau, prioritaire sur hover.
- Disabled/filtre cache: invisible sur carte; dans panneau, bouton filtre a opacite reduite et etat off explicite.
- Epuise: ressources visibles si filtre Ressources actif; symbole vide/barre diagonale, opacite 55%, quantite `0`, mention respawn si disponible.
- Raid: menaces T5..T7 avec chevron/insigne groupe, contour plus epais, libelle `raid`; ne pas utiliser seulement du rouge.

## Accessibilite couleur et forme

- Differencier les familles par forme: ruche hexagone, ressource losange, menace triangle/dent, BearDen repere special.
- Differencier richesse/tier par motif additionnel: 1/2/3 traits pour R1/R2/R3; chevrons ou barres pour T1..T7.
- Contraste minimum cible: texte/panneau 4.5:1, marqueur/terrain 3:1 avec contour.
- Taille tactile minimale: 44 x 44 px pour boutons, 32 x 32 px pour cible de marqueur avec zone de hitbox etendue.
- Navigation clavier/manette: focus visible, ordre Filtres -> Proche -> Legende, activation sans souris.

## Adaptation ecrans

- Tablette paysage: panneau a gauche, legende ouverte possible, carte lisible a droite et au centre; aucun element ne doit masquer le centre de lecture plus de 10% de la surface.
- Telephone portrait: panneau replie par defaut; ouvert en tiroir bas compact; maximum quatre lignes visibles avant defilement interne; aucun bouton ne doit sortir de l'ecran.
- Pan/zoom: HUD fixe et non scale; les marqueurs carte peuvent scaler legerement avec le zoom mais gardent une taille minimale lisible.

## Criteres d'acceptation Unity

- Le panneau `LECTURE CARTE` est replie par defaut et ouvert/ferme sans masquer plus de 34% de l'ecran paysage ou 38% de l'ecran portrait.
- Les quatre filtres Ruches/Ressources/Menaces/BearDen masquent uniquement les overlays runtime et ne modifient ni terrain Wave5, ni BearDen source, ni PNG.
- `Proche` selectionne le noeud visible eligible le plus proche du centre carte et ignore les familles filtrees off.
- La selection affiche un retour carte plus une ligne de statut courte, sans gain officiel ni appel serveur.
- La legende montre R1/R2/R3, T1-T4 solo, T5-T7 raid, ruches par progression et BearDen zone speciale.
- Les etats normal, hover/focus, selected, disabled, epuise et raid sont distinguables sans dependance exclusive a la couleur.
- Les boutons respectent une cible tactile minimale de 44 x 44 px; le panneau reste utilisable en tablette paysage et telephone portrait.
- Pendant pan/zoom, le HUD reste fixe, les gestes hors panneau pilotent la carte, et le terrain reste non masque par defaut.
- Les budgets P1 restent respectes: active chunks <= 25, cached textures Wave5 <= 96, hives <= 25, resources <= 75, bestiary <= 25 dans la fenetre active.
- Gate finale attendue: compilation Unity PASS, Play Mode PASS, preuve locale montrant filtres, `Proche`, legende, etats epuise/raid, tablette paysage et telephone portrait.
