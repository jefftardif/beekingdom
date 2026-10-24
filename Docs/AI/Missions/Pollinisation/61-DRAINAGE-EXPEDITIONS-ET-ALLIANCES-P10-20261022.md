# 61 — P10 : arrêt contrôlé de la pollinisation sans essaims bloqués

**Date : 22 octobre 2026 — implantation isolée sur PCJEFF.** Ce lot est
technique : il protège la continuité économique en cas de désactivation
d'une fonctionnalité, de maintenance ou de rollback applicatif.

## Risque P0 identifié et corrigé

Avant P10, le flag CivilExpeditions:Enabled=false coupait **aussi** les
lectures/terminaisons des missions déjà démarrées et la boucle de settlement
dans CivilExpeditionSettlementWorker. Des ouvrières pouvaient rester
réservées indéfiniment malgré leur date de retour passée.

En outre, FloralAllianceProjects:Enabled=false empêchait le crédit des
projets même lorsque leur contribution avait été gagnée pendant une mission
lancée antérieurement. La récompense personnelle pouvait alors être
enregistrée, mais les points et l'XP d'alliance correspondants perdus.

## Nouveaux paramètres, tous OFF par défaut

- CivilExpeditions:Enabled — permet les départs civils, comme auparavant.
- CivilExpeditions:DrainExistingFlights — permet uniquement la lecture
  authentifiée et le règlement des missions civiles déjà persistées,
  y compris par le worker SQL, en bloquant TOUS les nouveaux départs.
- FloralAllianceProjects:Enabled — permet les nouveaux projets et les
  contributions collectives, comme auparavant.
- FloralAllianceProjects:DrainExistingDeliveries — permet uniquement
  l'attribution des livraisons de missions en vol aux projets existants,
  avec la même transaction SQL et les mêmes vérifications d'adhésion,
  alors que la création/consultation de nouveaux projets est désactivée.

Les paramètres sont indépendants et contrôlés exclusivement par le serveur.
Aucun changement de schéma SQL et aucune migration destructive.

## Procédure de drainage QA/staging (nécessite une validation opérationnelle)

Pour interrompre la création des nouvelles missions tout en laissant rentrer
les essaims déjà partis, configurer :

    CivilExpeditions:Enabled=false
    CivilExpeditions:DrainExistingFlights=true
    FloralAllianceProjects:Enabled=false
    FloralAllianceProjects:DrainExistingDeliveries=true

FloralSites:Enabled peut rester true ou être basculé false. Dans tous les cas,
conserver **l'intégralité** de FloralSites:CatalogVersion et FloralSites:Sites
aux mêmes identifiants stables. Les états floraux persistent dans la base SQL,
non dans Unity. Ne jamais supprimer ni renommer un site encore référencé par
une mission active.

Laisser le serveur et CivilExpeditionSettlementWorker s'exécuter. Les missions
déjà engagées sont terminées par horloge UTC et les verrous transactionnels
ruche + champ + contribution (+ projet si applicable).

Suivre l'état réel dans SQL avec une requête en lecture seule :

    SELECT COUNT_BIG(*) AS AllOutstandingFlights
    FROM dbo.FloralCivilActiveMissions;

La requête DueAsync() ne liste que les missions **échues** : son résultat
vide n'implique PAS que tous les essaims ont déjà terminé. La file complète
ci-dessus doit être vide avant de désactiver définitivement les flags de
drainage ou de retirer le catalogue. Pour les alliances, vérifier également
que tous les vols concernés ont été attribués une seule fois au journal
FloralAllianceProjectDeliveries lorsque la politique d'adhésion le permet.

Après constat d'absence de toute mission active et conservation des backups,
repasser les deux flags DrainExisting* à false; les nouvelles activités
restent inactives tant que leurs flags Enabled demeurent false.

ATTENTION : un projet collectif inachevé peut demeurer Active avec des
points historiques. Le devenir de tels projets, leur expiration et leur
compensation restent des décisions produit ouvertes ; NE PAS les supprimer
automatiquement dans un rollback.

## Gardes de sécurité

1. Nouveau départ refusé en mode DrainExistingFlights même si Enabled reste true.
2. GET /game/v1/hives/{hiveId}/civil-expeditions reste authentifié et limité
   à la ruche propriétaire si drainage autorisé ; le POST start renvoie 503.
3. Le stockage JSON local reste incapable d'effectuer des mutations communes
   ruche + champ, même en mode drain. Il renvoie 503, pas de récompense fictive.
4. Si le catalogue nécessaire est absent, le service échoue explicitement
   sans libérer ni créditer partiellement les civils.
