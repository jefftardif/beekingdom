# 17 — Registre de décisions et questions non tranchées

**Règle :** ne pas déguiser les exemples et propositions en décisions du créateur. Chaque décision future doit être datée, avoir une conséquence sur les autres documents et être ajoutée ici.

## Décisions explicites du 2026-10-20

| ID | Décision | État |
|---|---|---|
| D-001 | Pas de refonte paysage/tuiles ; enrichir objets/activité de la carte | APPROUVÉ |
| D-002 | Pollinisation pilier majeur économique et territorial | APPROUVÉ |
| D-003 | Architecture hybride zones publiques et territoires d'alliance | APPROUVÉ |
| D-004 | Solo faible impact régional, projets floraux affaire de groupe/alliance | APPROUVÉ |
| D-005 | Niveau/activité alliance et niveaux des joueurs influencent réellement la pollinisation | APPROUVÉ |
| D-006 | Essaims civils spécialisés séparés des militaires | APPROUVÉ |
| D-007 | Militaires pour escortes, protection et conflits associés | APPROUVÉ |
| D-008 | Tanière de l'ours : événement mondial de coopération multi-alliance ET compétition récompenses | APPROUVÉ |
| D-009 | Documenter sans perdre les idées, dossier Markdown dans Docs/AI/Missions/Pollinisation | APPROUVÉ |

## Arbitrages prioritaires avant le Lot 1

| ID | Question | Option recommandée | Conséquence |
|---|---|---|---|
| Q-01 | Une unité civile représente-t-elle 1 abeille ou un lot abstrait ? | Lot/essaim à population conservée | Taille des états et UX |
| Q-02 | Les civils naissent-ils via Nurserie indépendante ? | Sous-filière civile distincte, sans duplication | Modèle et migration |
| Q-03 | Départs/claims de cohortes manuels ou automatiques ? | Claim serveur idempotent, notifications | UX/offline |
| Q-04 | Combien de spécialisations à l'Alpha ? | Butineuse polyvalente V1 seulement | Portée de l'Alpha |
| Q-05 | Niveau joueur déterminé par quelle donnée officielle ? | Niveaux de bâtiments/recherches validés | Règles de progression |
| Q-06 | Quel entretien consomme le civil ? | Coût de mission, pas famine hors ligne en Alpha | Économie et rétention |
| Q-07 | Perte permanente civile possible ? | Retour/blessures, perte définitive différée | Équité PvP |
| Q-08 | Premier site : local seul ou route deux champs ? | Modèle compatible deux champs, pilote E2E court | Architecture routes |

## Arbitrages avant Lot 3 — collectif

| ID | Question | Option recommandée |
|---|---|---|
| Q-09 | Mesure « joueur actif » | Contribution livrée et qualifiée dans fenêtre |
| Q-10 | Taille minimale groupes et caps par personne | Par niveau de projet, config serveur |
| Q-11 | Récompenses de participation/classement | Séparées, minimum pour vrais participants |
| Q-12 | Application des niveaux d'alliance | Déblocage d'échelle/slots + bonus bornés |
| Q-13 | Une mission sert-elle plusieurs projets ? | Un projet lié par mission en Alpha |
| Q-14 | Familial floral et adaptation génétique | Petit catalogue stable V1 |
| Q-15 | Trois modes dès l'Alpha ? | Équilibré d'abord, trois modes avant fin Alpha |

## Arbitrages avant territoire/PvP

| ID | Question | Option recommandée |
|---|---|---|
| Q-16 | Distinction Ruche-Mère / Balise / Relais | Réutiliser modèle territoire existant, définir 1 première installation |
| Q-17 | Capturer quoi et comment ? | Relais/position seulement, pas fertilité |
| Q-18 | Accès étrangers aux champs en territoire | Public / partenaire / privé, règles claires |
| Q-19 | Droits et tarifs d'accès | Gratuit ou contrôlé au début, taxes différées |
| Q-20 | PvP sur civils en zone publique | Interdit près des débutants |
| Q-21 | Escorte sans défense | Retraite et pertes limitées |
| Q-22 | Cycles d'attaque/vulnérabilité | Créneaux annoncés et protections fuseaux |
| Q-23 | Pertes écologiques pendant guerre | Aucune destruction permanente |

## Arbitrages événements et long terme

