# M088 — Validation voix et assets championnes

## Cause / constat
Les 136 MP3 étaient des pointeurs LFS non matérialisés dans ce worktree. Aucun manque de référence, de configuration AudioImporter ou de wiring vocal confirmé dans le code actuel ; les absences EN correspondent à M080X.

## Fichiers et intervention
Seul ce rapport est ajouté au commit. Les 136 MP3 ont été matérialisés depuis le cache LFS local, sans régénération, conversion ni changement de contenu versionné ; aucun code ni `.meta` modifié.

## Vérifications ciblées
136 tailles et SHA-256 conformes aux pointeurs LFS ; 136 AudioImporter, GUID uniques, aucun `.meta` orphelin dans ChampionVoices. Inventaire FR/EN : Striga 23/17, Zephyra 20/0, Ambra 19/14, Nectaria 22/0, Aurelia 21/0.

Lecture ciblée : résolution de langue, caches séparés FR/EN, filtre de catégorie insensible à la casse (dont `Cit`), repli `cit` dans la même langue, chemins exacts des deux FTUE FR et transmission du stepId conformes. Appels sélection, construction, patrouille, collecte, victoire, découverte et bestiaire présents ; lecture audio et atténuation musicale raccordées. Aucun hook des marches modifié.

## Compilation / limites
Import et décodage Unity 6000.5.3f1 tentés dans un projet temporaire isolé contenant les clips et leurs métadonnées : échec avant import, code 1, serveur Unity Package Manager inaccessible par IPC, y compris après relance autorisée. Aucun résultat de compilation du jeu/éditeur, de test runtime FR → EN → FR ou d'écoute certifié pour M088.

## Points non résolus
Validation Unity à reprendre après rétablissement du serveur de packages. Les cinq portraits `ChampionBees/*.png` existent dans la copie locale principale mais sont absents du worktree : exclusion `*.png` explicite dans `.gitignore` ; politique de stockage des images inchangée, rendu non certifié.

## Acceptation
- [x] Inventaire FR/EN, intégrité LFS, métadonnées et chemins vérifiés.
- [x] Hooks inspectés ; aucune réparation de code spéculative.
- [x] Aucun audio généré ; marches, Chat Royal et Alliance inchangés.
- [x] Travail limité au worktree ; aucun push ni merge main.
- [ ] Import Unity et parcours vocal runtime certifiés (blocage ci-dessus).
