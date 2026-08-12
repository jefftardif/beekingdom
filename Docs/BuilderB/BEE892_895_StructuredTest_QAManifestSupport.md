# Builder-B - BEE-892 a BEE-895 Structured Test Output et QA Manifest Support

Statut : support non-runtime  
Date : 2026-07-12  
Portee : Ruche jouable uniquement, tests structures et manifest QA  
Contexte : ARCH-203 valide Planner BEE-882 a BEE-900  
Integration : support Demo/QA/Builder-C, sans modification runtime Builder-A  

Ce document prepare le support Builder-B pour BEE-892 a BEE-895. Il ne modifie pas le runtime principal, la scene, les assets, le serveur, la carte monde ou l'APK. BEE-881 reste bloquee.

## Sources lues

- `C:/projets/beekingdomgame-master/Docs/Architecture/ARCH-203_Planner882_900_ValidationAndParallelDispatch.md`
- `C:/projets/beekingdom/QA/QA_DEMO_071_BEE861_880_VALIDATION.md`
- `C:/projets/beekingdom/prompts_codex/BEE-892_Unity_Structured_Test_Output_Restoration_Framework.md`
- `C:/projets/beekingdom/prompts_codex/BEE-893_Rapid_Tap_Anti_Regression_Test_Matrix_Framework.md`
- `C:/projets/beekingdom/prompts_codex/BEE-894_QA_Artifact_Manifest_Schema_Framework.md`
- `C:/projets/beekingdom/prompts_codex/BEE-895_Playable_Hive_Regression_Suite_Gate_Framework.md`

## Probleme a fermer

QA-071 accepte DEMO-071 avec reserves, mais signale que Unity `-runTests` compile sans produire le XML NUnit attendu. Les methodes batch dediees sont acceptees temporairement, mais ne doivent pas devenir le standard.

Objectif Builder-B :

- specifier une sortie XML ou equivalente machine-readable ;
- cadrer la matrice rapid tap anti-regression ;
- proposer un schema de manifest QA stable pour Demo/QA ;
- definir un gate regression suite ruche jouable ;
- garder les non-claims : pas de carte monde, pas BEE-881, pas serveur live, pas save/economie/armee officielle.

## 1. Support isole pour sortie structuree

Priorite recommandee :

1. Restaurer NUnit XML natif Unity si possible.
2. Si Unity ne produit toujours pas de XML, produire un equivalent JSON machine-readable a partir du batch runner.
3. Joindre les deux au manifest QA si disponibles.

### Cible NUnit XML minimale

Chemin recommande :

```text
C:/projets/beekingdomgame-master/Logs/demo072-bee882-895-tests.xml
```

Champs minimum attendus dans le XML :

```xml
<test-run testcasecount="N" result="Passed|Failed">
  <test-suite name="PlayableHiveRegressionSuite" result="Passed|Failed" duration="0.000">
    <test-case name="RapidTapUpgradeCostOnce" result="Passed" duration="0.000">
      <properties>
        <property name="bee" value="BEE-893" />
        <property name="scope" value="playable_hive_only" />
        <property name="non_claims" value="no_world_map,no_live_server,no_official_save,no_official_economy,no_persistent_army,bee_881_blocked" />
        <property name="artifact" value="Logs/demo072-bee882-895-structured-report.json" />
      </properties>
    </test-case>
  </test-suite>
</test-run>
```

### Fallback JSON equivalent

Si NUnit XML reste indisponible, le rapport JSON doit etre considere comme equivalent uniquement s'il est stable, parseable et cite par le manifest QA.

Chemin recommande :

```text
C:/projets/beekingdomgame-master/Logs/demo072-bee882-895-structured-report.json
```

Schema minimal :

```json
{
  "schema": "bee-kingdom.playable-hive.structured-test-report.v1",
  "runId": "DEMO-072_BEE882_895",
  "generatedAtUtc": "2026-07-12T00:00:00Z",
  "scope": {
    "runtimeBeeRange": "BEE-882/BEE-891 if implemented by Builder-A/Server-A",
    "supportBeeRange": "BEE-892/BEE-895",
    "playableHiveOnly": true,
    "worldMapScopeAllowed": false,
    "bee881Blocked": true
  },
  "summary": {
    "total": 0,
    "passed": 0,
    "failed": 0,
    "skipped": 0,
    "durationMs": 0
  },
  "tests": [
    {
      "id": "BEE893_RapidTapUpgradeCostOnce",
      "bee": "BEE-893",
      "name": "Rapid tap upgrade applies cost once",
      "status": "passed",
      "durationMs": 0,
      "assertions": [
        {"name": "upgrade_commit_count_after_double_input", "expected": 1, "actual": 1, "passed": true},
        {"name": "upgrade_cost_applied_once", "expected": true, "actual": true, "passed": true}
      ],
      "artifacts": [
        "Logs/demo072-bee882-895-tests.log"
      ],
      "nonClaims": {
        "officialServerLive": false,
        "officialEndpoint": false,
        "officialSave": false,
        "officialEconomy": false,
        "officialPersistentArmy": false,
        "worldMapRuntime": false,
        "bee881Implemented": false
      }
    }
  ]
}
```

