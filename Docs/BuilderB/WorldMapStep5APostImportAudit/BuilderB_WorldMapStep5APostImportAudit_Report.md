# Builder-B - Audit post-import Step5A Wave3

Date de l'audit final : 2026-07-14 14:35 UTC  
Périmètre : comparaison statique et en lecture seule du payload Wave3 et de l'import Unity Builder-A  
Unity lancé : non  
Fichiers `Assets`, PNG, `.meta`, scènes et réglages projet modifiés par Builder-B : aucun

## 1. Verdict exécutif

L'import réel contient exactement les 25 tuiles runtime attendues. Chaque PNG importé est byte-identique à sa source dans le payload Step5A, possède une empreinte unique et correspond pixel par pixel à la fenêtre 516 x 516 attendue du master reconstruit.

Le manifeste importé est byte-identique au manifeste validé. Les 40 frontières internes sont couvertes par 80 côtés de gouttière dirigés vérifiés, tous issus des vrais pixels voisins. Les 20 côtés extérieurs utilisent uniquement le clamp attendu.

Les 25 fichiers `.meta` satisfont les critères actifs demandés : `Clamp`, `Bilinear`, mipmaps désactivés, NPOT `None`, texture de type `Default` non-Sprite, lecture CPU désactivée et sRGB activé.

Cet audit ne constitue pas une validation Play Mode, un test de rendu Unity, un test APK ou une preuve appareil.

## 2. Sources comparées

Payload validé :

