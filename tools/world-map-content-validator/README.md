# World Map Art Content Validator

Version: `3.0.0`.

Outil autonome hors Unity pour verifier les images de fond de la carte mondiale Bee Kingdom. Il ne modifie aucun PNG source et ne contient aucune logique serveur, route, pathfinding ou gameplay.

## Dependances

- Python 3.10+
- Pillow
- NumPy

```powershell
python -m pip install -r requirements.txt
python -m unittest discover -s tests -v
```

Les tests fabriquent leurs propres images temporaires. Aucun asset du projet n'est lu ou modifie.

## Sorties

Une execution produit notamment:

- `validation.json`: resultat complet lisible par machine;
- `report.md`: synthese et tableaux QA;
- `reconstruction.png`: reconstruction des slices;
- `contact_sheet.png`: inventaire des tuiles;
- `seam_heatmap.png`: heatmap des frontieres canoniques;
- `top_risks.png`: extraits des risques prioritaires;
- `qa_grid.png`: grille annotee, separee des preuves perceptuelles;
- `runtime_gutters_contact_sheet.png`: controle des tuiles runtime;
- `perceptual_mosaic_100.png`, `73.png`, `50.png`, `25.png`;
- `perceptual_pan_horizontal.png`, `perceptual_pan_vertical.png`;
- `perceptual_contrast_enhanced.png`;
- les gabarits JSON de revue humaine.

Toutes les images `perceptual_*` sont generees sans grille de debug. `qa_grid.png` et `top_risks.png` sont explicitement des aides annotees et ne doivent pas servir a signer le gate perceptuel.

## Profil Wave 3

Le profil `wave3-continuous-5x5` remplace le contrat centre/anneau de Wave 2. Il exige:

- un master macro unique de `2560x2560`;
- 25 slices uniques de `512x512`, sans redimensionnement;
- un manifest avec ID, position, rectangle source, dimensions, SHA-256 et voisins N/E/S/O;
- une reconstruction pixel-identique au master;
- exactement 40 frontieres canoniques toutes PASS;
- 25 tuiles runtime `516x516`, soit 2 pixels de gutter par cote;
- des gutters derives des vrais voisins, avec clamp uniquement au bord externe du master;
- 40 relations de voisinage gutter toutes pixel-identiques;
- une revue de contenu interdit;
- une revue perceptuelle signee par Builder-C;
- `GRID_PATTERN_VISIBLE=NO` pour permettre le PASS de contenu.

Le profil refuse explicitement `--baseline-center` et tout nombre d'anneau. Aucun hash du centre Wave 1 n'est attendu.

Le rerun officiel est bloque tant que le rapport donne a `--readiness-report` ne contient pas exactement:

```text
READY_FOR_WORLD_MAP_ART_WAVE3_VALIDATION=YES
```

Commande future, uniquement apres publication du marker UI-B:

```powershell
python validate_world_map.py `
  --profile wave3-continuous-5x5 `
  --input "C:\projets\beekingdom\worldmap_art_wave5\UIB_ContinuousMaster5x5" `
  --output "C:\projets\beekingdom\worldmap_validation_wave5\uib_continuous_master_5x5" `
  --manifest "C:\projets\beekingdom\worldmap_art_wave5\UIB_ContinuousMaster5x5\manifest.json" `
  --reference-atlas "C:\projets\beekingdom\worldmap_art_wave5\UIB_ContinuousMaster5x5\atlas_master_wave3.png" `
  --gutters-dir "C:\chemin\vers\gutters_runtime_516" `
  --readiness-report "C:\chemin\vers\rapport_UIB_Wave3.md" `
  --forbidden-review "C:\chemin\vers\forbidden_content_review.json" `
  --perceptual-review "C:\chemin\vers\perceptual_continuity_review.json" `
  --label "UI-B Continuous Master Wave 3" `
  --fail-on-warn
```

## Manifest Wave 3

Bloc master minimal:

```json
{
  "master": {
    "file": "atlas_master_wave3.png",
    "sha256": "<sha256>",
    "dimensions": { "width": 2560, "height": 2560 }
  },
  "grid": { "columns": 5, "rows": 5, "expected_count": 25 },
  "tiles": []
}
```

Entree obligatoire pour chacune des 25 slices:

```json
{
  "id": "wave3_x0000_y0000",
  "tile_x": 0,
  "tile_y": 0,
  "file": "wave3_x0000_y0000.png",
  "sha256": "<sha256 slice>",
  "stored_dimensions": { "width": 512, "height": 512 },
  "source_rect": { "x": 0, "y": 0, "width": 512, "height": 512 },
  "neighbors": { "n": null, "e": "wave3_x0001_y0000", "s": "wave3_x0000_y0001", "w": null },
  "runtime_gutter": {
    "file": "wave3_x0000_y0000_gutter.png",
    "sha256": "<sha256 gutter>",
    "dimensions": { "width": 516, "height": 516 }
  }
}
```

Les IDs, fichiers et hashes de slices doivent etre uniques. Le hash du gutter est aussi obligatoire.

## Gate perceptuel

L'automatisation signale:

- lignes globales et ruptures basse frequence;
- damier de cellules apres suppression d'une tendance globale;
- bandes floues autour des limites;
- copies exactes, quasi-copies et miroirs;
- frontieres techniques a risque.

Elle ne peut jamais signer un PASS humain. Le formulaire doit couvrir grille, carre, anneau, damier, bandes floues, miroirs, repetitions et continuite des rivieres, reliefs, forets et biomes. Toute categorie `YES`, `UNCERTAIN` ou `NOT_REVIEWED` bloque le PASS. La signature exige un relecteur, le role `Builder-C`, une date UTC et `decision=PASS`.

## Contenu interdit

La detection semantique n'est pas revendiquee comme automatique. Le formulaire humain liste separement:

- route ou piste dominante;
- ruche ou structure joueur;
- ressource runtime;
- troupe ou essaim;
- trajectoire de vol peinte;
- UI, texte ou badge;
- frontiere de tuile peinte.

Les soupcons sont consignes pour revue. Ils ne doivent pas etre inventes a partir d'un score pixel.

## Profil Wave 2 historique

`wave2-5x5` reste disponible pour reproduire les preuves deja etablies: 25 tuiles, anneau 16, hash-lock du centre 3x3, 40 coutures et reconstruction de reference. Les tests de non-regression Wave 2 restent actifs.

## Codes retour

- `0`: PASS, ou WARN sans `--fail-on-warn`;
- `1`: WARN avec `--fail-on-warn`;
- `2`: FAIL ou erreur fatale.

Les seuils sont versionnes dans `thresholds.default.json`. Un score automatique est une aide au triage, jamais une approbation artistique.
