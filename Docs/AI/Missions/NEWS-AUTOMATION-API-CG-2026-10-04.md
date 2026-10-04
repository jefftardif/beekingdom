# BeeKingdom News Automation API — CG — 2026-10-04

## Objectif

Permettre la publication quotidienne des nouvelles BeeKingdom sans passer par
l'interface graphique d'administration du site.

Le système conserve le CMS actuel comme source de vérité : les articles restent
stockés et servis par le module `BeeKingdom.News` du serveur de jeu. Le site
`beekingdomgame.com` continue de lire les mêmes endpoints publics.

## Architecture

### Endpoint serveur

`POST /news/v1/automation/publish`

Corps JSON :

```json
{
  "slug": "alpha-getting-closer-2026-10-04",
  "titleEn": "...",
  "titleFr": "...",
  "excerptEn": "...",
  "excerptFr": "...",
  "bodyEn": "...",
  "bodyFr": "..."
}
```

Header requis :

`X-BeeKingdom-News-Key`

L'endpoint ne réutilise ni la session d'un joueur/admin, ni la clé globale
`Ops:AdminKey`.

### Idempotence

L'opération est idempotente par `slug` :

- slug absent : création du brouillon puis publication;
- slug existant : mise à jour des six champs bilingues puis publication;
- un nouvel appel avec le même slug ne crée jamais un second article;
- `PublishedAtUtc` conserve la date de première publication.

Une publication automatique exige les deux titres et les deux corps. Les
longueurs sont aussi bornées aux limites SQL existantes pour titres/extraits et
à 200 000 caractères par corps.

## Sécurité

La clé d'automatisation n'est jamais stockée dans Git ni sur le poste de
développement.

Lors du premier déploiement contenant cette fonctionnalité, le runner de
production génère 32 octets cryptographiquement aléatoires et conserve la clé
uniquement ici :

`C:\inetpub\BeeKingdomApi\news-automation.key`

ACL appliquée :

- `IIS AppPool\BeeKingdomApi` : lecture;
- `SYSTEM` : contrôle total;
- `Administrators` : contrôle total;
- héritage supprimé.

Le serveur lit la clé depuis ce fichier au moment de l'appel et effectue la
comparaison avec la routine constante `VerifyProvidedSecret` déjà utilisée
pour les secrets Ops.

L'automatisation est désactivée par défaut dans `appsettings.json` et activée
explicitement dans `appsettings.Production.json`.

## Publication sans secret sur le PC

Le workflow GitHub Actions `Publish BeeKingdom News` s'exécute sur le runner
`[self-hosted, beekingdom-deploy]`, donc directement sur le serveur.

Il :

1. lit la clé locale `news-automation.key`;
2. construit le JSON bilingue depuis les inputs du workflow;
3. appelle l'endpoint d'automatisation;
4. exige un résultat `Published`;
5. relit ensuite l'article via l'endpoint public;
6. échoue si l'article n'est pas publiquement accessible.

Ainsi, aucun secret de publication ne transite par ChatGPT, le navigateur ou
le poste PCJEFF.

## Client local

`Server/tools/Publish-BeeKingdomNews.ps1` prépare les inputs en JSON et
déclenche `publish-news.yml` avec `gh workflow run --json`.

Le contenu multiligne FR/EN peut donc être envoyé sans interaction navigateur.

## Validation locale effectuée

- `dotnet test ... --filter FullyQualifiedName~NewsServiceTests` :
  **28/28 PASS**.
- `dotnet build BeeKingdom.Server.csproj -c Release` :
  **PASS**, 0 erreur.
- test HTTP réel contre serveur local :
  - sans clé : **401**;
  - mauvaise clé : **401**;
  - bonne clé : création + statut **Published**;
  - second POST même slug : mise à jour, aucun doublon;
  - `PublishedAtUtc` inchangé;
  - endpoint public retourne bien l'article mis à jour.

## Déploiement

Le travail a été construit dans un worktree isolé basé sur `origin/deploy`
afin de ne pas inclure les 12 commits locaux ni les fichiers non commités du
chantier Unity/serveur principal.

Le déploiement de cette branche ne doit contenir que les changements décrits
dans ce document.
