# BeeKingdom — alternative terrain Wave6 (6 novembre 2026)

Branche isolée : `feature/worldmap-reviewed-semantic-terrain-20261106`,
basée sur `22f5f678`. Aucun déploiement, déplacement de joueur ni
modification de SQL n'est autorisé par cet essai.

## Décision

Remplacer les coordonnées du masque de placement défectueux par :
1. Les vraies tuiles Wave6, dont les empreintes SHA-256 sont vérifiées.
2. Un raster sémantique par tuile : `Land`, `River`, `Lake`, `Unknown`.
3. Une révision indépendante des propositions de couleur avec preuve
   de vérification, berges et empreinte de chaque aperçu.
4. Un planificateur qui réserve l'emprise entière et refuse les conflits.

Le code `SemanticTerrain.cs` et `MapPlacement.cs` existait déjà.
Le nouveau `ReviewedSemanticPlacementPlanner.cs` les relie dans
une opération de planification sans écriture. Il refuse une ruche dans
l'eau, l'eau sur terre, une classe de terrain inconnue, une révision
incorrecte et tout empiètement. Les positions déjà réservées sont prioritaires.
Les coordonnées non vérifiées ne sont jamais inventées ni déplacées.

## Prototype central vérifiable

`tools/qa/propose_wave6_semantic_terrain.py` a traité 9 tuiles
de 512 × 512 pixels, à partir des images originales de 516 × 516
(gouttière de 2 pixels retirée).

- Eau candidate : 528 733 pixels (22,4 %).
- Terre candidate : 1 550 974 pixels (65,7 %).
- Inconnu/berges : 279 589 pixels (11,9 %).

Résultats déterministes : deux exécutions ont donné les mêmes SHA-256
pour les propositions JSON, le stub `Unknown` et l'aperçu central.
Les 9 PNG correspondaient au manifeste Wave6.

Le générateur n'a **aucun accès à la production** et ne crée jamais
de données sémantiques déjà approuvées. L'outil distinct
`promote_reviewed_semantic_terrain.py` exige un examen par tuile;
sa révision vide est refusée. Si une tuile contient plus d'un type
d'eau, sa classification ne doit pas être approuvée comme un seul type.

## Diagnostic des positions actuellement chargées

La scène Unity a exporté 65 noeuds visibles, en lecture seule, vers
`C:\BeeKingdom\qa\BK-Actual-Visible-Nodes-20261106.json`.
Le rapprochement avec les 9 tuiles candidates indique 7
incompatibilités probables, incluant la ruche du joueur, l'araignée
voisine, la menace T3 centrale et une ressource de pollen sur eau
probable, plus une ressource d'eau sur terre probable.

Cela n'est **pas** une certification de terrain : ce sont des
hypothèses de classification à revoir. Les emprises complètes, les
étiquettes et les 2 491 autres tuiles ne sont pas encore certifiées.

## Résultats des tests

- Unity `ReviewedSemanticPlacementTests` : **18/18 PASS**.
- Générateur Python : 7/7 PASS.
- Révision indépendante Python : **12/12 PASS**.
- Diagnostic de positions Python : 5/5 PASS.
- Export réel Unity PlayMode : 1/1 PASS.
- Moteur commun compilé en .NET 8 : PASS, 0 erreur.
- Tests de règles côté serveur `WorldMapReviewedSemanticPlacementServerTests` : **8/8 PASS**.
- Vérification SHA-256 des ressources : **5 685/5 685** OK, aucune ressource ni GUID manquant.
- Tentative de promotion des données non révisées : refusée.

Rapports PCJEFF dans `C:\BeeKingdom\qa` :
`BK-ReviewedSemanticPlacement-Final-20261106.xml`,
`BK-ReadOnly-ActualNodes-20261106.xml`,
`SemanticTerrain-Prototype-20261106` et
`BK-VisibleNodes-vs-SemanticCandidates-20261106.json`.

## Conditions avant activation dans le jeu

Un raster graphique, même revu, ne donne pas à lui seul une autorité
de réservation entre joueurs. Le serveur doit publier le référentiel
signé, déterminer lui-même les emprises des icônes et des légendes
au zoom minimal, et vérifier les réservations dans une transaction
atomique. Un refus doit conserver la position originale. Le client
affichera ensuite les coordonnées officielles, avec les mêmes
transformations pour les images et les zones cliquables.

Résoudre les conflits historiques après révision des lieux;
ne pas masquer des objets ni modifier les données par simple zoom.
Validation GameView, transactions SQL réellement atomiques et Android requise avant fusion dans `main`.

## Livraison technique — règles communes Unity et serveur

Le projet `Server/src/BeeKingdom.Shared/BeeKingdom.Shared.csproj`
compile désormais **les mêmes sources C# que Unity** pour
`MapPlacement`, `SemanticTerrain` et `ReviewedSemanticPlacementPlanner`
(par liaison de fichiers, sans recopier les algorithmes).
Le build .NET 8 est réussi sans avertissement sur cette bibliothèque.
La suite ciblée des huit tests serveur valide le rejet eau/terre,
l'immuabilité des positions existantes, les conflits atomiques de plan,
les exceptions historiques explicites et le même SHA-256 que Unity/Python.

