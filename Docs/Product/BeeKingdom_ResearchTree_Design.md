# Bee Kingdom — Arbre de recherche (proposition de conception)

## Statut (2026-07-30)

**Branche 1 (Économie) implémentée** côté serveur et client : 10 recherches
réelles (paliers I/II/III miel/cire/pollen + convergence "Réserves scellées"),
dépendances vérifiées côté serveur, effets cumulatifs réels dans
`HiveOfflineProductionService`. Différence assumée par rapport à la
proposition ci-dessous : les prérequis de **niveau de bâtiment** (ex.
"Réserve de miel niveau 2") n'ont pas été implémentés dans cette passe — seuls
les prérequis de recherche (palier précédent) sont vérifiés, pour garder cette
étape limitée à un seul système. Les branches 2 (Défense), 3 (Population) et 4
(Chambre royale) restent non implémentées : elles dépendent de systèmes qui
n'existent pas encore (poste de garde, statistiques de puissance de combat,
système de caste, fédération liée au chantier Communication gelé).

## Contexte

Le système de recherche existant côté serveur (`HiveOperationService.ResearchCatalog`)
ne contient que deux entrées de démonstration, sans palier, sans dépendance et
sans exigence de bâtiment : `foraging_routes_i` (bonus de production de miel)
et `tempered_combs_i` (bonus de capacité de cire). La durée est actuellement
fixée à 16 secondes pour toute recherche, quel que soit son coût — un
placeholder technique, pas une valeur de conception.

Ce document propose un arbre complet, organisé en quatre branches, avec
paliers, dépendances, coûts et avantages concrets — à valider et ajuster avant
implémentation. Le principe directeur (hérité de la référence Ant Legion) :
plusieurs voies utiles en parallèle, aucune ne doit rendre les autres inutiles.

## Convention de lecture

Chaque entrée indique : **Palier** (I/II/III), **Prérequis** (recherche et/ou
niveau de bâtiment), **Coût** (miel / cire / pollen), **Durée**, **Effet**.
Les durées proposées restent courtes (adaptées aux tests) ; à multiplier par
un facteur d'équilibrage avant mise en production réelle.

---

## Branche 1 — Économie de la ruche

Prolonge directement les deux recherches déjà existantes. Renforce la
production et le stockage des trois ressources de base.

### Itinéraires de butinage (miel)
- **I** — Prérequis : aucun. Coût 240 miel / 90 pollen. Durée 2 min.
  Effet : +2 % production de miel (`HoneyProductionBonusBps = 200`, déjà en place).
- **II** — Prérequis : Itinéraires de butinage I, Réserve de miel niveau 2.
  Coût 900 miel / 500 pollen. Durée 6 min. Effet : +5 % production de miel
  supplémentaire (cumulatif, total +7 %).
- **III** — Prérequis : Itinéraires de butinage II, Réserve de miel niveau 3.
  Coût 2 400 miel / 1 400 pollen. Durée 12 min. Effet : +8 % production de
  miel supplémentaire (total +15 %).

### Alvéoles tempérées (cire)
- **I** — Prérequis : aucun. Coût 180 miel / 120 pollen. Durée 2 min.
  Effet : +5 % capacité de cire (`WaxCapacityBonusBps = 500`, déjà en place).
- **II** — Prérequis : Alvéoles tempérées I, Atelier de cire niveau 2.
  Coût 900 miel / 500 pollen. Durée 6 min. Effet : +8 % capacité
  supplémentaire (total +13 %), +3 % production de cire.
- **III** — Prérequis : Alvéoles tempérées II, Atelier de cire niveau 3.
  Coût 2 400 miel / 1 400 pollen. Durée 12 min. Effet : +10 % capacité
  supplémentaire (total +23 %), +5 % production de cire supplémentaire.

### Tri du pollen (nouvelle ressource couverte)
- **I** — Prérequis : aucun. Coût 200 miel / 150 wax. Durée 2 min.
  Effet : +5 % production de pollen.
- **II** — Prérequis : Tri du pollen I, Entrepôt niveau 2. Coût 800 miel /
  600 cire. Durée 6 min. Effet : +8 % production supplémentaire (total +13 %).