Critere QA : un batch log seul ne suffit plus si aucun XML/JSON equivalent n'est joint.

## 2. Matrice rapid tap anti-regression

| ID | Domaine | Scenario | Entree | Assertions PASS | Refus QA |
| --- | --- | --- | --- | --- | --- |
| RT-UG-01 | Upgrade | Double tap `Ameliorer` disponible | 2 taps en moins de 120 ms | `upgrade_commit_count_after_double_input = 1`, cout une fois, timer unique | double cout, double timer, niveau +2 |
| RT-UG-02 | Upgrade running | Tap `Ameliorer` pendant timer | tap pendant `pending/running` | repeat bloque, raison lisible, aucune mutation | bouton muet, second timer |
| RT-UG-03 | Upgrade refuse | Double tap avec ressources insuffisantes | 2 taps disabled/refused | 0 commit, cout 0, raison lisible | cout retire malgre refus |
| RT-UG-04 | Upgrade stale/snapshot | Tap sur etat conflit/stale | tap action | action refusee ou reconciliation future claire | claim save officielle ou erreur brute |
| RT-TR-01 | Training | Double tap `Entrainer` disponible | 2 taps rapides | `training_commit_count_after_double_input = 1`, queue +1, cout une fois | double queue, double cout |
| RT-TR-02 | Training busy | Tap quand queue occupee | tap sur training busy | raison `File occupee` ou equivalent, pas de nouveau lot | queue dupliquee |
| RT-TR-03 | Training full | Tap quand capacite atteinte | tap sur action bloquee | 0 commit, raison `Capacite atteinte` | bouton muet |
| RT-TR-04 | Training refuse | Ressources insuffisantes | 2 taps | 0 commit, cout 0, raison lisible | ressource negative |
| RT-RS-01 | Resource spend | Upgrade + training proches | actions successives | chaque cout applique une fois, ressources non negatives | cout croise double |
| RT-QA-01 | QA non-claims | Rapid taps + manifest | verification manifest | no world map/live/save/economy/army true as false claims | claim officiel visible |

### Champs structurels par test rapid tap

```json
{
  "testId": "RT-UG-01",
  "input": {
    "tapCount": 2,
    "tapWindowMs": 120,
    "target": "upgrade_button"
  },
  "before": {
    "honey": 1000,
    "wax": 100,
    "buildingLevel": 4,
    "queueCount": 0
  },
  "after": {
    "honey": 900,
    "wax": 90,
    "buildingLevelDelta": 1,
    "upgradeCommitCount": 1,
    "repeatBlockedCount": 1
  },
  "expected": {
    "costAppliedOnce": true,
    "timerCreatedOnce": true,
    "feedbackVisible": true
  }
}
```

## 3. Schema de manifest QA Demo/QA

Chemin recommande :

```text
C:/projets/beekingdom/prompt_demo/rapports/DEMO-072_BEE882_900/DEMO-072_QAArtifactManifest.json
```

Schema propose :

