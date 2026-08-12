# Builder-B - BEE-901 / BEE-902 / BEE-918 / BEE-919 / BEE-920 Structured Gate Support

Statut : support non-runtime  
Date : 2026-07-12  
Portee : Ruche jouable uniquement, fermeture reserves QA-072  
Contexte : ARCH-207 valide Planner BEE-901 a BEE-920  
Integration : support Demo/QA/Builder-C, sans modification runtime Builder-A  

Ce document implemente le scaffold de support Builder-B pour la sortie de tests structuree, le fallback JSON, la matrice de fermeture des reserves, le scope-lock no-world-map/BEE-881 et le gate DEMO-073. Il ne modifie pas le runtime principal, la scene, les assets, le serveur, la carte monde ou l'APK.

## Sources lues

- `C:/projets/beekingdomgame-master/Docs/Architecture/ARCH-207_Planner901_920_ValidationAndParallelDispatch.md`
- `C:/projets/beekingdom/QA/QA_DEMO_072_BEE882_900_VALIDATION.md`
- `C:/projets/beekingdom/prompts_codex/BEE-901_Structured_Unity_Test_Output_Recovery_Framework.md`
- `C:/projets/beekingdom/prompts_codex/BEE-902_Machine_Readable_Hive_Action_Loop_JSON_Fallback_Framework.md`
- `C:/projets/beekingdom/prompts_codex/BEE-918_Playable_Hive_QA_Reserve_Closure_Matrix_Framework.md`
- `C:/projets/beekingdom/prompts_codex/BEE-919_No_World_Map_And_BEE881_Scope_Lock_Framework.md`
- `C:/projets/beekingdom/prompts_codex/BEE-920_Playable_Hive_Product_Reserve_Closure_Gate_Framework.md`

## Objectif

Fermer ou encadrer la reserve QA-072 sur l'absence de sortie structuree. QA ne doit plus dependre seulement d'une methode batch et d'un log texte.

Priorite :

1. Restaurer NUnit XML Unity si Builder-C peut le brancher.
2. Produire un JSON machine-readable equivalent si XML indisponible.
3. Relier XML/JSON au manifest DEMO-073.
4. Couvrir produce, spend, reserved cost, upgrade, training, refus, recovery et non-claims.
5. Garder no-world-map, no BEE-881 et no official live claims.

## 1. Sortie structuree BEE-901

### Cible NUnit XML

Chemin attendu :

```text
C:/projets/beekingdomgame-master/Logs/demo073-bee901-920-tests.xml
```

Contenu minimal attendu :

```xml
<test-run id="DEMO-073" testcasecount="8" result="Passed" duration="0.000">
  <test-suite name="PlayableHiveStructuredGate" result="Passed">
    <test-case name="ProduceSpendUpgradeTrainActionLoop" result="Passed" duration="0.000">
      <properties>
        <property name="beeRange" value="BEE-901/BEE-920" />
        <property name="scope" value="playable_hive_only" />
        <property name="worldMapActive" value="false" />
        <property name="bee881Implemented" value="false" />
        <property name="officialServerLive" value="false" />
        <property name="artifactJsonFallback" value="Logs/demo073-bee901-920-structured-report.json" />
      </properties>
    </test-case>
  </test-suite>
</test-run>
```

QA peut accepter XML seul si :

- tous les test-cases critiques sont presents ;
- status, duration, assertions/properties et paths artefacts sont parseables ;
- non-claims critiques sont presents ;
- le manifest DEMO-073 reference ce fichier.

## 2. Fallback JSON BEE-902

Si Unity ne produit pas NUnit XML, le fallback JSON doit etre considere comme equivalent seulement s'il est machine-readable, stable, complet et reference par le manifest.

Chemin attendu :

```text
C:/projets/beekingdomgame-master/Logs/demo073-bee901-920-structured-report.json
```

Schema propose :