- **III** — Prérequis : Tri du pollen II, Entrepôt niveau 3. Coût 2 200 miel /
  1 600 cire. Durée 12 min. Effet : +10 % production supplémentaire
  (total +23 %), +5 % capacité de pollen.

### Réserves scellées (capacité globale, palier unique — technologie de convergence)
- Prérequis : Itinéraires III, Alvéoles III et Tri du pollen III (les trois
  recherches de niveau III de cette branche). Coût 6 000 miel / 4 000 cire /
  4 000 pollen. Durée 20 min. Effet : +10 % capacité de stockage sur les
  trois ressources simultanément (multiplicatif avec les bonus de niveau de
  bâtiment).

---

## Branche 2 — Défense de la ruche

Renforce la garde et la résistance de la colonie face aux menaces
extérieures (fausse teigne, petit coléoptère des ruches, guêpe, frelon —
cf. `AntLegion_BeeKingdom_FunctionalReference.md`).

### Carapace renforcée (défense passive)
- **I** — Prérequis : Poste de garde niveau 1. Coût 260 miel / 140 cire.
  Durée 3 min. Effet : +5 % défense des gardiennes.
- **II** — Prérequis : Carapace renforcée I, Poste de garde niveau 2.
  Coût 1 000 miel / 600 cire. Durée 8 min. Effet : +8 % défense
  supplémentaire (total +13 %).
- **III** — Prérequis : Carapace renforcée II, Poste de garde niveau 3.
  Coût 2 800 miel / 1 800 cire. Durée 15 min. Effet : +10 % défense
  supplémentaire (total +23 %), +5 % PV de garnison.

### Réflexes de patrouille (puissance active)
- **I** — Prérequis : Carapace renforcée I. Coût 300 miel / 200 pollen.
  Durée 4 min. Effet : +5 % puissance de patrouille (sorties/escouades).
- **II** — Prérequis : Réflexes de patrouille I, Coeur royal niveau 2.
  Coût 1 200 miel / 800 pollen. Durée 9 min. Effet : +8 % puissance
  supplémentaire (total +13 %), -5 % temps de trajet des sorties.
- **III** — Prérequis : Réflexes de patrouille II, Coeur royal niveau 3.
  Coût 3 200 miel / 2 200 pollen. Durée 16 min. Effet : +10 % puissance
  supplémentaire (total +23 %), -8 % temps de trajet supplémentaire.

### Alerte précoce (utilitaire, palier unique)
- Prérequis : Réflexes de patrouille II. Coût 1 500 miel / 900 cire /
  900 pollen. Durée 10 min. Effet : détection des menaces à portée étendue
  (préavis plus long avant une incursion), -10 % pertes en cas de défense
  réussie.

---

## Branche 3 — Population et formation

Accélère la croissance de la population et l'entraînement des castes
(gardiennes/voltigeuses/lanceuses, cf. choix de caste prévu).

### Nutrition larvaire (nurserie)
- **I** — Prérequis : Nurserie niveau 1. Coût 220 miel / 160 pollen.
  Durée 3 min. Effet : +5 % vitesse de développement du couvain.
- **II** — Prérequis : Nutrition larvaire I, Nurserie niveau 2. Coût 900 miel
  / 650 pollen. Durée 7 min. Effet : +8 % vitesse supplémentaire
  (total +13 %), +5 % stabilité du couvain.
- **III** — Prérequis : Nutrition larvaire II, Nurserie niveau 3.
  Coût 2 600 miel / 1 800 pollen. Durée 14 min. Effet : +10 % vitesse
  supplémentaire (total +23 %), +8 % stabilité supplémentaire.

### Formation accélérée (entraînement)
- **I** — Prérequis : Poste de garde niveau 1. Coût 260 miel / 180 cire.
  Durée 3 min. Effet : -5 % durée de formation des recrues.
- **II** — Prérequis : Formation accélérée I, Nutrition larvaire I.
  Coût 1 000 miel / 700 cire. Durée 8 min. Effet : -8 % durée
  supplémentaire (total -13 %).
