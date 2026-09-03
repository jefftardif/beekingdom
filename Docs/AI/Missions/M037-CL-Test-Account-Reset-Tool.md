# M037-CL — Complete Test Account Reset Tool (PowerShell)

**Note de numerotation :** demande initialement comme M036, mais un fichier
`M036-OC-Windows-Internal-Debug-Build-Portability.md` (non commite, autre
agent) occupait deja ce numero au moment de la creation de ce rapport —
renomme en M037 pour eviter toute collision, sans toucher au fichier M036
existant.

**Date:** 2026-08-30
**Agent:** Claude Code
**Portee:** Outil DEV/QA uniquement. Aucun code de gameplay/FTUE/WorldMap/Alliance/Web-Communication touche.
**Git:** Aucun commit, aucun push effectue pour cette mission (conforme a la contrainte finale de la mission).

---

## 1. Objectif de la mission

Fournir `Reset-BeeKingdomTestAccount.ps1`, invocable
`.\Reset-BeeKingdomTestAccount.ps1 -Email "example@gmail.com"`, qui supprime
integralement et de facon sure toutes les donnees BeeKingdom d'un joueur de
test (Compte -> Joueur -> Ruche -> toutes les donnees possedees), de sorte
qu'une reconnexion avec le MEME compte Google se comporte comme un tout
nouveau joueur (nouvel etat compte/joueur, nouvelle ruche, New Player
Bootstrap, FTUE depuis zero — aucun ancien batiment/ressource/troupe/
recherche/minuterie/progression de tutoriel ne doit reapparaitre). Le compte
Google lui-meme n'est jamais touche, seulement son identite/donnees
BeeKingdom.

## 2. Architecture reelle decouverte (avant toute conception)

Inspection directe du code (pas d'hypothese) :

- **Chaine d'identite** : `Email` (normalisation exacte `.Trim()`, aucune
  mise en minuscule — confirmee dans `SqlAccountCredentialStore.TryGetByEmail`)
  -> `dbo.AuthenticationAccounts.AccountId` (1:1) ->
  `dbo.AuthenticationAccounts.PlayerId` (1:1, genere independamment a la
  creation du compte, pas derive de l'AccountId) -> `dbo.Colonies.PlayerId` /
  `dbo.HivePlayerStates.PlayerId` -> `ColonyId`/`HiveId` (1:plusieurs).
- **`dbo.HivePlayerStates.StateJson`** : un seul blob JSON qui contient
  quasiment tout l'etat de jeu par joueur (ressources, batiments, recherche,
  champions, VIP, jetons de rappel, reservation d'escouade, exploration,
  progression FTUE) — il n'existe pas de tables separees par domaine de
  gameplay pour la plupart de ces donnees. Supprimer la/les ligne(s)
  `HivePlayerStates` retire tout ce contenu d'un coup.
- **Systeme d'identite legacy separe** : module `BeeKingdom.Accounts`
  (`dbo.Accounts`), avec son propre espace `AccountId`/`PlayerId`
  independant, accessible via `POST /accounts`, **jamais relie** au vrai
  chemin de connexion (`dbo.AuthenticationAccounts`) par aucun code du
  depot. Decision : signale par l'outil (compte trouve avec le meme email)
  mais **non supprime automatiquement** — relation avec le joueur reel
  ambigue, une suppression automatique serait une hypothese non verifiee.
- **Modele de donnees du chat**, confirme via
  `Server/src/BeeKingdom.Chat/Repositories/SqlChatRepository.cs` et les
  scripts de migration : `dbo.ChatConversations` (partage),
  `dbo.ChatConversationParticipants` (par joueur, suppression individuelle
  sure — deja le pattern utilise par `SaveConversation`),
  `dbo.ChatConversationSequences` (lie a la conversation, jamais par joueur),
  `dbo.ChatMessages` (partage, doit survivre pour les autres participants),
  `dbo.ChatInbox`/`dbo.ChatOutboxReceipts` (par joueur),
  `dbo.ChatConversationCreationReceipts` (par joueur, global),
  `dbo.ChatModerationReports`/`dbo.ChatModerationReportReceipts` (FK :
  receipts -> reports, ordre de suppression obligatoire),
  `dbo.ChatMessageTranslations` (`ON DELETE CASCADE` depuis `ChatMessages`,
  aucune suppression separee necessaire), `dbo.ChatGroupInvites`,
  `dbo.ChatPreferences`.
