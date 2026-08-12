# Step5A - Import Unity futur et rollback

## 1. Preflight obligatoire

Builder-A doit executer le validateur avant toute copie:

```powershell
& 'C:\Users\Utilisateur\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' `
  'Docs\BuilderB\WorldMapStep5AImportPayload\build_step5a_import_payload.py' verify
```

Exiger:

- `status = PASS`;
- `checks_passed = 30`;
- `checks_failed = 0`;
- `tile_count = 25`;
- `total_inner_mismatch_pixels = 0`;
- `total_runtime_expected_mismatch_pixels = 0`;
- `copied_hash_mismatch_count = 0`;
- tree SHA-256 `377ca038ad9364cd194d49319f5ddd45136b43ae613d6a542b6548948d21b823`.

Le validateur recalcule les pixels complets `516x516` depuis la reconstruction canonique. Il valide donc simultanement l'interieur `512x512`, les gouttieres de 2 pixels, les coins et les clamps externes.

## 2. Inventaire source vers destination future

Source:

`artifacts/WorldMapWave3_UnityImportPayload_staging/tiles/`

Destination cible d'integration (son existence et son contenu relevent de Builder-A):

`Assets/BeeKingdom/Playground/Resources/WorldMapWave3Runtime/UIB_ContinuousMaster5x5_v1/`

Le fichier `source-to-future-destination.csv` contient les 25 lignes exactes avec ordre, ID, ligne, colonne, hashes, chunk monde et UV. Builder-B n'a cree, remplace ou copie aucun fichier dans cette destination.

Mapping:

- `R0C0 -> chunk (30,30)`;
- `R2C2 -> chunk (32,32)`;
- `R4C4 -> chunk (34,34)`;
- ligne croissante vers le bas et `world.y +` du renderer IMGUI;
- colonne croissante vers la droite et `world.x +`;
- rotation 0, aucune transposee, aucun flip;
- ordre row-major `R0C0..R4C4`.

Le manifest `source.handoff.unity.json` est la source future de `manifest.runtime.unity.json`. Builder-B n'a effectue aucune de ces copies.

## 3. Contrat pixels et UV

Chaque PNG payload:

- dimensions: `516x516`;
- mode: `RGB`;
- contenu canonique: `x=2, y=2, width=512, height=512`;
- gouttiere: `2 px` sur chaque cote;
- UV: `2/516..514/516` sur U et V;
- gouttieres visibles comme contenu: interdit;
- gouttieres utilisees par le filtre bilineaire: requis.

Le preflight a valide:

- 25 noms uniques;
- 25 coordonnees uniques;
- 25 SHA PNG uniques;
- 25 SHA pixels uniques;
- 40 frontieres internes;
- 80 cotes internes diriges issus des vrais voisins;
- 20 cotes externes avec clamp;
- zero pixel different.

## 4. Reglages import Editor

| Reglage | Valeur |
|---|---|
| Texture Type | `Default` |
| Texture Shape | `2D` |
| sRGB | `On` |
| Alpha Source | `None` |
| Alpha Is Transparency | `Off` |
| Read/Write | `Off` |
| Non-Power of 2 | `None` |
| Wrap U/V/W | `Clamp` |
| Filter Mode | `Bilinear` |
| Aniso Level | `1` |
| Generate Mip Maps | `Off` |
| Streaming Mipmaps | `Off` |
| Max Size | `1024` |
| Compression premiere preuve | `None / RGB24` |

La premiere validation Unity doit rester non compressee pour verifier orientation, raccords et absence de grille. `Repeat` est interdit.

## 5. Reglages Android

Profil principal:

- override Android active;
- `ASTC 6x6 RGB`;
- qualite `Best`;
- Crunch `Off`;
- mipmaps `Off`;
- max size `1024`;
- NPOT conserve a `516x516`.

Fallback de compatibilite seulement si le profil appareils l'exige:

- `ETC2 RGB4`;
- Crunch `Off`;
- autres reglages identiques.

Apres compression, refaire les preuves de raccords aux zooms `0.85`, `1.10`, `1.35`, en paysage et portrait.

## 6. Budget memoire

Pour 25 textures `516x516`, hors metadata Unity/driver:

| Representation | Budget |
|---|---:|
| PNG payload sur disque | `11.2539 MiB` |
| RGB24 brut | `19.0441 MiB` |
| RGBA32 de reference | `25.3922 MiB` |
| ASTC 6x6 RGB | `2.8214 MiB` |
| ETC2 RGB4 | `3.1740 MiB` |

Politique runtime:

- 25 textures maximum en regime stable;
- 30 maximum pendant le remplacement d'une bande de cinq;
- `Read/Write Off` pour eviter une copie CPU brute;
- aucune residence du monde logique complet `64x64`;
- aucune duplication des 25 textures pour remplir le monde;
- hors macro 5x5: missing/fallback explicite, jamais modulo.

## 7. Integration attendue

1. Corriger et valider d'abord le bug de terrain statique.
2. Rejouer le preflight payload.
3. Copier exactement les 25 PNG selon le CSV.
4. Importer avec le profil Editor non compresse.
5. Utiliser un provider Wave3 distinct, derriere un flag reversible.
6. Ne pas renseigner `AtlasTexture` dans ce mode si le bootstrap donne priorite a Step4C.
7. Indexer par chunk borne `(30..34,30..34)` sans modulo.
8. Dessiner `512x512` unites monde avec les UV interieures.
9. Garder entites et vols en coordonnees monde.
10. Garder HUD/panneaux fixes et grille debug desactivee.
11. Verifier les 40 raccords et les cinq reperes d'orientation.
12. Appliquer ensuite le profil Android et rejouer les preuves.

Les chemins peints du decor ne doivent jamais alimenter les vols. Les abeilles restent sur des arcs/trajectoires aeriennes independantes.

## 8. Rollback

Le lot Step4C actuel doit rester intact pendant l'integration.

Si un hash, une orientation, un raccord, une UV, un import ou une performance echoue:

1. desactiver le mode `Wave3RuntimeGutterTiles5x5`;
2. liberer le cache Wave3;
3. reactiver le provider `Step4CContinuousAtlas`;
4. conserver le payload staging immuable pour analyse;
5. ne modifier aucun PNG du payload;
6. ne supprimer aucun build ou asset precedent;
7. produire une nouvelle version de payload seulement depuis une source autoritative corrigee;
8. ne jamais masquer l'echec par Repeat, duplication ou etirement.

Rollback immediat si:

- une tuile manque ou change de hash;
- un gutter devient visible comme contenu;
- une grille/couture apparait;
- un flip/rotation/transposee apparait;
- le terrain reste fixe pendant le pan;
- les marqueurs ou vols se decaleraient;
- une route au sol guide un vol;
- une charge globale 64x64 est introduite.

## 9. Claims honnetes

- payload hors Unity pret pour Builder-A;
- integration Unity non effectuee par Builder-B;
- aucune validation Play Mode/Android dans ce lot;
- aucune copie sous `Assets`;
- aucun monde immense/live livre;
- aucun serveur live.
