# P17 - Fenetre florale masquee par WorldMap IMGUI

Date : 23 octobre 2026.
Portee : carte du monde Unity, panneau premium pollinisation.
Aucun changement SQL ou API necessaire.

## Defaut reproduit en vrai Play Mode
- SRVESDT : GET floral-sites renvoie deux sites pour un compte officiel.
- Unity : etat de presentation Ready, Sites=2; panneau premium IsOpen=true.
- Les 27 libelles TMP existent, le canvas ScreenSpaceOverlay est actif, des raycasts
  touchent les controles du panneau, mais la GameView affichait le monde sans la fenetre.
- En desactivant temporairement le seul MonoBehaviour WorldMap, la capture en jeu
  montre le panneau complet avec Champs, Essaims et Alliance; la session officielle
  etait toujours valide. Ce controle isole le rendu IMGUI du monde.

## Cause et correction
- Le OnGUI de WorldMapMmoFullscreenFoundationBootstrap dessinait le monde complet
  meme lorsque PollinationWorldMapOverlay.IsModalOpen etait true.
- GUI.enabled=false interdisait les boutons IMGUI mais NE SUPPRIMAIT PAS leurs
  instructions de dessin. Le dessin IMGUI du monde recouvrait le canvas royal uGUI.
- Correction : retour anticipe dans WorldMap.OnGUI tant que le panneau floral est ouvert.
  Conserver TickCourierAlertsForExternalHost dans cette branche pour ne pas perdre
  les recues de courrier/combats.
- En dehors de ce modal, l'ancien rendu de la carte reste strictement inchange.
- Les objets worldmap, le terrain, les assets de tuiles et la progression ne sont pas
  supprimes ni modifies. Unites et simulation continuent dans Update.
- Pendant le modal, la carte en dessous n'est pas redessinee : le panneau premium
  couvre la totalite du GameView (identique au test d'isolement de rendu).

## Recettes du correctif
- Branch integration/pollinisation-main-snapshot-20261023 :
  Pollination EditMode 11/11 PASS, WorldMap EditMode 8/8 PASS,
  aucune erreur de compilation du correctif.
- Unity principal PCJEFF : meme modification appliquee avec sauvegarde originale
  et verification SHA-256.
- Recharge de scripts / sortie puis entree Play : compilation Editor sans CS errors.
- ATTENTION : le redemarrage de Play a clos la session officielle precedemment active.
  L'essai d'affichage definitif avec worldmap component actif devra etre refait
  apres une nouvelle connexion officielle.

## Recette manuelle a terminer
1. Dans Unity, jouer, connecter son compte officiel.
2. Ouvrir WorldMap Wave6.
3. Verifier le bouton POLLINISATION en haut-centre.
4. Ouvrir Champs, verifier les deux sites dans une capture de GameView rendue.
5. Verifier les autres onglets, le retour vers la carte et FR/EN.
6. S'assurer que Population, Expeditions, AllianceProjects restent OFF en production.
7. Conserver le garde anti-mutations et la certification de deux joueurs distincts
   comme preconditions a une ouverture de l'economie.