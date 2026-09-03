# Mission UI ciblee — Portrait de Championne dans le dialogue FTUE

Mission purement visuelle, distincte de M039 (Building Upgrade). Aucune
logique FTUE, aucun gameplay, aucun backend touche.

## Objectif

Remplacer l'initiale ("S") du panneau de dialogue FTUE par le portrait reel
de la Championne qui parle, de facon generique (pilotee par le `championId`
deja present dans le modele FTUE, pas par le texte affiche).

## Assets utilises (aucun asset genere)

- Striga : `Assets/BeeKingdom/Playground/Resources/WorldMapWave6Runtime/CombatMarch/ChampionMarchBody_striga.png`
  (1536x1024, PNG transparent, deja utilise par les visuels de marche de
  combat sur la carte du monde).
- Zephyra : meme dossier, `ChampionMarchBody_zephyra.png`.

Ecarte volontairement `Resources/PremiumBeeReference/ChampionBees/*.png` (les
icones utilisees par l'ecran "Abeilles championnes") : fond opaque
(scene de ruche en arriere-plan), incompatible avec l'effet "le personnage
depasse du panneau" demande — les artworks `ChampionMarchBody_*` sont
transparents et deja decoupes en pose dynamique, exactement adaptes.

Ambra, Aurelia et Nectaria n'ont pas d'equivalent `ChampionMarchBody_*`
aujourd'hui — le systeme retombe proprement sur le badge a initiale existant
pour tout `championId` sans portrait connu (jamais de crash ni de FTUE
casse).

## Mapping championId -> portrait

`TutorialDialoguePresenter.ChampionPortraitResourcePaths` (nouveau
dictionnaire statique, `StringComparer.OrdinalIgnoreCase`) :

```
"striga"  -> "WorldMapWave6Runtime/CombatMarch/ChampionMarchBody_striga"
"zephyra" -> "WorldMapWave6Runtime/CombatMarch/ChampionMarchBody_zephyra"
```

`ChampionPortrait(championId)` cherche dans ce dictionnaire, charge via
`Resources.Load<Texture2D>` (mis en cache dans un second dictionnaire
statique), retourne `null` si absent — c'est ce `null` qui declenche le
fallback vers l'ancien badge a initiale. Aucun test sur le texte affiche,
uniquement sur le `championId` deja transmis par le moteur FTUE
(`FtueStepDefinition.ChampionId` -> `Show(championId, text, onContinue)`).

## Mise en page

- Le personnage est ancre au coin inferieur-gauche du panneau et deborde
  au-dessus de son bord superieur (`portraitBox` avec `boxHeight = h * 1.6`,
  positionne pour que son bas colle au bas du panneau) — effet "guide integre
  a la scene" plutot qu'icone encadree.
- `GUI.DrawTexture(..., ScaleMode.ScaleToFit, true)` : aspect ratio
  preserve, transparence respectee, jamais de deformation. IMGUI ne
  capture aucun clic sur un simple `DrawTexture` (pas de `GUI.Button`
  dessus), donc le portrait ne peut pas voler un clic destine a
  Suite/Passer/au gameplay derriere le panneau — pas de `RaycastTarget` a
  gerer puisqu'il n'y a pas de Canvas UGUI ici, l'equivalent IMGUI est
  simplement de ne jamais englober le portrait dans un controle interactif.
- Nom de la Championne affiche en majuscules, gras, couleur accent
  (au-dessus du texte de dialogue, dans la colonne de texte — jamais dans
  l'image elle-meme).
- Largeur reservee pour le texte = `panel.width - largeurPortrait - marges`
  ; entierement recalculee a partir de `Screen.width`/`Screen.height`
  (`Mathf.Clamp(Screen.width * 0.30f, 170f, 300f)` pour la largeur du
  portrait, `Mathf.Min(180f, Screen.height * 0.28f)` deja existant pour la
  hauteur du panneau) — aucune position absolue, fonctionne a toute
  resolution/ratio.
- Correctif demande par Jeff en cours de validation visuelle : la ligne
  d'accent orange en haut du panneau traversait le portrait a travers ses
  zones transparentes (lisible comme un artefact). Deplacee pour ne
  commencer qu'apres la largeur reservee au portrait — elle ne longe plus
  que la partie texte/boutons du panneau.
- Deuxieme correctif demande par Jeff : le fond du panneau laissait
  transparaitre le texte de l'ecran derriere (menu "Connexion"/"Creer"
  visible a travers "STRIGA" et le dialogue). Cause : `GUI.Box` teinte la
  texture de style par defaut du skin, qui a sa propre alpha partielle
  bakee (bords/coins) — augmenter l'alpha du `GUI.color` ne suffisait pas a
  la rendre pleinement opaque. Remplace par un `GUI.DrawTexture` sur
  `Texture2D.whiteTexture` (remplissage plat garanti opaque), couleur
  assombrie (`0.05, 0.045, 0.03, 1`).

## Fichiers modifies

- `Assets/BeeKingdom/Tutorial/Runtime/TutorialDialoguePresenter.cs` — seul
  fichier touche.

## Validation Play Mode

Verification directe (reflexion via `script-execute`, sans passer par le
vrai flux FTUE pour ne pas perturber sa progression) : appel force de
`TutorialDialoguePresenter.Show("striga", ...)` puis `Show("zephyra", ...)`
sur l'instance reelle de la scene, capture d'ecran a chaque etape.

Resultat observe (confirme par Jeff en direct) :
- Portrait de Striga clairement visible, pose dynamique, deborde au-dessus
  du panneau depuis le coin inferieur-gauche.
- Visage non coupe (couronne et oeil visibles en zoom).
- Aspect ratio preserve, transparence correcte (arriere-plan visible entre
  les jambes/la cape).
- Nom "STRIGA" affiche distinctement au-dessus du texte de dialogue.
- Texte de dialogue parfaitement lisible a droite du portrait.
- Boutons Passer/Suite toujours accessibles, non masques.
- Meme test refait avec Zephyra : portrait different charge automatiquement,
  nom "ZEPHYRA" correct — confirme le mapping generique (aucun changement de
  code entre les deux, seul le `championId` differe).
- Correctif de la ligne orange applique et confirme visuellement par Jeff
  ("c'est parfait").

Aucune erreur de compilation, aucune exception console pendant les deux
recompilations (`assets-refresh` + verification `console-get-logs` avec
filtre Error, resultat vide a chaque fois).

## Limites connues

- Seules Striga et Zephyra ont un portrait premium aujourd'hui (les deux
  seules Championnes utilisees comme speaker dans le FTUE actuel). Ajouter
  Ambra/Aurelia/Nectaria au FTUE plus tard necessiterait soit un asset
  `ChampionMarchBody_<id>` equivalent, soit une entree de mapping vers un
  autre portrait existant — le fallback initiale reste actif entre-temps.
