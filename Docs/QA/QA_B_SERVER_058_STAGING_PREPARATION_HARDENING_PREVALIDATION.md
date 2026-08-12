# QA-B - SERVER-058 Staging Preparation Hardening - Prevalidation

Date: 2026-07-13  
Role: QA-B, prevalidation independante  
Gate officiel: non ferme; QA-A conserve le verdict final  
Mode d'audit: strictement read-only

## 1. Verdict executif

La soumission n'est pas prete a etre transmise comme correction complete des trois P1 de SERVER-056.

Deux blocages subsistent:

1. `Install-BeeKingdomStagingRelease.ps1` ne controle pas les reparse points sur ses chemins d'ecriture derives (`releases`, chemin de release et `current-release.txt`). Une jonction ou un lien preexistant sous la racine staging peut donc contourner la garde lexicale. Les 13 tests negatifs livres ne couvrent qu'un reparse point du cote packager.
2. P1-03 n'est pas ferme par une preuve avant/apres. Le rapport et la preuve de diff reconnaissent qu'aucun snapshot complet anterieur a SERVER-056 n'existe pour IIS/DNS et qu'aucun diff complet anterieur n'existe pour firewall/TLS. Le rapport affirme pourtant en introduction que les trois P1 sont corriges, puis qualifie P1-03 de seulement partiellement corrige.

Aucun P0 ni aucune exposition publique actuelle de Bee Kingdom n'ont ete observes dans les preuves livrees. Le blocage porte sur le claim de hardening complet et sur la tracabilite historique, pas sur une declaration de MMO live.

## 2. Perimetre inspecte

Sources producteur:

- `C:\projets\beekingdom\prompt_server\rapports\SERVER-058 - Staging Preparation Hardening Report.md`
- les 10 fichiers de `C:\projets\beekingdom\prompt_server\rapports\SERVER-058-StagingPreparationHardening-Evidence`
- `C:\projets\beekingdom\QA\QA_B_SERVER_056_LIVE_SERVER_PREPARATION_WAVE1_PREVALIDATION.md`
- `C:\projets\beekingdomgame-master\Server\deploy\New-BeeKingdomStagingPackage.ps1`
- `C:\projets\beekingdomgame-master\Server\deploy\Install-BeeKingdomStagingRelease.ps1`
- package, sidecar SHA-256 et manifeste locaux de SERVER-056

Restrictions respectees par QA-B:

- aucun appel distant;
- aucun test reseau supplementaire;
- aucun redeploiement, restart ou stop;
- aucune modification IIS, firewall, DNS ou TLS;
- aucune ouverture de port;
- aucune migration ou ecriture SQL;
- aucune ecriture dans le staging distant;
- aucun fichier producteur modifie.

Les tests .NET et les cas PowerShell n'ont pas ete reexecutes, afin de conserver le mode strictement read-only. Leurs sorties ont ete relues et recoupees. Les controles independants effectues par QA-B sont des lectures, parses, scans et calculs de hash sans extraction.

## 3. Recontrole des trois P1 SERVER-056

| P1 | Constat SERVER-058 | Resultat QA-B |
|---|---|---|
| P1-01 - chemins, noms, traversal, reparse | Frontieres racine et noms corriges; validations placees avant les premieres ecritures. Couverture reparse incomplete dans l'installateur. | BLOQUE |
| P1-02 - integrite ZIP/manifeste | SHA-256 du ZIP verifie avant extraction; presence, taille et SHA-256 des fichiers verifies apres extraction. Concordance locale exacte et preuve distante actuelle 60/60. | CONFIRME, avec limite historique explicite |
| P1-03 - surfaces publiques | Snapshot actuel riche et read-only disponible, mais aucun etat complet `avant` ne permet le diff historique demande. | NON FERME |

### 3.1 P1-01 - garde stricte encore contournable

Points corriges et confirmes:

- comparaison racine exacte ou racine suivie d'un separateur dans les deux scripts;
- validation stricte de `PackageName` et `ReleaseName`;
- rejet des collisions de prefixe et des formes de traversal testees;
- validation des racines avant `New-Item`;
- rejet des reparse points sur `PublishPath`, `OutputPath`, `StagingRoot`, le ZIP et le manifeste;
- borne de port `1024..65535`;
- aucune commande `Remove-Item` dans les deux scripts;
- zero erreur de parse PowerShell.

