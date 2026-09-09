# M074-CX — Prototype One Click Add Water

## Inspection

L’asset contient un Shader Graph URP, `Resources/water.mat`, normales, foam, réflexion/réfraction et des paramètres de vagues (`_WaveSpeed`, `_Speed`, `_Wave_Direction`). La direction existe dans le shader ; elle peut donc être évaluée sur une portion de rivière. Le matériau de démonstration est opaque par défaut (`AlphaMode` opaque), ce qui limite le raccord naturel avec une image de fond.

## Prototype

Ajout de `WorldMapOneClickWaterPrototype.cs`, limité à un seul bassin horizontal près de la grande chute, sans modification du système de chutes. Le prototype utilise sa propre caméra et RenderTexture, applique une opacité faible, anime une direction diagonale douce et se projette par la même conversion monde/écran OnGUI que la carte. Il est créé automatiquement par la carte et reste désactivé hors viewport.

## Validation

Le projet compile sans erreur Unity et le matériau est trouvé/instancié en Play Mode. La carte a toutefois rencontré son état de chargement « carte indisponible » pendant la capture finale, empêchant une validation CEO fiable du raccord visuel du bassin. Le prototype est donc prêt pour le retest visuel, mais aucune généralisation aux autres eaux n’a été faite.

## Limitation

Le shader expose une direction de vague, mais son matériau opaque ne fournit pas encore un masque de berge adapté à l’image peinte. Il faut décider visuellement si cette opacité faible suffit avant d’envisager un masque ou une intégration plus large.

## Acceptation

- [x] Inspection asset et paramètres.
- [x] Un seul prototype, hors système des chutes.
- [x] Caméra/RenderTexture/OnGUI et pan/zoom codés.
- [x] Compilation Unity sans erreur.
- [ ] Retest CEO du bassin en Play Mode avec terrain chargé.
