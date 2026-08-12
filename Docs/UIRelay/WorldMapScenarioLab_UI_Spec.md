# WorldMap Scenario LAB LOCAL - UI Spec P6

Date locale: 2026-07-15
Role: UI-Relay read-only
Portee: specification UX compacte pour scenarios configurables. Aucun fichier Unity, PNG, terrain Wave5, BearDen, APK, serveur ou donnee reelle ne doit etre modifie par cette spec.

## Base locale constatee

- `LAB LOCAL` existe deja avec deux ruches test, `Apply`, `Reset`, test collecte et test combat.
- P2 `LECTURE CARTE` existe avec filtres Ruches/Ressources/Menaces/BearDen, `Proche` et legende.
- P3/P4 valident collecte locale, epuisement, respawn demo, combat solo/raid local, symboles non limites a la couleur, BearDen preserve.
- Toute action LAB reste locale/demo: `official_gain=false`, `server=false`, aucune persistance officielle.

## Objectif UX

Permettre de configurer rapidement trois scenarios de demo sans masquer la carte ni creer d'ambiguite avec un etat officiel. Le panneau doit servir de banc d'essai local, pas d'interface de production.

## Implantation du panneau

- Nom visible permanent: `LAB LOCAL`.
- Badge obligatoire dans l'en-tete: `LOCAL - NON OFFICIEL`.
- Position tablette paysage: colonne droite, largeur cible 320 px, hauteur ouverte maximum 42% ecran, repliee 44 px.
- Position telephone portrait: tiroir bas compact, largeur `min(94% ecran, 380 px)`, hauteur ouverte maximum 40% ecran, defilement interne si besoin.
- Etat par defaut: replie si `LECTURE CARTE` est ouvert; ouvert possible si `LECTURE CARTE` est replie.
- Aucun modal, aucun voile, aucun panneau plein ecran; le terrain doit rester visible et manipulable hors rectangle HUD.

## Coexistence avec LECTURE CARTE

- `LECTURE CARTE` garde la priorite a gauche/haut; `LAB LOCAL` reste a droite ou bas.
- Les rectangles interactifs ne doivent jamais se chevaucher, y compris en portrait.
- Si les deux panneaux sont ouverts en portrait, `LAB LOCAL` passe en mode resume: presets + Apply/Reset seulement; les champs avances restent accessibles par defilement interne.
- Les filtres, `Proche`, legende et BearDen ne doivent pas changer les valeurs du LAB.
- Les presets LAB peuvent selectionner/mettre en evidence des noeuds, mais ne doivent pas forcer un filtre off a devenir on sans feedback explicite.

## Presets scenarios

Chaque preset est un bouton compact avec icone, couleur et libelle. Appliquer un preset marque le panneau comme `modifie` jusqu'a `Apply` ou `Reset`.

- `Collecte R3`: selectionne une ressource riche visible/eligible, configure la ruche joueur avec capacite de collecte suffisante, affiche objectif `Collecte locale R3`, conserve `official_gain=false`.
- `Duel`: configure PLAYER_TEST_HIVE et ENEMY_TEST_HIVE en niveaux proches, soldats/PV comparables, position lisible a l'ecran, combat local solo.
- `Raid T7`: selectionne ou prepare une menace T7 raid, augmente soldats/PV disponibles pour demo, affiche requis/disponible, conserve resultat local deterministe.
- Si la cible du preset est filtree ou hors fenetre active: afficher un avertissement inline et proposer `Proche`/recentrage doux, sans modifier les filtres silencieusement.

## Edition des deux ruches test

Deux sections symetriques: `Joueur` et `Ennemi`.

- Champs: niveau, classe, soldats, PV, ressources, position.
- Niveau: stepper ou champ numerique borne; paliers visuels H1/H2/H3 indiques par libelle court.
- Classe: select/segmented control; desactive ou `Neutre` avant niveau 10 si la logique locale l'exige.
- Soldats/PV/ressources: numeriques avec bornes visibles et message inline si hors borne.
- Position: coordonnee lisible + bouton `Placer ici` qui prend le centre carte courant; pas de glisser force sur carte si le panneau est actif.
- Toute valeur editee mais non appliquee porte l'etat `modifie`.

