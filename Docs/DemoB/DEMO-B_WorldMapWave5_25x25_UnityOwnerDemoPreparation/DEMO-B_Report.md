# DEMO-B - Preparation de la demonstration Unity World Map Wave5 25x25

Date : 2026-07-14  
Role : Demo-B  
Mode : preparation read-only, hors Unity  
Duree cible : 3 a 5 minutes  
Mise a jour preflight : gate Builder-A observe READY

## 1. Statut et portee

Ce document prepare le parcours que le proprietaire peut maintenant executer dans Unity. Demo-B n'a pas ouvert Unity : l'autorisation de lancement local repose sur le rapport canonique Builder-A, son recu statique, sa telemetrie et ses captures Play Mode.

Etat observe en lecture seule au moment de cette preparation :

- le lot artistique Wave5 existe sous `artifacts/UIB_ImmenseContinuousMaster25x25_staging/` ;
- le master fait `12800x12800`, sa grille est `25x25`, et les `625` PNG de tuiles sont presents ;
- le SHA-256 recalcule du master est `50F3FF9640251F365484F31DE4AA5AB542587381E5F8EEB9324D67BE37125913` ;
- le manifeste Wave5 a pour SHA-256 `9C92ADEAF69C930EEEBC94EAD295E8A35A3A8411BF6646A6841909C860B93655` ;
- le rapport canonique `BuilderA_WorldMapWave5_25x25_UnityIntegration_Report.md`, SHA-256 `894376D6E897C67E3983B5BD3435DFFEC445EF0ADF54E1A854300A28DB2691CD`, declare l'integration Wave5 25x25 PASS ;
- la scene canonique declaree est `Assets/Scenes/WorldMapMmoFullscreenFoundation.unity` ;
- le bootstrap courant relu a pour SHA-256 `C101A4420AB21A699E1459818E27CD1C2D70F6E90262438CA8CC72AC0D2AE999` et initialise le provider Wave5 ;
- l'ancien provider Wave3 5x5 peut subsister comme code historique, mais il est inactif dans la scene canonique et n'est pas un fallback ;
- la compilation C# est PASS avec zero erreur, le validateur Wave5 est PASS et `625/625` tuiles runtime sont recues ;
- les `11` captures Play Mode sont presentes, lisibles et toutes de hash distinct : `9` paysage `1280x720` et `2` portrait `720x1280` ;
- le cycle BearDen visible, masque puis restaure est PASS, le HUD est fixe et aucun ours n'est visible ;
- le handoff Builder-A porte exactement `READY_FOR_PLAYER_UNITY_TEST=YES`.

La navigation Ruche vers la scene canonique est documentee comme routant les deux boutons `Monde` vers `WorldMapMmoFullscreenFoundation`. Le proprietaire peut lancer le parcours maintenant; le clic joueur reel reste la premiere etape visible de sa demonstration.

## 2. Sources read-only

- Scene cible : `Assets/Scenes/WorldMapMmoFullscreenFoundation.unity`
- Bootstrap relu : `Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs`
- Rapport de navigation : `Docs/BuilderA/HiveToCanonicalWorldMapNavigationFix/BuilderA_HiveToCanonicalWorldMapNavigationFix_Report.md`
- Rapport canonique Builder-A : `C:/projets/beekingdom/prompts_codex/rapports/BuilderA_WorldMapWave5_25x25_UnityIntegration_Report.md`
- Miroir Builder-A : `Docs/BuilderA/BuilderA_WorldMapWave5_25x25_UnityIntegration_Report.md`
- Validation statique : `Docs/BuilderA/WorldMapWave5_25x25_UnityIntegration/WorldMapWave5_StaticValidation.txt`
- Recu Play Mode : `Docs/BuilderA/WorldMapWave5_25x25_UnityIntegration/PlayerProof/WorldMapWave5_PlayerProofReceipt.md`
- Telemetrie Play Mode : `Docs/BuilderA/WorldMapWave5_25x25_UnityIntegration/PlayerProof/WorldMapWave5_PlayerProofTelemetry.json`
- Preparation historique : `Docs/BuilderA/BuilderA_Wave5_25x25_UnityIntegrationPreparation_Report.md`
- Checklist QA-B : `Docs/QA/Wave5_25x25_Unity_Demo_Checklist.md`
- Master Wave5 : `artifacts/UIB_ImmenseContinuousMaster25x25_staging/master_25x25_12800.png`
- Manifeste Wave5 : `artifacts/UIB_ImmenseContinuousMaster25x25_staging/manifest.json`
- Rapport UI-B : `artifacts/UIB_ImmenseContinuousMaster25x25_staging/UIB_WorldMapImmenseContinuousMasterWave5_25x25_Report.md`
- Landmark integre : `artifacts/EventLandmarks/BearDen/bear_den_dormant_v1.png`

