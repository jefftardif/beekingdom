# QA WorldMap Step5A - Shared Transform Preflight

Date : 2026-07-14  
Role : QA-A  
Type : preparation de gate uniquement  
Execution Unity : non  
Modification produit : aucune

## Verdict de preparation

**PROTOCOL READY**

Le protocole Step5A est pret pour le futur handoff Builder-A. Il ferme la faiblesse de preuve Step4D en exigeant une interaction directe et mesurable : le terrain, les ruches, les ressources, les selections et les vols doivent partager le meme monde-vers-ecran pendant pan et zoom, tandis que le HUD reste fixe.

Ce verdict ne valide pas la correction Builder-A. Le gate produit reste bloque jusqu'au handoff, puis jusqu'a une validation Unity en Play Mode normal.

## Contexte decisif

Le proprietaire a directement observe un background statique pendant que les entites bougent ou zooment. Cette observation revoque Step4D pour la continuite runtime. Les sept anciennes captures fixes et leurs manifestes peuvent rester integres comme archive, mais ne prouvent pas la synchronisation temporelle des couches.

Le protocole interdit donc :

- un PASS fonde uniquement sur des PNG statiques ;
- une preuve issue uniquement d'un outil Editor qui construit une scene temporaire ;
- une conclusion tiree d'une petite variation UV sans deplacement terrain evident ;
- la reutilisation des 25 tuiles Wave3 par modulo pour simuler un monde 64x64.

## Sources relues

- `Docs/QA/Relief_WorldMapRuntimeContinuityStep4D/QA_Relief_WorldMapRuntimeContinuityStep4D.md` ;
- `Docs/QA/QA_B_WORLD_MAP_RUNTIME_CONTINUITY_STEP4D_DIRECT_VALIDATION.md`, comme historique revoque ;
- `Docs/BuilderB/WorldMapWave3UnityIntegrationHandoff/BuilderB_WorldMapWave3UnityIntegrationHandoff_Report.md` ;
- `Docs/BuilderB/WorldMapWave3UnityIntegrationHandoff/WorldMapWave3_UnityIntegrationProcedure.md` ;
- `Docs/BuilderB/WorldMapWave3UnityIntegrationHandoff/WorldMapWave3_BuilderASelfChecks.md` ;
- `Docs/Demos/DEMO-099_WorldMapWave3ContinuousArtBundle_STAGING/DEMO-099_Report.md` ;
- `Docs/BuilderC/BuilderC_WorldMapContinuousMasterWave3_Validator_Report.md` ;
- `Docs/UIA/WorldMapStep5ALandmarkMotionReference/UIA_WorldMapStep5A_LandmarkMotionReference_Report.md` ;
- `Docs/UIA/WorldMapStep5ALandmarkMotionReference/UIA_WorldMapStep5A_MasterLandmarks_Annotated.png` ;
- `Docs/UIA/WorldMapStep5ALandmarkMotionReference/UIA_WorldMapStep5A_PanZoomReference.png` ;
- `Docs/UIA/WorldMapStep5ALandmarkMotionReference/UIA_WorldMapStep5A_Landmarks.json`.

## Livrables prepares

1. Protocole executable :
   `Docs/QA/WorldMapStep5ASharedTransformPreflight/QA_STEP5A_SHARED_TRANSFORM_TEST_PROTOCOL.md`
2. Template machine-readable de preuve :
   `Docs/QA/WorldMapStep5ASharedTransformPreflight/QA_STEP5A_EVIDENCE_MANIFEST_TEMPLATE.json`
3. Recu d'adoption UI-A :
   `Docs/QA/WorldMapStep5ASharedTransformPreflight/QA_STEP5A_UIA_LANDMARK_REFERENCE_ADOPTION.md`

## Gates obligatoires apres handoff

| Gate | Condition de sortie |
|---|---|
| Reference UI-A | 13 landmarks identifies, `PH01/PH02/PV01` mesures, `Z01/Z02/Z03` mesures, quatre sources hash-lockees |
| Transformation partagee | pan evident du terrain et vecteurs terrain/entites concordants dans la tolerance QA |
| Zoom partage | terrain et entites utilisent le meme pivot, puis reviennent alignes |
| HUD fixe UI-A | translation maximum 1 px, ratio de taille `0.995..1.005` |
| Wave3 | exactement 25 tuiles uniques, hashes/mapping/UV conformes, aucune couture/grille/repetition |
| Camera | region art bornee `(30..34,30..34)`, aucun wrap/modulo ni art 64x64 simule |
| Responsive | paysage `1920x1080` et portrait `720x1280` utilisables sans rupture bloquante |
| Selection | ruches, ressources, halos, hit zones et panneaux restent alignes apres transformation |
| Vols | trajectoires air-only, extremites ancrees, aucune route terrestre |
| Preuve joueur | smoke test reproductible en moins de 30 secondes dans chaque orientation |

## Blockers non negociables

Le futur verdict sera `BLOCKED` si un seul point suivant est constate :

1. terrain statique ou mouvement sous le seuil pendant que les entites se deplacent ;
2. terrain et entites suivent des vecteurs ou pivots differents ;
3. HUD, panneau ou minimap suit la camera ou zoome ;
4. absence de preuve interactive directe ou temporelle ;
5. nombre de tuiles different de 25, hash faux, tuile manquante ou dupliquee ;
6. couture, grille, repetition, trou, gutter visible ou orientation fausse ;
7. modulo/wrap ou camera sortant du pilote 5x5 ;
8. layout paysage/portrait casse ou selection inaccessible ;
9. halo, hit zone, ressource, ruche ou arc desancre apres pan/zoom ;
10. vol guide par une route au sol ;
11. scene de capture temporaire presentee comme le runtime normal ;
12. source UI-A absente, scenario `PH01/PH02/PV01` ou `Z01/Z02/Z03` non mesure ;
13. annotation UI-A, anneau, numero de landmark ou croix visible dans le runtime joueur ;
14. claim de monde 64x64 artistique complet, serveur live ou fonctionnalite officielle absente.

Aucune reserve n'est admissible sur ces quatorze points. Une reserve device physique peut rester non bloquante car elle est hors scope de ce gate local.

## Etat d'attente

QA-A n'a pas lance Unity, n'a pas inspecte un runtime en cours de modification et n'a touche aucun fichier produit. La prochaine validation commencera seulement apres remise explicite du rapport Builder-A et du paquet Demo-A.

```text
QA_STEP5A_SHARED_TRANSFORM_PROTOCOL_READY=YES
UIA_STEP5A_LANDMARK_REFERENCE_REQUIRED=YES
STEP4D_SHARED_TRANSFORM_VERDICT=REVOKED_BLOCKED
BUILDER_A_HANDOFF_RECEIVED=NO
UNITY_VALIDATION_EXECUTED=NO
STEP5A_PRODUCT_VALIDATED=NO
WAITING_FOR_BUILDER_A=YES
```
