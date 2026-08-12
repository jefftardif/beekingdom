# Bee Kingdom — Instructions permanentes Claude Code

## Statut d'agence

Depuis le **2026-07-24**, Codex n'est plus utilisé sur ce projet. Claude Code
(cet agent) est seul responsable de terminer Bee Kingdom, avec accès direct à
l'éditeur Unity via les outils MCP `ai-game-developer` (connexion confirmée
fonctionnelle le 2026-07-24). `AGENTS.md` et `Docs/VM/Codex_VM_Continuation.md`
restent en place comme historique de référence, mais ne sont plus la mémoire
de travail active.

## Role et langue

- Travailler en francais avec l'utilisateur (Jeff).
- Lire le code, les scenes Unity et les rapports locaux avant de poser une
  question. Ne jamais demander a l'utilisateur de repeter l'historique du
  projet deja documente ici ou dans les references ci-dessous.
- Utiliser les outils MCP Unity (`mcp__ai-game-developer__*`) pour inspecter
  et modifier scenes, GameObjects, assets et scripts directement, plutot que
  de deviner l'etat du projet.
- REGLE ABSOLUE, EN VIGUEUR JUSQU'A NOUVEL ORDRE (rappelee avec insistance
  par Jeff le 2026-07-28 apres un premier rappel ignore) : ne JAMAIS afficher
  dans le texte de reponse le code modifie, les diffs, les noms de
  methodes/fonctions/fichiers touches, ni le detail des outils utilises.
  Seulement le resultat concret (ce qui marche/casse maintenant), les
  informations pertinentes pour la suite, et les questions a poser. Jeff paie
  en tokens chaque ligne de code affichee inutilement — la moindre recidive
  est couteuse et agacante. En cas de doute, ne rien afficher plutot que
  d'en afficher trop.

## Lecture obligatoire au debut d'une tache

1. `Docs/Claude/Claude_Continuation.md` — memoire de travail active, a jour a
   chaque session. **Toujours lire ce fichier en premier.**
2. `Docs/Product/BeeKingdom_LivingHive_ExecutionPlan.md`
3. `Docs/Benchmarks/AntLegion/AntLegion_BeeKingdom_FunctionalReference.md`
4. `Docs/Demos/LivingHive.md`
5. `Docs/Product/BeeKingdom_Localization.md`

Reference historique optionnelle (approfondir seulement si necessaire, le
document est long — 1000+ lignes) : `Docs/VM/Codex_VM_Continuation.md`. Les
jalons les plus recents (juillet 2026) sont en tete du document.

## Mise a jour obligatoire en fin de tache

A la fin de chaque session de travail significative (nouvelle fonctionnalite,
correction, jalon livre), ajouter une nouvelle entree en tete de
`Docs/Claude/Claude_Continuation.md` (voir le format dans ce fichier) avant de
rendre la main. Ne jamais laisser ce document devenir obsolete : c'est la
seule facon pour une future session Claude de reprendre sans repetition.

## Fondations intouchables

- Ne jamais modifier la carte terrain 50x50 ni ses images.
- Scene canonique: `Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity`
- Package terrain verrouille:
  `Assets/BeeKingdom/Playground/Resources/WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_wave5method_12288_preview`
- Ne jamais regenerer, recadrer ou remplacer le terrain sans demande explicite.
- Conserver l'image de base actuelle de la ruche `LivingHive`.
  Scene de travail principale: `Assets/Scenes/LivingHive.unity`.
- Ameliorer la ruche par le runtime, l'interface, l'animation, le tutoriel, le
  son, la progression et les services.
- Le chantier chat et messagerie (module Communication) a ete gele par
  Codex. Verifier son etat reel avant d'y toucher; ne rien y modifier sans
  besoin explicite de l'utilisateur.

## Cap produit

- Ant Legion est une reference fonctionnelle, pas un produit a copier.
  Transposer ses boucles au monde des abeilles et depasser la reference en
  clarte, qualite visuelle, ergonomie et profondeur.
- Conserver la collecte manuelle des ressources. Une future automatisation
  peut etre vendue comme confort, jamais comme puissance militaire
  irremplacable.
- Les achats doivent etre attirants sans rendre Bee Kingdom pay-to-win.
- Les textes doivent passer par les ressources de localisation.
- Les chapitres du tutoriel doivent raconter, expliquer les ressources et
  proposer des objectifs actifs avec des consequences, pas de simples
  attentes.

## Environnement (2026-07-24)

- Machine: `DESKTOP-D3D29K7`. Confirme par l'utilisateur le 2026-07-24 :
  Claude Code travaille directement sur la machine hote, ce qui remplace
  entierement le flux VM + synchronisation reseau decrit dans `Docs/VM/`
  (plus besoin de `Synchroniser-BeeKingdom.cmd`, de partage `Z:` ni de
  copie vers `\\DESKTOP-D3D29K7\BeeKingdomHost`).
- Unity: connexion MCP confirmee via `ai-game-developer`
  (`http://localhost:23770/...`, voir `.mcp.json`). Scene ouverte au moment
  de cette verification: `SandboxPlayground` (pas la scene canonique
  `LivingHive` — verifier la scene active avant de supposer un contexte).
- Git: le dossier `.git` existe mais est **vide** (aucune ref, aucun objet) —
  les commandes git echouent avec "not a git repository". Confirme avec
  l'utilisateur le 2026-07-24 : ne rien initialiser pour l'instant, continuer
  sans git. Ne pas lancer `git init` ni aucune operation git sans demande
  explicite future.
- Ne pas utiliser `--no-verify` ni contourner des verifications sans demande
  explicite.

## Methode

- Respecter les patterns existants et les choix deja faits par l'utilisateur.
- Verifier les assemblages jeu et editeur, puis les parcours Unity pertinents
  avant d'annoncer une tache terminee.
- Documenter toute decision durable dans les documents produit correspondants
  et dans `Docs/Claude/Claude_Continuation.md`.
- REGLE PERMANENTE (ajoutee le 2026-08-04) : chaque sprint doit integrer, en
  plus de son objectif principal, une petite amelioration Quality of Life -
  sans jamais devier le sprint de cet objectif principal. A documenter dans
  le meme jalon que le reste du sprint.
