# 59 — Audit préalable à la fusion P8 / main (sans régression)

Date : 21 octobre 2026
État : audit non destructif et sauvegardes de précaution effectués.
AUCUNE FUSION, AUCUN RESET, AUCUN PUSH sur main.

## Branches et base commune

- Branche officielle locale : main, racine C:\projets\beekingdomgame-master.
- Branche P1-P8 : feature/pollinisation-alpha-20261021.
- Base Git commune : afc9c31e0fb2b8e16b179257104b20afe916d577.
- HEAD officiel local au moment de l'audit : afc9c31e.
- Main a 50 fichiers suivis modifiés et 304 non suivis dans son
  espace de travail. Aucun de ces fichiers n'a été écrasé.
- Le lot P1-P7 publié apporte 124 chemins par rapport à main
  au moment du premier contrôle. Le test P8, ajouté ensuite,
  est une extension documentaire et QA en branche séparée.

## Cinq collisions de fichiers suivis

Ces chemins sont présents à la fois dans le diff de la branche de pollinisation
et dans les changements NON commitées du main officiel :

1. Assets/BeeKingdom/Playground/MobileAccountSessionRuntimeBootstrap.cs
2. Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs
3. Server/src/BeeKingdom.HiveOperations/HiveOperationModels.cs
4. Server/src/BeeKingdom.HiveOperations/HiveStateMigrator.cs
5. Server/src/BeeKingdom.Server/Program.cs

Il s'agit notamment des points d'intégration de session, de rendu WorldMap,
de sauvegarde de ruche et d'API SQL. Une copie directe, checkout forcé,
reset, merge automatique ou cherry-pick non examiné risquerait de perdre
des corrections de Unity, de la carte ou des autres bâtiments.

## Conflits de documents actuellement non suivis

Parmi les 304 fichiers non suivis de main, 53 chemins sont aussi déjà suivis
et versionnés dans la branche de pollinisation. Ils résident dans
Docs/AI/Missions/Pollinisation.

Comparaison SHA256 des deux emplacements :
- 50 fichiers du cahier sont identiques octet par octet.
- 3 fichiers divergent :
  Docs/AI/Missions/Pollinisation/README.md
  Docs/AI/Missions/Pollinisation/17-DECISIONS-OUVERTES.md
  Docs/AI/Missions/Pollinisation/18-TRACABILITE-IDEES.md

Ces trois fichiers ont reçu les mises à jour d'implantation P1-P8 dans la
branche; leurs originaux doivent être réconciliés, pas remplacés aveuglément.

## Sauvegarde non destructive de précaution

Huit fichiers actuels de main (les cinq fichiers source et les trois documents
divergents) ont été copiés, sans modification de main, sous :

C:\BeeKingdom\DEV\BK-Studio\qa\pollinisation-p8-20261021\official-main-overlap-backup

Le manifeste manifest-sha256.csv y consigne leurs empreintes SHA256.
Il ne s'agit pas d'une sauvegarde de toute la branche principale ou du
serveur SQL, mais d'un point de sécurité ciblé pour les collisions identifiées.

## Ordre d'intégration recommandé

1. Stabiliser et commiter séparément les modifications officielles de main
   de façon cohérente par fonction, avec les régressions Unity traitées.
2. Créer un worktree / branche d'intégration à partir du futur HEAD de main
   propre. Ne jamais merger à même les fichiers non commitées.
3. Réconcilier explicitement les cinq fichiers source, en privilégiant
   les correctifs actuellement valides et en adaptant P1-P8 sans les écraser.
4. Inclure les 50 documents byte-identiques et fusionner le contenu des
   trois documents divergents (aucune suppression de décisions du créateur).
5. Compiler toute l'API avec les tests SQL, puis compiler toute la carte
   Unity avec les tests Editor, PlayMode et l'infirmerie/Alliance/Recherche.
6. Effectuer un smoke FR/EN visuel en environnement isolé et des tests
   multi-joueurs avec flags OFF, puis only staging selon sauvegarde/rollback.
7. Prévoir un plan de désactivation, de retour arrière et de règlement
   des missions en vol avant ouverture Alpha.

## Restrictions

Aucune mise en production, aucune migration SRVESDT, aucun réglage serveur
modifié par cette opération. Le QA P8 et ses résultats demeurent dans le
worktree isolé et la branche feature. L'audit ne constitue pas une approbation
de fusion ou du design économique.