- **Regle reelle de leadership/depart de groupe** confirmee dans
  `ChatService.cs` : un Leader peut quitter librement s'il est le dernier
  membre actif ; sinon, `LeaveGroup` refuse
  (`leader_must_transfer_before_leaving`). Aucun chemin existant ne peut
  forcer le retrait d'un Leader pendant que d'autres membres restent — la
  politique de l'outil (transfert automatique du leadership avant
  suppression forcee) est concue pour ne JAMAIS contredire cette regle
  reelle, en s'appuyant sur le meme mecanisme que `TransferGroupLeadership`.

## 3. Politique de donnees partagees (chat)

Pour chaque conversation ou le joueur cible est membre :

- **Aucun autre membre actif restant** : la conversation entiere est
  supprimee (messages, sequence, participants, invites, inbox/outbox liees).
  Elle n'appartient plus a personne.
- **D'autres joueurs reels restent** : seules les donnees PROPRES au joueur
  cible sont retirees (`ChatConversationParticipants`, `ChatInbox`,
  `ChatOutboxReceipts`) — `ChatMessages`/`ChatConversations` restent
  intacts pour les autres. Si le joueur cible etait Leader, le leadership
  est transfere au membre actif le plus ancien avant sa suppression
  (mirroir exact des deux `UpdateParticipantRole` de
  `TransferGroupLeadership`).

## 4. Contrainte SQL absolue — respectee

**Aucune migration SQL, aucun changement de schema n'a ete necessaire.**
Toutes les tables touchees existent deja (creees par les migrations
`020_accounts.sql`, `030_authentication_sessions.sql`, `040_colonies.sql`,
`050_colony_snapshots.sql`, `061_chat_translations.sql`,
`063_chat_moderation_idempotency.sql`, `065_chat_groups.sql`,
`070_hive_operations.sql`). L'outil n'execute que des `DELETE`/`UPDATE`
via ADO.NET brut (`Microsoft.Data.SqlClient`), exactement comme toutes les
autres commandes de `BeeKingdom.Tools`. Aucun STOP necessaire — cette voie
n'a jamais ete requise.

## 5. Conception du pipeline (extensible)

Pipeline ordonne, en regions separees dans
`ResetTestAccountAsync`/methodes associees (`Server/src/BeeKingdom.Tools/Program.cs`) :

1. Resoudre l'identite (Email exact -> AccountId/PlayerId).
2. Decouvrir toutes les donnees possedees (toujours execute, meme en
   dry-run).
3. Nettoyage Chat (politique partagee ci-dessus).
4. Nettoyage Ruche/Colonie/Sessions.
5. Suppression de l'identite centrale (en dernier — chaque autre table est
   retrouvee PAR ce PlayerId/AccountId).
6. Verification post-suppression.

Un futur domaine ("Cleanup Alliance Membership") s'ajoute comme UNE methode
supplementaire appelee dans la meme transaction, sans toucher aux autres —
l'emplacement exact est marque par un commentaire dans le code
(`// Future domain slots in right here`).

## 6. Emplacement du script

Convention verifiee avant creation : `Server/tools/` existe deja et contient
exactement ce type de script (`New-ProductionCandidateLocal.ps1`,
`Test-ProductionConfiguration.ps1`, etc., style Verb-Noun PowerShell). Le
dossier `Tools/` a la racine du depot est un espace non lie (scripts Python
de generation de carte terrain) — non utilise ici. Fichiers ajoutes :

- `Server/tools/Reset-BeeKingdomTestAccount.ps1`
- `Server/tools/README-Reset-BeeKingdomTestAccount.md` (doc operateur)

## 7. Implementation serveur

Nouvelle commande `reset-test-account` dans
`Server/src/BeeKingdom.Tools/Program.cs`, meme style que les commandes
existantes (`HostApplicationBuilder`, DI, ADO.NET brut) :

- Resolution par email **exact** (`WHERE Email = @Email`, `.Trim()`
  uniquement) — jamais `LIKE`, contrairement aux autres commandes
  (`grant-resources`, etc.) qui acceptent des sous-chaines. Deliberement
  different : une correspondance approximative ici supprimerait le mauvais
  joueur.
- Phase de decouverte toujours executee (dry-run ou non), affiche
  AccountId/PlayerId/HiveId(s)/toutes les categories de donnees trouvees.
