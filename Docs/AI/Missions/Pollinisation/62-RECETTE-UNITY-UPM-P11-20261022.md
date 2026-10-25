# P11 — Recette Unity et correction de l'environnement UPM (22 octobre 2026)

Branche : feature/pollinisation-alpha-20261021. Machine : PCJEFF. Unity 6000.5.3f1.
Mode : QA local uniquement, aucune modification de main ni de SRVESDT.

## Cause du blocage Unity Package Manager

En mode batch depuis Valoraxo Remote, Unity échouait avant tout test :
Could not connect to IPC stream Upm-* after 30 seconds.

Le binaire Package Manager a été vérifié (signature valide, v9.26.1).
Son lancement direct depuis la même session reproduisait :
TypeError ERR_INVALID_ARG_TYPE, path argument undefined, getLocalConfigFolder.

L'environnement de cette session distante ne contenait pas PROGRAMDATA,
ALLUSERSPROFILE, TMP et HOME. En donnant des valeurs Windows valides
uniquement au processus enfant, le serveur UPM démarre et ouvre son IPC.
Le nouveau lancement Unity a confirmé project:list-packages => 200.

Aucune variable globale, règle Defender, clé de registre ni service
n'a été modifié. L'option -noUpm ne convient pas : sans les packages
Unity, le code du jeu perd notamment TMPro et UnityEngine.UI.

Documentation de référence :
https://docs.unity.com/en-us/engine/6000.5/manual/unity-editor/command-line-arguments/editor

## Tests réellement exécutés

Avant tests, vérification : 7 345 couples assets/.meta présents sur 7 345.

- Pollination EditMode, première campagne : 10/10 réussis, 0 échec, 0 ignoré.
- WorldMap EditMode, via le nouveau lanceur : 8/8 réussis, 0 échec.
- Pollination EditMode, réexécutée via le nouveau lanceur : 10/10 réussis.

Les campagnes Pollination couvrent les libellés français/anglais, la
fenêtre premium, le blocage des clics à travers, la stabilité du défilement,
la séparation des données officielles et des modèles Unity locaux.
Les tests WorldMap couvrent la scène, la navigation, les interactions,
les tuiles et le flag pollinisation OFF.

Les deux premiers filtres ont un test WorldMap commun. Ils ne
représentent pas 18 cas distincts.

Rapports XML :
- p11-envfixed-pollination-editor.xml
- p11-qa-EditMode-WorldMap-20261022-115918.xml
- p11-qa-EditMode-Pollination-20261022-120238.xml

Ces fichiers et les logs sont archivés hors Git sur PCJEFF dans
C:\BeeKingdom\DEV\BK-Studio\qa\pollinisation-p9-20261022.

## Lanceur de régression reproductible

Outil versionné : Tooling/Run-PollinationUnityQa-P11.ps1.
Exemples depuis le répertoire du projet :

    powershell.exe -NoProfile -ExecutionPolicy Bypass -File Docs/AI/Missions/Pollinisation/Tooling/Run-PollinationUnityQa-P11.ps1 -DryRun
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File Docs/AI/Missions/Pollinisation/Tooling/Run-PollinationUnityQa-P11.ps1 -TestFilter Pollination
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File Docs/AI/Missions/Pollinisation/Tooling/Run-PollinationUnityQa-P11.ps1 -TestFilter WorldMap

Le lanceur refuse main et tout worktree non autorisé. Il valide les
7 345 couples asset/.meta et refuse une double session Unity dans P8.
Les variables nécessaires à UPM sont locales au processus et restaurées
ensuite. Le lanceur conserve les XML/journaux horodatés par filtre,
attend le résultat NUnit puis libère au besoin uniquement son propre
processus batch. Il n'utilise pas -noUpm ni -quit avec -runTests.

## Effets secondaires observés

Une première réimportation Unity a modifié Resources.meta ainsi que le
matériau de police LiberationSans SDF - Fallback.asset. Ces différences
ont été inspectées et sauvegardées dans le dossier QA, puis les versions
Git HEAD ont été rétablies uniquement dans P8, qui était propre
avant la campagne. Ce n'est pas une restauration automatique.
Aucune modification du projet officiel main.

## Limites de validation

Le résultat Unity valide les tests automatisés et la compilation.
Il ne valide pas les captures visuelles réelles FR/EN, le tactile sur
appareil, ni les opérations à deux comptes contre SRVESDT.

Le worktree P9 visual QA reste ouvert dans Unity et dispose encore
de 5 524 couples assets/.meta absents. Il n'a PAS été modifié.
Sa réparation est déjà préparée en QA, à effectuer seulement après
fermeture normale et sauvegarde de la session en cours.

Les fonctionnalités de pollinisation restent désactivées en production.
