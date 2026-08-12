# Builder-B — World Map Wave 3 — Macro Master Slicer and Runtime Gutter Pipeline

Date: 2026-07-14  
Périmètre: pipeline artistique local hors Unity

## Résultat

Le pipeline déterministe pour un futur macro master UI-B 2560 x 2560 est implémenté, documenté et validé par tests synthétiques.

Il produit:

- 25 PNG canoniques lossless de 512 x 512, dans l'ordre strict `R0C0..R4C4`;
- un manifeste canonique versionné avec dimensions, coordonnées et hashes PNG/pixels;
- une reconstruction canonique contrôlée pixel par pixel;
- 25 PNG runtime de 516 x 516 avec 2 pixels de gouttière sur chaque côté;
- un manifeste runtime avec crop intérieur, origine macro, fenêtre source, UV, clamp, provenance des gouttières et hash source;
- une validation machine-readable refusant les lots incomplets ou altérés.

Le master UI-B Wave 3 n'était pas disponible et n'a pas été simulé comme livré. Le verdict d'ingest signifie que l'outil est prêt à le recevoir sans réécriture.

## Décision Architecte appliquée

La décision `Architect_WorldMap5x5ContinuousMasterResetDecision.md` ferme l'extension par anneau et impose une composition continue avant découpe. Le pipeline applique ce contrat:

- master unique 2560 x 2560;
- grille fixe 5 x 5;
- aucune dépendance au pilote 3 x 3;
- aucune exigence de hash legacy;
- aucune route, entité runtime ou trajectoire peinte par le pipeline;
- découpe après création du master seulement.

## Fichiers livrés

Racine:

`C:\projets\beekingdomgame-master\tools\world-map-macro-slicer\`

- `macro_slicer.py`: point d'entrée CLI;
- `worldmap_macro_slicer/cli.py`: commandes `slice` et `verify`, codes retour et verdicts;
- `worldmap_macro_slicer/core.py`: découpe, manifests, reconstruction et vérification;
- `tests/synthetic_master.py`: fixture 2560² déterministe;
- `tests/test_macro_slicer.py`: huit tests automatisés;
- `tests/generate_proof.py`: générateur de preuve compacte run1/run2;
- `README.md`: contrat, utilisation, structure et non-claims;
- `requirements.txt`: Pillow et NumPy;
- `proofs/automated_tests.txt`: journal des tests;
- `proofs/synthetic_proof_summary.json`: synthèse déterministe;
- `proofs/synthetic_canonical_manifest_snapshot.json`: manifeste canonique de preuve;
- `proofs/synthetic_runtime_manifest_snapshot.json`: manifeste runtime de preuve;
- `proofs/synthetic_validation_snapshot.json`: validation de preuve.

## CLI

Découpe:

```powershell
python macro_slicer.py slice `
  --input "C:\chemin\master_2560.png" `
  --output "C:\chemin\bundle_wave3_run1" `
  --version "uib-wave3-v1"
```

Vérification sans modification:

```powershell
python macro_slicer.py verify `
  --input "C:\chemin\master_2560.png" `
  --bundle "C:\chemin\bundle_wave3_run1" `
  --json
```

Entrée refusée si:

- fichier absent ou illisible;
- format différent de PNG;
- dimensions différentes de 2560 x 2560;
- mode différent de RGB/RGBA;
- dossier de sortie non vide;
- version contenant des caractères non autorisés.

## Bundle canonique

Structure:

```text
canonical/
  manifest.canonical.json
  reconstruction.png
  tiles/
    R0C0.png ... R4C4.png
```

Chaque tuile possède:

- `id`, `row`, `column`, `order_index`;
- crop macro `{x, y, width: 512, height: 512}`;
- dimensions stockées;
- chemin relatif;
- SHA-256 du PNG;
- SHA-256 des pixels bruts.

La reconstruction est produite en relisant les 25 PNG enregistrés, pas en recopiant directement le tableau source en mémoire. Le contrôle compte les pixels différents et recalcule ses hashes.

Résultat synthétique:

- tuiles: 25/25;
- pixels différents dans la reconstruction assemblée: `0`;
- pixels différents dans `reconstruction.png`: `0`;
- altérations de tuiles: `0`.

## Bundle runtime et gouttières

Structure:

```text
runtime/
  manifest.runtime.json
  tiles/
    R0C0_g2.png ... R4C4_g2.png
