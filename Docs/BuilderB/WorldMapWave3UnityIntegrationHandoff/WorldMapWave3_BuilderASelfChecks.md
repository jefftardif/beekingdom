# World Map Wave 3 - Matrice de self-checks Builder-A

## Regle de gate

Tous les controles marques `CRITIQUE` doivent etre `PASS`. Toute image de preuve doit etre issue du rendu Unity, avec l'overlay debug des chunks desactive pour les preuves produit.

| ID | Niveau | Controle | Methode / preuve PASS | Echec bloquant |
|---|---|---|---|---|
| HASH-01 | CRITIQUE | Master autoritatif | SHA-256 egal a `d3cdc2dde9d56cac58be6833790b6fd8fc38ac157f72a01dcebd8117583a95b4` | toute difference |
| HASH-02 | CRITIQUE | Bundle run1 | arbre egal a `2176c7c5b81108e006014a1310095c9570d414963539bc0766dd4c023456fd2f` | manque, extra ou difference |
| FILE-01 | CRITIQUE | Inventaire | exactement 25 PNG `R0C0_g2..R4C4_g2`, aucun doublon | compte different de 25 |
| FILE-02 | CRITIQUE | Dimensions/mode | 25/25 en `516x516 RGB` | autre dimension/mode |
| FILE-03 | CRITIQUE | Hashes tuiles | 25/25 egaux au manifeste | une seule difference |
| IMP-01 | CRITIQUE | Type/couleur | `Default`, 2D, sRGB On, Alpha None | import Sprite/alpha inattendu |
| IMP-02 | CRITIQUE | Sampling | Clamp, Bilinear, NPOT None, Mips Off | Repeat, resize ou mips |
| IMP-03 | CRITIQUE | Android | ASTC 6x6 RGB ou fallback ETC2 RGB4 documente; Crunch Off | format non documente |
| MAP-01 | CRITIQUE | Centre | `R2C2 -> chunk (32,32)` | autre mapping |
| MAP-02 | CRITIQUE | Coins | R0C0 `(30,30)`, R0C4 `(34,30)`, R4C0 `(30,34)`, R4C4 `(34,34)` | inversion/rotation/transposee |
| MAP-03 | CRITIQUE | Orientation | reconstruction Editor non compressee: identite seule correcte | flip H/V, rotation ou transposee |
| MAP-04 | CRITIQUE | Hors macro | missing/fallback explicite, aucune repetition modulo | art repete |
| UV-01 | CRITIQUE | UV interieures | `2/516..514/516` sur U et V, 25/25 | toute autre UV |
| UV-02 | CRITIQUE | Gouttieres invisibles | aucun pixel de gouttiere visible comme contenu | bord etire ou bande visible |
| SEAM-01 | CRITIQUE | 40 raccords | inspection native a 100%, 200% et zooms runtime; aucune ligne | une couture visible |
| SEAM-02 | CRITIQUE | Bords externes | clamp seulement sur 20 cotes externes | clamp sur frontiere interne |
| ZOOM-01 | CRITIQUE | Tablette paysage | `1920x1080`, zooms `0.85`, `1.10`, `1.35`, cadrage coherent | trou, couture ou grille |
| ZOOM-02 | CRITIQUE | Telephone portrait | `720x1280`, memes zooms, aucun panneau masque | trou, couture ou HUD perdu |
| PAN-01 | CRITIQUE | Pan multi-chunks | traverser au moins trois frontieres; marqueurs sans saut | saut, flash noir, art qui suit camera |
| CACHE-01 | CRITIQUE | Fenetre active | 25 textures en regime stable | chargement global 64x64 |
| CACHE-02 | CRITIQUE | Transition | cinq entrantes/cinq sortantes, plafond transitoire 30 | fuite ou depassement |
| CACHE-03 | IMPORTANT | Liberation | textures hors fenetre liberees apres bascule | memoire monotone |
| HUD-01 | CRITIQUE | HUD fixe | HUD, panneaux et minimap immobiles pendant pan/zoom | translation ou zoom UI |
| OVL-01 | CRITIQUE | Overlays monde | ruches, ressources, halos et selections restent alignes | derive apres pan/zoom |
| FLIGHT-01 | CRITIQUE | Vol actif | vol maintenu pendant changement de chunk, points A/B stables | saut ou disparition |
| FLIGHT-02 | CRITIQUE | Air-only | arc/direct aerien, independant des chemins peints | suivi d'une route au sol |
| GRID-01 | CRITIQUE | Esthetique produit | overlay chunks off et aucune grille de texture | grille visible |
| FALLBACK-01 | CRITIQUE | Rollback | flag Step4C restaure atlas/UV bornes sans suppression destructive | retour impossible |
| CLAIM-01 | CRITIQUE | Non-claims | local/demo, pas live, pas serveur, pas monde immense livre | claim officiel/live |

## Preuves minimales a joindre

1. sortie machine-readable du validateur de handoff;
2. capture import settings d'une tuile centrale et d'une tuile de bord;
3. tableau 25/25 des hashes et dimensions;
4. captures des cinq reperes d'orientation;
5. captures paysage et portrait aux trois zooms;
6. video courte d'un pan sur trois frontieres avec un vol actif;
7. capture sans grille montrant plusieurs raccords internes;
8. journal de cache avant/pendant/apres franchissement;
9. preuve du rollback Step4C;
10. manifeste de non-claims.

## Decision

- `PASS`: tous les controles CRITIQUE et IMPORTANT sont PASS.
- `PASS_WITH_RESERVES`: seulement reserve non visuelle/non fonctionnelle, explicitement acceptee par QA.
- `BLOCKED`: un check CRITIQUE echoue ou manque.

La presence d'une couture, d'une repetition, d'une mauvaise orientation, d'une grille, d'un vol guide par le decor ou d'un hash faux impose `BLOCKED`.