```json
{
  "schema": "bee-kingdom.playable-hive.structured-test-report.v2",
  "demoId": "DEMO-073",
  "qaGate": "QA-073",
  "generatedAtUtc": "2026-07-12T00:00:00Z",
  "scope": {
    "playableHiveOnly": true,
    "worldMapActive": false,
    "bee881Implemented": false,
    "runtimeOwner": "Builder-A",
    "structuredOutputOwner": "Builder-B/Builder-C",
    "supportOnly": true
  },
  "summary": {
    "total": 8,
    "passed": 8,
    "failed": 0,
    "skipped": 0,
    "durationMs": 0,
    "machineReadable": true
  },
  "actionLoop": {
    "produce": {
      "status": "passed",
      "resourceBefore": {"honey": 1000},
      "resourceAfter": {"honey": 1042},
      "deltaVisible": true,
      "feedbackVisible": true
    },
    "spend": {
      "status": "passed",
      "costDisplayed": {"honey": 100, "wax": 10},
      "costAppliedOnce": true,
      "resourcesNonNegative": true
    },
    "reservedCost": {
      "status": "passed",
      "reservedVisible": true,
      "reservedNotDoubleApplied": true,
      "releasedOrCommittedStateVisible": true
    },
    "upgrade": {
      "status": "passed",
      "beforeVisible": true,
      "pendingVisible": true,
      "completionVisible": true,
      "levelIncrementOnce": true,
      "rapidTapGuarded": true
    },
    "training": {
      "status": "passed",
      "queueVisible": true,
      "completionVisible": true,
      "troopIncrementOnce": true,
      "rapidTapGuarded": true
    },
    "refusal": {
      "status": "passed",
      "causeVisible": true,
      "nextStepVisible": true,
      "noCostApplied": true
    },
    "recovery": {
      "status": "passed",
      "recoveryActionVisible": true,
      "stateReturnsToActionable": true
    }
  },
  "tests": [
    {
      "id": "BEE901_STRUCTURED_OUTPUT_PRESENT",
      "name": "Structured output exists and is parseable",
      "bee": "BEE-901",
      "status": "passed",
      "durationMs": 0,
      "assertions": [
        {"name": "machine_readable_report_present", "expected": true, "actual": true, "passed": true}
      ],
      "artifacts": ["Logs/demo073-bee901-920-structured-report.json"]
    }
  ],
  "nonClaims": {
    "worldMapActive": false,
    "explorationActive": false,
    "allianceActive": false,
    "warActive": false,
    "mmoMapActive": false,
    "bee881Implemented": false,
    "officialServerLive": false,
    "officialEndpoint": false,
    "officialSave": false,
    "officialEconomy": false,
    "officialPersistentArmy": false,
    "productionSqlMigration": false,
    "productionPublish": false
  }
}
```

## 3. Matrice action loop structuree

| Domaine | Assertion machine-readable | PASS | BLOCKED |
| --- | --- | --- | --- |
| Produce | `produce.deltaVisible == true` | Ressource augmente avec feedback visible | Valeur change sans feedback |
| Spend | `spend.costAppliedOnce == true` | Cout applique une seule fois | Double depense ou ressource negative |
| Reserved cost | `reservedCost.reservedVisible == true` | Cout reserve visible, pas double commit | Cout reserve confus ou cache |
| Upgrade | `upgrade.completionVisible == true` | Avant/pending/completion/niveau/resultat visibles | Completion absente ou niveau double |
| Training | `training.queueVisible == true` et `troopIncrementOnce == true` | Queue + completion + troupe locale | Double queue ou troupe double |
| Refus | `refusal.causeVisible == true` | Cause et prochain geste visibles | Refus muet ou erreur brute |
| Recovery | `recovery.stateReturnsToActionable == true` | Joueur comprend comment revenir a une action | Blocage sans sortie |
| Non-claims | chaque champ officiel == false | Aucun claim interdit | Claim live/save/economie/armee/carte monde |

## 4. Matrice QA de fermeture des reserves BEE-901 a BEE-920

| Reserve QA-072 | BEE concernees | Preuve attendue DEMO-073 | PASS | PASS_WITH_RESERVES | BLOCKED |
| --- | --- | --- | --- | --- | --- |
| Structured test output missing | BEE-901/BEE-902 | XML NUnit ou JSON equivalent | Rapport parseable present | XML absent mais JSON complet | Aucun rapport machine-readable |
| Physical device proof absent | BEE-911/BEE-913, hors Builder-B | Pack device Builder-C | Device reel fourni | Reserve maintenue explicitement | Preuve simulee presentee comme physique |
| Portrait density | BEE-908, UI-B/UI-A | Capture portrait + scoring | Lisible/confortable | Lisible mais dense | Texte/action critique coupes |
| Upgrade completion visual proof | BEE-903 | Capture completion dediee | Niveau/resultat visibles | Completion couverte par JSON mais capture faible | Completion absente |
| Buttons non-mute | BEE-906 | Etats action + feedback | Tous boutons critiques feedback | Bouton secondaire perfectible | Bouton critique muet |
| Refusal recovery | BEE-907 | Cause + next step | Cause/prochain geste visibles | Microcopy a polir | Refus sans solution |
| No-world-map/BEE-881 | BEE-919 | Manifest scope-lock | Tous false / absent | N/A | Carte monde, BEE-881 ou claim monde |
| Gate final | BEE-920 | Manifest + rapport QA | Reserves fermees ou acceptees | Reserves non bloquantes explicites | Reserve bloquante ou non documentee |

