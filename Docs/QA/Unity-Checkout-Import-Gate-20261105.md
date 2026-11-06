# BeeKingdom — Protection des clones Unity incomplets (5 novembre 2026)

## Problème reproduit pendant la consolidation Git

La politique existante de .gitignore exclut intentionnellement les images
binaires du dépôt, mais conserve leurs fichiers Unity .meta pour garder les GUID.
Un clone GitHub ne dispose donc pas automatiquement du stockage graphique local.

Sur le worktree isolé BK-HudChat-Reconcile-20261105 :
- L'import Unity 6000.5.3f1 a échoué au démarrage du Package Manager
  (Impossible de se connecter au flux IPC Upm-47772 après 30 secondes).
- L'import a néanmoins retiré localement 5 512 fichiers .meta déjà suivis,
  leurs images étant absentes du worktree.
- Toutes ces suppressions étaient NON STAGÉES et confinées au worktree QA.
  Les 5 512 .meta ont été restaurés depuis HEAD dans ce seul worktree, sans
  aucune modification du main actif de PCJEFF.
- Une sonde déterministe exécutée après restauration a recensé 5 686
  binaires d'images absents parmi les propriétaires de .meta suivis :
  5 642 PNG, 36 GIF, 5 JPG et 3 TGA.
- La vérification de compilation indépendante n'est donc PAS RÉUSSIE.

## Garde automatique

tools/qa/ValidateUnityAssetOwners.py vérifie avant toute ouverture d'un clone
si chaque image associée à un fichier .meta suivi existe réellement. Il doit
refuser le lancement si des binaires sont manquants (code de sortie 2).
Il n'efface ni ne génère aucun asset, et n'invente pas de binaire manquant.

Exécution dans la racine du worktree :
python tools/qa/ValidateUnityAssetOwners.py

Ce contrôle n'est pas une garantie de compilation ou de qualité visuelle :
il autorise seulement l'import après restauration correcte des images.

## Prochaine validation requise

1. Récupérer le jeu d'assets depuis son stockage interne officiel, sans
   modifier la politique Git ni ajouter massivement des images ignorées.
2. Vérifier les empreintes SHA-256 des images faisant l'objet de l'inventaire
   UnityMetaInventory-20261105.json.
3. Relancer le contrôle de présence avant le Package Manager Unity.
4. Résoudre indépendamment le problème d'IPC Unity, sans toucher aux
   versions de packages de PCJEFF.
5. Compiler le worktree indépendant, puis exécuter les régressions HUD,
   Chat, Alliance, infirmerie et pollinisation en PlayMode et EditMode.

Aucun de ces blocages ne doit être contourné par une fusion vers main ou un
déploiement sur SRVESDT.
