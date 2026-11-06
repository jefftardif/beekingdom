# BeeKingdom — World Map : zoom et occupation (6 novembre 2026)

## Périmètre et sécurité

Copie Unity isolée : `C:\BeeKingdom\staging\BK-HudChat-Reconcile-20261105`.
Branche : `integration/bk-hud-chat-20261105`.
`main` local (`d74d8aa9`) conserve ses 53 fichiers modifiés préexistants.
Aucun changement SQL, API, serveur de production, build Android ou fusion `main` durant ce lot.
Les ressources graphiques non suivies dans Git doivent être restaurées depuis une source autorisée.

## Zoom — corrigé dans la branche d'intégration

Symptôme : la caméra animait `currentZoom` (amortissement 10) et
`currentWorldCenter` (amortissement 14) indépendamment. Cela faisait glisser
le point du monde sous le curseur pendant le zoom, même si les icônes avaient
des coordonnées mondiales constantes.

Correctif dans le commit `53ab6002` :
- La caméra interpole `center * zoom` et `zoom` avec la même progression,
  tout en conservant l'amortissement original pour le pan sans zoom.
- Les zooms répétés recalculent le point du monde depuis la caméra visible.
- Aucun `WorldCoord` d'entité, aucune réservation serveur ni état SQL modifié.

Gates Unity 6000.5.3f1 sur PCJEFF :
- `BK-Zoom-Anchor-EditMode-20261106.xml` : 4/4 PASS.
- `BK-Zoom-Anchor-ScenePlayMode-20261106.xml` : 1/1 PASS, scène officielle,
  28 frames de contrôle d'ancrage en temps réel.
- `BK-WorldOccupation-Visual-Separation-20261106.xml` : 9/9 PASS,
  règles isolées de séparation et réservation existantes.
- `BK-PlayMode-Menu-After-Zoom-20261106.xml` : 1/1 PASS,
  y compris le test des 12 cycles PLUS/Sac.
- `BK-Zoom-Anchor-Asset-Contract-20261106.json` : 5 685/5 685
  empreintes SHA-256 conformes, 7 633 métadonnées contrôlées,
  aucun GUID en double, aucun fichier manquant.

**Limite :** tests de caméra et d'état Unity, pas encore d'inspection
pixel par pixel de la GameView ni de validation tactile Android.

## Occupation réelle — défaut persistant, mesuré en lecture seule

Audit : `WorldMapOccupationReadOnlyPlayModeAudit` dans la scène officielle.
Il passe quand la collecte de preuves réussit; le PASS **ne veut jamais dire**
que la carte est sans conflits.

Rapport `BK-World-Occupation-ReadOnly-20261106.xml` (PCJEFF) :
- 65 noeuds visibles, dont 11 ressources d'eau;
- 22 croisements d'enveloppes estimés aux dimensions « mid-zoom »
  (non équivalents à une mesure d'encre de sprite pixel par pixel);
- 18 réservations historiques non résolues;
- la ruche `hive_player_test` entre en conflit avec
  `spider_next_to_hive` et plusieurs ressources proches;
- `useWave5Method12288PreviewRuntimePackageForPlayMode=False`,
  `runtimePlacementMaskLoaded=False`.
- Aucune validation hydrologique officielle dans cet audit : ne pas
  déclarer les points d'eau conformes aux rivières/lacs.

### Préflight de l'ancien masque

`tools/qa/verify_worldmap_placement_mask.py` inspecte, sans modifier,
`Assets/BeeKingdom/Playground/Resources/WorldMapRuntimePlacement/wave6_wave5method_12288_placement_mask.json`.

Sur 2 500 entrées :
- 822 coordonnées d'eau hors de [0,1];
- 516 coordonnées de ruche hors de [0,1];
- 516 ancres de ruches empruntées malgré `hive_safe=true`;
- 1 475 paires ruche/menace utilisant le même point **dans les bornes**
  (1 991 si l'on inclut aussi les 516 entrées hors bornes);
- aucun secteur dupliqué.

Le contrôle `--strict` renvoie intentionnellement **2 / FAIL** pour ce
masque. `test_verify_worldmap_placement_mask.py` : **7/7 PASS**.
Rapport sur PCJEFF : `C:\BeeKingdom\qa\BK-WorldMap-PreviewMask-Consistency-20261106.json`.

**Important :** un masque géométriquement valide ne prouverait pas, à lui
seul, qu'un point est sur la terre ou dans l'eau. Le masque actuel est
un aperçu visuel, pas une autorité de placement.

## Suite requise avant déploiement

1. Établir la source certifiée des polygones terrestres et hydrologiques,
   ainsi qu'une définition commune des enveloppes icône/étiquette/marge.
2. Valider côté serveur les réservations atomiques à la création,
   au mouvement et à la réapparition, sans altérer une position refusée.
3. Corriger les collisions héritées en coordonnées du monde avec des
   emplacements approuvés. Ne pas masquer des entités ni les déplacer
   seulement à l'écran selon le zoom.
4. Rejouer les tests du zoom, des zones cliquables et du HUD/Chat, puis
   vérifier la GameView et une véritable session tactile Android.

Aucune affirmation de « carte sans chevauchement » n'est autorisée
tant que ces quatre points ne sont pas vérifiés.