Le landmark BearDen est un PNG `1535x1024`, RGBA, SHA-256 `316E172A341B4F56DFDC690ADF416913D80FC377F9F8D788F69000D1F9A5FB8C`. La lecture visuelle confirme une taniere dormante sans ours dans l'asset.

## 3. Preflight proprietaire, gate Builder-A ferme

### Recu observe par Demo-B

| Controle Builder-A | Resultat observe |
|---|---|
| `READY_FOR_PLAYER_UNITY_TEST` | YES |
| Scene canonique | `Assets/Scenes/WorldMapMmoFullscreenFoundation.unity` |
| Compilation C# | PASS, zero erreur |
| Integration Wave5 | PASS, `625/625` |
| Captures Play Mode | `11/11`, lisibles et uniques |
| Centre et quatre coins | PASS |
| Zoom natif `1.35x` | PASS |
| Coutures / grille | `0` / NON |
| HUD fixe | PASS |
| Ancien Wave3 5x5 actif | NON |
| BearDen visible / masque / restaure | PASS / PASS / PASS |
| Ours visible | NON |
| Serveur live / evenement actif | NON / NON |

Le proprietaire est autorise a lancer le parcours Unity local maintenant. Avant de montrer l'ecran, il confirme rapidement les neuf points suivants et arrete seulement si l'etat reel diverge du handoff :

1. Unity compile sans erreur C# et la Game View est stable.
2. La demonstration demarre dans la Ruche, puis un clic reel sur `Monde` charge `WorldMapMmoFullscreenFoundation`.
3. Le HUD ou le recu runtime identifie explicitement `Wave5`, `25x25`, `625 tuiles` et le hash master attendu. Le libelle `Wave3 5x5 partage monde` est un NO-GO immediat.
4. La vue globale logique montre dans la Game View le HUD `Wave5 - monde 25x25`, la minimap et le diagnostic/catalogue `625`. Le streaming n'a pas a afficher les 625 textures simultanement.
5. Les quatre tuiles d'angle sont atteignables sans repetition ni wrap : `R00C00`, `R00C24`, `R24C00`, `R24C24`.
6. La tuile centrale `R12C12` est atteignable et le retour centre restaure exactement le cadrage initial.
7. La grille debug est desactivee. Dans le bootstrap historique, la touche `G` la bascule ; l'etat final doit rester OFF.
8. Le HUD reste ancre en espace ecran pendant un pan et un zoom. Derive admise : au plus `1 px`; ratio de taille admis : `0.995` a `1.005`.
9. Le rapport canonique d'integration Wave5/BearDen de Builder-A declare exactement `READY_FOR_PLAYER_UNITY_TEST=YES` ; ce point est observe PASS dans le handoff courant.

Si l'identite globale 25x25/625 ou une position d'angle diverge du handoff d'integration, la demonstration est arretee. Ne pas utiliser la Scene View, l'Inspector ou un montage d'images pour simuler l'etendue.

