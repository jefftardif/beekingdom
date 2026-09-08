# M074-CL — Chat Royal, bulle rouge ronde pour les non-lus

Le badge de compteur de messages non lus était un petit rectangle premium.
Remplacé par une bulle rouge ronde (texture procédurale `unread-bubble`,
même famille que `avatar-circle`), comme sur la maquette.

S'applique aux 6 canaux (boucle de dessin partagée dans
`DrawChatChannelsPane`) et aux badges de la liste des discussions
(`DrawChatConversationsPane`).

- Fichier : `HiveViewProductUiPresenter.cs`.
- Compilation vérifiée propre.

Commit local uniquement, aucun push.