```json
{
  "schema": "bee-kingdom.qa-artifact-manifest.v1",
  "demoId": "DEMO-072",
  "qaGate": "QA-072",
  "date": "2026-07-12",
  "runtimeScope": {
    "beeRange": "BEE-882 to BEE-891",
    "notes": "Only if implemented by authorized owners"
  },
  "supportScope": {
    "beeRange": "BEE-892 to BEE-895",
    "supportOnly": true
  },
  "blockedScope": {
    "bee881Blocked": true,
    "worldMapBlocked": true
  },
  "artifacts": {
    "captures": [
      {"id": "before_action", "path": "BEE895_01_BeforeAction.png", "required": true},
      {"id": "rapid_tap_upgrade", "path": "BEE895_02_RapidTapUpgrade.png", "required": true},
      {"id": "rapid_tap_training", "path": "BEE895_03_RapidTapTraining.png", "required": true}
    ],
    "logs": [
      {"id": "unity_compile", "path": "Logs/demo072-final-compile.log", "required": true},
      {"id": "unity_tests", "path": "Logs/demo072-bee882-895-tests.log", "required": true}
    ],
    "structuredReports": [
      {"id": "nunit_xml", "path": "Logs/demo072-bee882-895-tests.xml", "required": false},
      {"id": "structured_json", "path": "Logs/demo072-bee882-895-structured-report.json", "requiredIfXmlMissing": true}
    ],
    "reports": [
      {"id": "demo_report", "path": "DEMO-072_Report.md", "required": true},
      {"id": "qa_report", "path": "QA_DEMO_072_VALIDATION.md", "required": false}
    ]
  },
  "testSummary": {
    "machineReadablePresent": true,
    "nunitXmlPresent": false,
    "fallbackJsonPresent": true,
    "total": 0,
    "passed": 0,
    "failed": 0,
    "skipped": 0
  },
  "nonClaims": {
    "worldMapActive": false,
    "explorationWorldActive": false,
    "allianceActive": false,
    "warActive": false,
    "mmoMapActive": false,
    "officialServerLive": false,
    "officialEndpoint": false,
    "officialSave": false,
    "officialEconomy": false,
    "officialPersistentArmy": false,
    "bee881Implemented": false
  }
}
```

### Validation du manifest

QA doit refuser ou bloquer si :

- `machineReadablePresent` est false ;
- XML absent et JSON fallback absent ;
- un artefact `required:true` n'existe pas ;
- un support BEE-892/BEE-895 est declare comme runtime ;
- une non-claim interdite passe a true ;
- BEE-881 apparait comme implemente.

## 4. Gate regression suite Ruche jouable

BEE-895 doit assembler les categories critiques avant tout nouveau gate.

| Categorie | Minimum attendu | PASS | PASS_WITH_RESERVES | BLOCKED |
| --- | --- | --- | --- | --- |
| Structured output | XML NUnit ou JSON equivalent | Machine-readable parseable et cite par manifest | XML absent mais JSON equivalent complet | Aucun rapport structure |
| Produce | Ressources tick/feedback | Valeurs et deltas visibles/testes | Capture suffisante, assertion partielle | Changement sans feedback |
| Spend | Cout applique | Cout une fois, ressources non negatives | Reserve si seulement JSON prouve | Double depense |
| Upgrade | Accepted/rejected/pending/server-required | Etats visibles + assertions | Etat rare non capture mais teste | Bouton muet, cout/timer absent |
| Training | Queue/progression/completion | Queue unique, troop increment once | Reserve portrait/tactile | Double queue |
| Army | Compteurs locaux non persistants | Soldats/Gardiennes/Eclaireuses visibles | UI compacte mais lisible | Claim armee persistante |
| Snapshot/reconciliation | Dev-only future prep | Stale/conflict lisible si present | Support manifest seulement | Claim save officielle |
| No-world-map | Scope bloque | world map/exploration/alliance/war false | N/A | Toute carte monde active |
| Non-claims | Toutes fausses pour officiel/live | Manifest + preuves coherent | Microcopy a polir mais non trompeuse | Claim live/save/economie/armee |

### Decision BEE-895

- PASS : toutes les categories critiques passent et XML ou JSON equivalent est present.
- PASS_WITH_RESERVES : XML absent mais JSON equivalent complet, ou tactile/portrait reserve non bloquante.
- BLOCKED : aucun rapport machine-readable, double spend/queue, bouton critique muet, carte monde active, BEE-881 presente, ou claim officiel.

## 5. Non-claims obligatoires

```yaml
non_claims:
  world_map_active: false
  exploration_world_active: false
  alliance_active: false
  war_active: false
  mmo_map_active: false
  official_server_live: false
  official_endpoint: false
  official_save: false
  official_economy: false
  official_persistent_army: false
  bee_881_implemented: false
```

## 6. Recommandation d'integration non conflictuelle

Builder-B recommande :

- ne pas toucher aux classes runtime Builder-A depuis ce support ;
- laisser Builder-C restaurer l'outillage de test ou produire l'adaptateur JSON equivalent ;
- demander a Demo-A de joindre XML/JSON au dossier officiel ;
- demander a QA-A de ne plus accepter un batch log seul comme preuve long terme ;
- garder BEE-881 et toute carte monde hors scope.

## Verdict Builder-B

Le support BEE-892 a BEE-895 est pret pour DEMO-072 / QA-072. Le chemin recommande est : NUnit XML si possible, JSON machine-readable equivalent si XML indisponible, manifest QA stable, puis gate regression suite ruche.

READY_FOR_DEMO_072_TEST_SUPPORT = YES
