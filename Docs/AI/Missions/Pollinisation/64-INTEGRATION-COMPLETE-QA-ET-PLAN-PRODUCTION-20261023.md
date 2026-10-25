# P13 - Reconciliation Unity + API pollinisation (23 octobre 2026)

## Portee et provenance
- Base Unity : feature/pollinisation-alpha-20261021, commit dfd9ee8c (P12 floral et carte).
- Code serveur : release/pollination-srvesdt-20261022, commit beaeb20d (Academie deja publiee, P2/P3/P4).
- Cible isolee : integration/pollinisation-unified-20261023.
- Douze fichiers Server/ ont ete synchronises depuis la release; aucune modification dans Assets/.
- main officiel reste intact, avec 123 changements non commites preexistants. Ne pas merger directement sur son worktree sale.

## Preuves locales PCJEFF
- Build BeeKingdom.Server Release : PASS, 0 erreur, 5 avertissements.
- Tests HiveOperations : 327/327 PASS.
- Suite API ordinaire : 714 PASS, 9 ignores faute de SQL opt-in.
- Suite SQL API opt-in : 9/9 PASS sur instance LocalDB BKPollinationQA; base disposable creee et supprimee pour chaque test.
- Client Unity P5 (.NET avec vraies sources de contrat/presentation) : PASS.
- Validation Wave6 : P6_WAVE6_ANCHOR_QA_PASS=2, drapeaux OFF.
- Assets identiques a P12 : tests Unity P12 EditMode 11/11 et WorldMap 8/8 le 23 octobre sur worktree P12 (pas executes sur le nouveau worktree).
- Journaux : C:\BeeKingdom\DEV\BK-Studio\qa\pollinisation-unified-20261023.

## Etat reel SRVESDT
- IIS BeeKingdomApi et SQL Server SQLEXPRESS01 disponibles; API /health repond 200.
- API florale publiee le 22 octobre; routes floral-sites, civil-population et floral-projects repondent 503 (flags OFF).
- Pool IIS configure Persistence__Provider=SqlServer et une connexion SQL Runtime externe; appsettings.Production.json seul ne represente pas la configuration effective.
- L'agent Valoraxo Remote tourne sous NT AUTHORITY\SYSTEM, compte sans acces a la base BeeKingdom: SQL erreur 4060, HAS_DBACCESS=0. Cela ne prouve pas un echec d'acces sous l'identite IIS.
- Derniere sauvegarde SQL full repertoriee le 20 octobre 19:35:54, fichier present (~13,65 Mio). Aucune preuve d'une sauvegarde fraiche et restauree.
- Preflight SQL lecture seule sur SRVESDT : C:\BeeKingdom\staging\BK-Pollination-Sql-Preflight-20261023.ps1. Executer sous un compte Windows autorise SQL.
- Migrations SQL 096, 097, 098 dans les fichiers de release; application en production NON VERIFIEE.

## Etapes requises AVANT activation
1. Confirmer les migrations 096-098 sous une identite SQL autorisee, par lecture de dbo.SchemaVersion et des tables.
2. Realiser une nouvelle sauvegarde SQL complete COPY_ONLY CHECKSUM, verifier RESTORE VERIFYONLY et la procedure de retour arriere; preserver la configuration IIS.
3. Appliquer seulement les migrations manquantes via les operations securisees existantes, avec controle des locks.
4. Confirmer les identites officielles du monde et de la surface, puis configurer les sites floraux. Les fixtures world-wave6-qa et BK-DEMO-WORLD-WAVE6-LOCAL ne sont PAS de la geographie de production.
5. Activer sous controle avec des comptes de test, verifier expedition hors ligne, stock partage, rejouement idempotent et projet a deux membres distincts.
6. Integrer Unity dans le main officiel sans perdre les changements locaux d'Academie, Defense, fenetres premium, accents ni fichiers .meta.
7. Tester en GameView authentifiee FR/EN et sur deux vrais comptes avant de declarer le jeu livrable.

Ne pas annoncer la pollinisation comme jouable en production sur le seul resultat des tests locaux et de /health.