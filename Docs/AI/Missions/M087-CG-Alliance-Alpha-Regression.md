# M087-CG — Alliance alpha regression

## Cause racine
`GetMyAllianceAsync` renvoyait sans validation les réponses d'appartenance. Une réponse `HasAlliance=true` privée de son alliance ou de son membership devenait `NoAlliance` dans le contrôleur, proposant à tort le parcours de création ; des identités/rôles contradictoires étaient également acceptés.

## Fichiers modifiés
- `Assets/BeeKingdom/Networking/AllianceClient.cs`
- `Assets/BeeKingdom/Tests/Editor/AllianceClientTests.cs`
- Ce rapport.

## Implémentation
Validation ciblée de l'appartenance : présence des objets, identifiants alliance/joueur connecté, statut actif et cohérence du rôle avec le chef. Les contradictions produisent `alliance.invalid_response` via l'état d'erreur existant ; l'absence valide et les trois rôles restent acceptés.

## Vérifications
Client : 12 régressions reproduites avant correction, puis **32/32 tests réussis**, sources réelles compilées dans un harness temporaire NUnit/net10 avec référence Unity 6000.5.3f1. Serveur : **56/56** (`AllianceServiceTests`, `MyAllianceOverviewWireContractTests`), compilation net8 réussie avec roll-forward vers le runtime installé ; avertissements de nullabilité préexistants, aucune erreur ; `git diff --check` propre.

## Limites non résolues
Validation visuelle/Play Mode et compilation intégrale des assemblages Unity non effectuées. La navigation a été vérifiée par lecture ciblée, sans exécution des tests d'overlay Unity ; aucune anomalie supplémentaire confirmée dans les chemins examinés.

## Acceptation
- [x] Création : payload, wrapper, idempotence serveur et rechargement de l'appartenance vérifiés.
- [x] Membre/chef : rôles testés, affichage des noms et résolution du chef examinés.
- [x] Navigation : ouverture, fermeture et libération des overlays examinées.
- [x] Erreurs serveur/client : contrats existants contrôlés, réponse incohérente rejetée et tests ciblés verts.
- [x] Un seul risque corrigé ; Chat Royal, FTUE et World Map Marches inchangés.
- [x] Travail dans le worktree, aucun push ni merge main.
- [ ] Certification visuelle Unity.
