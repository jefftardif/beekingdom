# World Map Responsive HUD - Visual Proof Protocol

Date locale: 2026-07-15
Role: UI Relay P8
Objet: protocole de preuve visuelle du Spawn Inspector sur tablette paysage et telephone portrait.

Ce document definit la future collecte P8. Il ne constitue pas lui-meme une preuve visuelle et n'autorise aucune modification terrain, BearDen source, serveur, donnee officielle ou gain persistant.

## Resultat attendu

La collecte doit permettre a un reviewer qui n'a pas lance la scene de verifier:

- l'etat OFF/replie au chargement;
- la geometrie responsive et la part de carte encore visible;
- les interactions seed, regeneration, filtres, selection et defilement;
- le detail des quatre familles;
- les quatre exclusions;
- les trois etats de budget;
- le mode 50x50 logique sans terrain 50x50;
- la lisibilite couleur/monochrome et les parcours clavier, manette et tactile;
- l'absence d'autorite officielle, de persistance, de loot et de suppression.

## Surfaces de reference

Les dimensions ci-dessous sont des pixels logiques du viewport apres application du Canvas scaler. La collecte doit aussi consigner la taille brute de sortie et le facteur d'echelle.

| Code | Surface | Viewport logique de reference | Contraintes chiffrees |
|---|---|---:|---|
| TAB-L | Tablette paysage 16:10 | 1280x800 | Inspecteur cible 320 px; hauteur ouverte / 800 <= 0,38; largeur de carte centrale libre / 1280 >= 0,60 |
| TEL-P | Telephone portrait long | 390x844 | Hauteur du tiroir / 844 <= 0,36; hauteur de carte visible / 844 >= 0,55; safe area respectee |

Si le banc de preuve ne peut pas produire exactement ces dimensions, utiliser le device le plus proche, consigner `largeur x hauteur`, ratio, orientation, safe area et facteur d'echelle, puis appliquer les ratios sur les dimensions reelles. Une capture sans dimensions connues est invalide.

## Preparation commune

1. Demarrer depuis un chargement frais de la scene cible.
2. Confirmer qu'aucun etat d'une execution precedente n'est restaure.
3. Conserver le mode local, `server=false` et `official_gain=false`.
4. Consigner la version seed affichee; la reference P7 annonce `spawn_v1`.
5. Consigner la valeur litterale de Seed A, son hash attendu `01b78336`, la valeur litterale de Seed B et son hash attendu `fef6f1b4`. Un hash seul ne suffit pas a reproduire le test.
6. Rejouer chaque surface depuis le meme etat frais et avec la meme sequence d'actions.
7. Pour les exclusions et budgets limites, utiliser uniquement un scenario diagnostic local reproductible. Consigner sa seed et sa configuration; ne pas maquiller un etat absent.
8. Stabiliser l'animation avant chaque capture sans masquer focus, selection, avertissement ou timestamp utile.

## Regles de collecte

- Chaque capture primaire montre le viewport entier, sans recadrage ni redimensionnement.
- Une annotation eventuelle est secondaire; la capture brute correspondante reste obligatoire.
- Le nom visible, le badge local, les compteurs et les messages necessaires doivent etre lisibles a l'echelle 100%.
- Une transition comportementale utilise une paire `avant` / `apres` avec une seule action entre les deux.
- Le pointeur ne doit pas masquer la cible. Pour une preuve de focus, le focus reste volontairement visible.
- Ne pas accepter une maquette, un composite ou un texte de rapport a la place d'un etat reellement rendu.
- Un scenario absent vaut `NOT_PROVEN`; un comportement contraire vaut `FAIL`.

## Fiche obligatoire par preuve

Chaque entree du bordereau P8 contient:

| Champ | Contenu |
|---|---|
| Proof ID | Identifiant du scenario et suffixe `TAB-L` ou `TEL-P` |
| Build/session | Identifiant local reproductible |
| Viewport | Taille logique, taille brute, orientation, facteur d'echelle, safe area |
| Etat initial | Seed, version seed, monde 25x25/50x50, filtres carte et diagnostic |
| Action unique | Clic, touche, direction, selection ou defilement effectue |
| Attendu | Critere precis de la matrice ci-dessous |
| Observe | Valeur ou comportement effectivement vu |
| Mesures | Boites, ratios, contrastes ou compteurs applicables |
| Verdict | `PASS`, `FAIL` ou `NOT_PROVEN` |
| Capture(s) | Identifiants des captures brutes avant/apres |

