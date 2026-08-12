# WorldMap 50x50 Readiness - QA Matrix

Date locale: 2026-07-15

Role: QA-Relay independant, lecture des sources locales limitees.

## Sources locales lues

- `Docs/Recovery/BeeKingdom_Relay_Progress.md`
- `Docs/WorldMapRuntimeEntitiesWave1/WorldMap50x50Readiness_Report.md`
- `Docs/WorldMapRuntimeEntitiesWave1/MapReadingTools_Report.md`
- `Docs/WorldMapRuntimeEntitiesWave1/RuntimeEntitiesUnityIntegration_Report.md`
- `Docs/WorldMapRuntimeEntitiesWave1/HiveRuntimeProgressionIntegration_Report.md`
- `Docs/WorldMapRuntimeEntitiesWave1/ResourceInteractionStage_Report.md`
- `Docs/WorldMapRuntimeEntitiesWave1/BestiaryInteractionStage_Report.md`
- `Docs/WorldMapRuntimeEntitiesWave1/FinalVisualSmokeQA_Report.md`
- `Docs/WorldMapRuntimeEntitiesWave1/ProductionIntegrationContract.md`
- `Docs/WorldMapRuntimeEntitiesWave1/EntityMatrix.md`
- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/WorldMap50x50ReadinessProof/WorldMap50x50ReadinessProofReceipt.md`
- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/MapReadingToolsProof/MapReadingToolsProofReceipt.md`
- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/RuntimeIntegrationProof/RuntimeEntitiesProofReceipt.md`
- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/FinalVisualSmokeProof/manifest.md`

## Cadre QA

- Cette matrice valide une readiness locale/demo 50x50 logique, pas une release production.
- Aucun claim serveur, device, economie officielle, anti-cheat, publication, APK, remote, DNS/TLS/SQL ou donnee reelle.
- Le terrain visible reste Wave5 25x25. Aucun art terrain 50x50 n'est requis ni autorise pour ces gates.
- Les entites runtime sont overlays/logique locale; elles ne doivent jamais etre peintes dans le master terrain.
- PASS autorise seulement si la preuve locale indique PASS et si les budgets ci-dessous restent respectes.
- FAIL obligatoire si un test modifie les 625 tuiles Wave5, le master terrain, BearDen source, un APK, un asset PNG existant ou une scene hors portee.

## Budgets et seuils

| Domaine | Seuil PASS | FAIL si |
| --- | --- | --- |
| Catalogue logique 50x50 | 2500 coordonnees logiques disponibles en stress local | Catalogue incomplet, coordonnees instables ou dependantes des pixels terrain |
| Mode stress | Desactive par defaut | Active par defaut ou visible en experience normale |
| Active chunks | <= 25 au centre/densite, <= 9 sur bords NW/SE observes | Depassement > 25, fuite sur les bords, ou croissance non bornee |
| Textures terrain Wave5 cachees | <= 96 | Cache > 96 ou pollution par stress 50x50 |
| Chunk cache | Stable avant/apres stress | Taille augmente apres stress ou contient du 50x50 terrain genere |
| Ruches actives | <= 25 | Plus de 25 ruches actives dans la fenetre runtime |
| Ressources actives | <= 75 | Plus de 75 ressources actives dans la fenetre runtime |
| Bestiaire actif | <= 25 | Plus de 25 menaces actives dans la fenetre runtime |
| Allocation stress | <= 2 000 000 bytes | Allocation > 2 000 000 bytes pendant stress |
| Objets actifs proof | Densite observee <= 14 ruches / 40 ressources / 14 bestiaire | Toute densite active depasse les budgets globaux ou rend la carte illisible |
| Terrain 50x50 | Aucune generation art terrain 50x50 | Un PNG, tile, atlas ou master terrain 50x50 est cree pour ce gate |
| Serveur/officiel | ABSENT, `official_gain=false` pour demo | Gain officiel, persistence officielle, appel serveur ou remote |

Seuils observes dans les recus: catalogue 2500; catalog hives/resources/bestiary 725/3740/699; center active chunks 25; NW/SE active chunks 9/9; densest active chunks 25; densest hives/resources/bestiary 14/40/14; Wave5 cached textures 15; chunk cache 25/25; allocated bytes 0.

## Matrice P1-P5

