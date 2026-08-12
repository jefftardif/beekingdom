# World Map Macro Master Slicer — Wave 3

Pipeline autonome hors Unity pour transformer un macro master continu Bee Kingdom en deux lots déterministes:

- 25 tuiles canoniques lossless de 512 x 512, ordre strict `R0C0` à `R4C4`;
- 25 tuiles runtime de 516 x 516 avec une gouttière de 2 pixels sur chaque côté.

Le pipeline n'utilise ni réseau, ni Unity, ni serveur, ni asset UI-B préexistant. Il ne contient aucune logique de routes ou de déplacement.

## Prérequis

- Python 3.10+;
- Pillow;
- NumPy.

```powershell
python -m pip install -r requirements.txt
```

## Entrée contractuelle

Le master doit être:

- un PNG lisible;
- exactement 2560 x 2560;
- en mode RGB ou RGBA.

Tout autre format, dimension ou mode est refusé avant la création du bundle.

## Découpe

```powershell
python macro_slicer.py slice `
  --input "C:\chemin\master_2560.png" `
  --output "C:\chemin\bundle_wave3_run1" `
  --version "uib-wave3-v1"
```

Le dossier de sortie doit être absent ou vide. Cela évite de mélanger deux générations ou de masquer une tuile résiduelle.

Structure produite:

```text
bundle/
  canonical/
    manifest.canonical.json
    reconstruction.png
    tiles/R0C0.png ... R4C4.png
  runtime/
    manifest.runtime.json
    tiles/R0C0_g2.png ... R4C4_g2.png
  validation.json
```

Le manifeste et la validation n'incluent ni horodatage ni chemin de sortie absolu. Avec le même fichier source, la même version de lot et le même environnement Pillow, deux exécutions donnent des artefacts byte-identiques.

## Gouttières runtime

Pour une tuile intérieure, les 2 pixels de chaque côté sont lus directement dans les coordonnées voisines du macro master. Il n'y a ni étirement, ni extrapolation, ni duplication de ligne interne.

Exemple pour le bord gauche d'une tuile commençant à `x=1024`:

- gouttière gauche: colonnes master `1022` et `1023`;
- intérieur: colonnes `1024` à `1535`;
- gouttière droite: colonnes `1536` et `1537`.

Le clamp est appliqué uniquement aux limites externes du master:

- `R0*`: clamp haut de 2 pixels;
- `R4*`: clamp bas de 2 pixels;
- `*C0`: clamp gauche de 2 pixels;
- `*C4`: clamp droite de 2 pixels.

Le manifeste runtime décrit pour chaque tuile le crop canonique, l'origine macro, la fenêtre source non clampée, les côtés clampés, la provenance de chaque gouttière, le rectangle intérieur, les UV normalisés et les hashes source/PNG/pixels.

Les UV de l'intérieur sont définis sur les limites de pixels de la texture runtime:

- minimum: `2 / 516`;
- maximum: `514 / 516`.

Le flip vertical éventuel appartient à l'adaptateur runtime futur; ce pipeline reste indépendant de Unity.

## Vérification d'un bundle

```powershell
python macro_slicer.py verify `
  --input "C:\chemin\master_2560.png" `
  --bundle "C:\chemin\bundle_wave3_run1" `
  --json
```

Le vérificateur refuse notamment:

- master modifié ou hash source incohérent;
- manifeste absent, schéma invalide ou ordre différent de `R0C0..R4C4`;
- nombre de tuiles différent de 25;
- fichier manquant ou supplémentaire;
- identifiant ou contenu dupliqué;
- dimensions, mode, crop, origine, UV ou clamp incohérents;
- hash PNG ou pixel incohérent;
- tuile canonique altérée;
- reconstruction non pixel-identique;
- intérieur runtime altéré;
- gouttière ne provenant pas des pixels attendus du master;
- frontière interne manquante parmi les 40 attendues.

## Tests synthétiques

```powershell
python -m unittest discover -s tests -v
```

Les tests génèrent eux-mêmes un master 2560² combinant:

- gradients globaux;
- famille de rivières diagonales périodiques;
- relief haute fréquence.

Ces trois signaux traversent la grille et les 40 frontières internes. La suite contrôle la reconstruction, les gouttières, le clamp externe, les manifests, RGB/RGBA, le déterminisme run1/run2 et les refus de fichiers manquants, doublons, hashes incohérents, ordre faux et pixels altérés.

Preuve compacte reproductible:

```powershell
python tests/generate_proof.py --output proofs
```

## Commande prête pour le futur master UI-B

```powershell
python macro_slicer.py slice `
  --input "C:\projets\beekingdom\worldmap_art_wave5\UIB_MacroMasterWave3\master_2560.png" `
  --output "C:\projets\beekingdom\worldmap_macro_wave3\run1" `
  --version "uib-wave3-v1"
```

Le nom de dossier UI-B ci-dessus est une convention de handoff proposée; le CLI n'en dépend pas et ne nécessitera aucune réécriture si le chemin final diffère.

## Ingest réel avec double exécution

`real_ingest.py` ajoute le gate UI-B, deux générations depuis zéro, la comparaison aux tuiles autoritatives, les injections négatives avec restauration et le résumé machine-readable:

```powershell
python real_ingest.py `
  --ui-dir "C:\projets\beekingdom\worldmap_art_wave5\UIB_ContinuousMaster5x5" `
  --staging-root "C:\projets\beekingdomgame-master\artifacts\WorldMapWave3_RuntimeBundle_staging" `
  --expected-hash "D3CDC2DDE9D56CAC58BE6833790B6FD8FC38AC157F72A01DCEBD8117583A95B4" `
  --version "uib-wave3-continuous-v1"
```

Les dossiers `run1` et `run2` doivent être absents ou vides. Le script lit le dossier UI-B sans jamais le modifier.

## Non-claims

Le bundle décrit un pipeline local de préparation artistique. Il ne prouve pas une carte mondiale immense, un streaming Unity, une intégration officielle, un serveur live, une économie persistante ou des déplacements officiels.
