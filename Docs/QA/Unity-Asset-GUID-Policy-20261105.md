# BeeKingdom — Politique Unity .meta et ressources graphiques (5 novembre 2026)

## Détection et préservation

Le .gitignore du dépôt exclut intentionnellement les binaires *.png, *.jpg et
d'autres formats d'image : commentaire « Image binaries are NOT committed
(saved internally); keep their .meta for GUID stability ».

L'audit sur le poste PCJEFF a retrouvé :
- 188 images locales ignorées (184 PNG + 4 JPG), soit 142,45 Mio.
- Les images ont l'attribut Git filter=lfs, mais restent ignorées par .gitignore.
- 214 fichiers Unity .meta correspondants : 188 images et 26 dossiers.
- 1 903 GUID de métadonnées déjà suivis dans le dépôt.
- Zéro doublon de GUID parmi les nouveaux fichiers et les GUID existants.
- Certains dossiers importés dans le worktree indépendant avaient reçu des
  GUID générés automatiquement par Unity. Les GUID du projet PCJEFF ont été
  conservés à la place. Les GUID des autres objets ne sont pas modifiés.

Les métadonnées sont ajoutées à la branche WIP pour stabiliser les références
Unity. Aucun binaire d'image n'a été forcé dans Git. Les images restent à
récupérer depuis la source interne prévue par la politique du projet.

## Preuves et limites

Inventaire complet : UnityMetaInventory-20261105.json
Inventaire des fichiers ignorés : IgnoredUnityImages-20261105.json

Le premier inventaire liste les chemins, GUID, empreintes des .meta, tailles et
SHA-256 des images locales. Un clone de GitHub doit récupérer séparément des
binaires de même SHA-256 pour correspondre au projet PCJEFF.

ATTENTION : des .meta seuls ne rendent pas une image disponible à l'exécution.
Ne pas fusionner le checkpoint client avant d'avoir restauré les binaires de
référence, validé les assets dans Unity et réussi les tests de scènes/menus.

## Décisions de sécurité

- Aucun git add -f global des images, aucune modification de .gitignore ou
  de .gitattributes et aucune consommation supplémentaire de quota LFS.
- Pas de changement du projet Unity actif de PCJEFF ni de SRVESDT.
- Aucune mise en production ou fusion dans main.