Blocage residuel:

- `Install-BeeKingdomStagingRelease.ps1`, lignes 185-195, construit les chemins derives puis ne leur applique que `Assert-SameOrChildPath`, une verification lexicale;
- les ecritures surviennent ensuite aux lignes 209-218 et 232;
- `Assert-NoReparsePoint` n'est appelee qu'aux lignes 140, 141 et 169, sur la racine staging, le ZIP et le manifeste;
- une jonction preexistante sur `C:\BeeKingdom\staging\releases`, ou un lien sur `current-release.txt`, n'est donc pas inspecte avant l'ecriture;
- la preuve `local-negative-script-tests-redacted.json` ne contient aucun cas reparse pour l'installateur. Son unique cas reparse vise `OutputPath` du packager.

Impact: la promesse de confinement strict sous `C:\BeeKingdom\staging` n'est pas garantie pour une future execution de l'installateur.

Role correctif: Server-A.

Preuve expurgee attendue:

- appliquer la verification de reparse point a chaque chemin d'ecriture derive et a ses parents existants immediatement avant toute creation, extraction ou `Set-Content`;
- ajouter au minimum des tests negatifs sur une jonction `releases`, un chemin de release sous jonction et un `current-release.txt` symbolique;
- prouver que chaque cas est rejete avant toute ecriture hors racine, sans reproduire la cible du lien.

### 3.2 P1-02 - integrite confirmee

Audit statique de l'installateur:

- le SHA-256 attendu est valide comme hexadecimal de 64 caracteres;
- le SHA-256 du ZIP est calcule et compare avant `Expand-Archive`;
- les chemins de manifeste relatifs dangereux sont rejetes;
- chaque entree du manifeste est controlee apres extraction par presence, longueur et SHA-256;
- le mode `WhatIf` retourne avant les creations et ecritures.

Verification independante QA-B du package local, sans extraction:

- sidecar SHA-256 egal au hash recalcule du ZIP;
- nom de package du manifeste egal au nom de base du ZIP;
- 60 entrees de manifeste et 60 fichiers dans le ZIP;
- 0 entree manquante;
- 0 entree ZIP supplementaire;
- 0 mismatch de longueur;
- 0 mismatch SHA-256.

Preuve staging prive actuel:

- ZIP distant present;
- hash distant egal au hash attendu;
- release presente;
- 60 entrees de manifeste et 60 fichiers verifies;
- 0 mismatch;
- release courante coherente avec SERVER-056.

Reserve historique non bloquante pour P1-02: le script utilise lors de l'installation SERVER-056 ne faisait pas la verification avant extraction. Les preuves actuelles etablissent l'identite disponible maintenant, pas l'ordre historique des controles.

### 3.3 P1-03 - absence de baseline historique

Le snapshot SERVER-058 actuel contient:

- 10 sites IIS;
- 44 bindings IIS;
- 34 regles firewall pertinentes;
- 4 certificats locaux;
- 22 observations DNS;
- aucun host header Bee Kingdom observe;
- aucun nom DNS officiel Bee Kingdom fourni.

La preuve `remote-public-surface-diff-summary-redacted.json` indique explicitement:

- snapshot IIS anterieur disponible: false;
- snapshot DNS anterieur disponible: false;
- diff firewall anterieur complet disponible: false;
- diff TLS anterieur complet disponible: false;
- conclusion: l'absence exhaustive de modification historique ne peut pas etre prouvee.

Cette preuve est coherente avec la section P1-03 du rapport, mais contredit le claim introductif selon lequel les trois P1 seraient corriges.

Role correctif:

- Architecte/Infra: fournir une baseline anterieure autoritative si elle existe, ou accepter formellement l'impossibilite retrospective et declarer le snapshot SERVER-058 comme nouvelle baseline;
- Server-A: corriger le claim du rapport et, si une baseline autoritative est fournie, produire uniquement le diff expurge read-only correspondant;
- QA-A: decider si une derogation explicite suffit a fermer le gate historique.

## 4. Tests .NET

Source: `dotnet-test-release-redacted.txt`.

Resultats confirmes par parsing de la sortie:

