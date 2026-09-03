# M043P-CL — Player DisplayName Truth in Alliance

Après M043O (Journal fonctionnel), le CEO reste affiché comme
`da420f03` (les 8 premiers caractères de son PlayerId) au lieu de son vrai
nom, à la fois au tableau de bord ("Chef") et au Journal ("da420f03 a fondé
l'alliance."). Objectif : trouver la vraie source d'identité déjà
existante et y brancher Alliance — sans créer de nouveau système de profil.

## 1. Vérité d'identité — PROUVÉE, deux systèmes déconnectés

Deux tables de compte totalement indépendantes existent dans ce projet,
**qui ne se réfèrent jamais l'une à l'autre** :

- `BeeKingdom.Accounts.Accounts` (`AccountProfile.DisplayName`) — créé via
  `IAccountService.CreateAccount`, jamais appelé par le vrai flux
  d'authentification Google en production.
- `BeeKingdom.Authentication.AuthenticationAccounts` — créé via
  `CreateGoogleAccount`/`CreateEmailAccount`, avec son **propre** champ
  `DisplayName` + `IsOnboarded`, rempli par le vrai flux du jeu :
  `POST /auth/display-name` (`Program.cs:1992`) — l'écran où le joueur
  choisit réellement son nom public à la première connexion.

`AuthenticationService.cs` ne référence **jamais** `IAccountService` — les
deux systèmes n'ont jamais été reliés. `PlayerDirectoryService`
(M043B, utilisé par Alliance pour tout nom de membre) ne lisait que
`BeeKingdom.Accounts.Accounts.Profile.DisplayName` — **jamais rempli pour
un vrai joueur connecté via Google**, d'où la chaîne vide observée plus tôt
ce soir (`DisplayName=''`).

**A. Source authoritative identifiée ? OUI —
`BeeKingdom.Authentication.AuthenticationAccounts.DisplayName`
(`IsOnboarded=true`).**
**B. Le CEO a-t-il déjà un vrai nom public quelque part ? OUI** (en toute
logique — le jeu exige l'onboarding avant de jouer ; non vérifié par
requête SQL directe cette fois faute d'accès, mais prouvé par
l'architecture : aucun autre chemin ne permet d'atteindre le jeu sans
passer par cet écran).
**C. Valeur exacte identifiée ? N/A** — non interrogée directement en
base ce soir (accès SQL direct non disponible dans cette session) ; sera
visible dès le prochain chargement d'Alliance Center une fois le
correctif déployé.

## 2. Trace de la résolution — cause exacte prouvée

`PlayerDirectoryService.GetByPlayerId` → `IAccountService.GetAccountByPlayerId`
→ `AccountRecord.Profile.DisplayName` (chaîne vide pour un vrai joueur) →
`AllianceService` (batch resolve via `PlayerDirectoryService`) →
`AllianceMemberSummary.DisplayName=""` → côté client,
`ResolvedDisplayName` retombe légitimement sur l'identifiant tronqué
(comportement voulu de ce champ : ne jamais fabriquer un nom, afficher un
repli sûr) → Dashboard/Journal affichent `da420f03`.

**D. Pourquoi Alliance a reçu un DisplayName vide, prouvé ? OUI.**

## 3. Player Directory — corrigé pour lire la bonne source

`PlayerDirectoryService.GetByPlayerId` interroge maintenant
`IAccountCredentialStore.TryGetByPlayerId` (nouvelle méthode, même table
`AuthenticationAccounts`) **en premier** ; si un compte onboardé avec un
nom non vide existe, il est utilisé. Sinon, repli sur l'ancien chemin
(`BeeKingdom.Accounts`) — préserve le comportement existant pour tout
compte synthétique/de test créé uniquement via `IAccountService` (comme
les comptes des tests automatisés). Aucun système de stockage d'identité
dupliqué : lecture de la table déjà existante, rien de nouveau créé.

**F. PlayerDirectory aligné sur la source authoritative ? OUI.**

## 4. Communication — comparaison partielle

`BeeKingdom.Chat` porte un champ `SenderDisplayNameSnapshot` sur chaque
message, mais je n'ai pas pu tracer avec certitude dans le temps imparti
ce soir s'il est résolu côté serveur (via ce même `PlayerDirectoryService`)
ou fourni par le client Unity au moment de l'envoi (un "instantané" figé
au moment du message plutôt qu'une résolution en direct) — les deux
mécanismes sont structurellement différents (l'un ré-résout le nom courant
à chaque affichage, l'autre fige le nom au moment de l'action). Si le
Chat affiche lui aussi des identifiants tronqués, ce serait un défaut
distinct nécessitant sa propre investigation — non confirmé ni infirmé ce
soir.

**E. Identité Communication comparée ? PARTIEL** — mécanisme distinct
repéré, non entièrement tracé faute de temps ; pas de contradiction trouvée
avec le correctif Alliance, mais pas de garantie de cohérence non plus.

## 5. Authentification vs identité publique — respecté

Aucune donnée d'authentification (email, GoogleSubjectId, mot de passe) 
n'est exposée : `PlayerPublicIdentity` ne porte que `PlayerId`+`DisplayName`
(garanti structurellement par un test déjà existant,
`Search_ResultsNeverExposePrivateAccountData`, toujours vert). Le nouveau
code ne lit que `AuthenticationAccount.DisplayName`/`IsOnboarded`, jamais
`Email`/`GoogleSubjectId`/`PasswordHash`.

**I. Aucune identité privée Google/compte exposée ? OUI.**

## 6. Correction

- `Server/src/BeeKingdom.Authentication/Providers/IAccountCredentialStore.cs` :
  nouvelle méthode `TryGetByPlayerId(PlayerId, out AuthenticationAccount)`.
- `SqlAccountCredentialStore.cs`/`InMemoryAccountCredentialStore.cs` :
  implémentation (requête SQL directe sur `AuthenticationAccounts.PlayerId`,
  déjà indexée).
- `Server/src/BeeKingdom.Accounts/BeeKingdom.Accounts.csproj` : nouvelle
  référence de projet vers `BeeKingdom.Authentication` (aucun cycle —
  `Authentication` ne référence jamais `Accounts`).
- `PlayerDirectoryService.GetByPlayerId` : résolution authoritative avec
  repli, comme décrit en section 3.

Aucun changement à `Search()` (recherche de joueurs par nom pour les
invitations Alliance) — reste sur son comportement actuel
(`BeeKingdom.Accounts`) ; corriger la recherche pour interroger aussi les
noms `AuthenticationAccounts` serait un chantier plus large (une vraie
recherche cross-source), explicitement hors du périmètre "ne pas dériver
vers une refonte du système de profil" de cette mission.

**G. Noms des membres Alliance corrigés ? OUI** (dès que le membre a un
compte onboardé). **H. Noms d'acteur au Journal corrigés ? OUI** (même
mécanisme, `SyncAllianceRuntimeStateFromController` déjà branché sur
`ResolvedDisplayName` depuis M043O).

