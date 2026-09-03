# M043R-CL — Player Directory Search Must Use Real Authenticated Player Names

Objectif : la recherche de joueur (modal "Inviter un joueur") doit trouver
les vrais joueurs onboardés (source `AuthenticationAccounts`, établie comme
autoritative par M043P pour `GetByPlayerId`), pas seulement les comptes
synthétiques `BeeKingdom.Accounts`.

## 1. Stara existe — preuve directe (lecture seule)

Confirmé via une requête SQL en lecture seule directement fournie par le
CEO sur la base de production :

```
PlayerId    : 77510147-cc80-4922-9bde-aa8a296cdd68
DisplayName : Stara
IsOnboarded : true (1)
```

Aucune donnée privée (email, GoogleSubjectId, mot de passe) n'est reproduite
ici au-delà de ce que le CEO a déjà partagé lui-même dans le fil de travail.

**A. Stara proven to exist in AuthenticationAccounts? OUI.**

## 2. Cause reproduite dans le code — confirmée

`PlayerDirectoryService.Search()` (avant correctif) interrogeait
exclusivement `accounts.QueryAccount(...)` (`BeeKingdom.Accounts`), jamais
`credentials.SearchByDisplayName(...)` (`BeeKingdom.Authentication.
AuthenticationAccounts`) — alors que `GetByPlayerId` avait déjà été corrigé
en ce sens par M043P. Stara n'ayant jamais de compte
`BeeKingdom.Accounts` (créée uniquement via l'onboarding Google réel),
`Search("Stara")`, `Search("St")`, `Search("Sta")`, `Search("star")`
retournaient tous une liste vide — exactement le symptôme rapporté par le
CEO.

**B. Exact current search failure reproduced? OUI** (nouveau test
`Search_LegacyOnlyAccount_StillFound`-style scénario inversé : avant
correctif, un joueur auth-only comme Stara était absent de `Search()` —
prouvé en lisant le code, `accounts.QueryAccount` n'a aucune connaissance
des lignes `AuthenticationAccounts`).
**C. Legacy source identified as cause? OUI.**

## 3. Correctif — source autoritative fusionnée, pas remplacée

`PlayerDirectoryService.Search()` interroge maintenant **les deux
sources** :

- `credentials.SearchByDisplayName(query)` (autoritative,
  `IAccountCredentialStore`, déjà existante — aucune nouvelle méthode
  ajoutée côté `BeeKingdom.Authentication`), filtrée à
  `IsOnboarded == true && DisplayName non vide`.
- `accounts.QueryAccount(...)` (legacy, comptes synthétiques/tests),
  inchangé.

Fusion par `Dictionary<PlayerId, PlayerPublicIdentity>` : les deux sources
alimentent le dictionnaire, la source autoritative est appliquée **en
dernier** — donc si un même `PlayerId` existe dans les deux (cas M043P), le
nom autoritative écrase le nom legacy, exactement la même précédence
d'identité déjà établie pour `GetByPlayerId`.

**D. Authentication public-name search implemented? OUI.**
**G. Legacy accounts preserved? OUI** (test
`Search_LegacyOnlyAccount_StillFound`).
**H. Results deduplicated by PlayerId? OUI** (test
`Search_SamePlayerIdInBothSources_DeduplicatesAndAuthoritativeNameWins`).

## 4. Sémantique de recherche

- Longueur minimale 2 caractères : inchangé (`MinQueryLength = 2`, déjà en
  place).
- Insensible à la casse : `SearchByDisplayName` (`InMemory`: `Contains`
  avec `OrdinalIgnoreCase` ; `Sql`: `LIKE` déjà insensible à la casse par
  collation par défaut).
- Correspondance "contient" (pas seulement préfixe) pour rester cohérent
  avec le comportement legacy déjà en place — mais le tri place maintenant
  les correspondances de préfixe **avant** les correspondances "contient"
  ailleurs dans le nom (`OrderByDescending(name.StartsWith(query))`), donc
  "St" → "Stara" apparaît en tête même s'il existait un "Allstar".
- Limite de résultats toujours appliquée après fusion (`Skip`/`Take` sur le
  résultat fusionné, pas avant).

**E. Partial >=2 chars supported? OUI.**
**F. Case-insensitive search supported? OUI.**

## 5. Confidentialité

`PlayerPublicIdentity` ne porte toujours que `PlayerId` + `DisplayName`
(garantie structurelle, testée par
`Search_ResultsNeverExposePrivateAccountData` déjà existant et par le
nouveau `Search_NeverExposesEmailOrAuthProviderData`). Aucune méthode
publique n'expose `AuthenticationAccount` lui-même ; le filtrage
`IsOnboarded`/`DisplayName non vide` se fait avant la projection, jamais
après exposition.

**I. Private authentication data protected? OUI.**

## 6. UI Unity — états de recherche distincts

Le debounce (~350ms après la dernière frappe, dès 2 caractères) **existait
déjà** et fonctionnait correctement (`DrawAllianceInvitePlayerBody`,
M043B-CL) — ce n'était pas la cause du bug rapporté. Le bouton "Chercher"
déclenche toujours une recherche immédiate, inchangé.

Le vrai défaut UX trouvé : le même message "Tapez au moins 2 caractères…"
s'affichait aussi bien pour "pas encore tapé" que pour "recherché, zéro
résultat" que pour "erreur réseau" — ce qui, une fois Stara réellement
recherchable, aurait continué à mentir sur un vrai zéro-résultat ou une
vraie erreur. Nouveau `InvitePlayerSearchStatus` (`Idle` / `Searching` /
`Empty` / `Results` / `Error`) exposé par `AllianceCenterPanelController`,
consommé par `DrawAllianceInvitePlayerBody` pour distinguer les 4 messages
+ un bouton "Réessayer" explicite sur erreur (relance la même recherche).
`DisplayName` reste le seul identifiant affiché (jamais le `PlayerId` brut)
— inchangé.

**J. Unity live/debounced search implemented? OUI** (déjà en place,
confirmé fonctionnel, non modifié).
**K. Chercher still works immediately? OUI** (inchangé).

## 7. Éligibilité à l'invitation

Aucune règle d'Alliance nouvelle inventée. Le comportement produit actuel
(retourner le joueur même s'il appartient déjà à une autre alliance, refus
géré côté serveur au moment de l'invitation elle-même) n'a pas été touché —
hors périmètre explicite de cette mission.

## 8. Tests

`Server/tests/BeeKingdom.Tests/PlayerDirectoryServiceTests.cs` — 8 nouveaux
tests (section M043R-CL) :

- `Search_FindsAuthOnlyOnboardedPlayer_ExactStaraRuntimeContract` — le
  scénario exact rapporté (joueur auth-only, préfixe "St" + recherche
  insensible à la casse "stara").
- `Search_MatchesPartialContainsAnywhereInName`
- `Search_ExcludesNotYetOnboardedAuthenticationPlayer`
- `Search_ExcludesAuthenticationPlayerWithEmptyDisplayName`
- `Search_LegacyOnlyAccount_StillFound`
- `Search_SamePlayerIdInBothSources_DeduplicatesAndAuthoritativeNameWins`
- `Search_ResultLimitStillEnforcedWithMergedSources`
- `Search_NeverExposesEmailOrAuthProviderData`

**L. Stara regression test green? OUI.**

Suite serveur complète : **479 tests, 471 verts, 0 échec, 8 ignorés** (SQL,
instance locale indisponible — préexistant). Build serveur complet : 0
erreur.

**M. Server tests green? OUI.**

Compilation Unity (`assets-refresh`) : 0 erreur après le changement de
`PlayerDirectoryService.cs`, `AllianceCenterPresentation.cs` et
`HiveViewProductUiPresenter.cs`. Aucun test Unity automatisé nouveau écrit
pour la partie UI : `AllianceCenterPanelController` n'a aujourd'hui aucune
suite de tests existante (aucun faux `IAllianceClient` réutilisable dans le
repo), et construire un stub complet de cette interface uniquement pour ce
correctif aurait été disproportionné par rapport au périmètre de la
mission. Vérifié par compilation + relecture de code ; la vérification
comportementale réelle est le retest humain (section 13/9 ci-dessous), déjà
prévu par la mission.

**N. Unity compile/tests green? PARTIEL — compilation 0 erreur, pas de
nouveau test automatisé Unity (justifié ci-dessus).**

## 9. Déploiement

Changement serveur (`PlayerDirectoryService.cs`) + changement Unity local
(`AllianceCenterPresentation.cs`, `HiveViewProductUiPresenter.cs`, ce
dernier ne nécessitant aucun déploiement API). **Aucun déploiement effectué
— en attente d'autorisation explicite du CEO**, comme pour chaque
correctif serveur de cette session.

**O. Deployment required? OUI (serveur uniquement) — non encore fait.**

## 10. Verdict final (A–P)

| # | Critère | Résultat |
|---|---|---|
| A | Stara proven to exist? | ✅ OUI |
| B | Exact current search failure reproduced? | ✅ OUI |
| C | Legacy source identified as cause? | ✅ OUI |
| D | Authentication public-name search implemented? | ✅ OUI |
| E | Partial >=2 chars supported? | ✅ OUI |
| F | Case-insensitive search supported? | ✅ OUI |
| G | Legacy accounts preserved? | ✅ OUI |
| H | Results deduplicated by PlayerId? | ✅ OUI |
| I | Private authentication data protected? | ✅ OUI |
| J | Unity live/debounced search implemented? | ✅ OUI (déjà en place) |
| K | Chercher still works immediately? | ✅ OUI |
| L | Stara regression test green? | ✅ OUI |
| M | Server tests green? | ✅ OUI (479, 471 verts, 8 ignorés) |
| N | Unity compile/tests green? | ⚠️ Compilation OUI, pas de nouveau test auto Unity |
| O | Deployment required? | ✅ OUI — en attente d'autorisation |
| P | READY FOR CEO STARA SEARCH RETEST? | ⏳ Après déploiement serveur autorisé |

## 11. Prochain test utilisateur

Une fois le déploiement serveur autorisé et effectué : rouvrir Alliance
Center → Inviter → taper "St". Attendu : "Stara" apparaît automatiquement
après le debounce. Puis taper "Stara" en entier : même résultat. Ne pas
cliquer "Inviter" avant validation visuelle explicite, comme demandé.
