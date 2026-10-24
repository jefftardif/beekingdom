# 58 — Recette P8 : PlayMode Unity, clics, FR/EN et carte

Date : 21 octobre 2026.
Branche : feature/pollinisation-alpha-20261021.
Statut : recette fonctionnelle PlayMode réussie, captures visuelles non disponibles.
Production SRVESDT et main Unity : inchangés, fonctionnalités OFF.

## Environnement

- PCJEFF, Unity 6000.5.3f1, copie de développement isolée
  C:\BeeKingdom\DEV\BK-Studio\staging\pollinisation-alpha-20261021.
- Deux tests UnityTest dans Assets/BeeKingdom/Playground/Editor/PollinationP8PlayModeTests.cs.
- Test Runner EditMode exécutant réellement EnterPlayMode puis ExitPlayMode.
- Résultat XML conservé hors du dépôt :
  C:\BeeKingdom\DEV\BK-Studio\qa\pollinisation-p8-20261021\pollinisation-p8-logged-playmode-v6.xml.
- Preuve finale : 2 tests réussis sur 2, 0 échec.

## Scénario A — fenêtre royale fonctionnelle en PlayMode

- Instanciation de PollinationPremiumWindow avec un contrôleur QA in-memory,
  sans session réelle, sans API et sans écriture économique.
- Deux sites, une population civile, une expédition vide et un projet collectif
  sont projetés avec les vrais contrats réseau et leurs validations.
- Les libellés fr-CA vérifiés comportent Trèfle blanc et Fertilité.
  Le changement vers en-US vérifie COOPERATION et Qualified members.
- Un Canvas de carte simulé est superposé sous le Canvas royal :
  EventSystem.RaycastAll confirme que le premier hit appartient à
  la fenêtre premium et non au bouton de la carte sous-jacent.
- Un ScrollRect unique est conservé lors du changement d'onglets.
  Un rafraîchissement sans changement de données ne déplace pas
  la position de la scrollbar et ne remplace pas le ScrollRect.
- La lecture et l'affichage ne déclenchent ni départ d'essaim civil
  ni création automatique de projet.
- La fermeture désactive le Canvas et son blocage de saisie.
- Les erreurs Unity autres que la seule annulation réseau connue du
  plugiciel externe com.IvanMurzak.Unity.MCP sont vérifiées
  par un collecteur statique de logs. Il n'y en a aucune dans
  l'exécution finale de ce scénario.

## Scénario B — scène réelle WorldMap avec flags désactivés

- EnterPlayMode s'exécute avant le chargement explicite de la scène
  WorldMapMmoFullscreenFoundation ; Unity Test Runner remettait
  autrement en place sa propre scène SandboxPlayground.
- Le vrai WorldMapMmoFullscreenFoundationBootstrap est présent.
- PollinationFeatureEnabledForWorldMap est faux et aucun panneau
  de pollinisation n'est ouvert.
- Aucun site floral n'est fabriqué côté client lorsqu'il n'existe
  pas de catalogue serveur officiel actif.

## Limites visuelles explicitement ouvertes

Les trois commandes de capture Unity ScreenCapture ont renvoyé
P8_FR_SCREENSHOT_EXISTS=False, P8_EN_SCREENSHOT_EXISTS=False et
P8_WORLDMAP_SCREENSHOT_EXISTS=False en batchmode.
Le lot prouve le comportement UI en PlayMode, PAS une validation visuelle
humaine de captures réelles, du placement tactile sur appareil, ni
du rendu complet sur les diverses résolutions.

Les trois rapports XML des campagnes PlayMode et les logs sont
archivés hors Git dans BK-Studio/qa/pollinisation-p8-20261021.
Les essais initiaux ayant échoué pour des problèmes d'instrumentation
sont conservés dans ce répertoire et ne sont pas comptés comme
réussites. Le test final est vert.

## État des preuves préexistantes

- P7 : compilation Unity du projet entier sans erreur C#.
- P7 : 8 tests Editor Pollination + 7 tests WorldMap réussis.
- P6 : 315 tests HiveOperations et 716 tests API/SQL réussis.
Ces contrôles précédents ne constituent pas une nouvelle exécution globale P8.

## Prochaines étapes

1. Inspection GameView interactive sans toucher à l'éditeur principal,
   captures réelles FR/EN, portrait, paysage.
2. Validation sur appareils des tap/swipe, hitboxes, scroll et tutoriel.
3. Staging SQL + vraie API à plusieurs comptes avant tout flag ON.
4. Audit d'intégration des commits P1-P8 avec modifications officielles
   de main, sans reset, push ni fusion destructrice.
## Réexécution transversale P8 (preuve finale supplémentaire)

Le Test Runner Unity 6000.5.3f1 a été relancé avec le filtre Pollination
sur la branche comportant les nouveaux tests et la remise à zéro de
LogAssert.ignoreFailingMessages dans TearDown.

Résultat XML : Passed, TOTAL=10, PASSED=10, FAILED=0, SKIPPED=0.
Cela couvre les quatre tests Editor P5, les quatre tests premium Editor P7
et les deux tests entrant réellement en PlayMode P8.

Le XML et le journal ont été sauvegardés hors du dépôt sous
C:\BeeKingdom\DEV\BK-Studio\qa\pollinisation-p8-20261021\
pollinisation-p8-combined-regression.xml.

Cette suite vérifie le comportement, les contrats visuels et l'interception
des clics sans brancher de compte réel ni ouvrir la pollinisation en production.
Les captures PNG et la recette visuelle humaine demeurent indisponibles en
mode batch, et les précédents tests serveur P6 ne sont pas recompilés ici.