- La verification s'est faite en forcant l'affichage hors du vrai
  deroulement FTUE (pour ne pas modifier `FtueProgress`) ; le rendu du
  composant est identique quel que soit l'appelant, donc cela valide
  fidelement le comportement reel.

## Verdict

- **A.** Striga remplace-t-elle reellement le "S" ? **OUI**
- **B.** Le systeme choisit-il automatiquement le portrait selon la
  Championne (base sur `championId`, pas sur le texte) ? **OUI**
- **C.** Le portrait conserve-t-il son ratio et sa transparence ? **OUI**
  (`ScaleMode.ScaleToFit`, PNG transparent d'origine, aucune deformation)
- **D.** Le portrait est-il integre comme personnage-guide premium plutot
  qu'une petite icone ? **OUI** (deborde du panneau, taille proportionnelle
  a l'ecran, pose dynamique visible)
- **E.** Zephyra peut-elle utiliser le meme systeme ? **OUI** (verifie
  visuellement, meme code, mapping different)
- **F.** Les boutons et interactions FTUE fonctionnent-ils toujours ?
  **OUI** (Suite/Passer restent au meme endroit, portrait n'intercepte
  aucun clic — simple `DrawTexture`, jamais englobe dans un `GUI.Button`)

## Extension — Voix des Championnes (ElevenLabs), test de faisabilite

Demande de Jeff apres validation du portrait : faire parler les Championnes
avec leur voix ElevenLabs deja creee, en commencant par un test de
faisabilite sur la toute premiere replique de chacune.

### Convention de fichiers

Reutilise la structure deja en place pour les repliques generiques
(`ChampionVoiceBarkController` / dossiers `select`/`spawn`/`move`/`cit`),
avec une nouvelle categorie `ftue` — un clip par **ligne exacte** (contrairement
aux barks generiques, le texte du FTUE est fixe, donc pas de selection
aleatoire dans un dossier) :

```
Resources/PremiumBeeReference/ChampionVoices/{championId}/ftue/{championId}_{stepId}.mp3
```

Fichiers livres par Jeff et integres :
- `zephyra/ftue/zephyra_ftue.intro.welcome.mp3` (4.18s)
- `striga/ftue/striga_ftue.intro.barrack_intro.mp3` (5.15s)

### Implementation

`TutorialDialoguePresenter.Show(...)` accepte desormais un `stepId` optionnel
(nouveau parametre en fin de signature, `null` par defaut - retro-compatible).
`PlayVoiceIfAvailable` construit le chemin Resources a partir de
`championId` + `stepId`, charge et met en cache le clip, et ne fait
strictement rien si aucun fichier n'existe pour cette ligne precise (jamais
de crash, jamais de FTUE bloque en attendant un fichier absent). Un garde-fou
(`_lastVoicedKey`) evite de relancer la meme ligne si `Show()` est rappele
pour l'etape encore active ; reinitialise dans `Hide()` pour permettre une
relecture si le joueur revoit la meme etape plus tard.

