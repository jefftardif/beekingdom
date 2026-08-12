# Bee Kingdom - Instructions permanentes Codex

> **Retire depuis le 2026-07-24** : Codex n'est plus utilise sur ce projet.
> Claude Code a repris seul la suite du developpement. Voir `CLAUDE.md` et
> `Docs/Claude/Claude_Continuation.md` pour les instructions et la memoire de
> travail actives. Ce fichier reste en place comme reference historique.

## Role et langue

- Travailler en francais avec l'utilisateur.
- L'agent principal de la VM s'appelle `Architecte` et agit comme architecte,
  coordinateur et ingenieur senior.
- Lire le code et les rapports locaux avant de poser une question.
- Ne jamais demander a l'utilisateur de repeter l'historique du projet ou les
  observations deja faites dans Ant Legion.

## Lecture obligatoire au debut d'une tache

1. `Docs/VM/Codex_VM_Continuation.md`
2. `Docs/Product/BeeKingdom_LivingHive_ExecutionPlan.md`
3. `Docs/Benchmarks/AntLegion/AntLegion_BeeKingdom_FunctionalReference.md`
4. `Docs/Demos/LivingHive.md`

Ces documents sont la memoire de travail officielle. La reference Ant Legion
provient d'une longue session d'observation et de jeu. Ne pas recommencer cette
analyse sans une demande explicite; poursuivre son adaptation a Bee Kingdom.

## Fondations intouchables

- Ne jamais modifier la carte terrain 50x50 ni ses images.
- Scene canonique:
  `Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity`
- Ne jamais regenerer, recadrer ou remplacer le terrain sans demande explicite.
- Conserver l'image de base actuelle de la ruche `LivingHive`.
- Ameliorer la ruche par le runtime, l'interface, l'animation, le tutoriel, le son,
  la progression et les services.
- Le chantier chat et messagerie est repris sous la responsabilite exclusive de
  l'agent `Communication`. Les autres agents ne modifient pas les modules, tests
  ou documents chat sans coordination explicite.

## Cap produit

- Ant Legion est une reference fonctionnelle, pas un produit a copier.
- Transposer les boucles au monde des abeilles et depasser la reference en clarte,
  qualite visuelle, ergonomie et profondeur.
- Conserver la collecte manuelle des ressources. Une future automatisation peut
  etre vendue comme confort, jamais comme puissance militaire irremplacable.
- Les achats doivent etre attirants sans rendre Bee Kingdom pay-to-win.
- Les textes doivent passer par les ressources de localisation.
- Les chapitres du tutoriel doivent raconter, expliquer les ressources et proposer
  des objectifs actifs avec des consequences. Ne pas gonfler la duree par de
  simples attentes.

## Environnement VM

- Toujours travailler sur `C:/projets/beekingdomgame-master` dans la VM.
- `Z:` pointe vers la copie de l'ordinateur principal et sert uniquement a la
  synchronisation; ne jamais y ouvrir Unity.
- Unity requis: `6000.5.3f1` avec Android Build Support, SDK/NDK et OpenJDK.
- Ne pas utiliser Git dans la VM.
- Synchroniser avant et apres une serie de modifications avec
  `tools/vm-sync/Synchroniser-BeeKingdom.cmd`.
- Ne jamais ecraser un conflit de synchronisation. Lire
  `.codex/vm-sync-last-report.txt`.
- Le bac a sable Codex peut ne pas voir le lecteur reseau `Z:` ou le partage UNC,
  meme lorsque l'Explorateur Windows y accede. Cette indisponibilite ne doit pas
  bloquer une tranche deja autorisee sur la copie locale.
- Si le partage est inaccessible, travailler uniquement dans la copie locale `C:`,
  rester dans les fichiers attribues a l'agent, produire la liste exacte des
  modifications et laisser la synchronisation finale a l'utilisateur.
- Ne jamais relacher le bac a sable, remapper le partage ou ecrire directement
  dans `Z:` pour contourner cette limite.

## Methode

- Respecter les patterns existants et les changements de l'utilisateur.
- Utiliser `rg` pour rechercher et `apply_patch` pour les modifications manuelles.
- Verifier les assemblages jeu et editeur, puis les parcours Unity pertinents.
- Documenter toute decision durable dans les documents produit correspondants.
