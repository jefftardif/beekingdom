# ARCH-193 - Decision QA-069, gate BEE-840 et relance Planner BEE-841/BEE-860

Date: 2026-07-12
Responsable: Architect
Priorite: Ruche jouable produit, pas carte monde

## Rapports lus

- `C:\projets\beekingdom\QA\QA_DEMO_069_BEE832_840_VALIDATION.md`
- `C:\projets\beekingdom\prompt_demo\rapports\DEMO-069_BEE832_840\DEMO-069_Report.md`
- `C:\projets\beekingdom\prompts_codex\rapports\BuilderA_BEE832_833_Report.md`
- `C:\projets\beekingdom\prompts_codex\rapports\BuilderB_BEE840_Report.md`
- `C:\projets\beekingdom\prompts_codex\rapports\BuilderC_BEE832_833_Report.md`

## Verdict QA lu

QA-069 donne:

- `QA_069_RESULT = PASS_WITH_RESERVES`
- `BEE_840_GATE_ELIGIBLE_FOR_ARCHITECT_DECISION = YES`
- `READY_FOR_ARCHITECT_REVIEW = YES`

## Decision Architecte

Le gate BEE-840 est ferme pour la vague Ruche jouable BEE-821 a BEE-840.

BEE-841 devient eligible pour planification, mais avec une restriction stricte: aucune relance carte monde, aucune expansion world map, aucune fonctionnalite carte monde officielle tant que la ruche jouable produit n'est pas suffisamment exploitable comme jeu.

## Reserves maintenues

- Preuve tactile physique reelle toujours ouverte.
- Portrait telephone encore compact.
- Builder-C reste support/automation candidate.
- Builder-B BEE-840 reste gate documentaire, pas feature runtime.
- Serveur officiel, sauvegarde officielle, economie officielle et armee persistante officielle ne sont pas encore valides.

## Orientation BEE-841 a BEE-860

Planner doit composer la prochaine vague comme une vague Ruche jouable produit et serveur-first, pas carte monde.

Themes prioritaires:

1. Ressources qui augmentent de facon compréhensible et persistable.
2. Amelioration batiment avec etats joueur complets: cout, timer, completion, echec, annulation si autorisee.
3. Entrainement troupes avec queue, completion, armee visible et progression locale claire.
4. Pont serveur authoritative non-live ou dev-only pour ressources, upgrade, training, idempotence, anti double-spend.
5. Preparation sauvegarde officielle future sans claim production.
6. UX tactile reelle et confort portrait/tablette.
7. Demo/QA centrees sur action joueur, pas sur simple capture statique.
8. Gate avant toute carte monde.

## Equipes

Planner peut travailler maintenant.

Builder-A/B/C, UI-A, Demo-A, QA-A et Server-A attendent la prochaine vague planifiee avant nouvelles taches, sauf correction urgente.
