# QA-B - SERVER-060 Staging Reparse And Baseline - Prevalidation

Date: 2026-07-13  
Role: QA-B, prevalidation independante pour QA-A  
Mode: local et strictement read-only  
Gate officiel: non ferme par QA-B

## 1. Verdict executif

SERVER-060 leve les deux blocages P1 identifies par la prevalidation QA-B de SERVER-058:

1. L'installateur protege maintenant les chemins d'ecriture derives et leurs parents existants avant chaque operation mutante.
2. L'Architecte accepte explicitement l'impossibilite de preuve historique retrospective; le snapshot SERVER-058 devient la baseline T0 pour les comparaisons futures uniquement.

Les preuves locales confirment egalement:

- 8 scenarios executables de reparse/carry-forward conformes, en comptant le fallback exact-path a la place du symlink fichier natif indisponible;
- rejet avant ecriture et zero fichier cree sur les cibles externes controlees;
- aucune cible de lien divulguee;
- zero erreur de parse PowerShell;
- suite .NET: 142 reussis, 0 echec, 6 SQL opt-in ignores;
- zero secret haute confiance detecte;
- aucune operation distante ou sur une surface publique dans le perimetre SERVER-060.

Deux reserves P2 sont conservees: le test symlink fichier natif reste souhaitable sur un hote disposant du privilege requis, et le libelle `8 cas tous conformes` doit etre lu comme 7 cas executes du fichier principal plus le fallback exact-path execute dans la preuve complementaire.

Aucun P0 ou P1 residuel n'est observe dans le perimetre demande. QA-B recommande la transmission a QA-A, sans claim live et sans autorisation operationnelle.

## 2. Sources inspectees

- `C:\projets\beekingdom\prompt_server\rapports\SERVER-060 - Staging Reparse And Baseline Correction Report.md`
- les 6 fichiers de `C:\projets\beekingdom\prompt_server\rapports\SERVER-060-StagingReparseBaseline-Evidence`
- `C:\projets\beekingdom\QA\QA_B_SERVER_058_STAGING_PREPARATION_HARDENING_PREVALIDATION.md`
- `C:\projets\beekingdom\prompts_codex\rapports\Architect_Server058HistoricalBaselineDecision.md`
- `C:\projets\beekingdom\prompt_server\rapports\SERVER-058 - Staging Preparation Hardening Report.md`, version corrigee SERVER-060
- `C:\projets\beekingdomgame-master\Server\deploy\Install-BeeKingdomStagingRelease.ps1`
- `C:\projets\beekingdomgame-master\Server\deploy\New-BeeKingdomStagingPackage.ps1` pour le parse et le carry-forward
- manifeste local SERVER-056 pour le controle des cibles d'extraction.

## 3. Restrictions respectees par QA-B

QA-B n'a effectue:

- aucun appel distant;
- aucun appel reseau;
- aucun redeploiement, restart ou stop;
- aucune commande IIS;
- aucune commande firewall;
- aucune operation DNS;
- aucune operation TLS/certificat;
- aucune ouverture ou verification active de port;
- aucune migration ou ecriture SQL;
- aucune ecriture dans le staging;
- aucune reexecution des tests produisant des fichiers locaux.

Les controles QA-B ont ete limites a la lecture, au parsing, au calcul de hashes et a l'analyse statique.

## 4. Matrice des criteres

| Critere | Resultat QA-B | Statut |
|---|---|---|
| Chemins derives et parents existants proteges juste avant ecriture | appels confirmes avant chaque mutateur | PASS |
| `releases`, release finale, extraction/manifeste | controles avant creation et extraction, puis apres extraction | PASS |
| `current`, `logs`, `backups`, `config` | meme boucle de garde avant et apres chaque `New-Item` | PASS |
| `current-release.txt` | garde avant lecture et immediatement avant `Set-Content` | PASS |
| 8 scenarios reparse/carry-forward | 7 principaux executes + 1 fallback exact-path execute | PASS AVEC RESERVE DE LIBELLE |
| Symlink fichier natif | tentative honnete, indisponible sans privilege; fallback techniquement representatif du garde generique | PASS AVEC RESERVE P2 |
| Parse PowerShell | 0 erreur sur les deux scripts | PASS |
| Suite .NET | 142/0/6, total 148 | PASS SUR PREUVE |
| SERVER-058 corrige | ancien claim actif retire; P1-02 et P1-03 correctement limites | PASS |
| Secrets et cibles de liens | 0 finding | PASS |
| Surfaces publiques | aucune action dans SERVER-060; baseline T0 inchangee | PASS SUR PERIMETRE LOCAL |
| Separation local/prive/live | distinction explicite et aucune autorisation live | PASS |

