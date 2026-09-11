# M074-CX — Cascade adaptée au décor

## Ajustement supplémentaire — 2026-09-09
À la demande du CEO, opacité de scène passée de 0,82 à 0,92 (+12 % environ). Valeur appliquée et relue sur le matériau en Play Mode ; seul le paramètre de scène change, sans modification du contour ni du shader. Appréciation visuelle finale par le CEO.

## Cause
Les rideaux 3D répétés se chevauchaient avec des bords internes opaques et un cadrage perspective tronqué. Leur rectangle ne correspondait pas à la silhouette peinte ; la composition alpha successive atténuait également le mouvement.

## Fichiers modifiés
- `Assets/BeeKingdom/Playground/WorldMapWaterfallFxBootstrap.cs`
- `Assets/BeeKingdom/Playground/Resources/WaterfallFX/WaterfallMapSurface.shader` et `.meta` généré par Unity.
- `Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity` : seulement les paramètres du composant cascade par rapport à l'état reçu.
- Ce rapport et `Docs/Claude/Claude_Continuation.md`.

## Implémentation
Un seul maillage runtime suit les deux bras naturels, avec UV continus et bords fondus, utilisant les deux textures de l'asset acheté. Capture orthographique 640×429 et alpha appliqué une seule fois ; anciens prefabs conservés dans la scène et désactivés uniquement en Play Mode.

Sur retours du CEO : intensité enregistrée à 0,82 avec contraste renforcé ; bord supérieur du bras droit abaissé pour exclure la rivière entourée en rouge. Images du terrain et contenu du pack Tazo inchangés.

## Vérification
Compilation Unity 6000.5.3f1 jeu/éditeur terminée, shader sans message d'erreur. En Play Mode : 1 renderer, 169 sommets, 264 triangles, aucun sommet hors cadrage et aucun pixel opaque au bord de la capture ; inspection à zoom 1,6 et 0,8 avec déplacement.

Animation capturée dans `M074-CX-Waterfall/cascade.mp4`. `git diff --check` du C# passe ; les espaces YAML signalés sur le diff global de scène préexistaient à cette reprise (delta propre de scène : deux anciens paramètres remplacés par l'opacité).

## Points ouverts
Validation esthétique finale par le CEO et performances Android non mesurées. Une erreur de compilation du script temporaire de capture a été corrigée ; aucun script de capture ajouté aux assets du jeu.

## Acceptation
- [x] Bandes rectangulaires supprimées, terrain préservé.
- [x] Intensité renforcée et persistante après redémarrage.
- [x] Limite supérieure abaissée dans la zone signalée.
- [x] Compilation et vérifications ciblées réalisées.
- [ ] Approbation visuelle finale du CEO.
