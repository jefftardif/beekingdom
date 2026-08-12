# Builder-B - BEE-933 / BEE-934 / BEE-938 / BEE-939 / BEE-940 App Readiness Evidence Support

Statut : support non-runtime  
Date : 2026-07-12  
Portee : preuves DEMO-075 / QA-075, app readiness ruche jouable  
Contexte : ARCH-215 valide Planner BEE-921 a BEE-940  
Integration : support Builder-B uniquement, sans modification runtime Builder-A  

Ce document fournit les manifests et checklists propres pour DEMO-075 / QA-075. Il ne modifie pas le runtime Builder-A, la scene, les assets, le serveur, la carte monde ou l'APK. Il ne cree pas et ne debloque pas BEE-881.

## Sources lues

- `C:/projets/beekingdomgame-master/Docs/Architecture/ARCH-215_Planner921_940_ValidationAndParallelDispatch.md`
- `C:/projets/beekingdom/prompts_codex/BEE-933_Structured_Evidence_Continuity_Framework.md`
- `C:/projets/beekingdom/prompts_codex/BEE-934_APK_Device_Evidence_Manifest_Framework.md`
- `C:/projets/beekingdom/prompts_codex/BEE-938_Playable_Hive_App_Readiness_Checklist_Framework.md`
- `C:/projets/beekingdom/prompts_codex/BEE-939_No_World_Map_Scope_Lock_For_Device_Gate_Framework.md`
- `C:/projets/beekingdom/prompts_codex/BEE-940_Playable_Hive_Product_Readiness_Device_Gate_Framework.md`

## Objectif

Preparer un pack de support pour DEMO-075 / QA-075 qui permet de relire clairement :

- la continuite JSON/XML des preuves ;
- le manifest APK/device ;
- la checklist app readiness de la ruche jouable ;
- le scope-lock no world map / no BEE-881 ;
- les criteres du gate BEE-940 ;
- le statut exact de la preuve physique reelle, sans la pretendre fermee si les artefacts reels ne sont pas fournis.

## 1. Continuite JSON/XML des preuves - BEE-933

DEMO-075 doit conserver un fil lisible entre captures, logs, tests structures, manifest et verdict QA.

### Chemins recommandes

```text
C:/projets/beekingdomgame-master/Logs/demo075-bee921-940-tests.xml
C:/projets/beekingdomgame-master/Logs/demo075-bee921-940-structured-report.json
C:/projets/beekingdom/prompt_demo/rapports/DEMO-075_BEE921_940/DEMO-075_QAArtifactManifest.json
```

### XML minimal attendu

```xml
<test-run id="DEMO-075" result="Passed|Failed" testcasecount="0" duration="0.000">
  <test-suite name="PlayableHiveAppReadiness" result="Passed|Failed">
    <test-case name="DailyHiveLoopEvidenceContinuity" result="Passed" duration="0.000">
      <properties>
        <property name="scope" value="playable_hive_only" />
        <property name="worldMapActive" value="false" />
        <property name="bee881Created" value="false" />
        <property name="physicalProofClosed" value="true|false" />
        <property name="jsonFallback" value="Logs/demo075-bee921-940-structured-report.json" />
      </properties>
    </test-case>
  </test-suite>
</test-run>
```

### JSON structured report

```json
{
  "schema": "bee-kingdom.playable-hive.evidence-continuity.v1",
  "demoId": "DEMO-075",
  "qaGate": "QA-075",
  "generatedAtUtc": "2026-07-12T00:00:00Z",
  "scope": {
    "playableHiveOnly": true,
    "worldMapActive": false,
    "bee881Created": false,
    "bee881Unlocked": false,
    "officialServerLive": false,
    "officialEndpoint": false,
    "officialSave": false,
    "officialEconomy": false,
    "officialPersistentArmy": false
  },
  "evidenceContinuity": {
    "capturesIndexed": true,
    "logsIndexed": true,
    "structuredOutputIndexed": true,
    "apkDeviceManifestIndexed": true,
    "appReadinessChecklistIndexed": true,
    "scopeLockIndexed": true
  },
  "physicalProof": {
    "realDeviceEvidenceProvided": false,
    "phoneDeviceProofClosed": false,
    "tabletDeviceProofClosed": false,
    "reserveMustRemainIfNoArtifacts": true
  },
  "summary": {
    "status": "pass|pass_with_reserves|blocked",
    "blockingFailures": [],
    "nonBlockingReserves": []
  }
}
```