## 5. Audit statique des ecritures

### 5.1 Garde commune

`Assert-WritePathSafe`, lignes 78-87, combine:

- `Assert-SameOrChildPath`, comparaison racine exacte ou racine suivie d'un separateur;
- `Assert-NoReparsePoint`, qui part du chemin cible et remonte tous les parents existants jusqu'a la racine du volume;
- rejet sur l'attribut generique `FileAttributes.ReparsePoint`.

Pour un chemin cible encore inexistant, la boucle saute le composant absent puis inspecte ses parents existants. Une jonction ou un lien place sur un parent derive est donc detecte avant la creation du descendant.

### 5.2 Inventaire exhaustif des mutateurs

Le parsing AST identifie exactement quatre appels mutateurs dans l'installateur:

| Mutateur | Ligne | Cible |
|---|---:|---|
| `New-Item` | 241 | dossiers staging derives |
| `New-Item` | 251 | release finale |
| `Expand-Archive` | 254 | release et cibles du manifeste |
| `Set-Content` | 271 | `current-release.txt` |

Aucun `Remove-Item`, `Move-Item` ou `Copy-Item` n'est present.

### 5.3 Dossiers staging derives

Chemins construits aux lignes 215-221:

- `releases`;
- `current`;
- `logs`;
- `backups`;
- `config`;
- release finale;
- `current-release.txt`.

Tous subissent une prevalidation globale aux lignes 223-225, avant le mode `WhatIf` et avant toute ecriture.

Pour `releases`, `current`, `logs`, `backups` et `config`:

- garde immediate ligne 240;
- `New-Item` ligne 241;
- nouvelle garde ligne 242.

Le meme bloc de code couvre les cinq dossiers. La couverture ne repose donc pas sur cinq implementations divergentes.

### 5.4 Release finale

- garde avant le test d'existence: ligne 245;
- garde immediate avant creation: ligne 250;
- creation: ligne 251;
- garde apres creation: ligne 252.

Une jonction sur `releases`, une release finale elle-meme reparse ou un parent reparse est rejetee par le meme parcours.

### 5.5 Extraction et manifeste

`Assert-ManifestExtractionTargetsSafe`, lignes 147-163:

- protege la racine d'extraction;
- rejette les chemins absolus, `..` et backslashes dans les entrees du manifeste;
- construit chaque cible d'extraction;
- applique la garde racine et reparse a chaque cible et a ses parents existants.

Ordre d'execution:

- verification de la release creee: ligne 252;
- verification racine et des cibles manifeste: ligne 253;
- `Expand-Archive`: ligne 254;
- nouvelle garde de la release: ligne 255;
- verification manifeste post-extraction: ligne 256.

`Test-ManifestMatchesDirectory`, lignes 116-145, recontrole chaque fichier extrait par chemin, absence de reparse, presence, longueur et SHA-256.

Le manifeste courant contient 60 chemins uniques, 0 chemin vide et 0 forme dangereuse selon la meme expression de validation.

Reserve de portee: le hardening est lie au ZIP dont le SHA-256 attendu est approuve et au manifeste fourni. SERVER-060 n'ajoute pas de signature de package et cette prevalidation ne lui en attribue pas une.

### 5.6 `current-release.txt`

- garde avant lecture de l'ancienne valeur: ligne 263;
- lecture eventuelle: lignes 264-268;
- garde immediate avant ecriture: ligne 270;
- `Set-Content`: ligne 271.

Le reparse fallback place exactement au chemin `current-release.txt` est rejete pendant la prevalidation globale, avant toute creation de dossier ou ecriture.

Resultat de l'audit statique: toutes les cibles d'ecriture explicites de l'installateur sont couvertes, ainsi que leurs parents existants.

## 6. Huit scenarios reparse et carry-forward

### 6.1 Lecture exacte des preuves

`local-installer-reparse-negative-tests-redacted.json` contient huit lignes, mais l'une d'elles est une tentative de symlink fichier dont le setup a echoue faute de privilege. Les sept autres ont produit le resultat attendu.

