# QA-A - World Map Wave4 15x15 HD - Validation art independante finale

Date: 2026-07-14  
Mode: lecture seule, hors Unity  
Perimetre: paquet artistique local Wave4 15x15 uniquement  
Verdict: **PASS**

## 1. Decision

Le master Wave4 15x15 est accepte pour assignation a une future integration Unity distincte.

La validation independante confirme:

- paquet verrouille et hashes conformes;
- 225 tuiles presentes, uniques et exactes;
- reconstruction pixel-identique au master;
- 420/420 voisinages valides;
- aucune couture, grille, repetition ou miroir artificiel visible;
- detail natif et continuite perceptuelle acceptables;
- aucun element runtime peint dans le fond;
- integrite du paquet identique avant et apres QA.

Ce PASS valide uniquement l'art source local. Il ne valide aucune integration Unity, aucun runtime, appareil, APK, serveur, monde live ou fonctionnalite MMO.

## 2. Sources controlees

Source de verite:

`C:\projets\beekingdomgame-master\artifacts\UIB_ImmenseContinuousMaster15x15_staging\master_15x15_7680.png`

Lock:

`C:\projets\beekingdomgame-master\artifacts\UIB_ImmenseContinuousMaster15x15_staging\MASTER_WAVE4_15X15_LOCK.json`

Preuves recoupees:

- `C:\projets\beekingdom\prompt_demo\rapports\DEMO-B_WorldMapImmenseContinuousMasterWave4_15x15\DEMO-B_Report.md`
- `C:\projets\beekingdom\prompts_codex\rapports\BuilderC_WorldMapImmenseContinuousMasterWave4_15x15_Validation_Report.md`
- `C:\projets\beekingdomgame-master\Docs\UIA\WorldMapImmenseWave4ArtDirection\HDReviewRestart\UIA_WorldMapImmenseWave4_HD_FinalReadOnlyCounterReview.md`

QA n'a pas ouvert Unity et n'a modifie, recompresse ou remplace aucun PNG source.

## 3. Provenance et integrite

| Controle | Resultat independant | Verdict |
|---|---:|---|
| Master | 7680 x 7680, RGB | PASS |
| Taille master | 101 035 134 octets | PASS |
| SHA-256 master | `7E8D44D4BCB346DE386B314E6B9B843D3C3DEE1B80BC045477DA65A4C5F5498D` | PASS |
| SHA-256 lock | `E0491D02489E61597B73D1214E943E85E82398CF4844E191EA6D3F5DEBB0B74D` | PASS |
| Etat lock | `FROZEN`, mutation pixel interdite | PASS |
| SHA-256 rapport Demo-B | `0D245CE9CC92B9F0FD37C85F2BB2943851DFF360B17865D7E626D50224781BFA` | PASS |
| Fichiers du paquet avant/apres | 280 / 280 | PASS |
| Octets du paquet avant/apres | 649 612 915 / 649 612 915 | PASS |
| Empreinte agregee avant/apres | `16B823612A2C552B19CB67D6A9131B3A4EA18BD6138148474F71E28B5203E9D7` | PASS |

L'integrite source est strictement inchangee apres les controles QA.

## 4. Recalcul mecanique independant

Le validateur local a ete rejoue hors Unity avec sorties temporaires separees du paquet source. Il termine avec code 0.

| Critere | Resultat | Verdict |
|---|---:|---|
| Tuiles attendues/presentes | 225/225 | PASS |
| Dimensions et mode | 225 tuiles en 512 x 512 RGB | PASS |
| Hashes de tuiles uniques | 225/225 | PASS |
| Hashes conformes au manifest | 225/225 | PASS |
| Tuiles egales aux crops du master | 225/225 | PASS |
| Reconstruction vs master | 0 pixel different, delta max 0 | PASS |
| Trous | 0 | PASS |
| Recouvrements | 0 | PASS |
| Voisinages horizontaux | 210/210 | PASS |
| Voisinages verticaux | 210/210 | PASS |
| Total voisinages | 420/420 | PASS |
| Paires comparees | 25 200 | PASS |
| Quasi-doublons | 0 | PASS |
| Miroirs suspects | 0 | PASS |
| Motif de grille automatique | non suspect | PASS |

