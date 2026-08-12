# ARCH-194 - Validation Planner BEE-841/BEE-860 et dispatch equipe

Date: 2026-07-12
Responsable: Architect
Priorite: ruche jouable produit server-first, pas carte monde

## Rapports lus

- `C:\projets\beekingdom\prompts_codex\BEE-841_Playable_Hive_Server_First_Wave_Intake_Framework.md`
- `C:\projets\beekingdom\prompts_codex\BEE-842_Hive_Resource_Tick_Feedback_Persistability_Prep_Framework.md`
- `C:\projets\beekingdom\prompts_codex\BEE-844_Building_Upgrade_Cost_Timer_Completion_Framework.md`
- `C:\projets\beekingdom\prompts_codex\BEE-847_Troop_Training_Cost_Timer_Queue_Completion_Framework.md`
- `C:\projets\beekingdom\prompts_codex\BEE-849_Local_Army_Minimal_Product_Section_Framework.md`
- `C:\projets\beekingdom\prompts_codex\BEE-858_Server_Authoritative_Dev_Only_Bridge_Framework.md`
- `C:\projets\beekingdom\prompts_codex\BEE-860_Playable_Hive_Product_Server_First_Gate_Before_World_Map_Framework.md`
- Rapports `BEE-841_Report.md` a `BEE-860_Report.md`

## Decision Architecte

Planner BEE-841 a BEE-860 est valide.

La vague respecte le mandat:

- ruche jouable produit server-first;
- ressources ticks/feedback;
- amelioration batiment cout/timer/progression/completion;
- entrainement troupes cout/timer/queue/completion;
- armee locale visible non officielle;
- pont serveur authoritative dev-only/non-live;
- preparation sauvegarde future sans claim;
- confort portrait/tablette;
- preuves Demo/QA action joueur;
- gate strict avant toute carte monde.

## Non-claims confirmes

- Aucune nouvelle vague carte monde.
- Aucun world map runtime.
- Aucun serveur officiel/live.
- Aucune sauvegarde officielle.
- Aucune economie officielle.
- Aucune armee persistante officielle.
- BEE-861 reste bloquee.

## Dispatch

### Builder-A

Implementer BEE-842 a BEE-850 comme tranche runtime locale principale: ressources, upgrade, training, armee locale visible, non-claims.

### UI-A

Produire contraintes UX pour BEE-842 a BEE-855: lisibilite action, portrait/tablette, feedback immediat, armee minimale, etats erreurs/blocages.

### Server-A

Produire support BEE-858/BEE-859: bridge authoritative dev-only, contrats futurs, idempotence, anti double-spend, snapshots, preparation save sans activation.

### Builder-C

Produire matrices/tests BEE-851 a BEE-857: etats fiables, boutons non muets, confort portrait/tablette, preuve tactile/fixed HUD, timeline demo action.

### Builder-B

Produire support BEE-860: checklist gate avant carte monde, non-claims, lignes de manifeste DEMO/QA, criteres pour garder BEE-861 bloquee.

### Demo-A / QA-A

Attendre Builder-A et supports. Demo-A ne demarre pas tant que Builder-A n'a pas livre un bundle source. QA-A attend Demo-A.

## Ordre

Builder-A, UI-A, Server-A, Builder-C et Builder-B peuvent travailler en parallele.
Demo-A attend Builder-A + supports.
QA-A attend Demo-A.
