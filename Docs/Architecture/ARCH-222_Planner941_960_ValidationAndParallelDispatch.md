# ARCH-222 - Planner 941-960 Validation And Parallel Dispatch

Date: 2026-07-12

## Decision

Architecte valide Planner BEE-941 a BEE-960.

Rapport:

- `C:/projets/beekingdom/prompts_codex/rapports/Planner_BEE941_960_Report.md`
- Verdict: `BEE-960_READY_FOR_ARCHITECT_VALIDATION = YES`

## Scope valide

La vague respecte le gate courant:

- ruche jouable produit prioritaire;
- aucune carte monde;
- aucun BEE-881;
- aucun serveur officiel/live;
- aucune exploration/alliance/guerre/map MMO;
- preuve physique device toujours separee de la preuve locale/demo.

## BEE composees

- BEE-941: intake prochaine vague ruche jouable et garde reserve physique.
- BEE-942: protocole install/lancement APK courant.
- BEE-943: pack capture telephone portrait physique.
- BEE-944: pack capture tablette paysage physique.
- BEE-945: debut session quotidienne et collecte.
- BEE-946: capacite ressources et feedback overflow.
- BEE-947: clarte choix upgrade batiment.
- BEE-948: completion upgrade et reward feedback.
- BEE-949: disponibilite choix entrainement troupes.
- BEE-950: completion entrainement et prochaine action.
- BEE-951: panneau inspection armee locale.
- BEE-952: confirmations actions et etats disabled.
- BEE-953: recovery court apres refus.
- BEE-954: lisibilite critique telephone portrait.
- BEE-955: menu permanent tablette paysage.
- BEE-956: matrice confort gestes device.
- BEE-957: continuite manifests evidence.
- BEE-958: support server future non-claim only.
- BEE-959: pack scenarios QA rapide.
- BEE-960: gate prochaine vague ruche jouable.

## Dispatch parallele

### Builder-A

Implementer le coeur jouable:

- BEE-945
- BEE-946
- BEE-947
- BEE-948
- BEE-949
- BEE-950
- BEE-951

Objectif: rendre la session quotidienne plus jouable sans carte monde ni serveur officiel.

### Builder-B

Implementer preuves/etats transversaux et garde non-regression:

- BEE-952
- BEE-953
- BEE-957
- BEE-959
- BEE-960

Objectif: confirmations, disabled states, recovery refus, manifests propres et pack QA rapide.

### UI-B

Specification et verification UI produit:

- BEE-954
- BEE-955
- BEE-956
- support UI pour BEE-952/BEE-953

Objectif: lisibilite telephone portrait, menus permanents tablette paysage, confort gestes.

### Server-A

Support non-claim uniquement:

- BEE-958

Objectif: maintenir la distinction serveur futur/officiel sans endpoint live ni save officielle.

### Device proof

BEE-942 a BEE-944 restent dependantes d'appareils reels. Elles peuvent etre preparees en support, mais ne doivent pas etre declarees fermees sans captures/video appareil.

## Equipes en attente

Demo-A attend Builder-A + Builder-B + UI-B + Server-A.

QA-A attend Demo-A.

## Interdictions

- Ne pas relancer la carte monde.
- Ne pas debloquer BEE-881.
- Ne pas creer de claim serveur officiel/live.
- Ne pas transformer la reserve device physique en PASS sans appareil reel.
