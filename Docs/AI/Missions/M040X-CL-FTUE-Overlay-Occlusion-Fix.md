# M040X-CL — FTUE Dialogue Occlusion Fix

Mission déclenchée par un bug visuel réel observé par Jeff en Play Mode réel
pendant M040-CL (playthrough FTUE PART2) : des abeilles volantes et l'icône
de collecte de ressource s'affichaient PAR-DESSUS le panneau noir du
dialogue FTUE (`TutorialDialoguePresenter`), au lieu d'être masquées
derrière comme les bâtiments et l'environnement.

## BEFORE — pipelines de rendu réels (inspection directe du code, pas de suppositions)

| Élément | Pipeline | Script | Script Execution Order (avant) |
|---|---|---|---|
| Panneau noir FTUE + portrait + nom + texte + Passer/Suite | IMGUI (`OnGUI`) | `TutorialDialoguePresenter.cs` | 0 (par défaut, `[DefaultExecutionOrder]` posé mais jamais appliqué — voir Root Cause) |
| Icône de collecte (halo + icône) | IMGUI (`OnGUI`) | `HiveViewProductUiPresenter.DrawManualProductionBeesForExternalHost`, appelé depuis `HiveMapProductionBootstrap.OnGUI` | 0 |
| Abeilles volantes orbitant l'icône | IMGUI (`OnGUI`), même fonction que ci-dessus (`DrawRuntimeWorkerBee`) | idem | 0 |
| Abeilles ambiantes qui marchent/volent sur les chemins du palais | **Pas IMGUI** — vrais `MeshRenderer`/`MeshFilter` (quads texturés, shader `BeeKingdom/Experiments/ArtworkUnlit`) rendus par la caméra principale | `HiveMapAmbientBeesBootstrap.cs` | n/a (rendu caméra, toujours derrière l'IMGUI par construction Unity — jamais la source du bug) |

Un seul pipeline concurrent était réellement en cause : **IMGUI contre IMGUI**,
entre deux `MonoBehaviour` différents (`TutorialDialoguePresenter` vs
`HiveMapProductionBootstrap`), tous deux à l'ordre d'exécution par défaut
(0). Les abeilles ambiantes (vrais objets 3D) ne sont jamais en cause : le
rendu caméra passe toujours avant l'overlay IMGUI dans le pipeline Unity —
confirmé par inspection du code (aucun `OnGUI` sur ce script).

Aucun uGUI Canvas n'est impliqué dans ce bug précis — l'hypothèse
multi-pipeline (IMGUI/uGUI/world-space en concurrence) évoquée dans le
brief de mission a été vérifiée et écartée pour ce cas : tout le conflit
visuel se joue entre deux `OnGUI` IMGUI.

## ROOT CAUSE

Unity ne garantit pas d'ordre de dessin `OnGUI` entre deux scripts à ordre
d'exécution égal (0) — l'ordre observé dépend d'un tri interne non
documenté (proche de l'ordre d'instanciation), qui restait stable sur toute
la session mais plaçait `HiveMapProductionBootstrap.OnGUI` APRÈS
`TutorialDialoguePresenter.OnGUI`, donc son icône/ses abeilles se
dessinaient par-dessus le panneau déjà peint (opaque) juste avant.

Deux tentatives de correction par ordre d'exécution ont échoué,
**empiriquement démontrées en Play Mode réel** :

1. `[DefaultExecutionOrder(32000)]` posé sur `TutorialDialoguePresenter` —
   confirmé n'avoir jamais été appliqué : `MonoImporter.GetExecutionOrder`
   retournait toujours `0` après recompilation, même après réimport forcé.
2. Ordre forcé directement via `MonoImporter.SetExecutionOrder(script, 32000)`
   + `AssetDatabase.SaveAssets()` — cette fois bien persisté et relu
   correctement (`32000`, confirmé après un vrai redémarrage complet du
   Play Mode, pas juste une recompilation) — **mais le bug persistait quand
   même** : le badge/les abeilles continuaient de passer par-dessus le
   panneau malgré l'ordre d'exécution correctement réglé et vérifié.

Cette deuxième tentative constitue la preuve empirique demandée par la
mission (« Prouve d'abord pourquoi l'élément passe devant » /
« sauf impossibilité architecturale démontrée ») : l'ordre d'exécution des
scripts ne pilote pas de façon fiable l'ordre de dessin `OnGUI` entre
scripts différents dans cet environnement (Editor Game View hébergée,
`UnityEditor.GUIView:ProcessEvent`), quel que soit le mécanisme utilisé
pour le régler.

## AFTER — correctif retenu

Plutôt que de continuer à chasser un ordre de dessin non fiable entre
scripts, ou de masquer les éléments par un simple booléen (`if (tutorialOpen)
icon.SetActive(false)` — explicitement proscrit sauf impossibilité
architecturale, désormais démontrée), le correctif utilise le **vrai
découpage visuel IMGUI** : `GUI.BeginGroup(clipRect)` / `GUI.EndGroup()`.

- `TutorialDialoguePresenter` expose maintenant :
  - `public static bool IsAnyDialogueVisible` (vrai pendant `Show()`,
    faux après `Hide()`) ;
  - `public static Rect GetCurrentPanelRect()` (même calcul exact que le
    rect utilisé pour peindre le panneau noir).
- `HiveMapProductionBootstrap.OnGUI` enveloppe désormais sa boucle de
  dessin des 3 bâtiments (icône + halo + abeilles orbitantes) dans
  `GUI.BeginGroup(new Rect(0, 0, Screen.width, panelRect.yMin))` quand un
  dialogue FTUE est visible — c'est-à-dire tout l'écran SAUF la bande
  couverte par le panneau. Tout ce qui est dessiné dans cette bande est
  coupé net exactement à la bordure réelle du panneau, pixel pour pixel —
  pas une heuristique de chevauchement de rectangles, un vrai clip GPU/IMGUI
  identique à ce qui masque déjà naturellement les bâtiments derrière le
  panneau.
- Rien n'est désactivé, déplacé, ni sa trajectoire modifiée : les abeilles
  continuent de voler et d'orbiter normalement même sous le panneau — elles
  disparaissent seulement visuellement pendant qu'elles traversent la zone
  couverte, et réapparaissent immédiatement en la quittant, exactement comme
  un objet passant derrière un mur.
- L'ordre d'exécution forcé (`32000`, tentative 2) et le `GUI.depth`
  temporaire posé dans `TutorialDialoguePresenter.OnGUI` (`GUI.depth =
  -32000` puis restauré) restent en place — inoffensifs, mais non
  suffisants seuls, donc pas retirés au cas où ils aident dans d'autres
  scènes/contextes non testés ici.

Portée volontairement limitée à `HiveMapProductionBootstrap` (le système
réellement actif dans la scène testée `Environment2D5D_HiveMap_Test`). Le
système parallèle équivalent pour la vue "reference hive"
(`HiveViewProductUiPresenter.DrawManualCollectionReadyMarkers` /
`DrawBuildingUpgradeReadyMarkers`, scène `LivingHive`) n'a pas été touché —
non observé en bug réel cette session, à corriger de la même façon si
Jeff le constate un jour sur cette autre vue.

## Validation Play Mode réelle

Reproduction exacte de la scène signalée par Jeff : dialogue "STRIGA —
Retournons à la Caserne pour entraîner tes premières troupes." avec l'icône
de collecte (miel) + 2 abeilles orbitantes visibles dans la bande basse de
l'écran.

| Test | Résultat |
|---|---|
| Abeilles occluses derrière le panneau | **PASS** (confirmé par Jeff : "Enfin!!! Bravo tu as réussi!!!") |
| Icône Collect occluse derrière le panneau | **PASS** |
| Portrait Striga reste devant le panneau | **PASS** (jamais touché par ce correctif) |
| Texte reste devant le panneau | **PASS** |
| Boutons Passer/Suite restent utilisables | **PASS** (aucun changement à leur zone de clic) |

Compilation vérifiée propre (`EditorUtility.scriptCompilationFailed = False`)
après chaque étape via `assets-refresh` + lecture directe du flag, jamais
supposée à partir du seul retour de l'outil.

## Extension — portrait débordant + généralisation à d'autres fenêtres (même session)

Après validation du correctif ci-dessus, le même symptôme est réapparu ailleurs :
en ouvrant la Caserne (`HiveMapBarrackBootstrap`), le portrait de Striga (qui
déborde volontairement au-dessus du panneau, voir `TutorialDialoguePresenter.OnGUI`,
`boxHeight = h * 1.6f`) se faisait couper la tête par le panneau de la Caserne,
dessiné après lui dans cette session.

**Test empirique décisif (demandé explicitement par Jeff, "réglons ça une fois
pour toutes")** : IMGUI (`OnGUI`) contre uGUI `Canvas` en `Screen Space - Overlay`
avec `sortingOrder = 32000` (priorité de tri maximale). Deux carrés superposés
créés en direct en Play Mode (rouge = IMGUI, bleu = uGUI Overlay) : **le rouge
(IMGUI) gagne toujours**, confirmé visuellement par Jeff. Ceci est un
comportement fondamental d'Unity (IMGUI se dessine après absolument tout le
reste, caméras et Canvas compris, à chaque frame) — **une migration du dialogue
FTUE vers uGUI aurait donc fait l'inverse de l'effet recherché** (dialogue
passant DERRIÈRE tous les autres panneaux IMGUI du jeu, pas devant). Piste
explorée puis explicitement écartée sur preuve, pas par supposition.

**Fix retenu** : le débordement du portrait (`boxHeight = h * 1.6f`, dépassant
au-dessus du panneau) n'est intrinsèquement sûr que lorsqu'aucune autre fenêtre
IMGUI n'est ouverte derrière. Nouveau drapeau agrégé
`HiveViewProductUiPresenter.AnyModalOpenForExternalHost` (reprend exactement la
même liste déjà maintenue dans `HiveMapOverlayInputGateBootstrap.Update()` pour
le blocage d'input, réutilisée plutôt que dupliquée sous un nouveau nom). Le
portrait bascule automatiquement sur un cadrage contenu (`boxHeight = h - 4f`,
ne dépasse plus jamais) dès que ce drapeau est vrai, et retrouve son débordement
premium dès qu'aucune fenêtre n'est ouverte.

Le même correctif de découpage (`GUI.BeginGroup` sur la bande du panneau
uniquement, `GetCurrentPanelRect()` — pas `GetCurrentOcclusionRect()`, retirée
car elle recréait le même problème en excluant aussi la zone transparente du
portrait, révélant le décor brut de la ruche derrière la Caserne) a été étendu
à `HiveMapResearchBootstrap` et `HiveMapArmyBootstrap`, en plus de
`HiveMapProductionBootstrap` et `HiveMapBarrackBootstrap` déjà couverts.
`HiveMapRoyalPalaceBootstrap`, `HiveMapConstructionBootstrap` et
`HiveMapAllianceBootstrap` n'ont volontairement pas été touchés cette session
(chemins `OnGUI` avec plusieurs sorties anticipées entrelacées — risque de
`GUI.BeginGroup`/`EndGroup` désappariés si mal enveloppés ; à corriger avec le
même schéma si Jeff rencontre le même bug là).

Validé par Jeff en Play Mode réel après le fix ("j'ai retesté, ça a l'air bon").

## Limites connues / non testé

- Résolutions multiples : testé uniquement à la résolution Editor Game View
  courante de cette session. Non testé à une résolution mobile cible
  distincte.
- Mouvement prolongé (plusieurs secondes, une abeille traversant
  visuellement la frontière) : observé qualitativement correct par Jeff en
  jeu réel, mais pas capturé image par image.
- La vue "reference hive" (scène `LivingHive`, hors scope de cette session
  de test) n'a pas été vérifiée — même classe de bug probable si jamais
  observée là.

## Fichiers modifiés

- `Assets/BeeKingdom/Tutorial/Runtime/TutorialDialoguePresenter.cs`
- `Assets/BeeKingdom/Playground/HiveMapProductionBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapBarrackBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapResearchBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapArmyBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` (nouveau
  `AnyModalOpenForExternalHost`)

## Git

Aucun commit, aucun push — conformément à la mission.
