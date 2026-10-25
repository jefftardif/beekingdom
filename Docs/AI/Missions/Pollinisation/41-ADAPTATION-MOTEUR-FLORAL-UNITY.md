# 41 — Adapter FlowerManager et le moteur écologique existant sans le réinventer

## Réalité du dépôt

Dans Assets/BeeKingdom/World :
- FlowerManager maintient speciesById et patchesById, avance au temps de simulation locale et émet des événements.
- FlowerPatch gère Stage, AgeSeconds, Nectar, Pollen et Harvest en **double**.
- FlowerSpecies décrit capacité et BloomCycle.
- PollinationRules calcule vitesse de régénération selon saison/météo.
- EcologicalBalance multiplie PollinationFactor, ClimateFactor, BiomeFactor.
- RegionalEcologyPressureFramework combine pression d'épuisement, eau, météo, pollinisation et biome.

Ces systèmes sont une **base de calcul et de simulation client**, pas la vérité multijoueur. Ils sont utiles, mais l'agent ne doit pas accorder une ressource officielle à partir de leurs appels locaux.

## Stratégie de réconciliation

### Étape 1 — inventaire exact des usages

Chercher tous les appels de FlowerManager.Harvest, FlowerManager.Execute, FlowerPatch.Advance, NaturalResourceNode.Harvest, EcologicalBalance.SetPollination et événements FlowerBloomed/Depleted.

Tracer pour chaque appel : scène/editor/test/production, origine des données, impact sur monnaie, effet visuel. Cette étape détermine quels chemins pourraient entrer en conflit avec un FloralSiteState serveur.

### Étape 2 — catalogue partagé de concepts

Réutiliser espèces/familles et indices, mais **valider les IDs existants**. La compatibilité entre fleurs doit être un catalogue serveur et client lisible, dont l'existence ne dépend pas du rendu.

Identifier type de cycles exact, climat/météo, notion de région, espèces présentes. Les catalogues peuvent avoir une couche de mapping, pas deux noms incompatibles pour la même fleur.

### Étape 3 — état officiel de site

Introduire un snapshot serveur FloralSiteDto avec version, famille, phase, réserves, fertilité, biodiversité, droits d'accès. Le client l'applique en **lecture seule** à un composant d'affichage/adaptateur. Les interactions utilisateur passent par les API officielles.

### Étape 4 — simulation déterministe backend

Porter ou réimplémenter de petites fonctions purement mathématiques côté serveur (pas UnityEngine) : phase de floraison, regen bornée, compatibilité, pression écologique, rendement de livraison, avec tests de parité à la simulation lorsque la règle est commune.

Toute divergence voulue (ex. FlowerPatch double en laboratoire vs stock int64 en production) doit être documentée et contrôlée.

### Étape 5 — pas de double vérité

- Monde local/lab : les fleurs avancent librement sans économie officielle.
- Monde MMO : les mouvements visuels interpolent entre snapshots officiels.
- Aucune action Unity.Harvest locale ne déclenche un crédit PlayerHiveState.
- Si les deux moteurs tournent en parallèle pour des raisons cosmétiques, ils ne publient pas d'événement de récompense ou de projet et la provenance est visible dans logs.

## Identité et placement

Les coordonnées HexCoordinates et le modèle de WorldMapMmoFullscreenFoundationBootstrap doivent être rattachés aux IDs monde/site officiels. À chaque reconnexion, la même ID retourne le même site. Les cartes de chunks, assets et collisions de spawn restent intactes ; ne pas replacer les tuiles.

## Dégradation écologique

RegionalEcologyPressureFramework possède déjà les catégories Healthy/Fragile/Degraded/Critical. Ne pas imposer directement ces états comme valeurs serveur : les mapper vers l'indice officiel si pertinent avec un contrat documenté.

La « fertilité » n'est pas identique à « baisse des stocks » ; conserver l'indice de pression comme lecture secondaire, pas fusionner tous les compteurs.

## Compatibilité régénération

Les règles Unity utilisent des doubles et simulation DeltaSeconds. Le serveur officiel doit borner durées, stock et arrondis, sans donner plus de réserve si un joueur rafraîchit plus souvent le GET. Le temps de rattrapage après coupure serveur est une politique de produit configurable.

## Tests

- Parité de cycles de fleurs pour événements contrôlés.
- Site garde même ID/position après redémarrage Unity.
- Fermeture Unity ne suspend pas mission/écologie officielle.
- Fleur récoltée sur deux devices ne double pas son stock.
- Activer/désactiver flag civil ne change pas les missions de collecte historiques.
- Un refresh du snapshot modifie l'affichage, non le stock sans action serveur autorisée.
- Aucune régression FlowerEcosystemTests / RegionalEcologyPressureFramework107Tests.