- Q-24 : saisons et météo influencent-elles la fertilité dès Bêta ?
- Q-25 : apparition de fleurs rares par biodiversité et traits génétiques ?
- Q-26 : ours lié à la production de miel régionale ou événement mondial autonome ?
- Q-27 : récompenses ours selon dégâts, objectifs civils, alliances, minimum de participation ?
- Q-28 : coalitions max, durée de pacte, ruptures et retours sûrs ?
- Q-29 : nouveaux types de ressources/« nectar » : monnaie réelle ou étiquette visuelle ?
- Q-30 : maintenance d'infrastructures territoriales et progression au-delà de l'Alpha ?
- Q-31 : serveur tick/lazy settlement, transaction multi-agrégats et index mondial ?
- Q-32 : comment communiquer visuellement les zones et la fertilité sans saturer la carte ?

## Modèle d'entrée de décision future

- Date et auteur :
- Référence (D-... ou Q-...) :
- Options évaluées :
- Choix validé :
- Raisons :
- Effets sur fichiers de conception :
- Effets sur données/API/QA/équilibrage :
- Stade d'implantation :
- Preuves/test à créer :

## État de cette mission

Dossier **documentaire uniquement** ; aucune décision technique détaillée ci-dessus n'est réputée « livrée » simplement parce qu'elle est écrite.


## Questions complémentaires détectées lors de l'audit code canonique

- **Q-33** : niveau d'alliance **général** ou maîtrise florale distincte ? AllianceEntity ne contient pas de Level explicite dans l'audit.
- **Q-34** : comment intégrer FlowerManager/PollinationRules actuels à la source officielle serveur sans double simulation ?
- **Q-35** : faut-il des « slots » civils indépendants de la limite de trois marches actuelles du registre client ?
- **Q-36** : quand une mission pollinise, quelle part du pollen est déposée écologiquement et quelle part devient monnaie dans l'inventaire ?
- **Q-37** : faut-il garantir la récompense au retour automatique ou demander un claim manuel, tout en évitant qu'une mission hors-ligne bloque les projets ?
- **Q-38** : la naissance civile se sert-elle du mécanisme NurseryCohorts existant, ou d'un sous-état spécifique pour éviter de mélanger les recrues ?
- **Q-39** : comment gère-t-on la concordance des ID de fleurs du moteur local avec les ID des sites officiels monde ?
- **Q-40** : transaction SQL atomique inter-domaines ou saga/outbox avec réconciliation si repositories séparés ?

L'agent peut recommander des choix techniques réversibles ; toute règle touchant économie, droit territorial ou perte civile doit être clairement distinguée d'un choix approuvé.


## Notes d'implantation provisoires — 2026-10-21 (NON approuvées en design)

**ADR-001** : population civile V1 via sous-état propre `CivilPopulation` de la ruche ; naissance payée et maturation automatique à l'horloge serveur sous feature flag OFF. Coûts, effectifs et durées sont des paramètres QA sans accord produit ; Q-01 à Q-08/Q-38 restent ouverts pour activation.

**ADR-002** : sites officiels globaux, SQL transaction multientité ruche + site sur même base avant tout débit/crédit floral. Le repo JSON mono-ruche ne protège pas une zone partagée : ses mutations multientités restent OFF. Q-31 et Q-40 restent ouverts pour stratégie de reprise et validation SQL en environnement réel.

**ADR-003** : une progression d'alliance générale et durable est à construire séparément de la Recherche d'alliance. Q-33 et les seuils de participants restent ouverts.

Voir `52-ADR-ARCHITECTURE-ET-EXECUTION-20261021.md`. Aucun de ces choix techniques ne modifie les décisions D-001 à D-009.
## ADR-P4 (technique réversible, NON une approbation produit) — 2026-10-21

Les projets d'alliance sont maintenant persistants et testés localement. Les conditions QA par défaut sont objectif 100 points + 2 membres qualifiés à 5 points chacun; progression d'alliance séparée avec seuils XP 100/300 et slots 1/2. **Aucune valeur n'est approuvée par le créateur.** Garder ouvertes les questions de seuils, niveau, bonus, participation, politique de changement d'alliance et projet actif interrompu.

Politique QA retenue temporairement : compter une mission dans le projet actif du site uniquement si elle était lancée après sa création et si le joueur appartient encore à son alliance de départ au règlement. Une migration d'alliance après départ n'attribue pas la récolte à la nouvelle alliance. Les ressources individuelles sont préservées. Politique de drainage avant coupure du flag obligatoire à définir.

Références et preuves LocalDB : 54-VALIDATION-COLLECTIVE-P4-20261021.md. Aucun changement aux décisions D-001 à D-009.
