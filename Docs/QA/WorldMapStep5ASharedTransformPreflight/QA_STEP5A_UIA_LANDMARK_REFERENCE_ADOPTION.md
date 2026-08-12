# QA-A - Step5A UI-A Landmark Reference Adoption

Date : 2026-07-14  
Portee : adoption documentaire, aucune execution Unity  
Statut : source obligatoire pour la future preuve Step5A

## Integrite

| Source UI-A | Dimensions / octets | SHA-256 |
|---|---:|---|
| `UIA_WorldMapStep5A_LandmarkMotionReference_Report.md` | 10698 octets | `B1A2C4792284419FB5C14A1E73D22CB1C5A9785F9B813585774D3A4FB94B342B` |
| `UIA_WorldMapStep5A_MasterLandmarks_Annotated.png` | `3280x2560` | `A9592DF5625BADDC5CB4169DC3044DB58396C2C41C285486ECE3D07CB4BA92CE` |
| `UIA_WorldMapStep5A_PanZoomReference.png` | `3000x2000` | `A275B4C9092CA5E73AC01C15CDE39CD495CC8AF0F496F2E14E4E898F7BA86AAF` |
| `UIA_WorldMapStep5A_Landmarks.json` | 4868 octets | `5FD6B39AA20DAF155F693C5C3FA5912A3503749EE97E2E8E7E20CDB769AFEE1E` |

Controle independant :

- `13/13` landmarks uniques ;
- coordonnees master, tuile et locales coherentes pour `13/13` ;
- `3/3` paires de pan coherentes avec les coordonnees et la formule `delta_ecran = -zoom * delta_camera` ;
- `3/3` pivots relies au bon landmark, a la bonne tuile et aux bonnes coordonnees ;
- les deux PNG ont ete inspectes a leur resolution native.

## Contrat obligatoire

- pans : `PH01 L05-L06`, `PH02 L10-L12`, `PV01 L05-L08` ;
- zooms : `Z01/L05`, `Z02/L09`, `Z03/L11` aux facteurs `0.75 / 1.00 / 1.50` ;
- erreur terrain/entite ou pivot : `2 px` maximum en capture deterministe, `3 px` sur preuve physique compressee ;
- HUD : translation `1 px` maximum, ratio de taille `0.995..1.005` ;
- les 13 landmarks restent des reperes naturels, jamais des elements de gameplay.

Les annotations UI-A sont des derives QA. Anneaux, numeros, croix, textes de mesure et planches annotees sont interdits dans les captures runtime autoritatives. Toute annotation de comparaison doit etre appliquee apres capture sur une copie derivee, avec le PNG brut conserve et hashe.

Cette adoption ne valide aucun comportement runtime. Elle rend simplement les quatre sources UI-A obligatoires pour Demo-A et QA-A lors du prochain handoff.

```text
QA_STEP5A_UIA_REFERENCE_ADOPTED=YES
UIA_LANDMARKS_REQUIRED=13
UIA_PAN_PAIRS_REQUIRED=3
UIA_ZOOM_PIVOTS_REQUIRED=3
UIA_ANNOTATIONS_ALLOWED_IN_RUNTIME=NO
UNITY_EXECUTED_FOR_ADOPTION=NO
```
