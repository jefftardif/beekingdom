# M072-CL — Chat Royal, intégration des icônes premium

## Intégration

Les 6 PNG officiels remplacent l'icône procédurale + le socle rond dans la
colonne CANAUX. Le cadre premium faisant déjà partie de chaque asset, aucun
socle n'est redessiné par-dessus - juste `GUI.DrawTexture(..., ScaleMode.ScaleToFit)`
pour conserver le ratio.

Détail technique nécessaire : les PNG fournis dans `Assets/Art/UI/RoyalChat`
n'étaient pas chargeables au runtime depuis cet emplacement (`Resources.Load`
exige un dossier `Resources`, seul mécanisme de chargement de texture déjà
utilisé partout ailleurs dans cet écran). Déplacés vers
`Assets/BeeKingdom/Playground/Resources/RoyalChatIcons/` (même noms de
fichiers), à côté des autres dossiers d'icônes du jeu (`PremiumBeeIcons`,
`UI031Icons`). Un filet de sécurité (ancien rendu procédural) reste en place
si un asset venait à manquer.

## Cartes CANAUX agrandies

Hauteur de carte portée à ~74-100px (desktop) / 84px (mobile) selon la
largeur de colonne, icône jusqu'à 58-64px, texte recentré verticalement -
présence visuelle nettement plus proche de la maquette.

## Non touché

Backend, données, autres écrans. Seule `DrawChatChannelsPane` a été modifiée
côté présentation, plus l'ajout du chargeur de texture dédié.

- Fichier : `HiveViewProductUiPresenter.cs`.
- Compilation vérifiée propre (0 erreur), textures confirmées importées
  correctement (1254×1254, RGBA32).

Commit local uniquement, aucun push.

READY FOR CEO ICON RETEST
