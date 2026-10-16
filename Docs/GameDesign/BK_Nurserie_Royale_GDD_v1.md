# BeeKingdom — Nurserie royale : conception officielle v1

**Approuvé par le fondateur le 14 octobre 2026.**
**État : direction produit approuvée. Les chiffres des couvées restent des hypothèses de balancing.**
**Date de restauration documentaire : 16 octobre 2026.**

## Mission

La Nurserie assure la croissance de la population d'abeilles de la ruche :
Nurserie (couvain) → réserve de recrues → Caserne (formation) → armée → Infirmerie (récupération).
La Nurserie ne soigne pas les blessées; l'Infirmerie ne produit pas de recrues.
Ne jamais supprimer ni rétrograder les troupes existantes lors de l'intégration à la Caserne.

## Deux jauges complémentaires

- **Nutrition, 0–100** : qualité de l'alimentation du couvain, influence la vitesse d'incubation.
- **Stabilité, 0–100** : hygiène et sécurité du couvain, influence le rendement final des couvées.
- **Vitalité globale = minimum(Nutrition, Stabilité)**, jamais une moyenne.
- Seuils de qualité : 0–39 fragile, 40–64 surveillance, 65–89 stable, 90–100 florissant.
- Conseiller de soigner prioritairement la jauge la plus basse.

### Soins déjà codés et à préserver

| Action | Coût de départ | Durée | Gain à Terminer |
|---|---:|---:|---:|
| Nourrir | 300 miel | 12 s | +22 Nutrition (cap 100) |
| Stabiliser | 45 cire | 13 s | +7 Stabilité (cap 100) |

Le serveur est l'autorité : coûts, opération, dates UTC, reçus anti-rejeu et révision optimiste.
Aucune action mutante si le joueur est hors ligne ou la commande est incertaine.

## Couvées

Une couvée passe par œufs, larves, nymphes, puis recrues matures.
Le serveur doit conserver une date de départ et une date de fin immuables, déterminées à partir de Nutrition et Stabilité au **démarrage** de la couvée.
Une couvée mature alimente automatiquement une réserve dédiée de recrues (pas directement le roster militaire).
Aucune récolte répétitive imposée. Si la réserve est pleine, ne pas lancer une nouvelle couvée et ne jamais effacer des unités silencieusement.

## Prototype de calcul (équilibrage non définitif)

| Palier | Nutrition / vitesse | Stabilité / rendement |
|---|---:|---:|
| 0–39 fragile | 0,80× | 0,75× |
| 40–64 surveillance | 0,90× | 0,89× |
| 65–89 stable | 1,00× | 1,00× |
| 90–100 florissant | 1,10× | 1,045× |

- Durée réelle = plafond(durée de base / facteur vitesse Nutrition).
- Recrues matures = plancher(base × facteur rendement Stabilité).
- L'efficacité combinée indicative n'est **jamais réappliquée** au calcul.
- Prototype niveau 1 : **une** couvée, base 60 min, base 30 recrues, réserve 120 recrues.
- Prototype sans coût additionnel d'incubation : minuterie et capacité limitent la production.
- Pas de mort permanente de larves à cause de l'absence du joueur; pas de dégradation automatique des jauges en v1.

## Progression ultérieure (jalons à harmoniser aux niveaux réels)

- Niv. 1 : première couvée et réserve de recrues.
- Niv. 3 : couvée agrandie.
- Niv. 5 : deuxième couvée simultanée.
- Niv. 8 : réduction d'incubation.
- Niv. 10 : troisième couvée et stockage accru.
- Ensuite : recherche et spécialisations Croissance rapide, Élevage économique, Lignée royale/Génétique.

## Architecture

1. État persisté par joueur/ruche avec migration nullable rétrocompatible.
2. Mutation atomique côté serveur, vérifications de révision et clé d'idempotence.
3. Aucun compte ne peut lire ou modifier les couvées d'un autre joueur.
4. Même reçus et même résultat après retry/reconnexion; maturation créditée une seule fois.
5. Aucun accès à la Caserne via les recrues avant migration et tests explicites, préservant les troupes historiques.
6. Interface premium basée sur Activités, responsive FR/EN, contrôles réels, défilement vertical et protections click-through.
7. Aucune fonction fictive annoncée comme disponible; signaler lorsque le serveur est désactivé.
8. Feature flag NurseryCohorts désactivé par défaut jusqu'à validation complète en staging.

## Lots

**Lot 1 :** moteur déterministe Nutrition/Stabilité, tests des seuils, plafonds et temps (réalisé en isolation).
**Lot 2 :** persistance des couvées, réserve, routes GET/POST authentifiées, client Unity désactivé et tests (serveur/client intégrés derrière flag, pas de production active).
**Lot 3 :** interface premium connectée, tests Play authentifiés et simulation de maturation de bout en bout sur staging.
**Lot 4 :** économies avancées/niveaux et pont Caserne après migration non destructrice.

### Critères avant activation

- 0 nouvelle régression serveur/Unity par rapport au baseline certifié.
- Tests SQL et migrations sur environnement isolé, anti-rejeu et isolation multi-compte.
- Reconnexion, double clic, horloge UTC et réserve pleine sans perte.
- Preuves visuelles mobiles/desktop validées par le fondateur.
- Aucun déploiement silencieux ni activation automatique.

**Décision de produit approuvée; les chiffres sont des coefficients de test, pas un engagement de balancing définitif.**
