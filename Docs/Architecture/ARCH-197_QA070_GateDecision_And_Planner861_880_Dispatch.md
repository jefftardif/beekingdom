# ARCH-197 - Decision QA-070, gate BEE-860 et dispatch Planner BEE-861 a BEE-880

Date: 2026-07-12

## Decision Architecte

QA-070 est acceptee avec reserves.

La vague BEE-841 a BEE-860 est consideree suffisamment validee pour passer a la planification suivante.

## Rapport QA valide

- `C:/projets/beekingdom/QA/QA_DEMO_070_BEE842_860_VALIDATION.md`

## Verdict QA

- `QA_070_RESULT = PASS_WITH_RESERVES`
- `BEE_860_GATE_ELIGIBLE_FOR_ARCHITECT_DECISION = YES`
- `READY_FOR_ARCHITECT_REVIEW = YES`

## Ce qui est maintenant valide

- Boucle locale ruche jouable:
  - ressources qui augmentent;
  - feedback ressources et cap local;
  - amelioration batiment avec cout, timer, progression, completion et blocage;
  - entrainement avec cout, timer, file, completion;
  - armee locale visible;
  - gardes anti double action / anti double queue;
  - boutons importants non muets;
  - panneau droit et raisons disabled utilisables.

## Reserves maintenues

- Pas de serveur officiel live.
- Pas de sauvegarde officielle.
- Pas d'economie officielle.
- Pas d'armee persistante officielle.
- Pas de preuve tactile physique reelle.
- Le runner Unity `-runTests` n'a pas produit de XML NUnit pour ce filtre; la methode batch est acceptee pour ce gate seulement.
- UI-B a agi comme support temporaire; UI-A reste responsable du scoring officiel UI.
- BEE-851 a BEE-860 restent supports/gates, pas runtime Builder-A.
- Carte du monde et BEE-861 monde restent hors scope tant que la ruche jouable produit n'est pas plus solide.

## Decision de gate

BEE-860 peut etre fermee comme gate de support server-first local.

La prochaine vague ne doit pas relancer la carte du monde.

## Orientation obligatoire BEE-861 a BEE-880

Planner doit composer la prochaine vague en priorisant la ruche jouable produit et le passage progressif vers une architecture serveur-officielle, sans pretendre que le jeu est deja live.

Priorites:

1. Connecter proprement la boucle locale aux contrats serveur dev-only existants.
2. Preparer l'autorite serveur pour les actions joueur: upgrade, training, ressources.
3. Ajouter et documenter une premiere strategie de snapshot/sauvegarde future, sans activer de save officielle si le serveur n'est pas pret.
4. Clarifier les etats UI joueur entre local preview, serveur requis, action acceptee, action refusee, action en attente.
5. Continuer les preuves tablette paysage et telephone portrait, mais sans depenser la vague sur l'esthetique pure.
6. Preparer QA a tester la boucle de jeu comme joueur: produire, depenser, ameliorer, entrainer, constater l'armee.
7. Ne pas planifier la carte du monde active, l'exploration monde, les alliances, guerres ou map MMO dans cette vague.

## Equipes concernees pour la prochaine vague

Builder-A : Oui - runtime principal ruche jouable.
Builder-B : Oui - tests/support non conflictuel et instrumentation.
Builder-C : Oui - preuves, anti-regression, tactile/device quand possible.
Server-A  : Oui - contrats serveur et autorite dev-only/pre-officielle.
UI-A      : Oui - libelles/etats joueur officiels si disponible.
Demo-A    : Oui - officialisation apres Builder.
QA-A      : Oui - validation gate.
Planner   : Oui - composition BEE-861 a BEE-880.

## Statut

READY_FOR_PLANNER_BEE_861_880 = YES
