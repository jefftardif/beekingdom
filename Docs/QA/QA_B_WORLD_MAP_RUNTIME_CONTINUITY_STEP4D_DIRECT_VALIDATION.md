# QA-B - World Map Runtime Continuity Step4D - Validation directe sur preuve preservee

Date: 2026-07-14  
Role: QA-B, validation independante en lecture seule  
Portee: preuve locale Unity Editor, surface artistique macro 3x3 Step4D

## Verdict de synthese

La preuve manuelle preservee est coherente, complete et integre. Les sept PNG ont ete inspectes a leur resolution native et les sept manifestes ont ete parses independamment. Aucun blocker produit P0/P1 n'est releve.

Verdict QA-B: **PASS_WITH_RESERVES**.

Les reserves portent sur la dette P2 de la file automatique, sur l'absence de preuve video des instants intermediaires du pan et sur l'absence revendiquee de test appareil physique. Elles n'invalident pas les sept etats manuels exacts.

## Sources inspectees

- `Docs/QA/Architect_WorldMapRuntimeContinuityStep4D_DirectProof/Architect_WorldMapRuntimeContinuityStep4D_DirectProof.md`
- `Docs/QA/Architect_WorldMapRuntimeContinuityStep4D_DirectProof/SHA256SUMS.txt`
- les sept PNG et sept manifestes JSON du meme dossier;
- `C:/projets/beekingdom/prompts_codex/rapports/BuilderA_WorldMapRuntimeContinuityStep4D_ProofControls_Report.md`;
- `C:/projets/beekingdom/QA/ARCHITECT_WORLD_MAP_RUNTIME_CONTINUITY_STEP4D_DIRECT_PROOF.md`;
- baseline visuelle `DEMO096_01_Blocking_Landscape_1920x1080_Zoom110_GameView.png`, uniquement pour comparaison du defaut historique.

Unity n'a pas ete rouvert pour cette validation. L'archive Architecte et les sources producteur ont ete traitees en lecture seule. Un controle complementaire d'empreintes sur les 2 358 fichiers de `Assets` et `ProjectSettings` releve zero delta.

## Integrite de l'archive

- Entrees attendues dans `SHA256SUMS.txt`: **14**.
- PNG: **7/7 presents**, JSON: **7/7 presents**.
- SHA-256 recalcules: **14/14 concordants**, aucun absent, aucun mismatch.
- Appariement par prefixe temporel: **7/7 paires uniques**.
- Hash atlas declare par les sept manifestes: `533DAD1BBAA138FA12880D44BD5E4DA22F41F564524C87AE512D1F030E4154BD`.
- Hash recalcule du fichier source `atlas_master_1536.png`: identique.

## Matrice des etats exacts

| Prefixe UTC | Pixels PNG | Resolution manifeste | Zoom | Chunk | Centre monde | UV | Resultat |
| --- | --- | --- | ---: | --- | --- | --- | --- |
| `20260714_105400` | 720x1280 | 720x1280 | 1.10 | C32_32 | 16640,16640 | 0.258,0.068,0.748,0.938 | PASS |
| `20260714_105615` | 1920x1080 | 1920x1080 | 0.85 | C32_32 | 16640,16640 | 0,0.222,1,0.784 | PASS |
| `20260714_105620` | 1920x1080 | 1920x1080 | 1.10 | C32_32 | 16640,16640 | 0.068,0.258,0.938,0.748 | PASS |
| `20260714_105625` | 1920x1080 | 1920x1080 | 1.35 | C32_32 | 16640,16640 | 0.133,0.295,0.873,0.711 | PASS |
| `20260714_105629` | 1920x1080 | 1920x1080 | 1.10 | C32_32 | 16640,16640 | 0.068,0.258,0.938,0.748 | PASS |
| `20260714_105634` | 1920x1080 | 1920x1080 | 1.10 | C35_32 | 18176,16640 | 0.085,0.258,0.955,0.748 | PASS |
| `20260714_105639` | 1920x1080 | 1920x1080 | 1.10 | C36_32 | 18688,16640 | 0.09,0.258,0.96,0.748 | PASS |