- Sans `--apply` : aucune ecriture, retourne apres le rapport.
- Avec `--apply` : une seule `SqlTransaction` (`ReadCommitted`) pour tout le
  pipeline de nettoyage + suppression finale. Toute exception -> rollback
  complet, message d'erreur explicite, aucune suppression partielle
  conservee.
- Verification post-commit : re-interroge chaque categorie de donnees,
  affiche un bloc `RESET RESULT` PASS/FAIL par categorie + un verdict
  global.
- Idempotent : email introuvable -> "Account not found / already reset",
  code de sortie 0, jamais une exception.

## 8. Implementation PowerShell

`Reset-BeeKingdomTestAccount.ps1` :

- Parametres : `-Email` (obligatoire), `-DryRun`, `-Environment`
  (Development/Staging/Production, defaut Development), `-AllowProduction`.
- Aucun secret en dur — la resolution de la chaine de connexion reste
  entierement du ressort du mecanisme existant de `BeeKingdom.Tools`
  (variables d'environnement / `appsettings.{Environment}.json`), le script
  ne fait que transmettre `-Environment` via `DOTNET_ENVIRONMENT`/
  `ASPNETCORE_ENVIRONMENT`.
- Lance toujours d'abord un passage de decouverte (sans `--apply`), affiche
  la bannniere (Email/Environment/donnees trouvees).
- `-DryRun` s'arrete la, sans rien modifier.
- Sinon : exige de retaper l'email exact (`Read-Host`), refuse et annule si
  different.
- `-Environment Production` sans `-AllowProduction` est **refuse des le
  depart** avec message explicite. Avec les deux : bannniere
  `*** PRODUCTION DATABASE ***` + confirmation renforcee (taper
  `PRODUCTION` en majuscules) en plus du retapage de l'email.
- N'appelle `--apply` qu'apres reussite de toutes les confirmations.
- Code de sortie reflete celui de l'outil .NET sous-jacent.

Verifie par `[System.Management.Automation.Language.Parser]::ParseFile` —
aucune erreur de syntaxe.

## 9. Compilation

`dotnet build Server/src/BeeKingdom.Tools/BeeKingdom.Tools.csproj` — **PASS**,
0 avertissement, 0 erreur.

## 10. Documentation operateur

`Server/tools/README-Reset-BeeKingdomTestAccount.md` : objectif,
prerequis, usage (dry-run / reset reel / production), politique de donnees
partagees, idempotence, limites connues, procedure de test manuel complete
(creer -> dry-run -> reset -> reconnexion meme compte Google -> verifier
nouveau joueur -> relancer une deuxieme fois -> verifier idempotence).

## 11. Liste de tests

Aucun acces reseau a une vraie base SQL Server n'est disponible depuis cette
machine (confirme lors de l'investigation : la config Production suivie en
depot force `Persistence:Provider=InMemory`, la vraie configuration
`SqlServer` de production est injectee via variable d'environnement sur
l'App Pool IIS distant, inaccessible d'ici). En consequence :

| # | Test | Statut |
|---|------|--------|
| 1 | Compilation du projet `BeeKingdom.Tools` avec la nouvelle commande | **PASS** (verifie) |
| 2 | Syntaxe PowerShell valide (parse sans erreur) | **PASS** (verifie) |
| 3 | `-Environment Production` sans `-AllowProduction` refuse immediatement | **PASS** (verifie par lecture du code — refus avant tout appel reseau) |
| 4 | Resolution email exacte (pas de `LIKE`, `.Trim()` uniquement, coherent avec le login reel) | **PASS** (verifie par revue de code, identique a `SqlAccountCredentialStore`) |
| 5 | Idempotence : email introuvable -> code 0, message "already reset", pas d'exception | **PASS** (verifie par revue de code) |
| 6 | Transaction unique, rollback complet sur exception | **PASS** (verifie par revue de code — un seul `SqlTransaction`, `catch` -> `RollbackAsync`) |
| 7 | Ordre de suppression respecte les FK connues (receipts avant reports, etc.) | **PASS** (verifie par revue de code contre les scripts de migration) |
| 8 | Politique de conversation partagee (suppression totale si dernier membre, sinon suppression partielle + transfert de leadership) | **PASS** (verifie par revue de code contre `ChatService.cs`) |
| 9 | `DryRun` n'ecrit rien | **PASS** (verifie par revue de code — retourne avant toute transaction) |
| 10 | Aucune migration SQL / changement de schema introduit | **PASS** (verifie — aucun fichier de script de migration ajoute ni modifie) |
| 11 | Execution reelle contre une base SQL Server peuplee (dry-run puis reset) | **VALIDATION MANUELLE REQUISE** — pas d'acces reseau SQL depuis cet environnement |
| 12 | Reconnexion en jeu avec le meme compte Google apres reset -> nouveau joueur complet (procedure documentee dans le README) | **VALIDATION MANUELLE REQUISE** |

## 12. Ce qui n'a PAS ete fait (respect strict du "NE PAS FAIRE")

- Aucune migration SQL, aucun changement de schema.
- Aucun endpoint public destructeur ajoute (outil CLI/PowerShell local
  uniquement, memes conventions d'acces que les commandes existantes).
- Aucun secret dans le depot (script transmet uniquement `-Environment`,
  jamais de chaine de connexion ou cle).
- Aucun `playerId`/`hiveId` en dur.
- Aucune recherche d'email approximative (`LIKE`/`Contains`/`StartsWith`
  explicitement evites — voir section 7).
- Aucune suppression globale de table — toutes les requetes sont filtrees
  par `PlayerId`/`AccountId`/`ConversationId` resolus depuis l'email exact.
- Aucun autre joueur reset (email exact uniquement, jamais de sous-chaine).
- Aucun changement de gameplay/FTUE/WorldMap/Alliance/Communication web.
- Aucun commit, aucun push. Aucune modification/revert des changements non
  commites deja presents dans l'arbre de travail (feature groupes/
  invitations de chat, non touchee).

