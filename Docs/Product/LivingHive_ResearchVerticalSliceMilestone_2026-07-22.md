# LivingHive — tranche verticale Recherche

## Résultat

La fausse piste « Optimisation cire » et la troisième file inactive ont été remplacées par une première boucle de recherche abeille, mobile et persistante. Le joueur peut lancer une seule étude à la fois, voir son coût et son résultat, suivre la minuterie dans la file latérale, quitter le jeu, reprendre l’opération et obtenir l’effet exactement une fois.

Deux études complémentaires, non exclusives et terminables une seule fois, sont disponibles :

- `foraging_routes_i` — **Danse des routes I** : 240 miel, 90 pollen, 16 secondes, 2 abeilles affectées, +2 % de production de miel dans l’aperçu local;
- `tempered_combs_i` — **Rayons tempérés I** : 180 miel, 120 pollen, 16 secondes, 2 abeilles affectées, +5 % de capacité de cire dans l’aperçu local.

Il n’y a ni monnaie premium, ni raccourci payant, ni collecte automatique. Les deux choix se complètent et n’accordent aucune puissance militaire irremplaçable.

## Expérience mobile

- Portrait : l’entrée `Plus -> Recherche` conserve quatre cibles principales dans le rail inférieur; le menu présente les deux études dans un panneau 390x844, avec boutons de 44 px.
- Paysage : le panneau Recherche présente les mêmes choix et la troisième file montre maintenant le nom court, la progression et le temps restant réels.
- La zone `research_node` ouvre directement le menu, sans tenter une amélioration de bâtiment générique.
- Deux abeilles s’animent autour du nœud pendant une étude.
- Les textes français et anglais sont localisés. Les catalogues contiennent **683/683** clés uniques, avec ensembles strictement identiques.

## Frontière appareil / serveur

### Appareil

L’appareil conserve le rendu, la préférence de langue, une copie locale non officielle de l’opération, la liste locale des études terminées et, lors du futur raccordement, la clé d’idempotence en attente. Ce journal sert à l’aperçu hors ligne et à la reprise visuelle; il ne constitue jamais l’autorité économique.

Le journal local migre de `v1` à `v2`, conserve les files construction/formation, ajoute la file recherche et une liste de complétions. Une complétion est inscrite avant l’application de l’effet et est redérivée au chargement, ce qui empêche le double bonus après redémarrage.

### Serveur

Le noyau préparé par l’Intégrateur possède les identifiants autorisés, les prérequis, les soldes, la transaction de débit, l’horloge UTC, l’opération active, la révision, l’idempotence, la complétion et les effets officiels. Une même clé et une même charge rejouent le reçu; une charge contradictoire est refusée. Une complétion anticipée est refusée selon l’heure serveur.

Le drapeau `LivingHiveResearch:Enabled=false` reste fermé par défaut et en Production. Aucune route HTTP, activation, candidature, synchronisation ou mise en production n’est prétendue. Rapport serveur : `Docs/ProductionIntegration/LivingHive_ResearchQueue_ServerCore_2026-07-22.md`.

## Validation

- Unity `6000.5.3f1`, F8 LivingHive : sortie 0, marqueur `LivingHive manual collection checks passed.`, zéro `error CS`, journal `Artifacts/LivingHiveResearch_F8.log`.
- Deux tests dédiés couvrent coûts exacts, file unique, reprise, complétion idempotente, effets combinés et migration `v1 -> v2`.
- Serveur : `LivingHiveResearchTests` 2/2, `BeeKingdom.HiveOperations.Tests` 26/26, build Release 0 erreur; avertissement SqlClient historique seulement.
- Captures : sortie 0, zéro `error CS`, manifeste `Docs/Product/Evidence/LivingHiveResearch/LivingHiveResearch_CaptureManifest.md`.
- `LivingHive_Research_Portrait_More_390x844.png` : navigation mobile;
- `LivingHive_Research_Portrait_Menu_390x844.png` : deux études lisibles;
- `LivingHive_Research_Landscape_Running_1600x900.png` : étude active, abeilles et file réelle.

La première inspection paysage a révélé un résultat trop long et un nom débordant dans la file. Les libellés courts ont été localisés, le panneau a été resserré, puis F8 et les trois captures ont été rejoués et inspectés.

## Fondations préservées

- Scène canonique 50x50 : 7 776 octets, SHA-256 `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3`.
- Image de base LivingHive : 7 489 785 octets, SHA-256 `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`.
- Scène `LivingHive.unity` : 9 160 octets, SHA-256 `ECCFE9AA81AE883317E4E951C8552DCEF1A156179F35480567466AB95A9708E7`.
- Aucun terrain, tuile, image de carte, image de ruche ou scène n’a été modifié.

## Fichiers client exacts

- `Assets/BeeKingdom/Playground/LocalPreviewResearch.cs` et `.meta`
- `Assets/BeeKingdom/Playground/LocalPreviewQueueJournal.cs`
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveManualCollectionTests.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveResearchCapture.cs` et `.meta`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.fr-CA.json`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.en-US.json`
- preuves et manifeste sous `Docs/Product/Evidence/LivingHiveResearch`

Une compilation globale initiale a été bloquée par la frontière d’assemblage du fichier Communication déjà laissé en état sûr. Le thread Communication a remplacé la dépendance directe à `MobileAccountSessionGate` par son contrat injectable local dans son unique fichier propriétaire, puis s’est regelé. La compilation globale finale confirme la disparition des deux erreurs sans modification du présentateur, des tests ou des catalogues par Communication.

## Portes suivantes

- exposer les commandes HTTP authentifiées derrière le drapeau fermé;
- ajouter les tests HTTP, SQL jetable, TLS/IIS et Android sous .NET 8 natif;
- raccorder la session mobile officielle et le stockage protégé de la clé d’idempotence;
- réconcilier systématiquement le journal local avec l’instantané et la révision serveur;
- conserver le mode local explicitement non officiel tant que ces portes restent fermées.