- reussis: 142;
- echecs: 0;
- ignores: 6;
- total: 148;
- une seule ligne de synthese finale;
- six lignes d'ignore explicites, toutes associees aux exercices SQL opt-in ou de readiness schema SQL;
- les logs de test indiquent aussi que les background workers sont desactives.

Resultat: CONFIRME SUR PREUVE LIVREE. La suite n'a pas ete relancee par QA-B dans ce mandat read-only.

## 5. Treize tests negatifs PowerShell

Source: `local-negative-script-tests-redacted.json`.

Le fichier est coherent en interne:

- `CaseCount = 13`;
- `AllExpectedResultsMet = true`;
- collision de prefixe: rejetee sans creation du chemin;
- noms de package avec traversal, slash, backslash ou valeur vide: rejetes;
- racine staging en collision de prefixe: rejetee;
- noms de release avec traversal, forme absolue, slash ou valeur vide: rejetes;
- SHA-256 invalide: rejete;
- package verifie en `WhatIf`: accepte sans creation de release;
- reparse point du packager: rejete.

Resultat numerique: 13/13 comportements attendus documentes.

Limite bloquante: aucun cas ne place un reparse point dans un chemin d'ecriture derive de l'installateur. Le total 13/13 est exact, mais la couverture annoncee ne suffit pas a prouver la garde reparse complete.

## 6. Snapshots serveur read-only

Source principale: `remote-readonly-hardening-snapshot-redacted.json`.

| Controle | Preuve | Statut QA-B |
|---|---|---|
| Listener staging | `127.0.0.1:5089`, etat `Listen` | CONFIRME AU MOMENT DU SNAPSHOT |
| Processus | `dotnet.exe`, processus associe au listener | CONFIRME AU MOMENT DU SNAPSHOT |
| Workers | preuve ligne de commande: false; deux logs indiquent explicitement l'etat desactive | COMPORTEMENT CONFIRME, SOURCE CONFIG NON VISIBLE |
| IIS | 10 sites, 44 bindings | SNAPSHOT ACTUEL CONFIRME |
| Firewall | 34 regles pertinentes | SNAPSHOT ACTUEL CONFIRME |
| TLS | 4 certificats inventories | SNAPSHOT ACTUEL CONFIRME |
| DNS | 22 observations; aucun input DNS officiel Bee Kingdom | SNAPSHOT ACTUEL CONFIRME |
| Host header Bee Kingdom | absent | CONFIRME AU MOMENT DU SNAPSHOT |

Reserve P2: la desactivation des workers est prouvee par le comportement journalise, mais pas par la ligne de commande. La preuve `/health` publique a contourne la validation TLS pour cet audit read-only; elle prouve un HTTP 200 et la continuite du service existant, pas la validite de la chaine TLS par nom d'hote.

## 7. Exposition publique

Preuves relues:

- `remote-public-port-check-readonly-redacted.json`;
- `remote-public-health-readonly-redacted.json`;
- snapshot de surfaces publiques;
- synthese de diff.

Constats actuels:

- port public `5089`: ferme;
- listener staging: loopback uniquement;
- aucun host header Bee Kingdom observe;
- le service public existant repond toujours en HTTP 200 et s'identifie comme un service non Bee Kingdom;
- aucune preuve d'un publish Bee Kingdom public, d'un binding Bee Kingdom ou d'un DNS officiel Bee Kingdom.

Conclusion limitee: aucune exposition publique actuelle de Bee Kingdom n'est montree. L'affirmation plus forte selon laquelle aucune surface publique n'aurait ete modifiee pendant SERVER-056 ne peut pas etre etablie sans snapshot anterieur.

QA-B n'a effectue aucune nouvelle verification reseau.

## 8. Scan de secrets

Preuve producteur:

- scope declare: scripts et preuves SERVER-058;
- finding haute confiance: 0.

Scan independant QA-B en lecture seule:

- 15 fichiers controles: rapport SERVER-058, prevalidation SERVER-056, 10 preuves, 2 scripts et manifeste local;
- categories haute confiance: cle privee, jetons fournisseurs connus, JWT complet, identifiants integres a une URL, cle de compte et mot de passe de chaine de connexion;
- candidats haute confiance: 0;
- candidats generiques d'affectation de secret: 0.

Resultat: aucun secret a haute confiance observe. Aucune valeur potentiellement sensible n'est reproduite dans ce rapport.