`local-current-release-reparse-tests-redacted.json` ajoute le fallback executable au meme chemin. En remplacant le cas indisponible par ce fallback, huit scenarios executables et conformes sont disponibles:

| # | Scenario executable | Resultat | Preuve avant ecriture |
|---:|---|---|---|
| 1 | jonction sur `releases` | rejete | 0 fichier externe, pas de DLL, pas de pointeur courant |
| 2 | release finale sous jonction | rejetee | 0 fichier externe, pas de DLL, pas de pointeur courant |
| 3 | reparse sur `logs` | rejete | 0 fichier externe, pas de DLL, pas de pointeur courant |
| 4 | reparse exact sur `current-release.txt` | rejete | 0 fichier externe, pas de DLL |
| 5 | installation locale normale | acceptee | DLL presente, manifeste verifie, pointeur local attendu |
| 6 | collision de prefixe packager | rejetee | garde anterieure aux ecritures |
| 7 | collision de prefixe staging | rejetee | garde anterieure aux ecritures |
| 8 | SHA-256 invalide | rejete | refus avant creation staging |

Le cas 5 est un controle positif, pas un test negatif. L'expression correcte est donc `8 scenarios de couverture executables`, dont 7 rejets et 1 installation positive.

### 6.2 Expurgation

- les erreurs montrent uniquement le chemin reparse local expurge;
- les cibles des jonctions ou liens ne sont pas reproduites;
- les probes indiquent seulement des comptes, booleens ou valeurs locales synthetiques;
- scan independant des six preuves: 0 chemin cible potentiellement divulgue.

## 7. Evaluation du symlink fichier natif

La limite est honnete:

- la creation du symlink fichier exact a ete tentee;
- Windows l'a refusee pour absence de privilege local;
- le rapport ne transforme pas cette tentative en test passe;
- la preuve conserve `unavailable-no-local-symlink-privilege`.

Le fallback place un objet reparse exactement a `current-release.txt`. Il est techniquement suffisant pour valider les points suivants:

- le chemin exact est inclus dans la prevalidation;
- `Assert-NoReparsePoint` voit l'attribut `FileAttributes.ReparsePoint`;
- le rejet se produit avant `Set-Content` et, dans ce cas, avant toute autre ecriture;
- la cible externe reste inchangee et expurgee.

Le garde ne depend pas du type metier fichier/dossier: il rejette l'attribut reparse commun avant d'atteindre `Set-Content`. Pour un symlink fichier valide, le chemin de decision attendu est le meme.

Reserve P2: le fallback n'exerce pas la creation et le comportement natif d'un symlink fichier Windows. Un test defense-in-depth reste recommande sur un hote jetable avec Developer Mode ou privilege de creation de symlinks, sans utiliser le staging reel.

Cette limite ne bloque pas la prevalidation du garde generique et de son ordre d'execution.

## 8. Parse PowerShell

Preuve producteur:

- `ScriptParseErrorCount = 0`.

Recontrole QA-B avec le parser PowerShell local:

- `New-BeeKingdomStagingPackage.ps1`: 0 erreur;
- `Install-BeeKingdomStagingRelease.ps1`: 0 erreur;
- total: 0 erreur.

## 9. Suite .NET

Source: `dotnet-test-release-redacted.txt`.

Parsing de la sortie:

- lignes de synthese finale: 1;
- reussis: 142;
- echecs: 0;
- ignores: 6;
- total: 148;
- six lignes d'ignore nommees;
- les six skips concernent les exercices SQL opt-in ou readiness schema SQL;
- aucun resume d'echec non nul.

La suite n'a pas ete relancee par QA-B afin de respecter le mandat read-only.

## 10. Correction documentaire SERVER-058

### 10.1 Ancien claim

La phrase active `SERVER-058 corrige les trois P1` n'est plus presente. La mention des trois P1 restante apparait uniquement dans une note de correction qui identifie et remplace l'ancien claim, puis dans la formulation neutre `traitait les trois P1`.

### 10.2 P1-02

Le rapport corrige conserve exactement la limite requise:

- integrite actuelle du ZIP et des fichiers deployes confirmee;
- script futur durci;
- aucune pretention que le hash avait ete verifie avant extraction lors de l'installation historique SERVER-056.

### 10.3 P1-03 et baseline T0

La decision Architecte indique:

