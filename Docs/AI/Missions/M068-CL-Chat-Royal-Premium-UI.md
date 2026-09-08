# M068-CL — Chat Royal Premium UI

## Refonte visuelle

Basé sur la référence fournie par le CEO :

- Tailles de police nettement augmentées partout : bannière/titre, barre
  d'action, canaux, discussions, en-tête de conversation, bulles de
  messages, auteur, heure, statut, composer, bouton Envoyer, sélecteur de
  joueurs, panneau Membres. Confortable à lire en 1920x1080.
- Bouton "Envoyer" reconstruit en bouton premium (au lieu du bouton IMGUI
  par défaut).

## Correctif "Membres"

Le bouton "Membres" vivait collé au badge de statut serveur en haut à
droite de tout l'écran - déconnecté visuellement de la conversation, comme
signalé. Il est maintenant dans l'en-tête de la conversation elle-même,
comme sur la référence.

Sur desktop, le clic ouvre/ferme un vrai panneau "MEMBRES (n)" ancré à
droite (4e colonne), pas un modal plein écran - exactement la structure de
la référence. Sur mobile (espace insuffisant pour 4 colonnes), le même
bouton ouvre encore le modal plein écran existant.

Les deux partagent désormais le même contenu factorisé
(`DrawChatGroupMembersBody`) : liste des membres, ajouter, exclure (avec
confirmation M067-CL), transférer chef, quitter - tout branché sur les
mêmes fonctions `LivingHiveChatRuntime` déjà fonctionnelles. Aucune
logique recréée.

## Non touché

Backend, protocoles Chat, connexion serveur, création conversation
privée/groupe, envoi/réception, résolution DisplayName, logique de
permissions - uniquement la présentation et le branchement du panneau
Membres existant.

- Fichiers : `HiveViewProductUiPresenter.cs`,
  `HiveViewProductUiPresenter.ChatRoyal.cs`.
- Compilation vérifiée propre (0 erreur). Aucun test, aucun refactor
  d'architecture.

Commit local uniquement, aucun push.

READY FOR CEO PREMIUM CHAT UI RETEST