## 13. Limites connues / hypotheses documentees

- Le systeme legacy `dbo.Accounts` (module `BeeKingdom.Accounts`) est
  signale mais jamais supprime automatiquement — sa relation avec
  l'identite de connexion reelle n'est etablie par aucun code du depot.
  Decision documentee ici plutot qu'une suppression a l'aveugle.
- `HiveOperationQueue`/`HiveCommandReceipts` sont nettoyes par precaution
  (filtres par `PlayerId`) bien qu'aucun code du depot ne les peuple
  actuellement — evite une divergence silencieuse si une future
  fonctionnalite les utilise sans que cet outil soit mis a jour.
- Aucune donnee d'Alliance n'existe encore cote serveur au moment de cette
  mission — le point d'extension pour un futur "Cleanup Alliance
  Membership" est marque explicitement dans le code.

## 14. Fichiers ajoutes (aucun commit)

- `Server/src/BeeKingdom.Tools/Program.cs` (modifie — nouvelle commande
  `reset-test-account` + implementation)
- `Server/tools/Reset-BeeKingdomTestAccount.ps1` (nouveau)
- `Server/tools/README-Reset-BeeKingdomTestAccount.md` (nouveau)
- `Docs/AI/Missions/M037-CL-Test-Account-Reset-Tool.md` (ce rapport)

## 15. Verdict final

- **A. L'outil supprime-t-il completement les donnees d'un joueur de test ?**
  OUI (par revue de code exhaustive du pipeline contre le schema reel) —
  confirmation en conditions reelles = VALIDATION MANUELLE REQUISE (pas
  d'acces SQL local).
- **B. Le compte Google reste-t-il intact ?**
  OUI — aucune donnee Google/OAuth n'est touchee, seule l'identite
  BeeKingdom (`dbo.AuthenticationAccounts`) est supprimee.
- **C. L'outil est-il sur pour les donnees de chat partagees avec d'autres
  joueurs reels ?**
  OUI — politique explicite verifiee contre le vrai comportement de
  `ChatService.cs`, jamais de suppression de messages appartenant encore a
  d'autres participants actifs.
- **D. L'outil respecte-t-il la contrainte "aucune migration SQL" ?**
  OUI — aucune migration ni changement de schema, confirme.
- **E. L'outil est-il idempotent et sans risque de mauvais joueur supprime ?**
  OUI — email exact uniquement, deuxieme execution = no-op sans erreur.
- **F. La mission est-elle entierement testee en conditions reelles ?**
  NON — bloquants : (1) aucun acces reseau a une base SQL Server reelle
  depuis cette machine ; (2) verification finale ("reconnexion = nouveau
  joueur") necessite un vrai compte Google et le jeu lui-meme, hors de
  portee d'une verification automatisee. Ces deux points sont documentes
  comme VALIDATION MANUELLE REQUISE avec une procedure precise dans le
  README (section 11 et README section "Test manuel complet").
