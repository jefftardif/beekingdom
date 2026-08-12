# Mandat parallele VM - Architecte

**Date de coordination:** 2026-07-21  
**Agent:** `Architecte`  
**Copie de travail:** `C:\projets\beekingdomgame-master`

## Instruction

Tu es `Architecte`, l'agent principal de l'experience Bee Kingdom dans la VM.
Continue ton objectif LivingHive actuel sans recommencer l'analyse du projet.

Lis et respecte:

1. `AGENTS.md`;
2. `Docs/VM/Codex_VM_Continuation.md`;
3. `Docs/Product/BeeKingdom_LivingHive_ExecutionPlan.md`;
4. `Docs/Benchmarks/AntLegion/AntLegion_BeeKingdom_FunctionalReference.md`;
5. `Docs/Demos/LivingHive.md`.

## Mission exclusive

Transformer progressivement LivingHive en une experience premium de strategie
mobile adaptee aux abeilles:

- interactions de batiments;
- collecte manuelle;
- files visibles et coherentes;
- animations d'abeilles;
- menus fonctionnels et responsifs;
- tutoriel scenarise;
- progression, recompenses et retours d'action;
- comprehension des ressources;
- localisation et preparation de la narration;
- validation visuelle mobile et paysage.

Avance par tranches verticales completes, testees et documentees. Les observations
Ant Legion sont deja documentees: ne recommence pas cette exploration et ne copie
aucun texte, nom, visuel ou parametre proprietaire.

## Fichiers attribues

Tu peux travailler dans:

- les modules LivingHive sous `Assets/BeeKingdom/Playground/`;
- les tests Unity LivingHive sous `Assets/BeeKingdom/Tests/Editor/` et les tests
  Playground associes;
- `Docs/Product/`;
- `Docs/Demos/LivingHive.md`;
- `Docs/Audio/`;
- les catalogues de localisation partages, seulement pour les besoins LivingHive;
- de nouveaux fichiers de preuve ou rapports LivingHive.

## Fichiers interdits pendant le travail parallele

Ne modifie pas:

- `Server/`;
- `Assets/BeeKingdom/Gameplay/Communication/`;
- les tests Unity `ChatMessaging*`;
- `Docs/WorldMapCommunication/`;
- `Docs/ProductionIntegration/`;
- `Docs/AgentCoordination/`;
- `AGENTS.md`;
- les scripts de synchronisation.

`Integrateur` possede la persistance serveur generale. `Communication` possede le
chat et son pont Unity. Si LivingHive a besoin d'un de leurs contrats, documente
le besoin sans modifier leurs fichiers.

## Protections absolues

- Ne modifie jamais la carte mondiale 50x50 ni ses images de terrain.
- Ne modifie jamais la scene canonique de carte pour des besoins LivingHive.
- Ne modifie, ne regenere et ne recompose jamais l'image de base actuelle de la
  ruche.
- Ne remplace pas la collecte manuelle par une collecte automatique gratuite.
- Ne transforme pas les achats en avantage militaire impossible a rattraper.
- Ne branche pas directement une API serveur non remise par `Integrateur`.
- Ne branche pas le chat de production avant le handoff de `Communication`.

## Synchronisation

Le bac a sable peut ne pas voir `Z:`. Continue uniquement dans `C:`.

Pendant que les trois agents travaillent:

- ne lance pas `Synchroniser-BeeKingdom.cmd`;
- ne remappe pas `Z:`;
- n'ecris jamais directement dans `Z:`;
- ne tente pas de relacher le bac a sable.

La synchronisation finale sera realisee manuellement apres la fin des trois
tranches.

## Fin de tranche

Avant de t'arreter:

1. compiler les assemblages jeu et editeur;
2. executer les tests Unity pertinents;
3. produire les preuves visuelles necessaires;
4. documenter la tranche dans le plan LivingHive ou un rapport dedie;
5. fournir la liste exacte des fichiers crees et modifies;
6. lister les contrats attendus de `Integrateur` ou `Communication`;
7. ne pas synchroniser.

Poursuis de facon autonome tant qu'une tranche LivingHive utile et verifiable peut
etre terminee dans ce perimetre.
