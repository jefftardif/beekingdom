# 04 — Formation, spécialisation et progression des civils

## Architecture de progression

**[APPROUVÉ]** La qualité d'une expédition civile dépend de la progression réelle du joueur et de sa ruche. **[RECOMMANDÉ]** Quatre couches distinctes, complémentaires et plafonnées :

1. **Nurserie** : quantité et capacité démographique.
2. **Académie** : formation pratique des butineuses et ouverture de spécialisations.
3. **Génétique/Transformation** : qualités biologiques spécialisées de la lignée, non équivalentes à un diplôme.
4. **Recherche** : technologie permanente collective à la ruche et améliorations logistiques.

**Hall des championnes** : leadership de mission et talents spécifiques, pas source d'abeilles supplémentaires.

## Progression en deux vagues

**Alpha V1 :** une seule caste civile « Butineuse polyvalente ». Niveau de compétence simple, deux ou trois améliorations lisibles, aucune nouvelle monnaie ni conversion militaire.

**Alpha V2 / Bêta :** rôles spécialisés (choix, non multiplication des compteurs à l'infini) :
- **Pollinisatrice** : contribution écologique et transport de pollen.
- **Récolteuse** : capacité de nectar/pollen et cadence d'extraction.
- **Éclaireuse florale** : repérage des fleurs rares, ouverture de routes et réduction du risque.
- **Logisticienne** : portée, cargaison, endurance et fonctionnement des relais.
- **Restauratrice** (option) : amélioration de la fertilité/dégradation, distincte de la production directe.

Les spécialisations sont des capacités civile à gérer côté serveur ; deux rôles ne doivent pas se cumuler à 100 % sans coût/opportunité. Prévoir changement de rôle encadré (hors mission).

## Académie : ne pas casser les formations existantes

Audit 2026-10-20 : la mission BKSTUDIO-20261019-ACADEMY-TRAINING-V1.md décrit quatre matières déjà construites côté code : Tactics, Logistics, Exploration, Champion Mentorship, avec certificats et crédits. À la date du document, la production n'était pas certifiée.

**Recommandation :** ajouter un **programme civil** versionné (par exemple « Initiation au butinage », « Routes florales », « Pollinisation avancée »), via le service existant uniquement après analyse des contraintes de cours, files et diplômes. Un certificat militaire ne doit pas donner automatiquement des points floraux.

Distinguer formation de la **joueuse/du joueur** (diplôme permanent) et formation des **essaims** (qualité du groupe), si nécessaire ; éviter deux arbres de progression redondants.

## Génétique : rôle clairement biologique

Traits potentiels : aptitude au pollen, adaptation à une famille florale (trèfle, fleurs sauvages, arbres mellifères, etc.), endurance, résistance aux parasites, perception des fleurs rares, vitesse de vol.

- Un gène apporte un avantage **dans son domaine**, pas un multiplicateur global illimité.
- Plusieurs gènes ne doivent pas rendre un joueur capable de remplacer seul une alliance.
- Aucune génétique rare obligatoirement achetée ; acquisition possible via expéditions difficiles et événements.
- Réutiliser le système de Génétique officiel ; ne pas inventer des effets si le contrat de progression actuel ne les permet pas encore.

## Recherche

Axes recommandés : Économie florale, Botanique et compatibilité, Transport et portée, Régénération, Sécurité des essaims, Logistique d'alliance.

Chaque effet a une clé stable, un plafond d'empilement et une source reconnue. Ne pas attribuer deux fois le même bonus via Académie et Recherche.

## Championnes

Rôles possibles : directrice de butinage (rendement), botaniste (compatibilité), exploratrice (découverte), protectrice (résistance), médiatrice (bonus de pacte). Une championne engagée en mission civile doit être **réservée** si les contrats d'affectation existants exigent exclusivité.

La formule de bonus doit être additive ou multiplicative **mais avec plafond configuré** ; révéler le bonus sur le rapport de mission. Débloquer l'affectation seulement après audit du Hall des championnes.

## Liaison avec d'autres bâtiments

- **Entrepôt / Réserve de miel** : stockage et alimentation, sans confusion avec les devises officielles.
- **Banque** : projet futur de financement, pas prélèvement automatique tant que le bâtiment n'a pas de contrat correspondant.
- **Transformation** : bâtiment/capacité à confirmer dans le catalogue officiel avant de promettre sa fonctionnalité.
- **Infirmerie** : soins civils possibles si autorisés, avec pools séparés.
- **Caserne / Défense** : escorte et protection, pas fabrication de butineuses.
- **Palais royal** : prérequis de progression globale, jamais capacité de pollinisation magique.

## Indicateurs compréhensibles

Afficher au joueur : nombre disponible, niveau de formation, traits pertinents, spécialité, charge maximale, portée, temps estimé, contribution prédite avec explication des bonus.

### À tester

Formation démarrée deux fois, niveaux invalides, changement de spécialité en mission, relecture de diplôme, plusieurs championnes simultanées, empilement extrême de bonus, changement d'alliance, multilingue.