## 5. Scope-lock BEE-919

Le manifest DEMO-073 doit contenir ce bloc :

```yaml
scope_lock:
  playable_hive_only: true
  world_map_active: false
  exploration_active: false
  alliance_active: false
  war_active: false
  mmo_map_active: false
  bee_881_created: false
  bee_881_implemented: false
  bee_881_unlocked: false
  official_server_live: false
  official_endpoint: false
  official_save: false
  official_economy: false
  official_persistent_army: false
  production_sql_migration: false
  production_publish: false
```

QA doit bloquer si un seul de ces champs interdits passe a `true`.

## 6. Manifest DEMO-073 propre

Chemin recommande :

```text
C:/projets/beekingdom/prompt_demo/rapports/DEMO-073_BEE901_920/DEMO-073_QAArtifactManifest.json
```

Schema :

```json
{
  "schema": "bee-kingdom.demo-qa-artifact-manifest.v2",
  "demoId": "DEMO-073",
  "qaGate": "QA-073",
  "runtimeScope": {
    "owner": "Builder-A",
    "beeRange": "BEE-903/BEE-907/BEE-910/BEE-917 if delivered",
    "playableHiveOnly": true
  },
  "supportScope": {
    "builderB": ["BEE-901", "BEE-902", "BEE-918", "BEE-919", "BEE-920"],
    "supportOnly": true
  },
  "structuredOutput": {
    "nunitXmlPath": "C:/projets/beekingdomgame-master/Logs/demo073-bee901-920-tests.xml",
    "jsonFallbackPath": "C:/projets/beekingdomgame-master/Logs/demo073-bee901-920-structured-report.json",
    "machineReadableRequired": true,
    "xmlPreferred": true,
    "jsonFallbackAllowed": true
  },
  "requiredEvidence": [
    {"id": "produce", "type": "capture_or_test", "required": true},
    {"id": "spend_reserved_cost", "type": "capture_or_test", "required": true},
    {"id": "upgrade_completion", "type": "capture_and_test", "required": true},
    {"id": "training_completion", "type": "capture_and_test", "required": true},
    {"id": "refusal_recovery", "type": "capture_and_test", "required": true},
    {"id": "scope_lock", "type": "manifest", "required": true}
  ],
  "qaReserveClosure": {
    "structuredOutput": "pass|pass_with_reserves|blocked",
    "deviceProof": "pass|pass_with_reserves|blocked",
    "portraitDensity": "pass|pass_with_reserves|blocked",
    "upgradeCompletion": "pass|pass_with_reserves|blocked",
    "scopeLock": "pass|blocked"
  },
  "scopeLock": {
    "worldMapActive": false,
    "explorationActive": false,
    "allianceActive": false,
    "warActive": false,
    "mmoMapActive": false,
    "bee881Created": false,
    "bee881Implemented": false,
    "bee881Unlocked": false,
    "officialServerLive": false,
    "officialEndpoint": false,
    "officialSave": false,
    "officialEconomy": false,
    "officialPersistentArmy": false
  }
}
```

## 7. Gate BEE-920

BEE-920 peut etre propose pour Demo/QA si :

- XML NUnit existe ou JSON fallback equivalent existe ;
- manifest DEMO-073 reference tous les artefacts ;
- action loop structuree couvre produce, spend, reserved cost, upgrade, training, refus, recovery ;
- non-claims stricts sont false ;
- BEE-881 reste absente ;
- carte monde/exploration/alliance/guerre/map MMO restent absents ;
- supports sont marques support-only, pas runtime.

BEE-920 doit etre BLOCKED si :

- aucun rapport machine-readable ;
- double spend ou double queue ;
- upgrade completion toujours non prouvee ;
- refus/recovery muet ;
- BEE-881 creee/debloquee ;
- carte monde ou claim officiel live/save/economie/armee ;
- manifest DEMO-073 absent ou non parseable.

## Limites Builder-B

- Aucun code runtime ajoute.
- Aucun hook Builder-A modifie.
- Aucun serveur modifie.
- Aucun asset ou scene modifie.
- Aucun APK genere.
- Aucun BEE-881 cree ou debloque.
- Aucune carte monde.

## Verdict Builder-B

Le scaffold BEE-901 / BEE-902 / BEE-918 / BEE-919 / BEE-920 est pret pour DEMO-073. Builder-C peut brancher l'export XML/JSON, Builder-A peut fournir les assertions runtime, Demo-A peut officialiser le manifest, et QA-A peut valider sans dependance exclusive aux logs batch.

READY_FOR_DEMO_073_STRUCTURED_SUPPORT = YES