QA doit refuser une preuve dite "device proof closed" si `realDeviceEvidenceProvided` est false.

## 2. Manifest APK / device evidence - BEE-934

Le manifest APK/device doit relier build, appareil, orientation, scenario, preuve et verdict. Il doit pouvoir representer explicitement l'absence de preuve physique.

```json
{
  "schema": "bee-kingdom.apk-device-evidence-manifest.v1",
  "demoId": "DEMO-075",
  "apk": {
    "path": "C:/projets/beekingdomgame-master/Builds/Android/BeeKingdom.apk",
    "exists": true,
    "sha256": null,
    "buildTimestampUtc": null,
    "sourceCommitOrBuildId": null,
    "builder": "Builder-C or Demo-A",
    "runtimeScope": "playable_hive_only"
  },
  "devices": [
    {
      "deviceId": "phone-portrait-01",
      "kind": "phone",
      "physicalDevice": false,
      "model": null,
      "osVersion": null,
      "orientation": "portrait",
      "resolution": "390x844 or actual",
      "inputMode": "touch|simulated|unknown",
      "evidence": {
        "screenshots": [],
        "video": null,
        "installLog": null,
        "smokeTestLog": null
      },
      "verdict": "not_run|pass|pass_with_reserves|fail"
    },
    {
      "deviceId": "tablet-landscape-01",
      "kind": "tablet",
      "physicalDevice": false,
      "model": null,
      "osVersion": null,
      "orientation": "landscape",
      "resolution": "1280x720 or actual",
      "inputMode": "touch|simulated|unknown",
      "evidence": {
        "screenshots": [],
        "video": null,
        "installLog": null,
        "smokeTestLog": null
      },
      "verdict": "not_run|pass|pass_with_reserves|fail"
    }
  ],
  "deviceGate": {
    "physicalProofClosed": false,
    "phonePortraitProofClosed": false,
    "tabletLandscapeProofClosed": false,
    "reserveIfNoRealDevice": true
  },
  "nonClaims": {
    "worldMapActive": false,
    "bee881Created": false,
    "officialServerLive": false,
    "officialEndpoint": false,
    "officialSave": false,
    "officialEconomy": false,
    "officialPersistentArmy": false
  }
}
```

Champs obligatoires pour fermer une preuve physique :

- `physicalDevice: true`
- `model` renseigne
- `osVersion` renseigne
- capture ou video presente
- install/smoke log present si APK teste
- orientation et resolution reelles indiquees
- verdict explicite

## 3. Checklist app readiness ruche jouable - BEE-938

| Domaine | PASS attendu | PASS_WITH_RESERVES acceptable | BLOCKED si |
| --- | --- | --- | --- |
| Daily collect resources | Ressources visibles, collecte/tick et feedback clairs | Feedback compact mais lisible | Ressource change sans feedback |
| Daily upgrade building | Cout, timer, progression, completion visibles | Completion visible par manifest mais capture a renforcer | Cout/timer/completion absents |
| Daily train troops | Cout, queue, timer, arrivee troupes visibles | Queue compacte mais lisible | Training muet ou queue absente |
| Inspect local army | Soldats, Gardiennes, Eclaireuses visibles comme local non officiel | Section minimale mais comprehensible | Armee absente ou persistante officielle revendiquee |
| Refusal recovery | Cause + prochain geste visibles | Microcopy a polir mais non bloquante | Refus sans raison ou sans issue |
| Menus permanents | HUD, panneaux, navigation fixes et tapables | Densite portrait reservee | Boutons/touch targets inutilisables |
| Texte critique | Aucun texte critique coupe | Texte secondaire compact | Cout/raison/action coupes |
| Touch comfort | Cibles 48 px minimum, 56 px recommande | Simulateur seulement si declare | Preuve tactile pretendue sans device |
| Pan/zoom | Un doigt pan, deux doigts pinch, UI bloque gestes ruche | Reserve physique ouverte | HUD bouge ou bouton declenche pan |
| Structured evidence | XML ou JSON present et indexe | XML absent mais JSON valide | Aucun rapport machine-readable |
| APK/device manifest | APK et device status indexes | Device non teste mais reserve explicite | Proof physique fermee sans artefacts |
| Non-claims | Tous les claims officiels false | N/A | Claim live/save/economie/armee/carte monde |

## 4. Scope-lock no world map / no BEE-881 - BEE-939

DEMO-075 doit contenir un bloc scope-lock explicite :

