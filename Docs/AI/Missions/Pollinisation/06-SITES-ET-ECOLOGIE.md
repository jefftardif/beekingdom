# 06 — Sites floraux, écologie et cycle de floraison

## Contraintes

**[APPROUVÉ]** Conserver le paysage/tuiles, ajouter des **points d'intérêt interactifs** et informations. Fertilité et régions sont des états serveur partagés ; ne pas simuler une économie officielle uniquement côté Unity.

## Typologie des sites (recommandée)

- **Bosquet public** : accessible à un individu ; récompense modeste, faible influence régionale.
- **Champ fleuri** : enjeu de plusieurs joueurs, saison florale limitée.
- **Grande prairie** : objectifs collectifs d'alliance et pollinisation structurée.
- **Clairière rare** : fenêtre d'accès courte, famille florale spécifique, matériel génétique rare possible.
- **Arbre mellifère ancestral** : site stratégique de haut niveau pouvant requérir plusieurs routes.
- **Région florale** : agrégation serveur de plusieurs sites, pas une nouvelle tuile.
- **Floraison légendaire** : projet mondial temporaire, éventuellement réservé à coalition.

## Trois indicateurs indépendants

| Indicateur | Plage suggérée | Signification |
|---|---|---|
| Fertilité | 0 à 100 | potentiel de régénération sur le long terme |
| Biodiversité | 0 à 100 | qualité/variété de familles florales et chances d'apparition rare |
| Réserves florales | 0 à capacité | ressources immédiatement récoltables |

Ne pas mélanger « progression de mission », « progression de projet », « réserves » et « fertilité » : quatre compteurs avec comportements différents.

## Cycle écologique

**Croissance -> Floraison -> Pollinisation -> Régénération -> Croissance.**

- Une fleur ou un champ a une **famille florale** et des fenêtres de floraison.
- Les butineuses récoltent nectar/pollen, avec pression sur les **réserves**.
- Des transports de pollen compatibles contribuent à une **pollinisation effective**.
- Les contributions améliorent potentiellement la fertilité et/ou la biodiversité, avec **effet différé** sur production future.
- Les réserves repoussent graduellement ; une régénération naturelle minimale évite la stérilité permanente.
- Une production abondante peut attirer créatures/alliances (événement déclenché, non permanent).

## Modes de mission

**Récolte intensive :** bonus de récolte immédiate, baisse temporaire des réserves et parfois de la fertilité en cas d'excès.  
**Butinage équilibré :** transfert de pollen et récolte sans effet très négatif ; excellent comportement par défaut.  
**Restauration écologique :** moins de récolte, plus de progression fertilité/biodiversité ; rôle puissant à l'échelle alliance.

Les poids sont configurables ; **aucune valeur chiffrée n'est approuvée**. Une récolte intensive ne peut rendre un site définitivement stérile.

## Autorité et concurrence

Un identifiant de site stable (**worldId/regionId/siteId**) permet la synchronisation serveur sur plusieurs sessions, joueurs et alliés. L'état mondial est **partagé**, pas dupliqué dans chaque ruche. Les changements doivent être transactionnels et versionnés, ou résolus avec contrôle de concurrence documenté.

Une action réalisée par deux joueurs simultanément ne doit pas puiser deux fois la même réserve. Les récompenses tiennent compte des capacités réellement retirées du site.

## Échelle de la région

Agrégat pondéré des sites actifs, avec paramètres de climat/biome éventuels. NE PAS calculer chaque tuile. Un changement écologique peut être représenté par badges, particules et overlays localisés sans toucher aux textures du terrain.

## Saisons, météo et événements

Prévoir au contrat : contexte de saison, famille florale, effets météorologiques, ravageurs, floraison exceptionnelle. À l'Alpha, utiliser un cycle simple et déterministe ; simulations saisonnières et défis écologiques plus riches à la Bêta.

## Occupation et souveraineté

La fertilité appartient au **monde**. La propriété d'un relais et les droits d'accès économiques appartiennent à un système d'alliance distinct. Un transfert de propriété n'efface pas l'historique écologique.

## Invariants essentiels

- Fertilité/biodiversité bornées ; réserves entre zéro et capacité.
- Aucun champ négatif ni récolte supérieure aux quantités réellement présentes.
- Des sites publics toujours accessibles aux nouveaux joueurs.
- Toute baisse temporaire doit avoir une voie de régénération et un affichage explicite.
- La pollinisation d'un site ne suffit jamais à réclamer automatiquement un territoire.

