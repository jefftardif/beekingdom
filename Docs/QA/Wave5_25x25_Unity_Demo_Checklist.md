# QA-B - Wave5 25x25 Unity Demo Checklist

Date: 2026-07-14  
Statut: protocole QA-B en lecture seule; Builder-A reste le seul operateur Unity et QA-A conserve le verdict officiel.

## Reference attendue

- Scene canonique: `Assets/Scenes/WorldMapMmoFullscreenFoundation.unity` (`WorldMapMmoFullscreenFoundation`).
- Source Wave5: `master_25x25_12800.png`, `12800x12800`, SHA-256 `50F3FF9640251F365484F31DE4AA5AB542587381E5F8EEB9324D67BE37125913`.
- Grille logique: `25x25`, tuiles `512x512`, IDs exacts `R00C00` a `R24C24`, total `625`.
- Landmark taniere: ancre `R05C02`, master haut-gauche `(1280,3031)`; etat dormant.

## Preconditions

- [ ] Noter version/build, resolution et orientation; ouvrir la Console sans sauver de scene ni modifier asset, import, PNG ou `ProjectSettings`.
- [ ] Demarrer depuis la Ruche joueur en Play Mode local; aucune connexion serveur n'est necessaire.
- [ ] Creer un dossier de preuve horodate contenant captures, courte video de pan et releve JSON/texte; aucune retouche.

## Checklist executable

### 1. Ruche vers scene canonique

- [ ] Depuis la barre basse, activer `Monde`: la scene active devient exactement `WorldMapMmoFullscreenFoundation`.
- [ ] Refaire depuis l'entree laterale `Monde`: meme scene et meme bootstrap.
- [ ] Refuser tout affichage de l'ancien `ReferenceSurfaceMode.WorldBoundary`, preview integre ou ecran intermediaire obsolet.

### 2. Preuve 25x25 et inventaire 625

- [ ] Le diagnostic/manifeste runtime affiche `rows=25`, `columns=25`, `tile_count=625` et la source/hash Wave5 ci-dessus; aucune mention `5x5`, Wave3 ou ancien preview comme source active.
- [ ] Enumerer le catalogue: 625 IDs uniques, chaque paire `(row,column)` de `0..24` presente une fois, aucun trou, doublon ou ID hors borne.
- [ ] Pour chaque entree, confirmer PNG decodable `512x512`; si streaming, distinguer `625 cataloguees` du sous-ensemble charge sans exiger 625 GameObjects simultanes.
- [ ] Sonder et capturer ces neuf positions: `R00C00`, `R00C12`, `R00C24`, `R12C00`, `R12C12`, `R12C24`, `R24C00`, `R24C12`, `R24C24`.
- [ ] Atteindre visuellement au moins un coin `R24*` et un bord `*C24`; leur absence ou une limite a `R04/C04` prouve le retour de l'ancien 5x5.

### 3. Pan, zoom et transforms

- [ ] A zoom minimum, moyen et maximum, effectuer un pan lent horizontal, vertical et diagonal, puis pousser la camera contre les quatre bords.
- [ ] La camera s'arrete proprement: aucun hors-monde, noir, fallback, etirement, Repeat ou rebond montrant une zone invalide.
- [ ] Suivre simultanement un detail terrain net et une entite. Pendant le pan, leurs deltas ecran concordent a `2 px` pres; pendant le zoom, ils gardent le meme pivot et le meme facteur.
- [ ] Relever deux coins du HUD/minimap avant et apres chaque geste: translation maximale `1 px`, aucune mise a l'echelle perceptible, aucun chevauchement incoherent.

### 4. Continuite et absence d'artefacts

- [ ] Sur les neuf sondes et les pans, aucune couture, ligne droite, grille, bande, overlap, trou, tuile manquante, repetition, miroir, stamp ou flash visible.
- [ ] Aucun quadrillage debug, numero de tuile, ancien preview ou cache technique n'apparait dans la vue joueur.
- [ ] Aucun element runtime n'est peint dans le terrain: ruche, ressource, selection, HUD, icone, texte et trajectoire restent des overlays separes.
- [ ] Aucun chemin ou route terrestre ne sert de langage de deplacement; les vols restent aeriens.

### 5. Landmark taniere et bouton HUD

