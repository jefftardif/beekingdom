# BeeKingdom P19 — prévalidation SQL de production (lecture seule)

Cette étape ne lance ni expédition ni projet, ne modifie aucun état de ruche et ne change pas de drapeau. Elle vérifie la structure SQL prévue par les migrations 096, 097 et 098 et renvoie seulement les compteurs globaux de missions échues et de contributions enregistrées.

## Exécution autorisée

Un administrateur SQL disposant de **droits SELECT/VISIBILITÉ nécessaires sur la base BeeKingdom** exécute `P19-DatabaseReadOnly.sql` avec la connexion habituelle de son outil d’administration. La requête bloque si la base courante n'est pas `BeeKingdom`. Aucun compte, mot de passe, jeton ni rôle SQL ne doit être créé ou emprunté pour contourner les autorisations existantes.

L’agent Valoraxo Remote fonctionne actuellement sous `NT AUTHORITY\SYSTEM` sur SRVESDT ; son accès intégré à `BeeKingdom` a échoué. Le rapport d’exécution SQL production reste donc **non vérifié**. Ne pas interpréter les réussites de la base QA locale comme la preuve de la production.

## Critères avant activation

- Les sept tables et les deux index critiques doivent tous indiquer `PASS`.
- Le nombre de missions échues doit être vérifié; toute mission échue non réglée exige diagnostic avant nouvelles missions.
- Vérifier séparément le fournisseur SQL réellement utilisé par l'API via le diagnostic **autorisé** `/ops/readiness` ; le fichier JSON versionné seul ne permet pas de conclure.
- Confirmer une sauvegarde **SQL récupérable**, le fonctionnement du worker de règlement avec les drapeaux fermés/ou en mode drainage, et le rollback de P18.
- Confirmer le parcours de deux comptes réels et l’absence de doubles crédits.

**État au 24 octobre 2026 :** P18 actif en lecture seule. `CivilExpeditions.Enabled=false` et `FloralAllianceProjects.Enabled=false` demeurent inchangés. Aucune bascule n'est autorisée sur la base de cette seule requête.
