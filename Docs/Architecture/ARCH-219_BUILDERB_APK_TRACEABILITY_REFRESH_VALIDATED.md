# ARCH-219 - Builder-B APK Traceability Refresh Validated

Date: 2026-07-12

## Decision

Architecte valide la correction ciblee Builder-B pour la tracabilite APK DEMO-075.

Rapport:

- `C:/projets/beekingdom/prompts_codex/rapports/BuilderB_DEMO075_CurrentAPKTraceabilityFix_Report.md`
- Verdict: `READY_FOR_DEMO_075_APK_TRACEABILITY_REFRESH = YES`

## APK courant confirme

- Path: `C:/projets/beekingdomgame-master/Builds/Android/BeeKingdom.apk`
- Size bytes: `42953385`
- SHA256: `5A4867C35C95F6621C0EA72B6A61BD9E42D87E8218CCAA7A61FA738B29889554`
- Last write time local: `2026-07-12T10:26:31-04:00`

## Manifeste corrige

Builder-B a mis a jour:

- `C:/projets/beekingdom/prompt_demo/rapports/DEMO-075_BEE921_940/DEMO-075_APKDeviceManifest.json`

Le manifeste correspond maintenant au APK courant.

## Reserves conservees

Cette correction ne ferme pas:

- installation APK sur appareil;
- lancement APK sur appareil;
- preuve telephone portrait physique;
- preuve tablette paysage physique;
- confort tactile physique.

La reserve reste:

- `PHYSICAL_DEVICE_PROOF = PENDING`

## Non-claims conserves

- Aucun serveur officiel/live.
- Aucun endpoint officiel.
- Aucune sauvegarde officielle.
- Aucune economie officielle.
- Aucune armee persistante officielle.
- Aucune carte monde.
- Aucun BEE-881.

## Chaine suivante

Demo-A doit produire un addendum DEMO-075 limite a la correction de tracabilite APK.

QA-A doit ensuite revalider uniquement la reserve `APK traceability mismatch`.
