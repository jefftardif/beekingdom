# M057-CL — Correction de la derive du catalogue SQL (091_alliance_help)

Date : 2026-09-07
Perimetre : serveur uniquement. Aucune modification Unity, aucun commit, aucun
deploiement, aucune connexion a la base de production.

## 1. Symptome

Le test `BeeKingdom.Tests.DatabaseMigrationTests.CatalogSqlMatchesCheckedInScriptFiles`
echouait de facon reproductible sur l'entree `091_alliance_help.sql` :

```
Expected string length 1717 but was 3092. Strings differ at index 0.
```

Defaut preexistant, sans lien avec la mission chat qui l'a repere (voir le jalon
RAP-OPTIONNEL-COMMUNICATIONS_01 du 2026-09-07).

## 2. Mecanique verifiee avant toute correction

**Le test.** Il parcourt `BootstrapScripts` + `Migrations`, retrouve pour chaque
entree le fichier homonyme dans `Server/src/BeeKingdom.Database/Scripts/`, et
compare le texte integral apres une normalisation minimale : conversion CRLF ->
LF et `Trim()`. Aucune tolerance sur les commentaires, l'indentation interne ou
les lignes vides. C'est donc un test d'egalite stricte entre la copie inline du
catalogue et le fichier versionne.

**Le runner de migration** (`SqlServerMigrationRunner`). Points determinants
etablis par lecture directe :

- Il execute exclusivement `DatabaseCatalog.Migrations[].Sql`, c'est-a-dire la
  copie INLINE. Les fichiers `.sql` sur disque ne sont jamais lus a l'execution :
  ils servent de reference versionnee, garantie conforme par le test ci-dessus.
- Le suivi des migrations appliquees se fait **par NOM de script**, pas par
  contenu ni par hash : la table `dbo.SchemaVersion` ne stocke qu'une colonne
  `ScriptName`, alimentee par un simple `INSERT ... VALUES (@ScriptName)`, et la
  selection des migrations en attente est un `Where(script => !applied.Contains(script.Name))`.
- Consequence : une migration deja enregistree n'est **jamais** reexecutee, quel
  que soit le contenu ulterieur de sa copie inline. Chaque script s'execute dans
  sa propre transaction serialisable, sous verrou applicatif de session.

## 3. Diff exact entre la copie inline et le fichier disque

Comparaison mecanique : extraction de la copie inline, suppression des
commentaires SQL et des lignes vides des deux cotes, puis `diff`.

Resultat : **aucune difference**. Le DDL est rigoureusement identique de part et
d'autre — memes deux tables, memes colonnes, memes types, memes contraintes de
cle primaire et etrangere, memes deux index (dont l'index unique filtre sur
`Status = N'Open'`).

La totalite de l'ecart (1717 -> 3092 caracteres) provient de **commentaires
ajoutes au fichier disque apres coup** et jamais repercutes dans la copie inline :
un bloc d'en-tete M045-CL decrivant les deux tables, et deux commentaires
explicatifs places juste avant chacun des index.

**Aucune table, colonne, contrainte ou index ne manque en production.** La
question d'une migration additive `095_...` est donc sans objet ici : il n'y a
rien a rattraper cote schema.

## 4. Decision

Synchroniser la copie inline du catalogue sur le fichier disque, en y reportant
a l'identique les commentaires manquants. C'est le sens de correction sur :

- Le fichier disque est la version la plus documentee et la plus recente, et son
  DDL est deja celui qui tourne en production.
- Reporter des commentaires ne change strictement rien au SQL executable.
- L'alternative (retirer les commentaires du fichier disque pour l'aligner sur
  l'inline) detruirait de la documentation utile sans aucun benefice.

**Risque pour la production : nul.** Deux raisons cumulatives et independantes :
`091_alliance_help.sql` est deja enregistre dans `dbo.SchemaVersion`, donc le
runner ne le reexecutera jamais au prochain demarrage ; et meme s'il le faisait,
le seul delta est constitue de commentaires SQL, le DDL restant par ailleurs
integralement idempotent (`IF OBJECT_ID(...) IS NULL` sur les deux tables).

Sur un environnement neuf (dev, test, nouvelle instance), la nouvelle copie
inline cree exactement le meme schema qu'avant, commentaires en plus.

## 5. Preuves de test

Commande : `dotnet test` sur `Server/tests/BeeKingdom.Tests`.

- Cible : `DatabaseMigrationTests` — 12/12 reussis, 0 echec, dont
  `CatalogSqlMatchesCheckedInScriptFiles` qui echouait avant.
- Suite complete, trois executions consecutives : **647 reussis, 0 echec**,
  8 ignores, 655 au total.
  - `Server/tests/BeeKingdom.Tests/TestResults/m057.trx`
  - `Server/tests/BeeKingdom.Tests/TestResults/m057_r1.trx`
  - `Server/tests/BeeKingdom.Tests/TestResults/m057_r2.trx`

Reserve honnete : la toute premiere execution complete apres correction a
affiche 646 reussis / 1 echec, sans journal `.trx` permettant de nommer le test
fautif. Les trois executions instrumentees qui ont suivi sont vertes. Il s'agit
donc d'une instabilite ponctuelle (test sensible au temps ou a la concurrence),
sans rapport avec le catalogue SQL : `CatalogSqlMatchesCheckedInScriptFiles` est
purement deterministe (lecture de fichier + comparaison de chaines) et ne peut
pas etre a l'origine d'un echec intermittent. A surveiller si le phenomene se
reproduit.

## 6. Fichiers touches

- `Server/src/BeeKingdom.Database/DatabaseCatalog.cs` — copie inline de l'entree
  `091_alliance_help.sql` synchronisee sur le fichier disque (commentaires
  uniquement, DDL inchange).

Le fichier `Server/src/BeeKingdom.Database/Scripts/091_alliance_help.sql` n'a
pas ete modifie. Aucun nouveau script de migration n'a ete cree. Aucun test n'a
ete modifie : `CatalogSqlMatchesCheckedInScriptFiles` couvrait deja exactement
ce defaut et le prouve corrige.

## 7. Enseignement a retenir

Toute modification d'un fichier `Server/src/BeeKingdom.Database/Scripts/*.sql`
— **y compris un simple commentaire** — doit etre repercutee dans la copie
inline de `DatabaseCatalog.cs`, sans quoi la suite serveur passe au rouge. Et si
la modification touche le DDL d'un script deja applique en production, la copie
inline ne suffit pas : il faut un NOUVEAU script numerote, puisque le runner ne
rejoue jamais une migration deja enregistree par son nom.
