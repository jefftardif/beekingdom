# M080-CL — Chat Royal, sélection et bulles corrigées

## Cause commune des deux problèmes signalés

Le halo de sélection (M077/M078-CL) et le masque de bulle arrondie
(M079-CL) utilisaient tous les deux une texture carrée pré-cuite étirée
(`ScaleMode.StretchToFill`) avec un rayon de coin défini en UV (fraction de
la texture). Sur une bulle de message très large et peu haute (message
court sur une seule ligne), cet étirement non uniforme déformait le coin en
gros bourrelet - exactement le "très saccadé et brouillon" rapporté. Pour
la sélection, le halo restait par ailleurs visuellement trop discret face
à la maquette (remplissage plat attendu, pas un simple anneau).

## Correctif

Remplacé par `DrawFlatRoundedRect`, qui utilise l'API `GUI.DrawTexture`
d'Unity 6 avec un rayon de coin en **pixels réels** (`borderRadiuses`) -
toujours net, quel que soit le rapport largeur/hauteur du rectangle :

- **Bulles de message** : rayon fixe de 10px, plus de déformation.
- **Canal/discussion sélectionné** : remplissage plat doré/brun + bordure
  vive, structure identique à la maquette (au lieu du halo/anneau
  précédent, jamais validé).

Les cartes non sélectionnées gardent le rendu "panneau premium" existant,
non touché.

- Fichier : `HiveViewProductUiPresenter.cs`.
- Compilation vérifiée propre.

Commit local uniquement, aucun push.
