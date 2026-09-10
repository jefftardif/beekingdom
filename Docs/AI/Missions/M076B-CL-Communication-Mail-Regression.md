# M076B-CL — Régression Communication : onglet MAIL inaccessible

Date : 2026-09-10

## Cause

`DrawChatTopBar` (`HiveViewProductUiPresenter.cs`), ajouté par le redesign
Chat Royal (M070-CL, "CHAT / MAIL / Paramètres / Fermer" — la rangée réellement
visible et cliquée par le CEO en jeu), dessinait ses rects `mailTab`/`chatTab`
avec `DrawPremiumPanel` + `GUI.Label` uniquement — jamais enveloppés dans un
`GUI.Button`, contrairement à `gearButton`/`closeButton` juste à côté qui,
eux, avaient un vrai clic. Le bouton MAIL de cette rangée était donc purement
décoratif : cliquer dessus ne faisait rien.

L'ancienne paire d'onglets fonctionnelle (`DrawCommunicationTabBarForExternalHost`,
centrée en haut de l'écran, sous le bandeau) appelait bien
`OpenMailOverlayForExternalHost()`/`SwitchToChatFromMailForExternalHost()`,
mais elle est visuellement discrète et n'est pas celle que le CEO regarde ni
clique — d'où l'impression que "MAIL ne s'ouvre plus".

## Correction

Une seule zone touchée : les deux `Rect` `mailTab`/`chatTab` dans
`DrawChatTopBar` reçoivent maintenant un vrai `GUI.Button` (même pattern que
`gearButton`) appelant les fonctions réelles déjà existantes
(`OpenMailOverlayForExternalHost()` / `SwitchToChatFromMailForExternalHost()`),
plus une coloration active/inactive selon `courierScreenOpen` (au lieu de
couleurs figées). Aucune autre fonction, aucun autre écran, aucun système
Combat Patrol/PvE touché. L'ancienne paire d'onglets centrée n'a pas été
modifiée (elle fonctionnait déjà et reste le chemin de retour testé).

## Validation Play Mode (scène réelle `Environment2D5D_HiveMap_Test`)

Communication → Ouvrir (chat plein écran) → clic réel sur le bouton **MAIL**
de la rangée du haut → Boîte de réception affichée, liste de mails visible
(dont plusieurs "Rapport de combat") → ouverture d'un rapport de combat
réussie → retour à **CHAT** (onglet centré existant) → Chat Royal toujours
fonctionnel (canaux, messages, discussions intactes). Aucune nouvelle erreur
console.

## Compilation

`assets-refresh` propre avant et après le correctif.

## Commit

Commit local uniquement, aucun push.

## READY FOR CEO MAIL RETEST
