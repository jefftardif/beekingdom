# 40 — Prompt complet pour l'agent de développement BeeKingdom

> À transmettre à l'agent codeur avec **TOUT le répertoire Pollinisation**. Ne transmettre pas uniquement cette page : elle est l'ordre de mission, les autres fichiers sont les contrats.

## Contexte

Tu travailles sur BeeKingdom, MMORTS Unity + serveur C# déjà en développement, dépôt principal :
C:/projets/beekingdomgame-master

Les scènes, textures, paysages et tuiles de la carte du monde sont **à préserver** : notre problème est le manque d'activités réelles sur la carte, pas l'esthétique du terrain.

Le créateur a approuvé l'ajout d'un **pilier de pollinisation** aussi important que la guerre et les alliances. Ce pilier est **collectif**, fondé sur des **essaims civils distincts des troupes militaires**. Les compétences/niveaux des joueurs et le niveau de leur alliance doivent améliorer réellement la capacité de polliniser. Les **sites publics restent accessibles** aux joueurs solitaires ; une alliance peut contrôler des installations et se spécialiser, tandis que les grandes régions demandent de la coopération, parfois entre alliances concurrentes. La pollinisation doit motiver diplomatie, escortes, guerre territoriale et activité permanente.

## Ta mission immédiate

**Commence par le Lot 0, puis implémente le premier incrément Alpha end-to-end le plus sûr autorisé par ton mandat.**

1. Lis README, 00-AGENT-CODEUR-LIRE-EN-PREMIER.md, 01, 03, 05, 07, 08, 13, 14, 16, 17, 18, 20, 21, 22, 23, 24, 25, 38, 39 et les documents de ton lot.
2. Vérifie le HEAD, la branche et les fichiers modifiés avant le moindre changement. Main est susceptible de contenir des travaux d'autres agents ; aucune opération destructive.
3. Examine d'abord l'existant : FlowerManager/FlowerPatch/PollinationRules/RegionalEcologyPressureFramework, WorldResourceCollectionService, WorldPresenceService, NurseryCohortService, AcademyTrainingService, HiveStateMigrator, AllianceService/AllianceResearch/Diplomacy, WorldMapMarchRegistry, système officiel de ressources et combat.
4. Produis une **matrice de réutilisation** : réutilisable tel quel / adapter / nouveau / ne pas toucher, avec raisons.
5. Résous l'architecture d'état mondial partagé et la transaction **ruche + site + projet**, avec preuve de déduplication et de reprise après crash.
6. Découpe en petits lots testables : population civile -> sites floraux -> expédition complète -> contribution collective -> progression/alliance.
7. Pour chaque lot, ajouter migration additive, API authentifiée, tests de concurrence, Unity FR/EN si visible, feature flag et note de compatibilité.
8. Ne prétends jamais qu'une action est en production si seul l'écran local existe ; séparer simulations et mutations officielles.
9. Ne crée pas de nouvel effet de Gameplay sans vérifier les ID et systèmes existants.
10. Documente toute décision non approuvée dans 17 (avec ADR) ; n'invente pas de coûts finaux, mortalité, monnaie, taxe ou attaque libre des civils.

## Conditions de réussite du premier incrément réel

- Une ruche possède une population civile persistante et séparée des soldats.
- Elle peut lancer une mission florale officielle vers un site accessible de la carte existante.
- La mission se termine sans client Unity ouvert et conserve civils/ressources.
- Le serveur fournit un bilan de récolte et un effet écologique, tous deux idempotents.
- Deux joueurs voient le même état de site, et leurs prélèvements ne dépassent jamais les réserves.
- Une mission acceptée ne peut être créée/créditée deux fois par retry ou double clic.
- Aucun bâtiment, recherche, militaire ou système d'alliance déjà existant ne régresse.
- Les interfaces nouvelles sont entièrement en français et anglais, accents intacts et sans scintillement.
- L'agent fournit preuves exactes des tests exécutés et liste des limitations.
- Toute fonctionnalité jugée encore expérimentale reste derrière un flag OFF ou QA.

## Interdictions absolues

- Aucune refonte des tuiles/biomes/paysages ni réécriture de la carte.
- Aucune conversion des troupes militaires en butineuses.
- Aucune génération de honey/pollen côté client.
- Aucune création de propriétés de niveau alliance « fictives » ; identifier vraie donnée ou construire progression serveur explicite.
- Aucun nouveau 15e bâtiment intraruche à partir d'un nom du brainstorming.
- Aucune implémentation complète du raid de l'ours sous forme de simple NPC solo.
- Aucun reset Git, changement global non audité, migration destructive ou déploiement sans mandat distinct.
- Aucune fusion de tous les changements dans un unique commit impossible à auditer.

## Contraintes techniques clés

- Unity PCJEFF : visuel/interaction client.
- SRVESDT : API officielle et persistence ; ne pas toucher à production sans politique de backup/rollback.
- GitHub : historique de code, changements ciblés.
- Les ressources officielles connues sont honey, pollen, wax.
- WorldResourceCollectionService utilise CommittedTroops : distinct du civil.
- FlowerManager existe en mémoire Unity, n'est pas source MMO officielle.
- WorldPresenceService est un échantillon ambiant, pas un registre fiable pour un groupe.
- AllianceEntity n'a pas de Level explicite dans le code consulté ; définir progression réelle.
- AllianceDiplomacy comprend déjà les types de relation ; Pacte de Floraison doit être compatible.
- WorldMapMarchRegistry est local et ne garantit pas les réservations serveur.
- Le dossier contient **des décisions approuvées** et des **recommandations à valider** ; respecter les étiquettes.

## Livrables de ta première réponse de travail

1. **Audit P0 réel** avec tableau fonctions existantes et lacunes.
2. Diagramme final des agrégats et transactions choisies.
3. Décisions de design nécessaires avant lancement, réduites au strict bloquant.
4. Liste de fichiers source et tests touchés par P1/P2/P3.
5. Première tranche de code réellement testée si ton mandat l'autorise.
6. Résultats tests avec commandes, pas un simple « tout est bon ».
7. Rapport Git et état du déploiement.
8. Mise à jour de la documentation et tickets restants.

## Méthode de travail attendue

Ne code pas toute la Bêta en un seul mouvement ; vise un premier flux E2E digne de confiance. Les idées de relais, alliance, génétique avancée, PvP, pactes, floraisons et tanière de l'ours **restent dans la conception et les interfaces prévues** pour les lots ultérieurs.

Le créateur souhaite que **pas une seule idée importante ne soit perdue** ; mets à jour 18-TRACABILITE-IDEES.md, 17-DECISIONS-OUVERTES.md et l'index à chaque évolution.

## Format du rapport final du codeur

Version/HEAD, audit observé, architecture retenue, comportement implémenté, comportements seulement spécifiés, migrations, API, tests exécutés/pas exécutés, screenshots si UI, régressions vérifiées, flags, commits/PR et déploiement (avec preuves ou NON effectué), prochaines tâches.

