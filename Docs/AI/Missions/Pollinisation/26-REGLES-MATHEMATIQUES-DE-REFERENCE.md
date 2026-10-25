# 26 — Mathématiques déterministes du système floral

**Statut : recommandations mathématiques pour l'agent, à valider par gameplay et simulation. Aucun coefficient final approuvé.**

## Principes

1. Le serveur calcule les gains, la fertilité, la consommation et les droits ; Unity n'interpole que l'affichage.
2. Les nombres de monnaie/abeilles/pollen transporté sont des **entiers** ; les indices utilisent basis points (0..10000).
3. Chaque calcul est borné, avec arrondi défini et overflow vérifié.
4. Les avantages restent **explicables** aux joueurs dans un rapport de mission.
5. Les effets des niveau joueur, alliance, Génétique, Académie, Recherche et championnes doivent être sensibles **mais non exponentiels**.
6. Les seuils de participants distincts constituent une règle **séparée** de la somme des points.

## Notation

- W = civils réellement engagés.
- B = production de base / abeille / cycle.
- T = multiplicateur de formation (bp).
- G = multiplicateur de génétique (bp).
- R = multiplicateur de recherche (bp).
- C = multiplicateur de championne (bp).
- E = coefficient d'environnement et compatibilité (bp).
- M = efficacité de mission selon mode (bp).
- A = bonus de coordination d'alliance (bp), plafonné.
- S = saturation liée au nombre d'essaims/surface/stock (bp).
- D = durée admissible de travail calculée par serveur.
- Q = stock floral mobilisable (unités entières).
- P = points écologiques, différents du pollen réellement récolté.

## Algorithme de travail recommandé

~~~text
baseWork = checked(workers * workRate * workDurationUnits)
qualityBp = clamp(
    applyBonusesInDeclaredOrder(T,G,R,C,E,M,A), minQualityBp, maxQualityBp)
workUnits = floor(baseWork * qualityBp / 10000)
limitedWorkUnits = min(workUnits, siteWorkRemaining, routeCapacity)
usefulWork = floor(limitedWorkUnits * saturationBp / 10000)
~~~

Ne pas calculer de gigantesques produits en int64 avant division ; appliquer des fonctions d'arithmétique sûre (checked + decimal bornée ou multiplicateur rationnel), et utiliser une stratégie cohérente pour les divisions successives.

### Règle d'empilement RECOMMANDÉE (simplification forte)

Regrouper les bonus par domaine puis les **additionner à l'intérieur d'une catégorie** ; multiplier seulement des domaines distincts et appliquer cap global. Exemple (NON final) :
- SkillBp = 10000 + min(TrainingBonusBp + GeneticsBonusBp, 2500)
- TechBp = 10000 + min(ResearchBonusBp + ChampionBonusBp, 2000)
- CoordinationBp = 10000 + min(AllianceBonusBp, 1000)
- FinalWorkBp = clamp(floor(SkillBp × TechBp × CoordinationBp / 10000²), 5000, 16000).
Les plafonds chiffrés sont exploratoires, **non approuvés**.

## Pollen et conservation : pas de « double dépense »

Le pollen du champ source est une **réserve florale**, pas encore le solde « pollen » du joueur.

~~~text
pollenTakenFromSource
    = pollenDepositedAtDestination
    + pollenConvertedToPlayerReward
    + pollenLostInTransit
    + pollenRemainingInCargo
~~~

Un même pollen physique ne peut, dans la même mission, être simultanément **transporté pour polliniser ET crédité comme monnaie** à 100 % des deux côtés. Si le gameplay veut un bonus de récolte supplémentaire, le décrire comme autre source d'événement vérifiée, distincte de cette conservation.

La conversion nectar -> honey officiel, pollen récolté -> pollen officiel, et cire accessoire doivent avoir un **catalogue de taux** et des limites. Les noms « nectar » et « pollen transporté » ne signifient pas automatiquement création de nouvelles devises.

### Conservation économique

~~~text
officialBalanceAfter
  = officialBalanceBefore
  - authorizedSpent
  + settledEarned
  - authorizedLoss
~~~

