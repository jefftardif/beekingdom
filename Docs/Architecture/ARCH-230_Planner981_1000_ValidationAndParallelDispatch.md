# ARCH-230 - Planner 981-1000 Validation And Parallel Dispatch

Date: 2026-07-12

## Decision

Architecte valide Planner BEE-981 a BEE-1000.

Rapport:

- `C:/projets/beekingdom/prompts_codex/rapports/Planner_BEE981_1000_Report.md`
- Verdict: `BEE-1000_READY_FOR_ARCHITECT_VALIDATION = YES`

## Scope valide

La vague respecte le gate courant:

- vraie preuve visuelle locale player-facing;
- contact sheet image ou screenshot bundle local T0-T8;
- separation local/demo/device/officiel;
- aucune carte monde;
- aucun BEE-881;
- aucun serveur officiel/live.

## BEE composees

- BEE-981: intake preuve visuelle locale reelle.
- BEE-982: dossier screenshot bundle DEMO-078.
- BEE-983: contact sheet image T0-T8.
- BEE-984: T0 session start/resource screenshot.
- BEE-985: T1 action confirmation screenshot.
- BEE-986: T2 disabled state screenshot.
- BEE-987: T3 refusal/recovery screenshot.
- BEE-988: T4 upgrade completion screenshot.
- BEE-989: T5 training completion screenshot.
- BEE-990: T6 local army inspection screenshot.
- BEE-991: T7 gesture UI fixed screenshot.
- BEE-992: T8 non-claims scope lock screenshot.
- BEE-993: optional local video clip.
- BEE-994: visual artifact manifest enforcement.
- BEE-995: image quality and cropping QA.
- BEE-996: physical device proof separation.
- BEE-997: server live claim visual guard.
- BEE-998: no-world visual scope guard.
- BEE-999: DEMO-078 visual QA handoff.
- BEE-1000: hive local/demo visual proof gate.

## Note produit future

Le compte joueur, l'inscription, la connexion et la liaison Google/Facebook deviennent un axe produit/serveur a planifier apres le gate visuel BEE-1000. Cet axe ne doit pas etre glisse dans DEMO-078 pour ne pas casser le focus ruche.

## Dispatch parallele

### Builder-A

Implementer/generer les etats screenshots player-facing:

- BEE-984
- BEE-985
- BEE-986
- BEE-987
- BEE-988
- BEE-989
- BEE-990
- BEE-991
- BEE-992

Objectif: produire les etats runtime locaux necessaires aux captures T0-T8.

### Builder-B

Construire le bundle visuel et les manifests:

- BEE-981
- BEE-982
- BEE-983
- BEE-993
- BEE-994
- BEE-998
- BEE-999
- BEE-1000

Objectif: dossier DEMO-078, contact sheet image ou screenshot bundle, manifest visuel, scope guard et handoff QA.

### UI-B

Support qualite visuelle:

- BEE-995
- support UI pour BEE-983 a BEE-992

Objectif: verifier lisibilite, cropping, labels local/demo/device/officiel, aucun texte coupe, aucune confusion visuelle.

### Server-A

Support non-claim visuel:

- BEE-997
- support BEE-996

Objectif: garantir qu'aucune capture/manifest ne revendique serveur officiel/live, endpoint, save, economie ou armee persistante officielle.

## Equipes en attente

Demo-A attend Builder-A + Builder-B + UI-B + Server-A.

QA-A attend Demo-A.

## Interdictions

- Ne pas relancer carte monde.
- Ne pas debloquer BEE-881.
- Ne pas creer exploration/alliance/guerre/map MMO.
- Ne pas pretendre serveur officiel/live.
- Ne pas pretendre physical device proof sans artefacts reels.
- Ne pas accepter un plan textuel comme preuve visuelle.
