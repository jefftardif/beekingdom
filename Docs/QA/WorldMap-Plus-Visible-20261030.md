# BeeKingdom — menu « Plus » World Map (30 octobre 2026)

## Cause confirmée

Dans la scène `WorldMapWave6Wave5Method12288Preview`, le bouton Plus ouvre correctement `LivingHiveMenuCanvas.ToggleSharedPanel("More")`. L'état `SharedPanelId=More`, `PanelShown("More")=true`, `Canvas.enabled=true`, `CanvasGroup.alpha=1` et le panneau se trouve dans les limites de l'écran. Cependant, le rendu IMGUI de la World Map recouvre visuellement le Canvas uGUI. En désactivant provisoirement le script de dessin de la carte pendant un test, le panneau initial a été visible sur fond noir — cause confirmée sans modifier les données de jeu.

## Correction

`WorldMapSharedPanelForeground` dessine le menu **dans le dernier passage IMGUI de la carte**, avec priorité au premier plan. Il réutilise le contrôleur de menu existant; son Canvas uGUI originel est désactivé pour éviter les clics fantômes pendant l'ouverture. Fermeture par X, Échap ou clic en dehors du panneau.

- Entrées Plus : Armée et Paramètres transitent vers `LivingHiveMenuCanvas.SimulateMoreRowClick` et les callbacks déjà intégrés.
- Aide : conseils de navigation locaux; Support : adresse support@beekingdomgame.com avec option de copie.
- Sac : même mécanisme de premier plan, sans inventer de stocks serveur indisponibles.
- Les fonctionnalités de Chat, HUD, infirmerie, Alliance et pollinisation ne sont pas modifiées.

## Résultats réellement vérifiés sur PCJEFF

- Le panneau Plus apparaît visuellement **au-dessus du terrain qui est toujours présent**, sur capture de la vraie scène, après relance Play avec `wave6Provider.ManifestReady=true`, `HasLoadFailure=false`, `15/15` tuiles visibles.
- Fermeture : `SharedPanelId` vide, `SharedPanelOpen=false`, `Canvas.enabled=true`, raycaster réactivé et `WorldChatFeedVisibleForMap=true`.
- Compilation Unity 6000.5.3f1 sans erreur C# sur le composant.
- Tests Unity EditMode : **5/5 PASS**, dont les rectangles de popup pour 1920x1080, 390x844, 320x568 et la zone sûre 1024x768.

## Intégration Git

Branche ciblée : `fix/worldmap-plus-visible-20261030`, créée dans un worktree isolé depuis `main` d74d8aa9. Elle inclut le composant, ses tests et un hook minimal de fin d'OnGUI compatible avec cette base.

**Attention :** le worktree actif de PCJEFF contient encore des modifications non commitées du nouveau HUD World Map, notamment `EnsureSharedMenu` et le retour anticipé `SharedMenuOpen`. Le rendu réellement testé dans la scène active invoque `DrawFromMapOnGUI()` juste avant ce retour, pour garantir que le menu est dessiné après la carte. Ce hook propre au HUD en cours doit être conservé lors de la future réconciliation de la branche, sans écraser les autres changements.

La branche est un correctif de code prêt à réconcilier; elle ne prétend pas remplacer les modifications non commitées du HUD. Aucune API ni base SQL de production n'a été modifiée. Le correctif est testé **localement dans Unity**, pas encore vérifié dans une compilation Android.
