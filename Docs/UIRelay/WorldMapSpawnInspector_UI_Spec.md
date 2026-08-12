# WorldMap Spawn Inspector - UI Spec P7

Date locale: 2026-07-15
Role: UI-Relay read-only
Portee: specification UX compacte pour inspecteur LAB LOCAL des spawns deterministes. Aucun fichier Unity, PNG, terrain Wave5, BearDen, APK, serveur ou donnee reelle ne doit etre modifie par cette spec.

## Base locale constatee

- Les entites runtime sont construites par chunk logique seed et restent des overlays, jamais peintes dans le terrain.
- La carte visible reste Wave5 25x25; le futur 50x50 est logique/stress, desactive par defaut, sans art 50x50.
- Budgets P1: active chunks <= 25, hives <= 25, resources <= 75, bestiary <= 25, cache terrain Wave5 <= 96.
- `LECTURE CARTE` gere filtres, `Proche`, legende et BearDen; `LAB LOCAL` gere scenarios locaux non officiels.
- Le client affiche/previsualise seulement: aucun spawn, quantite, combat, recompense ou etat officiel n'est decide par l'UI.

## Objectif UX

Ajouter un inspecteur compact pour comprendre pourquoi et ou les ruches, ressources et menaces apparaissent dans la fenetre active 25x25 et dans l'apercu logique 50x50. L'outil sert au diagnostic local, pas a la generation officielle.

## Activation et emplacement

- Nom visible: `SPAWN INSPECTEUR`.
- Badge permanent: `LOCAL - APERCU NON OFFICIEL`.
- Overlay diagnostic desactive par defaut a chaque ouverture de scene.
- Position tablette paysage: panneau droit sous ou a cote de `LAB LOCAL`, largeur cible 320 px, hauteur ouverte max 38% ecran.
- Position telephone portrait: tiroir bas compact, hauteur ouverte max 36% ecran; un seul panneau LAB pleinement ouvert a la fois.
- Le terrain doit rester visible: aucun modal, aucun voile, aucun fond plein ecran, aucune carte miniature opaque.

## Selecteur de seed local

- Champ `Seed local` avec valeur courte lisible, bouton increment/decrement et bouton copier si disponible.
- Bouton `Aleatoire local` autorise uniquement une preview locale et marque l'etat `modifie`.
- Afficher `version seed` si disponible; sinon `version locale`.
- Changer la seed ne regenere rien automatiquement: l'utilisateur doit activer `Regenerer apercu`.
- Toute seed invalide affiche erreur inline et bloque seulement la regeneration.

## Regenerer apercu

- Bouton principal: `Regenerer apercu local`.
- Sous-texte ou statut obligatoire: `Jamais officiel`.
- L'action reconstruit uniquement l'apercu runtime/diagnostic local de la fenetre active ou du mode stress logique, sans serveur ni persistance.
- Apres regeneration: afficher timestamp local court, seed utilisee, monde cible `25x25 visible` ou `50x50 logique`.
- Si un budget est depasse, conserver l'apercu mais afficher avertissement; ne pas corriger silencieusement les spawns.

## Filtres famille et tier

- Filtres familles: `Ruches`, `Ressources`, `Menaces`, `Evenements`, `Exclusions`.
- Filtres tiers:
  - ressources: `R1`, `R2`, `R3`;
  - menaces: `T1-T4 solo`, `T5-T7 raid`;
  - ruches: `H1`, `H2`, `H3` ou niveaux/groupes si plus direct cote runtime.
- Les filtres diagnostic n'ecrasent pas les filtres `LECTURE CARTE`; si differents, afficher `Vue diag filtree`.
- Les filtres caches ne sont pas eligibles a la selection par clic dans l'inspecteur, mais l'entite deja selectionnee reste detaillee avec badge `hors filtre`.

## Panneau detail entite selectionnee

Le detail est compact et remplace les aides longues.

- Champs communs: `entity_id`, famille, type/variant, chunk logique, coordonnee monde, coordonnee normalisee, seed source, etat spawn.
- Ressource: tier R1/R2/R3, capacity, remaining si disponible, respawn_rule ou `demo`.
- Menace: tier T1..T7, mode solo/raid, PV local si disponible, requis raid si applicable.
- Ruche: niveau, classe, visual tier H1/H2/H3, faction overlay separe.
- Exclusion: type zone, raison, priorite, taille/volume approximatif si disponible.
- Boutons detail autorises: `Centrer`, `Copier id`, `Marquer local`. Aucun bouton de suppression, loot, validation officielle ou sauvegarde serveur.

