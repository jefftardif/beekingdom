# 27 — Catalogues et données initiales (exemples non approuvés)

## Intention

Un agent codeur doit sortir les paramètres de conception du code. **Tous les prix, capacités, noms et durées ci-dessous sont proposés** pour un bac de simulation et des tests, pas une économie finale à publier.

## Entités de catalogue minimales

**CivilWorkerCatalog**
- workerKey stable, displayNameKey, availabilityStage, prerequisites, specializations, baseWorkRate, carryingCapacity, range, riskModifier, academyRequirement, geneticAffinitySlots.

**CivilCohortCatalog**
- cohortTypeKey, nurseryBuildingLevelMinimum, basePopulation, baseIncubationTime, costsByResource, nutritionRequirement, stabilityRequirement, reserveCapacityPolicy, claimMode.

**FloralSpeciesCatalog**
- flowerFamilyKey, displayNameKey, environmentTypes, bloomWindows, nectarCapacity, pollenCapacity, compatibilityWith, regenRate, biodiversityValue, rarityWeight.

**FloralSiteCatalog**
- siteId stable, regionId, existingMapAnchor, floralFamily, tier, capacity, startingFertilityBp, startingBiodiversityBp, public/territorial flag, dangerTier, maxConcurrentExpeditions, localizationKey, iconKey, eventEligibility.

**MissionModeCatalog**
- modeKey, harvestMultiplierBp, pollinationMultiplierBp, overHarvestPressureBp, restorationMultiplierBp, hazardMultiplierBp, minAcademyLevel, UI description key.

**AllianceFloralProgressionCatalog**
- level, xpThreshold, projectSlots, maxProjectTier, relaySlots, pactSlots, coordinationBonusBp, requirements.

**PollinationProjectCatalog**
- projectTemplateId, regionTier, requiredAllianceLevel, memberCountMin, pointsRequired, uniqueRoutesRequired, deadline, rewardRules, maintenanceEffects, maxContributors.

**EventCatalog**
- eventKey, eligibleRegions, cooldown, noticeDuration, participationWindow, rewardTiers, anti-monopolyCaps, featureFlag.

## Espèces candidates Alpha

| Famille/espèce | Rôle initial | Rareté | Remarques |
|---|---|---|---|
| clover | champ de trèfles | commune | route de départ |
| wildflower | prairie fleurie | commune | découverte et diversité |
| melliferous_tree | arbre mellifère | avancée | site collectif |
| rare_lumina | fleur rare fictive | exceptionnelle | Bêta, événement |

Les codes sont des propositions. Si FlowerManager ou le monde officiel utilisent d'autres codes, établir une table de **mapping explicite** et préserver les IDs actifs.

## Sites pilotes : 3 modèles seulement

~~~json
{
  "catalogVersion": "floral-content-prototype-v1",
  "sites": [
    {
      "siteId": "qa-clover-a",
      "regionId": "qa-meadow",
      "flowerFamilyId": "clover",
      "tier": "public_small",
      "startingFertilityBp": 6000,
      "startingBiodiversityBp": 4500,
      "nectarCapacity": 1000,
      "pollenCapacity": 750,
      "isPublic": true
    },
    {
      "siteId": "qa-clover-b",
      "regionId": "qa-meadow",
      "flowerFamilyId": "clover",
      "tier": "public_medium",
      "startingFertilityBp": 4800,
      "startingBiodiversityBp": 5700,
      "nectarCapacity": 1500,
      "pollenCapacity": 900,
      "isPublic": true
    },
    {
      "siteId": "qa-forest-ancestor",
      "regionId": "qa-forest",
      "flowerFamilyId": "melliferous_tree",
      "tier": "alliance_large",
      "startingFertilityBp": 7500,
      "startingBiodiversityBp": 6200,
      "nectarCapacity": 5500,
      "pollenCapacity": 3000,
      "isPublic": false
    }
  ]
}
~~~

**Ne pas déployer les identifiants QA en production**, ne pas générer des positions aléatoires à chaque démarrage : relier à des ancrages de carte stables, tester collisions avec bâtiments/NPC et tanière existante.

## Exemple de modes (pas chiffres retenus)

| Mode | Récolte x | Pollinisation x | Pression sur la fertilité |
|---|---|---|---|
| intensive | 1,30 | 0,70 | plus forte |
| balanced | 1,00 | 1,00 | modérée |
| restore | 0,45 | 1,40 | amélioration nette potentielle |

Les bonus ne doivent pas générer de cargaison supérieure au stock ; prélever d'abord le stock disponible puis calculer la récompense autorisée.

## Exemple de stades de projet (non approuvé)

Paliers : 25 % -> première floraison ; 60 % -> production meilleure ; 100 % + nombre distinct + routes -> floraison remarquable. La compétition entre alliances doit être une **option** sur certaines zones, pas une obligation PvP pour une simple fleur publique.

## Validation de configuration

Rejeter au démarrage :
- clés dupliquées, chaîne vide, ID hors longueur, références à famille inexistante ;
- coûts négatifs, durées zéro, capacité négative, indicateurs hors 0..10000 ;
- route source/destination incompatible/absente ;
- projet qui demande plus d'alliances que le maximum autorisé ;
- bonus impossible à plafonner ou débordement ;
- points de projet innaccessibles même avec plusieurs joueurs plausibles ;
- icônes/localisations FR et EN manquantes pour un site visible.

## Gestion de version

Catalogue immuable pour une mission déjà engagée ; nouveau catalogueVersion peut ajouter des sites, corriger des valeurs futures, mais ne modifie pas silencieusement les gains d'une mission à mi-parcours. Journaliser source version dans receipts.

## Données localisables

Aucun texte joueur hardcodé dans un objet serveur ; les codes pointent vers clés de localisation en fr-CA/en-US. Les descriptions de sites incluent leur rôle, compatibilité, menaces et objectif collectif.

