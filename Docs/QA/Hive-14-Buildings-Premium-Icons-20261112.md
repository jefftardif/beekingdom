# BeeKingdom — audit des icônes premium des 14 bâtiments

**Date :** 12 novembre 2026
**Source officielle :** `C:\projets\beekingdomgame-master` — branche `main`
**Portée :** présentation visuelle Unity sur PCJEFF. Aucune modification serveur, donnée joueur, économie ou transaction.

## Correction de l'Académie

Les 11 cartes (État, Développement, Palais royal, Vocation, quatre programmes, Caserne, Recherche et Diplômes) utilisent chacune une illustration premium dédiée `PremiumBeeIcons/academy-card-*`, sur une toile RGBA de 384 × 384 pixels avec marge transparente et médaillon doré complet. La logique de carte partagée affiche l'art dans un rectangle de 100 × 100, sous un en-tête de 126 unités, avec UV complets. Le contenu de toutes les cartes est descendu de 64 unités pour ne pas être recouvert par l'en-tête; le contenu de défilement et les boutons conservent leur emplacement relatif.

## Audit des bâtiments

| Bâtiment | Clé originale | Résultat de l'audit |
|---|---|---|
| Nurserie | `nursery` | Icône hexagonale premium complète; pas de modification de sa logique |
| Réserve de miel | `honey` | Icône premium intacte; trois cartes de production déjà corrigées |
| Caserne | `guard` | Source remastérisée; anciennes lettres coupées supprimées; portraits de gardes normalisés |
| Défense | `defense` | Icône principale premium complète; icônes de gardes normalisées |
| Génétique | `flower` | Icône principale premium complète; illustrations de cartes conservées |
| Recherche | `research` | Icône de livre premium complète |
| Entrepôt | `inventory` | Source remastérisée; anciens caractères coupés supprimés; cartes de production déjà corrigées |
| Atelier de cire | `production` | Source remastérisée; ancien lettrage coupé supprimé; cartes de production déjà corrigées |
| Infirmerie | `help` | Icône de soin premium et illustration spécialisée de convalescence conservées |
| Académie | `book` | 11 médaillons dédiés, source et mise en page corrigées |
| Banque | `royal-jelly` | Source remastérisée, coupe d'or conservée sans texte résiduel |
| Palais royal | `queen-core` | Icône de couronne premium complète |
| Centre d'alliance | `alliance-center` | Icône de ruche premium complète |
| Hall des championnes | `quests` | Source d'archives remastérisée; trophée normalisé sans lettrage coupé |

Autres icônes présentes dans plusieurs fenêtres remastérisées : `guard-bee`, `worker`, `worker-bee`, `queen-bee`, `trophy`, `propolis`, `drone`, `shop` et `settings`. Chaque icône d'origine a été sauvegardée avant modification dans `C:\BeeKingdom\qa\BK-AcademyPremiumIcons-20261112\legacy-originals`.

## Non-régression et limites

- Les identifiants GUID Unity et les fichiers `.meta` des icônes existantes sont préservés pour ne pas casser leurs références, y compris les importateurs Sprite.
- Les nouvelles icônes de l'Académie utilisent de nouveaux GUID distincts et sont importées comme textures complètes.
- Les calculs, contrôleurs, traductions FR/EN, accès au serveur, récoltes et boutons ne sont pas modifiés.
- La compilation C# du jeu et des tests éditeur est vérifiée séparément. Les tests EditMode via Unity Test Runner et une vérification pixel à pixel dans la GameView du projet principal restent des niveaux supplémentaires distincts et ne doivent pas être considérés comme réussis sans rapport d'exécution.

## Vérifications effectuées

- Compilations C# du jeu et de l'assemblage de tests Unity : **0 erreur**; les avertissements préexistants concernent d'autres classes.
- Contrôle direct et complet des pixels : **14 icônes officielles résolues**, **28 sources PNG 384 × 384** (14 icônes historiques remastérisées, 11 médaillons Académie et 3 médaillons de production), **28 GUID distincts**, **0 pixel opaque dans la marge de 24 pixels**, **0 échec**. Journal sur PCJEFF : `C:\BeeKingdom\qa\BK-AcademyPremiumIcons-20261112\SourceGate.txt`.
- Test Unity EditMode tenté sur copie isolée; arrêté avant l'exécution des tests parce que Unity Package Manager ne parvient pas à ouvrir le canal IPC (`Upm-43392`). Aucun succès EditMode n'est donc revendiqué pour ce correctif.
- Le jeu reste ouvert sur PCJEFF et une vérification GameView avec la scène officielle reste requise pour confirmer l'aspect réel à l'écran et sur Android.
- Les PNG remplacés sont sauvegardés hors Git; aucune modification SQL/API ou de progression joueur.