## 4. Reperes de grille pour le handoff

Ces coordonnees sont celles du master artistique. L'integration doit fournir la conversion vers ses coordonnees monde si elle n'est pas identique.

| Position | Tuile | Centre master `(x,y)` | Usage demo |
|---|---|---:|---|
| Nord-ouest | `R00C00` | `(256,256)` | premier angle |
| Nord-est | `R00C24` | `(12544,256)` | deuxieme angle |
| Centre | `R12C12` | `(6400,6400)` | ouverture et retour |
| Sud-est | `R24C24` | `(12544,12544)` | troisieme angle |
| Sud-ouest | `R24C00` | `(256,12544)` | quatrieme angle |

## 5. Parcours proprietaire, environ 4 minutes

### 0:00-0:25 - Depart Ruche

- Stabiliser la vue Ruche.
- Montrer le bouton `Monde` sans ouvrir de menu technique.
- Dire : « Je pars de la Ruche vers la carte mondiale de demonstration. »
- Capturer `D00_Hive_MondeEntry_1920x1080.png`.

### 0:25-0:50 - Ouverture de la scene canonique

- Cliquer une seule fois sur `Monde`.
- Attendre la stabilisation complete de la Game View.
- Verifier le nom de scene dans le recu, pas dans une capture finale encombrementee par l'Editor.
- Montrer le centre `R12C12`, le HUD, la minimap et le libelle `Wave5 25x25 / 625`.
- Capturer `D01_WorldMap_Open_Center_R12C12_1920x1080.png`.

### 0:50-1:15 - Vue globale logique 25x25

- Montrer ensemble le HUD `Wave5 - monde 25x25`, le provider `Wave5 25x25 streaming` et la minimap.
- Expliquer que le catalogue contient `625` tuiles et que seules les tuiles du viewport sont chargees simultanement.
- Laisser la vue immobile au moins une seconde.
- Pointer la diversite des grands biomes, sans annoncer un monde live ou complet.
- Capturer `D02_WorldMap_GlobalLogical_25x25_625_1920x1080.png`.

### 1:15-2:15 - Pan continu vers les quatre coins

- Aller dans l'ordre horaire : nord-ouest, nord-est, sud-est, sud-ouest.
- Garder le HUD visible pendant chaque deplacement.
- A chaque coin, attendre `500 ms` apres stabilisation et montrer l'identifiant de tuile dans le recu ou le HUD.
- Ne jamais activer une grille pour prouver l'identifiant.
- Capturer :
  - `D03_Corner_NW_R00C00_1920x1080.png` ;
  - `D04_Corner_NE_R00C24_1920x1080.png` ;
  - `D05_Corner_SE_R24C24_1920x1080.png` ;
  - `D06_Corner_SW_R24C00_1920x1080.png`.

Pendant les pans, refuser tout bord noir, flash, trou, chevauchement, wrap vers le bord oppose, repetition ou changement de nettete rectangulaire.

### 2:15-2:45 - Zoom proche et tuiles 512

- Revenir vers la prairie centrale.
- Cadrer la jonction `R12C12` / `R12C13` a un niveau ou une tuile 512 est lisible sans afficher sa grille.
- Faire un court pan de part et d'autre de la jointure, puis un zoom avant et arriere.
- Verifier que terrain et overlays monde se deplacent ensemble et que le HUD ne bouge pas.
- Capturer `D07_Native512_R12C12_R12C13_Close_1920x1080.png`.

Le but est de montrer le detail natif et l'absence de couture. La limite 512 ne doit pas devenir visible dans l'image finale.

### 2:45-3:55 - Bouton HUD BearDen

Cette sequence est obligatoire dans la demonstration Wave5/BearDen. Son gate Builder-A est ferme avec `READY_FOR_PLAYER_UNITY_TEST=YES`; elle peut etre executee maintenant par le proprietaire.

Reference attendue :