Chaque débit et crédit possède un receipt unique, même dans une grande coalition.

## Fertilité : effet différé

Une mission réussie n'accorde pas nécessairement immédiatement plus de miel. Elle modifie :
- P : contribution au projet de floraison ;
- F : fertilité (capacité future de régénération) ;
- Bio : biodiversité (variété et rareté à long terme) ;
- Rsv : réserve présente (récolte et régénération).

**Recommandation** : appliquer les effets positifs sur un ou plusieurs **cycles futurs**, pas grossir rétroactivement les récompenses déjà reçues lors de la même mission.

~~~text
fertilityNextBp =
  clamp(fertilityCurrentBp
      + positivePollinationBp
      + naturalRecoveryBp
      - overHarvestPressureBp
      - eventDamageBp, 0, 10000)
~~~

### Régénération avec horloge absolue

~~~text
elapsed = max(0, min(serverNow - lastSettledAt, maxCatchUpWindow))
newReserve = min(capacity, oldReserve + regenPerTime * elapsed)
lastSettledAt = serverNow (ou date calculée selon choix ADR)
~~~

Stocker un éventuel « reste » fractionnaire si l'arrondi à chaque lecture crée une perte injustifiée ; ne pas augmenter la réserve par simple GET non transactionnel concurrent.

## Travail collectif et impossibilité du solo régional

~~~text
eligiblePlayers = COUNT(DISTINCT playerId)
  WHERE effectiveContributionsInPeriod >= minimumUsefulThreshold

projectComplete =
  totalEffectivePoints >= projectGoal
  AND eligiblePlayers >= minDistinctPlayers
  AND distinctApprovedRoutes >= minRoutes
  AND projectOpenAndAuthorized
~~~

La capacité de contribution peut dépendre du joueur, mais le minimum de joueurs **ne se convertit jamais en points**. Un joueur même extrêmement riche ne peut pas produire 12 identités de joueur.

### Plafond personnel / saturation

- maxContributionPerPlayerPerProjectPeriod évite domination solitaire.
- cap project n'interdit pas au joueur de continuer son butinage **économique** : les gains écologiques de projet sont plafonnés, pas nécessairement la récolte.
- les partenaires sous pacte sont admissibles sous un contrat explicite, sans fausse appartenance à l'alliance initiatrice.

## Impact du niveau d'alliance

Recommandation : 80 % de l'effet sur **l'échelle des projets et la logistique** (slots, relais, accès, capacité de coordination), et seulement une part modeste sous forme de rendement. Le chiffre 80 % ici exprime une orientation qualitative, pas un ratio calculable. Une alliance inactive ne dépasse pas des seuils de participation par son niveau.

## Exemple arithmétique NON contractuel

Essaim 100 civils, base 2 unités de travail chacun :
- baseWork = 200.
- formation +10 % et génétique +5 % = SkillBp 11500.
- recherche +8 %, championne +4 % = TechBp 11200.
- alliance +5 % = CoordinationBp 10500.
- avant environnement, facteur combiné environ 1,3524 -> travail projet environ 270 unités.
- si contribution déjà près du plafond personnel : seules les unités admissibles sont comptées dans le projet.
- la récompense de honey/pollen n'est pas nécessairement 270 : dépend du rendement, de la cargaison et du stock.

**Tester exactement l'arrondi** en C# avec nombres extrêmes, zéro, manque de stock et mêmes paramètres dans 2 clients.

## Analyse anti-exploit économique

Simuler au moins 100 cycles de 5 cohortes types ; vérifier aucun gain de ressources infini via naissance -> mission -> naissance, aucun bonus croissant sans saturation, aucune accumulation de crédits par aller-retour impossible physiquement.

## Mesures pour le système de télémétrie

- ressources nettes créées par jour/serveur et par type ;
- indice de Gini ou concentration des contributions ;
- durée nécessaire au projet selon taille/rang d'alliance ;
- % projets terminés avec 1 seul contributeur (doit être **zéro** pour grands projets) ;
- valeur relative des parcours militaire/économique ;
- variation moyenne/max de fertilité ;
- taux de sites appauvris durablement ;
- nombre de coups de frein anti-farming.

