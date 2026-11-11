# BeeKingdom — clic de récolte puis ouverture des détails (11 novembre 2026)

**Branche :** `feat/hive-producer-collect-then-details-no-info-icon-20261111`
**Projet :** `C:\BeeKingdom\staging\BK-HudChat-Reconcile-20261105`
**Périmètre :** Réserve de miel (`HONEY_RESERVE`), Entrepôt (`WAREHOUSE`), Atelier de cire (`TRANSFORMATION`).

## Règle approuvée

- **Ressource disponible, validée par le système existant :** au premier clic sur le bâtiment, déclencher la collecte. Pour un joueur authentifié, la mutation est envoyée au serveur, et le crédit reste soumis à confirmation; pour la démo, le moteur de production local existant reste utilisé.
- **Au clic suivant sur ce même bâtiment :** ouvrir sa fenêtre d'informations premium, même si le dernier instantané serveur indique encore une ressource récoltable.
- **Rien de récoltable :** ouvrir les informations dès le premier clic, sans collecte fictive.
- **Pas de bouton « i » :** un bâtiment, un seul parcours. Aucun badge d'information ne doit être dessiné au-dessus de la ruche.
- **Fenêtre ouverte :** la protection contre les clics traversants et la fermeture explicite (Retour, Échap, Améliorer) restent en vigueur.
- **Par bâtiment :** une collecte sur l'Atelier ne supprime pas la prochaine ouverture due à une collecte précédente sur la Réserve de miel.

## Modifications

- `HiveMapProductionInfoBootstrap.cs` est le seul abonné à `Selection.BuildingClicked` pour ces trois producteurs. Il utilise `ManualProductionReadyForExternalHost` (avec `model.CanCollect` quand la production officielle est configurée), puis `CollectManualProductionForExternalHost` une seule fois. Une `HashSet` mémorise quel producteur doit ouvrir les détails au clic suivant. Une barrière de frame évite de traiter le même clic deux fois.
- `HiveMapProductionBootstrap.cs` continue de faire progresser la production et d'afficher les animations, mais ne déclenche plus de collecte parallèle. Ses callbacks de clic historiques sont retirés.
- `HiveMapProductionInfoBootstrap.Pointer.cs` conserve une détection de repli pour les bâtiments dont l'art dépasse le collider. Elle utilise **le même** gestionnaire de clic que la sélection normale, jamais un second collecteur; elle protège le HUD et refuse le clic traversant sur une Banque ou un autre bâtiment.
- `HiveViewProductUiPresenter.cs` conserve le halo de récolte, mais retire son `GUI.Button` invisible qui pouvait réémettre la mutation.
- `HiveMapOverlayInputGateBootstrap.cs` ne traite plus la zone disparue du « i » comme un propriétaire tactile.
- Textes d'accompagnement FR/EN mis à jour, sans annoncer de récompense avant la réponse officielle.
- Tests de politique de clic et d'absence de bouton « i » mis à jour dans les suites de régression des bâtiments.

## Vérifications

Les projets C# Unity `Assembly-CSharp.csproj` et `Assembly-CSharp-Editor.csproj` ont été compilés avec **zéro erreur C#**. Les rapports datés se trouvent sous `C:\BeeKingdom\qa\BK-Producer-ClickSequence-*`.

Le contrôle strict du manifeste d'assets est effectué séparément (SHA-256 des ressources, GUIDs, métadonnées). Les nouveaux scénarios NUnit sont prêts pour une exécution dans Unity; un build .NET réussi ne prouve pas, à lui seul, que le clic physique a été rejoué sur la GameView.

Aucune donnée SQL ni API de SRVESDT n'a été modifiée. Aucun changement fusionné sur la branche principale et aucune modification des fichiers Unity générés sans rapport avec ce chantier.

## Vérification utilisateur

1. Arrêter Play, attendre la recompilation et relancer Play dans le projet d'intégration.
2. Réserve de miel avec ressources prêtes : cliquer une fois et observer l'animation de demande de récolte, puis cliquer une deuxième fois pour afficher la fiche.
3. Bâtiment sans ressources : un clic suffit pour afficher la fiche.
4. Refaire sur Entrepôt et Atelier de cire : aucun « i » ne doit rester visible.
5. Vérifier que les autres bâtiments (Banque, Nurserie, etc.) conservent leur fonctionnement.
