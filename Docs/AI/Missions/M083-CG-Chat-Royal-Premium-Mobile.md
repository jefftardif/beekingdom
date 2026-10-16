# M083-CG — Chat Royal premium mobile

## Cause
Les dimensions fixes faisaient sortir les onglets du portrait, chevaucher recherche/contenu et comprimer les messages avec Membres ouvert. La barre d’actions ne réservait pas sa scrollbar ; le contraste du texte sur les bulles envoyées était d’environ 2,21:1.

## Fichiers modifiés
`Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`, `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.ChatRoyal.cs` et ce rapport. Rapports Chat Royal M068–M093 et implémentation actuelle examinés avant modification.

## Implémentation
Onglets adaptatifs, navigation mono-panneau sous 960 px, bannière réduite en paysage court, réservation correcte des espaces recherche/scrollbar. Membres reste dans l’en-tête, avec une ligne dédiée en portrait et un modal sous 1280 px ; les clics sous le modal restent bloqués.

Composer élargi, actions séparées, texte de saisie et messages à 16 px en compact ; fond miel assombri pour un contraste texte/fond d’environ 7,42:1 (couleurs nominales). Identité premium et branchements existants conservés ; aucun fichier de marches World Map modifié.

## Vérifications
Compilation des sources du worktree : jeu **0 erreur / 248 avertissements**, éditeur **0 erreur / 141 avertissements**, avec références Unity/dépendances déjà compilées de la copie locale, sorties dans `Temp/M083`. `git diff --check` valide.

Tests limités à `ChatRoyalSendWiringTests` et `LivingHiveChatLayoutTests`, lancés dans un hôte Unity 6000.5.3f1 isolé : **non exécutés**, démarrage interrompu par l’échec IPC du Package Manager après 30 secondes, confirmé au second essai. Aucun résultat de test réussi revendiqué.

## Points non résolus
Tests Unity à relancer après réparation du Package Manager. Recette visuelle mobile non certifiée : les PNG premium ne sont pas présents dans ce worktree.

## Recette demandée
- [x] Corrections limitées aux écarts de présentation identifiés ; compilation jeu/éditeur.
- [x] Marches World Map intactes ; travail sur branche de worktree, sans push ni merge.
- [ ] Vérifier 320×568, 390×844 et 844×390 : onglets, recherche, actions et saisie longue lisibles.
- [ ] Vérifier groupe réel : Membres, fermeture, invitations/actions existantes ; panneau ancré à partir de 1280 px en paysage.
- [ ] Relancer les deux classes de tests ciblées ; vérifier envoi, emojis et lecture seule dans le client.