## 7. Tests

`Server/tests/BeeKingdom.Tests/PlayerDirectoryServiceTests.cs` — 4
nouveaux tests :

- `GetByPlayerId_PrefersOnboardedAuthenticationDisplayNameOverAccountRecord`
  — reproduit exactement la forme réelle (Account avec DisplayName vide +
  AuthenticationAccount onboardé) et prouve la priorité correcte.
- `GetByPlayerId_FallsBackToAccountRecordWhenNoAuthenticationAccountExists`
  — les comptes de test/seed existants continuent de fonctionner.
- `GetByPlayerId_IgnoresNotYetOnboardedAuthenticationAccount` — un joueur
  en cours d'onboarding ne doit pas écraser un nom déjà connu par un vide.
- `GetByPlayerId_UnknownPlayer_ReturnsNullWithoutCrashing` — repli sûr,
  jamais de crash.

Suite complète : **10/10 verts** pour `PlayerDirectoryServiceTests`,
**461/461 verts** pour la suite serveur complète (0 échec, 8 ignorés —
tests SQL nécessitant une instance locale, préexistant). Build serveur
complet : 0 erreur.

**J. Tests verts ? OUI.**

## 8. Déploiement requis

**Ce correctif est côté serveur** (nouvelle méthode d'interface, deux
implémentations, nouvelle référence de projet, logique de résolution) —
contrairement aux missions précédentes de ce soir (M043J-O, toutes
client-side). Conformément à la consigne : **aucun déploiement sans
autorisation explicite du CEO.**

## 9. Verdict final (A–K)

| # | Critère | Résultat |
|---|---|---|
| A | Source authoritative identifiée ? | ✅ OUI |
| B | Le CEO a déjà un vrai nom public ? | ✅ OUI (par architecture) |
| C | Valeur exacte identifiée ? | ⚠️ N/A — non interrogée directement, sera visible après déploiement |
| D | Pourquoi Alliance a reçu un nom vide, prouvé ? | ✅ OUI |
| E | Identité Communication comparée ? | ⚠️ PARTIEL |
| F | PlayerDirectory aligné sur la source authoritative ? | ✅ OUI |
| G | Noms des membres Alliance corrigés ? | ✅ OUI |
| H | Noms d'acteur au Journal corrigés ? | ✅ OUI |
| I | Aucune identité privée exposée ? | ✅ OUI |
| J | Tests verts ? | ✅ OUI — 10/10 + 461/461 |
| K | PRÊT POUR NOUVEAU TEST CEO "DISPLAYNAME" ? | ⏳ Après déploiement serveur autorisé |

## 10. Prochain test utilisateur

Une fois le déploiement serveur autorisé et effectué : rouvrir Alliance
Center. Attendu : le vrai nom du CEO (pas "da420f03") au tableau de bord
("Chef") et dans le Journal ("<Vrai nom> a fondé l'alliance.").
