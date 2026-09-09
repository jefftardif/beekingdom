# M075-CX — Système réutilisable des chutes

## Cause
Le prototype dépendait d'une seule scène et d'une seule chute codée dans le composant.

## Implémentation
Le composant expose maintenant des `WaterfallDefinition` sérialisées : identifiant, `worldRect`, échelle, orientation, direction du flux et opacité. Le catalogue `WaterfallCatalog.json` contient la chute de référence et quatre autres entrées cartographiques ; le runtime commun conserve la même caméra orthographique, RenderTexture, shader, animation, feathering et compositing OnGUI. Les définitions sont donc transférables vers la future World Map sans replacer chaque chute dans la scène.

## Vérification
Unity Play Mode : compilation sans erreur, shader sans erreur, chute de référence inchangée visuellement, 1 renderer runtime, aucun sommet hors caméra et aucun pixel opaque sur le bord de RenderTexture. Console Unity : aucune erreur. Vérification visuelle effectuée au zoom de référence.

## Limites
Les quatre coordonnées secondaires sont les premières entrées du catalogue et devront recevoir une validation CEO zone par zone avant déploiement production. Aucun gameplay ni terrain n'a été modifié.

## Acceptation
- [x] Référence visuelle conservée.
- [x] Configuration des chutes externalisée en données.
- [x] Paramètres de placement, dimension, orientation et flux présents.
- [x] Rendu commun caméra/RT/shader/animation/OnGUI conservé.
- [x] Compilation et validation Play Mode ciblées.
- [x] Commit local à effectuer.
