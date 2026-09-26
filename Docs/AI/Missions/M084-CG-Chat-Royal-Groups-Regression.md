# M084 — Chat Royal : régression des groupes

## Causes
Le client ignorait `Left=false`, conservait des droits/détails après exclusion et gardait une sélection devenue inaccessible lors du polling. Le départ du dernier chef laissait des invitations acceptables vers un groupe sans chef ; les correctifs M083 présents dans la base fournie sont conservés.

## Fichiers changés
- `Assets/BeeKingdom/Gameplay/Communication/LivingHiveChatController{,.Groups}.cs` et `Assets/BeeKingdom/Tests/Editor/LivingHiveChatGroupControllerTests.cs`.
- `Server/src/BeeKingdom.Chat/ChatGroupOperations.cs`, `Server/tests/BeeKingdom.Tests/{ChatGroupServiceTests,SqlServerOptInIntegrationTests}.cs`.
- `tools/tests/chat-groups/` : harnais ciblé reproductible ; présent rapport.

## Implémentation
Départ local uniquement après confirmation serveur ; purge des détails, sélection et cache récent après révocation ou liste serveur actualisée, sans éviction sur simple panne réseau. Annulation des invitations restantes au départ du dernier chef ; aucun redesign, changement World Map, push ou merge main.

## Vérification
**49/49 tests ciblés réussis, aucun ignoré** : 21 service serveur, 27 client/transport, 1 SQL LocalDB jetable avec nouveaux conteneurs de services ; compilation .NET réussie et revue ciblée sans anomalie. Quatre régressions reproduites avant correction (1 serveur, 3 client) ; `git diff --check` propre.

Preuves locales : `Logs/M084-Server/M084-ChatGroupServiceTests-Final.trx`, `Logs/M084-Server/M084-SqlGroups.trx`, `Logs/M084-ClientResults/M084-Client-FinalLocation.trx`. Le harnais compile les sources client réelles avec substituts minimaux Unity/JSON ; il ne valide ni le moteur ni `UnityEngine.JsonUtility` natif.

## Non résolu
Compilation Unity jeu/éditeur et tests EditMode non certifiés : Package Manager inaccessible, puis dépendances TMPro/UI/SignalR absentes lors de l'essai sans UPM (`Logs/M084-Groups-EditMode.log`, `Logs/M084-Groups-NoUpm.log`). Les suppressions automatiques de `.meta` produites par cet essai ont été intégralement restaurées ; aucun asset n'est modifié. Validation visuelle multijoueur en Play Mode non réalisée.

## Checklist d'acceptation
- [x] Création, idempotence, sélection du nouveau groupe et invitations acceptées/refusées.
- [x] Ajout, annulation d'invitation, exclusion, réinvitation et refus des accès retirés.
- [x] Transfert du leadership, nouveaux droits et refus des opérations non autorisées.
- [x] Quitter : chef devant transférer, départ confirmé/refusé et dernier chef sans invitations orphelines.
- [x] Persistance SQL après reconstruction des services ; cache client purgé après exclusion/polling/réouverture.
- [ ] Compilation Unity jeu/éditeur et validation visuelle dans la scène officielle.
- [x] Worktree dédié, aucune modification World Map ; commit local uniquement.
