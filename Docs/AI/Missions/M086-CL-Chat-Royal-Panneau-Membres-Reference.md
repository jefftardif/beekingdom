# M086-CL — Chat Royal, panneau Membres fidèle à la référence

## Icônes intégrées

`membres.png` (bouton "Membres" de l'en-tête), `ajout_membres.png`
(bouton "Ajouter des membres"), `couronne.png` (remplace l'emoji 👑 pour le
chef, devant le nom ET devant "Chef").

## Panneau Membres reconstruit

- Boutons "Membres" et "Ajouter des membres" : plats arrondis + icône,
  comme les autres boutons premium déjà refaits (M080/M082).
- Lignes de membre : fond plat arrondi, avatar 40px, nom + rôle, couronne
  dorée pour le chef.
- Menu d'actions par membre (transférer/exclure) : remplacé le rendu
  "toujours visible" par un bouton **⋮** qui déplie les actions - fermé par
  défaut, comme la référence. Reste **dans** la ligne (pas en dessous) pour
  ne jamais chevaucher la ligne suivante dans la liste défilante.
- "QUITTER LE GROUPE" : bouton plat arrondi rouge/bordeaux avec bordure
  dorée, avis "transférez d'abord le leadership" en petite ligne au-dessus
  pour le chef (le bouton lui-même reste toujours cliquable, comme
  précédemment - la règle serveur est vérifiée après coup, M067-CL).

Aucune fonction retirée : ajouter, exclure (avec confirmation), transférer,
quitter passent toujours par les mêmes appels `LivingHiveChatRuntime` déjà
fonctionnels.

Non touché : backend, données, autres écrans.

- Fichiers : `HiveViewProductUiPresenter.cs`, `HiveViewProductUiPresenter.ChatRoyal.cs`.
- Compilation vérifiée propre, icônes confirmées importées.

Commit local uniquement, aucun push.
