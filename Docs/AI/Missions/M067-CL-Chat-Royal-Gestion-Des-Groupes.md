# M067-CL — Chat Royal : Gestion des groupes

## Diagnostic

La quasi-totalité des 8 comportements demandés étaient déjà fonctionnels,
hérités des correctifs M065/M066 (scope serveur, format d'ID, résolveur de
nom serveur) et de l'implémentation existante de `DrawChatGroupMembersOverlay` :

- Membres affichés par DisplayName uniquement (jamais le PlayerId) - déjà
  correct côté serveur (`ChatGroupOperations.DisplayName`).
- Ajout de membre, transfert de leader : déjà câblés sur le backend réel,
  avec mise à jour immédiate automatique (`RunGroupMutationAsync` rafraîchit
  toujours le détail du groupe après chaque mutation, lu en direct chaque
  frame).
- Indicateur de chef (couronne + libellé) : déjà présent.
- Persistance : `OpenChatGroupMembers` rafraîchit déjà depuis le serveur à
  chaque ouverture.
- Permissions : entièrement portées par le backend existant
  (`ChatGroupOperations`), rien recréé côté client.

Deux bugs concrets trouvés et corrigés :

1. **"Quitter" fermait l'écran même si le serveur refusait.** Le chef doit
   d'abord transférer le leadership (règle backend existante,
   `group_leader_must_transfer`) - mais le bouton fermait l'overlay sans
   attendre ni vérifier la réponse serveur, donnant l'illusion que quitter
   avait fonctionné.
2. **"Exclure" un membre n'avait aucune confirmation.** Un seul clic exclut
   immédiatement.

## Correctifs

- Nouveau `ChatLeaveGroupAndClose` : attend la réponse serveur, resynchronise,
  et ne ferme l'écran que si le groupe a réellement disparu de la liste des
  conversations ; sinon affiche un toast explicatif.
- Bouton "Exclure" : ajout d'une confirmation simple à deux clics (armé 5s),
  même motif que le bouton d'administration d'alliance déjà existant
  (`DrawAllianceMemberAdminActionButton`) - aucun nouveau système inventé.

Aucun système Chat parallèle créé. Chat privé non touché.

- Fichier : `HiveViewProductUiPresenter.ChatRoyal.cs`.
- Compilation vérifiée propre. Aucun test, aucun refactor.

Commit local uniquement, aucun push.

READY FOR CEO GROUP MANAGEMENT RETEST