```

Chaque texture mesure 516 x 516:

- intérieur: rectangle `{x: 2, y: 2, width: 512, height: 512}`;
- UV intérieur: `2/516` à `514/516`;
- fenêtre source non clampée: origine macro moins 2 pixels et taille 516;
- source de chaque côté: `true_master_neighbor_pixels` ou `outer_edge_clamp`;
- clamp explicite par côté;
- hash du master source, du PNG et des pixels runtime.

Exemple `R2C2`:

- origine macro: `(1024, 1024)`;
- fenêtre source: `(1022, 1022, 516, 516)`;
- clamp haut/bas/gauche/droite: `0/0/0/0`;
- les quatre côtés proviennent de vrais pixels voisins du master.

Exemple `R0C0`:

- fenêtre théorique: `(-2, -2, 516, 516)`;
- clamp haut: 2;
- clamp gauche: 2;
- aucun clamp bas ou droite.

Le calcul utilise des coordonnées master clampées uniquement aux limites 0 et 2559. Pour toute frontière interne, les deux colonnes/lignes de gouttière sont des coordonnées source distinctes. Il n'existe ni stretch, ni extrapolation, ni duplication artificielle de ligne interne.

Résultat synthétique:

- frontières internes attendues: 40;
- frontières contrôlées: 40;
- frontières PASS: 40;
- pixels de gouttière différents de la source attendue: `0`;
- pixels intérieurs runtime différents: `0`.

## Vérificateur et refus

Le mode `verify` détecte et refuse:

- source modifiée ou contrat source incohérent;
- schéma, grille, `tile_count`, ordre ou gutter global incohérents;
- tuile ou entrée de manifeste manquante;
- PNG supplémentaire;
- ID dupliqué;
- doublon de fichier ou de pixels;
- hash PNG ou pixel incohérent;
- crop, dimensions, mode, origine, UV, clamp ou provenance incorrects;
- pixels canoniques altérés;
- reconstruction altérée;
- intérieur ou gutter runtime altéré;
- frontière interne non prouvée.

Les tests négatifs ont effectivement injecté puis détecté:

1. une tuile manquante;
2. un doublon exact;
3. un hash manifeste faux;
4. un ordre de tuiles inversé;
5. un contrat `stretching` interdit;
6. une altération de pixel dans une gouttière;
7. une altération de pixel dans l'intérieur runtime.

Après restauration des fixtures, le bundle repasse PASS.

## Tests synthétiques

La fixture combine sur tout le master:

- des gradients globaux continus;
- une famille périodique de rivières diagonales;
- un relief haute fréquence.

Les masques de rivière et de relief sont vérifiés sur chaque frontière verticale et horizontale de la grille. Les 40 frontières internes sont exercées.

Commande exécutée:

```powershell
python -m unittest discover -s tests -v
```

Résultat réel:

- 8 tests exécutés;
- 8 PASS;
- durée: 31.862 s;
- code retour: 0.

Le journal complet est `proofs/automated_tests.txt`.

## Déterminisme run1/run2

Le générateur de preuve crée un master synthétique, exécute deux découpes indépendantes et compare le SHA-256 de chaque fichier relatif.

Résultat:

- artefacts par run: 54;
- fichiers différents: 0;
- run1/run2 byte-identiques: `true`;
- hash PNG du master synthétique: `bca92c45f47fb680723c845928fca26cc899b808c6e7c18515eb58978634e639`;
- digest d'arbre run1: `9a1daeffd7fef148989299e9947c47d1e49a4dbc71c0f4fb09d91339d87d9db0`;
- digest d'arbre run2: `9a1daeffd7fef148989299e9947c47d1e49a4dbc71c0f4fb09d91339d87d9db0`.

Les 54 artefacts sont:

- 25 PNG canoniques;
- 1 reconstruction canonique;
- 1 manifeste canonique;
- 25 PNG runtime;
- 1 manifeste runtime;
- 1 validation JSON.

La preuve compacte est `proofs/synthetic_proof_summary.json`.

## Handoff futur UI-B

Le chemin du master n'est pas codé en dur. À sa livraison, le pipeline peut être lancé directement avec le chemin réel:

```powershell
python macro_slicer.py slice `
  --input "<master-ui-b-2560x2560.png>" `
  --output "<bundle-run1-vide>" `
  --version "uib-wave3-v1"
```

Une seconde sortie vide avec la même version permet de reproduire le contrôle byte-identique run1/run2. Le mode `verify` doit ensuite être exécuté avant tout handoff Builder-C ou intégration Unity future.

## Non-claims et périmètre

- Travail local/hors Unity uniquement.
- Aucun fichier UI-B lu ou modifié.
- Aucun fichier `Assets`, `Packages`, `ProjectSettings` ou scène modifié.
- Aucun réseau, serveur ou endpoint.
- Aucune logique de route ou pathfinding.
- Aucune carte immense/live revendiquée.
- Aucune intégration runtime officielle revendiquée.
- Aucun master UI-B réel déclaré validé avant sa livraison.

## Placement du rapport

Le chemin demandé était:

`C:\projets\beekingdom\prompts_codex\rapports\BuilderB_WorldMapMacroMasterSlicerWave3_Report.md`

L'environnement de cette session autorise la lecture de ce volume, mais pas son écriture. Le rapport complet est donc livré ici, dans le dossier exclusif autorisé:

`C:\projets\beekingdomgame-master\tools\world-map-macro-slicer\BuilderB_WorldMapMacroMasterSlicerWave3_Report.md`

Cette restriction de placement ne bloque pas le pipeline ni l'ingest futur du master.

## Verdicts finaux

`WORLD_MAP_MACRO_SLICER_WAVE3 = PASS`

`CANONICAL_RECONSTRUCTION_PIXEL_IDENTICAL = YES`

`RUNTIME_GUTTERS_FROM_TRUE_NEIGHBORS = YES`

`READY_FOR_UIB_WAVE3_MASTER_INGEST = YES`
