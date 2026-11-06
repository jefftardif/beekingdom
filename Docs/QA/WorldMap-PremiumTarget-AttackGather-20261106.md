# BeeKingdom — fenêtre premium HD Attaque / Récolte

Date : 6 novembre 2026
Branche isolée : \`feature/worldmap-premium-target-actions-20261106\`
Dépôt de travail : \`C:\BeeKingdom\staging\BK-HudChat-Reconcile-20261105\`
Base héritée : \`033395b0\`, comprenant le zoom stabilisé et les garde-fous de terrain, sans fusion vers \`main\`.

## Objectif et livraison Unity

L'ancien panneau fixe « Expédition » mélangeait une ressource et un ennemi, conservait des informations locales ou simulées, utilisait deux dispositions indépendantes et pouvait donner l'impression qu'une action de démonstration créditait réellement une récompense.

Le panneau a été remplacé par un composant commun :
- \`WorldTargetPanelPolicy.cs\` : sélection d'une seule cible et décisions d'action pures, préconditions explicites et refus par défaut.
- \`WorldMapMmoFullscreenFoundationBootstrap.TargetPremium.cs\` : dessin IMGUI HD avec surfaces neuf tranches générées localement, fond anthracite, bordures ambre, accent rouge pour les ennemis et vert pour les ressources, icônes existantes à la résolution Unity, métriques en quatre blocs, conseil/contextualisation, gros boutons tactiles, fermeture et centrage caméra.
- \`WorldMapMmoFullscreenFoundationBootstrap.cs\` : intégration aux vrais clics et zones actives; remplacement de l'ancien panneau et de la composition de l'escouade de collecte. La fenêtre est en avant du mini-Chat/HUD; les pressions sur la carte derrière elle sont bloquées. Fermer rend le contrôle à la carte.
- \`strings.fr-CA.json\` et \`strings.en-US.json\` : 71 nouvelles clés premium concordantes, UTF-8 contrôlé, accents français conservés.

## Fonctions réelles et états honnêtes

**Attaque :** nom localisé, tier, rôle, statut de l'escouade, distance réelle depuis la ruche, force requise provenant du modèle serveur quand le tier de l'aperçu correspond, éventuel bonus de repérage rare. Le bouton « Préparer l'armée » ouvre la caserne existante si aucune escouade n'est réservée. « Planifier l'attaque » ouvre la vraie fenêtre de patrouille serveur avec la cible mémorisée. Aucun combat ni butin fictif ne sont crédités par la fenêtre.

**Récolte :** type de ressource, rendement, quantité et durée du serveur lorsqu'ils sont disponibles, temps restant d'une mission active, distance depuis la ruche, bonus de cible du jour. Le bouton de départ ouvre une **composition premium** avec les trois familles d'abeilles, championne, quantité autorisée, et validation via le contrôleur officiel du serveur. Les états « rappel » et « récupérer » appellent les méthodes de collecte officielles déjà existantes.

**Ressources locales non officielles :** le parcours du tutoriel est préservé, sous la mention explicite « Vol d'observation (démo) » sans gain d'inventaire officiel. Aucun affichage de gain serveur inventé; les données non connues affichent « -- ».

**Interactions :** un seul panneau ennemi ou ressource visible à la fois; fermeture X/Échap/toucher externe; centrage sur la coordonnée de monde de la cible; orientation paysage/portrait et Safe Area, sans déplacer les objets au zoom. Le menu PLUS/Sac et le Chat conservent leurs parcours d'origine.

## Tests effectués

- Unity \`WorldTargetPanelPolicyTests\` + \`WorldTargetPanelLayoutTests\` : **28/28 PASS** — sélection unique, boutons, refus, tutoriel, orientation, marges de sécurité et zones tactiles.
- Unity \`WorldTargetPremiumPlayModeTests\` : **1/1 PASS** dans \`WorldMapMmoFullscreenFoundation\` — cible ressource/ennemi, basculement, exclusion des clics derrière la fenêtre et fermeture.
- Unity suite élargie \`BK-TargetPremium-CombinedRegressions-20261106.xml\` : **46/46 PASS** — nouveau panneau, zoom, espacement, réservations locales et premier plan partagé.
- Unity \`WorldMapMorePlayModeSmokeTests\` : **1/1 PASS**, avec **12 alternances PLUS/Sac**, afin de prévenir le retour de blocages d'entrée et de clignotements.
- \`verify_premium_target_localization.py\` : **PASS**, 71/71 clés appariées FR/EN, 58 clés effectivement référencées dans l'UI, aucun caractère de remplacement.
- Unity a compilé sans erreur lors de ces exécutions.
- Aucun état SQL, API ou position de monde n'a été modifié par ces changements.

## Limites de vérification à ne pas masquer

- En mode \`-batchmode\`, \`ScreenCapture.CaptureScreenshot\` n'a produit aucun PNG. Les tests attestent **les règles et la scène**, pas un contrôle pixel par pixel de l'apparence. L'essai de capture non pris en charge a été retiré du code livré.
- Un APK Android de QA indépendant est préparé via \`WorldTargetPremiumAndroidQaBuild.BuildQaApk\`, avec une identité d'application distincte de l'alpha, signature de développement, scènes officielles, sauvegarde/restauration des paramètres Android. Résultat APK à consigner **uniquement après** fin de compilation.
- Aucune installation/test de doigt réel sur tablette et aucun lancement officiel de combat/récolte avec un compte authentifié n'ont encore été démontrés par ces tests automatisés.
- Le chantier distinct du placement du terrain Wave6 garde ses restrictions : l'ancien masque est désactivé, le terrain sémantique n'est pas certifié; aucune activation SQL des nouveaux placements n'est faite ici.

## Règles de sécurité et de livraison

Ne pas fusionner dans \`main\` les changements locaux préexistants. Ne pas commiter les DLL NuGet, fichiers binaires Unity, métadonnées externes ou fichiers temporaires étrangers au panneau. Conserver l'APK et les logs dans \`C:\BeeKingdom\qa\`, hors Git.

En production, le nouveau panneau ne doit jamais annoncer une récompense ou une victoire avant la validation du contrôleur officiel. Les fonctions du terrain sémantique demeurent indépendantes de ce lot.