- l'absence de baseline complete pre-SERVER-056 est une limitation retrospective permanente;
- cette limitation ne doit pas etre presentee comme preuve d'absence de changement historique;
- P1-03 est ferme uniquement par waiver architectural explicite;
- le snapshot SERVER-058 devient T0 pour les comparaisons futures seulement;
- aucune operation ou exposition publique n'est autorisee par ce waiver.

Le marqueur source exact est present:

`ARCHITECT_SERVER058_T0_BASELINE_ACCEPTED = YES`

Le hash T0 consigne dans la preuve SERVER-060 correspond exactement au fichier `remote-readonly-hardening-snapshot-redacted.json` de SERVER-058.

Le rapport SERVER-058 corrige reprend ces distinctions et ne transforme pas le waiver en preuve retrospective.

Reserve editoriale P2: la section de conformite historique conserve `P1-01 corrige et teste` et un ancien travail restant demandant une decision Architecte. La note SERVER-060 en tete et la section P1-01 donnent le bon contexte actuel, mais ces deux lignes pourraient etre harmonisees lors d'une future passe documentaire.

## 11. Secrets et cibles de liens

Preuve producteur:

- `SecretOrReparseTargetDisclosureCount = 0`.

Scan independant QA-B:

- 11 fichiers controles: rapports SERVER-060/SERVER-058, decision Architecte, prevalidation precedente, 6 preuves et installateur;
- categories haute confiance: cles privees, jetons fournisseurs connus, JWT complets, identifiants dans URL, cles de compte et mots de passe de chaines de connexion;
- findings haute confiance: 0;
- affectations generiques suspectes: 0;
- chemins cibles de liens potentiellement divulgues dans les preuves: 0.

Aucune valeur sensible ou cible de lien n'est reproduite dans ce rapport.

## 12. Surfaces publiques et baseline

`local-only-public-surface-safety-redacted.json` declare:

- commande distante: false;
- test reseau distant: false;
- port public ouvert: false;
- IIS modifie: false;
- firewall modifie: false;
- DNS modifie: false;
- TLS modifie: false;
- migration SQL: false;
- redeploiement/restart/stop staging: false.

Le script modifie ne contient aucune commande IIS, firewall, DNS, TLS, reseau ou SQL. Le paquet de preuves SERVER-060 ne contient aucun nouveau snapshot distant et explique correctement pourquoi: le mandat l'interdisait.

Conclusion limitee: aucune action sur une surface publique n'appartient au perimetre SERVER-060 livre. QA-B n'a pas tente de reverifier l'etat distant. Le snapshot SERVER-058 reste la baseline T0 immuable de reference, dont le hash a ete recalcule et confirme.

## 13. Separation des niveaux de preuve

### Hardening local SERVER-060

Etabli:

- patch du script dans le workspace local;
- tests reparse/carry-forward locaux;
- installation positive locale synthetique;
- parse local;
- suite .NET locale;
- correction des claims et adoption de la decision Architecte.

### Staging prive existant

Non modifie et non reprobe par SERVER-060. Son existence et son etat prive restent ceux du snapshot read-only SERVER-058. SERVER-060 n'est pas une preuve de nouveau deploiement ni de nouvel etat runtime distant.

### Service live officiel

Non etabli, non autorise et non revendique:

- aucun deploiement public;
- aucune ouverture de port;
- aucun DNS ou certificat Bee Kingdom;
- aucune autorite gameplay live;
- aucune migration SQL distante;
- aucune fermeture de gate live.

## 14. Risques residuels et recommandation

### P0

Aucun observe.

### P1

Aucun observe dans le perimetre demande.

### P2 non bloquants

1. Executer plus tard le cas symlink fichier natif sur un hote local jetable disposant du privilege necessaire.
2. Normaliser le paquet de preuve pour presenter directement `7 cas principaux executes + 1 fallback`, plutot que `8 cas` dont un setup echoue.
3. Harmoniser les deux lignes residuelles du rapport SERVER-058 mentionnees en section 10.
4. Pour toute future vague publique, capturer T0 avant action et comparer au snapshot de reference selon la decision Architecte.

QA-A conserve seul le verdict officiel et la fermeture des gates.

## 15. Note de livraison

Le chemin demande `C:\projets\beekingdom\QA\QA_B_SERVER_060_STAGING_REPARSE_BASELINE_PREVALIDATION.md` est hors du perimetre d'ecriture accorde a cette session. La copie QA-B est donc produite dans le workspace autorise, sans modification des sources SERVER-060.

QA_B_SERVER_060_PREVALIDATION = READY_FOR_QA_A
