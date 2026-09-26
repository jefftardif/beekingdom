# M094 — Récupération Unity Package Manager / IPC

## Cause
L'environnement du lanceur omettait `ProgramData` et `ALLUSERSPROFILE`. UPM 9.26.1 plantait dans `getLocalConfigFolder` (`ERR_INVALID_ARG_TYPE`), puis Unity 6000.5.3f1 expirait après 30 s sur l'IPC ; restaurer seulement `ProgramData` rétablissait l'IPC mais la résolution échouait encore, restaurer les deux la rétablit (HTTP interne 200).

## Fichiers changés et correction
`tools/Run-UnityEditMode.ps1` complète uniquement ces variables absentes avec le dossier Windows `CommonApplicationData`, lance un filtre EditMode obligatoire, conserve logs/code de sortie/XML et vérifie un résultat positif non vide. Les variables du processus appelant sont restaurées ensuite ; aucune modification système persistante, gameplay, manifeste de paquets ou World Map. Le présent rapport est le seul autre fichier ajouté.

## Vérification
Commande : `& ./tools/Run-UnityEditMode.ps1 -TestFilter WaterHydrationSystemTests`. Compilation jeu/éditeur sans erreur C#, Unity **code 0**, **5/5 réussis**, aucun échec ni test ignoré ; syntaxe PowerShell et `git diff --check` valides.

Preuves locales : `Logs/M094/` (échecs et sondes comparatives), puis `Logs/EditMode-c1069339efcb47ffb2e63f5977a2b6b8/` (`editor.log`, `upm.log`, `exit-code.txt`, `results.xml`, exécution du 26 septembre 2026 à 21:45 UTC).

## Environnement et points non résolus
Le worktree contenait aussi des DLL sous forme de pointeurs LFS : 42 DLL hydratées pour la vérification depuis la copie locale, chacune vérifiée contre le SHA-256 du pointeur, puis représentation LFS initiale rétablie. Les médias LFS non hydratés produisent encore des erreurs d'import hors périmètre ; ils n'ont pas empêché les cinq tests. Avant une nouvelle compilation, hydrater les binaires LFS. Les changements automatiques d'import (`.meta` et réglage URP) ont été annulés ; l'éditeur interactif de `C:\projets` reste intact.

## Acceptation
- [x] Cause environnementale reproduite et isolée, correction limitée au lancement.
- [x] Compilation et résultat XML EditMode obtenus sur le worktree M094.
- [x] Aucun changement gameplay, World Map ou dépendances versionnées.
- [x] Lanceur réutilisable et rapport livrés ; aucun push ni merge main.

Référence : [configuration UPM officielle](https://docs.unity.com/en-us/engine/6000.3/manual/packages-list/upm-config/files). Le diagnostic des deux variables repose sur les essais locaux ci-dessus.
