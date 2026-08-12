# ARCH-226 - Planner 961-980 Validation And Parallel Dispatch

Date: 2026-07-12

## Decision

Architecte valide Planner BEE-961 a BEE-980.

Rapport:

- `C:/projets/beekingdom/prompts_codex/rapports/Planner_BEE961_980_Report.md`
- Verdict: `BEE-980_READY_FOR_ARCHITECT_VALIDATION = YES`

## Scope valide

La vague respecte le gate courant:

- preuves player-facing directes pour la ruche jouable;
- bundle capture/video local DEMO-077;
- procedure appareil reel preparee sans faux claim;
- distinction local/demo/device/officiel;
- aucune carte monde;
- aucun BEE-881;
- aucun serveur officiel/live.

## BEE composees

- BEE-961: intake vague preuve player-facing.
- BEE-962: carte scenarios capture locale.
- BEE-963: capture confirmation action.
- BEE-964: capture disabled state.
- BEE-965: preuve visuelle refus/recovery.
- BEE-966: preuve player-facing completion upgrade.
- BEE-967: preuve player-facing completion training.
- BEE-968: contact sheet boucle quotidienne.
- BEE-969: preuve video locale optionnelle.
- BEE-970: procedure install/lancement device reel.
- BEE-971: procedure telephone portrait physique.
- BEE-972: procedure tablette paysage physique.
- BEE-973: matrice preuve gestes physiques.
- BEE-974: labels local/demo vs physical proof.
- BEE-975: frontiere claim serveur officiel.
- BEE-976: split parallele.
- BEE-977: manifest artefacts DEMO-077.
- BEE-978: quick QA smoke pack player-facing.
- BEE-979: enforcement no-world/no-BEE881.
- BEE-980: gate proof/device-ready ruche.

## Dispatch parallele

### Builder-A

Implementer les etats runtime/capture player-facing:

- BEE-963
- BEE-964
- BEE-965
- BEE-966
- BEE-967

Objectif: produire des etats visibles directement pour confirmation, disabled, refus/recovery, upgrade completion et training completion.

### Builder-B

Implementer le pack preuves locales et manifests:

- BEE-961
- BEE-962
- BEE-968
- BEE-969
- BEE-977
- BEE-978
- BEE-979
- BEE-980

Objectif: construire le bundle DEMO-077, contact sheet, preuves locales, quick QA pack et enforcement no-world/no-BEE881.

### Builder-C

Preparer le protocole appareil reel:

- BEE-970
- BEE-971
- BEE-972
- BEE-973

Objectif: procedure install/lancement APK, phone portrait, tablet landscape et gestes physiques. Ne pas fermer la preuve sans artefacts reels.

### UI-B

Support labels et lisibilite:

- BEE-974
- support UI pour BEE-963 a BEE-968

Objectif: labels clairs local/demo/physical, lisibilite player-facing, boutons/etats comprehensibles.

### Server-A

Frontiere non-claim:

- BEE-975

Objectif: garantir qu'aucun artefact DEMO-077 ne revendique serveur officiel/live, endpoint, save, economie ou armee persistante officielle.

## Equipes en attente

Demo-A attend Builder-A + Builder-B + Builder-C + UI-B + Server-A.

QA-A attend Demo-A.

## Interdictions

- Ne pas relancer carte monde.
- Ne pas debloquer BEE-881.
- Ne pas creer exploration/alliance/guerre/map MMO.
- Ne pas pretendre serveur officiel/live.
- Ne pas fermer physical device proof sans captures/videos reelles d'appareil.
