# BeeKingdom P19 — lecture seule de la production (SRVESDT)

Ce vérificateur complète le SQL `../pollination-production-preflight/P19-DatabaseReadOnly.sql`. Il ne modifie aucun drapeau, état de ruche, schéma SQL, service Windows ni processus IIS.

## Exécution

Sur SRVESDT, dans une session PowerShell autorisée :

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\BeeKingdom\staging\pollination-production-readiness\Verify-P19ReadOnly.ps1" -OutputPath "C:\BeeKingdom\staging\pollination-production-readiness\P19-preflight-result.json"
```

La commande accepte `-SqlInstance` et `-Database` pour désigner explicitement une cible. Elle utilise **seulement l'identité Windows courante** avec `sqlcmd -E` ; ne pas inventer, extraire ou emprunter de mots de passe pour contourner un accès refusé. Le rapport JSON est volontairement écrit hors du répertoire public IIS.

## Contrôles vérifiés

- Santé de l'API et du pool IIS ; statuts des routes authentifiées et des routes actuellement fermées.
- Drapeaux `Alliance`, `FloralSites`, `CivilPopulation`, `CivilExpeditions`, `FloralAllianceProjects`, sans en modifier les valeurs.
- Concordance SHA-256 des deux DLL déployées avec le lot P18 validé et présence d'une sauvegarde **binaire** P18.
- Présence de sept tables des migrations 096–098 et dénombrement des missions ouvertes/échues, uniquement si le compte exécutant a déjà les droits SQL requis. Le script SQL complémentaire vérifie les deux index critiques.

## Résultat SRVESDT — 24 octobre 2026

- API `200`, catalogue/population sans session `401`, expéditions/projets désactivés `503` : conforme.
- DLL du lot P18 identiques aux binaires vérifiés ; sauvegarde binaire retrouvée.
- `SQL_READ_NOT_AUTHORIZED_FOR_CURRENT_IDENTITY` : le compte de l'agent distant n'a pas la permission de lire la base BeeKingdom. **Ne pas escalader ses privilèges automatiquement**.
- `SQL_PRODUCTION_BACKUP_RESTORE_NOT_VERIFIED` : une sauvegarde binaire n'est pas une restauration de base SQL validée.
- `rolloutAllowed=false` : le vérificateur ne constitue jamais, à lui seul, une autorisation d'activer les mutations. Une restauration SQL, une validation du worker et un parcours à deux comptes réels restent nécessaires.

## Sécurité

L'outil ne contient aucune clé ni chaîne de connexion et n'exécute que des requêtes `SELECT` sur les métadonnées et les comptes globaux. Il refuse de placer le rapport dans le répertoire exposé par IIS. Le résultat SQL bloqué n'est ni une erreur à masquer ni la preuve que les tables sont absentes.

Les expéditions et projets floraux demeurent fermés tant que les préconditions de `Docs/QA/Pollination-Readiness-20261024.md` ne sont pas satisfaites.