## 9. Coherence rapport/preuves

| Preuve | Claim recoupe | Conclusion |
|---|---|---|
| `dotnet-test-release-redacted.txt` | 142 reussis, 0 echec, 6 ignores | COHERENT |
| `local-install-manifest-verification-redacted.json` | installation locale, DLL presente, 60 fichiers verifies | COHERENT |
| `local-negative-script-tests-redacted.json` | 13 comportements attendus | COHERENT, couverture reparse incomplete |
| `local-positive-packager-test-redacted.json` | package positif de 60 fichiers | COHERENT |
| `remote-public-health-readonly-redacted.json` | continuite HTTP 200 du service public non Bee Kingdom | COHERENT, TLS non valide par ce test |
| `remote-public-port-check-readonly-redacted.json` | port public 5089 ferme | COHERENT |
| `remote-public-surface-diff-summary-redacted.json` | snapshot actuel, absence de baseline historique | COHERENT AVEC LA LIMITE; CONTREDIT LE CLAIM DES TROIS P1 CORRIGES |
| `remote-readonly-hardening-snapshot-redacted.json` | listener, workers, integrite distante et surfaces actuelles | COHERENT |
| `script-parse-validation-redacted.json` | zero erreur de parse | COHERENT ET RECONTROLE |
| `secret-scan-redacted.json` | zero secret haute confiance | COHERENT ET ETENDU PAR QA-B |

## 10. Separation des niveaux de preuve et des claims

### Preuve locale

- scripts durcis dans le workspace;
- package, sidecar et manifeste locaux;
- tests .NET et tests PowerShell produits localement;
- verification QA-B du ZIP sans extraction.

### Staging prive reel prepare

- snapshot distant read-only d'une release existante sous la racine staging declaree;
- listener loopback sur `127.0.0.1:5089`;
- hash ZIP distant concordant;
- 60/60 fichiers de manifeste concordants;
- workers observes desactives dans deux logs;
- port public 5089 ferme.

Ces preuves etablissent un staging prive prepare au moment des snapshots. Elles ne donnent aucune autorite gameplay officielle.

### Service live officiel

Non etabli et non revendique:

- aucun DNS Bee Kingdom officiel fourni;
- aucun binding ou host header Bee Kingdom observe;
- aucune exposition publique Bee Kingdom;
- aucun claim de comptes, sessions, persistence, world map MMO ou autorite gameplay live;
- le service public observe est un service existant distinct.

QA-B ne declare pas Bee Kingdom live et ne ferme aucun gate officiel.

## 11. Risques et decision de resoumission

### P0

Aucun P0 observe dans les preuves actuelles.

### P1-A - contournement reparse de l'installateur

- fichier: `C:\projets\beekingdomgame-master\Server\deploy\Install-BeeKingdomStagingRelease.ps1`;
- preuve: appels reparse limites aux lignes 140, 141 et 169; chemins d'ecriture derives aux lignes 185-195; ecritures aux lignes 209-218 et 232; aucun test reparse installateur dans les 13 cas;
- role correctif: Server-A;
- condition de levee: patch local et tests negatifs expurges demontrant le rejet avant toute ecriture.

### P1-B - P1-03 non ferme et claim contradictoire

- fichier: `C:\projets\beekingdom\prompt_server\rapports\SERVER-058 - Staging Preparation Hardening Report.md`;
- preuve: introduction affirmant trois P1 corriges; section P1-03 disant `Partiellement corrige`; preuve de diff indiquant quatre baselines historiques absentes;
- role correctif: Architecte/Infra pour decision de baseline ou derogation; Server-A pour corriger le rapport et produire tout diff read-only rendu possible;
- condition de levee: baseline/diff autoritatif ou acceptation explicite de la limite retrospective par l'Architecte, puis rapport sans claim contradictoire.

## 12. Note de livraison

Le chemin demande `C:\projets\beekingdom\QA\QA_B_SERVER_058_STAGING_PREPARATION_HARDENING_PREVALIDATION.md` est hors du perimetre d'ecriture accorde a cette session. La copie QA-B a donc ete produite dans le workspace autorise, sans modifier le depot producteur hors de ce nouveau rapport.

QA_B_SERVER_058_PREVALIDATION = BLOCKED
