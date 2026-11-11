# BeeKingdom — fiche de production invisible et Retour de la Banque

Date : 11 novembre 2026
Branche : `fix/hive-producer-modal-lifecycle-bank-arrow-20261111`

## Constat dans les captures

- La Banque affiche encore un bouton « Retour » textuel, différent de la flèche premium de la Nurserie et de la Caserne.
- Les boutons « i » de la Réserve de miel, de l'Entrepôt et de l'Atelier de cire n'affichent pas leurs fiches de production.
- **L'analyse du vrai `Logs/Editor.log` confirme que les clics étaient reçus** : `[BK-ProducerInfo] Normal building click: HONEY_RESERVE shown=True`, `Gold info badge: HONEY_RESERVE shown=True` et `TRANSFORMATION shown=True`. Ce n'était donc plus un problème de raycast ou de zone cliquable.

## Correction

### Cycle de vie de la fiche producteur

`HiveMapProductionInfoBootstrap.Update` et `OnGUI` fermaient automatiquement la fiche déjà ouverte lorsque `IsAnotherOverlayBlockingProductionInfo()` devenait vrai après l'ouverture. La fenêtre pouvait ainsi être créée avec `shown=True`, puis masquée ou détruite aussitôt par un état modal transitoire.

La fiche reste désormais ouverte et son Canvas premium demeure actif **jusqu'à une fermeture explicite** : bouton Retour, Échap ou transition vers l'amélioration. Le garde de départ `IsAnyOverlayBlocking()` est conservé : une nouvelle fiche ne peut pas s'ouvrir par-dessus une autre fenêtre déjà active. La fermeture capture toujours le geste afin de ne pas déclencher de clic sous le panneau.

Une trace ponctuelle `[BK-ProducerInfo] Premium canvas opened` présente le type de bâtiment, les drapeaux d'ouverture et l'état actif du Canvas, sans données de compte ni secret. Un avertissement ponctuel dans l'éditeur signale un état modal concurrent sans détruire la fiche.

Aucune donnée de production, taux, inventaire ni récompense n'a été modifiée.

### Banque : même flèche que Nurserie/Caserne

`HiveViewProductUiPresenter.Bank.cs` utilise désormais le **même composant `WorldMapHudLayout.DrawChatReturnButton`**, avec le fond or et la flèche 38 px sur surface tactile 52 px. L'ancien bouton textuel de 78 × 48 px est retiré. La navigation conserve la distinction existante : revenir de Réserve royale vers la feuille de route, ou fermer la fenêtre principale.

L'illustration HD `hive_bank` déjà raccordée reste conservée.

## Tests et statut

- `Assembly-CSharp.csproj` : compilation **PASS, 0 erreur C#** (journal sur PCJEFF : `C:\BeeKingdom\qa\BK-Producer-Modal-Bank-Compilation-20261111.log`).
- `Assembly-CSharp-Editor.csproj` : compilation **PASS, 0 erreur C#**, avec les nouveaux tests `HiveProducerModalPersistenceAndBankReturnTests` temporairement inclus. Le fichier .csproj généré a été restauré ensuite.
- Les tests ajoutés vérifient le bouton commun de la Banque, la persistance du panneau pour les trois producteurs malgré un drapeau concurrent, et la conservation de la fermeture explicite. **L'exécution NUnit dans Unity Test Runner reste à faire** : Unity est ouvert interactivement sur le même projet.
- Contrôle des fichiers Unity par SHA-256 et vérification de Git : rapport séparé dans `C:\BeeKingdom\qa`.
- Aucun changement au serveur de production ni à la branche `main`. Les fichiers générés et binaires locaux ne sont pas inclus.

## Validation GameView

Dans le projet déjà ouvert sur PCJEFF, arrêter Play, laisser recompiler, relancer Play. Ouvrir la **Banque** et vérifier que la flèche dorée remplace « Retour ». Cliquer ensuite sur la Réserve de miel, son « i », puis sur l'Entrepôt et l'Atelier de cire : chacun doit laisser sa fiche premium affichée jusqu'à fermeture.

Si une fiche ne paraît toujours pas, vérifier dans Console `[BK-ProducerInfo] Premium canvas opened` et `canvasActive=True/False`; cette trace sépare un défaut de Canvas d'un défaut d'interaction.