`FtueTutorialBootstrap.UpdateVisuals` (8 sites d'appel, un par
`FtueStepKind` qui affiche un dialogue) passe desormais `step.StepId` a
`Show(...)`.

**Correctif applique en cours de test live** (rapporte par Jeff : "la
musique est trop forte, je n'entendais presque pas la voix") : le nouveau
code de voix FTUE n'appelait pas `MusicManager.DuckForVoice(...)`, contrairement
au systeme de barks existant qui le fait deja. Ajoute (`clip.length + 0.6s`
de tampon, memes constantes que `ChampionVoiceBarkController`) - la musique
s'attenue automatiquement pendant que la Championne parle.

### Validation

Testee en Play Mode reel (forcage via reflexion sur l'instance reelle de
`TutorialDialoguePresenter`, sans perturber la vraie progression FTUE) :
Striga et Zephyra toutes les deux entendues clairement par Jeff, volume de
la voix bien au-dessus de la musique atténuée. Confirmation directe : "je
les ai entendues toutes les deux" / "c'est bon".

### Limites connues

- Seules les deux toutes premieres repliques (une par Championne) ont un
  clip aujourd'hui - c'etait explicitement un test de faisabilite, pas un
  doublage complet. Le texte du FTUE n'est pas encore considere final par
  Jeff ("peut-etre qu'on devrait attendre les textes definitifs").
  Etendre le doublage a d'autres lignes se fait uniquement en deposant de
  nouveaux fichiers `{championId}_{stepId}.mp3` au bon endroit - aucun
  changement de code necessaire.
- Toute etape sans clip reste silencieuse (texte affiche normalement,
  simplement pas de voix) - comportement voulu, pas un bug.

### Incident sans rapport (signale et resolu pendant cette session)

Jeff a accidentellement glisse tout le dossier `Docs/` (racine du projet)
dans `tools/Docs/` via le trackpad de son portable — 608 fichiers trackes
par git apparaissaient "supprimes" (aucune perte reelle, tout etait intact
dans `tools/Docs/`). Diagnostique et corrige : dossier deplace de
`tools/Docs/` vers `Docs/` (racine), confirme par `git status` (plus aucune
suppression, uniquement les modifications/ajouts attendus de la session).
Aucun rapport avec le travail FTUE - mentionne ici uniquement pour la
tracabilite de session.