`C:\projets\beekingdomgame-master\artifacts\WorldMapWave3_UnityImportPayload_staging\`

Import Builder-A audité en lecture seule :

`C:\projets\beekingdomgame-master\Assets\BeeKingdom\Playground\Resources\WorldMapWave3Runtime\UIB_ContinuousMaster5x5_v1\`

Reconstruction canonique de référence :

`C:\projets\beekingdomgame-master\artifacts\WorldMapWave3_RuntimeBundle_staging\run1\canonical\reconstruction.png`

Outil d'audit reproductible :

`C:\projets\beekingdomgame-master\Docs\BuilderB\WorldMapStep5APostImportAudit\audit_step5a_post_import.py`

SHA-256 de l'outil :

`6e3332ec98ff2c5102f7ee8696496602cc57bf3e147cb0563914456c83d55c5b`

Résultat machine-readable :

`C:\projets\beekingdomgame-master\Docs\BuilderB\WorldMapStep5APostImportAudit\post_import_audit.result.json`

SHA-256 du résultat :

`b380d262d7d692061b28189ab378479ff645c84d2e95cde08f5ffb6afb223e1e`

Le chemin canonique du rapport se trouve hors racine inscriptible de cette tâche. Le fallback explicitement autorisé a donc été utilisé.

## 3. Inventaire et conformité du manifeste

| Contrôle | Résultat |
|---|---:|
| PNG runtime attendus | 25 |
| PNG runtime présents | 25 |
| PNG manquants | 0 |
| PNG supplémentaires | 0 |
| Dimensions | 25/25 en 516 x 516 |
| Mode couleur | 25/25 en RGB |
| SHA-256 PNG conformes au payload | 25/25 |
| SHA-256 pixels conformes | 25/25 |
| Hashes PNG uniques | 25/25 |
| Hashes pixels uniques | 25/25 |
| Doublons exacts | 0 |

Manifeste importé : `manifest.runtime.unity.json`

- SHA-256 importé : `bde8c07b6430afe964e136256acfcc1f25854331476354bbb9eda9104e391911`
- SHA-256 attendu : `bde8c07b6430afe964e136256acfcc1f25854331476354bbb9eda9104e391911`
- Comparaison byte et JSON avec `source.handoff.unity.json` : identique.
- Schéma : `bee-kingdom.world-map-wave3-unity-integration-handoff.v1`.
- Ordre : `R0C0` à `R4C4`, row-major.
- Orientation : origine haut-gauche, aucune transposition, rotation ou inversion.
- Hash master déclaré : `d3cdc2dde9d56cac58be6833790b6fd8fc38ac157f72a01dcebd8117583a95b4`.

## 4. Validation pixels et gouttières

Pour chaque tuile importée, l'audit a reconstruit la fenêtre attendue depuis le master 2560 x 2560 : intérieur 512 x 512, gouttière de 2 pixels par côté, pixels voisins réels aux frontières internes et clamp seulement sur les bords du macro-master.

| Contrôle | Résultat |
|---|---:|
| Pixels différents par rapport au payload | 0 |
| Pixels différents par rapport au master + gutters | 0 |
| Pixels différents dans les intérieurs 512 x 512 | 0 |
| Frontières internes non dirigées | 40/40 |
| Côtés internes à vrais voisins | 80/80 |
| Côtés extérieurs clampés | 20/20 |

Conclusion : les gouttières importées n'ont pas été réencodées, étirées, remplacées ni dupliquées entre tuiles internes.

## 5. Réglages Unity `.meta`

Les 25 `.meta` PNG sont présents, possèdent 25 GUID uniques et partagent le même profil actif :

| Réglage | Valeur observée | Attendu | Résultat |
|---|---:|---:|---:|
| `textureType` | `0` (`Default`) | `Default` | PASS |
| `filterMode` | `1` (`Bilinear`) | `Bilinear` | PASS |
| `wrapU`, `wrapV`, `wrapW` | `1` (`Clamp`) | `Clamp` | PASS |
| `enableMipMap` | `0` | désactivé | PASS |
| `streamingMipmaps` | `0` | désactivé | PASS |
| `nPOTScale` | `0` (`None`) | `None` | PASS |
| `isReadable` | `0` | désactivé | PASS |
| `sRGBTexture` | `1` | activé | PASS |
| `aniso` | `1` | `1` | PASS |
| `textureShape` | `1` (`2D`) | `2D` | PASS |
| `alphaUsage` | `0` | aucune alpha source | PASS |
| `alphaIsTransparency` | `0` | désactivé | PASS |
| `sprites` | `[]` | aucune slice | PASS |

### Observations non bloquantes

1. Les `.meta` conservent `spriteMode: 2` comme valeur sérialisée inactive. Le réglage actif est bien `textureType: 0` (`Default`) et la liste `sprites` est vide. L'import effectif est donc non-Sprite.
2. Le profil Android n'a pas d'override explicite sur ASTC 6x6/max 1024 (`overridden: 0`). Les critères demandés dans cet audit restent conformes et la texture source 516 x 516 n'est pas redimensionnée. Le verrouillage explicite ASTC reste une recommandation de préparation Android, pas un blocker de DEMO-100 pour ce contrôle statique.

## 6. Preuve de lecture seule

Le script ne possède aucun chemin d'écriture sous `Assets`. Il refuse également un `--output` situé sous `Assets`.

L'arborescence importée a été hashée avant et après l'audit :

- Nombre de fichiers avant : 52.
- Nombre de fichiers après : 52.
- SHA-256 d'arbre avant : `77245ad1ccf9d918a5d40f5495ccd1c19f20c7c58e7ce9ab556649aa47b948b3`.
- SHA-256 d'arbre après : `77245ad1ccf9d918a5d40f5495ccd1c19f20c7c58e7ce9ab556649aa47b948b3`.
- Stabilité : PASS.

Aucun lancement Unity, build, import forcé, écriture de `.meta`, copie de PNG ou changement de scène n'a été effectué.

## 7. Exécution réalisée

Exécution finale :

```powershell
& 'C:\Users\Utilisateur\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' `
  'Docs\BuilderB\WorldMapStep5APostImportAudit\audit_step5a_post_import.py'
```

Résultat final du processus : code `0`, statut `PASS`.

Une première exécution de développement avait signalé à tort les 25 enregistrements à cause d'une lecture incorrecte du champ `source.relative_to_bundle` dans l'outil. Cette lecture a été corrigée, puis tous les contrôles ont été relancés depuis les sources. Les fichiers importés sont restés inchangés pendant les deux passages.

## 8. Portée et suite Demo

Ce contrôle confirme la fidélité statique de l'import Step5A et ne découvre aucun blocker réel pour le support DEMO-100. Demo/QA doivent encore observer dans Unity le rendu sans grille, le pan/zoom, l'orientation, les overlays et les vols aériens. Aucun déplacement par route terrestre n'a été ajouté ou validé par Builder-B.

Claims exclus : monde live, serveur live, intégration appareil, APK validé, rendu Unity validé ou carte MMO finale livrée.

## 9. Verdicts exacts

`POST_IMPORT_25_TILE_PAYLOAD = PASS`

`UNITY_IMPORT_SETTINGS = PASS`

`RUNTIME_GUTTERS_AND_HASHES = PASS`

`READY_FOR_DEMO_SUPPORT = YES`