| ID | Priorite | Zone | Test | Preuve attendue | PASS | FAIL |
| --- | --- | --- | --- | --- | --- | --- |
| QA-50-P1-001 | P1 | Stress catalogue 50x50 | Lancer le stress logique 50x50 local sur 2500 coordonnees | Recu readiness 50x50 | 2500 coordonnees, budgets PASS, aucun terrain 50x50 cree | Catalogue incomplet, generation terrain, budget FAIL |
| QA-50-P1-002 | P1 | Streaming/culling | Verifier centre, NW, SE et densite | Recu readiness 50x50 | Centre <= 25 chunks, bords <= 9, densite <= 25 | Fenetre active non bornee, fuite de chunks |
| QA-50-P1-003 | P1 | Pooling/cache | Comparer chunk cache avant/apres stress | Recu readiness 50x50 | Cache stable 25/25, textures Wave5 <= 96 | Cache grossit, stress pollue `chunkCache` |
| QA-50-P1-004 | P1 | Allocations | Mesurer allocation pendant stress | Recu readiness 50x50 | <= 2 000 000 bytes, observe 0 | Allocation > seuil ou mesure absente pour gate P1 |
| QA-50-P1-005 | P1 | Non-regression Wave5 | Verifier que Wave5 25x25 reste visible et preservee | Recu readiness + smoke QA | Terrain preserve, 625 tuiles non modifiees | Tuile manquante, asset Wave5 modifie, terrain masque par defaut |
| QA-50-P1-006 | P1 | BearDen | Verifier BearDen visible/cache/restaure sans remplacement | Smoke manifest | BearDen separe et restaure | BearDen source modifie/remplace ou regression visible |
| QA-50-P2-001 | P2 | Filtres | Activer/desactiver Ruches, Ressources, Menaces, BearDen | Recu Map Reading Tools | Filtres PASS, terrain non masque | Filtre masque terrain ou coupe mauvais overlay |
| QA-50-P2-002 | P2 | Recherche/selection | Selectionner le noeud le plus proche du centre carte | Recu Map Reading Tools | Statut proche coherent, selection PASS | Mauvais noeud, selection impossible, selection hors fenetre |
| QA-50-P2-003 | P2 | HUD | Pan/zoom avec HUD fixe | Recu Map Reading + FVS_05 | HUD fixe, interaction protegee par UI | HUD deplace avec carte, bloque la lecture ou declenche pan involontaire |
| QA-50-P2-004 | P2 | Legende | Verifier tiers/richesses R1-R3 et T1-T7 | Recu Map Reading | Legende compacte visible et coherente | Tiers ambigus, richesse illisible |
| QA-50-P3-001 | P3 | Collecte | Selectionner ressource, collecter localement | Recu Runtime Entities | Quantite diminue, collecte locale PASS | Quantite incoherente, collecte sur noeud invalide |
| QA-50-P3-002 | P3 | Epuisement | Epuiser une ressource et tenter recolte | Resource report + recu runtime | Collecte bloquee si epuise | Collecte possible a 0 ou UI ne signale pas l'etat |
| QA-50-P3-003 | P3 | Respawn | Attendre/forcer respawn demo deterministe local | Recu Runtime Entities | Quantite restauree, observe 129 -> 129 | Respawn non deterministe ou pretend officiel |
| QA-50-P3-004 | P3 | Lisibilite ressources | Verifier pauvre/moyen/riche sur Nectar, Pollen, Eau, Cire, Miel, Gelee royale, Propolis | Entity matrix + runtime report | Couverture R1/R2/R3, Eau et Miel presents | Ressource confondue avec UI/terrain ou type absent |
| QA-50-P3-005 | P3 | Feedback local | Verifier feedback selection/quantite/etat | Resource report + smoke proof | Selection, quantite, epuise/respawn visibles | Etat cache ou incomprehensible |
| QA-50-P4-001 | P4 | Bestiaire T1-T7 | Verifier couverture locale/demo T1..T7 | Bestiary report + recu runtime | T1..T7 coverage PASS | Tier absent ou mapping visuel incoherent |
| QA-50-P4-002 | P4 | Solo | Combattre cible tier bas en solo local | Recu runtime | Solo combat local PASS | Resultat non deterministe ou gain officiel declare |
| QA-50-P4-003 | P4 | Raid | Combattre T7 en raid local | Recu runtime | Raid local PASS, required/available/result visibles, `official_gain=false` | Mode raid absent, serveur implique, loot officiel |
| QA-50-P4-004 | P4 | Progression ruches | Tester niveaux 1/4/7/9, 10, 20/35/50 et classes | Hive report | H1/H2/H3 par niveau/classe, overlay faction separe | Deplacement ruche, sprite/faction fusionnes, classe illisible |
| QA-50-P4-005 | P4 | Densite centre/bords | Repeter lecture avec entites au centre, bord NW et bord SE | Receipts 50x50 + smoke edge | Aucun chevauchement critique, budgets actifs conserves | Densite masque terrain/HUD ou depasse budgets |
| QA-50-P5-001 | P5 | Accessibilite | Verifier couleur + symbole pour filtres et etats | P3 polish attendu | Couleur non seule porteuse d'information | Etat depend uniquement de la couleur |
| QA-50-P5-002 | P5 | Pan/zoom UX | Tester zooms courts et deplacements courts | Smoke QA + inspection locale | HUD stable, tuiles visibles, selection conservee ou reset explicite | Tuiles manquantes, selection fantome, HUD occlusif |
| QA-50-P5-003 | P5 | Non-regression Wave5 | Relancer smoke centre/bord apres P3 polish | Smoke proof | WAVE5_TERRAIN_REGRESSION=NO | Toute regression Wave5/BearDen/625 tuiles |
| QA-50-P5-004 | P5 | Contrat production | Verifier libelles demo/local et absence d'officiel | Production contract + reports | Client preview seulement, aucun gain officiel | UI ou log suggere etat officiel/persistence |
| QA-50-P5-005 | P5 | Nettoyage artefacts | Inspecter changements apres QA | Etat fichiers local | Seuls docs QA autorises modifies | Unity, PNG, APK ou assets modifies par QA |

