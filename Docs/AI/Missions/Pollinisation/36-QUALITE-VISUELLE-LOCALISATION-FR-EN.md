# 36 — Qualité visuelle et localisation bilingue : exigences d'implémentation

## Direction artistique approuvée

Conserver les tuiles, paysages et scène mondiaux. Les critiques portent sur **l'activité de la carte, les icônes trop fades et la faible lecture des zones** malgré la légende. Apporter une couche de gameplay distincte, pas refaire les textures de terrain.

## Carte : contrat des objets de gameplay

- Chaque site floral a une silhouette reconnaissable en moyen zoom et un indicateur de rareté/activité.
- Couleurs, contrastes, icônes et animations servent à lire une situation, pas simplement décorer.
- Au zoom éloigné : agrégation par région et « points chauds » (grande floraison, projet, ours).
- Au zoom moyen : marqueurs distincts de ressources classiques, régions d'alliance, relais et sites menacés.
- Au zoom rapproché : essaims civils et escortes militaires distincts, orientation et états clairs.
- Option de masquer les routes et les couches pour ne pas rendre la carte illisible.
- Ne pas représenter la zone fertile uniquement avec une couleur de tuile : lisibilité via contours, noms contextualisés, marqueurs et jauges.

## Interface premium

Réutiliser fenêtre Activités v4 en plein écran :
- aucun élément de ruche visible/cliquable à travers ;
- titre non recouvert par retour ;
- scroll vertical utilisable à la souris, drag et scrollbar ;
- aucun double scroll parasite ;
- stabilité de l'en-tête, de la sélection et du focus pendant refresh ;
- boutons mutation bloqués en cours d'appel, sans clignotement intermittent ;
- adaptation paysage mobile et PC.

Si composant de UI officiel n'est pas disponible sur la scène utilisée, afficher une indisponibilité lisible au lieu d'une fausse fonctionnalité.

## Table de terminologie contrôlée

| Français fr-CA | English en-US | Sens |
|---|---|---|
| Pollinisation | Pollination | mécanisme écologique |
| Butineuse | Forager bee | civile spécialisée |
| Essaim civil | Civilian swarm | groupe affecté |
| Champ floral | Floral field | site de ressources |
| Bosquet | Grove | petite échelle |
| Fertilité | Fertility | capacité régénérative |
| Biodiversité | Biodiversity | variété florale |
| Réserves florales | Floral reserves | quantité récoltable |
| Récolte intensive | Intensive harvesting | rendement présent |
| Butinage équilibré | Balanced foraging | rendement durable |
| Restauration écologique | Ecological restoration | fertilité future |
| Relais floral | Floral outpost | installation d'alliance |
| Projet de floraison | Bloom project | objectif collectif |
| Pacte de floraison | Bloom pact | accord entre alliances |
| Grande floraison | Great bloom | événement |
| Retour à la ruche | Return to hive | phase de mission |
| Escorte militaire | Military escort | protection armée |
| Tanière de l'ours | Bear's den | repère permanent / raid |
| Contribution qualifiée | Qualified contribution | points admissibles au projet |
| Participants distincts | Unique participants | vrais joueurs requis |

## Table de messages/états (exemples)

- « Essaim disponible » / « Swarm available »
- « En route vers le champ » / « Traveling to the field »
- « Récolte en cours » / « Gathering in progress »
- « Pollen en livraison » / « Delivering pollen »
- « Pollinisation terminée » / « Pollination completed »
- « Votre contribution : {value} points » / « Your contribution: {value} points »
- « Membres participants : {count}/{required} » / « Participating members: {count}/{required} »
- « Le champ ne produit plus actuellement » / « The field is not currently producing »
- « Alliance ou pacte requis » / « Alliance or pact required »
- « Les butineuses sont déjà en mission » / « Foragers are already assigned »
- « L'expédition est toujours en cours » / « The expedition is still active »

Paramètres {value}/{count} sont des **placeholders sémantiques**, pas des modèles JS.

## Unicode

Tous les JSON/Markdown/source doivent conserver accentuation française : é è ê à ù ç œ. Éviter codage Windows legacy, caractères U+FFFD et apostrophes corrompues. Tester roundtrip fichiers et rendu Unity.

## Organisation des clés

Préférer préfixe structuré : pollination.site.*, pollination.mission.*, pollination.mode.*, pollination.alliance.*, pollination.error.*, pollination.event.*. Ne pas utiliser des textes d'UI comme clés du serveur.

## Accessibilité

Contraste, texte non tronqué, différenciation par icône et texte en plus de la couleur, écran tactile taille raisonnable, absence de scintillement, soutien d'un joueur ayant des difficultés de perception des couleurs. Gérer les chiffres longs, langages différents et pluriels.

## Tests visuels minimum

- paysage PC 1920x1080 puis mobile petit écran ;
- scroll en français et anglais ;
- champs actifs/épuisés/protégés/hostiles ;
- projet avec 0, quelques et beaucoup de participants ;
- statut de mission sous forte latence ;
- après retour arrière et ouverture répétée ;
- changement de session ;
- marqueurs superposés ; carte à faible/moyen/fort zoom.

Ne jamais écrire « visuels validés » si aucune capture PlayMode n'a été produite.

