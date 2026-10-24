# 60 — P9 : intégrité des assets Unity, métadonnées et contrôle préalable

Date : 22 octobre 2026 — machine PCJEFF. **Audit et récupération locaux uniquement.**
Aucun déploiement SRVESDT, aucun changement de main, aucune fusion de branches et
aucune suppression volontaire d'assets.

## Problème confirmé

Avant correction, le worktree P8 (feature/pollinisation-alpha-20261021, HEAD
f9c315b4) affichait 5 524 fichiers .meta suivis par Git mais manquants sur disque.
Le worktree P9 (feature/pollinisation-p9-20261022) présentait exactement ces
5 524 suppressions et cinq modifications de fichiers suivis non liées.

Un contrôle indépendant a prouvé que :
- 5 512 fichiers binaires sources correspondants étaient présents dans
  C:\projets\beekingdomgame-master mais absents des worktrees P8 et P9.
- Le volume de ces sources était de 1 337 593 555 octets, soit environ 1,246 Gio.
- Répartition : 5 458 PNG, 36 GIF, 14 BYTES, 3 TGA et 1 JPG.
- Les règles .gitignore, dont la ligne 150 (*.png) et la ligne 156 (*.gif),
  expliquent pourquoi le contenu binaire ne se propage pas automatiquement dans
  un worktree Git, alors même que le fichier .meta correspondant est suivi.
- Les douze autres sources manquaient également du dossier main officiel :
  11 PremiumBeeIcons et la bannière alliance_future_hall.png.
- Une sauvegarde Git historique 0e262b38 conservait néanmoins les douze pointeurs
  LFS, et les douze objets binaires réels étaient disponibles dans le cache
  .git/lfs/objects de PCJEFF. La comparaison avec les .meta de HEAD a confirmé
  **12 SHA256 de fichiers exacts et 12 GUID Unity identiques**.

**Attention :** la relation causale "Unity a supprimé les .meta orphelins" est
une explication technique plausible et cohérente avec le dépôt, pas une preuve
par journal d'un processus Unity particulier.

## Intervention strictement non destructive

1. Vérification du HEAD et de l'absence de session Unity éditant les worktrees
   P8 et P9. Les éditeurs ouverts sur main et P9 visual QA ont été laissés intacts.
2. Copies des 5 512 sources ignorées depuis main vers P8 puis P9, uniquement
   lorsque la destination n'existait pas. Contrôle SHA256 source/destination.
3. Restauration des .meta **uniquement** pour les assets dont la source est
   effectivement présente, depuis leur contenu Git HEAD original, sans créer
   de nouveaux GUID.
4. Récupération des 12 images restantes depuis les objets Git LFS locaux
   documentés par le commit 0e262b38, avec contrôle SHA256 et taille.
   Les douze .meta de HEAD ont alors été rétablis à l'identique.
5. Préservation des cinq fichiers modifiés en P9 (fenêtre premium, cartes, tests
   et paramètres Unity) : SHA256 inchangés pour les cinq avant/après récupération.

## Résultats réels après correction

Le nouveau validateur en lecture seule :
Docs/AI/Missions/Pollinisation/Tooling/verify_unity_asset_pairs.py

Commande, depuis le dépôt :
python -B Docs/AI/Missions/Pollinisation/Tooling/verify_unity_asset_pairs.py --worktree C:\BeeKingdom\DEV\BK-Studio\staging\pollinisation-alpha-20261021

Pour P9, remplacer la valeur --worktree par :
C:\BeeKingdom\DEV\BK-Studio\staging\pollinisation-p9-20261022

Résultat P8 ET P9 après restauration :
- **7 345 couples asset / .meta suivis présents sur 7 345**.
- Aucun .meta suivi manquant; aucun asset associé à un .meta suivi manquant.
- Aucune suppression suivie résiduelle dans les deux worktrees.
- P8 : uniquement le validateur et la documentation ajoutés restent non suivis
  avant le commit de ce rapport.
- P9 : les cinq modifications originales restent présentes et inchangées.

Le script ne fait jamais de copie, de restauration, de reset ni de modification.
Il termine avec code 0 si tous les couples existent, code 2 si un problème
subsiste. Un rapport JSON complet peut être généré via --json-report.

Les deux scripts temporaires de récupération et leurs manifestes/journaux
cryptographiques sont archivés localement, hors Git, sous :
C:\BeeKingdom\DEV\BK-Studio\qa\pollinisation-p9-20261022

Les sources d'images ignorées par Git ne sont PAS ajoutées en masse à GitHub.
Leur traçabilité et leur distribution licite via Git LFS ou paquet d'assets
seront à organiser avant de considérer un clone Git vierge comme complet.

## Unity : état des tests au 22 octobre

Deux tentatives distinctes de Unity 6000.5.3f1 en batchmode sur le worktree P8
ont échoué **avant le lancement des tests**, car le serveur local Unity Package
Manager n'a pas ouvert son flux IPC après 30 secondes.
Journal : pollinisation-meta-restored-editor.log et
pollinisation-meta-restored-editor-retry.log dans le dossier QA précité.

Ce n'est pas un résultat de test de gameplay. Ne pas compter ces tentatives
comme tests réussis ni comme échecs de Pollination. Les preuves préexistantes
P8 (10/10 Unity tests réussis) restent documentées dans 58-RECETTE-PLAYMODE-P8-20261021.md,
mais elles n'ont pas été reproduites au cours de cette reprise.

Le serveur SQL, la production SRVESDT et les scènes officielles main n'ont pas
été touchés. Les flags de pollinisation restent désactivés en production.

## Suite recommandée

1. Exécuter systématiquement le contrôle asset/.meta avant Unity sur
   chaque nouveau worktree, sans restaurer aveuglément des .meta orphelins.
2. Résoudre l'indisponibilité IPC Unity Package Manager sans désactiver
   Windows Defender, sans fermer une session Unity active de l'utilisateur
   et sans modifier main.
3. Relancer tests Editor, PlayMode, rendu visuel FR/EN et WorldMap
   sur copie QA valide, puis staging serveur à deux vrais comptes.
4. Décider d'une politique de transport des binaires d'assets ignorés
   (.gitignore vs Git LFS vs archive approuvée, droits de diffusion inclus)
   avant toute migration de projet vers une nouvelle machine.
5. Fusion P8/P9 -> main uniquement après le traitement des cinq collisions
   historiques de sources et la stabilisation du main officiel.
## Troisième environnement : P9 visual QA ouvert (lecture seule)

Un contrôle supplémentaire du worktree `pollinisation-p9-visual-qa-20261021`
encore ouvert dans Unity a trouvé 1 821 couples présents sur 7 345,
soit 5 512 assets/références récupérables depuis main et 12 images récupérables
depuis la sauvegarde LFS. Ce worktree a été **laissé totalement intact**
parce que son éditeur Unity est actif; le réparer pendant l'importation
pourrait régénérer des GUID à tort ou perturber la scène de test.

Action future : fermer normalement cet éditeur QA, sauvegarder ses
modifications, exécuter la même récupération non destructive et recontrôler
7 345 / 7 345 avant toute nouvelle capture PlayMode ou GameView.## Test automatisé du validateur

La commande suivante a réussi sur PCJEFF :
python -B Docs/AI/Missions/Pollinisation/Tooling/test_verify_unity_asset_pairs.py

Résultat : **3 tests sur 3 réussis**. Les cas couverts sont :
présence normale d'une paire, métadonnée orpheline après disparition
du fichier et récupération possible depuis une référence séparée.
Les trois tests utilisent un dépôt Git temporaire isolé et ne modifient
aucun asset de BeeKingdom.
