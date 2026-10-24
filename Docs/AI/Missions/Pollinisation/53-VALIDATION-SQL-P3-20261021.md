# 53 — Validation P3 de pollinisation sur PCJEFF (2026-10-21)

**Branche :** `feature/pollinisation-alpha-20261021` (hors du `main` de Unity).
**Version validée du lot P3 :** commit `be7c3ff193b0f5a9c65ea52eea2277904ebd2726` et correctif de test SQL subséquent.
**Déploiement :** aucun sur SRVESDT. Trois flags `CivilPopulation`, `FloralSites`, `CivilExpeditions` OFF par défaut.

## Preuves exécutées

| Niveau | Commande/suite | Résultat |
|---|---|---|
| Compilation .NET | `BeeKingdom.Server.csproj` | Succès, 0 erreur; avertissements nullabilité préexistants |
| Module HiveOperations | `BeeKingdom.HiveOperations.Tests` (tous) | 308 réussis / 308 |
| API complète | `BeeKingdom.Tests`, passage détaillé avec TRX | 703 réussis, 9 ignorés, 0 échec / 712 |
| SQL optionnel | `SqlServerOptInIntegrationTests`, connexion LocalDB explicite | 9 réussis / 9 |
| Simulation SQL réelle P3 | `Server/ops/pollination-sql-qa` | Succès, code retour 0, deux scénarios réels (2 joueurs + 2 sites) |

**Total distinct vérifié : 1020 tests réussis.** Les 9 tests qui sont ignorés par défaut dans la suite API ont été exécutés séparément avec succès contre des bases LocalDB jetables.

### Incidents rencontrés et corrigés

1. Le test SQL P3 a trouvé que `READPAST` dans le scan des missions arrivées à terme était incompatible avec le niveau d'isolation conservé par la connexion SQL. Le scan pose désormais explicitement `READ COMMITTED` et ne dépend pas de `READPAST`. Le test de règlement SQL réel a ensuite réussi.
2. Lors de la première suite API complète, 1 test sur 712 a échoué sans précision dans le journal peu verbeux (702 réussis, 9 ignorés). Une réexécution complète avec journal détaillé et TRX a réussi (703 réussis, 9 ignorés). Ce premier échec est **non reproduit**, sans preuve d'une cause déterministe ni déclaration d'une correction fictive.
3. L'activation des neuf tests SQL normalement ignorés a fait échouer `SqlHiveStateRepositoryRoundTripsOfflineProductionReceiptAndReplay` : la réponse initiale emploie un tableau de lignes, sa version JSON relue emploie une `List`. L'égalité profonde par référence des propriétés de collection est impropre. Le test vérifie désormais le reçu immuable et l'équivalence du contenu JSON du snapshot; les neuf tests SQL ont alors réussi. **Aucune modification des règles de production hors pollinisation n'a été nécessaire.**

## SQL LocalDB QA, exclusivement sur PCJEFF

- Instance `BKPollinationQA`, base indépendante `BeeKingdomPollinationQA`, tables migrées par scripts 070/096/097.
- Le test de transaction concurrente prouve que deux ruches demandant chacune 12 unités sur un champ de 15 n'en extraient que 15, et que les deux contributions sont attribuées à deux identités distinctes.
- Une collision forcée de la contrainte unique du ledger fait échouer l'opération, mais ne modifie ni solde, ni réserve du champ, ni disponibilité des abeilles. La reprise est possible, sans double crédit.
- Le processeur SQL de missions dues règle une mission sur un second champ sans que le client Unity ne fasse de GET, puis vide l'index.
- Les GUID et mondes temporaires du test SQL ont été nettoyés. Les bases jetables du lot de neuf tests d'intégration ont été supprimées par leur teardown; la base indépendante `BeeKingdomPollinationQA` reste disponible pour QA.
- Les logfiles conservés localement hors Git sont `pollinisation-p3-sql-real.log`, `pollinisation-p3-full-hive.log`, `pollinisation-p3-full-api-detailed.log`, `pollinisation-p3-sql-optin.log`. TRX sous `Server/tests/BeeKingdom.Tests/TestResults/pollination-20261021/`.

## Limitations explicites

Ces validations **ne prouvent pas** un client Unity connecté, un site floral positionné sur la carte officielle, une mission à deux champs, un projet d'alliance collectif, le niveau d'alliance persisté, des contrats de diplomatie, des escortes ni une migration sur la vraie base SRVESDT. Les seuils/gains/cycles et les pertes civiles restent provisoires et sous feature flag OFF.

Avant activation, procéder à des sauvegardes et smoke-tests SQL distant, tests de charge/concurrence réseau, tests Unity FR/EN, retraitement après vrai redémarrage du service et vérification de rollback. Le `main` local avec travaux non committés d'autres chantiers ne doit pas être écrasé.