Dans `ReviewedSemanticPlacementPlanner.Build`, une position fixe ne
bénéficie plus d'une approbation implicite : elle doit passer **elle aussi**
la rasterisation sémantique, sauf si le serveur l'a explicitement marquée
comme historique. Dans ce cas, elle reste conservée à sa position, sans
déclarer son terrain valide et sans créer un nouveau droit de placement.
Ce planificateur reste **sans écriture** : le plan refusé est vide.

L'outil `promote_reviewed_semantic_terrain.py` exige désormais
`--original-tiles` lorsqu'une révision est fournie : il reprend l'empreinte
SHA-256 de chaque PNG d'origine au moment même de l'approbation,
en plus de celles de l'aperçu et de la proposition. Une image modifiée
est rejetée. Les 12 tests couvrent notamment cette régression.
Une preuve textuelle dans le fichier de révision n'est **pas une signature
cryptographique** ni une autorisation de publier : le service officiel doit
encore épingler la version et son empreinte, puis autoriser les actions.

## Résultats isolés de contrôle

- Unity : `C:\BeeKingdom\qa\BK-Reviewed-Semantic-FixedClaims-Unity-20261106.xml`
  — 18/18 PASS, code de sortie Unity 0.
- Serveur .NET 8 :
  `Server/tests/BeeKingdom.Tests/TestResults/BK-Semantic-ServerShared-Retest-20261106.trx`
  — 8/8 PASS.
- Intégrité : `C:\BeeKingdom\qa\BK-Semantic-SHA-Contract-20261106.json`
  — 5 685/5 685 empreintes, zéro ressource manquante, zéro GUID en double.
- Prototype de terrain candidat **hors Git et non approuvé** :
  `C:\BeeKingdom\qa\SemanticTerrain-Prototype-20261106`.
  Ne pas le copier vers la production, ni le configurer comme référentiel officiel.

## Activation future : points bloquants explicites

1. Faire réviser les catégories et berges des tuiles; les 9 du prototype
   et les 2 491 restantes n'ont **aucune** approbation automatique.
2. Enregistrer une version de terrain vérifiée et épinglée sur le serveur,
   avec les catégories et les enveloppes autorisées par type d'objet.
3. Implémenter la transaction SQL de réservation/rejet idempotente
   (création, mouvement, réapparition) : **non réalisée ici**.
4. Corriger les chevauchements hérités par des mouvements approuvés;
   l'audit de la scène montrait 22 croisements estimés et 18 positions
   historiques non résolues. Le zoom reste fixe, mais les collisions
   n'ont pas été effacées.
5. Exécuter les tests GameView/Android et vérifier réellement les zones
   cliquables, la lisibilité des légendes et les changements de zoom.

Ni `main`, ni SRVESDT, ni la base SQL, ni le build Android n'ont été
modifiés pour activer ces règles. Ce lot fournit la **fondation vérifiée**
pour remplacer le masque, pas une migration de positions déjà publiée.

## Production gate — 6 novembre 2026

Après la demande d'activation en production, une vérification **réelle
et en lecture seule** a été exécutée sur SRVESDT. Le service officiel
`https://api-ops.beekingdomgame.com/health` a répondu Healthy,
mais `/runtime/world-map-readiness` confirme que le gameplay carte
est encore `PreparationOnly`, `readOnly=true`, `nonLive=true`,
`mapGameplayEnabled=false`, `realTimeSynchronizationEnabled=false`,
et `productionRouteProven=false`.

Garde-fou livré dans
`Server/ops/world-map-semantic/Test-ProductionActivationGate.ps1`.
Copie identique sur SRVESDT :
`C:\BeeKingdom\TOOLS\WorldMapSemantic-ProductionActivationGate-20261106.ps1`.
SHA-256 des deux scripts :
`7AEC681CAAE5A9F665CC50E7733AB792139C174DDD268A98C1C789D6A1BC322A`.

Exécution SRVESDT : `WORLD_MAP_ACTIVATION=BLOCKED`,
`API_HEALTH=PASS`, code de sortie `2` (**échec attendu**).
Rapport :
`C:\BeeKingdom\staging\WorldMapSemantic-ProductionGate-20261106.json`.

Le garde a détecté notamment **aucun manifeste de terrain
révisé et lié à une révision**, **aucune configuration de réservations
SQL atomiques imposées par le serveur**, et **aucun endpoint de
production prouvant que le moteur d'admission est réellement appelé**.

**Aucune activation effective des placements n'a eu lieu :**
- `C:\inetpub\BeeKingdomApi` et la configuration IIS n'ont pas
  été modifiés.
- Aucun changement SQL, aucun déplacement de joueur, aucune
  réapparition de ressource modifiée.
- Aucune fusion vers `main`, aucun build Android distribué.
- La branche de la solution reste un prototype validé par tests,
  mais pas une fonctionnalité actuellement exécutée par l'API officielle.

Le script de contrôle ne possède **aucune commande d'activation**.
Il ne suffira pas de modifier les indicateurs booléens; le moteur
de réservations doit être relié aux transactions serveur, avec
preuve de capacité et tuiles officiellement revues, avant publication.