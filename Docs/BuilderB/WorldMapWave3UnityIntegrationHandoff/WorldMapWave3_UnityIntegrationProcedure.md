# World Map Wave 3 - Procedure d'integration Unity differee

## 1. Objet et gates

Cette procedure prepare Builder-A a integrer le macro master UI-B 5x5 apres, et seulement apres:

- `Builder-C runtime gutter validation = PASS`;
- `QA Unity integration authorization = PASS`.

Le present lot est un handoff. Il n'est ni copie sous `Assets`, ni branche dans la scene, ni valide dans Unity ou sur appareil.

## 2. Sources autoritatives et inventaire futur

Source du bundle valide:

`C:\projets\beekingdomgame-master\artifacts\WorldMapWave3_RuntimeBundle_staging\run1`

Master UI-B associe:

- version: `uib-wave3-continuous-v1`;
- dimensions: `2560x2560 RGB`;
- SHA-256: `d3cdc2dde9d56cac58be6833790b6fd8fc38ac157f72a01dcebd8117583a95b4`;
- reconstruction canonique: `0` pixel different;
- raccords runtime: `40/40` valides;
- arbre `run1`: `2176c7c5b81108e006014a1310095c9570d414963539bc0766dd4c023456fd2f`.

Destination future reservee, non creee par Builder-B:

`Assets/BeeKingdom/Playground/Resources/WorldMapWave3Runtime/UIB_ContinuousMaster5x5_v1/`

| Source | Destination future | Action Builder-B |
|---|---|---|
| `run1/runtime/tiles/R0C0_g2.png` a `R4C4_g2.png` | racine ci-dessus, noms inchanges | aucune copie |
| `WorldMapWave3_RuntimeTileUnityHandoff.manifest.json` | `manifest.runtime.unity.json` sous la meme racine | aucune copie |
| lot Step4C actuel sous `Resources/WorldMapWave4/UIB_SectorWave1` | inchange | aucun ecrasement |

Le detail 25/25, avec hash et destination unitaire, est dans `WorldMapWave3_SourceDestinationInventory.csv` et dans le manifeste machine-readable.

## 3. Contrat du renderer existant

Les sources Unity lues en lecture seule montrent:

- `ChunkSize = 512` et fenetre active de rayon `2`, donc `5x5` chunks;
- rendu actuel par `Texture2D` et IMGUI;
- surface Step4C dessinee par `GUI.DrawTextureWithTexCoords`;
- provider actuel charge par `Resources.Load`;
- le provider actuel repete ses secteurs par modulo;
- si `AtlasTexture` existe, la branche atlas court-circuite le dessin par chunk;
- entites, vols, overlays et HUD sont deja separes de l'art de fond.

Consequences pour Builder-A:

1. conserver le renderer `Texture2D`/IMGUI pour cette passe;
2. ajouter un mode de provider distinct, active par un flag reversible;
3. dans ce mode, ne pas fournir d'`AtlasTexture`, sinon le chemin Step4C restera prioritaire;
4. supprimer tout modulo pour le lot 5x5 et utiliser un mapping borne;
5. ne pas reutiliser `GUI.DrawTexture(... StretchToFill ...)` sur les 516 pixels complets;
6. dessiner le rectangle monde de 512 unites avec les UV interieures seulement.

`SpriteRenderer`, `RawImage` et `pixels per unit` ne sont pas le chemin existant. Aucun PPU ne s'applique a IMGUI. Si une migration ulterieure vers `SpriteRenderer` est decidee, `1 pixel interieur = 1 unite monde` implique `PPU = 1`, mais cette migration est hors de ce handoff.

## 4. Sequence d'integration recommandee

### Phase A - Preflight sans activation

1. Exiger les deux gates PASS.
2. Rejouer `verify_handoff.py` et exiger `status = PASS`.
3. Verifier le hash du master et les 25 hashes PNG avant import.
4. Archiver les hashes des trois sources Unity et la configuration Step4C courante.
5. Creer un flag de selection de contenu, par exemple `Step4CContinuousAtlas` ou `Wave3RuntimeGutterTiles5x5`, avec Step4C comme valeur de repli.

### Phase B - Copie future et import

1. Copier les 25 PNG vers la destination reservee, sans renommer.
2. Copier le manifeste de handoff sous le nom `manifest.runtime.unity.json`.
3. Laisser le dossier Step4C intact.
4. Appliquer les reglages d'import de la section 5 a l'ensemble des 25 textures.
5. Verifier apres import que les fichiers PNG source gardent leurs SHA-256; les `.meta` Unity sont nouveaux, mais les PNG ne doivent pas etre reecrits.

### Phase C - Adapter le provider

