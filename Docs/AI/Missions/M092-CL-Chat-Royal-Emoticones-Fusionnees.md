# M092-CL — Chat Royal, catégories d'émoticônes fusionnées

Les onglets de catégories (Récents, Smileys, Émotions, Gestes, Objets,
BeeKingdom) sont retirés. Toutes les émoticônes apparaissent maintenant
dans une seule grille défilante :

- Les récemment utilisées en premier.
- Ligne dorée de séparation.
- Le reste de toutes les catégories à la suite.

La grille peut désormais dépasser la hauteur visible du panneau et défile
(avant, chaque catégorie tenait seule dans la hauteur fixe).

Code d'onglets devenu mort retiré (`ChatEmojiTabOrder`, `ChatEmojiTabLabel`,
`ChatEmojiTabLabelCompact`, `ChatEmojiEmptyText`). Les points d'entrée
utilisés par les tests EditMode (`ChatEmojiCategoryForProof`,
`ChatEmojiCatalogEmojisForProof`, etc.) restent inchangés.

Non touché : backend, insertion d'émoticône, historique des récents,
autres écrans.

- Fichier : `HiveViewProductUiPresenter.cs`.
- Compilation vérifiée propre.

Commit local uniquement, aucun push.