## Signal LOCAL / NON OFFICIEL

- Badge en en-tete toujours visible dans les etats replie et ouvert.
- Les boutons d'action disent `Apply local`, `Reset local`, `Test collecte local`, `Test combat local`.
- Les resultats affichent explicitement `Gain officiel: non` ou `official_gain=false`.
- Aucune formulation ne doit suggerer recompense, sauvegarde serveur, classement, achat, raid officiel ou progression reelle.

## Etats UI requis

- Selectionne: contour double sur le champ/preset actif + anneau sur noeud carte concerne.
- Modifie: point ou barre laterale jaune/or, bouton `Apply local` actif, texte court `Modifs non appliquees`.
- Reset: animation courte de retour + statut `Valeurs locales restaurees`; aucun changement terrain.
- Erreur: bord rouge + icone alerte + message inline; l'action principale est bloquee seulement pour le champ invalide.
- Disabled: opacite reduite, icone verrou/info, raison courte visible au focus.
- Succes local: coche breve + statut, sans celebrer un gain officiel.

## Symboles, couleur et accessibilite

- Presets: Collecte R3 = losange ressource + 3 traits; Duel = deux hexagones opposes; Raid T7 = triangle menace + chevrons groupe.
- Ruches: joueur et ennemi distingues par forme/contour en plus de la couleur.
- Erreur, modifie, selectionne et disabled doivent rester distinguables en monochrome.
- Contraste texte/panneau cible 4.5:1; marqueur/terrain 3:1 avec contour.
- Cibles tactiles: boutons 44 x 44 px minimum; steppers avec zones separees.
- Clavier/manette: ordre focus en-tete -> presets -> Joueur -> Ennemi -> actions; fleches ajustent steppers, Entrer applique, Echap replie/annule focus.

## Adaptation tablette et telephone

- Tablette paysage: `LECTURE CARTE` a gauche, `LAB LOCAL` a droite; centre carte libre pour inspection et selection.
- Telephone portrait: un seul panneau pleinement ouvert a la fois; l'autre reste en barre repliee. La carte conserve au moins 55% de hauteur visible.
- Les textes longs se compactent: `LOCAL - NON OFFICIEL`, `Apply local`, `Reset local`, `R3`, `Duel`, `Raid T7`.
- Aucun champ ne doit sortir de l'ecran; les champs avances defilent dans le panneau, jamais la page entiere.

## Criteres d'acceptation Unity

- Le panneau `LAB LOCAL` affiche toujours `LOCAL - NON OFFICIEL` en mode replie et ouvert.
- Les presets `Collecte R3`, `Duel`, `Raid T7` configurent uniquement l'etat local/demo et marquent `modifie` avant application.
- Les deux ruches test permettent l'edition niveau, classe, soldats, PV, ressources et position avec validation inline.
- `Apply local`, `Reset local`, `Test collecte local`, `Test combat local` ne declenchent aucun serveur, gain officiel, APK, persistance officielle ou modification PNG/terrain.
- Les etats selectionne, modifie, reset, erreur, disabled et succes local sont visibles par symbole + couleur, pas par couleur seule.
- En tablette paysage, `LECTURE CARTE` et `LAB LOCAL` peuvent coexister sans collision et sans masquer le centre carte.
- En telephone portrait, un panneau ouvert maximum a la fois; la carte garde au moins 55% de hauteur visible.
- Les filtres, `Proche`, legende et BearDen restent fonctionnels et ne sont pas modifies silencieusement par le LAB.
- Les controles respectent 44 x 44 px minimum, focus clavier/manette visible et ordre logique.
- Gate attendue: compilation Unity PASS, Play Mode PASS, preuve locale montrant les trois presets, edition des deux ruches, etats modifie/reset/erreur, coexistence avec `LECTURE CARTE`, tablette paysage et telephone portrait.

READY_FOR_BUILDER_A_CONSUMPTION=YES
