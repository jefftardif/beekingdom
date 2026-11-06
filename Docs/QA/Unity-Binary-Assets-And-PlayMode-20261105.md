# BeeKingdom — ressources Unity et tests PlayMode (5 novembre 2026)

## Résultat

Branche cliente : `integration/bk-hud-chat-20261105`, worktree de QA isolé sur PCJEFF.

Le dépôt Git suit les métadonnées Unity mais exclut volontairement les images PNG, JPG, GIF et assimilées (règles `.gitignore`). Un clone ne contient donc pas à lui seul les illustrations de la World Map ou des fenêtres premium. Ne pas affirmer qu'un clone Git est prêt pour les builds sans avoir restauré ces ressources autorisées.

## Hydratation de QA, **locale seulement**

Copie des ressources binaires PCJEFF vers le worktree de test — aucun changement sur la source :
- 5 008 tuiles et images Wave6 ;
- 671 autres ressources Unity ;
- 3 métadonnées complémentaires ;
- 3 DLL NuGet non suivies, uniquement pour la QA ;
- 21 captures d'écran supplémentaires afin de rétablir la paire avec leurs métadonnées déjà suivies par Git.

La copie de 5 685 fichiers principaux représente environ 1 367 Mio et a été contrôlée par SHA-256. Les 21 captures supplémentaires n'ont pas été ajoutées au manifeste de production.

**Contrôles de cohérence :** 7 629 fichiers `.meta` suivis, aucun asset associé manquant dans la copie QA, aucun GUID dupliqué. Les 170 `.meta` comparés dont l'empreinte variait avaient les mêmes GUID et le même contenu après normalisation des espaces et fins de ligne; aucun paramètre d'importation différent n'a été détecté.

**Contrôle fort :** 5 685/5 685 empreintes SHA-256 vérifiées, 0 fichier manquant, 0 discordance. Le manifeste `Unity-Asset-Checksums-20261105.json` référence les fichiers et leurs empreintes, **sans contenir les binaires**.

## Vérificateur Git

```powershell
python tools/qa/verify_unity_asset_contract.py --root . --strict --manifest Docs/QA/Unity-Asset-Checksums-20261105.json
```

Le script vérifie les paires asset/métadonnée, l'unicité des GUID Unity, l'existence des ressources hors images de démonstration et, si le manifeste est fourni, leur SHA-256.

Un test de contrôle indépendant a vérifié quatre cas : asset absent, asset rétabli, GUID dupliqué et GUID corrigé. Tous les contrôles ont donné le résultat attendu. Sur une copie Git **non hydratée**, ce garde doit échouer : c'est intentionnel, pas une permission pour masquer les objets manquants.

Le manifeste est une preuve d'intégrité et **pas une stratégie de distribution**. La décision de publier les binaires via Git LFS ou un dépôt d'assets contrôlé reste à prendre; aucune importation massive dans GitHub n'a eu lieu.

## Tests Unity

Après hydratation, deux tests `PollinationP8PlayModeTests` ont été exécutés dans Unity 6000.5.3f1 avec les DTO de test en mémoire, sans accès au serveur de production :
- Fenêtre de pollinisation premium, entrée capturée, défilement conservé, boutons de paiement/création absents en aperçu, français/anglais.
- Scène World Map chargée avec fonctionnalité pollinisation désactivée et aucune fenêtre parasite ouverte.

Premier passage : 1 réussi, 1 échoué sur un ancien libellé « APERÇU EN LECTURE SEULE ». Le test a été actualisé pour contrôler les messages désormais affichés (« FUTURES BUTINEUSES » / « FUTURE FORAGERS », « BIENTÔT DISPONIBLE »), sans supprimer les assertions de blocage des dépenses et créations.

**Deuxième passage : 2/2 réussis, 0 échec.** Les 92 tests EditMode déjà validés (HUD, Alliance, infirmerie, bâtiments et carte) restent au dossier.

Les captures automatiques du PlayMode batch n'ont pas été générées (`P8_*_SCREENSHOT_EXISTS=False`). Les tests sont une preuve structurelle et fonctionnelle, **pas une validation visuelle finale**.

Journaux et XML conservés hors Git dans `C:\BeeKingdom\staging`. Aucun APK Android ni vérification tactile réelle.

## Limites et prochaines validations

- Les binaires restaurés en QA ne sont toujours pas distribués par GitHub : ils doivent être récupérés d'une source interne autorisée avant toute compilation indépendante.
- Fonts/assets modifiés, DLL tierces et ProjectSettings locaux non intégrés restent à examiner pour assurer une fidélité visuelle complète.
- Vérifier en GameView les panneaux Plus/Chat, les différents zooms, le terrain et les placements réels, puis sur Android.
- Le serveur SRVESDT, ses drapeaux, SQL et son API n'ont pas été modifiés pendant ce lot.
- Ne pas fusionner dans `main` sans ces contrôles; les changements non commités du projet principal restent protégés.

## Vérification supplémentaire du menu Plus

Test WorldMapMorePlayModeSmokeTests dans la scène officielle World Map,
sans connexion aux comptes des joueurs :

- Ouverture de More, identité du panneau et présence du renderer premium.
- Fermeture logique immédiate puis attente du fondu de 0,12 s.
- Réouverture du Sac, puis fermeture sans perdre l'état du menu.
- Filtrage limité aux annulations de transport MCP lors de la sortie du PlayMode;
  les autres erreurs de jeu restent signalées.

**Résultat final : 1/1 PlayMode réussi, 0 échec.** Le rendu pixel à pixel du
HUD, le Chat connecté, le terrain et les contrôles tactiles Android
nécessitent toujours une vérification visuelle complète.

Rapport : C:\BeeKingdom\staging\BK-HudChat-Reconcile-20261105-plus-playmode-diagnostic2.xml.