1. Charger le manifeste et indexer les tuiles par `(worldChunkX, worldChunkY)`.
2. Utiliser le macro origin fixe `(30,30)` et non la position courante de camera.
3. Retourner `missing` hors de `[30..34] x [30..34]`; ne jamais appliquer de modulo.
4. Charger uniquement l'ensemble desire autour du chunk camera, rayon `2`.
5. Conserver les positions des ruches, ressources et vols en `WorldCoord`; le changement de texture ne doit pas les deplacer.

### Phase D - Dessin anti-coutures

Pour chaque chunk visible:

1. calculer son rectangle monde de `512x512`;
2. convertir les quatre bords a l'ecran avec les memes bords partages entre voisins;
3. ne pas appliquer le pixel snapping primaire de Step 3;
4. appeler `GUI.DrawTextureWithTexCoords` avec l'UV interieure exacte;
5. dessiner l'atmosphere une seule fois apres tout le fond;
6. dessiner ensuite ruches, ressources, vols aeriens et selections;
7. dessiner le HUD fixe en dernier;
8. garder l'overlay de grille des chunks desactive hors mode preuve/debug.

## 5. Reglages Unity recommandes

| Reglage | Valeur | Justification |
|---|---|---|
| Texture Type | `Default` | le provider consomme un `Texture2D`, pas un Sprite |
| Texture Shape | `2D` | carte plane IMGUI |
| sRGB | `On` | art couleur RGB |
| Alpha Source | `None` | les 25 tuiles du lot sont RGB |
| Alpha Is Transparency | `Off` | aucun bord transparent a dilater |
| Read/Write | `Off` | evite une copie CPU non compressee |
| Non-Power of 2 | `None` | conserver exactement `516x516` |
| Wrap U/V/W | `Clamp` | interdit la repetition et protege les bords externes |
| Filter Mode | `Bilinear` | les gouttieres fournissent les vrais voisins au filtre |
| Aniso Level | `1` | rendu orthogonal 2D, cout superflu au-dela |
| Generate Mip Maps | `Off` | zoom actuel `0.85..1.35`, evite le bleed inter-tuile des mips automatiques |
| Streaming Mipmaps | `Off` | aucun mip dans cette passe |
| Max Size | `1024` | preserve la texture NPOT `516x516` |
| Compression Editor de preuve | `None / RGB24` | orientation et raccords controles avant compression |
| Android principal | `ASTC 6x6 RGB`, qualite `Best`, Crunch `Off` | compromis qualite/memoire, 516 divisible par 6 |
| Android compatibilite | `ETC2 RGB4`, Crunch `Off` | seulement si le profil appareils ne garantit pas ASTC |

Ne pas activer les mipmaps sans produire des gouttieres specifiques a chaque niveau de mip. Les mipmaps generees independamment par tuile peuvent recreer des coutures.

## 6. Mapping exact 5x5

Repere source et repere IMGUI ont la meme orientation pour le placement:

- origine logique: haut-gauche;
- colonne croissante vers la droite = `worldChunk.x + 1`;
- ligne croissante vers le bas = `worldChunk.y + 1`;
- rotation `0`;
- transposee `false`;
- flip horizontal `false`;
- flip vertical de placement `false`.

Le macro est ancre aux chunks `[30..34] x [30..34]`, centre sur le chunk initial `(32,32)`:

| Tuile | Chunk monde | Rectangle monde |
|---|---|---|
| `R0C0` | `(30,30)` | `(15360,15360,512,512)` |
| `R0C4` | `(34,30)` | `(17408,15360,512,512)` |
| `R2C2` | `(32,32)` | `(16384,16384,512,512)` |
| `R4C0` | `(30,34)` | `(15360,17408,512,512)` |
| `R4C4` | `(34,34)` | `(17408,17408,512,512)` |

Le manifeste source signale que l'adaptation V appartient au runtime. Avec le chemin IMGUI existant, ne pas introduire un flip manuel: importer normalement et utiliser un UV positif. La preuve obligatoire reste visuelle et mecanique:

1. reconstruire le 5x5 en mode Editor non compresse;
2. tester identite, flips H/V, rotation 180, transposee et rotations 90;
3. exiger que seule l'identite corresponde au master;
4. verifier cinq reperes: quatre coins et centre;
5. exiger `0` inversion, rotation ou transposee.

## 7. Contrat UV et gouttieres

Chaque texture fait `516x516`. Le contenu canonique est le rectangle pixels:

`x=2, y=2, width=512, height=512`

UV exactes, definies sur les frontieres de pixels:

- `uMin = vMin = 2/516 = 0.003875968992248062`;
- `uMax = vMax = 514/516 = 0.9961240310077519`;
- `uvWidth = uvHeight = 512/516 = 0.9922480620155039`.

Regles absolues:

