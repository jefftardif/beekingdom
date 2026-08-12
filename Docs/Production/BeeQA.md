# BeeQA — QA Framework Foundation

## Objectifs

BeeQA est l'outillage interne de développement et de validation de BeeKingdom. Il ne fait pas partie du gameplay et ne doit pas être requis par une build de production.

Le framework fournit un point d'entrée unique pour les outils QA et réduit le temps nécessaire pour vérifier un sprint.

## Architecture

BeeQA est isolé sous `Assets/BeeKingdom/BeeQA/Editor` et n'a aucune référence à `HiveViewProductUiPresenter` ou aux systèmes gameplay.

- `Modules` : registre et contrats des outils indépendants.
- `Scenarios` : scénarios de validation reproductibles.
- `Debug` : diagnostics développeur.
- `Reports` : exports et rapports QA.
- `Performance` : mesures et budgets.
- `Automation` : runners et parcours automatisés.
- `Tools` : outils transverses.

Le catalogue de catégories est data-driven dans `BeeQACatalog`. Les outils futurs implémentent `IBeeQAModule` ou dérivent de `BeeQAModuleBase`; `BeeQAModuleRegistry` les découvre automatiquement par réflexion et les enregistre sans modification du Dashboard. Aucun switch central n'est nécessaire.

## Point d'entrée

Dans Unity Editor : `BeeKingdom > BeeQA > Open Dashboard`.

Le panneau est disponible dans l'Editor et dans les contextes Debug/Development. Le code est placé dans un dossier `Editor`, donc il n'est pas inclus dans le runtime d'une build de production.

## Panneau Sprint 001

Le dashboard affiche les modules découverts, leurs métadonnées, leur statut, leur dernier résultat, leur durée, leur date et leur message. Il conserve également les 18 catégories prévues. Les boutons `Run` et `Run All` exécutent les modules via le registre.

Le premier module officiel est `BeeQASmokeTestModule`. Il vérifie le contexte Editor/Debug et l'intégrité du catalogue BeeQA, puis retourne `PASS` ou `FAIL`.

## Philosophie

- Un outil QA ne modifie pas la logique du jeu sans contrat explicite.
- Les scénarios sont reproductibles et leurs preuves sont exportables.
- Les modules évoluent indépendamment.
- Les outils de production restent séparés du contenu joueur.

## Évolutions prévues

Gameplay, Hive, World, Alliance, Economy, SpeedUps, Inventory, Rewards, Research, Buildings, Notifications, Performance, Networking, Save, UI, Graphics, Audio et Automation recevront leurs modules au fil des sprints.

Chaque futur module doit rester autonome, avoir un constructeur sans argument pour la découverte automatique et ne référencer aucun composant gameplay directement depuis le Dashboard.