Pour chaque manifeste: resolution attendue = resolution reelle = dimensions PNG; `expected_resolution_match=true`; 25 chunks actifs; quatre composantes UV effectivement dans `[0,1]`; `uv_bounded=true`; atlas charge; `atlas_wrap_mode=Clamp`.

La sequence temporelle et spatiale C32_32 -> C35_32 -> C36_32 est exacte. Les centres monde et les UV progressent dans le meme sens; le terrain se deplace visiblement entre les trois captures et n'est pas fige.

## Inspection visuelle independante

- z0.85 paysage: surface continue sur toute la largeur, sans grille, bande, trou ni repetition laterale.
- z1.10 paysage: aucune rupture verticale eau/cristaux vers x~430 et aucune repetition equivalente au bord droit.
- z1.35 paysage: detail net et continu, sans flou de camouflage ni raccord brutal.
- portrait 720x1280: cadrage reel conforme, terrain continu, HUD adapte, aucun element produit etire par une fausse resolution.
- pan C32/C35/C36: deplacement progressif du meme terrain; aucune apparition de bord d'atlas, Repeat, bande noire ou texture figee.
- Aucun des sept frames ne montre de limite de tuile/chunk, grille, overlap, trou, flash capture, bande ou overlay de masquage.

La baseline DEMO-096 presente une bande verticale nette: le bloc eau/cristaux s'arrete brutalement contre la prairie et se repete a droite. Ce motif n'est reproduit dans aucun PNG Step4D, notamment dans les deux etats C32_32 a z1.10.

Les PNG fixes ne peuvent pas, a eux seuls, exclure un flash d'une seule frame entre deux captures. Aucun artefact de ce type n'est present dans les etats archives; cette limite de preuve temporelle reste une reserve, pas une contradiction produit.

## Overlays et vols

L'inspection montre ruches, ressources, selections, HUD, minimap et arcs de vol comme couches graphiques distinctes du terrain. Le code corrobore cet ordre: terrain puis vols, ressources, ruches et HUD dans `WorldMapMmoFullscreenFoundationBootstrap.OnGUI`.

Le terrain utilise l'atlas continu par un seul `GUI.DrawTextureWithTexCoords` plein ecran. Les vols sont traces par des arcs de Bezier en coordonnees monde et portent explicitement le non-claim `aucune route`. Aucune logique de route terrestre n'apparait dans le chemin de rendu ou de mouvement inspecte.

## Non-claims

Les sept manifestes portent tous:

- `master_5x5_integrated=false`;
- `server_live=false`;
- `screenshot_retouched=false`;
- `visual_masking_overlay_added=false`.

Les captures affichent `locale/demo`, `Donnees non officielles` et/ou `serveur live absent`. Les rapports obligatoires excluent explicitement un test Android physique, une integration Wave3 5x5 et tout service staging/live. Aucun claim live ou device n'a ete detecte.

`READY_FOR_WORLD_MAP_WAVE3_UNITY_INTEGRATION=YES` signifie uniquement que le gate de continuite Step4D permet de passer a l'etape d'integration Wave3 sous ses propres gates. Il ne signifie pas que Wave3 5x5 est deja integre ou valide.

## Reserves non bloquantes

1. P2 outil: la file multi-etats automatique peut decaler image/manifeste et melanger les resolutions. Les sept etats manuels exacts sont coherents et hash-lockes; aucune conclusion produit n'est tiree de la file rejetee.
2. Temporalite: absence de video continue du pan; les trois etats ordonnes et leurs UV valident le mouvement discret, pas chaque frame intermediaire.
3. Plateforme: preuve Unity Editor Game View uniquement; aucun appareil physique n'est revendique.
4. Portee artistique: surface macro UI-B 3x3 uniquement; `master_5x5_integrated=false`.

## Marqueurs finaux

```text
QA_B_WORLD_MAP_RUNTIME_CONTINUITY_STEP4D = PASS_WITH_RESERVES
EXACT_SEVEN_STATE_PROOF = PASS
VISIBLE_TILE_OR_CHUNK_BOUNDARY = NO
READY_FOR_WORLD_MAP_WAVE3_UNITY_INTEGRATION = YES
```