5. Les missions existantes restent dédupliquées par MissionId; un redémarrage
   serveur ne peut ni recréditer le portefeuille ni appliquer deux fois l'XP.
6. Les règles d'adhésion aux projets restent celles de P4 : aucune
   contribution à une nouvelle alliance rejointe en cours de trajet.
7. Le worker P3 cherche les vols échus lorsque CanSettle=true et
   abandonne tout nouveau départ lorsque CanStart=false.

## Preuves déjà exécutées en QA

Tests métier CivilExpeditionServiceTests : missions désactivées, stock
floral public désactivé, absence de nouveaux départs, restitution des
ouvrières et crédit exact au retour, worker de due-process, refus de
catalogue manquant. Tests API CivilExpeditionEndpointTests : GET encore
protégé par auth+ownership, POST 503, JSON fail-closed.

SQL Server 2025 LocalDB sur PCJEFF (jamais SRVESDT) :
- Server/ops/pollination-sql-qa : départ autorisé avant coupure, désactivation
  CivilExpeditions et FloralSites puis fin d'expédition uniquement par
  CivilExpeditionDueProcessor; solde du champ, civils, ressources et ledger OK.
- Server/ops/pollination-alliance-qa : chef et second membre, projet en cours
  et mission engagée avant coupure. Les nouveaux départs/projets sont
  rejetés pendant drainage. La livraison du deuxième membre clôt le projet
  collectif et attribue son XP **une seule fois** dans SQL, bien que
  l'activité publique et la création de projet soient désactivées.

Les logs des deux scénarios SQL sont conservés dans le worktree sous
pollinisation-p10-sql-drain-real.log et
pollinisation-p10-collective-drain-real.log (fichiers ignorés par Git).
Les résultats de la suite globale restent à consigner une fois exécutés.

## Limites maintenues

- Ce test n'est pas une procédure de maintenance appliquée sur SRVESDT.
  Le vrai rollback production et les backups doivent être validés séparément.
- Le mode DrainExisting* est une option de configuration, pas un bouton Unity.
  Aucune information de panne ou de synchronisation interne n'est exposée
  dans l'interface de jeu.
- Des règles de compensation/alliance dissoute, des projets annulés, les
  technologies de déplacement multi-sites et le PvP des escortes restent
  au backlog. La carte et ses tuiles n'ont pas été modifiées.
- Les échecs intermittents de Unity Package Manager IPC en batchmode
  sur PCJEFF n'ont pas été résolus par ce lot serveur.
## Protection supplémentaire : perte d'une ligne de champ SQL

Le repository de transaction commun était auparavant autorisé à initialiser
un champ manquant depuis le catalogue, même lorsque la ruche portait encore
une mission active à destination de ce champ. Une perte de ligne SQL pouvait
ainsi recréer un stock plein et engendrer du pollen à tort.

Le code refuse maintenant explicitement un règlement si une mission en cours
référence un site dont la ligne dbo.FloralSiteStates a disparu. Il ne crée
ni nouveau stock, ni récompense, ni contribution, et conserve la mission
dans dbo.FloralCivilActiveMissions. La reprise nécessite la restauration
de la **ligne SQL originale** depuis une sauvegarde vérifiée.

Preuve locale : la sonde Server/ops/pollination-sql-qa supprime temporairement
la ligne **synthétique QA** d'un site pendant qu'un essaim y est engagé.
Le processeur retourne cette mission dans RetryMissionIds; les ouvrières
restent réservées et les crédits inchangés. La sonde réinsère ensuite la
copie exacte JSON+Revision de la ligne, puis le règlement aboutit une
seule fois et la file des vols est vidée.

Cette expérience QA **ne justifie pas** de supprimer/recréer les tables
d'un serveur réel, et ne remplace pas un plan de restauration production.## Bilan final de non-régression P10 — 2026-10-22

- Suite HiveOperations complète : **320 / 320 réussis**, 0 échec, 0 ignoré.
- Suite API + SQL LocalDB complète (variable opt-in configurée sur l'instance QA dédiée) : **718 / 718 réussis**, 0 échec, 0 ignoré ; résultat TRX P10-Drain-FullApi.trx dans le répertoire local TestResults/pollination-p10.
- Total des deux suites : **1 038 tests réussis**, 0 échec.
- Scénarios SQL réels : Server/ops/pollination-sql-qa (drain + perte du champ + restauration de sa ligne SQL originale) et Server/ops/pollination-alliance-qa (drain + progression collective) : **PASS**. Aucun compte joueur réel ni donnée SRVESDT touché.
- Des avertissements C# préexistants dans d'autres modules subsistent ; aucune erreur de compilation relevée lors des tests réussis.