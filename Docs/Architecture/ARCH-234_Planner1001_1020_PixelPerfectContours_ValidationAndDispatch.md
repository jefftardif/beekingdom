# ARCH-234 - Planner 1001-1020 Pixel Perfect Contours Validation And Dispatch

Date: 2026-07-12

## Decision

Architecte valide Planner BEE-1001 a BEE-1020.

Rapport:

- `C:/projets/beekingdom/prompts_codex/rapports/Planner_BEE1001_1020_Report.md`
- Verdict: `PIXEL_PERFECT_CONTOURS_READY_FOR_ARCHITECT_VALIDATION = YES`

## Scope valide

La vague repond a ARCH-233:

- contours de zones/buildings au pixel pres;
- format de donnees dedie;
- separation contour visuel et hitbox tactile;
- calibration par zone;
- integration runtime de la selection;
- alignement apres zoom/pan;
- qualite de rendu;
- preuve screenshot avant/apres;
- criteres UI/Demo/QA;
- aucun world map;
- aucun BEE-881;
- aucun serveur officiel/live.

## BEE composees

- BEE-1001: inventaire des zones/buildings.
- BEE-1002: format de donnees des contours.
- BEE-1003: separation contour visuel / hitbox tactile.
- BEE-1004: fichier calibration polygones zones.
- BEE-1005: handoff tooling auteur contours.
- BEE-1006: integration runtime selected zone outline.
- BEE-1007: alignement apres zoom/pan.
- BEE-1008: qualite de rendu contour.
- BEE-1009: garde anti halo generique.
- BEE-1010: priorisation multi-zone.
- BEE-1011: screenshots avant/apres.
- BEE-1012: criteres QA alignement pixel.
- BEE-1013: QA hitbox confortable invisible.
- BEE-1014: option asset mask/overlay.
- BEE-1015: versioning/review contour data.
- BEE-1016: performance runtime.
- BEE-1017: contact sheet comparative Demo.
- BEE-1018: garde no-world/no-server.
- BEE-1019: handoff QA.
- BEE-1020: gate contours pixel-perfect.

## Dispatch parallele

### Builder-A

Implementer le coeur runtime et calibration:

- BEE-1001
- BEE-1002
- BEE-1003
- BEE-1004
- BEE-1006
- BEE-1007
- BEE-1010

Objectif: remplacer les contours approximatifs par une couche contour/hitbox separée et calibrable.

### Builder-B

Preuves, regression guards et manifests:

- BEE-1009
- BEE-1011
- BEE-1015
- BEE-1016
- BEE-1018
- BEE-1019
- BEE-1020

Objectif: verifier pas de halo generique, produire avant/apres, versionner, garder scope ruche.

### UI-B

Direction visuelle et criteres rendu:

- BEE-1005
- BEE-1008
- BEE-1012
- BEE-1013
- BEE-1014

Objectif: epaisseur, glow, lisibilite, contour vs hitbox, options mask/overlay.

### Demo-A

Attend Builder-A + Builder-B + UI-B.

Puis produire BEE-1017 contact sheet comparative.

### QA-A

Attend Demo-A.

Valider BEE-1012/1013/1019/1020.

### Server-A

Pas de tache active. Aucun impact serveur.

## Interdictions

- Ne pas relancer carte monde.
- Ne pas debloquer BEE-881.
- Ne pas pretendre serveur officiel/live.
- Ne pas accepter cercle/halo generique comme solution finale.
- Ne pas confondre hitbox tactile et contour visuel.
