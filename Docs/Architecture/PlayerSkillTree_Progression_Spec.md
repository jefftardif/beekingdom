# Player Skill Tree and Progression Specification

Date locale: 2026-07-15  
Status: DESIGN_READY_FOR_IMPLEMENTATION  
Scope: progression joueur, arbre de competences, classes et laboratoire local

## 1. Intention

Chaque gain d'experience fait progresser le niveau du joueur et attribue un point de competence. Les points sont conserves lorsqu'ils ne peuvent pas encore etre depenses. Le niveau 10 ouvre l'interface de competence et impose le choix d'une classe.

Le systeme repose sur trois arbres:

1. `Combat`: survivre, attaquer, commander les soldats et gagner les duels/raids.
2. `Ressources / Evolution`: collecte, transport, production, construction et developpement de la ruche.
3. `Classe`: identite de combat et de soutien propre a la classe choisie au niveau 10.

La ruche peut afficher la progression visuelle de ses talents, mais les bonus restent des donnees de progression du joueur. Ils ne doivent pas etre peints dans le terrain Wave5 ou Wave6.

## 2. Regles de niveau et de points

### 2.1 Niveaux

- Niveaux jouables: `1..50` pour la premiere version.
- Niveau initial: `1`.
- Une seule source de verite doit calculer `xp_total`, `level` et `skill_points_unspent`.
- Les seuils XP sont versionnes et ne doivent jamais etre modifies sous le meme identifiant de table.
- Proposition de table initiale: `xp_to_next = round(100 * level^1.65)` avec une table materialisee par version pour permettre un rebalance sans casser les sauvegardes.

### 2.2 Points

- Chaque niveau gagne attribue exactement `1` point.
- Les points des niveaux 1 a 9 sont mis en reserve car l'arbre est verrouille avant le niveau 10.
- Au niveau 10, les points reserves deviennent depensables et le choix de classe ouvre le troisieme arbre.
- Les niveaux 10 a 50 continuent d'attribuer `1` point par niveau.
- Au niveau 50, un joueur possede donc `50` points attribues au total, moins les points deja depenses.
- Un gain XP ne peut jamais attribuer deux fois le meme niveau ou le meme point.
- Les points depenses et non depenses sont toujours visibles dans l'interface.

### 2.3 Classe au niveau 10

- Avant le niveau 10, la classe effective est `Neutral` et l'arbre de classe est verrouille.
- Au niveau 10, le joueur choisit une classe parmi:
  - `RoyalGuard`
  - `Striker`
  - `Nurturer`
  - `Scout`
  - `Alchemist`
- Le choix de classe est persistant dans la progression officielle.
- Le changement de classe n'est pas gratuit en production: il demande une action explicite, un cout versionne et un delai de re-specialisation.
- Dans le laboratoire local, le changement est instantane, sans cout et sans gain officiel, pour faciliter les tests.
- `Neutral` reste une classe de preview uniquement et ne doit pas apparaitre comme choix final au niveau 10.

## 3. Structure commune des arbres

Chaque arbre est un graphe acyclique versionne. Un noeud contient:

```text
skill_id
tree_id
class_id|null
display_key
description_key
max_rank
cost_per_rank
prerequisite_skill_ids
required_level
effect_type
effect_value_by_rank
exclusive_group|null
schema_version
```

Regles:

- Un noeud ne peut etre achete que si tous ses pre-requis sont satisfaits.
- Le cout est paye rang par rang.
- Une competence ne peut pas depasser `max_rank`.
- Les competences partageant un `exclusive_group` ne peuvent pas etre maximisees ensemble.
- Une modification de valeurs exige une nouvelle `skill_table_version`.
- Les bonus sont calcules par le moteur de progression, puis exposes au combat/economie via un profil immuable de joueur.
- Aucun arbre ne doit modifier directement les PNG, le terrain, les tuiles ou les landmarks.

## 4. Arbre Combat

L'arbre Combat est commun a toutes les classes. Il doit proposer des choix utiles sans remplacer l'identite de la classe.

| ID | Competence | Rangs | Pre-requis | Effet principal |
|---|---|---:|---|---|
| `combat_foundation` | Instinct de ruche | 3 | aucun | degats generaux +2/+4/+6 % |
| `combat_vitality` | Carapace renforcee | 3 | aucun | PV maximum +3/+6/+9 % |
| `combat_command` | Commandement | 3 | foundation 1 | puissance des soldats +3/+6/+9 % |
| `combat_guard` | Ligne de garde | 3 | vitality 1 | degats recus par la ruche -2/-4/-6 % |
| `combat_focus` | Cible prioritaire | 2 | command 2 | degats sur elite +5/+10 % |
| `combat_swarm` | Essaim coordonne | 3 | command 1 | cooldown d'attaque -2/-4/-6 % |
| `combat_counter` | Riposte | 3 | guard 2 | chance de contre 3/6/9 % |
| `combat_raid` | Tactiques de raid | 3 | focus 1 ou swarm 2 | efficacite T5-T7 +3/+6/+9 % |
| `combat_last_stand` | Dernier rempart | 1 | guard 3, vitality 3 | une reduction d'urgence par combat |
| `combat_mastery` | Maitrise martiale | 1 | 8 rangs Combat | +5 % au profil de combat final |

