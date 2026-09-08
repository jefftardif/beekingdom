# M093-CL — Chat Royal, émojis personnalisés en couleur (abeilles)

Intègre les 6 premiers émojis fournis
(`Assets/BeeKingdom/Playground/Resources/beemojis/` : bee_cry, bee_hangry,
bee_happy, bee_laugh, bee_love, bee_wow), avec le comportement décidé
ensemble : insertion par code texte `:nom_du_fichier:` (comme
Slack/Discord), rendu en couleur uniquement à l'affichage des messages
envoyés - pas de migration vers TextMeshPro.

## Ce qui a été construit

1. **Catalogue** : chargement de `Resources/beemojis/*.png` (nom de
   fichier = code), mis en cache.
2. **Sélecteur d'émojis** : les 6 images apparaissent comme cases dans la
   grille fusionnée (M092-CL), au même titre que les émojis unicode - clic
   → insère `:bee_happy:` dans le composer (texte brut, comme aujourd'hui
   en train de taper).
3. **Rendu des messages** : nouveau moteur "texte enrichi" - découpe le
   message en mots, un mot au format `:code:` correspondant à un fichier du
   dossier est dessiné comme une image inline (en couleur) au lieu du texte
   brut, avec retour à la ligne au niveau du mot. S'applique aux bulles
   (reçues et envoyées).
4. Les récents (déjà en tête de grille, M092-CL) fonctionnent aussi avec
   les émojis personnalisés - le code est mémorisé comme n'importe quel
   émoji, et redessiné en image dans la section "récents".

## Limite assumée (par choix, pas par contrainte technique)

Le composer (pendant la frappe) et l'aperçu de conversation (liste des
discussions) affichent le code littéral `:bee_happy:`, pas l'image - même
comportement que Slack/Discord pendant la frappe. Seul le message
effectivement affiché dans le fil de conversation montre l'image.

Non touché : backend, envoi, autres écrans.

- Fichier : `HiveViewProductUiPresenter.cs`.
- Compilation vérifiée propre, texture confirmée importée (512×512).

Commit local uniquement, aucun push.
