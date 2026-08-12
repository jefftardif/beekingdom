# WorldMap Runtime Entities Wave1 - Matrix

## Direction commune

- Perspective: isometrique 3/4 compatible BearDen.
- Lumiere: key light haut gauche, ombres courtes internes, pas d'ombre projetee lourde.
- Lisibilite: silhouette claire a 100 %, 50 % et 25 %.
- Fond final attendu: transparent.
- Terrain: jamais peint dans le master, spawn runtime seulement.
- Faction: petite surcouche runtime separee, pas integree au corps principal.
- Compatibilite: aucun coordonnees codees en dur, utilisable sur future carte 50x50.

## Ruches

| Groupe | Paliers | Corps | Differenciation | Overlay faction |
| --- | --- | --- | --- | --- |
| Neutre pre-niveau 10 | 1, 4, 7, 9 | petite ruche cire brute, base vegetale | volume et cellules visibles | fanion/medaillon runtime |
| Garde royale | 10, 20, 35, 50 | ruche fortifiee, alvéoles cerclées | plaques cire/ambre, gardes visuels | blason runtime |
| Assaillante | 10, 20, 35, 50 | ruche anguleuse, pointes cire/propolis | silhouettes agressives, sorties multiples | blason runtime |
| Nourriciere | 10, 20, 35, 50 | ruche ronde, nurseries visibles | miel chaud, alvéoles larges | blason runtime |
| Eclaireuse | 10, 20, 35, 50 | ruche elancee avec vigies | antennes, plateformes legeres | blason runtime |
| Alchimiste | 10, 20, 35, 50 | ruche atelier, fioles/propolis | reflets violets/verts subtils | blason runtime |

## Ressources

| Ressource | Pauvre | Moyen | Riche | Notes runtime |
| --- | --- | --- | --- | --- |
| Nectar | petite fleur sucriere | bouquet lumineux | massif fleuri dense | node recoltable, non peint |
| Pollen | touffe poudreuse | capsules jaunes | gerbe dorée | lisible par couleur chaude |
| Eau | goutte/rocher humide | petite mare claire | bassin cristallin | pas de riviere peinte |
| Cire | fragments cireux | depot alveole | amas cire + rayons | proche ruche possible |
| Miel | pot naturel/ruissellement | plaques ambrees | gros depot brillant | eviter aspect UI |
| Gelee royale | perle pale rare | bulbe nacre | nid nacre protege | rarete tres lisible |
| Propolis | morceaux resineux | souche resine | amas sombre brillant | brun/vert, distinct du bois |

## Bestiaire

| Tier | Type | Taille | Role | Silhouette |
| --- | --- | --- | --- | --- |
| T1 | puceron voleur | solo | nuisance | petit ovale, antennes |
| T2 | fourmi coupeuse | petit groupe | harcelement | mandibules, corps segmente |
| T3 | araignee sauteuse | solo elite | embuscade | pattes hautes, abdomen rond |
| T4 | mante predatrice | duo/elite | burst | bras faucilles |
| T5 | frelon brigand | escouade | menace volante | ailes larges, abdomen rayé |
| T6 | scorpion des racines | mini-raid | tank | pinces + queue |
| T7 | reine frelon ancienne | raid | boss | grande silhouette volante, couronne naturelle |

## Contre-revue locale

- Ours: exclu du bestiaire Wave1.
- Routes/UI/anneaux massifs: exclus des corps d'assets.
- Surcouches faction: separees du corps principal.
- Ressources: spawn runtime aleatoire, jamais peintes dans le terrain.
- Paliers: exprimes par masse, hauteur, detail et contraste, pas seulement par couleur.

