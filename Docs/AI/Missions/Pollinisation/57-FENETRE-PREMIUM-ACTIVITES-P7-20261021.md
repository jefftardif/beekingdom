# 57 — P7 : Centre de pollinisation premium (Activités v4)

Date : 21 octobre 2026. État : **branche isolée uniquement**, feature flags OFF, non déployé.

## Objectif et architecture

Faire évoluer la première fenêtre de pollinisation IMGUI P5 vers le modèle
**Activités v4**, sans refaire la carte, sans inventer un champ floral, sans
changer les règles de jeu ni détourner l'armée militaire.

**Nouveaux composants :**
- Assets/BeeKingdom/Playground/PollinationPremiumWindow.cs : toile uGUI
  plein écran avec GraphicRaycaster, CanvasScaler, protection de toute la surface
  contre les clics traversants, en-tête typographique Cinzel, fond royal,
  boutons Retour / Actualiser, conteneur SafeArea et scrollbar tactile dédiée.
- Assets/BeeKingdom/Playground/PollinationPremiumWindow.Cards.cs : trois
  onglets **Champs / Essaims / Alliance**, cartes réutilisant les matériaux
  premium de HiveMapPremiumBuildingWindow et les géométries publiques
  HeaderRectForProof, ScrollViewportForProof et ScrollTrackForProof.
- Assets/BeeKingdom/Playground/Editor/PollinationPremiumWindowEditorTests.cs :
  tests ciblés de la hiérarchie uGUI/TMP, du recouvrement, de la présence
  d'une seule scrollbar, des onglets, du bouton Actualiser, de la fermeture
  et de l'activation refusée hors session.

**Modifié seulement :**
- Assets/BeeKingdom/Playground/PollinationWorldMapOverlay.cs : le bouton d'entrée
  existant et les marqueurs P6 ouvrent la nouvelle fenêtre premium.
  L'ancien contenu IMGUI reste repliable uniquement si la nouvelle fenêtre
  n'est pas disponible. Lorsque le Canvas premium est ouvert, l'IMGUI ne
  dessine plus une seconde fenêtre. Les contrôleurs restent inchangés.

## Réutilisation précise d'Activités v4

Le composant P7 consomme les méthodes déjà approuvées de
HiveMapPremiumBuildingWindow : PremiumCanvasScale,
PremiumSafeCanvasRect, HeaderRectForProof, ScrollViewportForProof,
ScrollTrackForProof.

Il partage aussi les teintes royal-or, en-têtes, fond sombre, matériaux de
cartes et ressources de police Cinzel-Regular SDF. Aucune modification du
code de la fenêtre Activités v4 n'est nécessaire.

- Le Canvas royal est au sortingOrder 520 (Activités bâtiments : 420).
- Il utilise **un ScrollRect vertical** et une **Scrollbar** avec AutoHide.
- Les panneaux/onglets changent le contenu du ScrollRect, pas le ScrollRect
  lui-même. Le moteur ne réinitialise pas le scroll à chaque mise à jour
  de compteur ; les valeurs sont mises à jour sans modifier le texte
  lorsque celui-ci est déjà identique.
- Les cartes sont positionnées dans la zone sûre iOS/Android (encoche/barres
  système) via le layout officiel.
- Aucune UI sous-jacente ne doit recevoir des clics : Backdrop uGUI avec
  RawImage raycastTarget et blocages de la carte du monde P6.
- Les trois onglets FR/EN lisent la locale officielle BeeLocalization.

## Autorité gameplay

Le composant P7 n'introduit ni méthode d'autorité nouvelle, ni ressource
locale : IPollinationPanelController et PollinationClient P5 récupèrent
le stock mondial, les effectifs civils, la mission en cours, les délais,
le résultat et les contributions d'alliance directement du serveur.
Le bouton Actualiser est une lecture à la demande, jamais un retry
automatique d'une écriture POST incertaine.

Les actions restent :
- choisir un champ officiel ;
- préparer le nombre de butineuses dans les bornes confirmées ;
- lancer une mission civile en mode équilibré (si autorisée) ;
- demander une cohorte civile (si autorisée) ;
- voir la contribution collective et, pour un chef/officier, lancer un projet
  dans la limite des règles SQL du P4.

Les champs floraux QA sont une démonstration désactivée, pas des sites
communs disponibles en production.

## Vérifications Unity réelles dans le worktree

- Compilation complète du code BeeKingdom Unity 6000.5.3f1 : **0 erreur C#**.
- Suite Unity Editor P7 premium ciblée : 4/4 réussis, 0 échec.
- Suite Unity Editor combinée filtrée Pollination :
  **8/8 réussis** (P5 + P7), 0 échec.
- Suite de non-régression de la WorldMap filtrée : **7/7 réussis**.
- Côté serveur, dernier jalon P6 conservé : 315/315 HiveOperations
  et 716/716 API + SQL LocalDB.
- Test PlayMode visuel par un joueur réel, clics sur mobile, screenshots,
  intégration sur une vraie base d'Alpha, performances de 128 champs et
  rendu animé des essaims : **non effectués et non annoncés terminés**.

## Gardes qualité et sécurité

- Aucun ajout automatique dans une scène, un prefab, la carte terrain
  ou une base de données de production : construction runtime optionnelle.
- MobileAccountSessionRuntimeConfiguration.PollinationFeatureEnabled reste
  faux par défaut ; tous les flags serveur pollinisation restent faux.
- Le code principal Unity sur PCJEFF et SRVESDT restent inchangés.
- Les modifications de métadonnées .meta automatiquement supprimées lors
  des importations de l'ancien worktree (assets absents du checkout) sont
  restaurées à partir de Git **en ne ciblant que les suppressions**, sans
  rétablir ni écraser les nouveaux fichiers P7.
- Les résultats XML/logs de test doivent être archivés hors Git dans
  C:\BeeKingdom\DEV\BK-Studio\qa\pollinisation-p7-test-results.

## Restant avant activation Alpha

1. Essai visuel réel en PlayMode dans la scène WorldMapMmoFullscreenFoundation.
2. Vérification de la rotation/safe-area sur appareils, du tap/swipe et des
   overlays militaires, ainsi que des interactions avec le tutoriel.
3. Vérification des régressions de la version main actuelle et fusion
   soigneusement préparée depuis le worktree, jamais par reset.
4. Migration SQL + options sous contrôle en staging après sauvegarde vérifiée,
   puis tests multi-comptes et retour arrière.
5. Direction artistique spécifique des fleurs/champs et icônes premium
   approuvées sans retoucher les portraits existants.