- [ ] Au premier affichage de la scene et au debut d'une nouvelle session locale, BearDen est visible par defaut autour de `R05C02`; relever son ancre monde et sa position ecran.
- [ ] Le bouton compact du HUD WorldMap est visible, n'utilise aucune image d'ours et reste fixe a `1 px` pres pendant pan et zoom.
- [ ] Premier clic: seul BearDen est masque. Terrain, ruches, ressources, selections, vols et HUD restent visibles et inchanges.
- [ ] Second clic, sans mouvement camera: BearDen reapparait au meme ancrage `R05C02` et a `1 px` maximum de sa position initiale.
- [ ] Apres un pan/zoom, BearDen suit le transform monde tandis que le bouton reste fixe; un nouveau cycle masquer/afficher ne change ni l'ancre ni l'echelle relative.
- [ ] Aucun ours, silhouette/ombre animale ou ours dans l'icone, le landmark ou le reste de la scene; aucune fumee active, combat, pulsation, compte a rebours, bouton d'attaque ou icone d'evenement actif.
- [ ] Aucun sentier, route, fleche au sol ou trait reliant l'entree; le petit parvis reste contenu dans le sprite.
- [ ] L'etat du toggle reste local a la session: aucune requete, mutation, persistance, activation serveur ou logique d'evenement; une nouvelle session revient a visible par defaut.
- [ ] Les SHA-256 des PNG terrain, des 625 tuiles et du sprite BearDen sont identiques avant/apres le test; aucune retouche ni reecriture PNG.

### 6. Non-claims

- [ ] La vue indique clairement `local/demo`, `donnees non officielles` et/ou `serveur live absent`.
- [ ] Aucun texte ne revendique monde live, evenement actif officiel, persistance serveur, matchmaking ou evenement multi-serveur reel.
- [ ] Toute donnee simulee reste etiquetee locale; aucun bouton ne declenche une mutation serveur.

## Paquet minimal a remettre a QA-A

- [ ] `E01`: navigation video/captures des deux entrees Ruche -> scene canonique.
- [ ] `E02`: inventaire 625 avec IDs, dimensions, hash source et compte des erreurs egal a zero.
- [ ] `E03`: neuf captures de sondes, non retouchees, avec coordonnee de tuile visible dans la preuve QA seulement.
- [ ] `E04`: video ou sequence pan/zoom avec mesures terrain-entite-HUD et preuves des quatre bornes camera.
- [ ] `E05`: trois captures appariees `visible par defaut -> masque -> restaure`, avec ancre `R05C02`, position et etat des autres couches.
- [ ] `E06`: sequence pan/zoom montrant bouton fixe, BearDen lie au monde et second cycle hide/show.
- [ ] `E07`: capture des non-claims, extrait Console sans erreur C# et preuve qu'aucune action serveur/evenement n'est emise.
- [ ] `E08`: liste des hashes PNG avant/apres, sans difference.

## Marqueurs BearDen attendus apres execution

Ces marqueurs sont une cible de sortie, pas un verdict de ce protocole. Ils ne peuvent etre emis avec les valeurs ci-dessous qu'apres compilation reelle de la scene Wave5 25x25 et execution complete par l'operateur Unity autorise:

```text
BEAR_DEN_VISIBLE_BY_DEFAULT=PASS
BEAR_DEN_TOGGLE_HIDE=PASS
BEAR_DEN_TOGGLE_SHOW=PASS
BEAR_DEN_TOGGLE_HUD_FIXED=PASS
BEAR_VISIBLE=NO
```

Ne pas revendiquer `READY_FOR_PLAYER_UNITY_TEST` sur la base de la preparation documentaire, du seul smoke de navigation ou des assets hors Unity. Ce marqueur reste interdit tant que la scene 25x25 n'a pas compile avec code `0` et ete testee en Play Mode.

## Arret et escalade

Consigner `FAIL` sur le controle concerne et transmettre a QA-A, sans fermer le gate, si: compte different de 625; grille autre que 25x25; coin/bord inaccessible; couture ou tuile absente; camera hors borne; transform terrain/entite divergent; HUD ou bouton mobile; ancien preview; BearDen absent par defaut; toggle affectant une autre couche; ancre modifiee; ours/route/icone active; changement PNG; logique serveur/evenement; ou claim live/multi-serveur trompeur.

QA_PROTOCOL_READY=YES
