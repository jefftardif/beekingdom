# M073-CL — Chat Royal, retrait des petites icônes emoji des canaux

Les noms de canaux (Alliance/Monde/Privé/Groupes/Système/Événements)
portaient un préfixe emoji (💬🌍👥📢📣🐝) redondant maintenant que la grande
icône ronde officielle (M072-CL) joue ce rôle. Préfixes retirés dans
`BuildChatChannels` et `EnsureChatGroupsChannel`.

Non touché : backend, données, autres écrans.

- Fichiers : `HiveViewProductUiPresenter.cs`, `HiveViewProductUiPresenter.ChatRoyal.cs`.
- Compilation vérifiée propre.

Commit local uniquement, aucun push.