```yaml
scope_lock:
  playable_hive_only: true
  world_map_runtime: false
  world_map_modified: false
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

QA doit bloquer si une des lignes interdites devient `true`, ou si une capture carte monde/exploration/alliance/guerre est presentee comme preuve de ce lot.

## 5. Manifest DEMO-075 / QA-075

Chemin recommande :

```text
C:/projets/beekingdom/prompt_demo/rapports/DEMO-075_BEE921_940/DEMO-075_QAArtifactManifest.json
```

Schema propose :

```json
{
  "schema": "bee-kingdom.demo-qa-app-readiness-manifest.v1",
  "demoId": "DEMO-075",
  "qaGate": "QA-075",
  "runtimeScope": {
    "builderA": ["BEE-925", "BEE-926", "BEE-927", "BEE-928", "BEE-929", "BEE-930"],
    "playableHiveOnly": true
  },
  "supportScope": {
    "builderB": ["BEE-933", "BEE-934", "BEE-938", "BEE-939", "BEE-940"],
    "builderC": ["BEE-921", "BEE-922", "BEE-923", "BEE-924"],
    "uiB": ["BEE-931", "BEE-932"],
    "serverA": ["BEE-935", "BEE-936", "BEE-937"],
    "supportOnly": true
  },
  "artifacts": {
    "structuredXml": "C:/projets/beekingdomgame-master/Logs/demo075-bee921-940-tests.xml",
    "structuredJson": "C:/projets/beekingdomgame-master/Logs/demo075-bee921-940-structured-report.json",
    "apkDeviceManifest": "DEMO-075_APKDeviceManifest.json",
    "appReadinessChecklist": "DEMO-075_AppReadinessChecklist.json",
    "contactSheet": "DEMO-075_ContactSheet.png",
    "demoReport": "DEMO-075_Report.md"
  },
  "readiness": {
    "dailyHiveLoopEvidencePresent": false,
    "apkEvidencePresent": false,
    "realDeviceEvidencePresent": false,
    "physicalProofClosed": false,
    "physicalProofReserveExplicit": true,
    "structuredEvidencePresent": true,
    "scopeLockPresent": true
  },
  "nonClaims": {
    "worldMapRuntime": false,
    "bee881Unlocked": false,
    "officialServerLive": false,
    "officialEndpoint": false,
    "officialSave": false,
    "officialEconomy": false,
    "officialPersistentArmy": false
  }
}
```

## 6. Criteres BEE-940 gate

BEE-940 peut etre `PASS` seulement si :

- daily hive loop runtime est prouvee par Builder-A/Demo-A ;
- JSON ou XML structure est present ;
- APK/device manifest est present ;
- preuve physique reelle est fournie si le gate pretend fermer la reserve device ;
- app readiness checklist est remplie ;
- scope-lock no-world-map/no-BEE-881 est present ;
- aucun claim officiel interdit n'apparait.

BEE-940 peut etre `PASS_WITH_RESERVES` si :

- la ruche jouable est lisible et testable ;
- les preuves structurees et manifests sont propres ;
- APK/device manifest existe mais physical proof reste `false` ;
- la reserve device est explicitement maintenue ;
- aucun claim officiel n'est fait.

BEE-940 doit etre `BLOCKED` si :

- physical proof est declare ferme sans artefacts reels ;
- aucun manifest APK/device n'est fourni ;
- aucun JSON/XML structure n'est fourni ;
- carte monde, exploration, alliance, guerre ou map MMO apparaissent ;
- BEE-881 est creee, implementee ou debloquee ;
- serveur officiel/live, endpoint officiel, save/economie/armee persistante sont revendiques ;
- action principale, cout, timer, queue, texte critique ou raisons disabled sont coupes/inutilisables.

## 7. Limites Builder-B

- Aucun runtime Builder-A modifie.
- Aucune scene modifiee.
- Aucun asset modifie.
- Aucun serveur modifie.
- Aucun APK genere par Builder-B.
- Aucune carte monde.
- Aucun BEE-881 cree ou debloque.
- Aucune fermeture de preuve physique sans artefacts reels.

## Verdict Builder-B

Le support BEE-933 / BEE-934 / BEE-938 / BEE-939 / BEE-940 est pret pour DEMO-075 / QA-075. Le pack definit les manifests, checklists et scope-locks necessaires, tout en maintenant explicitement la reserve physique si aucun vrai appareil n'est prouve.

READY_FOR_DEMO_075_EVIDENCE_SUPPORT = YES
