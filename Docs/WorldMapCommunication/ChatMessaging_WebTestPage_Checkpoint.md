# Bee Kingdom - ChatMessaging Web Test Page Checkpoint

Date: 2026-07-16
Scope: page web simple pour tester le chat live. Aucun changement Unity, PNG, Wave5, BearDen ou APK.

## URL

`https://chat.dravii.com/test-chat/`

## Comptes tests

Parent:

- Email: `parent.chat.test@dravii.com`
- Password: `BeeChat-Parent-2026!`
- PlayerId: `6239a736-786a-401a-8b43-883249f4a5cc`

Fils:

- Email: `fils.chat.test@dravii.com`
- Password: `BeeChat-Fils-2026!`
- PlayerId: `7bd4d987-ab1b-423f-bada-2cbf4db98634`

## Livraison

- Page locale source: `Server/artifacts/chat-test-page/index.html`
- Page serveur: `C:\inetpub\BeeKingdom.ChatTest\index.html`
- IIS application enfant: `/test-chat`
- Site parent: `BeeKingdom.ChatApi`
- URL publique: `https://chat.dravii.com/test-chat/`

## Checks

- Page web: 200.
- Login parent: OK.
- Login fils: OK.
- Creation conversation privee: OK.
- Envoi parent vers fils: OK.
- Lecture fils: OK.

## Notes

- La page est temporaire et contient des comptes tests dedies.
- Le chat live reste servi par l'API `https://chat.dravii.com`.
- Le polling REST est utilise pour simplifier le test manuel.
