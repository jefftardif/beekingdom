# Reset-BeeKingdomTestAccount.ps1

Reinitialise completement un compte de test BeeKingdom (Compte -> Joueur -> Ruche
-> toutes les donnees possedees) pour qu'une reconnexion avec le MEME compte
Google/email se comporte comme un tout nouveau joueur (nouvelle ruche, New
Player Bootstrap, tutoriel a zero). Le compte Google lui-meme n'est jamais
touche - seulement son identite et ses donnees BeeKingdom.

## Objectif

Outil DEV/QA. Sert a retester le parcours nouveau joueur (FTUE) avec un vrai
compte Google sans devoir en creer un nouveau a chaque fois.

## Prerequis

- Le meme environnement/config que les autres commandes `BeeKingdom.Tools`
  (`grant-resources`, `repair-squad-reservation`, etc.) : la chaine de
  connexion SQL est resolue par le mecanisme existant
  (`ConnectionStrings:*` / `SqlServer:*`, via variables d'environnement ou
  `appsettings.{Environment}.json`) - ce script ne lit ni ne stocke aucun
  secret lui-meme.
- `dotnet` disponible dans le PATH.

## Utilisation

Dry-run (rien n'est modifie, affiche uniquement ce qui serait supprime) :

```powershell
.\Reset-BeeKingdomTestAccount.ps1 -Email "example@gmail.com" -DryRun
```

Reset reel (environnement par defaut = Development) :

```powershell
.\Reset-BeeKingdomTestAccount.ps1 -Email "example@gmail.com"
```

Le script affiche toujours d'abord un rapport de decouverte (AccountId,
PlayerId, HiveId(s), donnees trouvees), puis - si ce n'est pas un `-DryRun` -
demande de RETAPER l'email exact pour confirmer avant de supprimer quoi que
ce soit.

## Protection Production

Par defaut, `-Environment Production` est REFUSE :

```powershell
.\Reset-BeeKingdomTestAccount.ps1 -Email "example@gmail.com" -Environment Production
# Refuse: -Environment Production exige aussi -AllowProduction.
```

Pour reellement l'autoriser, il faut les deux : `-Environment Production
-AllowProduction`. Une bannniere `*** PRODUCTION DATABASE ***` s'affiche, et
une deuxieme confirmation (taper `PRODUCTION` en majuscules) est demandee en
plus du retapage de l'email.

```powershell
.\Reset-BeeKingdomTestAccount.ps1 -Email "example@gmail.com" -Environment Production -AllowProduction
```

## Comportement sur les donnees partagees (chat)

Si le joueur est le dernier membre actif d'une conversation, la conversation
entiere est supprimee. Si d'autres joueurs reels restent dans une
conversation/groupe, seules les donnees PROPRES au joueur cible sont retirees
(sa participation, son inbox) - l'historique des messages reste intact pour
les autres. Si le joueur cible etait Leader d'un groupe avec d'autres membres
actifs, le leadership est transfere automatiquement avant suppression (jamais
de groupe reel laisse sans leader).

## Idempotence

Relancer l'outil sur un email deja reinitialise (ou qui n'a jamais existe)
est un succes silencieux ("Account not found / already reset"), jamais une
erreur, et ne supprime jamais le mauvais joueur.

## Limites connues

- Necessite un acces reseau au vrai serveur SQL cible (pas disponible depuis
  toutes les machines de developpement).
- Un systeme d'identite legacy separe (`dbo.Accounts`, module
  `BeeKingdom.Accounts`, jamais relie au vrai login joueur) n'est PAS
  supprime automatiquement - seulement signale s'il existe une ligne avec le
  meme email. A traiter manuellement si pertinent.
- Ne touche pas a une future appartenance d'Alliance (pas encore implementee
  cote serveur au moment de cet outil) - prevu comme prochaine etape
  d'extension du pipeline (voir `ResetTestAccountAsync` dans
  `Server/src/BeeKingdom.Tools/Program.cs`).

## Test manuel complet (a faire par un humain)

1. Creer/utiliser un compte de test avec un vrai compte Google, jouer
   quelques minutes (batir, ressources, etc.).
2. `.\Reset-BeeKingdomTestAccount.ps1 -Email "..." -DryRun` - verifier que le
   rapport de decouverte correspond a ce qui a ete joue.
3. `.\Reset-BeeKingdomTestAccount.ps1 -Email "..."` - confirmer, verifier
   `RESET RESULT` = PASS partout.
4. Se reconnecter dans le jeu avec le MEME compte Google - confirmer :
   nouvelle ruche, New Player Bootstrap, tutoriel depuis le debut, aucune
   ancienne donnee (batiments/ressources/troupes/recherche/minuteries)
   visible.
5. Relancer l'outil une deuxieme fois sur le meme email - confirmer
   "Account not found / already reset", aucune erreur.