- tuile d'ancrage : `R05C02` ;
- haut-gauche master attendu : `(1280,3031)` ;
- asset : `bear_den_dormant_v1.png` ;
- etat initial d'une nouvelle session locale : visible ;
- etat evenement : inactif ;
- autorite serveur : absente.

#### A. Visible par defaut

- Rejoindre `R05C02` avec le controle integre, sans inventer de coordonnee runtime.
- Sur une nouvelle session locale, verifier que la taniere est visible avant toute action sur le bouton.
- Relever l'ancre monde, la position ecran de la taniere et le rectangle ecran du bouton HUD `BearDen`.
- Capturer `D08_BearDen_DefaultVisible_R05C02_1920x1080.png`.
- Effectuer un pan puis un zoom pendant que la taniere est visible : la taniere suit exactement le terrain et le bouton reste fixe a `1 px` pres.
- Enregistrer cette partie dans `V01_BearDen_VisibleHidden_PanZoom_Toggle.mp4`, puis revenir au cadrage et au zoom de D08.

#### B. Premier clic, taniere masquee

- Cliquer une seule fois sur le bouton HUD `BearDen`.
- Verifier que seule la taniere disparait.
- Le terrain, les ruches, les ressources, les selections, les vols, la minimap et les autres panneaux gardent leur visibilite, leur ancre et leur etat.
- Entre D08 et D09 au meme cadrage, les seules differences admises sont la zone alpha de BearDen et l'etat visuel du bouton.
- Capturer `D09_BearDen_Hidden_OtherLayersUnchanged_1920x1080.png`.
- Repeter le meme pan et le meme zoom pendant l'etat masque : aucun landmark fantome ne doit apparaitre et le bouton HUD reste fixe a `1 px` pres.
- Continuer `V01`, puis revenir exactement au cadrage et au zoom de D08 sans cliquer entre-temps.

#### C. Second clic, restauration exacte

- Sans mouvement de camera, re-cliquer une seule fois sur le bouton `BearDen`.
- Verifier que la taniere reapparait au meme ancrage `R05C02`, avec la meme echelle et a `1 px` maximum de sa position ecran initiale.
- Capturer `D10_BearDen_RestoredSameAnchor_R05C02_1920x1080.png`.
- Confirmer qu'aucun ours, silhouette ou ombre d'ours n'est visible dans la taniere, le bouton ou la scene.
- Confirmer l'absence de fumee active, combat, pulsation, compte a rebours, attaque, recompense, evenement actif, requete serveur ou persistance.

Si le landmark ou le bouton manque pendant le parcours reel malgre le handoff PASS, ne pas simuler cette etape avec le PNG brut : arreter la presentation et renvoyer la regression a Builder-A/QA.

### 3:55-4:15 - Retour centre et HUD fixe

- Utiliser `R` uniquement si le handoff Wave5 confirme que le reset historique est conserve ; sinon utiliser la commande de retour centre documentee par l'integrateur.
- Retrouver `R12C12`, le zoom et le cadrage d'ouverture.
- Comparer visuellement les rectangles du HUD avec `D01`.
- Capturer `D11_ReturnCenter_R12C12_HudFixed_1920x1080.png`.

### 4:15-4:35 - Conclusion honnete

Formulation conseillee, uniquement apres preflight reussi :

> Cette demonstration montre un candidat artistique local 25x25 dans la scene Unity canonique. Elle ne prouve ni serveur live, ni multi-serveur actif, ni monde immense complet.

## 6. Captures minimales