- les gouttieres ne sont jamais du contenu visible;
- elles ne sont accessibles qu'au filtrage bilineaire autour des bords interieurs;
- les 40 frontieres internes tirent leurs gouttieres des vrais pixels voisins du master;
- le clamp est permis uniquement sur les 20 cotes externes du macro master;
- `WrapMode.Repeat`, modulo, etirement du `516` complet et atlas repete sont interdits;
- l'art ne doit afficher ni grille ni ligne de chunk.

## 8. Strategie de chargement et budget mobile

Le cache logique est identique en tablette paysage et telephone portrait; seul le cadrage change.

Politique:

1. ensemble stable: rayon `2`, soit `25` tuiles maximum;
2. au franchissement d'une frontiere, cinq tuiles sortent et cinq entrent;
3. charger la bande entrante de facon asynchrone;
4. conserver l'ancienne bande jusqu'a disponibilite de la nouvelle;
5. basculer atomiquement, puis liberer la bande sortante;
6. plafond transitoire strict: `30` textures;
7. hors macro fourni: etat missing ou fallback Step4C explicite, jamais repetition;
8. ne jamais charger les `4096` chunks logiques du monde `64x64`.

Budget theorique pour les 25 tuiles `516x516`, hors metadata Unity/driver:

| Representation | 25 tuiles |
|---|---:|
| PNG sur disque | `11.2539 MiB` |
| RGB24 brut | `19.0441 MiB` |
| RGBA32 brut, reference pessimiste | `25.3922 MiB` |
| ETC2 RGB4 | `3.1740 MiB` |
| ETC2 RGBA8, reference | `6.3480 MiB` |
| ASTC 6x6 RGB recommande | `2.8214 MiB` |
| ASTC 8x8 RGB, reference plus destructive | `1.6117 MiB` |

Avec `Read/Write Off`, ne pas garder une copie CPU brute. Les mipmaps ne sont pas incluses; elles ajouteraient environ `33%`. A titre de garde-fou, un monde `64x64` entier representerait environ `462.25 MiB` en ASTC 6x6 et plus de `3 GiB` en RGB24: ce chargement global est interdit.

Pour une extension au-dela de ce premier macro, conserver la meme interface de cache et remplacer `Resources` par Addressables/AssetBundles regionaux seulement apres une decision d'architecture. Ne pas melanger cette migration avec la premiere integration du 5x5.

## 9. Separation couches et vols

Ordre de rendu recommande:

1. fond tuiles monde;
2. passe atmosphere unique;
3. ruches et ressources en coordonnees monde;
4. arcs, tokens et traces de vols aeriens en coordonnees monde;
5. selection et debug optionnel;
6. HUD, panneaux et minimap fixes.

Le provider d'art ne doit exposer aucune information de route aux vols. Les chemins peints dans le master sont decoratifs. Les vols restent des interpolations directes/arcs entre deux `WorldCoord`, independantes des tuiles chargees.

## 10. Rollback Step4C

Le rollback doit etre un changement de mode, pas une restauration destructive:

1. ne jamais ecraser `Resources/WorldMapWave4/UIB_SectorWave1`;
2. conserver le provider Step4C et son atlas;
3. si un check critique echoue, desactiver `Wave3RuntimeGutterTiles5x5`;
4. reinitialiser le cache de tuiles Wave 3 et liberer ses textures;
5. reactiver `Step4CContinuousAtlas`;
6. relancer les checks Step4C existants: UV bornes, Clamp, atlas present, HUD fixe et absence de repetition visible;
7. conserver les preuves d'echec Wave 3, sans modifier les PNG source;
8. ne supprimer le nouveau dossier qu'apres verification du chemin absolu et accord Builder-A/QA.

Declencheurs de rollback immediat:

- hash ou dimension incorrecte;
- tuile manquante;
- inversion/rotation/transposee;
- grille ou couture visible a un zoom cible;
- sampling visible des gouttieres;
- repetition par modulo;
- ecran noir lors d'un changement de chunk;
- HUD qui se deplace;
- vol qui saute, se detache ou suit un chemin peint;
- depassement du plafond memoire convenu.

## 11. Validation attendue

Builder-A doit executer `WorldMapWave3_BuilderASelfChecks.md` et produire les preuves associees. Le gate est bloque si un check critique n'est pas PASS. Un resultat `PASS_WITH_RESERVES` n'est acceptable que pour une reserve documentaire ou device explicitement hors integration visuelle; il ne couvre jamais une couture, une orientation fausse, une repetition, un vol terrestre ou un hash incorrect.

## 12. Claims honnetes

- materiel pret pour Builder-A apres gates;
- aucune integration Unity effectuee par Builder-B;
- aucune validation Play Mode, Android ou appareil effectuee dans ce handoff;
- aucune carte live/officielle ou monde immense livre;
- aucun serveur live;
- aucun placement persistant officiel;
- aucune logique de route terrestre.