- **III** — Prérequis : Formation accélérée II, Nutrition larvaire II.
  Coût 2 800 miel / 2 000 cire. Durée 15 min. Effet : -10 % durée
  supplémentaire (total -23 %), +1 place de formation simultanée.

### Lignées spécialisées (palier unique, convergence caste)
- Prérequis : Formation accélérée III. Coût 4 000 miel / 2 500 cire /
  2 500 pollen. Durée 20 min. Effet : débloque le choix explicite de caste
  principale (Gardiennes / Voltigeuses / Lanceuses) et son bonus associé.

---

## Branche 4 — Chambre royale et prestige

Prolonge le Coeur royal au-delà de son niveau de bâtiment, en synergie avec
le système VIP (bonus de confort, jamais de puissance militaire irremplaçable
— cf. règle du cap produit).

### Décret royal (gouvernance)
- **I** — Prérequis : Coeur royal niveau 2. Coût 500 miel / 300 pollen.
  Durée 4 min. Effet : +5 % vitesse de toutes les productions (miel/cire/
  pollen cumulées, effet plus large mais plus faible que les recherches de
  branche 1).
- **II** — Prérequis : Décret royal I, Coeur royal niveau 3. Coût 1 800 miel
  / 1 200 pollen. Durée 10 min. Effet : +8 % vitesse supplémentaire
  (total +13 %).
- **III** — Prérequis : Décret royal II, Coeur royal niveau 4. Coût 4 500
  miel / 3 000 pollen. Durée 18 min. Effet : +10 % vitesse supplémentaire
  (total +23 %), +5 % capacité de stockage globale.

### Diplomatie florale (fédération, palier unique)
- Prérequis : Décret royal I. Coût 1 200 miel / 800 cire / 800 pollen.
  Durée 8 min. Effet : débloque l'entraide inter-ruches (renforts, territoire
  floral partagé) — cf. adaptation `Fédération de ruches` de la référence
  Ant Legion. Nécessite le serveur de communication réel, pas de simulacre
  local (chantier actuellement gelé — voir `CLAUDE.md`).

---

## Résumé des dépendances (vue d'ensemble)

```
Branche 1 (Économie)
  Itinéraires I -> II -> III -\
  Alvéoles I -> II -> III ----+--> Réserves scellées
  Tri du pollen I -> II -> III /

Branche 2 (Défense)
  Carapace I -> II -> III
       |
  Réflexes I -> II -> III --> Alerte précoce

Branche 3 (Population)
  Formation I -> II -> III --\
  Nutrition I -> II -> III ---+--> Lignées spécialisées

Branche 4 (Chambre royale)
  Décret royal I -> II -> III
       |
       +--> Diplomatie florale
```

## Conséquences techniques (pour la prochaine session d'implémentation)

Le modèle serveur actuel (`ResearchCompletion`) ne porte que deux champs de
bonus fixes (`HoneyProductionBonusBps`, `WaxCapacityBonusBps`). Pour
implémenter cet arbre tel quel, il faudra le généraliser — même logique que
la généralisation faite aujourd'hui pour l'amélioration de bâtiment
(catalogue à entrées multiples, dépendances vérifiées côté serveur, durée et
coût configurables par recherche plutôt qu'un `AddSeconds(16)` codé en dur).
C'est un chantier de taille comparable à celui des améliorations de bâtiment
d'aujourd'hui — à traiter comme un prochain jalon, pas un ajustement rapide.

## Questions ouvertes pour Jeff

1. Les valeurs de coût/durée proposées sont des points de départ — veux-tu
   les ajuster avant qu'on les implémente ?
2. La branche 4 (Diplomatie florale) dépend du chantier Communication, gelé
   depuis Codex — la recherche elle-même peut être implémentée, mais son
   effet réel restera inactif tant que ce chantier ne reprend pas.
3. Faut-il prévoir un bâtiment "Laboratoire" dédié (comme dans Ant Legion),
   ou la recherche reste-t-elle indépendante de tout bâtiment physique dans
   Bee Kingdom ?
