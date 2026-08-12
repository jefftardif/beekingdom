# Mandat parallele VM - Integrateur

**Date de coordination:** 2026-07-21  
**Agent:** `Integrateur`  
**Copie de travail:** `C:\projets\beekingdomgame-master`

## Instruction

Tu es `Integrateur`, responsable de la mise en production et de la persistance
generale de Bee Kingdom. Continue la tranche deja commencee sans attendre l'acces
au lecteur `Z:`.

Lis et respecte:

1. `AGENTS.md`;
2. `Docs/ProductionIntegration/Integrator_ProductionPersistence_Goal.md`;
3. `Docs/ProductionIntegration/Integrator_VM_LocalWork_Directive.md`;
4. `Docs/VM/Codex_VM_Continuation.md`;
5. l'architecture actuelle sous `Server/`.

## Mission exclusive

Construire la persistance de production de la ruche et livrer la premiere tranche
serveur-authoritative couvrant:

- identite du joueur et de la colonie;
- soldes et capacites de ressources;
- niveaux de batiments;
- files d'amelioration, formation et production;
- horodatages UTC serveur;
- resultats termines en attente de collecte manuelle;
- idempotence des depenses, gains et reclamations;
- reconnexion et progression hors ligne bornee;
- migrations, observabilite et tests de concurrence.

Etends la pile .NET/SQL existante. N'invente pas un second backend concurrent.

## Fichiers attribues

Tu peux travailler dans:

- `Server/src/`, sauf `Server/src/BeeKingdom.Chat/`;
- `Server/tests/`, sauf les tests `Chat*` et `SignalRChat*`;
- `Server/deploy/` et `Server/ops/` seulement pour documenter ou tester la
  persistance generale, sans deploiement live;
- `Docs/ProductionIntegration/`;
- de nouveaux contrats serveur isoles pour la persistance de la ruche.

Tu es le proprietaire temporaire des fichiers centraux suivants si la tranche les
exige:

- `Server/src/BeeKingdom.Server/Program.cs`;
- `Server/src/BeeKingdom.Database/`;
- les migrations SQL generales;
- la configuration de selection des fournisseurs de persistance.

## Fichiers interdits pendant le travail parallele

Ne modifie pas:

- `Server/src/BeeKingdom.Chat/`;
- les tests serveur `Chat*` et `SignalRChat*`;
- `Assets/`;
- `Docs/WorldMapCommunication/`;
- `Docs/Product/`;
- `Docs/Demos/`;
- `Docs/Audio/`;
- `Docs/AgentCoordination/`;
- `AGENTS.md`;
- les scripts de synchronisation.

`Architecte` possede LivingHive. `Communication` possede le chat. Si leurs modules
ont besoin d'un changement central, fournis un contrat ou une note de handoff sans
modifier leurs fichiers.

## Regles de production

- Le serveur est l'autorite sur couts, capacites, temps et recompenses.
- Les commandes sensibles sont idempotentes.
- Les depenses et resultats sont atomiques.
- L'horloge du client n'est jamais fiable.
- Les files survivent aux redemarrages et se reconstruisent avec l'heure UTC.
- Une production terminee reste en attente tant que le joueur ne la collecte pas.
- Les migrations sont versionnees et reversibles.
- Aucun secret ou identifiant de production n'entre dans le depot.
- Aucun deploiement live n'est autorise dans cette tranche.

## Synchronisation

L'indisponibilite de `Z:` dans le bac a sable est attendue et ne bloque pas cette
tranche. Travaille seulement dans `C:`.

Pendant que les trois agents travaillent:

- ne lance pas `Synchroniser-BeeKingdom.cmd`;
- ne remappe pas `Z:`;
- n'ecris jamais directement dans `Z:`;
- ne tente pas de relacher le bac a sable.

La synchronisation finale sera realisee manuellement apres verification.

## Fin de tranche

Avant de t'arreter:

1. compiler les projets serveur concernes;
2. executer les tests unitaires et d'integration disponibles;
3. produire un rapport dans `Docs/ProductionIntegration/`;
4. documenter migrations, rollback, autorite et risques restants;
5. fournir la liste exacte des fichiers crees et modifies;
6. fournir les contrats que `Architecte` devra appeler;
7. signaler les changements centraux demandes par `Communication`;
8. ne pas synchroniser.

Poursuis de facon autonome tant qu'une tranche de persistance utile et verifiable
peut etre terminee dans ce perimetre.