Les cinq alertes generiques de l'oracle producteur ont ete contre-inspectees visuellement:

- `R05C01--S--R06C01`;
- `R13C01--E--R13C02`;
- `R01C04--E--R01C05`;
- `R00C09--S--R01C09`;
- `R09C11--S--R10C11`.

Elles correspondent a des variations naturelles de terrain, vegetation, relief ou lumiere. Aucune ligne de couture ni rupture de feature n'y est visible.

## 5. Inspection visuelle independante

L'inspection a porte sur les images reelles, a leur echelle utile, et non sur les seuls rapports ou manifests.

| Preuve inspectee | Couverture | Constat |
|---|---:|---|
| Pages natives a 100 % | 9/9 pages de 2560 x 2560 | Detail net; aucune couture, grille, bande ou zone floue rectangulaire |
| Vue a 63,7 % | 4/4 quadrants | Monde continu; biomes et eclairage coherents |
| Vue a 50 % | master complet | Composition globale lisible; aucune couronne, carre central ou repetition |
| Vues a 25 % et 12,5 % | master complet | Silhouette territoriale continue; aucune grille ou bande d'exposition |
| Crops natifs | 12/12 | Roches, neige, arbres, eau, fleurs et cristaux conservent le detail |
| Paires de jonction | 4/4 | Features traversantes continues, sans ligne ni saut de luminosite |
| Pans | 14/14 | 5 horizontaux, 5 verticaux et 4 diagonaux sans couture ni phase repetee |

### Continuite artistique

- Hydrologie: cours d'eau, lacs, zones humides et delta restent continus.
- Reliefs: transitions montagne, roche, plaine et rive plausibles.
- Forets: densite et lisiere evoluent sans decoupe rectangulaire.
- Biomes: nord alpin, ouest boise et humide, prairie centrale, est cristallin et sud delta forment un ensemble coherent.
- Lumiere: aucune bande, rupture d'exposition ou halo de jointure detecte.
- Prairie centrale: grande zone respirante conservee et visuellement exploitable pour de futurs placements runtime.
- Cristaux: formations integrees au terrain, variees et sans duplication artificielle perceptible.

### Elements interdits

Aucun des elements suivants n'est peint dans le master ou les tuiles:

- route terrestre ou logique de chemin au sol;
- UI, texte, grille debug ou annotation QA;
- ruche, ressource collectable, troupe, vol, ennemi ou marqueur runtime;
- stamp, miroir, halo, anneau, couronne ou carre central artificiel.

## 6. Garde-fous pour l'integration future

Ces points ne sont pas des reserves sur l'art livre; ils deviennent des contraintes de la future integration Unity:

1. Les silhouettes cristallines du terrain ne doivent pas etre reutilisees telles quelles comme marqueurs runtime, afin d'eviter toute confusion joueur.
2. La prairie centrale doit rester lisible et ne pas etre saturee par des overlays ou entites.
3. L'integration doit conserver les tuiles sans recompression destructive, repetition, modulo, couture ou grille.
4. Les entites, routes de vol, HUD et interactions devront rester des overlays runtime separes.

## 7. Conclusion

Le paquet artistique Wave4 15x15 HD satisfait les controles de provenance, reconstruction, voisinage, detail natif, anti-duplication et continuite perceptuelle. Aucun defaut P0/P1 n'exige une reprise artistique globale.

L'assignation d'une integration Unity peut commencer comme gate ulterieur separe. Cette decision n'affirme pas que cette integration existe deja et n'autorise aucun claim runtime, appareil, APK, serveur ou live.

QA_ART_PACKAGE_GATE=PASS

QA_ART_RECONSTRUCTION_GATE=PASS

QA_ART_BOUNDARY_GATE=PASS

QA_ART_NATIVE_DETAIL_GATE=PASS

QA_ART_PERCEPTUAL_CONTINUITY=PASS

QA_ART_VISIBLE_SEAMS=NO

QA_ART_GRID_PATTERN_VISIBLE=NO

QA_ART_RUNTIME_ELEMENTS_PAINTED=NO

QA_ART_WAVE4_15X15=PASS

READY_FOR_WAVE4_15X15_UNITY_INTEGRATION_ASSIGNMENT=YES