Les bonus Combat doivent respecter les caps du laboratoire combat et ne doivent pas transformer un test local en gain officiel.

## 5. Arbre Ressources / Evolution

Cet arbre sert la collecte et l'evolution de la ruche. Il ne donne pas de ressources par magie: il modifie l'efficacite d'une action legale de collecte, de transport ou de production.

| ID | Competence | Rangs | Pre-requis | Effet principal |
|---|---|---:|---|---|
| `resource_foraging` | Butinage efficace | 3 | aucun | vitesse de collecte +4/+8/+12 % |
| `resource_sense` | Sens des ressources | 3 | aucun | detection des nodes +4/+8/+12 % |
| `resource_capacity` | Alveoles profondes | 3 | foraging 1 | capacite de transport +5/+10/+15 % |
| `resource_route` | Routes de collecte | 3 | sense 1 | temps de trajet -3/-6/-9 % |
| `resource_refine` | Raffinage propre | 3 | capacity 2 | rendement de transformation +3/+6/+9 % |
| `resource_construction` | Evolution de la ruche | 3 | capacity 1 | cout de construction -2/-4/-6 % |
| `resource_rare` | Instinct des rares | 2 | sense 3 | priorite de detection R3 +5/+10 % |
| `resource_recovery` | Cycle durable | 3 | refine 2 ou route 2 | recuperation de collecte +3/+6/+9 % |
| `resource_specialist` | Specialiste economique | 1 | 8 rangs Ressources | +5 % d'efficacite globale hors combat |
| `resource_mastery` | Maitrise de l'evolution | 1 | specialist 1 | une file d'evolution supplementaire en preview |

Le moteur de ressources doit continuer a appliquer les caps, distances, exclusions, contention et anti-farm deja definis par `ResourceSpawnEconomySpec.md`.

## 6. Arbres de classe

Chaque classe possede une branche courte et lisible. Les noeuds de classe sont indisponibles tant que la classe n'est pas choisie.

### 6.1 RoyalGuard

Identite: protection de la ruche, tenue de ligne et defense des allies.

| ID | Competence | Rangs | Effet |
|---|---|---:|---|
| `royalguard_iron_wall` | Mur de cire | 3 | reduction des degats recus |
| `royalguard_taunt` | Provocation royale | 2 | attire les attaques vers les gardes |
| `royalguard_hive_bastion` | Bastion de ruche | 3 | PV de la ruche et gardes augmentes |
| `royalguard_last_oath` | Dernier serment | 1 | protection d'urgence de l'essaim |
| `royalguard_command` | Ordre royal | 2 | bonus aux unites proches |

### 6.2 Striker

Identite: degats, vitesse d'execution et pression sur une cible prioritaire.

| ID | Competence | Rangs | Effet |
|---|---|---:|---|
| `striker_venom_tip` | Dard venimeux | 3 | degats sur la duree |
| `striker_frenzy` | Frenezie | 3 | vitesse d'attaque sous seuil |
| `striker_precision` | Pique precise | 2 | chance de critique |
| `striker_execute` | Finisseur | 2 | degats sur cible affaiblie |
| `striker_breaker` | Brise-carapace | 1 | ignore une partie de l'armure |

### 6.3 Nurturer

Identite: soin, croissance de la ruche et maintien de l'essaim.

| ID | Competence | Rangs | Effet |
|---|---|---:|---|
| `nurturer_pollen_mend` | Soin au pollen | 3 | soin periodique |
| `nurturer_brood_care` | Soin du couvain | 3 | recuperation des unites |
| `nurturer_bloom_aura` | Aura florale | 2 | bonus allies proches |
| `nurturer_reserve` | Reserve de nectar | 2 | ressource de secours en scenario |
| `nurturer_rebirth` | Seconde floraison | 1 | une recuperation d'urgence |

### 6.4 Scout

Identite: reconnaissance, vitesse de collecte, evasion et lecture de la carte.

| ID | Competence | Rangs | Effet |
|---|---|---:|---|
| `scout_trail_sense` | Sens des pistes | 3 | detection ressources et monstres |
| `scout_swift_wings` | Ailes rapides | 3 | vitesse de deplacement |
| `scout_ambush` | Embuscade | 2 | bonus au premier contact |
| `scout_cartography` | Cartographie | 2 | rayon d'inspection augmente |
| `scout_escape` | Repli agile | 1 | reduction du risque de perte |

### 6.5 Alchemist

Identite: reactions, debuffs, raffinage et preparation des raids.

| ID | Competence | Rangs | Effet |
|---|---|---:|---|
| `alchemist_acid_resin` | Resine acide | 3 | reduction de defense ennemie |
| `alchemist_catalyst` | Catalyseur | 3 | rendement de transformation |
| `alchemist_volatile_honey` | Miel volatil | 2 | degats de zone controles |
| `alchemist_antidote` | Antidote | 2 | resistance aux effets |
| `alchemist_master_mix` | Melange majeur | 1 | amplifie une preparation de raid |