## Matrice de scenarios

Sauf mention contraire, chaque scenario est obligatoire sur TAB-L et TEL-P.

| Proof ID | Etat/action | Attendus observables |
|---|---|---|
| RP-01 | Chargement frais, aucune action | Overlay diagnostic OFF; inspecteur replie/desactive; carte majoritaire; aucun panneau en collision |
| RP-02 | Ouvrir `SPAWN INSPECTEUR` | Badge `LOCAL - APERCU NON OFFICIEL`; aucun modal/voile; bandeau budget visible; emplacement conforme au format |
| RP-03 | Modifier Seed A en Seed B sans regenerer | Etat `modifie`; marqueurs, IDs selectionnes, timestamp et compteurs appliques restent inchanges |
| RP-04 | Entrer une seed invalide | Erreur inline; seule la regeneration est bloquee; repli, filtres et navigation restent utilisables |
| RP-05 | Activer `Regenerer apercu local` | `Jamais officiel`; nouvelle distribution locale; timestamp court, seed utilisee et `25x25 visible`; aucune persistance annoncee |
| RP-06 | Basculer chaque famille puis un tier | Seul l'overlay diagnostic change; `LECTURE CARTE` conserve son etat; `Vue diag filtree` apparait si les vues divergent |
| RP-07 | Selectionner une entite puis masquer son filtre | Entite detaillee conservee avec `hors filtre`, opacite reduite et oeil barre; une autre entite cachee n'est pas cliquable |
| RP-08-H | Selectionner une ruche | ID, famille, type, chunk, coordonnees monde/normalisee, seed source, etat spawn, niveau, classe, H1/H2/H3 et faction separee |
| RP-08-R | Selectionner une ressource | Champs communs, R1/R2/R3, capacity, remaining si disponible, respawn_rule ou `demo` |
| RP-08-M | Selectionner une menace | Champs communs, T1-T7, solo/raid, PV local si disponible et requis raid si applicable |
| RP-08-X | Selectionner une exclusion | Champs communs, type de zone, raison, priorite et taille/volume approximatif si disponible |
| RP-09-B | Afficher BearDen seul | Contour/hachure distinct, libelle `BearDen exclu`, hit reel non nul, terrain et BearDen source visuellement preserves |
| RP-09-W | Afficher eau seule | Hachures bleues et symbole vague, hit reel non nul, absence de peinture terrain |
| RP-09-C | Afficher falaise seule | Hachures grises et symbole pente, hit reel non nul, absence de peinture terrain |
| RP-09-E | Afficher evenement seul | Pointille violet/ambre et drapeau, hit reel non nul, absence de peinture terrain |
| RP-10-N | Budget sous 80% | Les compteurs exposes portent coche et etat calme; valeurs et denominateurs lisibles |
| RP-10-A | Budget a au moins 80% sans depassement | Compteur concerne avec triangle et ambre; information encore lisible sans masquer la carte |
| RP-10-D | Budget strictement depasse | Alerte rouge et `Budget depasse local`; apercu conserve, aucune correction silencieuse |
| RP-11 | Basculer en 50x50 logique | `catalogue: 2500 coord.`, `terrain 50x50: non genere`, monde cible logique; aucun terrain 50x50 affiche |
| RP-12 | Ouvrir une liste assez longue puis defiler | Defilement limite au panneau; page et carte ne defilent pas; en-tetes/actions utiles restent atteignables |
| RP-13-K | Parcours clavier complet | Ordre activation -> seed -> regeneration -> filtres -> liste/densite -> detail -> actions; Entree active et Echap replie |
| RP-13-G | Parcours manette complet | A active, B replie, directions changent valeurs, L/R ou Tab change le groupe; aucun piege de focus |
| RP-13-T | Parcours tactile | Cibles de controle >=44x44 px logiques; hitboxes marqueurs >=32x32; aucun geste exige un hover |
| RP-14 | Etats selection/focus en couleur puis monochrome | Double anneau de selection, halo de focus, motifs R/T/H et types distinguables sans dependance exclusive a la couleur |
| RP-15 | Inventorier les actions visibles | Seulement `Centrer`, `Copier id`, `Marquer local` selon disponibilite; aucune suppression, loot, validation officielle ou sauvegarde serveur |