## Scenarios obligatoires centre/bords/densite

| Scenario | Position | Donnees minimales | PASS attendu |
| --- | --- | --- | --- |
| Centre | Fenetre centrale runtime | 25 chunks actifs maximum, ruches/ressources/bestiaire visibles | Lecture claire, selection proche disponible, HUD fixe |
| Bord nord-ouest | Bord NW Wave5 | 9 chunks actifs observes, terrain conserve | Pas de tuile manquante, pas de fuite de chunks |
| Bord sud-est | Bord SE Wave5 | 9 chunks actifs observes | Pas de tuile manquante, pas de fuite de chunks |
| Densite | Fenetre la plus dense observee | 25 chunks actifs, 14/40/14 entites observees | Budgets hives/resources/bestiary respectes, overlays lisibles |
| BearDen | Etat visible/cache/restaure | BearDen separe des entites | Aucun remplacement source, retour visuel correct |

## Gates de sortie proposes

P1 peut etre declare PASS si QA-50-P1-001 a QA-50-P1-006 passent.

P2 peut etre declare PASS si P1 reste PASS et QA-50-P2-001 a QA-50-P2-004 passent.

P3 peut etre declare PASS si P1/P2 restent PASS et QA-50-P3-001 a QA-50-P3-005 passent.

P4 peut etre declare PASS si P1/P2/P3 restent PASS et QA-50-P4-001 a QA-50-P4-005 passent.

P5 peut etre declare PASS si P1/P2/P3/P4 restent PASS et QA-50-P5-001 a QA-50-P5-005 passent, sans modification Unity/PNG/APK et sans claim serveur/device.

## Verdict initial depuis sources lues

| Priorite | Statut QA-Relay | Justification courte |
| --- | --- | --- |
| P1 | PASS documentaire local | Readiness 50x50 PASS, budgets observes sous seuils, Wave5 preserve |
| P2 | PASS documentaire local | Filtres, recherche proche, HUD fixe, legende et terrain non masque PASS |
| P3 | READY_FOR_EXECUTION | Collecte/epuisement/respawn PASS en recu; polish visuel indique comme prochaine phase |
| P4 | READY_FOR_EXECUTION | T1-T7, solo/raid, ruches H1/H2/H3 PASS en recu local |
| P5 | READY_FOR_EXECUTION_WITH_NOTES | Smoke QA PASS_WITH_NOTES; accessibilite/polish final encore a verifier apres P3 |

## Template de verdict

Copier ce bloc pour chaque passe QA.

```text
WORLD_MAP_50X50_READINESS_QA_VERDICT

Date locale:
Auteur:
Portee:
Build/scene locale:

Claims exclus:
- Serveur/device/release/APK:
- Donnees reelles/economie officielle:
- Modification Unity/PNG/Wave5/BearDen:

Budgets observes:
- Catalogue logique:
- Active chunks centre/NW/SE/densite:
- Hives/resources/bestiary actifs max:
- Wave5 cached textures:
- Chunk cache avant/apres stress:
- Allocations stress:

Resultats:
- P1 stress catalogue/streaming/culling/pooling:
- P2 filtres/recherche/HUD/legende:
- P3 collecte/epuisement/respawn:
- P4 solo/raid T1-T7/progression ruches:
- P5 BearDen/pan-zoom/non-regression/accessibilite:

Defauts bloquants:
- 

Notes non bloquantes:
- 

Verdict final:
PASS / PASS_WITH_NOTES / FAIL / NOT_RUN

Signature QA:
```
