# BeeKingdom — Audit premium HD des fenêtres des bâtiments
**Date :** 6 novembre 2026
**Branche :** `feature/hive-all-buildings-premium-audit-20261106`
**Périmètre :** les 14 entrées officielles de `BuildingCatalog` et leurs parcours d'ouverture dans la carte de ruche Unity.

## Résultat vérifié

L'audit conserve les fenêtres premium déjà fonctionnelles et corrige les incohérences suivantes :

1. **Illustration exacte par bâtiment.** `HiveMapPremiumBuildingWindow.BannerResourcePathForProof` utilise le `LegacyKey` vérifié par `BuildingMappingTable` ; les 14 ressources `PremiumBeeReference/BuildingBanners/<legacyKey>` existent et sont validées par Unity. La Nurserie, l'Infirmerie et la Défense ne récupèrent plus la bannière du Centre d'alliance.
2. **Hall des championnes.** Son illustration HD provient maintenant du `archives_honeyfall` du catalogue officiel, au lieu du chemin inexistant `champion_hall`.
3. **Réserve de miel, Entrepôt, Transformation.** Leur ancien panneau gris `GUI.Box/GUILayout` est remplacé par le composant plein écran `HiveMapPremiumBuildingWindow`, avec cartes riches, en-tête HD propre au bâtiment, scrolling, prise en compte de la Safe Area, état serveur, ressource, stock, débit, capacité et bouton d'actualisation conditionnel. Le bouton « i » possède désormais une zone tactile de 44 pixels.
4. **Intégrité fonctionnelle.** Les nouvelles vues d'information ne déclenchent **aucune collecte ni attribution de ressources**. Le clic sur le bâtiment garde la collecte existante. Les valeurs serveur inconnues restent à « -- » et le mode hors ligne est en lecture seule.
5. **Protection des superpositions.** Le panneau de production est reconnu comme propriétaire des interactions, mais ne se prend plus lui-même pour une autre fenêtre bloquante. Le geste de fermeture ne se transmet pas aux bâtiments sous-jacents. Les fenêtres Académie, Défense, Génétique, Infirmerie ne peuvent plus retomber sur un ancien panneau provisoire gris.
6. **FR/EN.** Vingt nouvelles clés de production dans chaque catalogue français canadien et anglais américain, validées dans Unity, sans clé technique ou caractère de remplacement Unicode visible.

## Revue du catalogue de 14 bâtiments

| Bâtiment | Fenêtre ou interaction vérifiée | Constat |
|---|---|---|
| Nurserie | Écran premium de soins et couvain, serveur | Conservé ; bannière dédiée |
| Réserve de miel | Collecte au clic + fenêtre premium de production | **Modernisée** |
| Caserne | Écran d'armée, formation et amélioration | Conservé |
| Défense | Écran premium de troupes et patrouilles | Conservé ; bannière dédiée |
| Génétique | Écran premium de progression, serveur, lecture seule des fonctions à venir | Conservé ; bannière dédiée |
| Recherche | Fenêtre premium plein écran, progression et filtres | Conservée |
| Entrepôt | Collecte au clic + fenêtre premium de production | **Modernisée** |
| Transformation | Collecte au clic + fenêtre premium de production | **Modernisée** |
| Infirmerie | Fenêtre premium de convalescence serveur | Conservée ; bannière dédiée |
| Académie | Fenêtre premium de formation serveur | Conservée ; bannière dédiée |
| Banque | Fenêtre premium des réserves royales et développement | Conservée |
| Palais royal / Administration | Fenêtre premium de progression et prérequis | Conservée |
| Centre d'alliance | Fenêtre premium, membres, recherche et chat | Conservée |
| Hall des championnes | Catalogue premium, affectation et amélioration | **Bannière corrigée** |

La classification « conservé » signifie que les tests de code/interface existants passent ; elle ne constitue pas une nouvelle validation visuelle manuelle au pixel près.

## Contrôles reproductibles

**Unity 6000.5.3f1 / EditMode : 91/91 tests réussis, 0 échec.**
Rapport sur PCJEFF : `C:\BeeKingdom\qa\BK-HiveAllBuildings-PremiumFinal-20261106.xml`.

Ces tests incluent :
- `PremiumBuildingPolishTests`, Académie, Génétique, Infirmerie, Nurserie, Défense, Palais royal et activités ;
- `AllHiveBuildingsPremiumCoverageTests` et `HiveAllBuildingsPremiumAuditTests` ;
- les 14 chemins d'illustration HD réels dans Unity, les modes FR/EN, le plein écran, l'indépendance du panneau d'information et la protection des actions serveur.

Le **contrôle strict des SHA-256 d'assets Unity** et la vérification du commit Git sont consignés séparément lors de la livraison.

## Limites transparentes

- Les captures HD n'ont pas été validées manuellement dans GameView ou sur une tablette réelle ; les contrôles automatiques ne prouvent pas à eux seuls le rendu pixel par pixel.
- Aucune action serveur d'un vrai joueur (formation, soins, banque, collecte) n'a été déclenchée par ces tests.
- L'APK Android premium précédent restait bloqué dans la compilation native Unity ; aucun nouveau build Android n'est prétendu livré ici.
- L'audit porte sur les fenêtres des bâtiments, **pas** sur l'activation du masque de terrain Wave6 ni sur les transactions de placement. Aucune donnée ni configuration de SRVESDT n'est modifiée dans cette branche.
- Préserver `main`, les modifications locales antérieures et les éléments Unity régénérés (DLL NuGet, caches, matériaux auto-modifiés) qui n'appartiennent pas à ce correctif.

**Résultat :** les 14 parcours ont été inventoriés ; les interfaces vieillissantes identifiées ont été modernisées, les bannières spécifiques corrigées et les tests disponibles ont passé. Une inspection visuelle tactile réelle demeure le dernier niveau de confirmation de qualité HD.