| ID | Capture | Preuve attendue | Obligatoire |
|---|---|---|---|
| D00 | Ruche avant clic `Monde` | point de depart joueur | oui |
| D01 | ouverture centre `R12C12` | scene canonique, HUD et minimap | oui |
| D02 | vue globale logique | HUD/minimap 25x25 et catalogue 625 | oui |
| D03 | angle `R00C00` | borne nord-ouest atteinte | oui |
| D04 | angle `R00C24` | borne nord-est atteinte | oui |
| D05 | angle `R24C24` | borne sud-est atteinte | oui |
| D06 | angle `R24C00` | borne sud-ouest atteinte | oui |
| D07 | zoom proche `R12C12/R12C13` | detail 512 et jointure invisible | oui |
| D08 | BearDen visible par defaut | `R05C02`, ancre et bouton releves | oui |
| D09 | BearDen masque | seule la taniere disparait | oui |
| D10 | BearDen restaure | meme ancre/echelle, erreur `<=1 px` | oui |
| D11 | retour centre | cadrage restaure et HUD fixe | oui |
| V01 | video BearDen deux etats | pan/zoom visible et masque, bouton fixe | oui |

Les `11` captures Builder-A ferment deja le preflight technique. Si le parcours proprietaire est enregistre comme nouveau paquet Demo, les douze captures ci-dessus doivent etre des Game View completes, non recadrees, a resolution identique, et `V01` prouve le pan/zoom des deux etats sans montage. La demonstration complete reste comprise entre 3 et 5 minutes.

## 7. Modele de manifeste de capture

Le paquet futur doit completer un enregistrement par capture sans utiliser de valeur inventee :

```json
{
  "schema": "bee-kingdom.owner-demo.wave5-25x25.v1",
  "scope": "local_demo_only",
  "capture_id": "D02",
  "file": "D02_WorldMap_GlobalLogical_25x25_625_1920x1080.png",
  "captured_utc": "<UTC_REEL>",
  "scene": "WorldMapMmoFullscreenFoundation",
  "resolution": { "width": 1920, "height": 1080 },
  "art_provider": "<LIBELLE_RUNTIME_REEL>",
  "master_sha256": "50F3FF9640251F365484F31DE4AA5AB542587381E5F8EEB9324D67BE37125913",
  "grid": { "rows": 25, "columns": 25, "tile_size": 512, "tile_count": 625 },
  "camera": { "center_world": "<VALEUR_REELLE>", "zoom": "<VALEUR_REELLE>" },
  "focus_tile": "<RxxCyy_OU_GLOBAL>",
  "global_identity_25x25_625_visible": "<YES_NO>",
  "outer_bounds_proven_by_corner_sequence": "<YES_NO>",
  "debug_grid_visible": false,
  "hud_rect": "<X_Y_W_H_REELS>",
  "hud_drift_from_D01_px": "<VALEUR_MESUREE>",
  "bear_den": {
    "integrated": "<YES_NO_REEL>",
    "anchor_tile": "R05C02",
    "anchor_master_top_left": [1280, 3031],
    "state": "<DEFAULT_VISIBLE_HIDDEN_RESTORED>",
    "visible": "<YES_NO_REEL>",
    "world_anchor": "<VALEUR_RUNTIME_REELLE>",
    "screen_anchor_px": "<X_Y_REELS>",
    "restore_error_px": "<VALEUR_MESUREE>",
    "hud_button_rect": "<X_Y_W_H_REELS>",
    "hud_button_drift_px": "<VALEUR_MESUREE>",
    "terrain_unchanged_by_toggle": "<YES_NO_MESURE>",
    "other_entities_unchanged_by_toggle": "<YES_NO_MESURE>",
    "bear_visible_outside_event": false,
    "event_active": false,
    "server_action_emitted": false,
    "asset_sha256": "316E172A341B4F56DFDC690ADF416913D80FC377F9F8D788F69000D1F9A5FB8C"
  },
  "claims": {
    "live_server": false,
    "active_multi_server": false,
    "complete_immense_world": false
  }
}
```

## 8. Refus immediats

Arreter la demonstration avant toute presentation publique si un seul point suivant est observe :