## 7. Regles d'equilibrage

- Les trois arbres doivent rester viables: Combat pour l'affrontement, Ressources pour la croissance, Classe pour l'identite.
- Aucun noeud unique ne doit etre obligatoire pour toutes les classes.
- Les bonus additifs sont preferes aux multiplicateurs en cascade.
- Les bonus de vitesse, degats, capacite et reduction sont soumis a des caps versionnes.
- Le calcul final doit fournir un `PlayerSkillProfile` immutable au combat et a l'economie.
- Un profil de competence ne doit pas changer les valeurs d'une partie deja lancee sans evenement de recalcul explicite.

## 8. Re-specialisation

Production:

- action explicite depuis l'ecran de competences;
- cout en ressource rare versionne;
- cooldown de re-specialisation;
- confirmation avant perte des points;
- historique local de la derniere re-specialisation.

Laboratoire local:

- bouton `Reset talents` sans cout;
- bouton `Set level`;
- bouton `Set class`;
- bouton `Apply preview`;
- remise a zero deterministe;
- aucun XP, inventaire, gain officiel ou persistence serveur.

## 9. Contrat de donnees

Exemple de profil versionne:

```json
{
  "schema_version": "player_progression_v1",
  "skill_table_version": "skill_tree_v1",
  "xp_table_version": "xp_curve_v1",
  "player_id": "LOCAL_TEST_PLAYER",
  "level": 10,
  "xp_total": 3650,
  "class_id": "Scout",
  "skill_points_awarded": 10,
  "skill_points_spent": 0,
  "skill_points_unspent": 10,
  "ranks": {},
  "authority": {
    "server": false,
    "official": false,
    "official_gain": false,
    "source_kind": "local_demo"
  }
}
```

La couche officielle remplacera uniquement `authority` et la persistance. Elle ne doit pas modifier les identifiants, la topologie ou les prerequis de l'arbre sans migration explicite.

## 10. Interface joueur

L'ecran de competences doit afficher:

- niveau, XP actuelle, XP requise et points disponibles;
- trois onglets `Combat`, `Ressources / Evolution`, `Classe`;
- verrou visible avant le niveau 10;
- choix de classe explicite au niveau 10;
- noeuds achetables, verrouilles et maximises avec une legende claire;
- pre-requis visibles sans surcharge;
- bouton de re-specialisation hors du chemin d'achat;
- apercu des bonus avant confirmation;
- indication persistante du profil actif.

Les elements de l'interface restent fixes pendant le pan/zoom de la WorldMap. L'arbre ne doit pas masquer la carte, les ruches, les ressources ou les landmarks.

## 11. Tests indispensables

| Gate | Verification |
|---|---|
| `LEVEL_XP_EXACTLY_ONCE` | un niveau attribue exactement un point |
| `PRE_LEVEL_10_LOCK` | les arbres sont visibles mais non depensables avant 10 |
| `LEVEL_10_CLASS_REQUIRED` | une classe valide ouvre le troisieme arbre |
| `CLASS_TREE_ISOLATION` | une classe ne voit pas les noeuds d'une autre |
| `PREREQUISITE_GATE` | aucun achat illegal n'est accepte |
| `POINT_BUDGET` | depense <= points attribues |
| `RESET_DETERMINISTIC` | reset restaure exactement le budget initial |
| `PROFILE_IMMUTABLE_HANDOFF` | combat/economie recoivent le meme profil calcule |
| `LOCAL_AUTHORITY_ONLY` | le labo garde `server=false` et `official_gain=false` |
| `LEGACY_WAVE5_REGRESSION` | aucun PNG, master, tuile ou BearDen modifie |

## 12. Ordre de developpement

1. Implementer le modele `player_progression_v1` et les tables versionnees XP/skills.
2. Ajouter le calcul deterministe des points et le choix de classe niveau 10.
3. Brancher le laboratoire local aux deux ruches test deja editables.
4. Ajouter l'interface des trois arbres et le preview des bonus.
5. Connecter le `PlayerSkillProfile` au combat puis a l'economie, avec caps et regression.
6. Faire la contre-revue UI/QA/Builder-C avant toute activation officielle.

## 13. Garde de perimetre actuelle

Cette specification ne modifie pas Unity, Wave5, Wave6, les 625 tuiles, BearDen, l'APK, le serveur ou les donnees reelles. Les tests de progression peuvent fonctionner dans le laboratoire local, mais aucune action locale ne doit etre presentee comme progression officielle du joueur.

### Verdict

`SKILL_TREE_DESIGN = READY`  
`LEVEL_PROGRESS_RULES = READY`  
`CLASS_AT_LEVEL_10 = READY`  
`LOCAL_TEST_HOOKS = READY`  
`OFFICIAL_PROGRESSION_IMPLEMENTATION = NOT_RUN`
