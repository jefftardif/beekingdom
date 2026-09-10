# M074-CX — Prototype One Click Add Water

## Inspection

L’asset contient un Shader Graph URP, `Resources/water.mat`, normales, foam, réflexion/réfraction et des paramètres de vagues (`_WaveSpeed`, `_Speed`, `_Wave_Direction`). La direction existe dans le shader ; elle peut donc être évaluée sur une portion de rivière. Le matériau de démonstration est opaque par défaut (`AlphaMode` opaque), ce qui limite le raccord naturel avec une image de fond.

## Prototype

`WorldMapOneClickWaterPrototype.cs` couvre maintenant deux zones contrôlées : une portion de rivière avec courant diagonal et un bassin avec ondulation lente. Chaque zone a sa propre caméra, RenderTexture, vitesse, direction et opacité ; aucune modification du système de chutes.

## Validation

Le projet compile et le matériau Pro est trouvé/instancié en Play Mode. Le rendu Editor a signalé des erreurs `Screen position out of view frustum` dans `ESSW.Editorcontroller.PlanarReflections` ; elles viennent du module de réflexions de l’asset et doivent être vérifiées pendant le retest avec la carte chargée. Aucune généralisation aux autres eaux n’a été faite.

## Limitation

Le shader expose une direction de vague, mais son matériau opaque ne fournit pas encore un masque de berge adapté à l’image peinte. Il faut décider visuellement si cette opacité faible suffit avant d’envisager un masque ou une intégration plus large.

## Acceptation

- [x] Inspection asset et paramètres.
- [x] Un seul prototype, hors système des chutes.
- [x] Caméra/RenderTexture/OnGUI et pan/zoom codés.
- [x] Compilation Unity sans erreur.
- [ ] Retest CEO de la rivière et du bassin en Play Mode avec terrain chargé.
