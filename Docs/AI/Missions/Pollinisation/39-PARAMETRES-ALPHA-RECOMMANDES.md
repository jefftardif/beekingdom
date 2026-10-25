# 39 — Proposition de décisions par défaut pour avancer sans perdre de temps

**Ce document n'est PAS une approbation.** Il offre des valeurs et comportements par défaut **techniquement réversibles** à l'agent afin qu'il puisse commencer sous flag OFF/QA si le créateur n'a pas encore arbitré.

## V1 minimale recommandée

| Dimension | Défaut recommandé (non approuvé) | Pourquoi |
|---|---|---|
| Caste civile V1 | general_forager uniquement | simplicité du ledger |
| Population | entiers >=0, stock séparé des militaires | identité claire |
| Origine | sous-filière Nurserie « cohorte civile » | réutiliser bâtiment |
| Maturation | temps serveur + claim idempotent | conforme patterns existants |
| Nourriture | coût d'expédition, pas famine hors-ligne | éviter punition passive |
| Perte civile | aucune perte permanente à l'Alpha | moins d'attrition/risque |
| Blessure/fatigue | récupération temporaire si nécessaire | coût d'opportunité |
| Mission active | 1 à 2 civiles simultanées en QA | portée contrôlée |
| Sites | 2 sites publics trèfle compatibles | premier trajet réel |
| Mode | balanced au départ | écologie et économie équilibrées |
| Déplacements | source -> destination -> retour | gameplay distinctif |
| Écologie | fertilité + réserve, biodiversité préparée | surface minimale |
| Projet | un objectif de 2+ joueurs distincts | validateur collectif |
| Niveau d'alliance | progression durable documentée, design ADR | absence Level existant |
| Escorte | hors premier prototype, point d'extension prêt | éviter refonte combat |
| Pacte/territoire | hors premier prototype | pas de diplomatie incomplète |
| Récompense | honey/pollen officiels sous ledger | aucune nouvelle monnaie |
| Effet de floraison | production/régénération **future** | boucle durable |
| Bonus de niveau | effets bornés, valeurs catalogue | éviter max-level autowin |
| États Unity | serveur officiel + cache read-only | cohérence MMO |
| Déploiement | flag OFF, QA explicite avant ON | sécurité |

Les chiffres exacts (coût miel, 100 civils, 12 minutes, etc.) demeurent des **fixtures de tests**, jamais du tuning live.

## Décisions qu'on peut reporter proprement

- 5 classes civiles, conversion de spécialité, générations génétiques avancées ;
- relais, balises, contrôle direct de zones et diplomatie de pactes ;
- PvP d'escortes, mortalité civile durable ;
- diversification des biomes, saisons ;
- monnaies additionnelles, taxation d'alliance, marché interalliances ;
- raids mondiaux floraux, autre que spécification de la tanière de l'ours.

Les données / interfaces doivent néanmoins pouvoir supporter l'extension future sans réécriture massive.

## Décisions nécessitant une validation produit ferme avant activation réelle

1. Nombre exact de civils produits par cohorte, prix et durée.
2. Rendements honey/pollen/wax et effet de croissance régionale.
3. Définition de « niveau joueur » et « niveau alliance ».
4. Compteurs de contributions et minimum de participants distincts pour chaque type de projet.
5. Récompenses individuelles et collectives, caps.
6. Règles d'attaque, de pertes civiles et d'accès aux territoires.
7. Introduction de nouvelles ressources / achats / boosts.
8. Partage interalliances et pouvoirs de pacte.

## Si la conception manque d'information

Choisir un paramètre *configurable* et publier son statut dans un ADR. Ne pas inventer une « promesse » dans une interface publique à partir d'un exemple de document. L'agent doit réaliser les couches sûres possibles puis exposer le blocage précisément.

## Produit minimum démontrable

Deux comptes de test, deux ruches, un site partagé, un objectif de groupe. Joueur A lance butinage, joueur B aide, les révisions progressent, la fertilité est identique à tous, les points de projet sont calculés une fois, les civils rentrent même si les deux clients quittent le jeu.

C'est cette preuve, et non une nouvelle illustration de fleur, qui doit déclencher la validation du système.

