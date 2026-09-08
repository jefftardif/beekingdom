# M070-CL — Chat Royal, reproduction fidèle de la maquette

Reprise du travail Codex (M069/M070-CX) resté incomplet. Corrections composant
par composant, sur la base de l'image de référence fournie.

## Changements concrets

- **Avatars vraiment ronds** (conversations, messages, panneau Membres) : les
  panneaux carrés sont remplacés par une texture procédurale cuite une fois
  (`avatar-circle`, fond sombre + anneau or), avec initiales par-dessus.
  Pastille de présence également ronde (`presence-dot`, était un carré blanc).
- **Icônes de canaux dans un socle rond** au lieu d'être posées nues sur le
  fond, comme sur la référence.
- **En-tête** : ajout des boutons Paramètres (⚙) et Fermer (✕) à droite des
  onglets CHAT/MAIL, manquants jusqu'ici.
- **Séparateur "Aujourd'hui"** au-dessus de la liste des messages.
- **Panneau Membres** : avatar rond par membre, rôle du chef affiché en
  "CHEF" gras doré (au lieu de "Chef"), bouton "QUITTER LE GROUPE" toujours
  en majuscules avec l'avis de transfert de leadership déplacé en petite
  ligne au-dessus au lieu de remplacer le libellé du bouton.
- Complète le travail déjà en place côté Codex (grand header, cartes de
  canaux/conversations avec icônes 36-40px, bulles gauche/droite, composer
  large avec bouton d'envoi doré ➤, panneau Membres ancré à droite).

## Non touché

Backend, protocoles, connexion, création conversation/groupe, envoi/
réception, résolution DisplayName, permissions - uniquement la présentation.

Aucun asset externe requis pour cette passe (icônes du jeu existantes +
textures procédurales déjà utilisées ailleurs dans l'interface premium).

- Fichiers : `HiveViewProductUiPresenter.cs`,
  `HiveViewProductUiPresenter.ChatRoyal.cs`.
- Compilation vérifiée propre (0 erreur).

Commit local uniquement, aucun push.

READY FOR CEO PIXEL-VISUAL RETEST