- le HUD, le log ou le provider indique encore `Wave3 5x5`, `25 tuiles` ou `UIB_ContinuousMaster5x5_v1` ;
- la scene affiche le panneau `WorldMap Wave3 indisponible` ou un fallback ;
- le clic `Monde` ouvre l'ancien preview `ReferenceSurfaceMode.WorldBoundary` au lieu de la scene canonique ;
- la vue globale logique n'identifie pas `25x25 / 625`, ou la minimap ne correspond pas au domaine ;
- un angle attendu est inaccessible, mal identifie ou wrappe vers le bord oppose ;
- une tuile manque, devient noire, clignote, se superpose ou repete une autre zone ;
- une couture, grille, ligne 512, carre central, anneau ou bande floue devient visible ;
- le terrain reste fixe tandis que les entites bougent, ou un overlay monde glisse sur le terrain ;
- le HUD translate de plus de `1 px`, zoome ou derive pendant le pan ;
- une grille debug, un ancien preview ou une capture Scene View est utilise comme verdict ;
- le landmark BearDen est montre sans integration runtime verifiable ;
- le handoff Builder-A Wave5/BearDen ne contient pas `READY_FOR_PLAYER_UNITY_TEST=YES` ;
- BearDen n'est pas visible par defaut a `R05C02` dans une nouvelle session locale ;
- le premier clic masque ou modifie le terrain, une autre entite, la minimap ou un autre panneau ;
- le second clic restaure BearDen a une autre ancre, une autre echelle ou avec une erreur superieure a `1 px` ;
- le bouton BearDen derive de plus de `1 px`, zoome avec la carte ou bloque le pan hors de sa hitbox ;
- un ours apparait hors evenement, ou la taniere a ete peinte dans le fond de carte ;
- un evenement, compte a rebours, recompense, attaque, appel serveur ou persistance est active par le bouton ;
- une phrase suggere un serveur live, un multi-serveur actif ou un monde immense termine.

Un refus ne doit pas etre contourne par recadrage, montage, fondu, capture d'Inspector ou retouche d'image.

## 9. Criteres de reussite

La demonstration proprietaire est recevable seulement si :

- le trajet Ruche vers `WorldMapMmoFullscreenFoundation` est visible ;
- l'identite runtime `25x25 / 625` correspond au master hash-locke ;
- la vue globale et les quatre coins sont tous montres ;
- le zoom proche preserve le detail et ne revele aucune couture ;
- le retour centre reproduit le cadrage initial ;
- le HUD reste fixe et la minimap reste lisible ;
- BearDen est visible par defaut a `R05C02`, se masque seul, puis revient au meme emplacement a `1 px` pres ;
- BearDen suit le terrain pendant pan/zoom dans l'etat visible, reste absent dans l'etat masque, et son bouton HUD reste fixe ;
- aucun ours, evenement ou effet serveur n'est visible ou actif ;
- les douze captures, la video V01 et leur manifeste sont complets ;
- les limites local/demo sont dites clairement.

## 10. Verdict de preparation

DEMO_B_PREPARATION_READ_ONLY=PASS

CURRENT_WAVE5_UNITY_INTEGRATION_VERIFIED_BY_BUILDER_A=YES

BUILDER_A_CANONICAL_INTEGRATION_REPORT_OBSERVED=PASS

BUILDER_A_WAVE5_READY_FOR_PLAYER_UNITY_TEST_OBSERVED=YES

OLD_WAVE3_5X5_ACTIVE=NO

BUILDER_A_PLAYMODE_CAPTURES_11_OF_11=PASS

BEAR_DEN_VISIBLE_HIDE_RESTORE=PASS

BEAR_VISIBLE=NO

OWNER_UNITY_DEMO_NOW=READY_TO_LAUNCH_LOCAL_PLAYER_PATH

READY_FOR_OWNER_UNITY_DEMO_WHEN_INTEGRATED=YES

READY_FOR_OWNER_UNITY_DEMO_NOW=YES