## Compteurs densite et budgets

- Bandeau compact toujours visible quand l'inspecteur est ouvert:
  - chunks actifs: `x/25`;
  - hives: `x/25`;
  - resources: `x/75`;
  - bestiary: `x/25`;
  - cache Wave5 si expose: `x/96`.
- Couleurs et symboles:
  - OK: coche + vert/bleu calme;
  - attention >= 80%: triangle + ambre;
  - depassement: alerte + rouge + libelle `Budget depasse local`.
- En 50x50 logique, afficher aussi `catalogue: 2500 coord.` et rappeler `terrain 50x50: non genere`.
- Les avertissements ne doivent jamais masquer la carte: une ligne inline et une icone suffisent.

## Zones d'exclusion

- Familles a afficher: BearDen, eau, falaise, evenement.
- Les exclusions sont des overlays hachures/contours, jamais une peinture dans le terrain.
- BearDen: contour special et libelle court `BearDen exclu`; ne modifie pas l'affichage BearDen source.
- Eau: hachures bleues + symbole vague.
- Falaise: hachures grises + symbole pente.
- Evenement: pointille violet/ambre + symbole drapeau.
- Les exclusions peuvent etre activees seules pour verifier pourquoi aucun spawn n'apparait dans une zone.

## Symboles, couleur et lisibilite

- Ruches: hexagone; ressources: losange; menaces: triangle; exclusions: contour/hachure; evenements: drapeau.
- Tier par motif en plus de la couleur: R1/R2/R3 = 1/2/3 traits; T1..T7 = barres ou chevrons; H1/H2/H3 = 1/2/3 anneaux.
- Selection: anneau double; hover/focus: halo fin; hors filtre: opacite reduite + icone oeil barre.
- Contraste cible: texte/panneau 4.5:1, marqueur/terrain 3:1 avec contour.
- Le diagnostic doit rester lisible en monochrome; aucune information critique ne depend seulement de la couleur.

## Clavier, manette et tactile

- Ordre focus: activation overlay -> seed -> regenerer -> filtres -> liste/densite -> detail -> actions detail.
- Entrer/A active, Echap/B replie, fleches changent filtres/seed, L/R ou Tab change groupe.
- Taille tactile minimale: boutons 44 x 44 px; marqueurs carte avec hitbox etendue >= 32 x 32 px.
- Toute info hover doit aussi etre disponible au focus clavier/manette.

## Adaptation tablette et telephone

- Tablette paysage: `LECTURE CARTE` a gauche, `LAB LOCAL`/`SPAWN INSPECTEUR` a droite; centre carte libre.
- Telephone portrait: `SPAWN INSPECTEUR` replie par defaut; si ouvert, `LECTURE CARTE` et `LAB LOCAL` passent en barres repliees.
- Carte visible minimale: 55% de hauteur en portrait, 60% de largeur au centre en paysage.
- Les listes longues defilent dans le panneau, pas sur la page entiere.

## Criteres d'acceptation Unity

- `SPAWN INSPECTEUR` est desactive/replie par defaut et affiche `LOCAL - APERCU NON OFFICIEL` quand il est visible.
- Le selecteur `Seed local` ne regenere pas automatiquement; `Regenerer apercu local` est requis et affiche `Jamais officiel`.
- Les filtres famille/tier affectent seulement l'overlay diagnostic et n'ecrasent pas silencieusement `LECTURE CARTE`.
- La selection d'une entite affiche le detail requis pour ruches, ressources, menaces et exclusions, avec `entity_id`, chunk et coordonnees.
- Les compteurs densite affichent chunks, hives, resources, bestiary et avertissent a 80% puis au depassement des budgets P1.
- BearDen, eau, falaise et evenement sont visibles comme zones d'exclusion par contours/hachures, jamais par modification du terrain.
- Aucun bouton ou message ne suggere spawn officiel, sauvegarde serveur, loot, recompense ou changement persistant.
- Les symboles + couleurs restent distinguables en monochrome, avec contraste et tailles tactiles conformes.
- Tablette paysage et telephone portrait gardent la carte majoritaire et evitent toute collision avec `LECTURE CARTE` et `LAB LOCAL`.
- Gate attendue: compilation Unity PASS, Play Mode PASS, preuve locale montrant seed, regeneration, filtres, detail entite, budgets, exclusions et overlay off par defaut.

READY_FOR_P7_CONSUMPTION=YES