## Controle geometrique TAB-L

Relever les boites `x, y, largeur, hauteur` du viewport, de `LECTURE CARTE`, de `LAB LOCAL`, de `SPAWN INSPECTEUR` et de la plus grande zone centrale de carte non occultee.

Le verdict TAB-L est PASS seulement si:

- l'inspecteur est a droite, sous ou a cote de `LAB LOCAL`;
- sa largeur vise 320 px sans troncature des libelles;
- sa hauteur ouverte ne depasse pas 38% de la hauteur reelle;
- la zone centrale libre de carte atteint au moins 60% de la largeur reelle;
- `LECTURE CARTE` reste a gauche et ne chevauche ni l'inspecteur ni ses cibles;
- les listes defilent dans le panneau;
- les plus longs libelles, notamment le badge local et le message de depassement, restent entierement lisibles.

## Controle geometrique TEL-P

Relever les boites du viewport, de la safe area, du tiroir, des barres repliees et de la zone de carte visible.

Le verdict TEL-P est PASS seulement si:

- l'inspecteur est replie par defaut;
- ouvert, il forme un tiroir bas dont la hauteur ne depasse pas 36% de la hauteur reelle;
- `LECTURE CARTE` et `LAB LOCAL` deviennent des barres repliees et un seul panneau LAB est pleinement ouvert;
- la carte visible conserve au moins 55% de la hauteur reelle;
- aucune cible ni ligne de texte ne deborde de la safe area;
- le clavier logiciel, s'il apparait pour la seed, ne rend pas la validation ou la fermeture inatteignable;
- le defilement reste interne au tiroir et aucune action ne passe sous le bord bas.

## Controle des budgets

Pour RP-10-N/A/D, relever la valeur et le denominateur de:

- chunks actifs: `x/25`;
- hives: `x/25`;
- resources: `x/75`;
- bestiary: `x/25`;
- cache Wave5: `x/96`, s'il est expose.

Confirmer explicitement si `threats` et `bestiary` designent le meme compteur. Le seuil d'attention est calcule sur la valeur reelle: `x / budget >= 0,80`. Le depassement exige `x > budget`.

## Controle lisibilite et accessibilite

- Texte sur panneau: contraste mesure >=4,5:1.
- Marqueur contre terrain, contour compris: contraste mesure >=3:1.
- Chaque famille conserve une forme distincte: hexagone, losange, triangle, contour/hachure ou drapeau.
- Chaque tier conserve un motif: traits R1-R3, barres/chevrons T1-T7, anneaux H1-H3.
- Les informations obtenues au hover sont reproduites au focus.
- Les mesures de cibles portent sur la boite interactive, pas seulement sur l'icone visible.
- Le test monochrome doit provenir du rendu ou d'un filtre d'affichage consigne, jamais d'une annotation qui remplace l'etat teste.

## Regle de verdict P8

`PASS`: attendu visible, mesure conforme et preuve brute referencee.

`FAIL`: ecart visible ou mesure hors seuil.

`NOT_PROVEN`: capture, mesure, seed reproductible ou etat requis absent/ambigu.

Le gate visuel final ne peut etre PASS que si tous les Proof IDs obligatoires sont PASS sur les surfaces requises. Les resultats `Spawn inspector UI: PASS`, `Exclusion zones: PASS` ou `Density budgets: PASS` du recu P7 ne remplacent aucun Proof ID.

## Gate de disponibilite

Le protocole couvre les manques inventories par la contre-revue P7 et peut etre execute sans etendre l'autorite du client.

READY_FOR_P8_VISUAL_PROOF=YES
